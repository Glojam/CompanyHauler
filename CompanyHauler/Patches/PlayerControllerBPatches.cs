using CompanyHauler.Scripts;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace CompanyHauler.Patches;

/// <summary>
///  Available from CruiserImproved, licensed under MIT License.
///  Source: https://github.com/digger1213/CruiserImproved/blob/main/source/Patches/PlayerController.cs
/// </summary>
[HarmonyPatch(typeof(PlayerControllerB))]
internal class PlayerControllerPatches
{
    private static bool usingSeatCam = false;

    [HarmonyPatch("Update")]
    [HarmonyPostfix]
    public static void Update_Postfix(PlayerControllerB __instance)
    {
        if (__instance != GameNetworkManager.Instance.localPlayerController) return;
 
        bool cameraSettingsEnabled = CompanyHauler.BoundConfig.haulerLean.Value;
        if (!cameraSettingsEnabled) return;

        Vector3 cameraOffset = Vector3.zero;

        // Only do this if its a Hauler
        bool validHauler = __instance.inVehicleAnimation && __instance.currentTriggerInAnimationWith && __instance.currentTriggerInAnimationWith.overridePlayerParent;
        if (validHauler && __instance.currentTriggerInAnimationWith.overridePlayerParent.TryGetComponent<HaulerController>(out var controller))
        {
            usingSeatCam = true;
            cameraOffset = new Vector3(0f, 0f, 0f);
            Vector3 lookFlat = __instance.gameplayCamera.transform.localRotation * Vector3.forward;
            lookFlat.y = 0;
            float angleToBack = Vector3.Angle(lookFlat, Vector3.back);
            if (angleToBack < 70 && CompanyHauler.BoundConfig.haulerLean.Value)
            {
                //If we're looking backwards, offset the camera to the side ('leaning')
                cameraOffset.x = Mathf.Sign(lookFlat.x) * ((70f - angleToBack) / 70f);
            }
            __instance.gameplayCamera.transform.localPosition = cameraOffset;
        }
        else if (!__instance.inVehicleAnimation && usingSeatCam == true)
        {
            //If player is not in the cruiser, reset the camera once
            usingSeatCam = false;
            __instance.gameplayCamera.transform.localPosition = Vector3.zero;
        }
    }

    [HarmonyPatch("LateUpdate")]
    [HarmonyPrefix]
    private static void LateUpdate_Postfix(PlayerControllerB __instance)
    {
        if (!__instance.isPlayerControlled)
            return;

        if (__instance.isPlayerDead)
            return;

        bool validTruck = __instance.inVehicleAnimation &&
            __instance.currentTriggerInAnimationWith &&
            __instance.currentTriggerInAnimationWith.overridePlayerParent;

        if (validTruck &&
            __instance.currentTriggerInAnimationWith.overridePlayerParent.TryGetComponent<HaulerController>(out var controller))
        {
            // fix players first-person arms orientation after interacting with certain objects (i.e. terminal, start round lever) causing visual issues such as the ignition-key animation being off
            __instance.playerModelArmsMetarig.parent.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            __instance.playerModelArmsMetarig.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            __instance.localArmsTransform.localPosition = new Vector3(0, -0.008f, -0.43f);
            __instance.localArmsTransform.localRotation = Quaternion.Euler(84.78056f, 0f, 0f);
            __instance.playerBodyAnimator.transform.localPosition = new Vector3(0, 0f, 0f);
            __instance.playerBodyAnimator.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        }
    }

    [HarmonyPatch("PlaceGrabbableObject")]
    [HarmonyPostfix]
    static void PlaceGrabbableObject_Postfix(GrabbableObject placeObject)
    {
        ScanNodeProperties scanNode = placeObject.GetComponentInChildren<ScanNodeProperties>();

        // add rigidbody to the scanNode so it'll be scannable when attached to the cruiser
        if (scanNode && !scanNode.GetComponent<Rigidbody>())
        {
            var rb = scanNode.gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }
    }
}
