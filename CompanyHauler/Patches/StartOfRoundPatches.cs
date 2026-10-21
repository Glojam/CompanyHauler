using HarmonyLib;
using System;
using CompanyHauler.Scripts;

namespace CompanyHauler.Patches;

[HarmonyPatch]
public static class StartOfRoundPatches
{
    /// <summary>
    ///  Available from CruiserImproved, licensed under MIT License.
    ///  Source: https://github.com/digger1213/CruiserImproved/blob/main/source/Patches/StartOfRound.cs
    ///  Source: https://github.com/digger1213/CruiserImproved/blob/main/source/Network/Patches/StartOfRound.cs
    /// </summary>
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SyncAlreadyHeldObjectsServerRpc))]
    [HarmonyPostfix]
    static void StartOfRound_Post_SyncAlreadyHeldObjectsServerRpc(StartOfRound __instance, int joiningClientId)
    {
        if (!__instance.attachedVehicle || __instance.attachedVehicle is not HaulerController controller) return;
        try
        {
            if (controller == null)
            {
                Plugin.LogError("Attempted to send client data, but the Pickup is null? please report this to Scandal.");
                return;
            }
            controller.SendClientSyncData((ulong)joiningClientId);
        }
        catch (Exception e)
        {
            Plugin.LogError("Exception caught sending saved Pickup data:\n" + e);
        }
    }
}