using CompanyHauler.Scripts;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace CompanyHauler.Patches;

// This patch makes weed killer avoid filling the Hauler with turbo boost. Does not affect cruiser at all.
[HarmonyPatch(typeof(SprayPaintItem))]
internal class SprayPaintItemPatches
{
    [HarmonyPatch("TrySprayingWeedKillerBottle")]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> TrySprayingWeedKillerBottle_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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

        // Ensure it's a branch with a valid label
        if (matcher.Instruction.operand is not Label ogBranchTarget)
        {
            CompanyHauler.Logger.LogDebug(matcher.Instruction.operand.ToString() + "; transpilation failed for SprayPaintItemPatch");
            return matcher.InstructionEnumeration();
        }

        // Replace the comparison: if ((carHP >= baseCarHP) && (vehicleController as HaulerController == null))
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