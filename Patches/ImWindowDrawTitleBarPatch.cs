using EditorSpeedSplits.GUIManager;
using HarmonyLib;
using Imui.Controls;
using Imui.Core;
using System;
using UnityEngine;

namespace EditorSpeedSplits.Patches
{
    [HarmonyPatch(typeof(ImWindow), nameof(ImWindow.DrawTitleBar))]
    internal class ImWindowDrawTitleBarPatch
    {
        [HarmonyPrefix]
        static bool Prefix(ImGui gui, ImRect rect, ReadOnlySpan<char> text)
        {
            if (!(text == "com.andme.editorspeedsplits_Splits") || !Plugin.Instance._guiDrawer.isDrawingSplitsButtons)
            {
                return true; // Run original DrawTitleBar for other windows
            }

            EditorSplitsGUIDrawer.MyDrawTitleBar(gui, rect, text);

            // Skip original DrawTitleBar
            return false;
        }
    }
}
