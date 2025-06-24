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


