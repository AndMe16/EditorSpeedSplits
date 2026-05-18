using EditorSpeedSplits.Splits;
using Imui.Controls;
using Imui.Core;
using Imui.Style;
using System;
using System.Collections.Generic;
using UnityEngine;
using ZeepSDK.Messaging;
using ZeepSDK.UI;

namespace EditorSpeedSplits.GUIManager
{
    public class EditorSplitsGUIDrawer : IZeepGUIDrawer
    {
        public bool _SplitsButtonOpen = false;

        public bool _SplitsListOpen = false;

        public bool isDrawingSplitsButtons = false;

        public bool isDrawingSplitsList = false;

        public bool isHoveringSplitsButtons = false;

        public bool isHoveringSplitsList = false;

        private bool wasHoveringAnyWindowLastFrame = false;

        List<EditorSplit> splits;
        int selectedIndex = -1;

        // Constants
        const float SplitsButtonWidthPercent = 0.1f;
        const float SplitsButtonHeightRows = 2f;
        const int splitWidth = 8;
        const int timeWidth = 12;
        const int velocityWidth = 8;

        public void OnZeepGUI(ImGui gui)
        {
            var central = Plugin.central;
            if (central != null) {

                if (central.saveload.gameObject.activeSelf || central.settings.gameObject.activeSelf || central.pause.gameObject.activeSelf || central.unsavedContentPopup.gameObject.activeSelf)
                {
                    return;
                }

                SplitsButtons(gui);
                SplitsList(gui);

                BlockInput();
            }
            
        }

        private void BlockInput()
        {
            if (HoveringWindows())
            {
                if (!wasHoveringAnyWindowLastFrame)
                {
                    if (Plugin.central?.cam != null)
                        Plugin.central.cam.OverrideOutsideGameView(true);
                }
                wasHoveringAnyWindowLastFrame = true;
            }
            else
            {
                if (wasHoveringAnyWindowLastFrame)
                {
                    if (Plugin.central?.cam != null)
                        Plugin.central.cam.OverrideOutsideGameView(false);
                }
                wasHoveringAnyWindowLastFrame = false;
            }
        }

        private void SplitsButtons(ImGui gui)
        {
           
            ImRect rect = new ImRect(Screen.width * 0.4f - Screen.width * SplitsButtonWidthPercent * 0.5f, 0, Screen.width * SplitsButtonWidthPercent, gui.GetRowHeight() * SplitsButtonHeightRows);

            isDrawingSplitsButtons = _SplitsButtonOpen;

            if (_SplitsButtonOpen && gui.BeginWindow("com.andme.editorspeedsplits_Splits", ref _SplitsButtonOpen, ref isHoveringSplitsButtons, rect, ImWindowFlag.NoCloseButton))
            {
                var columns = gui.Arena.AllocArray<ImRect>(2);
                gui.GetWindowContentRect().SplitHorizontal(ref columns, columns.Length, gui.Style.Layout.Spacing);
                SplitsButton(gui, columns);
                ResetButton(gui, columns);

                isDrawingSplitsButtons = false;
                gui.EndWindow();
            }
        }

        private void SplitsButton(ImGui gui, Span<ImRect> columns)
        {
            ImStyleButton splitsButtonStyle = gui.Style.Button;

            splitsButtonStyle.Normal.BackColor = new Color(255f / 255f, 146f / 255f, 0f / 255f); // Orange rgb(255, 146, 0)
            splitsButtonStyle.Hovered.BackColor = new Color(255f / 255f, 201f / 255f, 128f / 255f); // Lighter Orange rgb(255, 201, 128)
            splitsButtonStyle.Pressed.BackColor = new Color(153f / 255f, 0f / 255f, 0f / 255f); // Darker red for pressed state rgb(153, 0, 0)

            splitsButtonStyle.Normal.FrontColor = new Color(255f / 255f, 255f / 255f, 255f / 255f); // White rgb(255, 255, 255)
            var id = gui.GetNextControlId();
            if (gui.Button(id, "Splits", columns[0], in splitsButtonStyle, out _))
            {
                if (_SplitsListOpen)
                {
                    _SplitsListOpen = false;
                }
                else
                {
                    _SplitsListOpen = true;
                    RefreshSplits();
                }
                
            }
        }

