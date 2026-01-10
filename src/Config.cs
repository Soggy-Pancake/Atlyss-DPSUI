using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Nessie.ATLYSS.EasySettings;
using BepInEx.Configuration;
using BepInEx.Bootstrap;
using UnityEngine;

namespace Atlyss_DPSUI;
#pragma warning disable CS8618 

internal static class DPSUI_Config {
    internal static ConfigFile config;

    internal static ConfigEntry<KeyCode> togglePartyUIBind;
    internal static ConfigEntry<KeyCode> toggleLocalUIBind;
    internal static ConfigEntry<KeyCode> switchPartyUITypeBind;

    internal static ConfigEntry<float> transitionTime;
    internal static ConfigEntry<float> damageHoldTime;

    internal static ConfigEntry<bool> keepDamageUntilPause;
    internal static ConfigEntry<bool> showFullDungeonDamage;
    internal static ConfigEntry<bool> showLocalUI;
    internal static ConfigEntry<bool> showPartyUI;

    internal static ConfigEntry<bool> speedyBoiMode;

    internal static ConfigEntry<string> backgroundImage;
    internal static ConfigEntry<string> textFont;

    internal static ConfigEntry<float> clientUpdateRate;

    public static void init(ConfigFile _config) {
        config = _config;
        togglePartyUIBind = config.Bind("KeyBinds", "partyUIToggleKey", KeyCode.LeftBracket);
        switchPartyUITypeBind = config.Bind("KeyBinds", "partyUITypeKey", KeyCode.RightBracket);
        toggleLocalUIBind = config.Bind("KeyBinds", "localToggleKey", KeyCode.Comma);

        ConfigDescription transitionDescr = new ConfigDescription("Time it takes to slide in party ui in seconds", new AcceptableValueRange<float>(0f, 1f));
        transitionTime = config.Bind("General", "transitionTime", 0.25f, transitionDescr);

        ConfigDescription damageDesc = new ConfigDescription("Total time in seconds to keep track of damage for when calculating local DPS (Becomes time gap required to reset if keepDamageUntilPause is true)", new AcceptableValueRange<float>(1f, 120f));
        damageHoldTime = config.Bind("General", "damageHoldTime", 10f, damageDesc);

        showLocalUI = config.Bind("General", "showLocalUI", true);
        showPartyUI = config.Bind("General", "showPartyUI", true);
        keepDamageUntilPause = config.Bind("General", "keepDamageUntilPause", false, "Keep track of all of the damage since you started attacking until you stop instead of the last x seconds");
        showFullDungeonDamage = config.Bind("General", "showFullDungeonDamage", true, "Show damage totals while in the dungeon instead of just the boss");
        speedyBoiMode = config.Bind("General", "speedyBoiMode", false, "Display dungeon split times.");
        backgroundImage = config.Bind("General", "backgroundImage", "_graphic/_ui/bk_06", "Background image to use for the party damage UI");
        textFont = config.Bind("General", "textFont", "", "Path of the font to use");

        // Host only
        ConfigDescription updateDesc = new ConfigDescription("Minimum time in seconds between updates to clients", new AcceptableValueRange<float>(0.1f, 5f));
        clientUpdateRate = config.Bind("Host", "clientUpdateRate", 1f, updateDesc);

        if (Chainloader.PluginInfos.ContainsKey("EasySettings")) {
            addEasySettings();
        } else {
            Plugin.logger.LogWarning("Soft dependency EasySettings not found!");
        }

        Plugin.logger.LogInfo("Config initalized!");
        DPSUI_GUI.userShowPartyUI = showPartyUI.Value;
        DPSUI_GUI.userShowLocalUI = showLocalUI.Value;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static void addEasySettings() {
        Settings.OnApplySettings.AddListener(() => { config.Save(); });
        Settings.OnInitialized.AddListener(ActuallyAdd);

        void ActuallyAdd() {
            SettingsTab modTab = Settings.ModTab;
            modTab.AddHeader("Atlyss DPSUI");
            modTab.AddToggle(showPartyUI);
            modTab.AddToggle(showLocalUI);
            modTab.AddToggle(speedyBoiMode);
            modTab.AddKeyButton(togglePartyUIBind);
            modTab.AddKeyButton(toggleLocalUIBind);
            modTab.AddKeyButton("Switch UI Mode", switchPartyUITypeBind);
            modTab.AddAdvancedSlider(transitionTime);
            modTab.AddAdvancedSlider(damageHoldTime, wholeNumbers: true);
            modTab.AddToggle(keepDamageUntilPause);
            modTab.AddToggle(showFullDungeonDamage);

            // Host Settings
            modTab.AddAdvancedSlider("[Host] Client update rate", clientUpdateRate);
        }
    }
}