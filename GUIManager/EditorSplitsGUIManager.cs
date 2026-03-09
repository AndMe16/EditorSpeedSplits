using EditorSpeedSplits.Splits;
using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZeepSDK.UI;

namespace EditorSpeedSplits.GUIManager
{
    internal class EditorSplitsGUIManager : MonoBehaviour
    {
        internal Transform canvas;

        private GameObject buttonsPanel;
        private GameObject splitsPanel;
        private Transform splitsContent;
        private bool splitsVisible;
        private TMP_FontAsset cachedEditorFont;
        private Sprite cachedSprite;
        private Transform modRootTransform;

        // Mod root
        private static readonly Vector2 ModRootAnchorMin = new Vector2(0.375f, 0.0f);
        private static readonly Vector2 ModRootAnchorMax = new Vector2(0.625f, 0.5f);

        // Button panel (offset-driven)
        private static readonly Vector2 ButtonPanelAnchorMin = new Vector2(0f, 0f);
        private static readonly Vector2 ButtonPanelAnchorMax = new Vector2(1f, 1f);
        private static readonly Vector2 ButtonPanelOffsetMin = new Vector2(0f, 0f);
        private static readonly Vector2 ButtonPanelOffsetMax = new Vector2(0f, -260f);
        private const float ButtonPanelMinWidth = 110f;
        private const float ButtonPanelMinHeight = 40f;
        private const float ButtonPanelHeaderHeight = 15f;
        private const float ButtonPanelHeaderPad = 2f;
        private static readonly Color ButtonPanelColor = new Color(1f, 0.55f, 0.04f, 1f);
        private static readonly Color ButtonPanelHeaderColor = new Color(1f, 0.75f, 0.39f, 1f);

        // Primary buttons
        private const float ButtonsPad = 3f;

        // Splits button
        private static readonly Vector2 SplitsButtonAnchorMin = new Vector2(0.0f, 0f);
        private static readonly Vector2 SplitsButtonAnchorMax = new Vector2(0.5f, 1f);
        private static readonly Color SplitsButtonColor = new Color(1f, 0.75f, 0.39f, 1f);
        // Reset button
        private static readonly Vector2 ResetButtonAnchorMin = new Vector2(0.5f, 0f);
        private static readonly Vector2 ResetButtonAnchorMax = new Vector2(1f, 1f);
        private static readonly Color ResetButtonColor = new Color(0f, 0.54f, 0.82f, 1f);

        // Header bar
        private static readonly Vector2 HeaderBarAnchorMin = new Vector2(0.02f, 1f);
        private static readonly Vector2 HeaderBarAnchorMax = new Vector2(0.98f, 1f);
        private static readonly Vector2 ResizeHandleAnchorMin = new Vector2(0.90f, 0.1f);
        private static readonly Vector2 ResizeHandleAnchorMax = new Vector2(0.99f, 0.9f);

        // Splits panel (offset-driven)
        private static readonly Vector2 SplitsPanelAnchorMin = new Vector2(0f, 0f);
        private static readonly Vector2 SplitsPanelAnchorMax = new Vector2(1f, 1f);
        private static readonly Vector2 SplitsPanelOffsetMin = new Vector2(0f, 55f);
        private static readonly Vector2 SplitsPanelOffsetMax = new Vector2(0f, 0f);
        private static readonly Color SplitsPanelHeaderColor = new Color(0.09f, 0.25f, 0.62f, 0.92f);


        private const float SplitsPanelMinWidth = 280f;
        private const float SplitsPanelMinHeight = 110f;
        private const float SplitsPanelHeaderHeight = 24f;
                
        private const float SplitsPanelHeaderPad = 4f;
        
        // Shared layout/style constants
        private static readonly Vector2 CenterPivot = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 TopCenterPivot = new Vector2(0.5f, 1f);
        private static readonly Vector2 FullStretchAnchorMin = Vector2.zero;
        private static readonly Vector2 FullStretchAnchorMax = Vector2.one;
        private static readonly Vector2 ZeroOffset = Vector2.zero;
        private const float RoundedSpritePPUMultiplier = 1f;

        private static readonly Color ResizeHandleColor = new Color(0f, 0f, 0f, 0.20f);
        private const float ResizeHandleArrowFontSize = 18f;

