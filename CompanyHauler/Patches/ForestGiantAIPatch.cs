using System;
using System.Collections.Generic;
using System.Text;
using CompanyHauler.Scripts;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace CompanyHauler.Patches;

// If a player is in a Hauler, protect them from getting grabbed since it has a ceiling
[HarmonyPatch(typeof(ForestGiantAI))]
[HarmonyPatch(nameof(ForestGiantAI.OnCollideWithPlayer))]
public static class ForestGiantCollisionPatch
{
    static bool Prefix(Collider other, ForestGiantAI __instance)
    {
        PlayerControllerB player = __instance.MeetsStandardPlayerCollisionConditions(other, __instance.inEatingPlayerAnimation);
        if (!(player != null) || !(player == GameNetworkManager.Instance.localPlayerController))
        {
            return false;
        }
        HaulerController haulerController = UnityEngine.Object.FindObjectOfType<HaulerController>();
        if (haulerController != null && player.physicsParent != null && player.physicsParent == haulerController.transform && !haulerController.backDoorOpen)
        {
            if (player == haulerController.currentDriver && haulerController.driverSideDoor.boolValue)
            {
                CompanyHauler.Logger.LogDebug("player is Driver and their door is open. Eating them!");
                return false;
            }
            else if (player == haulerController.currentPassenger && haulerController.passengerSideDoor.boolValue)
            {
                CompanyHauler.Logger.LogDebug("player is Passenger and their door is open. Eating them!");
                return false;
            }
            else if (player == haulerController.currentBL && haulerController.BLSideDoor.boolValue)
            {
                CompanyHauler.Logger.LogDebug("player is BL and their door is open. Eating them!");
                return false;
            }
            else if (player == haulerController.currentBR && haulerController.BRSideDoor.boolValue)
            {
                CompanyHauler.Logger.LogDebug("player is BR and their door is open. Eating them!");
                return false;
            }
            CompanyHauler.Logger.LogDebug("player is in Hauler, but their door is closed. Aborting!");
            return true;
        }
        return false;
    }
}
