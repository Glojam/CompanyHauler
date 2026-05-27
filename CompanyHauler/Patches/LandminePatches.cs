using CompanyHauler.Scripts;
using CompanyHauler.Utils;
using HarmonyLib;
using UnityEngine;

namespace CompanyHauler.Patches;

[HarmonyPatch(typeof(Landmine))]
public static class LandminePatches
{
    [HarmonyPatch(nameof(Landmine.SpawnExplosion))]
    [HarmonyPrefix]
    private static void SpawnExplosion_Prefix(Landmine __instance, Vector3 explosionPosition, bool spawnExplosionEffect, ref float killRange, ref float damageRange, int nonLethalDamage, float physicsForce, GameObject overridePrefab, bool goThroughCar)
    {
        HaulerController controller = References.pickupController;
        if (controller == null)
            return;

        if (!VehicleUtils.IsPlayerNearPickup(GameNetworkManager.Instance.localPlayerController, pickupController: controller))
            return;

        bool isProbablyLightning = !goThroughCar && !spawnExplosionEffect &&
            killRange == 2.4f && damageRange == 5f && nonLethalDamage == 1f && physicsForce == 0f;

        if (isProbablyLightning && 
            (VehicleUtils.IsPlayerSeatedInPickup() || VehicleUtils.IsPlayerInPickupCab(pickupController: controller)))
        {
            killRange = -1f;
            damageRange = -1f;
        }
    }
}
