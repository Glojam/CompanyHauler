using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using CompanyHauler.Utils;
using CompanyHauler.Scripts;

namespace CompanyHauler.Patches.Enemies;

[HarmonyPatch(typeof(BaboonBirdAI))]
public static class BaboonBirdAIPatches
{
    [HarmonyPatch(nameof(BaboonBirdAI.OnCollideWithPlayer))]
    [HarmonyPrefix]
    static bool BaboonBirdAI_Pre_OnCollideWithPlayer(BaboonBirdAI __instance, Collider other, bool __runOriginal)
    {
        if (!__runOriginal)
            return false;

        HaulerController pickupController = VehicleUtils.pickupController;
        if (pickupController == null)
            return true;

        PlayerControllerB playerController = 
            __instance.MeetsStandardPlayerCollisionConditions(other, __instance.inSpecialAnimation || __instance.doingKillAnimation, false);
        if (playerController == null)
            return true;

        bool playerOnPickup = VehicleUtils.IsPlayerInPickupBounds(playerController, pickupController);
        bool playerInCab = VehicleUtils.IsPlayerInPickupCab(playerController, pickupController);
        bool playerSeated = VehicleUtils.IsPlayerSeatedInPickup(playerController, pickupController);

        if (playerSeated)
            return !VehicleUtils.IsSeatedPlayerProtected(playerController, pickupController, checkSunroof: false, checkWindows: true, windshieldCheck: true, velocityCheck: true, velocityMagnitude: 5f);

        if (!playerOnPickup)
            return true;

        if (!playerInCab)
            return true;

        return !VehicleUtils.IsPlayerProtectedByPickup(playerController, pickupController, checkSunroof: false, checkWindows: true, windshieldCheck: true, velocityCheck: true, velocityMagnitude: 5f);
    }
}
