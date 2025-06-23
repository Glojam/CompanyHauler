using CompanyHauler.Scripts;
using HarmonyLib;
using UnityEngine;

namespace CompanyHauler.Patches;

// Cabin light override injection
[HarmonyPatch(typeof(VehicleController))]
[HarmonyPatch(nameof(VehicleController.SetFrontCabinLightOn))]
public static class CabinLightPatch
{
    static void Prefix(bool setOn, VehicleController __instance)
    {
        HaulerController? hauler = __instance as HaulerController;
        if (hauler != null)
        {
            Material[] materials = hauler.mainBodyMesh.materials;
            materials[3] = setOn ? hauler.cabinLightOnMat : hauler.cabinLightOffMat;
            hauler.mainBodyMesh.materials = materials;
            hauler.cablightToggle = !hauler.cablightToggle;
        }
    }
}

// Chime after start
[HarmonyPatch(typeof(VehicleController))]
[HarmonyPatch(nameof(VehicleController.SetIgnition))]
public static class SetIgnitionPatch
{
    static void Prefix(bool started, VehicleController __instance)
    {
        HaulerController? hauler = __instance as HaulerController;
        if (hauler != null && started && started != __instance.ignitionStarted)
        {
            hauler.ChimeAudio.Stop();
            hauler.ChimeAudio.PlayOneShot(hauler.chimeSound);
        }
    }
}

// Custom gearshift anim replacement (add override)
[HarmonyPatch(typeof(VehicleController))]
[HarmonyPatch(nameof(VehicleController.TakeControlOfVehicle))]
public static class TakeControlOfVehiclePatch
{
    static void Postfix(VehicleController __instance)
    {
        if (__instance is HaulerController hauler)
        {
            hauler.ReplaceGearshiftAnim();
        }
    }
}

// Custom gearshift anim replacement (undo override)
[HarmonyPatch(typeof(VehicleController))]
[HarmonyPatch(nameof(VehicleController.LoseControlOfVehicle))]
public static class LoseControlOfVehiclePatch
{
    static void Prefix(VehicleController __instance)
    {
        if (__instance is HaulerController hauler)
        {
            hauler.ReturnGearshiftAnim();
        }
    }
}