        private void ResetButton(ImGui gui, Span<ImRect> columns)
        {
            ImStyleButton resetButtonStyle = gui.Style.Button;

            resetButtonStyle.Normal.BackColor = new Color(64f / 255f, 122f / 255f, 255f / 255f); // Blue rgb(64, 122, 255)
            resetButtonStyle.Hovered.BackColor = new Color(128f / 255f, 166f / 255f, 255f / 255f); // Lighter Blue rgb(128, 166, 255)
            resetButtonStyle.Pressed.BackColor = new Color(0f / 255f, 64f / 255f, 128f / 255f); // Darker blue for pressed state rgb(0, 64, 128)

            resetButtonStyle.Normal.FrontColor = new Color(255f / 255f, 255f / 255f, 255f / 255f); // White rgb(255, 255, 255)
            var id = gui.GetNextControlId();
            if (gui.Button(id, "Reset", columns[1], in resetButtonStyle, out _))
            {
                ResetSplits();
            }
        }

        internal static void MyDrawTitleBar(ImGui gui, ImRect rect, ReadOnlySpan<char> text)
        {
            
            ref readonly var style = ref gui.Style.Window;
            

            var radiusTopLeft = style.Box.BorderRadius.TopLeft - style.Box.BorderThickness;
            var radiusTopRight = style.Box.BorderRadius.TopRight - style.Box.BorderThickness;
            var radius = new ImRectRadius(radiusTopLeft, radiusTopRight);

            Span<Vector2> border = stackalloc Vector2[2] { rect.BottomLeft, rect.BottomRight };

            // Changed colors to match the splits button style
            Color titlebarColor = new Color(87f / 255f, 87f / 255f, 87f / 255f); // Dark gray for title bar background rgb(87, 87, 87)

            gui.Canvas.Rect(rect, titlebarColor, radius);
            gui.Canvas.Line(border, style.Box.BorderColor, false, style.Box.BorderThickness, 0.0f);
            
        }

        private void SplitsList(ImGui gui)
        {
            ImRect rect;

            if (_SplitsListOpen)
            {
                if (splits != null)
                {
                    var textSize = gui.MeasureTextSize($"{"",-splitWidth}" + $"{"",-timeWidth}" + $"{"",velocityWidth}");
                    var rectSize = textSize;
                    // Probably missing some spacing values here and there, but it looks good enough
                    // TODO: Check the timing, I saw some bugs
                    rectSize.x += gui.Style.Layout.InnerSpacing * 2f;
                    rectSize.x += gui.Style.Scroll.Size + gui.Style.Layout.Spacing*4f;
                    rectSize.x *= 1.1f; // Add some extra width to prevent text from being too close to the edge or scrollbar
                    rectSize.y = gui.GetRowHeight() * splits.Count + gui.Style.Layout.Spacing * (splits.Count - 1) + gui.Style.Layout.InnerSpacing * 2f;
                    rectSize.y += gui.GetRowHeight()*2f; 

                    rect = new ImRect(Screen.width * 0.4f - rectSize.x * 0.5f, gui.GetRowHeight() * SplitsButtonHeightRows, rectSize.x, rectSize.y);

                }
                else
                {
                    rect = new ImRect(Screen.width * 0.4f - Screen.width * 0.1f, gui.GetRowHeight() * SplitsButtonHeightRows, Screen.width * 0.2f, gui.GetRowHeight() * 3);
                }

                isDrawingSplitsList = _SplitsListOpen;
                if (gui.BeginWindow("Splits List", ref _SplitsListOpen, ref isHoveringSplitsList, rect))
                {
                    if (splits == null)
                    {
                        gui.Text("No splits available.");
                        isDrawingSplitsList = false;
                        gui.EndWindow();
                        return;
                    }

                    gui.BeginList((gui.GetLayoutWidth(), gui.GetLayoutHeight()));
                    for (int i = 0; i < splits.Count; ++i)
                    {
                        var isSelected = selectedIndex == i;

                        string splitLabel =
                        $"{(splits[i].index != 0 ? $"CP{splits[i].index}" : "FIN"),-splitWidth}" +
                        $"{FormatTime(splits[i].time),-timeWidth}" +
                        $"{splits[i].velocity,velocityWidth:F2}";


                        if (gui.ListItem(isSelected, splitLabel))
                        {
                            selectedIndex = i;
                            OnSplitRowClicked(splits[i]);   
                        }
                    }
                    gui.EndList();
                    isDrawingSplitsList = false;
                    gui.EndWindow();
                }
            }
        }

