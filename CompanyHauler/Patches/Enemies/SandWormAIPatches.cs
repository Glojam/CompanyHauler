using CompanyHauler.Scripts;
using CompanyHauler.Utils;
using GameNetcodeStuff;
using HarmonyLib;

namespace CompanyHauler.Patches.Enemies;

[HarmonyPatch(typeof(SandWormAI))]
public static class SandWormAIPatches
{
    // nom nom nom
    [HarmonyPatch(nameof(SandWormAI.EatPlayer))]
    [HarmonyPrefix]
    static bool SandWormAI_Pre_EatPlayer(SandWormAI __instance, PlayerControllerB playerScript, bool __runOriginal)
    {
        if (!__runOriginal)
            return false;

        HaulerController pickupController = VehicleUtils.pickupController;
        if (pickupController == null)
            return true;

        if (VehicleUtils.IsPlayerSeatedInPickup(playerScript, pickupController))
            return false;

        if (VehicleUtils.IsPlayerInPickupCab(playerScript, pickupController))
            return false;

        return true;
    }
}