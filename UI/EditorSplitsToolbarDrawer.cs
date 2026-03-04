using Imui.Controls;
using Imui.Core;
using ZeepSDK.UI;

namespace EditorSpeedSplits.UI
{
    public class EditorSplitsToolbarDrawer : IZeepToolbarDrawer
    {
        public string MenuTitle => "EditorSplits";

        public void DrawMenuItems(ImGui gui)
        {
            if (gui.Menu("Reset Splits"))
            {
                Plugin.ResetSplitsForCurrentLevel(true);
            }
        }
    }
}
