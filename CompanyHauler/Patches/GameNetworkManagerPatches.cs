using HarmonyLib;
using CompanyHauler.Networking;

namespace CompanyHauler.Patches;

[HarmonyPatch(typeof(GameNetworkManager))]
public static class GameNetworkManagerPatches
{
    [HarmonyPatch(nameof(GameNetworkManager.Start))]
    [HarmonyPostfix]
    private static void GameNetworkManager_Post_Start(GameNetworkManager __instance)
    {
        HaulerNetworker.Init();
    }
}