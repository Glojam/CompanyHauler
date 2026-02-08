using CompanyHauler.Scripts;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace CompanyHauler.Patches;

[HarmonyPatch(typeof(VehicleController))]
public static class VehicleControllerPatches
{
    // Cabin light override injection
    [HarmonyPatch("SetFrontCabinLightOn")]
    [HarmonyPrefix]
    static void SetFrontCabinLightOn_Prefix(VehicleController __instance, bool setOn)
    {
        HaulerController? hauler = __instance as HaulerController;
        if (hauler == null) return;

        Material[] materials = hauler.mainBodyMesh.materials;
        materials[3] = setOn ? hauler.cabinLightOnMat : hauler.cabinLightOffMat;
        hauler.mainBodyMesh.materials = materials;
        hauler.cablightToggle = !hauler.cablightToggle;
    }

    // Prevent the driver from enabling collision to seated players, which is a problem for the Hauler
    [HarmonyPatch("EnableVehicleCollisionForAllPlayers")]
    [HarmonyPrefix]
    static bool EnableVehicleCollisionForAllPlayers_Prefix(VehicleController __instance)
    {
        if (__instance is HaulerController hauler)
            return false;

        return true;
    }

    // Prevent the driver from disabling collision to unseated players, which is a problem for the Hauler
    [HarmonyPatch("DisableVehicleCollisionForAllPlayers")]
    [HarmonyPrefix]
    static bool DisableVehicleCollisionForAllPlayers_Prefix(VehicleController __instance)
    {
        if (__instance is HaulerController hauler)
            return false;

        return true;
    }

    // Custom gearshift anim replacement (add override)
    [HarmonyPatch("TakeControlOfVehicle")]
    [HarmonyPrefix]
    static void TakeControlOfVehicle_Postfix(VehicleController __instance)
    {
        if (__instance is HaulerController hauler)
        {
            hauler.ReplaceGearshiftAnimLocalClient();
        }
    }

    // Driver-only collision change + Custom gearshift anim replacement (undo override)
    [HarmonyPatch("LoseControlOfVehicle")]
    [HarmonyPrefix]
    static void LoseControlOfVehicle_Prefix(VehicleController __instance)
    {
        if (__instance is HaulerController hauler)
        {
            if (hauler.currentDriver == GameNetworkManager.Instance.localPlayerController)
            {
                hauler.SetVehicleCollisionForPlayer(setEnabled: true, GameNetworkManager.Instance.localPlayerController);
            }
            hauler.ReturnGearshiftAnimLocalClient();
        }
    }

    [HarmonyPatch("StartMagneting")]
    [HarmonyPrefix]
    static bool StartMagneting_Prefix(VehicleController __instance, bool __runOriginal)
    {
        if (!__runOriginal)
            return false;

        if (__instance is not HaulerController hauler)
            return true;

        hauler.StartMagneting();
        return false;
    }

    [HarmonyPatch("CollectItemsInTruck")]
    [HarmonyPrefix]
    static bool CollectItemsInTruck_Prefix(VehicleController __instance, bool __runOriginal)
    {
        if (!__runOriginal)
            return false;

        if (__instance is not HaulerController hauler)
            return true;

        hauler.CollectItemsInHauler();
        return false;
    }

    // Redirect function, too lazy for a transpiler - Scandal
    [HarmonyPatch("CarReactToObstacle")]
    [HarmonyPrefix]
    static bool CarReactToObstacle_Prefix(VehicleController __instance, Vector3 vel, Vector3 position, Vector3 impulse, CarObstacleType type, float obstacleSize, EnemyAI enemyScript = null!, bool dealDamage = true)
    {
        if (__instance is HaulerController hauler)
        {
            hauler.CarReactToObstacle(vel, position, impulse, type, obstacleSize, enemyScript, dealDamage);
            return false;
        }
        return true;
    }

    // Custom passenger sit anim replacement (add override)
    [HarmonyPatch("SetPassengerInCar")]
    [HarmonyPrefix]
    static void SetPassengerInCar_Postfix(VehicleController __instance, PlayerControllerB player)
    {
        if (__instance is HaulerController hauler)
        {
            CompanyHauler.Logger.LogDebug($"true; {hauler == null}");
            //hauler.ReplacePassengerAnimLocalClient(); // TODO feature
        }
    }

    // Custom passenger sit anim replacement (undo override)
    [HarmonyPatch("OnPassengerExit")]
    [HarmonyPrefix]
    static void OnPassengerExit_Prefix(VehicleController __instance)
    {
        if (__instance is HaulerController hauler)
        {
            CompanyHauler.Logger.LogDebug($"true; {hauler == null}");
            //hauler.ReturnPassengerAnimLocalClient(); // TODO feature
        }
    }

