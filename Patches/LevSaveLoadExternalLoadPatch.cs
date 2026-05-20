using System.IO;
using EditorSpeedSplits.Utilities;
using HarmonyLib;
using JetBrains.Annotations;
using ZeepSDK.LevelEditor;

namespace EditorSpeedSplits.Patches;

[HarmonyPatch(typeof(LEV_SaveLoad), "ExternalLoad")]
internal class LevSaveLoadExternalLoadPatch
{
    [HarmonyPostfix]
    [UsedImplicitly]
    private static void Postfix(string filePath, bool isTestLevel)
    {
        if (!LevelEditorApi.IsInLevelEditor)
            return;

        if (isTestLevel)
            return;

        Plugin.FullLevelName = LevelIdentifier.MakeLevelIdentifier(
            Path.ChangeExtension(filePath, null));

        Plugin.SyncEditorUIWithSplitsAvailability();

        Plugin.Instance.GUIDrawer?.RefreshSplits();
    }
}