using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using CompanyHauler.Utils;
using CompanyHauler.Scripts;

namespace CompanyHauler.Patches.Enemies;

[HarmonyPatch(typeof(MaskedPlayerEnemy))]
public static class MaskedPlayerEnemyPatches
{
    [HarmonyPatch(nameof(MaskedPlayerEnemy.OnCollideWithPlayer))]
    [HarmonyPrefix]
    static bool MaskedPlayerEnemy_Pre_OnCollideWithPlayer(MaskedPlayerEnemy __instance, Collider other, bool __runOriginal)
    {
        if (!__runOriginal)
            return false;

        HaulerController pickupController = VehicleUtils.pickupController;
        if (pickupController == null)
            return true;

        PlayerControllerB playerController =
            __instance.MeetsStandardPlayerCollisionConditions(other, __instance.inKillAnimation || __instance.startingKillAnimationLocalClient || !__instance.enemyEnabled, false);
        if (playerController == null)
            return true;

        bool playerOnPickup = VehicleUtils.IsPlayerInPickupBounds(playerController, pickupController);
        bool playerSeated = VehicleUtils.IsPlayerSeatedInPickup(playerController, pickupController);

        if (playerSeated)
            return !VehicleUtils.IsSeatedPlayerProtected(playerController, pickupController, checkSunroof: false, checkWindows: false, windshieldCheck: false, velocityCheck: true, velocityMagnitude: 5f);

        if (!playerOnPickup)
            return true;

        return !VehicleUtils.IsPlayerProtectedByPickup(playerController, pickupController, checkSunroof: false, checkWindows: false, windshieldCheck: false, velocityCheck: true, velocityMagnitude: 5f);
    }
}