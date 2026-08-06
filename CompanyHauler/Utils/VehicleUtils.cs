using CompanyHauler.Patches;
using CompanyHauler.Scripts;
using GameNetcodeStuff;
using UnityEngine;

namespace CompanyHauler.Utils;
public static class VehicleUtils
{
    internal static HaulerController pickupController = null!;

    public static bool IsEnemyInPickup(EnemyAI enemyScript, BoxCollider navMeshBounds)
    {
        return navMeshBounds.ClosestPoint(enemyScript.transform.position) == enemyScript.transform.position ||
               navMeshBounds.ClosestPoint(enemyScript.agent.destination) == enemyScript.agent.destination;
    }

    public static bool IsPlayerSeatedInPickup(PlayerControllerB playerScript, HaulerController pickupScript)
    {
        return playerScript != null && playerScript?.overridePhysicsParent == pickupScript?.transform;
    }

    public static bool IsPlayerInPickupBounds(PlayerControllerB playerScript, HaulerController pickupScript, bool usePlayerData = false)
    {
        return usePlayerData
            ? PlayerControllerBPatches.playerData[playerScript].playerRidingOnPickup
            : pickupScript.vehicleZone.playerInZone;
    }

    public static bool IsPlayerInPickupCab(PlayerControllerB playerScript, HaulerController pickupScript, bool usePlayerData = false)
    {
        return usePlayerData
            ? PlayerControllerBPatches.playerData[playerScript].playerRidingInPickupCab
            : pickupScript.vehicleCabZone.playerInZone;
    }

    public static bool IsPlayerOnOutsideOfPickup(PlayerControllerB playerScript, HaulerController pickupScript, bool usePlayerData = false)
    {
        return IsPlayerInPickupBounds(playerScript, pickupScript, usePlayerData) &&
               !IsPlayerInPickupCab(playerScript, pickupScript, usePlayerData) &&
               !IsPlayerSeatedInPickup(playerScript, pickupScript);
    }

    public static bool IsPickupCabEnclosed(HaulerController pickupScript)
    {
        return !pickupScript.windshieldBroken &&
               !pickupScript.driverSideDoor.boolValue && !pickupScript.passengerSideDoor.boolValue &&
               !pickupScript.backLeftDoor.boolValue && !pickupScript.backRightDoor.boolValue &&
               !pickupScript.frontLeftWindow.isWindowOpen && !pickupScript.frontRightWindow.isWindowOpen &&
               !pickupScript.backLeftWindow.isWindowOpen && !pickupScript.backRightWindow.isWindowOpen &&
               !pickupScript.sunroofOpen;
    }

    public static bool IsPickupMovingFastEnough(HaulerController pickupScript, float threshold)
    {
        return pickupScript.averageVelocity.magnitude > threshold;
    }

    public static bool IsSeatedPlayerProtected(PlayerControllerB playerScript, HaulerController pickupScript, bool checkSunroof = false, bool checkWindows = false, bool windshieldCheck = false, bool velocityCheck = false, float velocityMagnitude = 0f)
    {
        if (velocityCheck && IsPickupMovingFastEnough(pickupScript, velocityMagnitude))
            return true;

        bool driverSeat = playerScript == pickupScript.currentDriver;
        bool passengerSeat = playerScript == pickupScript.currentPassenger;
        bool backLeftSeat = playerScript == pickupScript.currentBackLeftPassenger;
        bool backMiddleSeat = playerScript == pickupScript.currentBackMiddlePassenger;
        bool backRightSeat = playerScript == pickupScript.currentBackRightPassenger;

        bool frontSeatOccupant = driverSeat || passengerSeat;

        bool windshieldBroken = pickupScript.windshieldBroken;
        bool sunroofOpen = pickupScript.sunroofOpen;

        if ((checkSunroof && sunroofOpen) ||
            (frontSeatOccupant && windshieldCheck && windshieldBroken))
            return false;

        bool frontLeftSideOpen =
            pickupScript.driverSideDoor.boolValue ||
            (checkWindows && pickupScript.frontLeftWindow.isWindowOpen);

        bool frontRightSideOpen =
            pickupScript.passengerSideDoor.boolValue ||
            (checkWindows && pickupScript.frontRightWindow.isWindowOpen);

        bool backLeftSideOpen =
            pickupScript.backLeftDoor.boolValue ||
            (checkWindows && pickupScript.backLeftWindow.isWindowOpen);

        bool backRightSideOpen =
            pickupScript.backRightDoor.boolValue ||
            (checkWindows && pickupScript.backRightWindow.isWindowOpen);

        if (driverSeat && frontLeftSideOpen)
            return false;

        if (passengerSeat && frontRightSideOpen)
            return false;

        if (backLeftSeat && backLeftSideOpen)
            return false;

        if (backMiddleSeat && (backLeftSideOpen || backRightSideOpen))
            return false;

        if (backRightSeat && backRightSideOpen)
            return false;

        return true;
    }

    public static bool IsPlayerProtectedByPickup(PlayerControllerB playerScript, HaulerController pickupScript, bool checkSunroof = false, bool checkWindows = false, bool windshieldCheck = false, bool velocityCheck = false, float velocityMagnitude = 0f)
    {
        if (pickupScript.carDestroyed)
            return false;

        if (velocityCheck && IsPickupMovingFastEnough(pickupScript, velocityMagnitude))
            return true;

        bool playerInCab = IsPlayerInPickupCab(playerScript, pickupScript);

        bool windshieldBroken = pickupScript.windshieldBroken;
        bool sunroofOpen = pickupScript.sunroofOpen;

        bool doorsOpen =
            pickupScript.driverSideDoor.boolValue ||
            pickupScript.passengerSideDoor.boolValue ||
            pickupScript.backLeftDoor.boolValue ||
            pickupScript.backRightDoor.boolValue;

        bool windowsOpen =
            pickupScript.frontLeftWindow.isWindowOpen ||
            pickupScript.frontRightWindow.isWindowOpen ||
            pickupScript.backLeftWindow.isWindowOpen ||
            pickupScript.backRightWindow.isWindowOpen;

        if (playerInCab &&
            (doorsOpen ||
             (checkSunroof && sunroofOpen) ||
             (checkWindows && windowsOpen) ||
             (windshieldCheck && windshieldBroken)))
            return false;

        if (IsPlayerInPickupBounds(playerScript, pickupScript) &&
            !playerInCab)
            return false;

        return true;
    }

    public static bool IsPlayerNearPickup(PlayerControllerB playerScript, HaulerController pickupScript)
    {
        Vector3 vehicleTransform = pickupScript.mainRigidbody.position;
        Vector3 playerTransform = playerScript.transform.position;

        return Vector3.Distance(playerTransform, vehicleTransform) <= 10f;
    }
}