        private string FormatTime(float seconds)
        {
            TimeSpan t = TimeSpan.FromSeconds(seconds);
            return $"{t.Minutes:00}:{t.Seconds:00}.{t.Milliseconds:000}";
        }

        internal void RefreshSplits()
        {
            splits = null;
            selectedIndex = -1;

            string levelName = Plugin.fullLevelName;

            if (string.IsNullOrEmpty(levelName))
                return;

            var replay = SplitRecorder.LoadBestSplits(levelName);
            if (replay == null)
                return;

            splits = replay.splits;
            
        }
        
        private void ResetSplits()
        {
            Plugin.ResetSplitsForCurrentLevel(true);
            _SplitsListOpen = false;
        }

        private void OnSplitRowClicked(EditorSplit split)
        {
            if (split == null)
                return;

            if (!TryMoveEditorCamera(split.planePosition, split.planeOrientation, split.bounds))
            {
                Plugin.logger.LogWarning($"Could not move editor camera for split {split.index}.");
                return;
            }
        }

        private bool TryMoveEditorCamera(Vector3 planePosition, Vector3 planeOrientation, Bounds bounds)
        {
            if (Plugin.central?.cam == null)
                return false;

            var moveCamera = Plugin.central.cam;
            if (moveCamera.cameraTransform == null)
                return false;

            if (planePosition == Vector3.zero && planeOrientation == Vector3.zero && (bounds == null || bounds.size == Vector3.zero))
            {
                Plugin.logger.LogWarning("Invalid camera parameters: planePosition, planeOrientation, or bounds are zero.");
                MessengerApi.LogWarning("[EditorSpeedSplits] Corrupted Splits File. Cannot move camera for this replay");
                return false;
            }

            Vector3 size;
            if (bounds == null)
                size = Vector3.one * 5f;
            else
                size = bounds.size;

            // ---- Dynamic Offsets ----
            float cameraBackOffset = Mathf.Min(Mathf.Max(size.x, size.z) * 0.7f, 500);
            float cameraHeightOffset = 5f;

            Vector3 planeDir = Vector3.ProjectOnPlane(planeOrientation, Vector3.up).normalized;

            if (planeDir.sqrMagnitude < 0.001f)
                planeDir = Vector3.forward;

            Vector3 projectedOrientation = planeDir;


            // Move camera to the plane position
            moveCamera.transform.position = planePosition + Vector3.up * cameraHeightOffset + projectedOrientation * cameraBackOffset;

            // Rotate camera to look at the plane orientation
            moveCamera.cameraTransform.LookAt(planePosition, Vector3.up);

            moveCamera.rotationX = Mathf.DeltaAngle(0f, moveCamera.cameraTransform.eulerAngles.y);
            moveCamera.rotationY = -Mathf.DeltaAngle(0f, moveCamera.cameraTransform.eulerAngles.x);

            return true;
        }
        public bool HoveringWindows()
        {
            return (isHoveringSplitsButtons && _SplitsButtonOpen) || (isHoveringSplitsList && _SplitsListOpen);
        }
    }
}