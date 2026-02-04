using HarmonyLib;
using ZeepSDK.Racing;

namespace EditorSpeedSplits.Patches
{
    [HarmonyPatch(typeof(GameMaster), "ReloadBestTimes")]
    internal class GameMasterReloadBestTimesPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(GameMaster __instance)
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
                __instance.SetupPersonalBestAndMedals(
                    replay.Time,
                    WinCompare.CreateSplitTimeList(replay.Splits, replay.velocities));

                Plugin.logger.LogInfo($"Replay found, setting personal best to {replay.Time}");
            }

            return false;
        }
    }
}
