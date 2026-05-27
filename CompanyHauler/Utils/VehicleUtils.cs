using CompanyHauler.Scripts;
using GameNetcodeStuff;
using UnityEngine;

namespace CompanyHauler.Utils;
public static class VehicleUtils
{
    public static bool IsEnemyInPickup(EnemyAI enemyScript, HaulerController pickupController)
    {
        if ((pickupController.collisionTrigger.insideTruckNavMeshBounds.ClosestPoint(enemyScript.transform.position) == enemyScript.transform.position) ||
            (pickupController.collisionTrigger.insideTruckNavMeshBounds.ClosestPoint(enemyScript.agent.destination) == enemyScript.agent.destination))
            return true;
        return false;
    }

    public static bool IsPlayerSeatedInPickup()
    {
        return PlayerUtils.isSeatedInPickup;
    }

    public static bool IsPlayerInPickupBounds(HaulerController pickupController)
    {
        return pickupController.vehicleZone.playerInZone;
    }

    public static bool IsPlayerInPickupCab(HaulerController pickupController)
    {
        return pickupController.vehicleCabZone.playerInZone;
    }

    public static bool IsSeatedPlayerProtected(PlayerControllerB playerController, HaulerController pickupController, bool checkSunroof = false, bool checkWindows = false, bool windshieldCheck = false, bool velocityCheck = false, float velocityMagnitude = 0f)
    {
        float avgVel = pickupController.averageVelocity.magnitude;

        if (velocityCheck && avgVel > velocityMagnitude)
            return true;

        bool windshieldBroken = pickupController.windshieldBroken;
        bool sunroofOpen = pickupController.sunroofOpen;
        bool isFrontSeatOccupant = playerController == pickupController.currentDriver ||
                                   playerController == pickupController.currentPassenger;

        if ((checkSunroof && sunroofOpen) || (isFrontSeatOccupant && windshieldCheck && windshieldBroken))
            return false;

        bool frontLeftSideOpen = pickupController.driverSideDoor.boolValue || (checkWindows && pickupController.frontLeftWindow.isWindowOpen);
        bool frontRightSideOpen = pickupController.passengerSideDoor.boolValue || (checkWindows && pickupController.frontRightWindow.isWindowOpen);

        bool backLeftSideOpen = pickupController.backLeftDoor.boolValue || (checkWindows && pickupController.backLeftWindow.isWindowOpen);
        bool backRightSideOpen = pickupController.backRightDoor.boolValue || (checkWindows && pickupController.backRightWindow.isWindowOpen);

        if ((playerController == pickupController.currentDriver && frontLeftSideOpen) || 
            (playerController == pickupController.currentPassenger && frontRightSideOpen))
            return false;
        if ((playerController == pickupController.currentBackLeftPassenger && backLeftSideOpen) ||
            (playerController == pickupController.currentMiddlePassenger && (backLeftSideOpen || backRightSideOpen)) ||
            (playerController == pickupController.currentBackRightPassenger && backRightSideOpen))
            return false;

        return true;
    }

    public static bool IsPlayerProtectedByPickup(PlayerControllerB playerController, HaulerController pickupController, bool checkSunroof = false, bool checkWindows = false, bool windshieldCheck = false, bool velocityCheck = false, float velocityMagnitude = 0f)
    {
        if (pickupController.carDestroyed)
            return false;

        float avgVel = pickupController.averageVelocity.magnitude;

        if (velocityCheck && avgVel > velocityMagnitude)
            return true;

        bool windshieldBroken = pickupController.windshieldBroken;
        bool sunroofOpen = pickupController.sunroofOpen;

        bool doorsOpen = pickupController.driverSideDoor.boolValue ||
                         pickupController.passengerSideDoor.boolValue ||
                         pickupController.backLeftDoor.boolValue ||
                         pickupController.backRightDoor.boolValue;

        bool windowsOpen = pickupController.frontLeftWindow.isWindowOpen ||
                           pickupController.frontRightWindow.isWindowOpen ||
                           pickupController.backLeftWindow.isWindowOpen ||
                           pickupController.backRightWindow.isWindowOpen;

        bool tailgateOpen = pickupController.tailgateOpen;

        if (IsPlayerInPickupCab(pickupController) && 
            doorsOpen || 
            (checkSunroof && sunroofOpen) || 
            (checkWindows && windowsOpen) || 
            (windshieldCheck && windshieldBroken))
            return false;
        else if (IsPlayerInPickupBounds(pickupController) &&
                !IsPlayerInPickupCab(pickupController))
            return false;

        return true;
    }

    public static bool IsPlayerNearPickup(PlayerControllerB playerController, HaulerController pickupController)
    {
        Vector3 vehicleTransform = pickupController.mainRigidbody.position;
        Vector3 playerTransform = playerController.transform.position;

        if (Vector3.Distance(playerTransform, vehicleTransform) > 10f)
            return false;

        return true;
    }
}