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
    static void PlayerIsTargetable_Postfix(EnemyAI __instance, PlayerControllerB playerScript, bool cannotBeInShip, bool overrideInsideFactoryCheck, bool checkForMineshaftStartTile, ref bool __result)
    {
        if (__instance is not BushWolfEnemy)
            return;

        HaulerController controller = References.pickupController;
        if (controller == null)
            return;

        var playerData = PlayerControllerBPatches.playerData[playerScript];
        bool isOccupant = playerData.playerSeatedInPickup;

        if (isOccupant && VehicleUtils.IsSeatedPlayerProtected(playerController: playerScript, pickupController: controller, checkSunroof: false, checkWindows: true))
            __result = false;

        if (playerData.playerRidingInPickupCab && 
            !controller.driverSideDoor.boolValue && !controller.passengerSideDoor.boolValue && 
            !controller.backLeftDoor.boolValue && !controller.backRightDoor.boolValue)
            __result = false;
    }
}