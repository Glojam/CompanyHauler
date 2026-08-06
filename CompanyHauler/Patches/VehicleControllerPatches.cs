using HarmonyLib;
using UnityEngine;
using CompanyHauler.Scripts;
using GameNetcodeStuff;

namespace CompanyHauler.Patches;

[HarmonyPatch(typeof(VehicleController))]
public static class VehicleControllerPatches
{
    [HarmonyPatch(nameof(VehicleController.DisableVehicleCollisionForAllPlayers))]
    [HarmonyPrefix]
    static bool VehicleController_Pre_DisableVehicleCollisionForAllPlayers(VehicleController __instance, bool __runOriginal)
    {
        if (!__runOriginal)
            return false;

        if (__instance is not HaulerController vehicle)
            return true;

        vehicle.DisableVehicleCollisionForAllPlayers();
        return false;
    }

    [HarmonyPatch(nameof(VehicleController.EnableVehicleCollisionForAllPlayers))]
    [HarmonyPrefix]
    static bool VehicleController_Pre_EnableVehicleCollisionForAllPlayers(VehicleController __instance, bool __runOriginal)
    {
        if (!__runOriginal)
            return false;

        if (__instance is not HaulerController vehicle)
            return true;

        vehicle.EnableVehicleCollisionForAllPlayers();
        return false;
    }

    [HarmonyPatch(nameof(VehicleController.SetVehicleCollisionForPlayer))]
    [HarmonyPrefix]
    static bool VehicleController_Pre_SetVehicleCollisionForPlayer(VehicleController __instance, bool __runOriginal, bool setEnabled, PlayerControllerB player)
    {
        if (!__runOriginal)
            return false;

        if (__instance is not HaulerController vehicle)
            return true;

        vehicle.SetVehicleCollisionForPlayer(setEnabled, player);
        return false;
    }

    [HarmonyPatch(nameof(VehicleController.SetBackDoorOpen))]
    [HarmonyPrefix]
    static bool VehicleController_Pre_SetBackDoorOpen(VehicleController __instance, bool __runOriginal, bool open)
    {
        if (!__runOriginal)
            return false;

        if (__instance is not HaulerController)
            return true;

        return false;
    }

    [HarmonyPatch(nameof(VehicleController.SetFrontCabinLightOn))]
    [HarmonyPrefix]
    static bool VehicleController_Pre_SetFrontCabinLightOn(VehicleController __instance, bool __runOriginal, bool setOn)
    {
        if (!__runOriginal)
            return false;

        if (__instance is not HaulerController vehicle)
            return true;

        vehicle.SetFrontCabinLightOn(setOn);
        return false;
    }

    // thank you MattyMatty, and DiFFoZ for helping me with this!!
    [HarmonyPatch(nameof(VehicleController.AddEngineOil))]
    [HarmonyPrefix]
    static bool VehicleController_Pre_AddEngineOil(VehicleController __instance, bool __runOriginal)
    {
        if (!__runOriginal)
            // somebody else has redirected the function ignore the call
            return false;

        if (__instance is not HaulerController vehicle)
            // not us run the original code
            return true;

        // our class run our code, and skip original.
        vehicle.AddEngineOil();
        return false;
    }

    [HarmonyPatch(nameof(VehicleController.AddTurboBoost))]
    [HarmonyPrefix]
    static bool VehicleController_Pre_AddTurboBoost(VehicleController __instance, bool __runOriginal)
    {
        if (!__runOriginal)
            return false;

        if (__instance is not HaulerController)
            return true;

        return false;
    }

    [HarmonyPatch(nameof(VehicleController.StartMagneting))]
    [HarmonyPrefix]
    static bool VehicleController_Pre_StartMagneting(VehicleController __instance, bool __runOriginal)
    {
        if (!__runOriginal)
            return false;

        if (__instance is not HaulerController)
            return true;

        return false;
    }

    [HarmonyPatch(nameof(VehicleController.CollectItemsInTruck))]
    [HarmonyPrefix]
    static bool VehicleController_Pre_CollectItemsInTruck(VehicleController __instance, bool __runOriginal)
    {
        if (!__runOriginal)
            return false;

        if (__instance is not HaulerController)
            return true;

        return false;
    }


    [HarmonyPatch(nameof(VehicleController.DestroyCar))]
    [HarmonyPrefix]
    static bool VehicleController_Pre_DestroyCar(VehicleController __instance, bool __runOriginal)
    {
        if (!__runOriginal)
            return false;

        if (__instance is not HaulerController vehicle)
            return true;

        vehicle.DestroyCar();
        return false;
    }

    [HarmonyPatch(nameof(VehicleController.ExitDriverSideSeat))]
    [HarmonyPrefix]
    static bool VehicleController_Pre_ExitDriverSideSeat(VehicleController __instance, bool __runOriginal)
    {
        if (!__runOriginal)
            return false;

        if (__instance is not HaulerController)
            return true;

        return false;
    }

    [HarmonyPatch(nameof(VehicleController.ExitPassengerSideSeat))]
    [HarmonyPrefix]
    static bool VehicleController_Pre_ExitPassengerSideSeat(VehicleController __instance, bool __runOriginal)
    {
        if (!__runOriginal)
            return false;

        if (__instance is not HaulerController)
            return true;

        return false;
    }

