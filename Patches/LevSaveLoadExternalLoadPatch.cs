using EditorSpeedSplits.Splits;
using EditorSpeedSplits.Utilities;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using ZeepSDK.LevelEditor;

namespace EditorSpeedSplits.Patches
{
    [HarmonyPatch(typeof(LEV_SaveLoad), "ExternalLoad")]
    internal class LevSaveLoadExternalLoadPatch
    {
        [HarmonyPostfix]
        private static void Postfix(string filePath, bool isTestLevel)
        {
            if (!LevelEditorApi.IsInLevelEditor)
                return;

            if (isTestLevel)
                return;

            Plugin.fullLevelName = LevelIdentifier.MakeLevelIdentifier(
                Path.ChangeExtension(filePath, null));

            Plugin.SyncEditorUIWithSplitsAvailability();

            Plugin.Instance._guiDrawer?.RefreshSplits();
        }
    }
}
