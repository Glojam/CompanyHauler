using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using CompanyHauler.Compatibility;
using CompanyHauler.Networking;
using HarmonyLib;

namespace CompanyHauler;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("scandal.scandalstweaks", BepInDependency.DependencyFlags.HardDependency)]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony? Harmony { get; set; }

    public void Awake()
    {
        Logger = base.Logger;
        Instance = this;

        HaulerConfig.InitConfig();

        Patch();
        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }

    internal static void Patch()
    {
        Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

        Logger.LogDebug("Patching...");

        Harmony.PatchAll();

        if (IsModPresent("NoteBoxz.LethalMin")) LethalMinCompatibility.PatchAllCompatibilityMethods(Harmony);
        if (IsModPresent("ImmersiveVisor")) ImmersiveVisorCompatibility.PatchAllCompatibilityMethods(Harmony);

        Logger.LogDebug("Finished patching!");
    }

    internal static void Unpatch()
    {
        Logger.LogDebug("Unpatching...");

        Harmony?.UnpatchSelf();

        Logger.LogDebug("Finished unpatching!");
    }

    internal static bool IsModPresent(string name)
    {
        return Chainloader.PluginInfos.ContainsKey(name);
    }
}
