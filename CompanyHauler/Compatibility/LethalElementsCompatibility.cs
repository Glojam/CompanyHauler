using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using VoxxWeatherPlugin.Patches;
using VoxxWeatherPlugin.Weathers;
using VoxxWeatherPlugin.Utils;
using CompanyHauler.Utils;
using CompanyHauler.Scripts;

namespace CompanyHauler.Compatibility;

/// <summary>
///  Available from BrutalCompanyMinus, licensed under MIT licence.
///  Source: https://github.com/Sylkadi/BrutalCompanyMinus

///  Available from BrutalCompanyMinusExtraReborn, licensed under GNU General Public License.
///  Source: https://github.com/TheSoftDiamond/BrutalCompanyMinusExtraReborn
/// </summary>
[HarmonyPatch]
public static class LethalElementsCompatibility
{
    public static bool IsIndoorLighting(PlayerControllerB playerScript)
    {
        return playerScript.currentAudioTrigger != null && playerScript.currentAudioTrigger.insideLighting;
    }

    [HarmonyPatch(typeof(PlayerEffectsManager), nameof(PlayerEffectsManager.SetPlayerTemperature))]
    [HarmonyPrefix]
    public static void PlayerEffectsManager_Pre_SetPlayerTemperature(PlayerEffectsManager __instance, float temperatureDelta)
    {
        if (HeatwaveWeather.Instance == null || !HeatwaveWeather.Instance.IsActive)
            return;

        HaulerController pickupController = VehicleUtils.pickupController;
        if (pickupController == null)
            return;

        PlayerControllerB playerController = GameNetworkManager.Instance.localPlayerController;
        if (!VehicleUtils.IsPlayerInPickupBounds(playerController, pickupController) &&
            !VehicleUtils.IsPlayerSeatedInPickup(playerController, pickupController) &&
            !VehicleUtils.IsPlayerInPickupCab(playerController, pickupController))
            return;

        bool insideOfCabin = VehicleUtils.IsPlayerSeatedInPickup(playerController, pickupController) || 
                             VehicleUtils.IsPlayerInPickupCab(playerController, pickupController);
        bool outsideOfPickup = VehicleUtils.IsPlayerOnOutsideOfPickup(playerController, pickupController);
        bool inDoorLighting = IsIndoorLighting(playerController);

        if (insideOfCabin && VehicleUtils.IsPickupCabEnclosed(pickupController))
        {
            if (!inDoorLighting)
            {
                PlayerEffectsManager.heatTransferRate = 0.75f;
            }
            else
            {
                PlayerEffectsManager.heatTransferRate = 1f;
            }
        }
        else PlayerEffectsManager.heatTransferRate = 1f;
        if (outsideOfPickup) PlayerEffectsManager.heatTransferRate = 1f;
    }

    [HarmonyPatch(typeof(HaulerController), nameof(HaulerController.Start))]
    [HarmonyPostfix]
    public static void HaulerController_Post_Start(HaulerController __instance)
    {
        SnowTrackersManager.AddFootprintTracker(__instance, 6f, 0.75f, 1f, new Vector3(0, 0, -1f));
    }

    [HarmonyPatch(typeof(HaulerController), nameof(HaulerController.FixedUpdate))]
    [HarmonyPostfix]
    public static void HaulerController_Post_FixedUpdate(HaulerController __instance)
    {
        if (!SnowPatches.IsSnowActive())
        {
            return;
        }
        SnowTrackersManager.UpdateFootprintTracker(__instance, !__instance.allWheelsAirborne);
    }

    [HarmonyPatch(typeof(SnowfallVFXManager), nameof(SnowfallVFXManager.Update))]
    [HarmonyPostfix]
    public static void SnowfallVFXManager_Post_Update_VFX()
    {
        HaulerController pickupController = VehicleUtils.pickupController;
        if (pickupController == null)
            return;

        PlayerControllerB playerController = GameNetworkManager.Instance.localPlayerController;
        if (!VehicleUtils.IsPlayerInPickupBounds(playerController, pickupController) &&
            !VehicleUtils.IsPlayerSeatedInPickup(playerController, pickupController) &&
            !VehicleUtils.IsPlayerInPickupCab(playerController, pickupController))
            return;

        bool insideOfCabin = VehicleUtils.IsPlayerSeatedInPickup(playerController, pickupController) ||
                             VehicleUtils.IsPlayerInPickupCab(playerController, pickupController);

        if (insideOfCabin)
        {
            PlayerEffectsManager.isUnderSnow = false;
            SnowfallVFXManager.snowMovementHindranceMultiplier = 1f;
        }
    }

    [HarmonyPatch(typeof(BlizzardWeather), nameof(BlizzardWeather.SetColdZoneState))]
    [HarmonyPrefix]
    public static bool BlizzardWeather_Pre_SetColdZoneState(BlizzardWeather __instance)
    {
        HaulerController pickupController = VehicleUtils.pickupController;
        if (pickupController == null)
            return true;

        PlayerControllerB playerController = GameNetworkManager.Instance.localPlayerController;
        if (VehicleUtils.IsPlayerInPickupBounds(playerController, pickupController) ||
            VehicleUtils.IsPlayerSeatedInPickup(playerController, pickupController) ||
            VehicleUtils.IsPlayerInPickupCab(playerController, pickupController))
            return false;

        return true;
    }

