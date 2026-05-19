using EditorSpeedSplits.Splits;
using EditorSpeedSplits.Utilities;
using HarmonyLib;
using System.IO;

namespace EditorSpeedSplits.Patches
{
    [HarmonyPatch(typeof(LEV_SaveLoad), "ExternalSaveFile")]
    internal class LevSaveLoadExternalSaveFilePatch
    {
        [HarmonyPostfix]
        private static void Postfix(LEV_SaveLoad __instance, bool isTestMap)
        {
            if (isTestMap)
                return;

            string newFullLevelName = LevelIdentifier.MakeLevelIdentifier(
                Path.Combine(
                    __instance.GetFolderWeJustSavedInto().FullName,
                    __instance.fileName.text));

            var bestSplits = SplitRecorder.LoadBestSplits(Plugin.fullLevelName);

            if (bestSplits != null)
                SplitRecorder.SaveBestSplits(newFullLevelName, bestSplits.totalTime, bestSplits.splits, bestSplits.completed, bestSplits.gotCPs, bestSplits.totalCPs);

            Plugin.fullLevelName = newFullLevelName;
        }
    }
}
