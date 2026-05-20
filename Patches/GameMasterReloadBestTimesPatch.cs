using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using JetBrains.Annotations;

namespace EditorSpeedSplits.Patches;

[HarmonyPatch(typeof(GameMaster), "ReloadBestTimes")]
internal class GameMasterReloadBestTimesPatch
{
    [HarmonyPrefix]
    [UsedImplicitly]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private static bool Prefix(GameMaster __instance)
    {
        if (!__instance.GlobalLevel.IsTestLevel)
            return true;

        var replay = Plugin.GetReplaySplits();


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