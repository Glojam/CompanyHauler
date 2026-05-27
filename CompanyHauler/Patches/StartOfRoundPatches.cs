using HarmonyLib;
using CompanyHauler.Networking;

namespace CompanyHauler.Patches;

[HarmonyPatch(typeof(StartOfRound))]
public static class StartOfRoundPatches
{
    [HarmonyPatch(nameof(StartOfRound.Awake))]
    [HarmonyPrefix]
    private static void Awake_Prefix(StartOfRound __instance)
    {
        HaulerNetworker.Create();
    }
}