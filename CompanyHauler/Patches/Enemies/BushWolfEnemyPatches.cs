using CompanyHauler.Patches;
using CompanyHauler.Scripts;
using CompanyHauler.Utils;
using HarmonyLib;

namespace CompanyHauler.Patches.Enemies;

[HarmonyPatch(typeof(BushWolfEnemy))]
public static class BushWolfEnemyPatches
{
    [HarmonyPatch(nameof(BushWolfEnemy.Update))]
    [HarmonyPostfix]
    static void Update_Postfix(BushWolfEnemy __instance)
    {
        if (__instance.targetPlayer == null)
            return;
        if (__instance.targetPlayer.isPlayerDead || !__instance.targetPlayer.isPlayerControlled ||
            __instance.targetPlayer.inAnimationWithEnemy || __instance.stunNormalizedTimer > 0f) return;

        HaulerController controller = References.pickupController;
        if (controller == null)
            return;

        var targetData = PlayerControllerBPatches.playerData[__instance.targetPlayer];
        bool isOccupant = targetData.playerSeatedInPickup;

        if (isOccupant && VehicleUtils.IsSeatedPlayerProtected(playerController: __instance.targetPlayer, pickupController: controller, checkSunroof: false, checkWindows: true))
        {
            __instance.agent.speed = 0f;
            __instance.CancelReelingPlayerIn();
            if (__instance.IsOwner && __instance.tongueLengthNormalized < -0.25f)
            {
                __instance.SwitchToBehaviourState(0);
                return;
            }
            return;
        }

        if (targetData.playerRidingInPickupCab && 
            !controller.driverSideDoor.boolValue && !controller.passengerSideDoor.boolValue &&
            !controller.backLeftDoor.boolValue && !controller.backRightDoor.boolValue)
        {
            __instance.agent.speed = 0f;
            __instance.CancelReelingPlayerIn();
            if (__instance.IsOwner && __instance.tongueLengthNormalized < -0.25f)
            {
                __instance.SwitchToBehaviourState(0);
                return;
            }
            return;
        }
    }
}