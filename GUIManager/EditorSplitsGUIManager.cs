using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
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

        internal void Initialize()
        {
            if (!CreateModRoot(out Transform modRoot))
                return;

            CreateEditorSplitsUI(modRoot);
            CreateSplitsPanel(modRoot);
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

            modRoot = gameView.Find("EditorSplits_UI");
            if (modRoot == null)
            {
                GameObject go = new GameObject(
                    "EditorSplits_UI",
                    typeof(RectTransform)
                );
                go.transform.SetParent(gameView, false);
                modRoot = go.transform;

                RectTransform rt = modRoot.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0.25f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.offsetMin = new Vector2(0, 0);
                rt.offsetMax = new Vector2(0, 0);
            }


            return true;
        }

        internal void CreateEditorSplitsUI(Transform modRoot)
        {
            if (modRoot.Find("ButtonPanel") != null)
                return;

            buttonsPanel = new GameObject(
                "ButtonPanel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            buttonsPanel.transform.SetParent(modRoot, false);

            RectTransform panelRT = buttonsPanel.GetComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.30f, 0.88f);
            panelRT.anchorMax = new Vector2(0.70f, 0.97f);
            panelRT.pivot = new Vector2(0.5f, 0.5f);
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;

            Image img = buttonsPanel.GetComponent<Image>();
            img.color =
                new Color(1f, 0.55f, 0.04f, 1f);
            var sprite = GetRoundedButtonSprite();
            if (sprite != null)
                img.sprite = sprite;

            // --- Buttons ---
            CreateButton(
                buttonsPanel.transform,
                "SplitsButton",
                "Splits",
                new Vector2(0.05f, 0.1f),
                new Vector2(0.45f, 0.9f),
                new Color(1f, 0.75f, 0.39f, 1f),
                ToggleSplitsList
            );

            CreateButton(
                buttonsPanel.transform,
                "ResetButton",
                "Reset",
                new Vector2(0.55f, 0.1f),
                new Vector2(0.95f, 0.9f),
                new Color(0f, 0.54f, 0.82f, 1f),
                Plugin.ResetSplitsForCurrentLevel
            );
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

        private void RefreshSplits()
        {
            if (splitsContent == null)
                return;

            for (int i = splitsContent.childCount - 1; i >= 0; i--)
                Destroy(splitsContent.GetChild(i).gameObject);

            var dummySplits = new[]
            {
                (time: 6.478f, speed: 57),
                (time: 10.547f, speed: 98),
                (time: 14.231f, speed: 112),
            };

            for (int i = 0; i < dummySplits.Length; i++)
            {
                CreateSplitRow(
                    splitsContent,
                    i + 1,
                    dummySplits[i].time,
                    dummySplits[i].speed
                );
            }
        }


        private void CreateSplitRow(
            Transform parent,
            int cpIndex,
            float timeSeconds,
            int speed)
        {
            GameObject row = new GameObject(
                $"SplitRow_CP{cpIndex}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement)
            );
            row.transform.SetParent(parent, false);

            // --- Layout control ---
            LayoutElement layoutElement = row.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = 28f;
            layoutElement.minHeight = 28f;
            layoutElement.flexibleHeight = 0f;

            Image bg = row.GetComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.5f);

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



            btn.onClick.AddListener(() => OnSplitRowClicked(cpIndex));

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
            contentRT.offsetMin = new Vector2(8, 2);
            contentRT.offsetMax = new Vector2(-8, -2);

            var hLayout = content.GetComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 8;
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.childForceExpandHeight = true;
            hLayout.childForceExpandWidth = true;

            CreateSplitText(
                content.transform,
                $"CP{cpIndex}",
                0.25f,
                TMPro.TextAlignmentOptions.Left
            );

            CreateSplitText(
                content.transform,
                FormatTime(timeSeconds),
                0.45f,
                TMPro.TextAlignmentOptions.Center
            );

            CreateSplitText(
                content.transform,
                speed.ToString(),
                0.3f,
                TMPro.TextAlignmentOptions.Right
            );

        }


        private void OnSplitRowClicked(int cpIndex)
        {
            Plugin.logger.LogInfo($"Clicked CP{cpIndex} (camera jump not implemented yet)");
        }

        private ColorBlock GetRowColors()
        {
            return new ColorBlock
            {
                normalColor = new Color(0f, 0f, 0f, 1f),
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
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = go.GetComponent<Image>();
            img.color = backgroundColor;

            var sprite = GetRoundedButtonSprite();
            if (sprite != null)
                img.sprite = sprite;

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
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return text;
        }

        internal void CreateSplitsPanel(Transform modRoot)
        {
            if (modRoot.Find("SplitsPanel") != null)
                return;

            splitsPanel = new GameObject(
                "SplitsPanel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            splitsPanel.transform.SetParent(modRoot, false);

            RectTransform rt = splitsPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.05f, 0.05f);
            rt.anchorMax = new Vector2(0.95f, 0.85f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = splitsPanel.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.2f);

            var sprite = GetRoundedButtonSprite();
            if (sprite != null)
                img.sprite = sprite;

            // --- Content root ---
            GameObject content = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup)
            );
            content.transform.SetParent(splitsPanel.transform, false);

            RectTransform contentRT = content.GetComponent<RectTransform>();
            contentRT.anchorMin = Vector2.zero;
            contentRT.anchorMax = Vector2.one;
            contentRT.offsetMin = new Vector2(10, 10);
            contentRT.offsetMax = new Vector2(-10, -10);

            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 6;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;


            splitsContent = content.transform;

            splitsPanel.SetActive(false);
        }
    }
}
