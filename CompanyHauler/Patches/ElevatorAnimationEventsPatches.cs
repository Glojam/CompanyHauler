using CompanyHauler.Scripts;
using CompanyHauler.Utils;
using GameNetcodeStuff;
using HarmonyLib;

namespace CompanyHauler.Patches;

[HarmonyPatch(typeof(ElevatorAnimationEvents))]
public static class ElevatorAnimationEventsPatches
{
    [HarmonyPatch(nameof(ElevatorAnimationEvents.ElevatorFullyRunning))]
    [HarmonyPrefix]
    static void ElevatorAnimationEvents_Pre_ElevatorFullyRunning()
    {
        HaulerController pickupController = VehicleUtils.pickupController;
        if (pickupController == null)
            return;

        PlayerControllerB playerController = GameNetworkManager.Instance.localPlayerController;

        bool playerSeated = VehicleUtils.IsPlayerSeatedInPickup(playerController, pickupController);
        bool playerOnPickup = VehicleUtils.IsPlayerInPickupBounds(playerController, pickupController);
        bool playerInCab = VehicleUtils.IsPlayerInPickupCab(playerController, pickupController);

        if (playerSeated || playerOnPickup || playerInCab)
            playerController.isInElevator = true;
    }
}