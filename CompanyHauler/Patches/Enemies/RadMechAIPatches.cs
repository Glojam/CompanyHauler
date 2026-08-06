using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using CompanyHauler.Utils;
using CompanyHauler.Scripts;

namespace CompanyHauler.Patches.Enemies;

[HarmonyPatch(typeof(RadMechAI))]
public static class RadMechAIPatches
{
    [HarmonyPatch(nameof(RadMechAI.OnCollideWithPlayer))]
    [HarmonyPrefix]
    static bool RadMechAI_Pre_OnCollideWithPlayer(RadMechAI __instance, Collider other, bool __runOriginal)
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
            return !VehicleUtils.IsSeatedPlayerProtected(playerController, pickupController, checkSunroof: true, checkWindows: true, windshieldCheck: true);

        if (!playerOnPickup)
            return true;

        return !VehicleUtils.IsPlayerProtectedByPickup(playerController, pickupController, checkSunroof: true, checkWindows: true, windshieldCheck: true);
    }
}