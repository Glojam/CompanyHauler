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
    static bool IsEggInsideClosedTruck_Prefix(GiantKiwiAI __instance, KiwiBabyItem egg, bool closedTruck, ref bool __result, bool __runOriginal)
    {
        if (!__runOriginal)
            return false;

        HaulerController controller = References.pickupController;
        if (controller == null)
            return true;

        if (egg.parentObject == controller.physicsRegion.parentNetworkObject.transform)
        {
            __result = !controller.tailgateOpen;
            return false;
        }
        return true;
    }

    [HarmonyPatch(nameof(GiantKiwiAI.AnimationEventB))]
    [HarmonyPrefix]
    static void AnimationEventB_Prefix(GiantKiwiAI __instance)
    {
        HaulerController controller = References.pickupController;
        if (controller == null)
            return;

        PlayerControllerB playerControllerB = GameNetworkManager.Instance.localPlayerController;
        if (playerControllerB == null ||
            !playerControllerB.isPlayerControlled ||
            playerControllerB.isPlayerDead)
            return;


        if (VehicleUtils.IsPlayerSeatedInPickup())
        {
            if (VehicleUtils.IsSeatedPlayerProtected(playerController: playerControllerB, pickupController: controller, checkSunroof: true, checkWindows: true, windshieldCheck: true, velocityCheck: true, velocityMagnitude: 10f))
            {
                __instance.timeSinceHittingPlayer = 0f;
                return;
            }
            return;
        }

        if (VehicleUtils.IsPlayerInPickupBounds(pickupController: controller))
        {
            if (VehicleUtils.IsPlayerProtectedByPickup(playerController: playerControllerB, pickupController: controller, checkSunroof: false, checkWindows: true, windshieldCheck: true, velocityCheck: true, velocityMagnitude: 10f))
            {
                __instance.timeSinceHittingPlayer = 0.4f;
                return;
            }
            return;
        }
    }
}