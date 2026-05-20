using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Imui.Controls;
using Imui.Core;
using JetBrains.Annotations;

namespace EditorSpeedSplits.Patches;

[HarmonyPatch(typeof(ImWindow), nameof(ImWindow.GetTitleBarHeight))]
internal class ImWindowGetTitleBarHeightPatch
{
    [HarmonyPostfix]
    [UsedImplicitly]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private static void Postfix(ImGui gui, ref float __result)
    {
        if (Plugin.Instance.GUIDrawer.IsDrawingSplitsButtons) __result = gui.GetRowHeight() * 0.5f;
    }
}