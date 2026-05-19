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

        private static bool shouldSaveSplits = false;

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


            if (isFinish)
            {
                if (!__instance.master.currentLevelMode.DidWeGetMedal(
                    LevelModeBase.MedalType.Finished, result))
                {
                    Plugin.logger.LogInfo($"Finished level but did not get medal, not saving splits.");
                    return;
                }
                
                if (result.time <= 0f)
                    return;

                // If there are no previous splits, we want to save the current splits as the new best splits
                if (SplitRecorder.previousLevelSplits == null)
                {
                    Plugin.logger.LogInfo($"No splits file found Recording Splits");
                }

                // If we already have completed splits and the new time is not better, or we got less CPs, we don't save the splits
                else if (SplitRecorder.previousLevelSplits != null && SplitRecorder.previousLevelSplits?.completed == true && SplitRecorder.previousLevelSplits?.totalTime <= result.time && SplitRecorder.previousLevelSplits?.gotCPs >= result.racepoints)
                {
                    Plugin.logger.LogInfo($"Finished level but did not beat previous best time {SplitRecorder.previousLevelSplits?.totalTime} vs {result.time} or got less CPs {SplitRecorder.previousLevelSplits?.gotCPs} vs {result.racepoints}, not saving splits.");
                    return;
                }

                shouldSaveSplits = true;
                Plugin.logger.LogInfo($"Finished level with a new PB time! Recording Splits");

                MessengerApi.LogSuccess("[EditorSpeedSplits] New PB! Recording Splits");

            }
            else
            {
                Plugin.logger.LogInfo(
                    $"Checkpoint passed. CP{result.racepoints}/{__instance.master.racePoints}");

                if (result.split_times[^1].time <= 0f)
                    return;

                // If there are no previous splits, we want to save the current splits as the new best splits
                if (SplitRecorder.previousLevelSplits == null)
                {
                    Plugin.logger.LogInfo($"No splits file found Recording Splits");
                }

                else if (!(!SplitRecorder.previousLevelSplits.completed
                    && (result.racepoints > SplitRecorder.previousLevelSplits.gotCPs 
                        || (result.racepoints == SplitRecorder.previousLevelSplits.gotCPs 
                            && result.split_times[^1].time < SplitRecorder.previousLevelSplits.splits[^1].time
                            ))
                        )
                    )
                {
                    Plugin.logger.LogInfo($"Passed CP{result.racepoints} but did not beat previous CP time or got less CPs, not saving splits.");
                    return;
                }
                shouldSaveSplits = true;
                MessengerApi.Log("[EditorSpeedSplits] New CP PB");
            }
            if (shouldSaveSplits)
            {
                SetNewPB(Plugin.fullLevelName, result, isFinish, __instance.master.racePoints);
            }

        }

        private static void SetNewPB(string levelName, WinCompare.Result result, bool completed, int totalCPs)
        {
            SplitRecorder.SaveBestSplits(levelName, completed?result.time:result.split_times[^1].time, SplitRecorder.Splits, completed, result.racepoints, totalCPs);
        }
    }
}