    [HarmonyPatch(nameof(VehicleController.CarReactToObstacle))]
    [HarmonyPrefix]
    static bool VehicleController_Pre_CarReactToObstacle(VehicleController __instance, bool __runOriginal, Vector3 vel, Vector3 position, Vector3 impulse, CarObstacleType type, float obstacleSize, EnemyAI enemyScript, bool dealDamage)
    {
        if (!__runOriginal)
            return false;

        if (__instance is not HaulerController vehicle)
            return true;

        vehicle.CarReactToObstacle(vel, position, impulse, type, obstacleSize, enemyScript, dealDamage);
        return false;
    }

    [HarmonyPatch(nameof(VehicleController.DealPermanentDamage))]
    [HarmonyPrefix]
    static bool VehicleController_Pre_DealPermanentDamage(VehicleController __instance, bool __runOriginal, int damageAmount, Vector3 damagePosition)
    {
        if (!__runOriginal)
            return false;

        if (__instance is not HaulerController vehicle)
            return true;

        vehicle.DealPermanentDamage(damageAmount, damagePosition);
        return false;
    }

    [HarmonyPatch(nameof(VehicleController.DamagePlayerInVehicle))]
    [HarmonyPrefix]
    static bool VehicleController_Pre_DamagePlayerInVehicle(VehicleController __instance, bool __runOriginal, Vector3 vel, float magnitude)
    {
        if (!__runOriginal)
            return false;

        if (__instance is not HaulerController vehicle)
            return true;

        vehicle.DamagePlayerInVehicle(vel);
        return false;
    }

    [HarmonyPatch(nameof(VehicleController.SetInternalStress))]
    [HarmonyPrefix]
    static bool VehicleController_Pre_SetInternalStress(VehicleController __instance, bool __runOriginal, float carStressIncrease)
    {
        if (!__runOriginal)
            return false;

        if (__instance is not HaulerController vehicle)
            return true;

        vehicle.SetInternalStress(carStressIncrease);
        return false;
    }

    [HarmonyPatch(nameof(VehicleController.ToggleHeadlightsLocalClient))]
    [HarmonyPrefix]
    static bool VehicleController_Pre_ToggleHeadlightsLocalClient(VehicleController __instance, bool __runOriginal)
    {
        if (!__runOriginal)
            return false;

        if (__instance is not HaulerController vehicle)
            return true;

        vehicle.ToggleHeadlightsLocalClient();
        return false;
    }

    [HarmonyPatch(nameof(VehicleController.SetHeadlightMaterial))]
    [HarmonyPrefix]
    static bool VehicleController_Pre_SetHeadlightMaterial(VehicleController __instance, bool __runOriginal, bool on)
    {
        if (!__runOriginal)
            return false;

        if (__instance is not HaulerController vehicle)
            return true;

        vehicle.SetHeadlightMaterial(on);
        return false;
    }

    [HarmonyPatch(nameof(VehicleController.SpringDriverSeatLocalClient))]
    [HarmonyPrefix]
    static bool VehicleController_Pre_SpringDriverSeatLocalClient(VehicleController __instance, bool __runOriginal)
    {
        if (!__runOriginal)
            return false;

        if (__instance is not HaulerController)
            return true;

        return false;
    }

    [HarmonyPatch(nameof(VehicleController.SetRadioOnLocalClient))]
    [HarmonyPrefix]
    static bool VehicleController_Pre_SetRadioOnLocalClient(VehicleController __instance, bool __runOriginal, bool on, bool setClip)
    {
        if (!__runOriginal)
            return false;

        if (__instance is not HaulerController vehicle)
            return true;

        vehicle.SetRadioOnLocalClient(on, setClip);
        return false;
    }

    [HarmonyPatch(nameof(VehicleController.SwitchRadio))]
    [HarmonyPrefix]
    static bool VehicleController_Pre_SwitchRadio(VehicleController __instance, bool __runOriginal)
    {
        if (!__runOriginal)
            return false;

        if (__instance is not HaulerController vehicle)
            return true;

        vehicle.SwitchRadio();
        return false;
    }

    [HarmonyPatch(nameof(VehicleController.ChangeRadioStation))]
    [HarmonyPrefix]
    static bool VehicleController_Pre_ChangeRadioStation(VehicleController __instance, bool __runOriginal)
    {
        if (!__runOriginal)
            return false;

        if (__instance is not HaulerController vehicle)
            return true;

        vehicle.ChangeRadioStation();
        return false;
    }

    [HarmonyPatch(nameof(VehicleController.StartTryCarIgnition))]
    [HarmonyPrefix]
    static bool VehicleController_Pre_StartTryCarIgnition(VehicleController __instance, bool __runOriginal)
    {
        if (!__runOriginal)
            return false;

        if (__instance is not HaulerController vehicle)
            return true;

        vehicle.StartTryCarIgnition();
        return false;
    }

    [HarmonyPatch(nameof(VehicleController.CancelTryCarIgnition))]
    [HarmonyPrefix]
    static bool VehicleController_Pre_CancelTryCarIgnition(VehicleController __instance, bool __runOriginal)
    {
        if (!__runOriginal)
            return false;

        if (__instance is not HaulerController vehicle)
            return true;

        vehicle.CancelTryCarIgnition();
        return false;
    }

    [HarmonyPatch(nameof(VehicleController.RemoveKeyFromIgnition))]
    [HarmonyPrefix]
    static bool VehicleController_Pre_RemoveKeyFromIgnition(VehicleController __instance, bool __runOriginal)
    {
        if (!__runOriginal)
            return false;

        if (__instance is not HaulerController vehicle)
            return true;

        vehicle.RemoveKeyFromIgnition();
        return false;
    }
}