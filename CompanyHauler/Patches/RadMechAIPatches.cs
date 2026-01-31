using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using CompanyHauler.Utils;

namespace CompanyHauler.Patches;

[HarmonyPatch(typeof(RadMechAI))]
internal static class RadMechAIPatches
{
    [HarmonyPatch("OnCollideWithPlayer")]
    [HarmonyPrefix]
    static bool OnCollideWithPlayer_Prefix(RadMechAI __instance, Collider other)
    {
        PlayerControllerB playerControllerB = GameNetworkManager.Instance.localPlayerController;
        if (playerControllerB == null || !playerControllerB.isPlayerControlled || playerControllerB.isPlayerDead)
            return true;

        if (References.truckController == null)
            return true;

        // not in our truck, run vanilla logic
        if (!VehicleUtils.IsPlayerInTruck(playerControllerB, References.truckController))
            return true;
        // this check is also important to prevent returning false if the player isn't in our truck

        // check if the player is protected in our truck
        if (VehicleUtils.IsPlayerProtectedByTruck(playerControllerB, References.truckController))
        {
            // player is protected, so do not allow the grab
            return false;
        }
        // run vanilla logic
        return true;
    }
}