using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using EditorSpeedSplits.Configuration;
using EditorSpeedSplits.Splits;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using ZeepSDK.Messaging;

namespace EditorSpeedSplits.Patches;

[HarmonyPatch(typeof(ReadyToReset), "HeyYouHitATrigger")]
internal class ReadyToResetHeyYouHitATriggerPatch
{
    public static readonly List<GameObject> Triggers = [];

    private static bool _shouldSaveSplits;

    [HarmonyPostfix]
    [UsedImplicitly]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
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
        if (!Plugin.Instance.ShouldRecordSplits)
            return;

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

        if (Triggers.Contains(theTrigger))
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

        Triggers.Add(theTrigger);

        var col = theTrigger.GetComponent<Collider>();
        var bounds = col == null ? new Bounds(theTrigger.transform.position, Vector3.one) : col.bounds;

        EditorSplit split = new()
        {
            index = isFinish ? 0 : __instance.master.playerResults[__instance.index].split_times.Count,

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
                Plugin.logger.LogInfo("Finished level but did not get medal, not saving splits.");
                return;
            }

            if (result.time <= 0f)
                return;

            // If there are no previous splits, we want to save the current splits as the new best splits
            if (SplitRecorder.PreviousLevelSplits == null)
            {
                Plugin.logger.LogInfo("No splits file found Recording Splits");
            }

            // If we already have completed splits and the new time is not better
            // We save if the # CPs is different
            else if (SplitRecorder.PreviousLevelSplits != null &&
                     SplitRecorder.PreviousLevelSplits?.completed == true &&
                     SplitRecorder.PreviousLevelSplits?.totalTime <= result.time &&
                     SplitRecorder.PreviousLevelSplits?.GotCPs == result.racepoints)
            {
                Plugin.logger.LogInfo(
                    $"Finished level but did not beat previous best time {SplitRecorder.PreviousLevelSplits?.totalTime} vs {result.time} or got less CPs {SplitRecorder.PreviousLevelSplits?.GotCPs} vs {result.racepoints}, not saving splits.");
                return;
            }

            _shouldSaveSplits = true;
            Plugin.logger.LogInfo("Finished level with a new PB time! Recording Splits");

            MessengerApi.LogSuccess("[EditorSpeedSplits] New PB! Recording Splits");
        }
        else
        {
            Plugin.logger.LogInfo(
                $"Checkpoint passed. CP{result.racepoints}/{__instance.master.racePoints}");

            if (result.split_times[^1].time <= 0f)
                return;

            // If there are no previous splits, we want to save the current splits as the new best splits
            if (SplitRecorder.PreviousLevelSplits == null)
            {
                Plugin.logger.LogInfo("No splits file found Recording Splits");
            }

            else if (!(!SplitRecorder.PreviousLevelSplits.completed
                       && (result.racepoints > SplitRecorder.PreviousLevelSplits.GotCPs
                           || (result.racepoints == SplitRecorder.PreviousLevelSplits.GotCPs && !ModConfig.CpPBSpeed.Value
                               && result.split_times[^1].time < SplitRecorder.PreviousLevelSplits.splits[^1].time)
                           || (result.racepoints == SplitRecorder.PreviousLevelSplits.GotCPs && ModConfig.CpPBSpeed.Value
                               && result.split_times[^1].velocity > SplitRecorder.PreviousLevelSplits.splits[^1].velocity)
                           )
                     )
                    )
            {
                Plugin.logger.LogInfo(
                    $"Passed CP{result.racepoints} but did not beat previous CP time or got less CPs, not saving splits.");
                return;
            }

            _shouldSaveSplits = true;
            MessengerApi.Log("[EditorSpeedSplits] New CP PB");
        }

        if (_shouldSaveSplits) SetNewPb(Plugin.FullLevelName, result, isFinish);
    }

    private static void SetNewPb(string levelName, WinCompare.Result result, bool completed)
    {
        SplitRecorder.SaveBestSplits(levelName, completed ? result.time : result.split_times[^1].time,
            SplitRecorder.Splits, completed);
    }
}