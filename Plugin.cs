using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Imui.Controls;
using Imui.Core;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using ZeepSDK.LevelEditor;
using ZeepSDK.Messaging;
using ZeepSDK.UI;
using EditorSpeedSplits.GUIManager;

namespace EditorSpeedSplits
{
    [BepInPlugin("com.andme.editorspeedsplits", "EditorSpeedSplits", MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource logger;
        private Harmony harmony;

        public static Plugin Instance { get; private set; }

        internal static LEV_LevelEditorCentral central;

        internal static string fullLevelName = "";

        private EditorSplitsToolbarDrawer _toolbarDrawer;

        private EditorSplitsGUIManager guiManager;
        GameObject uiRoot;

        private void Awake()
        {
            Instance = this;
            logger = Logger;

            harmony = new Harmony("com.andme.editorspeedsplits");
            harmony.PatchAll();

            ModConfig.Initialize(Config);

            logger.LogInfo("Plugin com.andme.editorspeedsplits is loaded!");

            LevelEditorApi.EnteredLevelEditor += OnEnteredLevelEditor;
            LevelEditorApi.ExitedLevelEditor += OnExitedLevelEditor;

            _toolbarDrawer = new EditorSplitsToolbarDrawer();
            UIApi.AddToolbarDrawer(_toolbarDrawer);

        }

        private void OnEnteredLevelEditor()
        {
            central = FindObjectOfType<LEV_LevelEditorCentral>();
            if (central == null)
            {
                logger.LogWarning("Level Editor Central not found.");
                return;
            }
            SetupEditorUI();
        }

        private void OnExitedLevelEditor()
        {
            central = null;
            if (uiRoot != null)
            {
                Destroy(uiRoot);
                uiRoot = null;
                guiManager = null;
            }
        }


        internal static void ResetSplitsForCurrentLevel()
        {

            if (!LevelEditorApi.IsTestingLevel && !LevelEditorApi.IsInLevelEditor)
                return;

            if (LevelEditorApi.IsInLevelEditor && central != null)
            {
                if (central.input.inputLocked)
                    return;
            }

            string currentFullLevelName = fullLevelName;

            if (string.IsNullOrEmpty(currentFullLevelName))
            {
                logger.LogWarning("No level loaded in the editor to reset splits for.");
                return;
            }

            ReplayManager.Instance.Replays.Remove(fullLevelName);

            GameMaster gameMaster = FindObjectOfType<GameMaster>();

            gameMaster?.SetupPersonalBestAndMedals(0f, []);

            logger.LogInfo($"Splits reset for level {fullLevelName}");
            MessengerApi.Log("[EditorSpeedSplits] Splits Reset");
        }

        private void Update()
        {
            if (!Input.GetKeyDown(ModConfig.ResetSplitsKey.Value))
                return;

            // Input globally locked / paused
            if (Time.timeScale == 0f)
                return;

            // Player typing in an input field
            if (IsTypingInInputField())
                return;

            ResetSplitsForCurrentLevel();
        }

        private bool IsTypingInInputField()
        {
            if (EventSystem.current == null)
                return false;

            var selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null)
                return false;

            return selected.GetComponent<TMP_InputField>() != null;
        }

        private void SetupEditorUI()
        {
            if (guiManager != null)
                return; // already created

            uiRoot = new GameObject("EditorSplits_Manager");
            guiManager = uiRoot.AddComponent<EditorSplitsGUIManager>();
            guiManager.Initialize();
        }

        private void OnDestroy()
        {
            UIApi.RemoveToolbarDrawer(_toolbarDrawer);
            LevelEditorApi.EnteredLevelEditor -= OnEnteredLevelEditor;
            LevelEditorApi.ExitedLevelEditor -= OnExitedLevelEditor;
            harmony?.UnpatchSelf();
            harmony = null;
        }
    }


    // PATCH: GameMaster.ReloadBestTimes
    [HarmonyPatch(typeof(GameMaster), "ReloadBestTimes")]
    class GameMaster_ReloadBestTimes_Patch
    {
        [HarmonyPrefix]
        static bool Prefix(GameMaster __instance)
        {

            if (!__instance.GlobalLevel.IsTestLevel)
                return true;

            string currentFullLevelName = Plugin.fullLevelName;

            ReplayManager.ReplayInfo replay = null;

            if (!string.IsNullOrEmpty(currentFullLevelName))
            {
                replay = ReplayManager.Instance.GetReplay(currentFullLevelName);
                Plugin.logger.LogInfo($"ReloadBestTimes called for level {currentFullLevelName}");
            }
            
            if (replay == null)
            {
                __instance.SetupPersonalBestAndMedals(0f, []);
                Plugin.logger.LogInfo("No replay found, setting personal best to 0");
            }
            else
            {
                __instance.SetupPersonalBestAndMedals(replay.Time, WinCompare.CreateSplitTimeList(replay.Splits, replay.velocities));

                Plugin.logger.LogInfo($"Replay found, setting personal best to {replay.Time}");
            }
                

            return false;
        }
    }


