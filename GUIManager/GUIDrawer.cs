using System;
using System.Collections.Generic;
using EditorSpeedSplits.Splits;
using Imui.Controls;
using Imui.Core;
using UnityEngine;
using ZeepSDK.Messaging;
using ZeepSDK.UI;

namespace EditorSpeedSplits.GUIManager;

public class EditorSplitsGUIDrawer : IZeepGUIDrawer
{
    // Constants
    private const float SplitsButtonWidthPercent = 0.1f;
    private const float SplitsButtonHeightRows = 2f;
    private const int SplitWidth = 8;
    private const int TimeWidth = 12;
    private const int VelocityWidth = 8;

    private readonly WindowRects _windowRects = new();

    private bool _isHoveringSplitsButtons;

    private bool _isHoveringSplitsList;

    private WindowRects _loadedWindowRects = new();

    private bool _loadRectsButtons;
    private bool _loadRectsList;
    private int _selectedIndex = -1;

    private bool _shouldUpdateSize;

    private List<EditorSplit> _splits;

    private bool _wasHoveringAnyWindowLastFrame;

    public bool IsDrawingSplitsButtons;
    public bool SplitsButtonOpen;

    public bool SplitsListOpen;

    public void OnZeepGUI(ImGui gui)
    {
        var central = Plugin.Central;
        if (central == null) return;
        if (central.saveload.gameObject.activeSelf || central.settings.gameObject.activeSelf ||
            central.pause.gameObject.activeSelf || central.unsavedContentPopup.gameObject.activeSelf) return;

        SplitsButtons(gui);
        SplitsList(gui);

        BlockInput();
    }

    private void SplitsButtons(ImGui gui)
    {
        if (!SplitsButtonOpen) return;
        ImRect rect;
        if (_loadRectsButtons)
        {
            _loadRectsButtons = false;
            rect = _loadedWindowRects.ButtonsWindow;
            Plugin.logger.LogInfo("Loaded splits buttons window rect");
        }
        else
        {
            rect = new ImRect(Screen.width * 0.4f - Screen.width * SplitsButtonWidthPercent * 0.5f, 0,
                Screen.width * SplitsButtonWidthPercent, gui.GetRowHeight() * SplitsButtonHeightRows);
        }

        IsDrawingSplitsButtons = true;

        if (gui.BeginWindow("com.andme.editorspeedsplits_Splits", ref SplitsButtonOpen,
                ref _isHoveringSplitsButtons, rect, ImWindowFlag.NoCloseButton))
        {
            ref var windowState = ref gui.WindowManager.GetWindowState(gui.PeekId());
            _windowRects.ButtonsWindow = windowState.Rect;

            var columns = gui.Arena.AllocArray<ImRect>(2);
            gui.GetWindowContentRect().SplitHorizontal(ref columns, columns.Length, gui.Style.Layout.Spacing);
            SplitsButton(gui, columns);
            ResetButton(gui, columns);
            gui.EndWindow();
        }

        IsDrawingSplitsButtons = false;
    }

    private void SplitsButton(ImGui gui, Span<ImRect> columns)
    {
        var splitsButtonStyle = gui.Style.Button;

        splitsButtonStyle.Normal.BackColor = new Color(255f / 255f, 146f / 255f, 0f / 255f); // Orange rgb(255, 146, 0)
        splitsButtonStyle.Hovered.BackColor =
            new Color(255f / 255f, 201f / 255f, 128f / 255f); // Lighter Orange rgb(255, 201, 128)
        splitsButtonStyle.Pressed.BackColor =
            new Color(153f / 255f, 0f / 255f, 0f / 255f); // Darker red for pressed state rgb(153, 0, 0)

        splitsButtonStyle.Normal.FrontColor =
            new Color(255f / 255f, 255f / 255f, 255f / 255f); // White rgb(255, 255, 255)
        var id = gui.GetNextControlId();
        if (!gui.Button(id, "Splits", columns[0], in splitsButtonStyle, out _)) return;
        if (SplitsListOpen)
        {
            SplitsListOpen = false;
        }
        else
        {
            SplitsListOpen = true;
            RefreshSplits();
        }
    }

