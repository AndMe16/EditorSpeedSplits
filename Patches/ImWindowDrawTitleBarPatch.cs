using System;
using EditorSpeedSplits.GUIManager;
using HarmonyLib;
using Imui.Controls;
using Imui.Core;
using JetBrains.Annotations;

namespace EditorSpeedSplits.Patches;

[HarmonyPatch(typeof(ImWindow), nameof(ImWindow.DrawTitleBar))]
internal class ImWindowDrawTitleBarPatch
{
    [HarmonyPrefix]
    [UsedImplicitly]
    private static bool Prefix(ImGui gui, ImRect rect, ReadOnlySpan<char> text)
    {
        if (text is not "com.andme.editorspeedsplits_Splits" || !Plugin.Instance.GUIDrawer.IsDrawingSplitsButtons)
            return true; // Run the original DrawTitleBar for other windows
        EditorSplitsGUIDrawer.MyDrawTitleBar(gui, rect);

        // Skip the original DrawTitleBar
        return false;
    }
}