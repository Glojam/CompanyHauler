using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;

namespace CompanyHauler;

internal class UserConfig
{
    // General
    internal static ConfigEntry<bool> haulerMirror = null!;

    // QOL
    internal static ConfigEntry<bool> RecenterWheel = null!;

    internal static void InitConfig()
    {
        ConfigFile config = Plugin.Instance.Config;
        config.SaveOnConfigSet = false;

        // General
        haulerMirror = config.Bind("General", "Enable Mirrors", true, "Enable rendering for the side mirrors? (may impact performance for low end hardware)");

        // QOL
        RecenterWheel = config.Bind("Quality Of Life", "Automatically Center Wheel", false, "Should the wheel be automatically re-centered?");

        ClearOrphanedEntries(config);
        config.Save();
        config.SaveOnConfigSet = true;
    }

    static void ClearOrphanedEntries(ConfigFile cfg)
    {
        PropertyInfo orphanedEntriesProp = AccessTools.Property(typeof(ConfigFile), "OrphanedEntries");
        var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp.GetValue(cfg);
        orphanedEntries.Clear();
    }
}