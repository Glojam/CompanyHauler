using HarmonyLib;
using FirstPersonView;
using CompanyHauler.Scripts;

namespace CompanyHauler.Compatibility;

/// <summary>
///  Available from BrutalCompanyMinus, licensed under MIT licence.
///  Source: https://github.com/Sylkadi/BrutalCompanyMinus

///  Available from BrutalCompanyMinusExtraReborn, licensed under GNU General Public License.
///  Source: https://github.com/TheSoftDiamond/BrutalCompanyMinusExtraReborn
/// </summary>
public static class FirstPersonViewCompatibility
{
    [HarmonyPatch(typeof(HaulerController), nameof(HaulerController.SetCarIgnitionKey))]
    [HarmonyPrefix]
    public static void HaulerController_Pre_SetCarKeyEffects(HaulerController __instance, ref bool localUseBodyHands)
    {
        if (!LocalBodyViewController.LocalBodyShown)
            return;
        localUseBodyHands = true;
    }
}