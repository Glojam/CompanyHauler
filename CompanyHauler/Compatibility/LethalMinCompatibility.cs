using HarmonyLib;
using System;
using UnityEngine;
using System.Runtime.CompilerServices;
using LethalMin;
using CompanyHauler.Scripts;

namespace CompanyHauler.Compatibility;

/// <summary>
///  Available from BrutalCompanyMinus, licensed under MIT licence.
///  Source: https://github.com/Sylkadi/BrutalCompanyMinus

///  Available from BrutalCompanyMinusExtraReborn, licensed under GNU General Public License.
///  Source: https://github.com/TheSoftDiamond/BrutalCompanyMinusExtraReborn
/// </summary>

public static class LethalMinCompatibility
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void PatchAllCompatibilityMethods(Harmony harmony)
    {
        ApplyPikminPatch(harmony);
    }

    [HarmonyPrefix]
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void ApplyPikminPatch(Harmony harmony)
    {
        var vehicleCollisionMethod = AccessTools.Method(typeof(HaulerCollisionTrigger), nameof(HaulerCollisionTrigger.OnTriggerEnter));
        var prefixvehicleCollisionMethod = AccessTools.Method(typeof(LethalMinCompatibility), nameof(OnTriggerEnter_Prefix));

        var pikminControllerMethod = AccessTools.Method(typeof(PikminVehicleController), nameof(PikminVehicleController.InitializeReferences));
        var prefixPikminControllerMethod = AccessTools.Method(typeof(LethalMinCompatibility), nameof(InitializeReferences_Prefix));

        harmony.Patch(vehicleCollisionMethod, prefix: new HarmonyMethod(prefixvehicleCollisionMethod));
        harmony.Patch(pikminControllerMethod, prefix: new HarmonyMethod(prefixPikminControllerMethod));
    }

    public static bool InitializeReferences_Prefix(PikminVehicleController __instance)
    {
        if (__instance.TryGetComponent<HaulerController>(out var controller))
        {
            __instance.controller = controller;
            __instance.PointsRegion = controller.collisionTrigger.insideTruckNavMeshBounds;
            __instance.PikminCheckRegion = controller.physicsRegion.itemDropCollider;

            __instance.PikminWarpPoint = new GameObject("Pikmin Warp Point").transform;
            __instance.PikminWarpPoint.SetParent(__instance.transform);
            __instance.PikminWarpPoint.localPosition = new Vector3(0f, -2.18f, -7.33f);
            __instance.PikminWarpPoint.localScale = new Vector3(1f, 1f, 1f);

            __instance.OriginalWTLocalPosition = __instance.PikminWarpPoint.localPosition;

            return false;
        }
        return true;
    }

    public static bool OnTriggerEnter_Prefix(HaulerCollisionTrigger __instance, Collider other)
    {
        try
        {
            if (other.gameObject.CompareTag("Enemy") && other.gameObject.TryGetComponent<PikminCollisionDetect>(out _))
            {
                return false;
            }
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError(string.Format("Error in HaulerCollisionTrigger.OnTriggerEnterPrefix: {0}", e));
            return true;
        }
        return true;
    }
}
