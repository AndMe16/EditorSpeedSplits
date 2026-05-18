using EditorSpeedSplits.Splits;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using ZeepSDK.LevelEditor;
using ZeepSDK.Messaging;

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
            if (!__instance.GlobalLevel.IsTestLevel)
                return;

            if (__instance.manager.amountOfPlayers != 1)
                return;

            if (__instance.index != 0)
                return;

            if (!__instance.master.countFinishCrossing)
                return;

            if (theTrigger == null)
                return;

            if (triggers.Contains(theTrigger))
                return;

            if (__instance.master.playerResults == null || __instance.master.playerResults.Count <= __instance.index)
                return;

            var result = __instance.master.playerResults[0];

            if (result == null)
                return;

            float timeOffset;
            float velocity;

            if (isFinish)
            {
                timeOffset = result.time;
                velocity = velocityKMH;
            }
            else
            {
                if (result.split_times == null || result.split_times.Count == 0)
                    return;
                timeOffset = result.split_times[^1].time;
                velocity = result.split_times[^1].velocity;
            }

            triggers.Add(theTrigger);

            Bounds bounds;

            Collider col = theTrigger.GetComponent<Collider>();
            if (col == null)
                bounds = new Bounds(theTrigger.transform.position, Vector3.one);
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

            if ( isFinish )
            {
                if (__instance.master.currentLevelMode.DidWeGetMedal(
                    LevelModeBase.MedalType.Finished, result))
                {

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
    }
}
