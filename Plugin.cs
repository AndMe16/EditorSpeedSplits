using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EditorSpeedSplits.GUIManager;
using HarmonyLib;
using Imui.Controls;
using Imui.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using ZeepSDK.LevelEditor;
using ZeepSDK.Messaging;
using ZeepSDK.Racing;
using ZeepSDK.Storage;
using ZeepSDK.UI;

namespace EditorSpeedSplits
{
    [BepInPlugin("com.andme.editorspeedsplits", "EditorSpeedSplits", MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("ZeepSDK")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource logger;
        private Harmony harmony;

        public static Plugin Instance { get; private set; }

        internal static LEV_LevelEditorCentral central;

        internal static string fullLevelName = "";

        internal static readonly string defaultName = "LevelEditorSplitsModDefaultName";

        private EditorSplitsToolbarDrawer _toolbarDrawer;

        internal static EditorSplitsGUIManager guiManager;
        GameObject uiRoot;

        internal IModStorage personalBestSplitsStorage;

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
            RacingApi.PlayerSpawned += OnPlayerSpawned;

            _toolbarDrawer = new EditorSplitsToolbarDrawer();
            UIApi.AddToolbarDrawer(_toolbarDrawer);

            personalBestSplitsStorage = StorageApi.CreateModStorage(this);

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

        private void OnEnteredLevelEditor()
        {
            central = FindObjectOfType<LEV_LevelEditorCentral>();
            if (central == null)
            {
                logger.LogWarning("Level Editor Central not found.");
                return;
            }

            if (string.IsNullOrEmpty(fullLevelName))
            {
                fullLevelName = defaultName;
                ReplayManager.Instance.Replays.Remove(fullLevelName);
                SplitRecorder.DeleteBestSplits(fullLevelName);

            }

            SetupEditorUI();
        }

        private void OnPlayerSpawned()
        {
            if (!LevelEditorApi.IsTestingLevel)
                return;

            SplitRecorder.Clear();
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
                MessengerApi.LogWarning("[EditorSpeedSplits] No level loaded to reset splits for");
                return;
            }

            ReplayManager.Instance.Replays.Remove(fullLevelName);

            GameMaster gameMaster = FindObjectOfType<GameMaster>();

            gameMaster?.SetupPersonalBestAndMedals(0f, []);

            SplitRecorder.DeleteBestSplits(fullLevelName);

            guiManager?.RefreshSplits();

            logger.LogInfo($"Splits reset for level {fullLevelName}");
            MessengerApi.Log("[EditorSpeedSplits] Splits Reset");
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

        internal static ReplayManager.ReplayInfo GetReplaySplits()
        {
            ReplayManager.ReplayInfo replay = null;

            if (!string.IsNullOrEmpty(fullLevelName))
            {
                replay = ReplayManager.Instance.GetReplay(fullLevelName);
            }

            return replay;
        }

        internal static string MakeLevelIdentifier(string levelPath)
        {
            // Nombre corto y legible
            string shortName = Path.GetFileNameWithoutExtension(levelPath);

            // Hash estable del path completo
            string hash = ComputeHash(levelPath);

            return $"{shortName}_{hash}";
        }


        private static string ComputeHash(string input)
        {
            using (var sha1 = SHA1.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = sha1.ComputeHash(bytes);

                // 8 chars es suficiente y corto
                return BitConverter.ToString(hashBytes)
                    .Replace("-", "")
                    .Substring(0, 8)
                    .ToLowerInvariant();
            }
        }


        private void OnDestroy()
        {
            UIApi.RemoveToolbarDrawer(_toolbarDrawer);
            LevelEditorApi.EnteredLevelEditor -= OnEnteredLevelEditor;
            LevelEditorApi.ExitedLevelEditor -= OnExitedLevelEditor;
            RacingApi.PlayerSpawned -= OnPlayerSpawned;
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

            ReplayManager.ReplayInfo replay = Plugin.GetReplaySplits();

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

            if (ReplayManager.Instance.Replays.TryGetValue(currentFullLevelName, out ReplayManager.ReplayInfo replayInfo))
            {
                if (result.time > replayInfo.Time)
                {
                    return;
                }
            }
            SplitRecorder.SaveBestSplits(currentFullLevelName, result.time);

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

            Plugin.fullLevelName = Plugin.MakeLevelIdentifier(Path.ChangeExtension(filePath,null));

            Plugin.guiManager?.RefreshSplits();
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

            string newFullLevelName = Plugin.MakeLevelIdentifier(Path.Combine(__instance.GetFolderWeJustSavedInto().FullName, __instance.fileName.text));

            var currentReplay = Plugin.GetReplaySplits();

            if (currentReplay != null)
            {
                ReplayManager.Instance.AddReplay(newFullLevelName, currentReplay.Time, WinCompare.CreateSplitTimeList(currentReplay?.Splits, currentReplay?.velocities));
                SplitRecorder.SaveBestSplits(newFullLevelName, currentReplay.Time);
            }

            Plugin.fullLevelName = newFullLevelName;
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

    // PATCH: ReadyToReset.HeyYouHitATrigger
    [HarmonyPatch(typeof(ReadyToReset), "HeyYouHitATrigger")]
    class ReadyToReset_HeyYouHitATrigger_Patch
    {
        [HarmonyPostfix]
        static void Postfix(
            ReadyToReset __instance,
            bool isFinish, 
            Vector3 planePosition, 
            Vector3 planeOrientation
            )
        {
            if (!LevelEditorApi.IsTestingLevel)
                return;

            // Get recorded time from player results
            float timeOffset = __instance.master.playerResults[__instance.index].split_times[^1].time;
            float velocity = __instance.master.playerResults[__instance.index].split_times[^1].velocity;

            EditorSplit split = new EditorSplit
            {
                time = timeOffset,
                velocity = velocity,

                planePosition = planePosition,
                planeOrientation = planeOrientation,
            };

            if (!isFinish)
            {
                SplitRecorder.Add(split);
                Plugin.logger.LogInfo($"Recorded split {split.index} at time {split.time} with velocity {split.velocity} km/h");
            }
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

    internal class EditorSplit
    {   
        public int index;

        public float time;
        public float velocity;

        public Vector3 planePosition;
        public Vector3 planeOrientation;
    }

    internal class  LevelSplits
    {
        public string levelName;

        public float totalTime;

        public List<EditorSplit> splits;
    }

    internal static class SplitRecorder
    {
        public static readonly List<EditorSplit> Splits = new();

        public static void Clear()
        {
            Splits.Clear();
        }

        public static void Add(EditorSplit split)
        {
            split.index = Splits.Count + 1;
            Splits.Add(split);
        }

        public static void SaveBestSplits(string levelName, float bestTime)
        {
            LevelSplits levelSplits = new LevelSplits
            {
                levelName = levelName,
                totalTime = bestTime,
                splits = new List<EditorSplit>(Splits)
            };

            // FileName
            // Transform path to identifier
            string identifier = levelName.Replace(Path.DirectorySeparatorChar, '_').Replace(Path.AltDirectorySeparatorChar, '_');

            Plugin.Instance.personalBestSplitsStorage.SaveToJson(identifier, levelSplits);

            Plugin.logger.LogInfo($"Saved best splits for level {levelName} to storage.");
        }

        public static LevelSplits LoadBestSplits(string levelName)
        {
            // Transform path to identifier
            string identifier = levelName.Replace(Path.DirectorySeparatorChar, '_').Replace(Path.AltDirectorySeparatorChar, '_');

            if (!Plugin.Instance.personalBestSplitsStorage.JsonFileExists(identifier))
                return null;
            
            Plugin.logger.LogInfo($"Loading best splits for level {levelName} from storage.");

            return Plugin.Instance.personalBestSplitsStorage.LoadFromJson<LevelSplits>(identifier);
        }

        public static void DeleteBestSplits(string levelName)
        {
            // Transform path to identifier
            string identifier = levelName.Replace(Path.DirectorySeparatorChar, '_').Replace(Path.AltDirectorySeparatorChar, '_');
            if (Plugin.Instance.personalBestSplitsStorage.JsonFileExists(identifier))
            {
                Plugin.Instance.personalBestSplitsStorage.DeleteJsonFile(identifier);
                Plugin.logger.LogInfo($"Deleted best splits for level {levelName} from storage.");
            }
        }
    }


}
