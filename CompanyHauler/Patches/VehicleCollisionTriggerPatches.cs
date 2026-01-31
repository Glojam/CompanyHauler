using CompanyHauler.Scripts;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace CompanyHauler.Patches;

[HarmonyPatch(typeof(VehicleCollisionTrigger))]
internal class VehicleCollisionTriggerPatches
{
    /// <summary>
    ///  Available from CruiserImproved, licensed under MIT License.
    ///  Source: https://github.com/digger1213/CruiserImproved/blob/main/source/Patches/VehicleCollisionTrigger.cs
    /// </summary>
    /// 
    [HarmonyPatch("OnTriggerEnter")]
    [HarmonyPrefix]
    static bool OnTriggerEnter_Prefix(VehicleCollisionTrigger __instance, Collider other)
    {
        if (__instance.mainScript is not HaulerController)
        {
            return true;
        }
        if (!__instance.mainScript.hasBeenSpawned)
        {
            return true;
        }
        if (__instance.mainScript.magnetedToShip && __instance.mainScript.magnetTime > 0.8f)
        {
            return true;
        }

        PlayerControllerB player;
        // Patch hitting players standing on/in the Hauler
        if (other.CompareTag("Player") && (player = other.GetComponentInParent<PlayerControllerB>()))
        {
            Transform physicsTransform = __instance.mainScript.physicsRegion.physicsTransform;
            if (player.physicsParent == physicsTransform || player.overridePhysicsParent == physicsTransform)
            {
                return false;
            }
            return true;
        }
        EnemyAICollisionDetect enemyAI;
        if (other.CompareTag("Enemy") && (enemyAI = other.GetComponentInParent<EnemyAICollisionDetect>()))
        {
            if (!enemyAI.mainScript || !enemyAI.mainScript.agent || !enemyAI.mainScript.agent.navMeshOwner)
            {
                return true;
            }

            // Prevent hitting entities inside the truck
            Behaviour navmeshOn = (Behaviour)enemyAI.mainScript.agent.navMeshOwner;
            if (navmeshOn.transform.IsChildOf(__instance.mainScript.transform))
            {
                return false;
            }

            // Prevent hitting and bouncing off unkillable small entities (bees, ghost girl, earth leviathan). This matches vanilla behaviour with those entities and makes more sense
            if (!enemyAI.mainScript.enemyType.canDie && enemyAI.mainScript.enemyType.SizeLimit == NavSizeLimit.NoLimit) return false;

            return true;
        }
        return true;
    }
}