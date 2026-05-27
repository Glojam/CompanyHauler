using CompanyHauler.Scripts;
using CompanyHauler.Utils;
using GameNetcodeStuff;
using HarmonyLib;

namespace CompanyHauler.Patches.Enemies;

[HarmonyPatch(typeof(SandWormAI))]
public static class SandWormAIPatches
{
    [HarmonyPatch(nameof(SandWormAI.EatPlayer))]
    [HarmonyPrefix]
    static bool EatPlayer_Prefix(SandWormAI __instance, PlayerControllerB playerScript, bool __runOriginal)
    {
        if (!__runOriginal)
            return false;

        HaulerController controller = References.pickupController;
        if (controller == null)
            return true;

        if (VehicleUtils.IsPlayerSeatedInPickup())
        {
            return false;
        }
        if (VehicleUtils.IsPlayerInPickupBounds(pickupController: controller))
        {
            if (VehicleUtils.IsPlayerInPickupCab(pickupController: controller))
            {
                return false;
            }
            return true;
        }
        return true;
    }
}