using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Imui.Controls;
using Imui.Core;
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZeepSDK.LevelEditor;
using ZeepSDK.Messaging;
using ZeepSDK.UI;

namespace EditorSpeedSplits
{
    [BepInPlugin("com.andme.editorspeedsplits", "EditorSpeedSplits", MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource logger;
        private Harmony harmony;

        public static Plugin Instance { get; private set; }

        internal static LEV_LevelEditorCentral central;

        internal static Transform canvas;

        internal static string fullLevelName = "";

        private GameObject buttonsPanel;
        private GameObject splitsPanel;
        private Transform splitsContent;
        private bool splitsVisible;


        private TMP_FontAsset cachedEditorFont;
        private Sprite cachedSprite;

        private EditorSplitsToolbarDrawer _toolbarDrawer;

        private void Awake()
        {
            Instance = this;
            logger = Logger;

            harmony = new Harmony("com.andme.editorspeedsplits");
            harmony.PatchAll();

            ModConfig.Initialize(Config);

            logger.LogInfo("Plugin com.andme.editorspeedsplits is loaded!");

            LevelEditorApi.EnteredLevelEditor += OnEnteredLevelEditor;

            _toolbarDrawer = new EditorSplitsToolbarDrawer();
            UIApi.AddToolbarDrawer(_toolbarDrawer);
        }

        private void OnEnteredLevelEditor()
        {
            central = FindObjectOfType<LEV_LevelEditorCentral>();
            if (central == null)
            {
                logger.LogWarning("Level Editor Central not found.");
                return;
            }

            Transform modRoot;
            bool rootCreated = CreateModRoot(out modRoot);
            if (!rootCreated)
            {
                logger.LogWarning("Failed to create mod root.");
                return;
            }

            CreateEditorSplitsUI(modRoot);
            CreateSplitsPanel(modRoot);

        }

        private static bool CreateModRoot(out Transform modRoot)
        {
            canvas = central.transform.Find("Canvas");

            if (canvas == null)
                {
                logger.LogWarning("Canvas not found");
                modRoot = null;
                return false;
            }

            Transform gameView = canvas.Find("GameView");

            if (gameView == null)
            {
                logger.LogWarning("GameView not found");
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

        private void CreateEditorSplitsUI(Transform modRoot)
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
                ResetSplitsForCurrentLevel
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
            logger.LogInfo($"Clicked CP{cpIndex} (camera jump not implemented yet)");
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
            text.enableAutoSizing = false;

            var font = GetEditorFont();
            if (font != null)
                text.font = font;

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

            var tmpText = central.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            if (tmpText != null)
                cachedEditorFont = tmpText.font;

            return cachedEditorFont;
        }

        private Sprite GetRoundedButtonSprite()
        {
            if (cachedSprite != null)
                return cachedSprite;

            var button = central.GetComponentInChildren<Button>(true);
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
                text.fontSizeMin = 5;

            RectTransform rt = text.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return text;
        }

        private void CreateSplitsPanel(Transform modRoot)
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


        internal static void ResetSplitsForCurrentLevel()
        {

            if (!LevelEditorApi.IsTestingLevel && !LevelEditorApi.IsInLevelEditor)
                return;

            if (LevelEditorApi.IsInLevelEditor && central != null)
            {
                if (central.input.inputLocked)
                    return;
            }

            string currentFullLevelName = fullLevelName;

            if (string.IsNullOrEmpty(currentFullLevelName))
            {
                logger.LogWarning("No level loaded in the editor to reset splits for.");
                return;
            }

            ReplayManager.Instance.Replays.Remove(fullLevelName);

            GameMaster gameMaster = FindObjectOfType<GameMaster>();

            gameMaster?.SetupPersonalBestAndMedals(0f, []);

            logger.LogInfo($"Splits reset for level {fullLevelName}");
            MessengerApi.Log("[EditorSpeedSplits] Splits Reset");
        }

        private void Update()
        {
            if (!Input.GetKeyDown(ModConfig.ResetSplitsKey.Value))
                return;

            // Input globally locked / paused
            if (Time.timeScale == 0f)
                return;

            // Player typing in an input field
            if (IsTypingInInputField())
                return;

            ResetSplitsForCurrentLevel();
        }

        private bool IsTypingInInputField()
        {
            if (EventSystem.current == null)
                return false;

            var selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null)
                return false;

            return selected.GetComponent<TMP_InputField>() != null;
        }

        private void OnDestroy()
        {
            UIApi.RemoveToolbarDrawer(_toolbarDrawer);
            LevelEditorApi.EnteredLevelEditor -= OnEnteredLevelEditor;
            harmony?.UnpatchSelf();
            harmony = null;
        }
    }


    // PATCH: GameMaster.ReloadBestTimes
    [HarmonyPatch(typeof(GameMaster), "ReloadBestTimes")]
    class GameMaster_ReloadBestTimes_Patch
    {
        [HarmonyPrefix]
        static bool Prefix(GameMaster __instance)
        {

            if (!__instance.GlobalLevel.IsTestLevel)
                return true;

            string currentFullLevelName = Plugin.fullLevelName;

            ReplayManager.ReplayInfo replay = null;

            if (!string.IsNullOrEmpty(currentFullLevelName))
            {
                replay = ReplayManager.Instance.GetReplay(currentFullLevelName);
                Plugin.logger.LogInfo($"ReloadBestTimes called for level {currentFullLevelName}");
            }
            
            if (replay == null)
            {
                __instance.SetupPersonalBestAndMedals(0f, []);
                Plugin.logger.LogInfo("No replay found, setting personal best to 0");
            }
            else
            {
                __instance.SetupPersonalBestAndMedals(replay.Time, WinCompare.CreateSplitTimeList(replay.Splits, replay.velocities));

                Plugin.logger.LogInfo($"Replay found, setting personal best to {replay.Time}");
            }
                

            return false;
        }
    }


    // PATCH: GameMaster.GetResults2
    [HarmonyPatch(typeof(GameMaster), "GetResults2")]
    class GameMaster_GetResults2_Patch
    {
        [HarmonyPostfix]
        static void Postfix(GameMaster __instance)
        {
            if (!__instance.GlobalLevel.IsTestLevel)
                return;

            if (__instance.manager.amountOfPlayers != 1)
                return;

            var result = __instance.playerResults[0];
            if (result == null)
                return;

            if (!__instance.currentLevelMode.DidWeGetMedal(
                    LevelModeBase.MedalType.Finished, result))
                return;

            if (result.time <= 0f)
                return;

            string currentFullLevelName = Plugin.fullLevelName;

            if (string.IsNullOrEmpty(currentFullLevelName))
                return;

            ReplayManager.Instance.AddReplay(
                currentFullLevelName,
                result.time,
                result.split_times
            );
        }
    }

    // PATCH: LEV_SaveLoad.ExternalLoad
    [HarmonyPatch(typeof(LEV_SaveLoad), "ExternalLoad")]
    class LEV_SaveLoad_ExternalLoad_Patch
    {
        [HarmonyPostfix]
        static void Postfix(string filePath, bool isTestLevel)
        {
            if (!LevelEditorApi.IsInLevelEditor)
                return;

            if (isTestLevel)
                return;

            Plugin.fullLevelName = Path.ChangeExtension(filePath,null);
            Plugin.logger.LogInfo($"Set fullLevelName to {Plugin.fullLevelName}");
        }
    }

    // PATCH: LEV_SaveLoad.ExternalSaveFile
    [HarmonyPatch(typeof(LEV_SaveLoad), "ExternalSaveFile")]
    class LEV_SaveLoad_ExternalSaveFile_Patch
    {
        [HarmonyPostfix]
        static void Postfix(LEV_SaveLoad __instance, bool isTestMap)
        {
            if (isTestMap)
                return;
            Plugin.fullLevelName = Path.Combine( __instance.GetFolderWeJustSavedInto().FullName, __instance.fileName.text);
            Plugin.logger.LogInfo($"Set fullLevelName to {Plugin.fullLevelName}");
        }
    }

    // PATCH: LEV_ReturnToMainMenu.ReturnToMainMenu
    [HarmonyPatch(typeof(LEV_ReturnToMainMenu), "ReturnToMainMenu")]
    class LEV_ReturnToMainMenu_ReturnToMainMenu_Patch
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            Plugin.fullLevelName = "";
            Plugin.logger.LogInfo("Cleared fullLevelName on return to main menu");
        }
    }


    public class EditorSplitsToolbarDrawer : IZeepToolbarDrawer
    {
        public string MenuTitle => "EditorSplits";

        public void DrawMenuItems(ImGui gui)
        {
            if (gui.Menu("Reset Splits"))
            {
                Plugin.ResetSplitsForCurrentLevel();
            }
        }
    }

    public class ModConfig : MonoBehaviour
    {
        public static ConfigEntry<bool> ResetSplits;
        public static ConfigEntry<KeyCode> ResetSplitsKey;


        // Constructor that takes a ConfigFile instance from the main class
        public static void Initialize(ConfigFile config)
        {
            ResetSplits = config.Bind("1. Gameplay", "1.1 Reset Splits", true,
                "[button] Reset the current level's splits");

            ResetSplitsKey = config.Bind("2. Bindings", "2.1 Reset Splits Key", KeyCode.None,
                "Key to reset the current level's splits");

            ResetSplits.SettingChanged += OnResetSplits;
        }

        private static void OnResetSplits(object sender, System.EventArgs e)
        {
            Plugin.ResetSplitsForCurrentLevel();
        }

        private void OnDestroy()
        {
            ResetSplits.SettingChanged -= OnResetSplits;
        }

    }



}
