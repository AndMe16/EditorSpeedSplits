using HarmonyLib;
using Imui.Controls;
using Imui.Core;
using UnityEngine;


namespace EditorSpeedSplits.Patches
{
    [HarmonyPatch(typeof(ImWindow), nameof(ImWindow.Background))]
    internal class ImWindowBackgroundPatch
    {
        [HarmonyPostfix]
        static void Postfix(ImGui gui, ImWindowState state)
        {
            if (Plugin.Instance._guiDrawer.isDrawingSplitsButtons)
            {

                var color = new Color(171f / 255f, 85f / 255f, 85f / 255f); // rgb(171, 85, 85)

                gui.Canvas.Rect(state.Rect, color, gui.Style.Window.Box.BorderRadius);
            }  
        }
    }
}
