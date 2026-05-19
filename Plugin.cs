using BepInEx;
using BepInEx.Logging;
using EditorSpeedSplits.Configuration;
using EditorSpeedSplits.GUIManager;
using EditorSpeedSplits.Patches;
using EditorSpeedSplits.Splits;
using EditorSpeedSplits.UI;
using HarmonyLib;
using System.Collections.Generic;
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

        public EditorSplitsGUIDrawer _guiDrawer;

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

            _guiDrawer = new EditorSplitsGUIDrawer();
            UIApi.AddZeepGUIDrawer(_guiDrawer);

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

            ResetSplitsForCurrentLevel(true);
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
                SplitRecorder.DeleteBestSplits(fullLevelName);

            }

            SyncEditorUIWithSplitsAvailability();
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
            DestroyEditorUI();
        }


        internal static void ResetSplitsForCurrentLevel(bool ShowMessage)
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

            GameMaster gameMaster = FindObjectOfType<GameMaster>();

            gameMaster?.SetupPersonalBestAndMedals(0f, []);

            SplitRecorder.DeleteBestSplits(fullLevelName);

            Instance._guiDrawer.RefreshSplits();

            logger.LogInfo($"Splits reset for level {fullLevelName}");
            if (ShowMessage)
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

        internal static void SyncEditorUIWithSplitsAvailability()
        {
            if (string.IsNullOrEmpty(fullLevelName))
            {
                logger.LogWarning("No level loaded in the editor to sync UI for.");
                Instance?.DestroyEditorUI();
                return;
            }

            if (SplitRecorder.HasSplits(fullLevelName))
            {
                logger.LogInfo("Splits found for current level, showing editor UI.");
                Instance?.SetupEditorUI();
                return;
            }
            logger.LogInfo("No splits found for current level, hiding editor UI.");
            Instance?.DestroyEditorUI();
        }


        private void SetupEditorUI()
        {
            _guiDrawer._SplitsButtonOpen = true;
        }

        private void DestroyEditorUI()
        {
            _guiDrawer._SplitsButtonOpen = false;
            _guiDrawer._SplitsListOpen = false;
            _guiDrawer.isDrawingSplitsButtons = false;
            _guiDrawer.isDrawingSplitsList = false;
        }


        internal static ReplayManager.ReplayInfo GetReplaySplits()
        {
            ReplayManager.ReplayInfo replay = null;

            if (!string.IsNullOrEmpty(fullLevelName))
            {
                if (SplitRecorder.HasSplits(fullLevelName))
                {
                    var bestSplits = SplitRecorder.LoadBestSplits(fullLevelName);
                    List<float> splitTimes = [];
                    List<float> splitVelocities = [];
                    for (int i = 0; i < bestSplits.splits.Count; i++)
                    {
                        splitTimes.Add(bestSplits.splits[i].time);
                        splitVelocities.Add(bestSplits.splits[i].velocity);
                    }
                    replay = new ReplayManager.ReplayInfo
                    {
                        LevelUID = bestSplits.levelName,
                        Time = bestSplits.totalTime,
                        Splits = splitTimes,
                        velocities = splitVelocities
                    };

                    SplitRecorder.previousLevelSplits = bestSplits;
                }
                else
                {
                    SplitRecorder.previousLevelSplits = null;
                }
            }

            return replay;
        }

        private void OnDestroy()
        {
            UIApi.RemoveToolbarDrawer(_toolbarDrawer);
            UIApi.RemoveZeepGUIDrawer(_guiDrawer);
            LevelEditorApi.EnteredLevelEditor -= OnEnteredLevelEditor;
            LevelEditorApi.ExitedLevelEditor -= OnExitedLevelEditor;
            RacingApi.PlayerSpawned -= OnPlayerSpawned;
            harmony?.UnpatchSelf();
            harmony = null;
        }
    }
}
