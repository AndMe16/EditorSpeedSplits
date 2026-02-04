using HarmonyLib;
using ZeepSDK.LevelEditor;

namespace EditorSpeedSplits.Patches
{
    [HarmonyPatch(typeof(LEV_ReturnToMainMenu), "ReturnToMainMenu")]
    internal class LevReturnToMainMenuPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            Plugin.fullLevelName = "";
            Plugin.logger.LogInfo("Cleared fullLevelName on return to main menu");
        }
    }
}