    // PATCH: GameMaster.GetResults2
    [HarmonyPatch(typeof(GameMaster), "GetResults2")]
    class GameMaster_GetResults2_Patch
    {
        [HarmonyPostfix]
        static void Postfix(GameMaster __instance)
        {
            if (!__instance.GlobalLevel.IsTestLevel)
                return;

            if (__instance.manager.amountOfPlayers != 1)
                return;

            var result = __instance.playerResults[0];
            if (result == null)
                return;

            if (!__instance.currentLevelMode.DidWeGetMedal(
                    LevelModeBase.MedalType.Finished, result))
                return;

            if (result.time <= 0f)
                return;

            string currentFullLevelName = Plugin.fullLevelName;

            if (string.IsNullOrEmpty(currentFullLevelName))
                return;

            ReplayManager.Instance.AddReplay(
                currentFullLevelName,
                result.time,
                result.split_times
            );
        }
    }

    // PATCH: LEV_SaveLoad.ExternalLoad
    [HarmonyPatch(typeof(LEV_SaveLoad), "ExternalLoad")]
    class LEV_SaveLoad_ExternalLoad_Patch
    {
        [HarmonyPostfix]
        static void Postfix(string filePath, bool isTestLevel)
        {
            if (!LevelEditorApi.IsInLevelEditor)
                return;

            if (isTestLevel)
                return;

            Plugin.fullLevelName = Path.ChangeExtension(filePath,null);
            Plugin.logger.LogInfo($"Set fullLevelName to {Plugin.fullLevelName}");
        }
    }

    // PATCH: LEV_SaveLoad.ExternalSaveFile
    [HarmonyPatch(typeof(LEV_SaveLoad), "ExternalSaveFile")]
    class LEV_SaveLoad_ExternalSaveFile_Patch
    {
        [HarmonyPostfix]
        static void Postfix(LEV_SaveLoad __instance, bool isTestMap)
        {
            if (isTestMap)
                return;
            Plugin.fullLevelName = Path.Combine( __instance.GetFolderWeJustSavedInto().FullName, __instance.fileName.text);
            Plugin.logger.LogInfo($"Set fullLevelName to {Plugin.fullLevelName}");
        }
    }

    // PATCH: LEV_ReturnToMainMenu.ReturnToMainMenu
    [HarmonyPatch(typeof(LEV_ReturnToMainMenu), "ReturnToMainMenu")]
    class LEV_ReturnToMainMenu_ReturnToMainMenu_Patch
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            Plugin.fullLevelName = "";
            Plugin.logger.LogInfo("Cleared fullLevelName on return to main menu");
        }
    }


    public class EditorSplitsToolbarDrawer : IZeepToolbarDrawer
    {
        public string MenuTitle => "EditorSplits";

        public void DrawMenuItems(ImGui gui)
        {
            if (gui.Menu("Reset Splits"))
            {
                Plugin.ResetSplitsForCurrentLevel();
            }
        }
    }

    public class ModConfig : MonoBehaviour
    {
        public static ConfigEntry<bool> ResetSplits;
        public static ConfigEntry<KeyCode> ResetSplitsKey;


        // Constructor that takes a ConfigFile instance from the main class
        public static void Initialize(ConfigFile config)
        {
            ResetSplits = config.Bind("1. Gameplay", "1.1 Reset Splits", true,
                "[button] Reset the current level's splits");

            ResetSplitsKey = config.Bind("2. Bindings", "2.1 Reset Splits Key", KeyCode.None,
                "Key to reset the current level's splits");

            ResetSplits.SettingChanged += OnResetSplits;
        }

        private static void OnResetSplits(object sender, System.EventArgs e)
        {
            Plugin.ResetSplitsForCurrentLevel();
        }

        private void OnDestroy()
        {
            ResetSplits.SettingChanged -= OnResetSplits;
        }

    }



}
