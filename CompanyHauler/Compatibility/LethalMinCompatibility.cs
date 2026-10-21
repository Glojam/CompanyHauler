using HarmonyLib;
using System;
using UnityEngine;
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
    [HarmonyPatch(typeof(PikminVehicleController), nameof(PikminVehicleController.InitializeReferences))]
    [HarmonyPrefix]
    public static bool PikminVehicleController_Pre_InitializeReferences(PikminVehicleController __instance)
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

    [HarmonyPatch(typeof(HaulerCollisionTrigger), nameof(HaulerCollisionTrigger.OnTriggerEnter))]
    [HarmonyPrefix]
    public static bool HaulerCollisionTrigger_Pre_OnTriggerEnter(HaulerCollisionTrigger __instance, Collider other)
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
            Plugin.LogError(string.Format("Error in `HaulerCollisionTrigger.OnTriggerEnter`: {0}", e));
            return true;
        }
        return true;
    }
}
