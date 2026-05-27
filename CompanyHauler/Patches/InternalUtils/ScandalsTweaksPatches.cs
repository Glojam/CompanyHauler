using CompanyHauler.Patches;
using CompanyHauler.Scripts;
using CompanyHauler.Utils;
using GameNetcodeStuff;
using HarmonyLib;
using ScandalsTweaks.Patches;
using ScandalsTweaks.Utils;

namespace CompanyHauler.Patches.InternalUtils;

[HarmonyPatch]
public static class ScandalsTweaksPatches
{
    public static bool IsPlayerInPickup(PlayerControllerB player)
    {
        if (PlayerControllerBPatches.playerData[player].playerSeatedInPickup ||
            PlayerControllerBPatches.playerData[player].playerRidingInPickupCab)
            return true;
        return false;
    }

    [HarmonyPatch(typeof(GlobalUtilities), nameof(GlobalUtilities.ShouldAllowSightForVehicle))]
    [HarmonyPrefix]
    private static bool ShouldAllowSightForVehicle_Prefix(PlayerControllerB player, EnemyAI enemy, ref bool __result)
    {
        if (IsPlayerInPickup(player))
        {
            __result = !player.isCrouching;
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(GiantKiwiAI_Patches), nameof(GiantKiwiAI_Patches.IsTargetPlayerInVehicle))]
    [HarmonyPrefix]
    private static bool IsTargetPlayerInVehicle_Prefix(GiantKiwiAI giantKiwiAi, VehicleController vehicleController, ref bool __result)
    {
        if (vehicleController is not HaulerController controller)
            return true;

        var targetData = PlayerControllerBPatches.playerData[giantKiwiAi.targetPlayer];
        bool targetInTruck = targetData.playerSeatedInPickup ||
                             targetData.playerRidingInPickupCab ||
                             controller.ontopOfTruckCollider.ClosestPoint(giantKiwiAi.targetPlayer.transform.position) ==
                             giantKiwiAi.targetPlayer.transform.position;

        if (targetInTruck)
        {
            __result = true;
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(Landmine_Patches), nameof(Landmine_Patches.ShouldCheckCustomKnockback))]
    [HarmonyPrefix]
    private static bool ShouldCheckCustomKnockback_Prefix(ref bool __result)
    {
        if (PlayerUtils.isSeatedInPickup)
        {
            __result = true;
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(Landmine_Patches), nameof(Landmine_Patches.CanPlayerBeKnockedBack))]
    [HarmonyPrefix]
    private static bool CanPlayerBeKnockedBack_Prefix(ref bool __result)
    {
        PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
        if (IsPlayerInPickup(player: localPlayer))
        {
            __result = false;
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(Landmine_Patches), nameof(Landmine_Patches.CurrentForceMultiplier))]
    [HarmonyPrefix]
    private static bool CurrentForceMultiplier_Prefix(ref float __result)
    {
        if (References.pickupController != null)
        {
            __result = 1f;
            return false;
        }
        return true;
    }
}
