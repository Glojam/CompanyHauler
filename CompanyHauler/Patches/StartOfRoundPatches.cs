using HarmonyLib;
using System;
using CompanyHauler.Scripts;

namespace CompanyHauler.Patches;

[HarmonyPatch(typeof(StartOfRound))]
public static class StartOfRoundPatches
{
    // Sync the hosts health value to other clients upon joining
    [HarmonyPatch("SyncAlreadyHeldObjectsServerRpc")]
    [HarmonyPostfix]
    static void SyncAlreadyHeldObjectsServerRpc(StartOfRound __instance, int joiningClientId)
    {
        if (!__instance.attachedVehicle || __instance.attachedVehicle is not HaulerController) return;
        try
        {
            if (__instance.attachedVehicle.TryGetComponent<HaulerController>(out var controller))
            {
                controller.SendClientSyncData();
            }
        }
        catch (Exception e)
        {
            CompanyHauler.Logger.LogError("Exception caught sending Hauler data:\n" + e);
        }
    }
}