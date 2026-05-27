using CompanyHauler.Scripts;
using CompanyHauler.Utils;
using HarmonyLib;

namespace CompanyHauler.Patches;

[HarmonyPatch(typeof(HUDManager))]
public static class HUDManagerPatches
{
    [HarmonyPatch(nameof(HUDManager.HelmetCondensationDrops))]
    [HarmonyPostfix]
    private static void HelmetCondensationDrops_Postfix(HUDManager __instance)
    {
        HaulerController controller = References.pickupController;
        if (controller == null)
            return;

        if (VehicleUtils.IsPlayerInPickupCab(controller))
        {
            __instance.increaseHelmetCondensation = false;
        }
    }
}
