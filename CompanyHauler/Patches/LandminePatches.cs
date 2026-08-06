using CompanyHauler.Scripts;
using CompanyHauler.Utils;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace CompanyHauler.Patches;

[HarmonyPatch(typeof(Landmine))]
public static class LandminePatches
{
    [HarmonyPatch(nameof(Landmine.SpawnExplosion))]
    [HarmonyPrefix]
    private static void Landmine_Pre_SpawnExplosion(Landmine __instance, Vector3 explosionPosition, bool spawnExplosionEffect, ref float killRange, ref float damageRange, int nonLethalDamage, float physicsForce, GameObject overridePrefab, bool goThroughCar)
    {
        HaulerController pickupController = VehicleUtils.pickupController;
        if (pickupController == null)
            return;

        PlayerControllerB playerController = GameNetworkManager.Instance.localPlayerController;
        if (!VehicleUtils.IsPlayerNearPickup(playerController, pickupController))
            return;

        bool isProbablyLightning =
            !goThroughCar &&
            !spawnExplosionEffect &&
            killRange == 2.4f &&
            damageRange == 5f &&
            nonLethalDamage == 1 &&
            physicsForce == 0f;

        bool playerSeated = VehicleUtils.IsPlayerSeatedInPickup(playerController, pickupController);
        bool playerInCab = VehicleUtils.IsPlayerInPickupCab(playerController, pickupController);
        bool explosionOutsideCab =
            pickupController.vehicleCabZone.physicsCollider.ClosestPoint(explosionPosition) != explosionPosition;

        if (isProbablyLightning &&
            (playerInCab || playerSeated) &&
            explosionOutsideCab)
        {
            killRange = -1f;
            damageRange = -1f;
        }
    }
}