using CompanyHauler.Scripts;
using CompanyHauler.Utils;
using GameNetcodeStuff;
using HarmonyLib;
using ScandalsTweaks.Utils;
using ScandalsTweaks.Compatibility;
using ScandalsTweaks.Patches.Enemies;

namespace CompanyHauler.Patches.InternalUtils;

[HarmonyPatch]
public static class ScandalsTweaksPatches
{
    public static bool IsPlayerInPickupCab(PlayerControllerB player)
    {
        HaulerController pickupController = VehicleUtils.pickupController;
        if (pickupController == null)
            return false;

        return VehicleUtils.IsPlayerSeatedInPickup(player, pickupController) || 
               VehicleUtils.IsPlayerInPickupCab(player, pickupController, usePlayerData: true);
    }

    [HarmonyPatch(typeof(JLLCompatibility), nameof(JLLCompatibility.CanBlowPlayer))]
    [HarmonyPrefix]
    private static bool JLLCompatibility_Pre_CanBlowPlayer(PlayerControllerB player, ref bool __result)
    {
        if (IsPlayerInPickupCab(player))
        {
            __result = false;
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(JLLCompatibility), nameof(JLLCompatibility.CanDamagePlayer))]
    [HarmonyPrefix]
    private static bool JLLCompatibility_Pre_CanDamagePlayer(PlayerControllerB player, ref bool __result)
    {
        if (IsPlayerInPickupCab(player))
        {
            __result = false;
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(Utilities), nameof(Utilities.ShouldAllowSightThroughVehicle))]
    [HarmonyPrefix]
    private static bool Utilities_Pre_ShouldAllowSightThroughVehicle(PlayerControllerB player, EnemyAI enemy, ref bool __result)
    {
        if (IsPlayerInPickupCab(player))
        {
            __result = !player.isCrouching;
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(GiantKiwiAIPatches), nameof(GiantKiwiAIPatches.IsTargetPlayerInVehicle))]
    [HarmonyPrefix]
    private static bool GiantKiwiAIPatches_Pre_IsTargetPlayerInVehicle(GiantKiwiAI giantKiwiAi, VehicleController vehicleController, ref bool __result)
    {
        if (vehicleController is not HaulerController pickupController)
            return true;

        PlayerControllerB targetPlayer = giantKiwiAi.targetPlayer;
        bool playerSeated = VehicleUtils.IsPlayerSeatedInPickup(targetPlayer, pickupController);
        bool playerInCab = VehicleUtils.IsPlayerInPickupCab(targetPlayer, pickupController, usePlayerData: true);

        bool playerOnPickup =
            pickupController.ontopOfTruckCollider.ClosestPoint(targetPlayer.transform.position) ==
            targetPlayer.transform.position;

        if (playerSeated ||
            playerInCab ||
            playerOnPickup)
        {
            __result = true;
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(BushWolfEnemyPatches), nameof(BushWolfEnemyPatches.IsTargetPlayerProtected))]
    [HarmonyPrefix]
    private static bool BushWolfEnemyPatches_Pre_IsTargetPlayerProtected(PlayerControllerB player, VehicleController vehicle, ref bool __result)
    {
        if (vehicle is not HaulerController pickupController)
            return true;

        bool playerSeated = VehicleUtils.IsPlayerSeatedInPickup(player, pickupController);
        bool playerInCab = VehicleUtils.IsPlayerInPickupCab(player, pickupController, usePlayerData: true);

        if (playerSeated)
        {
            if (VehicleUtils.IsSeatedPlayerProtected(player, pickupController, checkSunroof: false, checkWindows: true))
            {
                __result = true;
                return false;
            }
            return true;
        }

        if (!playerInCab)
            return true;

        if (VehicleUtils.IsPlayerProtectedByPickup(player, pickupController, checkSunroof: false, checkWindows: false))
        {
            __result = true;
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(ScandalsTweaks.Patches.LandminePatches), nameof(ScandalsTweaks.Patches.LandminePatches.ShouldCheckCustomKnockback))]
    [HarmonyPrefix]
    private static bool LandminePatches_Pre_ShouldCheckCustomKnockback(ref bool __result)
    {
        PlayerControllerB playerController = GameNetworkManager.Instance.localPlayerController;
        HaulerController haulerController = VehicleUtils.pickupController;
        if (VehicleUtils.IsPlayerSeatedInPickup(playerController, haulerController))
        {
            __result = true;
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(ScandalsTweaks.Patches.LandminePatches), nameof(ScandalsTweaks.Patches.LandminePatches.CanPlayerBeKnockedBack))]
    [HarmonyPrefix]
    private static bool LandminePatches_Pre_CanPlayerBeKnockedBack(ref bool __result)
    {
        PlayerControllerB playerController = GameNetworkManager.Instance.localPlayerController;
        HaulerController haulerController = VehicleUtils.pickupController;
        if (VehicleUtils.IsPlayerSeatedInPickup(playerController, haulerController))
        {
            __result = false;
            return false;
        }
        return true;
    }
}