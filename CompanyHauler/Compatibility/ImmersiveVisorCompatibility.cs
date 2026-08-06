using HarmonyLib;
using Woecust.ImmersiveVisor;
using CompanyHauler.Utils;
using CompanyHauler.Scripts;
using GameNetcodeStuff;

namespace CompanyHauler.Compatibility;

/// <summary>
///  Available from BrutalCompanyMinus, licensed under MIT licence.
///  Source: https://github.com/Sylkadi/BrutalCompanyMinus

///  Available from BrutalCompanyMinusExtraReborn, licensed under GNU General Public License.
///  Source: https://github.com/TheSoftDiamond/BrutalCompanyMinusExtraReborn
/// </summary>
[HarmonyPatch]
public static class ImmersiveVisorCompatibility
{
    [HarmonyPatch(typeof(VisorRainState), nameof(VisorRainState.LineCastForCeiling))]
    [HarmonyPrefix]
    public static bool VisorRainState_Pre_LineCastForCeiling(VisorRainState __instance, ref bool __result)
    {
        HaulerController pickupController = VehicleUtils.pickupController;
        if (pickupController == null)
            return true;

        PlayerControllerB playerController = GameNetworkManager.Instance.localPlayerController;

        bool playerInCab = VehicleUtils.IsPlayerInPickupCab(playerController, pickupController);
        bool playerSeated = VehicleUtils.IsPlayerSeatedInPickup(playerController, pickupController);

        if (playerInCab || playerSeated)
        {
            __result = true;
            return false;
        }
        return true;
    }
}
