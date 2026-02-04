using EditorSpeedSplits.Splits;
using EditorSpeedSplits.Utilities;
using HarmonyLib;
using System.IO;
using ZeepSDK.LevelEditor;
using ZeepSDK.Racing;

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

            var currentReplay = Plugin.GetReplaySplits();

            if (currentReplay != null)
            {
                ReplayManager.Instance.AddReplay(
                    newFullLevelName,
                    currentReplay.Time,
                    WinCompare.CreateSplitTimeList(
                        currentReplay?.Splits,
                        currentReplay?.velocities));
                SplitRecorder.SaveBestSplits(newFullLevelName, currentReplay.Time);
            }

            Plugin.fullLevelName = newFullLevelName;
        }
    }
}