    private void ResetButton(ImGui gui, Span<ImRect> columns)
    {
        var resetButtonStyle = gui.Style.Button;

        resetButtonStyle.Normal.BackColor = new Color(64f / 255f, 122f / 255f, 255f / 255f); // Blue rgb(64, 122, 255)
        resetButtonStyle.Hovered.BackColor =
            new Color(128f / 255f, 166f / 255f, 255f / 255f); // Lighter Blue rgb(128, 166, 255)
        resetButtonStyle.Pressed.BackColor =
            new Color(0f / 255f, 64f / 255f, 128f / 255f); // Darker blue for pressed state rgb(0, 64, 128)

        resetButtonStyle.Normal.FrontColor =
            new Color(255f / 255f, 255f / 255f, 255f / 255f); // White rgb(255, 255, 255)
        var id = gui.GetNextControlId();
        if (gui.Button(id, "Reset", columns[1], in resetButtonStyle, out _)) ResetSplits();
    }

    internal static void MyDrawTitleBar(ImGui gui, ImRect rect)
    {
        ref readonly var style = ref gui.Style.Window;


        var radiusTopLeft = style.Box.BorderRadius.TopLeft - style.Box.BorderThickness;
        var radiusTopRight = style.Box.BorderRadius.TopRight - style.Box.BorderThickness;
        var radius = new ImRectRadius(radiusTopLeft, radiusTopRight);

        Span<Vector2> border = stackalloc Vector2[2] { rect.BottomLeft, rect.BottomRight };

        // Changed colors to match the splits button style
        Color titlebarColor =
            new(87f / 255f, 87f / 255f, 87f / 255f); // Dark gray for title bar background rgb(87, 87, 87)

        gui.Canvas.Rect(rect, titlebarColor, radius);
        gui.Canvas.Line(border, style.Box.BorderColor, false, style.Box.BorderThickness, 0.0f);
    }

    private void SplitsList(ImGui gui)
    {
        if (SplitsListOpen)
        {
            ImRect rect;
            if (_splits != null)
            {
                var textSize = gui.MeasureTextSize($"{"",-SplitWidth}" + $"{"",-TimeWidth}" + $"{"",VelocityWidth}");
                var rectSize = textSize;
                // Probably missing some spacing values here and there, but it looks good enough
                // TODO: Check the timing, I saw some bugs
                rectSize.x += gui.Style.Layout.InnerSpacing * 2f;
                rectSize.x += gui.Style.Scroll.Size + gui.Style.Layout.Spacing * 4f;
                rectSize.x *=
                    1.1f; // Add some extra width to prevent text from being too close to the edge or scrollbar
                rectSize.y = gui.GetRowHeight() * _splits.Count + gui.Style.Layout.Spacing * (_splits.Count - 1) +
                             gui.Style.Layout.InnerSpacing * 2f;
                rectSize.y += gui.GetRowHeight() * 2f;

                rect = new ImRect(Screen.width * 0.4f - rectSize.x * 0.5f, gui.GetRowHeight() * SplitsButtonHeightRows,
                    rectSize.x, rectSize.y);
            }
            else
            {
                rect = new ImRect(Screen.width * 0.4f - Screen.width * 0.1f,
                    gui.GetRowHeight() * SplitsButtonHeightRows, Screen.width * 0.2f, gui.GetRowHeight() * 3);
            }

            if (_loadRectsList)
            {
                _loadRectsList = false;
                rect.Position = _loadedWindowRects.SplitsWindow.Position;
                Plugin.logger.LogInfo("Loaded splits list window rect");
            }

            if (gui.BeginWindow("Splits List", ref SplitsListOpen, ref _isHoveringSplitsList, rect))
            {
                ref var windowState = ref gui.WindowManager.GetWindowState(gui.PeekId());
                _windowRects.SplitsWindow = windowState.Rect;
                if (_shouldUpdateSize) windowState.Rect.Size = rect.Size;

                if (_splits == null)
                {
                    gui.Text("No splits available.");
                    gui.EndWindow();
                    return;
                }

                gui.BeginList((gui.GetLayoutWidth(), gui.GetLayoutHeight()));
                for (var i = 0; i < _splits.Count; ++i)
                {
                    var isSelected = _selectedIndex == i;

                    var splitLabel =
                        $"{(_splits[i].index != 0 ? $"CP{_splits[i].index}" : "FIN"),-SplitWidth}" +
                        $"{FormatTime(_splits[i].time),-TimeWidth}" +
                        $"{_splits[i].velocity,VelocityWidth:F2}";


                    if (!gui.ListItem(isSelected, splitLabel)) continue;
                    _selectedIndex = i;
                    OnSplitRowClicked(_splits[i]);
                }

                gui.EndList();
                gui.EndWindow();
            }
        }

        _shouldUpdateSize = false;
    }

