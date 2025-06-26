using System.Collections.Generic;
using System.Reflection.Emit;
using CompanyHauler.Scripts;
using GameNetcodeStuff;
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
            hauler.ChimeAudio.Play();
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
            hauler.ReplaceGearshiftAnimLocalClient();
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
            hauler.ReturnGearshiftAnimLocalClient();
        }
    }
}

// Custom passenger sit anim replacement (add override)
[HarmonyPatch(typeof(VehicleController))]
[HarmonyPatch(nameof(VehicleController.SetPassengerInCar))]
public static class SetPassengerInCarPatch
{
    static void Postfix(VehicleController __instance)
    {
        if (__instance is HaulerController hauler)
        {
            hauler.ReplacePassengerAnimLocalClient();
        }
    }
}

// Custom passenger sit anim replacement (undo override)
[HarmonyPatch(typeof(VehicleController))]
[HarmonyPatch(nameof(VehicleController.OnPassengerExit))]
public static class OnPassengerExitPatch
{
    static void Prefix(VehicleController __instance)
    {
        if (__instance is HaulerController hauler)
        {
            hauler.ReturnPassengerAnimLocalClient();
        }
    }
}

// Set the headlight material for the two other LOD meshes
// This is a base game oversight. I could easily patch this on Cruiser here, but this is not a cruiser improvements mod.
[HarmonyPatch(typeof(VehicleController))]
[HarmonyPatch(nameof(VehicleController.SetHeadlightMaterial))]
public static class SetHeadlightMaterialPatch
{
    static void Postfix(bool on, VehicleController __instance)
    {
        if (__instance is HaulerController hauler)
        {
            Material material = ((!on) ? hauler.headlightsOffMat : hauler.headlightsOnMat);
            Material[] sharedMaterials = hauler.mainBodyMesh.sharedMaterials;
            sharedMaterials[1] = material;
            hauler.lod1Mesh.sharedMaterials = sharedMaterials;
            hauler.lod2Mesh.sharedMaterials = sharedMaterials;
        }
    }
}

// Custom gearshift anim replacement (undo override)
[HarmonyPatch(typeof(VehicleController))]
[HarmonyPatch(nameof(VehicleController.DestroyCar))]
public static class DestroyCarPatch
{
    static void Postfix(VehicleController __instance)
    {
        if (__instance is HaulerController hauler)
        {
            foreach (GameObject rip in hauler.haulerObjectsToDestroy) { rip.SetActive(false); }
        }
    }
}

// Transpiler that allows Hauler drivers' animator to play the column shifter clip even if loking forward
[HarmonyPatch(typeof(VehicleController))]
[HarmonyPatch(nameof(VehicleController.Update))]
public static class VehicleControllerUpdatePatch
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator).Start();

        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(GameNetworkManager), "get_Instance")),
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(GameNetworkManager), "localPlayerController")),
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PlayerControllerB), "ladderCameraHorizontal")),
            new CodeMatch(OpCodes.Ldc_R4, 52f),
            new CodeMatch(ins => ins.opcode == OpCodes.Ble_Un || ins.opcode == OpCodes.Blt_Un_S)
        );

        if (!matcher.IsValid)
        {
            CompanyHauler.Logger.LogDebug("Matching is not valid. Transpilation failed.");
            return instructions;
        }

        matcher.Advance(5); // move to instruction after the branch

        // Create a label to mark the start of the IF body
        Label ifBodyStartLabel = generator.DefineLabel();
        matcher.SetInstruction(matcher.Instruction.WithLabels(ifBodyStartLabel));

        // Do skipping logic before the matched code
        matcher.Advance(-5);

        matcher.Insert(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Isinst, typeof(HaulerController)),
            new CodeInstruction(OpCodes.Brtrue_S, ifBodyStartLabel) // If HaulerController, go directly to if-body
        );

        return matcher.InstructionEnumeration();
    }
}