        private const float SplitRowHeight = 28f;
        private static readonly Color SplitRowBackgroundColor = new Color(1f, 1f, 1f, 0.5f);
        private static readonly Vector2 SplitRowContentOffsetMin = new Vector2(8f, 2f);
        private static readonly Vector2 SplitRowContentOffsetMax = new Vector2(-8f, -2f);
        private const float SplitRowContentSpacing = 8f;
        private const float SplitRowFlexibleHeight = 0f;

        private static readonly Color RowNormalColor = new Color(0f, 0f, 0f, 1f);
        private static readonly Color RowHighlightedColor = new Color(1f, 1f, 1f, 0.6f);
        private static readonly Color RowPressedColor = new Color(1f, 1f, 1f, 0.8f);
        private static readonly Color RowSelectedColor = new Color(1f, 1f, 1f, 0.6f);
        private static readonly Color RowDisabledColor = new Color(1f, 1f, 1f, 0.02f);
        private const float RowColorMultiplier = 1f;
        private const float RowFadeDuration = 0.08f;

        private const float DefaultBoundsSize = 5f;
        private const float CameraHeightOffset = 5f;
        private const float CameraBackOffsetScale = 0.7f;
        private const float CameraBackOffsetMax = 500f;
        private const float MinPlaneDirectionSqrMagnitude = 0.001f;

        private static readonly Color SplitsPanelColor = new Color(0f, 0f, 0f, 0.2f);
        private static readonly Vector2 ScrollViewOffsetMin = new Vector2(10f, 10f);
        private const float ScrollViewPadding = 10f;
        private const float ScrollSensitivity = 20f;
        private static readonly Color ViewportMaskColor = new Color(1f, 1f, 1f, 0.01f);

        private static readonly Vector2 ContentTopStretchAnchorMin = new Vector2(0f, 1f);
        private static readonly Vector2 ContentTopStretchAnchorMax = new Vector2(1f, 1f);
        private const float SplitsContentSpacing = 6f;

        internal void Initialize()
        {
            if (!CreateModRoot(out Transform modRoot))
                return;

            modRootTransform = modRoot;

            CreateEditorSplitsUI(modRootTransform);
            CreateSplitsPanel(modRootTransform);

        }


        internal bool CreateModRoot(out Transform modRoot)
        {
            canvas = Plugin.central.transform.Find("Canvas");

            if (canvas == null)
            {
                Plugin.logger.LogWarning("Canvas not found");
                modRoot = null;
                return false;
            }

            Transform gameView = canvas.Find("GameView");

            if (gameView == null)
            {
                Plugin.logger.LogWarning("GameView not found");
                modRoot = null;
                return false;
            }

            modRoot = gameView.Find("EditorSplits_UI_");
            if (modRoot == null)
            {
                GameObject go = new GameObject(
                    "EditorSplits_UI_",
                    typeof(RectTransform)
                );
                go.transform.SetParent(gameView, false);
                modRoot = go.transform;

                RectTransform rt = modRoot.GetComponent<RectTransform>();
                rt.anchorMin = ModRootAnchorMin;
                rt.anchorMax = ModRootAnchorMax;
                rt.pivot = CenterPivot;
                rt.offsetMin = ZeroOffset;
                rt.offsetMax = ZeroOffset;

                //UIApi.AddToConfigurator(rt);
            }


            return true;
        }

        internal void CreateEditorSplitsUI(Transform modRoot)
        {
            Transform existingPanel = modRoot.Find("ButtonPanel");
            if (existingPanel != null)
            {
                buttonsPanel = existingPanel.gameObject;
                EnsureHeaderBar(buttonsPanel.transform, buttonsPanel.GetComponent<RectTransform>(), ButtonPanelHeaderColor, ButtonPanelMinWidth, ButtonPanelMinHeight, ButtonPanelHeaderHeight, ButtonPanelHeaderPad);
                EnsurePrimaryButtons(buttonsPanel.transform);
                return;
            }


            buttonsPanel = new GameObject(
                "ButtonPanel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            buttonsPanel.transform.SetParent(modRoot, false);
            AddInputBlocker(buttonsPanel);

            RectTransform panelRT = buttonsPanel.GetComponent<RectTransform>();
            panelRT.anchorMin = ButtonPanelAnchorMin;
            panelRT.anchorMax = ButtonPanelAnchorMax;
            panelRT.pivot = CenterPivot;
            panelRT.offsetMin = ButtonPanelOffsetMin;
            panelRT.offsetMax = ButtonPanelOffsetMax;

            Image img = buttonsPanel.GetComponent<Image>();
            img.color = ButtonPanelColor;
            var sprite = GetRoundedButtonSprite();
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = RoundedSpritePPUMultiplier;
            }

            EnsureHeaderBar(buttonsPanel.transform, buttonsPanel.GetComponent<RectTransform>(), ButtonPanelHeaderColor, ButtonPanelMinWidth, ButtonPanelMinHeight, ButtonPanelHeaderHeight, ButtonPanelHeaderPad);
            EnsurePrimaryButtons(buttonsPanel.transform);
        }

