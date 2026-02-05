using EditorSpeedSplits.Splits;
using HarmonyLib;
using UnityEngine;
using ZeepSDK.LevelEditor;

namespace EditorSpeedSplits.Patches
{
    [HarmonyPatch(typeof(ReadyToReset), "HeyYouHitATrigger")]
    internal class ReadyToResetHeyYouHitATriggerPatch
    {
        [HarmonyPostfix]
        private static void Postfix(
            ReadyToReset __instance,
            bool isFinish,
            Vector3 planePosition,
            Vector3 planeOrientation)
        {
            if (!LevelEditorApi.IsTestingLevel)
                return;

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
                Plugin.logger.LogInfo(
                    $"Recorded split {split.index} at time {split.time} with velocity {split.velocity} km/h");
            }
        }
    }
}
