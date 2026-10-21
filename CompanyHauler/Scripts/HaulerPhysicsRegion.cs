using GameNetcodeStuff;
using ScandalLib.Interfaces;
using ScandalLib.Scripts;

namespace CompanyHauler.Scripts;

public class HaulerPhysicsRegion : ExtendedPhysicsRegion
{
    public HaulerController vehicleController = null!;

    // must null check before calling
    internal bool IsLocalPlayerInShelter()
    {
        return vehicleController.vehicleCabZone.playerInZone && vehicleController.IsCabinEnclosed();
    }

    public override bool IsTargetPlayerInVehicle(PlayerControllerB playerScript, EnemyAI enemyAi)
    {
        if (playerScript is not IPlayerManager playerInterface)
            return false;
        switch (enemyAi)
        {
            case GiantKiwiAI:
                return vehicleController.IsPlayerSeated(playerScript) ||
                    ((vehicleController.IsPlayerInCabin(playerScript) && vehicleController.IsCabinEnclosed()) ||
                     vehicleController.ontopOfTruckCollider.ClosestPoint(playerScript.transform.position) == playerScript.transform.position);
            default:
                return false;
        }
    }

    public override bool IsPlayerProtectedFromEnemy(PlayerControllerB playerScript, EnemyAI enemyAi)
    {
        switch (enemyAi)
        {
            case BaboonBirdAI:
                return IsPlayerProtectedByVehicle(playerScript, enemyAi, checkWindows: true, checkWindshield: true, checkSunroof: false, velocityCheck: true, velocityThreshold: 8f);
            case ForestGiantAI:
                return IsPlayerProtectedByVehicle(playerScript, enemyAi, checkWindows: true, checkWindshield: true, checkSunroof: true, velocityCheck: true, velocityThreshold: 10f);
            case GiantKiwiAI:
                return IsPlayerProtectedByVehicle(playerScript, enemyAi, checkWindows: true, checkWindshield: true, checkSunroof: true, velocityCheck: true, velocityThreshold: 12f);
            case MaskedPlayerEnemy:
                return IsPlayerProtectedByVehicle(playerScript, enemyAi, checkWindows: false, checkWindshield: false, checkSunroof: false, velocityCheck: true, velocityThreshold: 4f);
            case MouthDogAI:
                return IsPlayerProtectedByVehicle(playerScript, enemyAi, checkWindows: false, checkWindshield: true, checkSunroof: false, velocityCheck: true, velocityThreshold: 12f);
            case PumaAI:
                return IsPlayerProtectedByVehicle(playerScript, enemyAi, checkWindows: true, checkWindshield: true, checkSunroof: false, velocityCheck: true, velocityThreshold: 9f);
            case RadMechAI:
                return IsPlayerProtectedByVehicle(playerScript, enemyAi, checkWindows: true, checkWindshield: true, checkSunroof: true, velocityCheck: true, velocityThreshold: 10f);
            case RedLocustBees:
                return IsPlayerProtectedByVehicle(playerScript, enemyAi, checkWindows: true, checkWindshield: true, checkSunroof: false, velocityCheck: true, velocityThreshold: 8f);
            case SandWormAI:
                return vehicleController.IsPlayerSeated(playerScript) ||
                       vehicleController.IsPlayerInCabin(playerScript);
            case BushWolfEnemy:
                return IsPlayerProtectedByVehicle(playerScript, enemyAi, checkWindows: false, checkWindshield: false, checkSunroof: false, velocityCheck: true, velocityThreshold: 6f);
            default:
                return false;
        }
    }

    internal bool IsPlayerProtectedByVehicle(PlayerControllerB playerScript, EnemyAI enemyAi, bool checkWindows = false, bool checkWindshield = false, bool checkSunroof = false, bool velocityCheck = false, float velocityThreshold = 0f)
    {
        var playerSeated = vehicleController.IsPlayerSeated(playerScript);
        if (playerSeated)
        {
            return IsSeatedPlayerProtected(playerScript, checkWindows, checkWindshield, checkSunroof, velocityCheck, velocityThreshold);
        }
        var playerInCab = vehicleController.IsPlayerInCabin(playerScript);
        if (!playerInCab)
            return false;
        if (velocityCheck)
        {
            float vehicleSpeed = vehicleController.averageVelocity.magnitude;
            if (vehicleSpeed >= velocityThreshold)
                return true;
        }
        return IsPlayerProtectedByPickup(playerScript, checkWindows, checkWindshield, checkSunroof, velocityCheck, velocityThreshold);
    }

