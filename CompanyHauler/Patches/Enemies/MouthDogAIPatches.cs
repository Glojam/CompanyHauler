using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using CompanyHauler.Utils;
using CompanyHauler.Scripts;

namespace CompanyHauler.Patches.Enemies;

[HarmonyPatch(typeof(MouthDogAI))]
public static class MouthDogAIPatches
{
    [HarmonyPatch(nameof(MouthDogAI.OnCollideWithPlayer))]
    [HarmonyPrefix]
    static bool OnCollideWithPlayer_Prefix(MouthDogAI __instance, Collider other, bool __runOriginal)
    {
        if (!__runOriginal)
            return false;

        HaulerController controller = References.pickupController;
        if (controller == null)
            return true;

        PlayerControllerB playerControllerB = __instance.MeetsStandardPlayerCollisionConditions(other, __instance.inKillAnimation, false);
        if (playerControllerB == null)
            return true;

        if (VehicleUtils.IsPlayerSeatedInPickup())
        {
            if (VehicleUtils.IsSeatedPlayerProtected(playerController: playerControllerB, pickupController: controller, checkSunroof: false, checkWindows: false, windshieldCheck: true, velocityCheck: true, velocityMagnitude: 10f))
            {
                return false;
            }
            return true;
        }
        if (VehicleUtils.IsPlayerInPickupBounds(pickupController: controller))
        {
            if (VehicleUtils.IsPlayerProtectedByPickup(playerController: playerControllerB, pickupController: controller, checkSunroof: false, checkWindows: false, windshieldCheck: true, velocityCheck: true, velocityMagnitude: 10f))
            {
                return false;
            }
            return true;
        }
        return true;
    }
}
