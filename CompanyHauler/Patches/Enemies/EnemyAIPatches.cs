using GameNetcodeStuff;
using HarmonyLib;
using CompanyHauler.Utils;
using CompanyHauler.Scripts;

namespace CompanyHauler.Patches.Enemies;

[HarmonyPatch(typeof(EnemyAI))]
public static class EnemyAIPatches
{
    [HarmonyPatch(nameof(EnemyAI.PlayerIsTargetable))]
    [HarmonyPostfix]
    static void EnemyAI_Post_PlayerIsTargetable(EnemyAI __instance, PlayerControllerB playerScript, bool cannotBeInShip, bool overrideInsideFactoryCheck, bool checkForMineshaftStartTile, ref bool __result)
    {
        if (__instance is not BushWolfEnemy)
            return;

        HaulerController pickupController = VehicleUtils.pickupController;
        if (pickupController == null)
            return;

        bool playerSeated = VehicleUtils.IsPlayerSeatedInPickup(playerScript, pickupController);
        bool playerInCab = VehicleUtils.IsPlayerInPickupCab(playerScript, pickupController, usePlayerData: true);

        if (playerSeated)
        {
            if (VehicleUtils.IsSeatedPlayerProtected(playerScript, pickupController, checkSunroof: false, checkWindows: true))
                __result = false;
            return;
        }

        if (!playerInCab)
            return;

        if (VehicleUtils.IsPlayerProtectedByPickup(playerScript, pickupController, checkSunroof: false, checkWindows: false))
            __result = false;
    }
}