using CompanyHauler.Scripts;
using GameNetcodeStuff;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace CompanyHauler.Utils;
public static class VehicleUtils
{
    internal static HaulerController pickupController = null!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // internal is like public, but only we can use this!
    internal static bool IsPlayerLocal(PlayerControllerB? player)
    {
        return player != null && player == GameNetworkManager.Instance.localPlayerController;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPlayerNearPickup(PlayerControllerB? playerScript, HaulerController? pickupScript)
    {
        return playerScript != null && pickupScript != null && Vector3.Distance(playerScript.transform.position, pickupScript.mainRigidbody.position) <= 10f;
    }
}