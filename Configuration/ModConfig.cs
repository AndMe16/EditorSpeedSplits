using System;
using BepInEx.Configuration;
using UnityEngine;

namespace EditorSpeedSplits.Configuration;

public class ModConfig : MonoBehaviour
{
    private static ConfigEntry<bool> _resetSplits;
    public static ConfigEntry<KeyCode> ResetSplitsKey;
    public static ConfigEntry<bool> CpPBSpeed;

    private void OnDestroy()
    {
        _resetSplits.SettingChanged -= OnResetSplits;
    }

    public static void Initialize(ConfigFile config)
    {
        _resetSplits = config.Bind("1. Gameplay", "1.1 Reset Splits", true,
            "[button] Reset the current level's splits");

        ResetSplitsKey = config.Bind("2. Bindings", "2.1 Reset Splits Key", KeyCode.None,
            "Key to reset the current level's splits");

        CpPBSpeed = config.Bind("1. Gameplay", "1.2 Speed for CP PBs", false,
            "Use speed instead of time for checkpoint personal bests");

        _resetSplits.SettingChanged += OnResetSplits;
    }

    private static void OnResetSplits(object sender, EventArgs e)
    {
        Plugin.ResetSplitsForCurrentLevel(true);
    }
}