        private void EnsurePrimaryButtons(Transform parent)
        {
            if (parent.Find("SplitsButton") == null)
            {
                CreateButton(
                    parent,
                    "SplitsButton",
                    "Splits",
                    SplitsButtonAnchorMin,
                    SplitsButtonAnchorMax,
                    SplitsButtonColor,
                    ToggleSplitsList
                );
            }

            if (parent.Find("ResetButton") == null)
            {
                CreateButton(
                    parent,
                    "ResetButton",
                    "Reset",
                    ResetButtonAnchorMin,
                    ResetButtonAnchorMax,
                    ResetButtonColor,
                    ResetSplits
                );
            }
        }

        private void EnsureHeaderBar(Transform panel, RectTransform target, Color color, float minWidth, float minHeight, float headerHeight, float headerPad)
        {
            Transform existingHeader = panel.Find("HeaderBar");
            GameObject headerBar;
            if (existingHeader == null)
            {
                headerBar = new GameObject(
                    "HeaderBar",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(EditorSplitsUIDragHandle)
                );
                headerBar.transform.SetParent(panel, false);
            }
            else
            {
                headerBar = existingHeader.gameObject;
                if (headerBar.GetComponent<EditorSplitsUIDragHandle>() == null)
                    headerBar.AddComponent<EditorSplitsUIDragHandle>();
            }

            var headerRT = headerBar.GetComponent<RectTransform>();
            headerRT.anchorMin = HeaderBarAnchorMin;
            headerRT.anchorMax = HeaderBarAnchorMax;
            headerRT.pivot = TopCenterPivot;
            headerRT.offsetMin = new Vector2(0f, -(headerPad + headerHeight));
            headerRT.offsetMax = new Vector2(0f, -headerPad);

            var headerImage = headerBar.GetComponent<Image>();
            headerImage.color = color;
            var sprite = GetRoundedButtonSprite();
            if (sprite != null)
            {
                headerImage.sprite = sprite;
                headerImage.type = Image.Type.Sliced;
                headerImage.pixelsPerUnitMultiplier = RoundedSpritePPUMultiplier;
            }

            var dragHandle = headerBar.GetComponent<EditorSplitsUIDragHandle>();
            dragHandle.Target = target;

            EnsureResizeHandle(headerBar.transform, target, minWidth, minHeight);
        }

        private void EnsureResizeHandle(Transform headerBar, RectTransform target, float minWidth, float minHeight)
        {
            Transform existingHandle = headerBar.Find("ResizeHandle");
            GameObject resizeHandle;
            if (existingHandle == null)
            {
                resizeHandle = new GameObject(
                    "ResizeHandle",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(EditorSplitsUIResizeHandle)
                );
                resizeHandle.transform.SetParent(headerBar, false);
            }
            else
            {
                resizeHandle = existingHandle.gameObject;
                if (resizeHandle.GetComponent<EditorSplitsUIResizeHandle>() == null)
                    resizeHandle.AddComponent<EditorSplitsUIResizeHandle>();
            }

            RectTransform handleRT = resizeHandle.GetComponent<RectTransform>();
            handleRT.anchorMin = ResizeHandleAnchorMin;
            handleRT.anchorMax = ResizeHandleAnchorMax;
            handleRT.pivot = CenterPivot;
            handleRT.offsetMin = Vector2.zero;
            handleRT.offsetMax = Vector2.zero;

            Image handleImage = resizeHandle.GetComponent<Image>();
            handleImage.color = ResizeHandleColor;

            var resizeHandleComponent = resizeHandle.GetComponent<EditorSplitsUIResizeHandle>();
            resizeHandleComponent.Target = target;
            resizeHandleComponent.MinWidth = minWidth;
            resizeHandleComponent.MinHeight = minHeight;

            Transform arrowLabel = resizeHandle.transform.Find("Arrow");
            if (arrowLabel == null)
            {
                var text = CreateTMPLabel(resizeHandle.transform, "->");
                text.name = "Arrow";
                text.alignment = TextAlignmentOptions.Center;
                text.fontSize = ResizeHandleArrowFontSize;
                text.enableAutoSizing = false;
            }
        }


