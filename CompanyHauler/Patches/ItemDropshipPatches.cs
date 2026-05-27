using CompanyHauler.Utils;
using HarmonyLib;

namespace CompanyHauler.Patches;

[HarmonyPatch(typeof(ItemDropship))]
public static class ItemDropshipPatches
{
    [HarmonyPatch(nameof(ItemDropship.Start))]
    [HarmonyPrefix]
    private static void Start_Prefix(ItemDropship __instance)
    {
        if (__instance == null) 
            return;
        References.itemShip = __instance;
    }
}
