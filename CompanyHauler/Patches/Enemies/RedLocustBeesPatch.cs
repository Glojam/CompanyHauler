using CompanyHauler.Scripts;
using CompanyHauler.Utils;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace CompanyHauler.Patches.Enemies;

[HarmonyPatch(typeof(RedLocustBees))]
public static class RedLocustBeesPatches
{
    [HarmonyPatch(nameof(RedLocustBees.OnCollideWithPlayer))]
    [HarmonyPrefix]
    static void OnCollideWithPlayer_Prefix(RedLocustBees __instance, Collider other)
    {
        HaulerController controller = References.pickupController;
        if (controller == null)
            return;

        PlayerControllerB playerControllerB = __instance.MeetsStandardPlayerCollisionConditions(other, false, false);
        if (playerControllerB == null)
            return;

        if (VehicleUtils.IsPlayerSeatedInPickup())
        {
            if (VehicleUtils.IsSeatedPlayerProtected(playerController: playerControllerB, pickupController: controller, checkSunroof: false, checkWindows: true, windshieldCheck: false, velocityCheck: true, velocityMagnitude: 5f))
            {
                __instance.timeSinceHittingPlayer = 0f;
                return;
            }
            return;
        }

        if (VehicleUtils.IsPlayerInPickupBounds(pickupController: controller))
        {
            if (VehicleUtils.IsPlayerProtectedByPickup(playerController: playerControllerB, pickupController: controller, checkWindows: true, windshieldCheck: false, velocityCheck: true, velocityMagnitude: 5f))
            {
                __instance.timeSinceHittingPlayer = 0f;
                return;
            }
            return;
        }
    }
}