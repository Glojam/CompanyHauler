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
public static class PlayerControllerBPatch
{
    private static bool usingSeatCam = false;

    [HarmonyPatch("Update")]
    [HarmonyPostfix]
    public static void Update_Postfix(PlayerControllerB __instance)
    {
        // Unable to detect LCVR currently due to a LCVR dependency 'Mimics' being unavailable
        //if (LCVRCompatibility.inVrSession) return;

        if (__instance != GameNetworkManager.Instance.localPlayerController) return;
 
        bool cameraSettingsEnabled = CompanyHauler.BoundConfig.haulerLean.Value;
        if (!cameraSettingsEnabled) return;

        Vector3 cameraOffset = Vector3.zero;

        // Only do this if its a Hauler
        bool validCruiser = __instance.inVehicleAnimation && __instance.currentTriggerInAnimationWith && __instance.currentTriggerInAnimationWith.overridePlayerParent;
        if (validCruiser && __instance.currentTriggerInAnimationWith.overridePlayerParent.TryGetComponent<HaulerController>(out var controller))
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
}
