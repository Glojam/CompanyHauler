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
    static bool PumaAI_Pre_OnCollideWithPlayer(PumaAI __instance, Collider other, bool __runOriginal)
    {
        if (!__runOriginal)
            return false;

        HaulerController pickupController = VehicleUtils.pickupController;
        if (pickupController == null)
            return true;

        PlayerControllerB playerController =
            __instance.MeetsStandardPlayerCollisionConditions(other, false, false);
        if (playerController == null)
            return true;

        bool playerOnPickup = VehicleUtils.IsPlayerInPickupBounds(playerController, pickupController);
        bool playerSeated = VehicleUtils.IsPlayerSeatedInPickup(playerController, pickupController);

        if (playerSeated)
            return !VehicleUtils.IsSeatedPlayerProtected(playerController, pickupController, checkSunroof: false, checkWindows: true, windshieldCheck: true, velocityCheck: true, velocityMagnitude: 10f);

        if (!playerOnPickup)
            return true;

        return !VehicleUtils.IsPlayerProtectedByPickup(playerController, pickupController, checkSunroof: false, checkWindows: true, windshieldCheck: true, velocityCheck: true, velocityMagnitude: 10f);
    }
}