using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using CompanyHauler.Utils;
using CompanyHauler.Scripts;

namespace CompanyHauler.Patches.Enemies;

[HarmonyPatch(typeof(ForestGiantAI))]
public static class ForestGiantAIPatches
{
    [HarmonyPatch(nameof(ForestGiantAI.AnimationEventA))]
    [HarmonyPrefix]
    static bool AnimationEventA_Prefix(ForestGiantAI __instance, bool __runOriginal)
    {
        if (!__runOriginal)
            return false;

        HaulerController controller = References.pickupController;
        if (controller == null)
            return true;

        PlayerControllerB playerControllerB = GameNetworkManager.Instance.localPlayerController;
        if (playerControllerB == null)
            return false;

        // do not allow fall death in the pickup
        if (VehicleUtils.IsPlayerInPickupCab(pickupController: controller) ||
            VehicleUtils.IsPlayerSeatedInPickup())
            return false;

        // not in our pickup, run vanilla logic
        return true;
    }

    [HarmonyPatch(nameof(ForestGiantAI.OnCollideWithPlayer))]
    [HarmonyPrefix]
    static bool OnCollideWithPlayer_Prefix(ForestGiantAI __instance, Collider other, bool __runOriginal)
    {
        if (!__runOriginal)
            return false;

        HaulerController controller = References.pickupController;
        if (controller == null)
            return true;

        PlayerControllerB playerControllerB = __instance.MeetsStandardPlayerCollisionConditions(other, __instance.inEatingPlayerAnimation, false);
        if (playerControllerB == null)
            return true;

        if (VehicleUtils.IsPlayerSeatedInPickup())
        {
            if (VehicleUtils.IsSeatedPlayerProtected(playerController: playerControllerB, pickupController: controller, checkSunroof: true, checkWindows: true, windshieldCheck: true, velocityCheck: true, velocityMagnitude: 8f))
            {
                return false;
            }
            return true;
        }
        if (VehicleUtils.IsPlayerInPickupBounds(pickupController: controller))
        {
            if (VehicleUtils.IsPlayerProtectedByPickup(playerController: playerControllerB, pickupController: controller, checkSunroof: true, checkWindows: true, windshieldCheck: true, velocityCheck: true, velocityMagnitude: 8f))
            {
                return false;
            }
            return true;
        }
        return true;
    }
}