        private void ResetSplits()
        {
            Plugin.ResetSplitsForCurrentLevel(true);
            if (splitsVisible)
                ToggleSplitsList();
        }

        private void ToggleSplitsList()
        {
            if (splitsPanel == null)
                return;

            splitsVisible = !splitsVisible;
            splitsPanel.SetActive(splitsVisible);

            if (splitsVisible)
                RefreshSplits();
        }

        internal void RefreshSplits()
        {
            if (splitsContent == null)
                return;

            for (int i = splitsContent.childCount - 1; i >= 0; i--)
                Destroy(splitsContent.GetChild(i).gameObject);

            string levelName = Plugin.fullLevelName;

            if (string.IsNullOrEmpty(levelName))
                return;

            var replay = SplitRecorder.LoadBestSplits(levelName);
            if (replay == null)
                return;

            var splits = replay.splits;

            foreach (var split in splits)
            {
                CreateSplitRow(
                    splitsContent,
                    split
                );
            }
        }


        private void CreateSplitRow(
            Transform parent,
            EditorSplit split
            )
        {
            GameObject row = new GameObject(
                $"SplitRow_CP{split.index}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement)
            );
            row.transform.SetParent(parent, false);

            // --- Layout control ---
            LayoutElement layoutElement = row.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = SplitRowHeight;
            layoutElement.minHeight = SplitRowHeight;
            layoutElement.flexibleHeight = SplitRowFlexibleHeight;

            Image bg = row.GetComponent<Image>();
            bg.color = SplitRowBackgroundColor;

            var sprite = GetRoundedButtonSprite();
            if (sprite != null)
            {
                bg.sprite = sprite;
                bg.type = Image.Type.Sliced;
            }

            Button btn = row.GetComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            btn.colors = GetRowColors();

            var nav = btn.navigation;
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;

            btn.onClick.AddListener(() => OnSplitRowClicked(split));

            // --- Content ---
            GameObject content = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup)
            );
            content.transform.SetParent(row.transform, false);

            RectTransform contentRT = content.GetComponent<RectTransform>();
            contentRT.anchorMin = Vector2.zero;
            contentRT.anchorMax = Vector2.one;
            contentRT.offsetMin = SplitRowContentOffsetMin;
            contentRT.offsetMax = SplitRowContentOffsetMax;

            var hLayout = content.GetComponent<HorizontalLayoutGroup>();
            hLayout.spacing = SplitRowContentSpacing;
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.childForceExpandHeight = true;
            hLayout.childForceExpandWidth = true;

            CreateSplitText(
                content.transform,
                split.index != 0 ? $"CP{split.index}" : "FIN",
                0.25f,
                TMPro.TextAlignmentOptions.Left
            );

            CreateSplitText(
                content.transform,
                FormatTime(split.time),
                0.45f,
                TMPro.TextAlignmentOptions.Center
            );

            CreateSplitText(
                content.transform,
                split.velocity.ToString("N2"),
                0.3f,
                TMPro.TextAlignmentOptions.Right
            );
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

            Vector3 size;
            if (bounds == null)
                size = Vector3.one * DefaultBoundsSize;
            else
                size = bounds.size;

            // ---- Dynamic Offsets ----
            float cameraBackOffset = Mathf.Min(Mathf.Max(size.x, size.z) * CameraBackOffsetScale, CameraBackOffsetMax);
            float cameraHeightOffset = CameraHeightOffset;

            Vector3 planeDir = Vector3.ProjectOnPlane(planeOrientation, Vector3.up).normalized;

