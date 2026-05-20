using HarmonyLib;
using Imui.Controls;
using Imui.Core;
using JetBrains.Annotations;
using UnityEngine;

namespace EditorSpeedSplits.Patches;

[HarmonyPatch(typeof(ImWindow), nameof(ImWindow.Background))]
internal class ImWindowBackgroundPatch
{
    [HarmonyPostfix]
    [UsedImplicitly]
    private static void Postfix(ImGui gui, ImWindowState state)
    {
        if (!Plugin.Instance.GUIDrawer.IsDrawingSplitsButtons) return;
        var color = new Color(171f / 255f, 85f / 255f, 85f / 255f); // rgb(171, 85, 85)

#pragma warning disable Harmony003
        gui.Canvas.Rect(state.Rect, color, gui.Style.Window.Box.BorderRadius);
#pragma warning restore Harmony003
    }
}