using EditorSpeedSplits.Splits;
using HarmonyLib;
using UnityEngine;
using ZeepSDK.LevelEditor;
using System.Collections.Generic;

namespace EditorSpeedSplits.Patches
{
    [HarmonyPatch(typeof(ReadyToReset), "HeyYouHitATrigger")]
    internal class ReadyToResetHeyYouHitATriggerPatch
    {
        public static List<GameObject> triggers = [];

        [HarmonyPostfix]
        private static void Postfix(
            ReadyToReset __instance,
            ref GameObject theTrigger,
            ref bool isFinish,
            ref Vector3 planePosition,
            ref Vector3 planeOrientation,
            ref float velocityKMH,
            ref Vector3 zeepkistPosition,
            ref Vector3 zeepkistOrientation,
            ref Vector3 pointOnPlane
            )
        {
            if (!LevelEditorApi.IsTestingLevel)
                return;

            if (!__instance.master.countFinishCrossing)
                return;

            if (theTrigger == null)
                return;

            if (triggers.Contains(theTrigger))
                return;

            if (__instance.master.playerResults == null || __instance.master.playerResults.Count <= __instance.index)
                return;

            float timeOffset;
            float velocity;

            if (isFinish)
            {
                timeOffset = __instance.master.playerResults[__instance.index].time;
                velocity = velocityKMH;
            }
            else
            {
                if (__instance.master.playerResults[__instance.index].split_times == null || __instance.master.playerResults[__instance.index].split_times.Count == 0)
                    return;
                timeOffset = __instance.master.playerResults[__instance.index].split_times[^1].time;
                velocity = __instance.master.playerResults[__instance.index].split_times[^1].velocity;
            }

            triggers.Add(theTrigger);

            Bounds bounds;

            Collider col = theTrigger.GetComponent<Collider>();
            if (col == null)
                bounds = new Bounds(theTrigger.transform.position, Vector3.zero);
            else
                bounds = col.bounds;

            EditorSplit split = new()
            {
                index = isFinish?0:__instance.master.playerResults[__instance.index].split_times.Count,

                isFinish = isFinish,

                time = timeOffset,
                velocity = velocity,

                planePosition = planePosition,
                planeOrientation = planeOrientation,

                zeepkistPosition = zeepkistPosition,
                zeepkistOrientation = zeepkistOrientation,

                pointOnPlane = pointOnPlane,

                bounds = bounds

            };

            SplitRecorder.Add(split);
            Plugin.logger.LogInfo(
                $"Recorded split {split.index} at time {split.time} with velocity {split.velocity} km/h isFinish: {split.isFinish}");
        }
    }
}
