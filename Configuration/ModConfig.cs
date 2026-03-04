using BepInEx.Configuration;
using UnityEngine;

namespace EditorSpeedSplits.Configuration
{
    public class ModConfig : MonoBehaviour
    {
        public static ConfigEntry<bool> ResetSplits;
        public static ConfigEntry<KeyCode> ResetSplitsKey;

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
            Plugin.ResetSplitsForCurrentLevel(true);
        }

        private void OnDestroy()
        {
            ResetSplits.SettingChanged -= OnResetSplits;
        }
    }
}
