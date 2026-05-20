using System.Diagnostics.CodeAnalysis;
using System.IO;
using EditorSpeedSplits.Splits;
using EditorSpeedSplits.Utilities;
using HarmonyLib;
using JetBrains.Annotations;

namespace EditorSpeedSplits.Patches;

[HarmonyPatch(typeof(LEV_SaveLoad), "ExternalSaveFile")]
internal class LevSaveLoadExternalSaveFilePatch
{
    [HarmonyPostfix]
    [UsedImplicitly]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private static void Postfix(LEV_SaveLoad __instance, bool isTestMap)
    {
        if (isTestMap)
            return;

        var newFullLevelName = LevelIdentifier.MakeLevelIdentifier(
            Path.Combine(
                __instance.GetFolderWeJustSavedInto().FullName,
                __instance.fileName.text));

        var bestSplits = SplitRecorder.LoadBestSplits(Plugin.FullLevelName);

        if (bestSplits != null)
            SplitRecorder.SaveBestSplits(newFullLevelName, bestSplits.totalTime, bestSplits.splits,
                bestSplits.completed);

        Plugin.FullLevelName = newFullLevelName;
    }
}