    [HarmonyPatch(typeof(SnowfallWeather), nameof(SnowfallWeather.SetColdZoneState))]
    [HarmonyPrefix]
    public static bool SnowfallWeather_Pre_SetColdZoneState(SnowfallWeather __instance)
    {
        if (BlizzardWeather.Instance == null)
            return true;

        HaulerController pickupController = VehicleUtils.pickupController;
        if (pickupController == null)
            return true;

        PlayerControllerB playerController = GameNetworkManager.Instance.localPlayerController;
        if (!VehicleUtils.IsPlayerInPickupBounds(playerController, pickupController) &&
            !VehicleUtils.IsPlayerSeatedInPickup(playerController, pickupController) &&
            !VehicleUtils.IsPlayerInPickupCab(playerController, pickupController))
            return true;

        bool insideOfCabin = VehicleUtils.IsPlayerSeatedInPickup(playerController, pickupController) ||
                             VehicleUtils.IsPlayerInPickupCab(playerController, pickupController);
        bool outsideOfPickup = VehicleUtils.IsPlayerOnOutsideOfPickup(playerController, pickupController);
        bool inDoorLighting = IsIndoorLighting(playerController);

        if (insideOfCabin)
        {
            if (VehicleUtils.IsPickupCabEnclosed(pickupController))
            {
                PlayerEffectsManager.isInColdZone = !inDoorLighting;
            }
            else
            {
                PlayerEffectsManager.isInColdZone = IsWindAllowedVehicle(playerController, pickupController) && BlizzardWeather.Instance.isLocalPlayerInWind;
            }
            return false;
        }
        else if (outsideOfPickup)
        {
            PlayerEffectsManager.isInColdZone = IsWindAllowedVehicle(playerController, pickupController) && BlizzardWeather.Instance.isLocalPlayerInWind;
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(BlizzardWeather), nameof(BlizzardWeather.Update))]
    [HarmonyPrefix]
    public static bool BlizzardWeather_Pre_Update(BlizzardWeather __instance)
    {
        HaulerController pickupController = VehicleUtils.pickupController;
        if (pickupController == null)
            return true;

        PlayerControllerB playerController = GameNetworkManager.Instance.localPlayerController;
        if (!VehicleUtils.IsPlayerInPickupBounds(playerController, pickupController) &&
            !VehicleUtils.IsPlayerSeatedInPickup(playerController, pickupController) &&
            !VehicleUtils.IsPlayerInPickupCab(playerController, pickupController))
            return true;

        bool insideOfCabin = VehicleUtils.IsPlayerSeatedInPickup(playerController, pickupController) ||
                             VehicleUtils.IsPlayerInPickupCab(playerController, pickupController);
        bool outsideOfPickup = VehicleUtils.IsPlayerOnOutsideOfPickup(playerController, pickupController);

        SnowfallWeather.Instance?.Update();

        if (insideOfCabin)
        {
            PlayerEffectsManager.heatTransferRate = 0.75f;
            __instance.isPlayerInBlizzard = __instance.isLocalPlayerInWind && !VehicleUtils.IsPickupCabEnclosed(pickupController);
        }
        else if (outsideOfPickup)
        {
            PlayerEffectsManager.heatTransferRate = 1f;
            __instance.isPlayerInBlizzard = __instance.isLocalPlayerInWind;
        }
        return false;
    }

    public static bool IsWindAllowedVehicle(PlayerControllerB playerController, HaulerController pickupController)
    {
        if (IsIndoorLighting(playerController)) return false;

        bool insideOfCabin = VehicleUtils.IsPlayerSeatedInPickup(playerController, pickupController) ||
                             VehicleUtils.IsPlayerInPickupCab(playerController, pickupController);
        bool outsideOfPickup = VehicleUtils.IsPlayerOnOutsideOfPickup(playerController, pickupController);

        if (insideOfCabin && !VehicleUtils.IsPickupCabEnclosed(pickupController))
        {
            return true;
        }
        else if (outsideOfPickup)
        {
            return true;
        }
        return false;
    }

    [HarmonyPatch(typeof(HeatwavePatches), nameof(HeatwavePatches.CheckConditionsForHeatingStop))]
    [HarmonyPrefix]
    public static bool HeatwavePatches_Pre_CheckConditionsForHeatingStop(PlayerControllerB playerController, ref bool __result)
    {
        HaulerController pickupController = VehicleUtils.pickupController;
        if (pickupController == null)
            return true;

        if (!VehicleUtils.IsPlayerInPickupBounds(playerController, pickupController) &&
            !VehicleUtils.IsPlayerSeatedInPickup(playerController, pickupController) &&
            !VehicleUtils.IsPlayerInPickupCab(playerController, pickupController))
            return true;

        bool insideOfCabin = VehicleUtils.IsPlayerSeatedInPickup(playerController, pickupController) ||
                             VehicleUtils.IsPlayerInPickupCab(playerController, pickupController);
        bool outsideOfPickup = VehicleUtils.IsPlayerOnOutsideOfPickup(playerController, pickupController);
        bool inDoorLighting = IsIndoorLighting(playerController);

        if (insideOfCabin && VehicleUtils.IsPickupCabEnclosed(pickupController))
        {
            __result = true;
        }
        else
        {
            __result = inDoorLighting ? true : false;
        }
        if (outsideOfPickup) __result = inDoorLighting ? true : false;
        return false;
    }

    [HarmonyPatch(typeof(HeatwavePatches), nameof(HeatwavePatches.CheckConditionsForHeatingPause))]
    [HarmonyPrefix]
    public static bool HeatwavePatches_Pre_CheckConditionsForHeatingPause(PlayerControllerB playerController, ref bool __result)
    {
        HaulerController pickupController = VehicleUtils.pickupController;
        if (pickupController == null)
            return true;

        if (!VehicleUtils.IsPlayerInPickupBounds(playerController, pickupController) &&
            !VehicleUtils.IsPlayerSeatedInPickup(playerController, pickupController) &&
            !VehicleUtils.IsPlayerInPickupCab(playerController, pickupController))
            return true;

        __result = false;
        return false;
    }
}