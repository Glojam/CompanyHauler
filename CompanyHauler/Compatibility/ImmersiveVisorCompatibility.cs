using HarmonyLib;
using System.Runtime.CompilerServices;
using Woecust.ImmersiveVisor;
using CompanyHauler.Utils;
using CompanyHauler.Scripts;

namespace CompanyHauler.Compatibility;

/// <summary>
///  Available from BrutalCompanyMinus, licensed under MIT licence.
///  Source: https://github.com/Sylkadi/BrutalCompanyMinus

///  Available from BrutalCompanyMinusExtraReborn, licensed under GNU General Public License.
///  Source: https://github.com/TheSoftDiamond/BrutalCompanyMinusExtraReborn
/// </summary>

public static class ImmersiveVisorCompatibility
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void PatchAllCompatibilityMethods(Harmony harmony)
    {
        ApplyVisorPatch(harmony);
    }

    [HarmonyPrefix]
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void ApplyVisorPatch(Harmony harmony)
    {
        var linecastMethod = AccessTools.Method(typeof(VisorRainState), nameof(VisorRainState.LineCastForCeiling));
        var prefixLinecastMethod = AccessTools.Method(typeof(ImmersiveVisorCompatibility), nameof(LineCastForCeiling_Prefix));

        harmony.Patch(linecastMethod, prefix: new HarmonyMethod(prefixLinecastMethod));
    }

    public static bool LineCastForCeiling_Prefix(VisorRainState __instance, ref bool __result)
    {
        HaulerController controller = References.pickupController;
        if (controller == null)
            return true;

        if (VehicleUtils.IsPlayerInPickupCab(controller))
        {
            __result = true;
            return false;
        }
        return true;
    }
}
