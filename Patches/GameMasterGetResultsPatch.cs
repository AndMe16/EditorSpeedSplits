using EditorSpeedSplits.Splits;
using HarmonyLib;
using ZeepSDK.Messaging;
using ZeepSDK.Racing;

namespace EditorSpeedSplits.Patches
{
    [HarmonyPatch(typeof(GameMaster), "GetResults2")]
    internal class GameMasterGetResultsPatch
    {
        [HarmonyPostfix]
        private static void Postfix(GameMaster __instance)
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

            if (!SplitRecorder.HasSplits(currentFullLevelName))
            {
                Plugin.ResetSplitsForCurrentLevel(false);
                Plugin.logger.LogInfo($"No splits file found for {currentFullLevelName}, initializing empty splits.");
            }

            if (ReplayManager.Instance.Replays.TryGetValue(
                currentFullLevelName,
                out ReplayManager.ReplayInfo replayInfo))
            {
                if (result.time > replayInfo.Time)
                    return;
            }

            SplitRecorder.SaveBestSplits(currentFullLevelName, result.time, SplitRecorder.Splits);

            ReplayManager.Instance.AddReplay(
                currentFullLevelName,
                result.time,
                result.split_times
            );

            MessengerApi.LogSuccess("[EditorSpeedSplits] New PB! Recording Splits");
        }
    }
}
