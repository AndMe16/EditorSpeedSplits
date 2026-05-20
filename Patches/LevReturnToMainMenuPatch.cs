using HarmonyLib;
using JetBrains.Annotations;

namespace EditorSpeedSplits.Patches;

[HarmonyPatch(typeof(LEV_ReturnToMainMenu), "ReturnToMainMenu")]
internal class LevReturnToMainMenuPatch
{
    [HarmonyPostfix]
    [UsedImplicitly]
    private static void Postfix()
    {
        Plugin.FullLevelName = "";
        Plugin.logger.LogInfo("Cleared fullLevelName on return to main menu");
    }
}