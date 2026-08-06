using CompanyHauler.Scripts;
using CompanyHauler.Utils;
using GameNetcodeStuff;
using HarmonyLib;

namespace CompanyHauler.Patches;

[HarmonyPatch(typeof(HUDManager))]
public static class HUDManagerPatches
{
    [HarmonyPatch(nameof(HUDManager.HelmetCondensationDrops))]
    [HarmonyPostfix]
    private static void HUDManager_Post_HelmetCondensationDrops(HUDManager __instance)
    {
        HaulerController pickupController = VehicleUtils.pickupController;
        if (pickupController == null)
            return;

        PlayerControllerB playerController = GameNetworkManager.Instance.localPlayerController;

        bool playerSeated = VehicleUtils.IsPlayerSeatedInPickup(playerController, pickupController);
        bool playerInCab = VehicleUtils.IsPlayerInPickupCab(playerController, pickupController);

        if (playerInCab || playerSeated)
            __instance.increaseHelmetCondensation = false;
    }
}