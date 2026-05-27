using GameNetcodeStuff;
using UnityEngine;
using HarmonyLib;
using CompanyHauler.Utils;
using CompanyHauler.Scripts;

namespace CompanyHauler.Patches.Enemies;

[HarmonyPatch(typeof(PumaAI))]
public static class PumaAIPatches
{
    [HarmonyPatch(nameof(PumaAI.OnCollideWithPlayer))]
    [HarmonyPrefix]
    static bool OnCollideWithPlayer_Prefix(PumaAI __instance, Collider other, bool __runOriginal)
    {
        if (!__runOriginal)
            return false;

        HaulerController controller = References.pickupController;
        if (controller == null)
            return true;

        PlayerControllerB playerControllerB = __instance.MeetsStandardPlayerCollisionConditions(other, false, false);
        if (playerControllerB == null)
            return true;

        if (VehicleUtils.IsPlayerSeatedInPickup())
        {
            if (VehicleUtils.IsSeatedPlayerProtected(playerController: playerControllerB, pickupController: controller, checkSunroof: false, checkWindows: true, windshieldCheck: true, velocityCheck: true, velocityMagnitude: 10f))
            {
                return false;
            }
            return true;
        }

        if (VehicleUtils.IsPlayerInPickupBounds(pickupController: controller))
        {
            if (VehicleUtils.IsPlayerProtectedByPickup(playerController: playerControllerB, pickupController: controller, checkSunroof: false, checkWindows: true, windshieldCheck: true, velocityCheck: true, velocityMagnitude: 10f))
            {
                return false;
            }
            return true;
        }
        return true;
    }
}