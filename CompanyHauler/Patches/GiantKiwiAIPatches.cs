using CompanyHauler.Utils;
using GameNetcodeStuff;
using HarmonyLib;

namespace CompanyHauler.Patches;

[HarmonyPatch(typeof(GiantKiwiAI))]
internal static class GiantKiwiAIPatches
{
    [HarmonyPatch("AnimationEventB")]
    [HarmonyPrefix]
    static void AnimationEventB_Prefix(GiantKiwiAI __instance)
    {
        PlayerControllerB playerControllerB = GameNetworkManager.Instance.localPlayerController;

        if (playerControllerB == null || !playerControllerB.isPlayerControlled || playerControllerB.isPlayerDead)
            return;

        // check there is one of our trucks on the map
        if (References.truckController == null)
            return;

        // not in our truck, run vanilla logic
        if (!VehicleUtils.IsPlayerInTruck(playerControllerB, References.truckController))
            return;

        // check if the player is protected in our truck
        if (VehicleUtils.IsPlayerProtectedByTruck(playerControllerB, References.truckController))
        {
            // idk if this works but it's worth a try
            __instance.timeSinceHittingPlayer = 0.4f;
            return;
        }
    }
}