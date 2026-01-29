using BepInEx;
using BepInEx.Logging;
using EditorSpeedSplits;
using HarmonyLib;
using System.Collections.Generic;

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

            logger.LogInfo("Plugin com.andme.editorspeedsplits is loaded!");
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

            Plugin.logger.LogInfo("ReloadBestTimes Prefix called");
            if (!__instance.GlobalLevel.IsTestLevel)
                return true;


            ReplayManager.ReplayInfo replay = ReplayManager.Instance.GetReplay(__instance.GlobalLevel.UID);
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

            ReplayManager.Instance.AddReplay(
                __instance.GlobalLevel.UID,
                result.time,
                result.split_times
            );
        }
    }



}
