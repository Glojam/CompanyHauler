using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;

namespace CompanyHauler;

class HaulerConfig
{
    public readonly ConfigEntry<bool> haulerMirror;
    public readonly ConfigEntry<int> haulerHealth;
    public readonly ConfigEntry<bool> haulerLean;
    public readonly ConfigEntry<bool> haulerAutoCenter;

    public HaulerConfig(ConfigFile cfg)
    {
        cfg.SaveOnConfigSet = false;

        haulerMirror = cfg.Bind(
            "General",
            "MirrorOn",
            true,
            "Enable rendering for the side mirrors (may impact performance for low end hardware)"
        );

        haulerHealth = cfg.Bind(
            "General",
            "Health",
            100,
            "Max health of the Hauler (default: 100). For reference, the Cruiser has a max health of 30."
        );

        haulerLean = cfg.Bind(
            "General",
            "AllowLean",
            true,
            "Allow leaning in the driver seat to see out the back window."
        );

        haulerAutoCenter = cfg.Bind(
            "General",
            "AutoCenter",
            false,
            "Automatically center the steering wheel"
        );

        ClearOrphanedEntries(cfg);
        cfg.Save();
        cfg.SaveOnConfigSet = true;
    }

    static void ClearOrphanedEntries(ConfigFile cfg)
    {
        PropertyInfo orphanedEntriesProp = AccessTools.Property(typeof(ConfigFile), "OrphanedEntries");
        var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp.GetValue(cfg);
        orphanedEntries.Clear();
    }
}