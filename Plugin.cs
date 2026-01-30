using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EditorSpeedSplits;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using ZeepSDK.Level;

namespace EditorSpeedSplits
{
    [BepInPlugin("com.andme.editorspeedsplits", "EditorSpeedSplits", MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource logger;
        private Harmony harmony;

        public static Plugin Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            logger = Logger;

            harmony = new Harmony("com.andme.editorspeedsplits");
            harmony.PatchAll();

            ModConfig.Initialize(Config);

            logger.LogInfo("Plugin com.andme.editorspeedsplits is loaded!");
        }

        internal static void ResetSplitsForCurrentLevel()
        {
            LevelScriptableObject  currentLevel = LevelApi.CurrentLevel;
            if (currentLevel == null)
            {
                logger.LogWarning("No current level loaded.");
                return;
            }
            if (!currentLevel.IsTestLevel)
            {
                logger.LogWarning("Current level is not a test level.");
                return;
            }

            string currentHash = LevelApi.GetLevelHash(currentLevel);
            ReplayManager.Instance.Replays.Remove(currentHash);
            logger.LogInfo($"Splits reset for level {currentLevel.Path} {currentHash}.");
        }

        private void OnDestroy()
        {
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

            string currentHash = LevelApi.GetLevelHash(__instance.GlobalLevel);

            ReplayManager.ReplayInfo replay = ReplayManager.Instance.GetReplay(currentHash);

            Plugin.logger.LogInfo($"ReloadBestTimes called for level {__instance.GlobalLevel.Path} {currentHash}");
            if (replay == null)
            {
                __instance.SetupPersonalBestAndMedals(0f, new List<WinCompare.SplitTime>());
                Plugin.logger.LogInfo("No replay found, setting personal best to 0");
            }
            else
            {
                __instance.SetupPersonalBestAndMedals(replay.Time, WinCompare.CreateSplitTimeList(replay.Splits, replay.velocities));

                Plugin.logger.LogInfo($"Replay found, setting personal best to {replay.Time} {replay.Splits} {replay.velocities}");
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

            string currentHash = LevelApi.GetLevelHash(__instance.GlobalLevel);

            ReplayManager.Instance.AddReplay(
                currentHash,
                result.time,
                result.split_times
            );
        }
    }

    public class ModConfig : MonoBehaviour
    {
        public static ConfigEntry<bool> ResetSplits;


        // Constructor that takes a ConfigFile instance from the main class
        public static void Initialize(ConfigFile config)
        {
            ResetSplits = config.Bind("1. Gameplay", "1.1 Reset Splits", true,
                "[button] Reset the current level's splits");

            ResetSplits.SettingChanged += onResetSplits;
        }

        private static void onResetSplits(object sender, System.EventArgs e)
        {
            Plugin.ResetSplitsForCurrentLevel();
        }

        private void OnDestroy()
        {
            ResetSplits.SettingChanged -= onResetSplits;
        }

    }



}
