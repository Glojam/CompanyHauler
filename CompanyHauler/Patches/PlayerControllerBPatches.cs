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
        public bool playerRidingInPickupCab;
        public bool playerRidingOnPickup;
    }

    public static Dictionary<PlayerControllerB, PlayerControllerBData> playerData = new();

    // optimisation
    private static Quaternion armsMetarigParentRot = Quaternion.Euler(90f, 0f, 0f);
    private static Quaternion armsMetarigRot = Quaternion.Euler(-90f, 0f, 0f);

    private static Vector3 localArmsPos = new Vector3(0f, -0.008f, -0.43f);
    private static Quaternion localArmsRot = Quaternion.Euler(84.78056f, 0f, 0f);

    private static Vector3 playerBodyPos = Vector3.zero;
    private static Quaternion playerBodyRot = Quaternion.Euler(-90f, 0f, 0f);

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
    static void PlayerControllerB_Post_Awake(PlayerControllerB __instance)
    {
        RemoveStalePlayerData();
        if (!playerData.ContainsKey(__instance))
        {
            PlayerControllerBData thisData = new();
            playerData.Add(__instance, thisData);
        }
    }

    [HarmonyPatch(nameof(PlayerControllerB.LateUpdate))]
    [HarmonyPrefix]
    public static void PlayerControllerB_Pre_LateUpdate(PlayerControllerB __instance)
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
        HaulerController pickupController = VehicleUtils.pickupController;

        var playerData = PlayerControllerBPatches.playerData[playerController];
        bool ridingInPickupCab = pickupController?.vehicleCabZone.playerInZone ?? false;
        bool ridingOnPickup = pickupController?.vehicleZone.playerInZone ?? false;

        if (playerData.playerRidingInPickupCab == ridingInPickupCab &&
            playerData.playerRidingOnPickup == ridingOnPickup)
        {
            return;
        }

        playerData.playerRidingInPickupCab = ridingInPickupCab;
        playerData.playerRidingOnPickup = ridingOnPickup;
        HaulerNetworker.Instance?.SyncPlayerZoneRpc(playerController.NetworkObject,
                                                 ridingInPickupCab,
                                                 ridingOnPickup);
    }

    [HarmonyPatch(nameof(PlayerControllerB.LateUpdate))]
    [HarmonyPostfix]
    private static void PlayerControllerB_Post_LateUpdate(PlayerControllerB __instance)
    {
        if (__instance == null ||
            __instance.isPlayerDead ||
            !__instance.isPlayerControlled)
        {
            return;
        }

        HaulerController pickupController = VehicleUtils.pickupController;
        if (pickupController == null)
            return;

        if (!VehicleUtils.IsPlayerSeatedInPickup(__instance, pickupController))
            return;

        var playerModelArmsRig = __instance.playerModelArmsMetarig;
        var localArmsTransform = __instance.localArmsTransform;
        var playerBodyAnimator = __instance.playerBodyAnimator.transform;

        playerModelArmsRig.parent.transform.localRotation = armsMetarigParentRot;
        playerModelArmsRig.localRotation = armsMetarigRot;
        localArmsTransform.localPosition = localArmsPos;
        localArmsTransform.localRotation = localArmsRot;
        playerBodyAnimator.localPosition = playerBodyPos;
        playerBodyAnimator.localRotation = playerBodyRot;
    }
}