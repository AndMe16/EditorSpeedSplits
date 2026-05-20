using System.Collections.Generic;
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

namespace EditorSpeedSplits;

[BepInPlugin("com.andme.editorspeedsplits", "EditorSpeedSplits", MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("ZeepSDK")]
public class Plugin : BaseUnityPlugin
{
    private const string DefaultName = "LevelEditorSplitsModDefaultName";

    // ReSharper disable once InconsistentNaming
    internal static ManualLogSource logger;

    internal static LEV_LevelEditorCentral Central;

    internal static string FullLevelName = "";
    private Harmony _harmony;

    private EditorSplitsToolbarDrawer _toolbarDrawer;

    public EditorSplitsGUIDrawer GUIDrawer;

    internal IModStorage PersonalBestSplitsStorage;

    public static Plugin Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        logger = Logger;

        _harmony = new Harmony("com.andme.editorspeedsplits");
        _harmony.PatchAll();

        ModConfig.Initialize(Config);

        logger.LogInfo("Plugin com.andme.editorspeedsplits is loaded!");

        LevelEditorApi.EnteredLevelEditor += OnEnteredLevelEditor;
        LevelEditorApi.ExitedLevelEditor += OnExitedLevelEditor;
        RacingApi.PlayerSpawned += OnPlayerSpawned;

        PersonalBestSplitsStorage = StorageApi.CreateModStorage(this);

        _toolbarDrawer = new EditorSplitsToolbarDrawer();
        UIApi.AddToolbarDrawer(_toolbarDrawer);

        GUIDrawer = new EditorSplitsGUIDrawer();
        UIApi.AddZeepGUIDrawer(GUIDrawer);

        GUIDrawer.LoadWindowsRects();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(ModConfig.ResetSplitsKey.Value))
            return;

        // Input globally locked / paused
        if (Time.timeScale == 0f)
            return;

        // Player typing in an input field
        // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
        if (IsTypingInInputField())
            return;

        // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
        ResetSplitsForCurrentLevel(true);
    }

    private void OnDestroy()
    {
        Instance.GUIDrawer.SaveWindowsRects();

        UIApi.RemoveToolbarDrawer(_toolbarDrawer);
        UIApi.RemoveZeepGUIDrawer(GUIDrawer);
        LevelEditorApi.EnteredLevelEditor -= OnEnteredLevelEditor;
        LevelEditorApi.ExitedLevelEditor -= OnExitedLevelEditor;
        RacingApi.PlayerSpawned -= OnPlayerSpawned;
        _harmony?.UnpatchSelf();
        _harmony = null;
    }

    private static void OnEnteredLevelEditor()
    {
        Central = FindObjectOfType<LEV_LevelEditorCentral>();
        if (Central == null)
        {
            logger.LogWarning("Level Editor Central not found.");
            return;
        }

        if (string.IsNullOrEmpty(FullLevelName))
        {
            FullLevelName = DefaultName;
            SplitRecorder.DeleteBestSplits(FullLevelName);
        }

        SyncEditorUIWithSplitsAvailability();
    }

    private static void OnPlayerSpawned()
    {
        if (!LevelEditorApi.IsTestingLevel)
            return;

        SplitRecorder.Clear();
        ReadyToResetHeyYouHitATriggerPatch.Triggers.Clear();
    }

    private void OnExitedLevelEditor()
    {
        Central = null;
        DestroyEditorUI();
    }


    internal static void ResetSplitsForCurrentLevel(bool showMessage)
    {
        if (!LevelEditorApi.IsTestingLevel && !LevelEditorApi.IsInLevelEditor)
            return;

        if (LevelEditorApi.IsInLevelEditor && Central)
            if (Central.input.inputLocked)
                return;

        var currentFullLevelName = FullLevelName;

        if (string.IsNullOrEmpty(currentFullLevelName))
        {
            logger.LogWarning("No level loaded in the editor to reset splits for.");
            MessengerApi.LogWarning("[EditorSpeedSplits] No level loaded to reset splits for");
            return;
        }

        // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
        var gameMaster = FindObjectOfType<GameMaster>();

        gameMaster?.SetupPersonalBestAndMedals(0f, []);

        SplitRecorder.DeleteBestSplits(FullLevelName);

        Instance.GUIDrawer.RefreshSplits();

        logger.LogInfo($"Splits reset for level {FullLevelName}");
        if (showMessage)
            MessengerApi.Log("[EditorSpeedSplits] Splits Reset");
    }

    private static bool IsTypingInInputField()
    {
        if (!EventSystem.current)
            return false;

        var selected = EventSystem.current.currentSelectedGameObject;
        return !selected
            ? false
            :
            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            selected.GetComponent<TMP_InputField>();
    }

    internal static void SyncEditorUIWithSplitsAvailability()
    {
        if (string.IsNullOrEmpty(FullLevelName))
        {
            logger.LogWarning("No level loaded in the editor to sync UI for.");
            Instance?.DestroyEditorUI();
            return;
        }

        if (SplitRecorder.HasSplits(FullLevelName))
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
        GUIDrawer.SplitsButtonOpen = true;
    }

    private void DestroyEditorUI()
    {
        GUIDrawer.SplitsButtonOpen = false;
        GUIDrawer.SplitsListOpen = false;
        GUIDrawer.IsDrawingSplitsButtons = false;
    }


    internal static ReplayManager.ReplayInfo GetReplaySplits()
    {
        ReplayManager.ReplayInfo replay = null;

        if (string.IsNullOrEmpty(FullLevelName)) return replay;
        if (SplitRecorder.HasSplits(FullLevelName))
        {
            var bestSplits = SplitRecorder.LoadBestSplits(FullLevelName);
            List<float> splitTimes = [];
            List<float> splitVelocities = [];
            foreach (var t in bestSplits.splits)
            {
                splitTimes.Add(t.time);
                splitVelocities.Add(t.velocity);
            }

            replay = new ReplayManager.ReplayInfo
            {
                LevelUID = bestSplits.levelName,
                Time = bestSplits.totalTime,
                Splits = splitTimes,
                velocities = splitVelocities
            };

            SplitRecorder.PreviousLevelSplits = bestSplits;
        }
        else
        {
            SplitRecorder.PreviousLevelSplits = null;
        }

        return replay;
    }
}