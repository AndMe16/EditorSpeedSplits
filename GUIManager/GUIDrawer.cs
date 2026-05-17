using Imui.Controls;
using Imui.Core;
using Imui.Style;
using System;
using UnityEngine;
using ZeepSDK.UI;

namespace EditorSpeedSplits.GUIManager
{
    public class EditorSplitsGUIDrawer : IZeepGUIDrawer
    {
        public bool _SplitsButtonOpen = false;

        public bool _SplitsListOpen = false;

        public bool isDrawingSplitsButtons = false;

        // Constants
        const float SplitsButtonWidthPercent = 0.1f;
        const float SplitsButtonHeightRows = 2f;

        public void OnZeepGUI(ImGui gui)
        {
            SplitsButtons(gui);
        }

        private void SplitsButtons(ImGui gui)
        {
           
            ImRect rect = new ImRect(Screen.width * 0.5f - Screen.width * SplitsButtonWidthPercent * 0.5f, 0, Screen.width * SplitsButtonWidthPercent, gui.GetRowHeight() * SplitsButtonHeightRows);

            isDrawingSplitsButtons = _SplitsButtonOpen? true : false;

            if (_SplitsButtonOpen && gui.BeginWindow("com.andme.editorspeedsplits_Splits", ref _SplitsButtonOpen, rect, ImWindowFlag.NoCloseButton))
            {
                
                var columns = gui.Arena.AllocArray<ImRect>(2);

                gui.GetWindowContentRect().SplitHorizontal(ref columns, columns.Length, gui.Style.Layout.Spacing);

                ImStyleButton splitsButtonStyle = gui.Style.Button;

                splitsButtonStyle.Normal.BackColor = new Color(255f / 255f, 146f / 255f, 0f / 255f); // Orange rgb(255, 146, 0)
                splitsButtonStyle.Hovered.BackColor = new Color(255f / 255f, 201f / 255f, 128f / 255f); // Lighter Orange rgb(255, 201, 128)
                splitsButtonStyle.Pressed.BackColor = new Color(153f / 255f, 0f / 255f, 0f / 255f); // Darker red for pressed state rgb(153, 0, 0)

                splitsButtonStyle.Normal.FrontColor = new Color(255f / 255f, 255f / 255f, 255f / 255f); // White rgb(255, 255, 255)
                var id = gui.GetNextControlId();
                if (gui.Button(id, "Splits", columns[0], in splitsButtonStyle, out _))
                {
                    // Button1 action
                }

                ImStyleButton resetButtonStyle = gui.Style.Button;

                resetButtonStyle.Normal.BackColor = new Color(64f / 255f, 122f / 255f, 255f / 255f); // Blue rgb(64, 122, 255)
                resetButtonStyle.Hovered.BackColor = new Color(128f / 255f, 166f / 255f, 255f / 255f); // Lighter Blue rgb(128, 166, 255)
                resetButtonStyle.Pressed.BackColor = new Color(0f / 255f, 64f / 255f, 128f / 255f); // Darker blue for pressed state rgb(0, 64, 128)

                resetButtonStyle.Normal.FrontColor = new Color(255f / 255f, 255f / 255f, 255f / 255f); // White rgb(255, 255, 255)
                id = gui.GetNextControlId();
                if (gui.Button(id, "Reset", columns[1], in resetButtonStyle, out _))
                {
                    // Button2 action
                }
                isDrawingSplitsButtons = false;
                gui.EndWindow();
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

            var contentRect = rect.TakeLeft(rect.W - (gui.GetRowHeight() - gui.Style.Layout.InnerSpacing));
            var textSettings = new ImTextSettings(gui.Style.Layout.TextSize, style.TitleBar.Alignment, overflow: style.TitleBar.Overflow);

            Color titlebarTextColor = new Color(255f / 255f, 255f / 255f, 255f / 255f); // White for title text rgb(255, 255, 255)

            gui.Canvas.Text("Splits", titlebarTextColor, contentRect, in textSettings);
            
        }



        //private void SavesWindow(ImGui gui)
        //{
        //    if (_SavesWindowOpen && gui.BeginWindow("Editor Recordings", ref _SavesWindowOpen, (500, 500)))
        //    {
        //        ListOfRecordings(gui);

        //        RecordingInfo(gui);
        //        gui.EndWindow();
        //    }
        //}

        //private void PlaybackWindowOpen(ImGui gui)
        //{
        //    ImRect rect = new ImRect(0, 0, Screen.width * 0.3f, Screen.height * 0.2f);

        //    if (_PlaybackWindowOpen && gui.BeginWindow("Playback Controls", ref _PlaybackWindowOpen, rect, ImWindowFlag.NoCloseButton))
        //    {
        //        var manager = PlaybackManager.PlaybackManager.Instance;

        //        gui.Separator("Playback");

        //        CustomSliderHeader(gui, "Time", manager._currentSessionTime);
        //        if (gui.Slider(ref manager._currentSessionTime, 0, ((float)manager.Session.duration.TotalSeconds)))
        //        {
        //            manager.ScrubToTime(manager._currentSessionTime);
        //        }

        //        gui.BeginHorizontal();

        //        var playPauseIcon = manager.IsFollowingTimeline ? "\u23F8" : "\u25B6";

        //        if (gui.Button(playPauseIcon, size: new ImSize(gui.GetLayoutWidth() * 0.1f, gui.GetRowHeight())))
        //        {
        //            if (manager.IsFollowingTimeline)
        //            {
        //                manager.StopFollowingTimeline();
        //            }
        //            else
        //            {
        //                manager.StartFollowingTimeline();
        //            }
        //        }

        //        gui.AddSpacing();

        //        float speed = manager.SpeedMultiplier;
        //        gui.NumericEdit(ref speed, step: 0.25f, size: new ImSize(gui.GetLayoutWidth() * 0.3f, gui.GetRowHeight()), flags: ImNumericEditFlag.PlusMinus, format: "F2", min: 0.25f, max: 50);
        //        manager.SpeedMultiplier = speed;

        //        gui.AddSpacing();

        //        if (!manager.IsFollowingTimeline)
        //        {
        //            if (gui.Button("<", ImSizeMode.Auto))
        //            {
        //                manager.StepBackward();
        //                manager.UpdateGhostFromTimeline(manager._currentSessionTime);
        //            }
        //            if (gui.Button(">", ImSizeMode.Auto))
        //            {
        //                manager.StepForward();
        //                manager.UpdateGhostFromTimeline(manager._currentSessionTime);
        //            }
        //        }

        //        gui.AddSpacing();

        //        if (gui.Checkbox(ref manager.followCamera, "Follow Camera", ImSizeMode.Auto))
        //        {
        //            manager.ToggledFollowCamera();
        //        }

        //        gui.EndHorizontal();

        //        gui.Separator("Recording");

        //        gui.BeginHorizontal();

        //        var playbackRecorder = RecorderLifecycleBridge.RecorderLifecycleBridge.playbackCameraRecorder;

        //        if (playbackRecorder != null)
        //        {

        //            if (playbackRecorder?.recording == true)
        //            {
        //                if (gui.Button("\u23F9", size: new ImSize(gui.GetLayoutWidth() * 0.1f, gui.GetRowHeight())))
        //                {
        //                    playbackRecorder.StopRecording();
        //                }

        //            }
        //            else
        //            {
        //                if (gui.Button("\u23FA", new ImSize(gui.GetLayoutWidth() * 0.1f, gui.GetRowHeight())))
        //                {
        //                    playbackRecorder.StartRecording();
        //                }
        //            }
        //        }

        //        string timeSinceStartRecordingString;

        //        if (playbackRecorder.recordingTime != TimeSpan.Zero)
        //        {
        //            timeSinceStartRecordingString = playbackRecorder.recordingTime.ToString(@"hh\:mm\:ss");
        //        }
        //        else
        //        {
        //            timeSinceStartRecordingString = "--:--:--";
        //        }

        //        gui.TextEditNonEditable(timeSinceStartRecordingString, size: new ImSize(gui.GetLayoutWidth() * 0.2f, gui.GetRowHeight()));

        //        gui.EndHorizontal();

        //        gui.EndWindow();
        //    }
        //}

        //private void ListOfRecordings(ImGui gui)
        //{
        //    gui.Separator("List of recordings");

        //    gui.BeginList((gui.GetLayoutWidth(), ImList.GetEnclosingHeight(gui, gui.GetRowsHeightWithSpacing(5))));

        //    for (int i = 0; i < values.Length; ++i)
        //    {

        //        if (gui.ListItem(ref _selectedIndex, i, values[i]))
        //        {
        //            selectedRecording = values[i];
        //            var session = FilesManager.FilesManager.LoadRecordingSession(Plugin.Storage, selectedRecording);
        //            if (session != null)
        //                info = $"Name: {selectedRecording}\n" +
        //                      $"Date: {session.savingTime:G}\n" +
        //                      $"Duration: {session.duration:hh':'mm':'ss}\n" +
        //                      $"Actions recorded: {session.eventCount}";

        //        }
        //    }

        //    gui.EndList();

        //}

        //private void RecordingInfo(ImGui gui)
        //{
        //    gui.Separator("Recording info");

        //    if (selectedRecording != null)
        //    {
        //        if (info != null)
        //        {
        //            gui.TextEditNonEditable(info, (gui.GetLayoutWidth(), gui.GetTextLineHeight() * 4.5f), true);
        //        }
        //        else
        //        {
        //            gui.TextEditNonEditable("Failed to load recording session.", (gui.GetLayoutWidth(), gui.GetTextLineHeight() * 1.5f), true);
        //        }

        //        gui.AddSpacing();

        //        gui.BeginHorizontal();
        //        if (gui.Button("Open", ImSizeMode.Auto))
        //        {
        //            Plugin.logger.LogInfo($"[GUIDrawer] Opening recording {selectedRecording}");
        //            RecorderLifecycleBridge.RecorderLifecycleBridge.OpenPlaybackScene(selectedRecording);
        //        }

        //        gui.AddSpacing();

        //        if (gui.Button("Delete", ImSizeMode.Auto))
        //        {
        //            Plugin.logger.LogInfo($"[GUIDrawer] Deleting recording {selectedRecording}");
        //            FilesManager.FilesManager.DeleteRecordingSession(Plugin.Storage, selectedRecording);
        //            RefreshUI();
        //        }

        //        gui.EndHorizontal();
        //    }
        //}

        //private void RefreshFiles()
        //{
        //    values = FilesManager.FilesManager.GetAllRecordingSessions(Plugin.Storage);
        //}

        //public void OpenSavesWindow()
        //{
        //    RefreshUI();
        //    _SavesWindowOpen = true;
        //}

        //public void OpenPlaybackWindow()
        //{
        //    _PlaybackWindowOpen = true;
        //}

        //public void ClosePlaybackWindow()
        //{
        //    _PlaybackWindowOpen = false;
        //}

        //public void RefreshUI()
        //{
        //    RefreshFiles();
        //    selectedRecording = null;
        //    _selectedIndex = -1;
        //}

        //public void CustomSliderHeader(ImGui gui,
        //                                ReadOnlySpan<char> label,
        //                                float value)
        //{
        //    gui.AddSpacingIfLayoutFrameNotEmpty();
        //    gui.BeginHorizontal();

        //    var rowHeight = gui.GetRowHeight();
        //    var height = rowHeight * gui.Style.Slider.HeaderScale;
        //    var rect = gui.AddLayoutRect(gui.GetLayoutWidth(), height);
        //    var barHeight = gui.Style.Slider.BarThickness * rowHeight;
        //    var padding = (rowHeight - barHeight) * 0.5f;
        //    var fontSize = gui.TextDrawer.GetFontSizeFromLineHeight(height);

        //    // (artem-s): align with slider's bar
        //    rect.X += padding;
        //    rect.W -= padding * 2;

        //    // (artem-s): shift rect down by spacing value so there is no gap between header and slider itself
        //    rect.Y -= gui.Style.Layout.Spacing;

        //    var textSettings = new ImTextSettings(fontSize, 0.0f, 1.0f, overflow: ImTextOverflow.Ellipsis);
        //    gui.Text(label, textSettings, rect);

        //    var time = TimeSpan.FromSeconds(value);

        //    string valueFormatted = value % 1 == 0
        //        ? time.ToString(@"hh\:mm\:ss")
        //        : time.ToString(@"hh\:mm\:ss\.f");
        //    textSettings.Align.X = 1.0f;
        //    gui.Text(valueFormatted, textSettings, rect);

        //    gui.EndHorizontal();
        //}

    }
}