using HarmonyLib;
using CompanyHauler.Networking;
using System;
using CompanyHauler.Scripts;

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

    /// <summary>
    ///  Available from CruiserImproved, licensed under MIT License.
    ///  Source: https://github.com/digger1213/CruiserImproved/blob/main/source/Patches/StartOfRound.cs
    ///  Source: https://github.com/digger1213/CruiserImproved/blob/main/source/Network/Patches/StartOfRound.cs
    /// </summary>
    [HarmonyPatch(nameof(StartOfRound.SyncAlreadyHeldObjectsServerRpc))]
    [HarmonyPostfix]
    static void SyncAlreadyHeldObjectsServerRpc_Postfix(StartOfRound __instance, int joiningClientId)
    {
        if (!__instance.attachedVehicle || __instance.attachedVehicle is not HaulerController controller) return;
        try
        {
            if (controller == null)
            {
                Plugin.Logger.LogError("Hauler: Attempted to send client data, but the pickup is null? please report this to Scandal.");
                return;
            }
            controller.SendClientSyncData();
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError("Hauler: Exception caught sending saved Cruiser data:\n" + e);
        }
    }
}