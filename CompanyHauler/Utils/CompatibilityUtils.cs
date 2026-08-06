using BepInEx.Bootstrap;
using CompanyHauler.Compatibility;
using HarmonyLib;

namespace CompanyHauler.Utils;

internal static class CompatibilityUtils
{
    // hard deps
    internal const string GUID_LETHAL_LEVEL_LOADER = "imabatby.lethallevelloader";
    internal const string GUID_SCANDALLIB = "scandal.scandallib";
    internal const string GUID_SCANDALS_TWEAKS = "scandal.scandalstweaks";
    internal const string GUID_INPUT_UTILS = "com.rune580.LethalCompanyInputUtils";

    // soft deps
    internal const string GUID_LETHALELEMENTS = "voxx.LethalElementsPlugin";
    internal const string GUID_LETHALMIN = "NoteBoxz.LethalMin";
    internal const string GUID_IMMERSIVE_VISOR = "ImmersiveVisor";
    internal const string GUID_FIRST_PERSON_VIEW = "com.seeya.firstpersonview";

    internal static bool INSTALLED_LETHALELEMENTS;
    internal static bool INSTALLED_LETHALMIN;
    internal static bool INSTALLED_IMMERSIVE_VISOR;
    internal static bool INSTALLED_FIRST_PERSON_VIEW;

    internal static void Init(Harmony harmony)
    {
        if (Chainloader.PluginInfos.ContainsKey(GUID_LETHALELEMENTS))
        {
            INSTALLED_LETHALELEMENTS = true;
            harmony.PatchAll(type: typeof(LethalElementsCompatibility));
            Plugin.LogDebug("CROSS-COMPATIBILITY - LethalElements detected");
        }
        if (Chainloader.PluginInfos.ContainsKey(GUID_LETHALMIN))
        {
            INSTALLED_LETHALMIN = true;
            harmony.PatchAll(type: typeof(LethalMinCompatibility));
            Plugin.LogDebug("CROSS-COMPATIBILITY - LethalMin detected");
        }
        if (Chainloader.PluginInfos.ContainsKey(GUID_IMMERSIVE_VISOR))
        {
            INSTALLED_IMMERSIVE_VISOR = true;
            harmony.PatchAll(type: typeof(ImmersiveVisorCompatibility));
            Plugin.LogDebug("CROSS-COMPATIBILITY - ImmersiveVisor detected");
        }
        if (Chainloader.PluginInfos.ContainsKey(GUID_FIRST_PERSON_VIEW))
        {
            INSTALLED_FIRST_PERSON_VIEW = true;
            harmony.PatchAll(type: typeof(FirstPersonViewCompatibility));
            Plugin.LogDebug("CROSS-COMPATIBILITY - FirstPersonView detected");
        }
    }
}