    private static string FormatTime(float seconds)
    {
        var t = TimeSpan.FromSeconds(seconds);
        return $"{t.Minutes:00}:{t.Seconds:00}.{t.Milliseconds:000}";
    }

    internal void RefreshSplits()
    {
        _splits = null;
        _selectedIndex = -1;

        var levelName = Plugin.FullLevelName;

        if (string.IsNullOrEmpty(levelName))
            return;

        var replay = SplitRecorder.LoadBestSplits(levelName);
        if (replay == null)
            return;

        _splits = replay.splits;

        _shouldUpdateSize = true;
    }

    private void ResetSplits()
    {
        Plugin.ResetSplitsForCurrentLevel(true);
        SplitsListOpen = false;
    }

    private static void OnSplitRowClicked(EditorSplit split)
    {
        if (split == null)
            return;

        if (!TryMoveEditorCamera(split.planePosition, split.planeOrientation, split.bounds))
            Plugin.logger.LogWarning($"Could not move editor camera for split {split.index}.");
    }

    private static bool TryMoveEditorCamera(Vector3 planePosition, Vector3 planeOrientation, Bounds bounds)
    {
        if (Plugin.Central?.cam == null)
            return false;

        var moveCamera = Plugin.Central.cam;
        if (moveCamera.cameraTransform == null)
            return false;

        if (planePosition == Vector3.zero && planeOrientation == Vector3.zero &&
            bounds.size == Vector3.zero)
        {
            Plugin.logger.LogWarning("Invalid camera parameters: planePosition, planeOrientation, or bounds are zero.");
            MessengerApi.LogWarning("[EditorSpeedSplits] Corrupted Splits File. Cannot move camera for this replay");
            return false;
        }

        var size = bounds.size;

        // ---- Dynamic Offsets ----
        var cameraBackOffset = Mathf.Min(Mathf.Max(size.x, size.z) * 0.7f, 500);
        const float cameraHeightOffset = 5f;

        var planeDir = Vector3.ProjectOnPlane(planeOrientation, Vector3.up).normalized;

        if (planeDir.sqrMagnitude < 0.001f)
            planeDir = Vector3.forward;

        var projectedOrientation = planeDir;


        // Move camera to the plane position
        moveCamera.transform.position =
            planePosition + Vector3.up * cameraHeightOffset + projectedOrientation * cameraBackOffset;

        // Rotate camera to look at the plane orientation
        moveCamera.cameraTransform.LookAt(planePosition, Vector3.up);

        moveCamera.rotationX = Mathf.DeltaAngle(0f, moveCamera.cameraTransform.eulerAngles.y);
        moveCamera.rotationY = -Mathf.DeltaAngle(0f, moveCamera.cameraTransform.eulerAngles.x);

        return true;
    }

    private bool HoveringWindows()
    {
        return (_isHoveringSplitsButtons && SplitsButtonOpen) || (_isHoveringSplitsList && SplitsListOpen);
    }

    private void BlockInput()
    {
        if (HoveringWindows())
        {
            if (!_wasHoveringAnyWindowLastFrame)
                if (Plugin.Central?.cam != null)
                    Plugin.Central.cam.OverrideOutsideGameView(true);
            _wasHoveringAnyWindowLastFrame = true;
        }
        else
        {
            if (_wasHoveringAnyWindowLastFrame)
                if (Plugin.Central?.cam != null)
                    Plugin.Central.cam.OverrideOutsideGameView(false);
            _wasHoveringAnyWindowLastFrame = false;
        }
    }

    public void SaveWindowsRects()
    {
        Plugin.Instance.PersonalBestSplitsStorage.SaveToJson("SplitsWindowRects", _windowRects);
    }

    public void LoadWindowsRects()
    {
        if (!Plugin.Instance.PersonalBestSplitsStorage.JsonFileExists("SplitsWindowRects")) return;
        _loadedWindowRects =
            Plugin.Instance.PersonalBestSplitsStorage.LoadFromJson<WindowRects>("SplitsWindowRects");
        _loadRectsButtons = true;
        _loadRectsList = true;
    }
}