    public bool IsSeatedPlayerProtected(PlayerControllerB playerScript, bool checkWindows = false, bool windshieldCheck = false, bool checkSunroof = false, bool velocityCheck = false, float velocityThreshold = 0f)
    {
        float vehicleSpeed = vehicleController.averageVelocity.magnitude;
        if (velocityCheck && vehicleSpeed >= velocityThreshold)
            return true;

        bool driverSeat = playerScript == vehicleController.currentDriver;
        bool passengerSeat = playerScript == vehicleController.currentPassenger;
        bool backLeftSeat = playerScript == vehicleController.currentBackLeftPassenger;
        bool backMiddleSeat = playerScript == vehicleController.currentBackMiddlePassenger;
        bool backRightSeat = playerScript == vehicleController.currentBackRightPassenger;

        bool frontSeatOccupant = driverSeat || passengerSeat;

        bool windshieldBroken = vehicleController.windshieldBroken;
        bool sunroofOpen = vehicleController.sunroofOpen;

        if ((checkSunroof && sunroofOpen) ||
            (frontSeatOccupant && windshieldCheck && windshieldBroken))
            return false;

        bool frontLeftSideOpen =
            vehicleController.driverSideDoor.boolValue ||
            (checkWindows && vehicleController.frontLeftWindow.isWindowOpen);

        bool frontRightSideOpen =
            vehicleController.passengerSideDoor.boolValue ||
            (checkWindows && vehicleController.frontRightWindow.isWindowOpen);

        bool backLeftSideOpen =
            vehicleController.backLeftDoor.boolValue ||
            (checkWindows && vehicleController.backLeftWindow.isWindowOpen);

        bool backRightSideOpen =
            vehicleController.backRightDoor.boolValue ||
            (checkWindows && vehicleController.backRightWindow.isWindowOpen);

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

    public bool IsPlayerProtectedByPickup(PlayerControllerB playerScript, bool checkWindows = false, bool windshieldCheck = false, bool checkSunroof = false, bool velocityCheck = false, float velocityThreshold = 0f)
    {
        if (vehicleController.carDestroyed)
            return false;

        float vehicleSpeed = vehicleController.averageVelocity.magnitude;
        if (velocityCheck && vehicleSpeed >= velocityThreshold)
            return true;

        var playerInCab = vehicleController.IsPlayerInCabin(playerScript);

        bool windshieldBroken = vehicleController.windshieldBroken;
        bool sunroofOpen = vehicleController.sunroofOpen;

        bool doorsOpen =
            vehicleController.driverSideDoor.boolValue ||
            vehicleController.passengerSideDoor.boolValue ||
            vehicleController.backLeftDoor.boolValue ||
            vehicleController.backRightDoor.boolValue;

        bool windowsOpen =
            vehicleController.frontLeftWindow.isWindowOpen ||
            vehicleController.frontRightWindow.isWindowOpen ||
            vehicleController.backLeftWindow.isWindowOpen ||
            vehicleController.backRightWindow.isWindowOpen;

        if (playerInCab &&
            (doorsOpen ||
             (checkSunroof && sunroofOpen) ||
             (checkWindows && windowsOpen) ||
             (windshieldCheck && windshieldBroken)))
            return false;

        if (vehicleController.IsPlayerAttached(playerScript) &&
            !playerInCab)
            return false;

        return true;
    }

    public override bool IsPlayerProtectedFromKnockback(PlayerControllerB playerScript)
    {
        return vehicleController != null && vehicleController.IsPlayerSeated(playerScript);
    }

    public override bool IsLocalPlayerSheltered()
    {
        return vehicleController != null && IsLocalPlayerInShelter();
    }
}