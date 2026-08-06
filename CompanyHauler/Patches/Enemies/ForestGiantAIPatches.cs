using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using CompanyHauler.Utils;
using CompanyHauler.Scripts;

namespace CompanyHauler.Patches.Enemies;

[HarmonyPatch(typeof(ForestGiantAI))]
public static class ForestGiantAIPatches
{
    [HarmonyPatch(nameof(ForestGiantAI.AnimationEventA))]
    [HarmonyPrefix]
    static bool ForestGiantAI_Pre_AnimationEventA(ForestGiantAI __instance, bool __runOriginal)
    {
        if (!__runOriginal)
            return false;


        HaulerController pickupController = VehicleUtils.pickupController;
        if (pickupController == null)
            return true;

        PlayerControllerB playerController = GameNetworkManager.Instance.localPlayerController;
        if (playerController == null)
            return false;

        bool playerSeated = VehicleUtils.IsPlayerSeatedInPickup(playerController, pickupController);
        bool playerInCab = VehicleUtils.IsPlayerInPickupCab(playerController, pickupController);

        if (playerSeated || playerInCab)
            return false;

        return true;
    }

    [HarmonyPatch(nameof(ForestGiantAI.OnCollideWithPlayer))]
    [HarmonyPrefix]
    static bool ForestGiantAI_Pre_OnCollideWithPlayer(ForestGiantAI __instance, Collider other, bool __runOriginal)
    {
        if (!__runOriginal)
            return false;

        HaulerController pickupController = VehicleUtils.pickupController;
        if (pickupController == null)
            return true;

        PlayerControllerB playerController = 
            __instance.MeetsStandardPlayerCollisionConditions(other, __instance.inEatingPlayerAnimation, false);
        if (playerController == null)
            return true;

        bool playerOnPickup = VehicleUtils.IsPlayerInPickupBounds(playerController, pickupController);
        bool playerSeated = VehicleUtils.IsPlayerSeatedInPickup(playerController, pickupController);

        if (playerSeated)
            return !VehicleUtils.IsSeatedPlayerProtected(playerController, pickupController, checkSunroof: true, checkWindows: true, windshieldCheck: true, velocityCheck: true, velocityMagnitude: 8f);

        if (!playerOnPickup)
            return true;

        return !VehicleUtils.IsPlayerProtectedByPickup(playerController, pickupController, checkSunroof: true, checkWindows: true, windshieldCheck: true, velocityCheck: true, velocityMagnitude: 8f);
    }
}