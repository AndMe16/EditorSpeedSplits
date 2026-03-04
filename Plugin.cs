using BepInEx;
using BepInEx.Logging;
using EditorSpeedSplits.Configuration;
using EditorSpeedSplits.GUIManager;
using EditorSpeedSplits.Patches;
using EditorSpeedSplits.Splits;
using EditorSpeedSplits.UI;
using HarmonyLib;
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

            if (!SplitRecorder.HasSplits(fullLevelName))
                return;

            SetupEditorUI();
        }

        private void OnPlayerSpawned()
        {
            if (!LevelEditorApi.IsTestingLevel)
                return;

            SplitRecorder.Clear();
            ReadyToResetHeyYouHitATriggerPatch.triggers.Clear();
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
}