            if (planeDir.sqrMagnitude < MinPlaneDirectionSqrMagnitude)
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

        private ColorBlock GetRowColors()
        {
            return new ColorBlock
            {
                normalColor = RowNormalColor,
                highlightedColor = RowHighlightedColor,
                pressedColor = RowPressedColor,
                selectedColor = RowSelectedColor,
                disabledColor = RowDisabledColor,
                colorMultiplier = RowColorMultiplier,
                fadeDuration = RowFadeDuration
            };
        }


        private void CreateSplitText(
            Transform parent,
            string value,
            float widthRatio,
            TMPro.TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(
                "Text",
                typeof(RectTransform),
                typeof(TMPro.TextMeshProUGUI),
                typeof(LayoutElement)
            );
            go.transform.SetParent(parent, false);

            var text = go.GetComponent<TMPro.TextMeshProUGUI>();
            text.text = value;
            text.alignment = alignment;
            text.color = Color.white;
            text.fontSize = 14;
            text.enableAutoSizing = true;
            text.fontSizeMin = 1;

            var font = GetEditorFont();
            if (font != null)
                text.font = font;

            text.fontSharedMaterial.EnableKeyword("UNDERLAY_ON");
            text.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.7f);
            text.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.5f);
            text.fontMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, Color.black);

            text.fontSharedMaterial.EnableKeyword("OUTLINE_ON");
            text.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.05f);
            text.fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);

            LayoutElement layout = go.GetComponent<LayoutElement>();
            layout.flexibleWidth = widthRatio;
            layout.minWidth = 0;
            layout.preferredWidth = 0;
        }


        private string FormatTime(float seconds)
        {
            TimeSpan t = TimeSpan.FromSeconds(seconds);
            return $"{t.Minutes:00}:{t.Seconds:00}.{t.Milliseconds:000}";
        }


        private Button CreateButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color backgroundColor,
            UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button)
            );
            go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(ButtonsPad, ButtonsPad);
            rt.offsetMax = new Vector2(-ButtonsPad, -(ButtonPanelHeaderHeight + 2 * ButtonPanelHeaderPad));

            Image img = go.GetComponent<Image>();
            img.color = backgroundColor;

            var sprite = GetRoundedButtonSprite();
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = RoundedSpritePPUMultiplier;
            }


            CreateTMPLabel(go.transform, label);

            Button btn = go.GetComponent<Button>();
            if (onClick != null)
                btn.onClick.AddListener(onClick);

            return btn;
        }


        private TMP_FontAsset GetEditorFont()
        {
            if (cachedEditorFont != null)
                return cachedEditorFont;

            var font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
           .FirstOrDefault(f => f.name == "Code New Roman b SDF");

            if (font)
            {
                cachedEditorFont = font;
            }
            else
            {
                Plugin.logger.LogError("Font not found in loaded resources!");
            }

            return cachedEditorFont;
        }

        private Sprite GetRoundedButtonSprite()
        {
            if (cachedSprite != null)
                return cachedSprite;

            var button = Plugin.central.GetComponentInChildren<Button>(true);
            if (button != null)
            {
                var img = button.GetComponent<Image>();
                if (img != null)
                    cachedSprite = img.sprite;
            }
            return cachedSprite;
        }

        private TMPro.TextMeshProUGUI CreateTMPLabel(
            Transform parent,
            string textValue)
        {
            GameObject go = new GameObject(
                "Text",
                typeof(RectTransform),
                typeof(TMPro.TextMeshProUGUI)
            );
            go.transform.SetParent(parent, false);

            var text = go.GetComponent<TMPro.TextMeshProUGUI>();
            text.text = textValue;
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.color = Color.white;

            var font = GetEditorFont();
            if (font != null)
                text.font = font;
            text.enableAutoSizing = true;
            text.fontSizeMin = 1;

            text.fontSharedMaterial.EnableKeyword("UNDERLAY_ON");
            text.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.7f);
            text.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.5f);
            text.fontMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, Color.black);

            text.fontSharedMaterial.EnableKeyword("OUTLINE_ON");
            text.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.05f);
            text.fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);

            RectTransform rt = text.rectTransform;
            rt.anchorMin = FullStretchAnchorMin;
            rt.anchorMax = FullStretchAnchorMax;
            rt.offsetMin = ZeroOffset;
            rt.offsetMax = ZeroOffset;

            return text;
        }

        internal void CreateSplitsPanel(Transform modRoot)
        {
            Transform existingPanel = modRoot.Find("SplitsPanel");
            if (existingPanel != null)
            {
                splitsPanel = existingPanel.gameObject;
                EnsureHeaderBar(splitsPanel.transform, splitsPanel.GetComponent<RectTransform>(), SplitsPanelHeaderColor, SplitsPanelMinWidth, SplitsPanelMinHeight, SplitsPanelHeaderHeight, SplitsPanelHeaderPad);
                return;
            }


            splitsPanel = new GameObject(
                "SplitsPanel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            splitsPanel.transform.SetParent(modRoot, false);
            AddInputBlocker(splitsPanel);

            RectTransform rt = splitsPanel.GetComponent<RectTransform>();
            rt.anchorMin = SplitsPanelAnchorMin;
            rt.anchorMax = SplitsPanelAnchorMax;
            rt.offsetMin = SplitsPanelOffsetMin;
            rt.offsetMax = SplitsPanelOffsetMax;

            Image img = splitsPanel.GetComponent<Image>();
            img.color = SplitsPanelColor;

            var sprite = GetRoundedButtonSprite();
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = RoundedSpritePPUMultiplier;
            }

            EnsureHeaderBar(splitsPanel.transform, splitsPanel.GetComponent<RectTransform>(), SplitsPanelHeaderColor, SplitsPanelMinWidth, SplitsPanelMinHeight, SplitsPanelHeaderHeight, SplitsPanelHeaderPad);

            // --- Scroll View ---
            GameObject scrollView = new GameObject(
                "ScrollView",
                typeof(RectTransform),
                typeof(ScrollRect)
            );
            scrollView.transform.SetParent(splitsPanel.transform, false);

            RectTransform scrollRT = scrollView.GetComponent<RectTransform>();
            scrollRT.anchorMin = FullStretchAnchorMin;
            scrollRT.anchorMax = FullStretchAnchorMax;
            scrollRT.offsetMin = ScrollViewOffsetMin;
            scrollRT.offsetMax = new Vector2(-ScrollViewPadding, -(SplitsPanelHeaderHeight + 2 * SplitsPanelHeaderPad));

            ScrollRect scrollRect = scrollView.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = ScrollSensitivity;

            // Viewport
            GameObject viewport = new GameObject(
                "Viewport",
                typeof(RectTransform),
                typeof(RectMask2D),
                typeof(Image)
            );
            viewport.transform.SetParent(scrollView.transform, false);

            RectTransform viewportRT = viewport.GetComponent<RectTransform>();
            viewportRT.anchorMin = FullStretchAnchorMin;
            viewportRT.anchorMax = FullStretchAnchorMax;
            viewportRT.offsetMin = ZeroOffset;
            viewportRT.offsetMax = ZeroOffset;

            Image vpImg = viewport.GetComponent<Image>();
            vpImg.color = ViewportMaskColor;

            scrollRect.viewport = viewportRT;

            // Content
            GameObject content = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter)
            );
            content.transform.SetParent(viewport.transform, false);

            RectTransform contentRT = content.GetComponent<RectTransform>();
            contentRT.anchorMin = ContentTopStretchAnchorMin;
            contentRT.anchorMax = ContentTopStretchAnchorMax;
            contentRT.pivot = TopCenterPivot;
            contentRT.offsetMin = ZeroOffset;
            contentRT.offsetMax = ZeroOffset;
            contentRT.anchoredPosition = ZeroOffset;

            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            layout.spacing = SplitsContentSpacing;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;

            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRT;

            splitsContent = content.transform;


            splitsPanel.SetActive(false);
        }


        private void OnDestroy()
        {
            if (buttonsPanel != null)
                Destroy(buttonsPanel);
            if (splitsPanel != null)
                Destroy(splitsPanel);

        }

        private void AddInputBlocker(GameObject target)
        {
            if (target.GetComponent<EditorSplitsUIInputBlocker>() == null)
                target.AddComponent<EditorSplitsUIInputBlocker>();
        }
    }
}
