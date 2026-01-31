using CompanyHauler.Utils;
using HarmonyLib;

namespace CompanyHauler.Patches;

[HarmonyPatch(typeof(ItemDropship))]
public class ItemDropshipPatches
{
    [HarmonyPatch("Start")]
    [HarmonyPrefix]
    private static void Start_Prefix(ItemDropship __instance)
    {
        if (__instance == null) return;

        if (References.itemShip == null)
            References.itemShip = __instance;
    }
}
