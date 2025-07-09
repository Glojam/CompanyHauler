using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;

namespace CompanyHauler;

class HaulerConfig
{
    public readonly ConfigEntry<bool> haulerMirror;

    public HaulerConfig(ConfigFile cfg)
    {
        cfg.SaveOnConfigSet = false;

        haulerMirror = cfg.Bind(
            "General",
            "MirrorOn",
            true,
            "Enable rendering for the side mirrors (may impact performance for low end hardware)"
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