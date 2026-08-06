using GameNetcodeStuff;
using HarmonyLib;
using CompanyHauler.Utils;
using CompanyHauler.Scripts;

namespace CompanyHauler.Patches.Enemies;

[HarmonyPatch(typeof(GiantKiwiAI))]
public static class GiantKiwiAIPatches
{
    [HarmonyPatch(nameof(GiantKiwiAI.IsEggInsideClosedTruck))]
    [HarmonyPrefix]
    static bool GiantKiwiAI_Pre_IsEggInsideClosedTruck(GiantKiwiAI __instance, KiwiBabyItem egg, bool closedTruck, ref bool __result, bool __runOriginal)
    {
        if (!__runOriginal)
            return false;

        HaulerController pickupController = VehicleUtils.pickupController;
        if (pickupController == null)
            return true;

        if (egg.parentObject != pickupController.physicsRegion.parentNetworkObject.transform)
            return true;

        __result = !pickupController.tailgateOpen;
        return false;
    }

    [HarmonyPatch(nameof(GiantKiwiAI.AnimationEventB))]
    [HarmonyPrefix]
    static void GiantKiwiAI_Pre_AnimationEventB(GiantKiwiAI __instance)
    {
        HaulerController pickupController = VehicleUtils.pickupController;
        if (pickupController == null)
            return;

        PlayerControllerB playerController = GameNetworkManager.Instance.localPlayerController;
        if (playerController == null ||
            !playerController.isPlayerControlled ||
            playerController.isPlayerDead)
            return;

        bool playerOnPickup = VehicleUtils.IsPlayerInPickupBounds(playerController, pickupController);
        bool playerSeated = VehicleUtils.IsPlayerSeatedInPickup(playerController, pickupController);

        if (playerSeated)
        {
            if (VehicleUtils.IsSeatedPlayerProtected(playerController, pickupController, checkSunroof: true, checkWindows: true, windshieldCheck: true, velocityCheck: true, velocityMagnitude: 10f))
                __instance.timeSinceHittingPlayer = 0.4f;
            return;
        }

        if (!playerOnPickup)
            return;

        if (VehicleUtils.IsPlayerProtectedByPickup(playerController, pickupController, checkSunroof: false, checkWindows: true, windshieldCheck: true, velocityCheck: true, velocityMagnitude: 10f))
            __instance.timeSinceHittingPlayer = 0.4f;
    }
}