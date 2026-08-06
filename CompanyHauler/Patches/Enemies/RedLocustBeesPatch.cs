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
    static void RedLocustBees_Pre_OnCollideWithPlayer(RedLocustBees __instance, Collider other)
    {
        HaulerController pickupController = VehicleUtils.pickupController;
        if (pickupController == null)
            return;

        PlayerControllerB playerController =
            __instance.MeetsStandardPlayerCollisionConditions(other, false, false);
        if (playerController == null)
            return;

        bool playerOnPickup = VehicleUtils.IsPlayerInPickupBounds(playerController, pickupController);
        bool playerSeated = VehicleUtils.IsPlayerSeatedInPickup(playerController, pickupController);

        if (playerSeated)
        {
            if (VehicleUtils.IsSeatedPlayerProtected(playerController, pickupController, checkSunroof: false, checkWindows: true, windshieldCheck: false, velocityCheck: true, velocityMagnitude: 5f))
                __instance.timeSinceHittingPlayer = 0f;
            return;
        }

        if (!playerOnPickup)
            return;

        if (VehicleUtils.IsPlayerProtectedByPickup(playerController, pickupController, checkWindows: true, windshieldCheck: false, velocityCheck: true, velocityMagnitude: 5f))
            __instance.timeSinceHittingPlayer = 0f;
    }
}