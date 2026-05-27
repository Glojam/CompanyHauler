using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using CompanyHauler.Utils;
using CompanyHauler.Scripts;

namespace CompanyHauler.Patches.Enemies;

[HarmonyPatch(typeof(BaboonBirdAI))]
public static class BaboonBirdAIPatches
{
    [HarmonyPatch(nameof(BaboonBirdAI.OnCollideWithPlayer))]
    [HarmonyPrefix]
    static bool OnCollideWithPlayer_Prefix(BaboonBirdAI __instance, Collider other, bool __runOriginal)
    {
        if (!__runOriginal)
            return false;

        HaulerController controller = References.pickupController;
        if (controller == null)
            return true;

        PlayerControllerB playerControllerB = __instance.MeetsStandardPlayerCollisionConditions(other, __instance.inSpecialAnimation || __instance.doingKillAnimation, false);
        if (playerControllerB == null)
            return true;

        if (VehicleUtils.IsPlayerSeatedInPickup())
        {
            if (VehicleUtils.IsSeatedPlayerProtected(playerController: playerControllerB, pickupController: controller, checkSunroof: false, checkWindows: true, windshieldCheck: true, velocityCheck: true, velocityMagnitude: 5f))
            {
                return false;
            }
            return true;
        }

        if (VehicleUtils.IsPlayerInPickupBounds(pickupController: controller))
        {
            if (VehicleUtils.IsPlayerProtectedByPickup(playerController: playerControllerB, pickupController: controller, checkSunroof: false, checkWindows: true, windshieldCheck: true, velocityCheck: true, velocityMagnitude: 5f))
            {
                return false;
            }
            return true;
        }
        return true;
    }
}
