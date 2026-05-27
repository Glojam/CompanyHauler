using CompanyHauler.Networking;
using CompanyHauler.Scripts;
using CompanyHauler.Utils;
using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;


namespace CompanyHauler.Patches;

[HarmonyPatch(typeof(PlayerControllerB))]
public static class PlayerControllerBPatches
{
    public class PlayerControllerBData
    {
        public bool playerSeatedInPickup;
        public bool playerRidingInPickupCab;
        public bool playerRidingOnPickup;

        public bool syncedSeatedInPickup;
        public bool syncedRidingInPickupCab;
        public bool syncedRidingOnPickup;
    }

    public static Dictionary<PlayerControllerB, PlayerControllerBData> playerData = new();

    // optimisation
    private static Quaternion armsMetarigParentRot = Quaternion.Euler(90f, 0f, 0f);
    private static Quaternion armsMetarigRot = Quaternion.Euler(-90f, 0f, 0f);

    private static Vector3 localArmsPos = new Vector3(0, -0.008f, -0.43f);
    private static Quaternion localArmsRot = Quaternion.Euler(84.78056f, 0f, 0f);

    private static Vector3 playerBodyPos = Vector3.zero;
    private static Quaternion playerBodyRot = Quaternion.Euler(-90, 0, 0);

    private static void RemoveStalePlayerData()
    {
        List<PlayerControllerB> playersToRemove = new();
        foreach (PlayerControllerB player in playerData.Keys)
        {
            if (!player)
            {
                playersToRemove.Add(player);
            }
        }

        foreach (PlayerControllerB player in playersToRemove)
        {
            playerData.Remove(player);
        }
    }

    [HarmonyPatch(nameof(PlayerControllerB.Awake))]
    [HarmonyPostfix]
    static void Awake_Postfix(PlayerControllerB __instance)
    {
        RemoveStalePlayerData();
        if (!playerData.ContainsKey(__instance))
        {
            PlayerControllerBData thisData = new();
            playerData.Add(__instance, thisData);
        }
    }

    [HarmonyPatch(nameof(PlayerControllerB.UpdatePlayerAnimationsToOtherClients))]
    [HarmonyPrefix]
    static bool UpdatePlayerAnimationsToOtherClients_Prefix(PlayerControllerB __instance, Vector2 moveInputVector)
    {
        if (__instance != GameNetworkManager.Instance.localPlayerController)
            return true;

        if (PlayerUtils.disableAnimationSync) return false;
        return true;
    }

    /// <summary>
    ///  Available from CruiserImproved, licensed under MIT License.
    ///  Source: https://github.com/digger1213/CruiserImproved/blob/main/source/Patches/PlayerController.cs
    /// </summary>
    [HarmonyPatch(nameof(PlayerControllerB.LateUpdate))]
    [HarmonyPostfix]
    public static void LateUpdate_Zone_Postfix(PlayerControllerB __instance)
    {
        if (__instance == null ||
            !__instance.isPlayerControlled ||
            __instance != GameNetworkManager.Instance.localPlayerController)
        {
            return;
        }
        SetPlayerVehicleZone(__instance);
    }

    private static void SetPlayerVehicleZone(PlayerControllerB playerController)
    {
        HaulerController haulerController = References.pickupController;

        var localPlayerData = playerData[playerController];
        bool sittingInPickup = PlayerUtils.isSeatedInPickup;
        bool ridingInPickupCab = haulerController?.vehicleCabZone.playerInZone ?? false;
        bool ridingOnPickup = haulerController?.vehicleZone.playerInZone ?? false;

        if (localPlayerData.playerSeatedInPickup == sittingInPickup &&
            localPlayerData.playerRidingInPickupCab == ridingInPickupCab &&
            localPlayerData.playerRidingOnPickup == ridingOnPickup)
        {
            return;
        }

        localPlayerData.playerSeatedInPickup = sittingInPickup;
        localPlayerData.playerRidingInPickupCab = ridingInPickupCab;
        localPlayerData.playerRidingOnPickup = ridingOnPickup;
        HaulerNetworker.Instance?.SyncPlayerZoneRpc(playerController.NetworkObject,
                                                 sittingInPickup,
                                                 ridingInPickupCab,
                                                 ridingOnPickup);
    }

    [HarmonyPatch(nameof(PlayerControllerB.LateUpdate))]
    [HarmonyPostfix]
    private static void LateUpdate_Postfix(PlayerControllerB __instance)
    {
        if (__instance == null ||
            __instance.isPlayerDead ||
            !__instance.isPlayerControlled)
        {
            return;
        }

        if (!__instance.inVehicleAnimation || 
            !playerData[__instance].playerSeatedInPickup)
        {
            return;
        }

        __instance.playerModelArmsMetarig.parent.transform.localRotation = armsMetarigParentRot;
        __instance.playerModelArmsMetarig.localRotation = armsMetarigRot;
        __instance.localArmsTransform.localPosition = localArmsPos;
        __instance.localArmsTransform.localRotation = localArmsRot;
        __instance.playerBodyAnimator.transform.localPosition = playerBodyPos;
        __instance.playerBodyAnimator.transform.localRotation = playerBodyRot;
    }
}
