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

        // Shared constants
        private const float DefaultPivot = 0.5f;
        private const float FullAlpha = 1f;
        private const float TextAutoSizeMin = 1f;
        private const float TextUnderlayOffsetX = 0.7f;
        private const float TextUnderlayOffsetY = -0.5f;
        private const float TextOutlineWidth = 0.05f;
        private const float HeaderPadMultiplier = 2f;

        private static readonly Color SharedTextColor = Color.white;
        private static readonly Color SharedTextShadowColor = Color.black;
        private static readonly Color SplitsPanelHeaderColor = new Color(0.09f, 0.25f, 0.62f, 0.92f);
        private static readonly Color SplitsPanelColor = new Color(0f, 0f, 0f, 0.2f);
        private static readonly Color ResizeHandleColor = new Color(0f, 0f, 0f, 0.20f);
        private static readonly Color SplitRowColor = new Color(1f, 1f, 1f, 0.5f);
        private static readonly Color ViewportColor = new Color(1f, 1f, 1f, 0.01f);

        // Mod root
        private const float ModRootAnchorMinX = 0.375f;
        private const float ModRootAnchorMaxX = 0.625f;
        private const float ModRootAnchorMinY = 0.0f;
        private const float ModRootAnchorMaxY = 0.5f;

        // Button panel
        private const float ButtonPanelAnchorMinX = 0.30f;
        private const float ButtonPanelAnchorMaxX = 0.70f;
        private const float ButtonPanelAnchorMinY = 0.0f;
        private const float ButtonPanelAnchorMaxY = 0.15f;
        private const float ButtonPanelMinWidth = 110f;
        private const float ButtonPanelMinHeight = 40f;
        private const float ButtonPanelHeaderHeight = 15f;
        private const float ButtonPanelHeaderPad = 2f;
        private static readonly Color ButtonPanelColor = new Color(1f, 0.55f, 0.04f, FullAlpha); // Orange
        private static readonly Color ButtonPanelHeaderColor = new Color(1f, 0.75f, 0.39f, FullAlpha); // Blue

        // Primary buttons
        private const float ButtonsPad = 3f;

        // Splits button
        private const float SplitsButtonAnchorMinX = 0.0f;
        private const float SplitsButtonAnchorMaxX = 0.5f;
        private const float SplitsButtonAnchorMinY = 0f;
        private const float SplitsButtonAnchorMaxY = 1f;
        private static readonly Color SplitsButtonColor = new Color(1f, 0.75f, 0.39f, FullAlpha); // Lighter Orange
        // Reset button
        private const float ResetButtonAnchorMinX = 0.5f;
        private const float ResetButtonAnchorMaxX = 1f;
        private const float ResetButtonAnchorMinY = 0f;
        private const float ResetButtonAnchorMaxY = 1f;
        private static readonly Color ResetButtonColor = new Color(0f, 0.54f, 0.82f, FullAlpha); // Lighter Blue

        // Header bar
        private const float HeaderBarAnchorMinX = 0.02f;
        private const float HeaderBarAnchorMaxX = 0.98f;



        private const float SplitsPanelMinWidth = 280f;
        private const float SplitsPanelMinHeight = 110f;
        private const float SplitsPanelHeaderHeight = 24f;
                
        private const float SplitsPanelHeaderPad = 4f;


        // Split row
        private const float SplitRowHeight = 28f;
        private const float SplitRowContentPaddingX = 8f;
        private const float SplitRowContentPaddingY = 2f;
        private const float SplitRowContentSpacing = 8f;
        private const float SplitRowTextFontSize = 14f;

        // Resize handle
        private const float ResizeHandleAnchorMinX = 0.90f;
        private const float ResizeHandleAnchorMaxX = 0.99f;
        private const float ResizeHandleAnchorMinY = 0.1f;
        private const float ResizeHandleAnchorMaxY = 0.9f;
        private const float ResizeHandleArrowFontSize = 18f;

        // Splits panel
        private const float SplitsPanelAnchorMinY = 0.17f;
        private const float SplitsPanelScrollPadding = 10f;

        // Camera offsets
        private const float CameraDefaultBoundsSize = 5f;
        private const float CameraBackOffsetScale = 0.7f;
        private const float CameraBackOffsetMax = 500f;
        private const float CameraHeightOffset = 5f;
        private const float CameraDirectionThreshold = 0.001f;

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
                rt.anchorMin = new Vector2(ModRootAnchorMinX, ModRootAnchorMinY);
                rt.anchorMax = new Vector2(ModRootAnchorMaxX, ModRootAnchorMaxY);
                rt.pivot = new Vector2(DefaultPivot, DefaultPivot);
                rt.offsetMin = new Vector2(0, 0);
                rt.offsetMax = new Vector2(0, 0);

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
            panelRT.anchorMin = new Vector2(ButtonPanelAnchorMinX, ButtonPanelAnchorMinY);
            panelRT.anchorMax = new Vector2(ButtonPanelAnchorMaxX, ButtonPanelAnchorMaxY);
            panelRT.pivot = new Vector2(0.5f, 0.5f);
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;

            Image img = buttonsPanel.GetComponent<Image>();
            img.color = ButtonPanelColor;
            var sprite = GetRoundedButtonSprite();
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 1f;
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
                    new Vector2(SplitsButtonAnchorMinX, SplitsButtonAnchorMinY),
                    new Vector2(SplitsButtonAnchorMaxX, SplitsButtonAnchorMaxY),
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
                    new Vector2(ResetButtonAnchorMinX, ResetButtonAnchorMinY),
                    new Vector2(ResetButtonAnchorMaxX, ResetButtonAnchorMaxY),
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
            headerRT.anchorMin = new Vector2(HeaderBarAnchorMinX, 1);
            headerRT.anchorMax = new Vector2(HeaderBarAnchorMaxX, 1);
            headerRT.pivot = new Vector2(DefaultPivot, FullAlpha);
            headerRT.sizeDelta = new Vector2(0f, headerHeight);
            headerRT.anchoredPosition = new Vector2(0f, -headerPad);

            var headerImage = headerBar.GetComponent<Image>();
            headerImage.color = color;
            var sprite = GetRoundedButtonSprite();
            if (sprite != null)
            {
                headerImage.sprite = sprite;
                headerImage.type = Image.Type.Sliced;
                headerImage.pixelsPerUnitMultiplier = 1f;
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
            handleRT.anchorMin = new Vector2(ResizeHandleAnchorMinX, ResizeHandleAnchorMinY);
            handleRT.anchorMax = new Vector2(ResizeHandleAnchorMaxX, ResizeHandleAnchorMaxY);
            handleRT.pivot = new Vector2(DefaultPivot, DefaultPivot);
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
            layoutElement.flexibleHeight = 0f;

            Image bg = row.GetComponent<Image>();
            bg.color = SplitRowColor;

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
            contentRT.offsetMin = new Vector2(SplitRowContentPaddingX, SplitRowContentPaddingY);
            contentRT.offsetMax = new Vector2(-SplitRowContentPaddingX, -SplitRowContentPaddingY);

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
                size = Vector3.one * CameraDefaultBoundsSize;
            else
                size = bounds.size;

            // ---- Dynamic Offsets ----
            float cameraBackOffset = Mathf.Min(Mathf.Max(size.x, size.z) * CameraBackOffsetScale, CameraBackOffsetMax);
            float cameraHeightOffset = CameraHeightOffset;

            Vector3 planeDir = Vector3.ProjectOnPlane(planeOrientation, Vector3.up).normalized;

            if (planeDir.sqrMagnitude < CameraDirectionThreshold)
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
                normalColor = new Color(0f, 0f, 0f, FullAlpha),
                highlightedColor = new Color(1f, 1f, 1f, 0.6f),
                pressedColor = new Color(1f, 1f, 1f, 0.8f),
                selectedColor = new Color(1f, 1f, 1f, 0.6f),
                disabledColor = new Color(1f, 1f, 1f, 0.02f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
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
            text.color = SharedTextColor;
            text.fontSize = SplitRowTextFontSize;
            text.enableAutoSizing = true;
            text.fontSizeMin = TextAutoSizeMin;

            var font = GetEditorFont();
            if (font != null)
                text.font = font;

            text.fontSharedMaterial.EnableKeyword("UNDERLAY_ON");
            text.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, TextUnderlayOffsetX);
            text.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, TextUnderlayOffsetY);
            text.fontMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, SharedTextShadowColor);

            text.fontSharedMaterial.EnableKeyword("OUTLINE_ON");
            text.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, TextOutlineWidth);
            text.fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, SharedTextShadowColor);

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
            //rt.anchorMin = anchorMin;
            //rt.anchorMax = anchorMax;

            //rt.offsetMin = new Vector2(-10f, 0);
            //rt.offsetMax = new Vector2(10f, - (ButtonPanelHeaderHeight + HeaderTopMargin + ButtonPanelHeaderBottomGap));

            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(ButtonsPad, ButtonsPad);
            rt.offsetMax = new Vector2(-ButtonsPad, -(ButtonPanelHeaderHeight + HeaderPadMultiplier * ButtonPanelHeaderPad));

            Image img = go.GetComponent<Image>();
            img.color = backgroundColor;

            var sprite = GetRoundedButtonSprite();
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 1f;
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
            text.color = SharedTextColor;

            var font = GetEditorFont();
            if (font != null)
                text.font = font;
            text.enableAutoSizing = true;
            text.fontSizeMin = TextAutoSizeMin;

            text.fontSharedMaterial.EnableKeyword("UNDERLAY_ON");
            text.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, TextUnderlayOffsetX);
            text.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, TextUnderlayOffsetY);
            text.fontMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, SharedTextShadowColor);

            text.fontSharedMaterial.EnableKeyword("OUTLINE_ON");
            text.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, TextOutlineWidth);
            text.fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, SharedTextShadowColor);

            RectTransform rt = text.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

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
            rt.anchorMin = new Vector2(0f, SplitsPanelAnchorMinY);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = splitsPanel.GetComponent<Image>();
            img.color = SplitsPanelColor;

            var sprite = GetRoundedButtonSprite();
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 1f;
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
            scrollRT.anchorMin = Vector2.zero;
            scrollRT.anchorMax = Vector2.one;
            scrollRT.offsetMin = new Vector2(SplitsPanelScrollPadding, SplitsPanelScrollPadding);
            scrollRT.offsetMax = new Vector2(-SplitsPanelScrollPadding, -(SplitsPanelHeaderHeight + HeaderPadMultiplier * SplitsPanelHeaderPad));

            ScrollRect scrollRect = scrollView.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 20f;

            // Viewport
            GameObject viewport = new GameObject(
                "Viewport",
                typeof(RectTransform),
                typeof(RectMask2D),
                typeof(Image)
            );
            viewport.transform.SetParent(scrollView.transform, false);

            RectTransform viewportRT = viewport.GetComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = Vector2.zero;
            viewportRT.offsetMax = Vector2.zero;

            Image vpImg = viewport.GetComponent<Image>();
            vpImg.color = ViewportColor;

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
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(DefaultPivot, FullAlpha);
            contentRT.offsetMin = Vector2.zero;
            contentRT.offsetMax = Vector2.zero;
            contentRT.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 6;
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