    // Set the headlight material for the two other LOD meshes
    // This is a base game oversight. I could easily patch this on Cruiser here, but this is not a cruiser improvements mod.
    [HarmonyPatch("SetHeadlightMaterial")]
    [HarmonyPrefix]
    static void SetHeadlightMaterial_Postfix(VehicleController __instance, bool on)
    {
        if (__instance is HaulerController hauler)
        {
            Material[] materials = hauler.mainBodyMesh.materials;
            materials[1] = on ? hauler.headlightsOnMat : hauler.headlightsOffMat;
            hauler.lod1Mesh.materials = materials;
            hauler.lod2Mesh.materials = materials;
        }
    }

    /// <summary>
    ///  Available from CruiserImproved, licensed under MIT License.
    ///  Source: https://github.com/digger1213/CruiserImproved/blob/main/source/Patches/VehicleController.cs
    /// </summary>

    // Fix radio not changing station for clients
    [HarmonyPatch("SetRadioStationClientRpc")]
    [HarmonyPostfix]
    static void SetRadioStationClientRpc_Postfix(VehicleController __instance)
    {
        if (__instance is HaulerController hauler)
        {
            __instance.SetRadioOnLocalClient(true, true);
        }
    }

    // Set radio time for consistency among owner & clients
    [HarmonyPatch("SetRadioOnLocalClient")]
    [HarmonyPostfix]
    static void SetRadioOnLocalClient_Postfix(VehicleController __instance, bool on, bool setClip)
    {
        if (__instance is HaulerController hauler)
        {
            if (on && setClip)
            {
                hauler.SetRadioTime();
            }
        }
    }

    // Play the door chime when shifting to drive/rev with any of the doors open
    [HarmonyPatch("ShiftToGearClientRpc")]
    [HarmonyPostfix]
    static void ShiftToGearClientRpc_Postfix(VehicleController __instance, int setGear, int playerId)
    {   
        if (__instance is HaulerController hauler)
        {
            if (!hauler.keyIsInIgnition) return;
            if ((hauler.gear == CarGearShift.Drive || hauler.gear == CarGearShift.Reverse) && (hauler.driverSideDoor.boolValue || hauler.passengerSideDoor.boolValue || hauler.BLSideDoor.boolValue || hauler.BRSideDoor.boolValue))
            {
                Debug.Log("Playing chime");
                hauler.ChimeAudio.PlayOneShot(hauler.chimeSoundCritical);
            }
        }
    }

    // Fix radio not turning on for clients unless the channel is changed
    [HarmonyPatch("SwitchRadio")]
    [HarmonyPostfix]
    static void SwitchRadio_Postfix(VehicleController __instance)
    {
        if (__instance is HaulerController hauler)
        {
            if (__instance.radioOn)
            {
                __instance.SetRadioStationServerRpc(__instance.currentRadioClip, (int)Mathf.Round(__instance.radioSignalQuality));
                hauler.SetRadioTime();
            }
        }
    }

    [HarmonyPatch("SetVehicleAudioProperties")]
    [HarmonyPrefix]
    static void SetVehicleAudioProperties_Prefix(VehicleController __instance, AudioSource audio, ref bool audioActive)
    {
        if (audioActive && ((audio == __instance.extremeStressAudio && __instance.magnetedToShip) || ((audio == __instance.rollingAudio || audio == __instance.skiddingAudio) && (__instance.magnetedToShip || (!__instance.FrontLeftWheel.isGrounded && !__instance.FrontRightWheel.isGrounded && !__instance.BackLeftWheel.isGrounded && !__instance.BackRightWheel.isGrounded)))))
            audioActive = false;
    }

    // Fix modded damage triggers from causing unexpected destroy behavior
    [HarmonyPatch("DestroyCar")]
    [HarmonyPrefix]
    static bool DestroyCar_Prefix(VehicleController __instance, bool __runOriginal)
    {
        if (!__runOriginal)
            return false;

        if (__instance is not HaulerController vehicle)
            return true;

        vehicle.DestroyCar();
        return false;
    }

    // Fix modded damage triggers from causing unexpected damage behavior
    [HarmonyPatch("DealPermanentDamage")]
    [HarmonyPrefix]
    static bool DealPermanentDamage_Prefix(VehicleController __instance, bool __runOriginal, int damageAmount, Vector3 damagePosition = default(Vector3))
    {
        if (!__runOriginal)
            return false;

        if (__instance is not HaulerController vehicle)
            return true;

        vehicle.DealPermanentDamage(damageAmount, damagePosition);
        return false;
    }
}