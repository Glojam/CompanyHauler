using GameNetcodeStuff;
using UnityEngine;
using System.Linq;
using CompanyHauler.Scripts;

namespace CompanyHauler.Utils;
public static class VehicleUtils
{
    public static bool IsPlayerInTruck(PlayerControllerB player, HaulerController vehicle)
    {
        // variables
        Vector3 playerTransform = player.transform.position;
        Transform playerOverride = player.overridePhysicsParent;
        Collider physicsCollider = vehicle.physicsRegion.gameObject.GetComponent<Collider>();

        // player is the driver
        if (player == vehicle.currentDriver)
            return true;

        // player is the front right passenger
        if (player == vehicle.currentPassenger)
            return true;

        // player is the back left passenger
        if (player == vehicle.currentBL)
            return true;

        // player is the back right passenger
        if (player == vehicle.currentBR)
            return true;

        // player is within the physics regions bounds, and they're not within the cab nor the storage compartment
        //if (playerOverride == null && (physicsCollider.bounds.Contains(playerTransform)) && (!vehicle.storageCompartment.bounds.Contains(playerTransform) && !(vehicle.cabinPoint.bounds.Contains(playerTransform))))
        //    return true;

        // player is within the cabin
        //if (playerOverride == null && vehicle.cabinPoint.bounds.Contains(playerTransform))
        //    return true;

        // player is within the storage compartment
        //if (playerOverride == null && vehicle.storageCompartment.bounds.Contains(playerTransform))
        //    return true;

        return false;
    }

    public static bool IsPlayerProtectedByTruck(PlayerControllerB player, HaulerController vehicle)
    {
        // variables
        bool driverDoorOpen = vehicle.driverSideDoor.boolValue;
        bool passengerDoorOpen = vehicle.passengerSideDoor.boolValue;
        bool backLeftPassengerDoorOpen = vehicle.BLSideDoor.boolValue;
        bool backRightPassengerDoorOpen = vehicle.BRSideDoor.boolValue;

        Vector3 playerTransform = player.transform.position;
        Transform playerOverride = player.overridePhysicsParent;
        Collider physicsCollider = vehicle.physicsRegion.gameObject.GetComponent<Collider>();

        // player is the driver and their door is open
        if (player == vehicle.currentDriver && driverDoorOpen)
            return false;

        // player is the front right passenger and their door is open
        if (player == vehicle.currentPassenger && passengerDoorOpen)
            return false;

        // player is the back left passenger and their door is open
        if (player == vehicle.currentBL && backLeftPassengerDoorOpen)
            return false;

        // player is the back right passenger and their door is open
        if (player == vehicle.currentBR && backRightPassengerDoorOpen)
            return false;

        // player is within the physics regions bounds, and they're not within the cab nor the storage compartment
        //if (playerOverride == null && (physicsCollider.bounds.Contains(playerTransform)) && (!vehicle.storageCompartment.bounds.Contains(playerTransform) && (!vehicle.cabinPoint.bounds.Contains(playerTransform))))
        //    return false;

        // player is within the cabin, and either door is open
        //if (playerOverride == null && vehicle.cabinPoint.bounds.Contains(playerTransform) && (driverDoorOpen || passengerDoorOpen))
        //    return false;

        // player is within the storage compartment, and the back door is open
        //if (playerOverride == null && vehicle.storageCompartment.bounds.Contains(playerTransform) && (backDoorOpen || sideDoorOpen))
        //    return false;

        return true;
    }

    public static bool IsLocalPlayer(this PlayerControllerB player)
    {
        return player == GameNetworkManager.Instance.localPlayerController;
    }
}