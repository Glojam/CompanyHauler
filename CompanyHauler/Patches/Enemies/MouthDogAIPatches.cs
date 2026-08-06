using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using CompanyHauler.Utils;
using CompanyHauler.Scripts;

namespace CompanyHauler.Patches.Enemies;

[HarmonyPatch(typeof(MouthDogAI))]
public static class MouthDogAIPatches
{
    [HarmonyPatch(nameof(MouthDogAI.OnCollideWithPlayer))]
    [HarmonyPrefix]
    static bool MouthDogAI_Pre_OnCollideWithPlayer(MouthDogAI __instance, Collider other, bool __runOriginal)
    {
        if (!__runOriginal)
            return false;

        HaulerController pickupController = VehicleUtils.pickupController;
        if (pickupController == null)
            return true;

        PlayerControllerB playerController =
            __instance.MeetsStandardPlayerCollisionConditions(other, __instance.inKillAnimation, false);
        if (playerController == null)
            return true;

        bool playerOnPickup = VehicleUtils.IsPlayerInPickupBounds(playerController, pickupController);
        bool playerSeated = VehicleUtils.IsPlayerSeatedInPickup(playerController, pickupController);

        if (playerSeated)
        {
            return !VehicleUtils.IsSeatedPlayerProtected(playerController, pickupController, checkSunroof: false, checkWindows: false, windshieldCheck: true, velocityCheck: true, velocityMagnitude: 10f);
        }

        if (!playerOnPickup)
            return true;

        return !VehicleUtils.IsPlayerProtectedByPickup(playerController, pickupController, checkSunroof: false, checkWindows: false, windshieldCheck: true, velocityCheck: true, velocityMagnitude: 10f);
    }
}