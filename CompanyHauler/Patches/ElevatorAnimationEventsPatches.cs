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
    static void ElevatorFullyRunning_Prefix()
    {
        HaulerController controller = References.pickupController;
        if (controller == null)
            return;

        // save players who are on the magneted pickup from being abandoned
        PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
        if (VehicleUtils.IsPlayerInPickupBounds(controller))
            localPlayer.isInElevator = false;
    }
}
