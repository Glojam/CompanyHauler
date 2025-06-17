using CompanyHauler.Scripts;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace CompanyHauler.Patches;

// This patch makes weed killer avoid filling the Hauler with turbo boost. Does not affect cruiser at all.
[HarmonyPatch(typeof(SprayPaintItem))]
[HarmonyPatch(nameof(SprayPaintItem.TrySprayingWeedKillerBottle))]
public static class TrySprayingWeedKillerBottlePatch
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.Start();
        // Match the comparison: if (carHP >= baseCarHP)
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldloc_1),
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(VehicleController), nameof(VehicleController.carHP))),
            new CodeMatch(OpCodes.Ldloc_1),
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(VehicleController), nameof(VehicleController.baseCarHP))),
            new CodeMatch(instruction => instruction.opcode == OpCodes.Blt_S || instruction.opcode == OpCodes.Blt)
        );

        // Move to the branch instruction (which is the last match)
        matcher.Advance(4); // move from Ldloc_1 (start) to the 5th instruction matched
        var branchInstr = matcher.Instruction;

        //Debug.Log($"Found instruction: {branchInstr.opcode}, Operand: {branchInstr.operand}");

        // Ensure it's a branch with a valid label
        if (!(matcher.Instruction.operand is Label ogBranchTarget))
        {
            Debug.Log(matcher.Instruction.operand.ToString());
            throw new System.Exception("Expected a branch label but found null or incorrect type.");
        }

        // Replace the comparison: if ((carHP >= baseCarHP) && (vehicleVontroller as HaulerController == null))
        matcher.Advance(1);

        matcher.Insert(
            new CodeInstruction(OpCodes.Ldloc_1),
            new CodeInstruction(OpCodes.Isinst, typeof(HaulerController)), // Attempt cast to HaulerController
            new CodeInstruction(OpCodes.Ldnull), // Load null
            new CodeInstruction(OpCodes.Ceq), // Compare (is HaulerController == null?)
            new CodeInstruction(OpCodes.Brfalse_S, ogBranchTarget) // If false (indeed HaulerController), skip the if
        );

        return matcher.InstructionEnumeration();
    }
}

