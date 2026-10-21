using BepInEx;
using BepInEx.Logging;
using CompanyHauler.Patches;
using CompanyHauler.Utils;
using HarmonyLib;
using System.Runtime.CompilerServices;

namespace CompanyHauler;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(CompatibilityUtils.GUID_LETHAL_LEVEL_LOADER, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(CompatibilityUtils.GUID_SCANDALLIB, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(CompatibilityUtils.GUID_SCANDALS_TWEAKS, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(CompatibilityUtils.GUID_INPUT_UTILS, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(CompatibilityUtils.GUID_LETHALELEMENTS, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(CompatibilityUtils.GUID_LETHALMIN, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(CompatibilityUtils.GUID_FIRST_PERSON_VIEW, BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony? Harmony { get; set; }

    internal static VehicleControls VehicleControlsInstance = null!;

    internal static bool initialized = false;

    public void Awake()
    {
        if (initialized)
        {
            return;
        }
        initialized = true;
        Logger = base.Logger;
        Instance = this;

        VehicleControlsInstance = new VehicleControls();
        UserConfig.InitConfig();

        Patch();
        LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }

    internal static void Patch()
    {
        Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

        LogDebug("Patching...");

        CompatibilityUtils.Init(Harmony);
        Harmony.PatchAll();

        LogDebug("Finished patching!");
    }

    internal static void Unpatch()
    {
        LogDebug("Unpatching...");

        Harmony?.UnpatchSelf();

        LogDebug("Finished unpatching!");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void LogMessage(string messageLog)
    {
        Logger.LogMessage(messageLog);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void LogDebug(string debugLog)
    {
        Logger.LogDebug(debugLog);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void LogInfo(string infoLog)
    {
        Logger.LogInfo(infoLog);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void LogWarning(string warningLog)
    {
        Logger.LogWarning(warningLog);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void LogError(string errorLog)
    {
        Logger.LogError(errorLog);
    }
}
