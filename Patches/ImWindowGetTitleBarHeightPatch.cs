using HarmonyLib;
using Imui.Controls;
using Imui.Core;


namespace EditorSpeedSplits.Patches
{
    [HarmonyPatch(typeof(ImWindow), nameof(ImWindow.GetTitleBarHeight))]
    internal class ImWindowGetTitleBarHeightPatch
    {
        [HarmonyPostfix]
        static void Postfix(ImGui gui, ref float __result)
        {
            if (Plugin.Instance._guiDrawer.isDrawingSplitsButtons)
            {
                __result = gui.GetRowHeight() * 0.5f;
            }
        }
    }
}
