using CompanyHauler.Patches;
using CompanyHauler.Utils;
using GameNetcodeStuff;
using ScandalsTweaks.Scripts;

namespace CompanyHauler.Scripts;

public class HaulerSeatAnimator : VehicleSeatAnimator
{
    public HaulerController vehicleController = null!;

    public override void OnPlayerLeaveGame()
    {
        if (seatTrigger == vehicleController.driverSeatTrigger) vehicleController.OnDriverLeaveGameServerRpc((int)seatTrigger.playerScriptInSpecialAnimation.playerClientId);
        else if (seatTrigger == vehicleController.passengerSeatTrigger) vehicleController.OnPassengerLeaveGameRpc((int)seatTrigger.playerScriptInSpecialAnimation.playerClientId);
        else if (seatTrigger == vehicleController.backLeftPassengerSeatTrigger) vehicleController.OnBackLeftPassengerLeaveGameRpc((int)seatTrigger.playerScriptInSpecialAnimation.playerClientId);
        else if (seatTrigger == vehicleController.backPassengerSeatTrigger) vehicleController.OnMiddlePassengerLeaveGameRpc((int)seatTrigger.playerScriptInSpecialAnimation.playerClientId);
        else if (seatTrigger == vehicleController.backRightPassengerSeatTrigger) vehicleController.OnBackRightPassengerLeaveGameRpc((int)seatTrigger.playerScriptInSpecialAnimation.playerClientId);
        else return;
    }

    public override void ResetPlayerData(PlayerControllerB player)
    {
        player.ladderCameraHorizontal = 0f;
    }

    public override void SetPlayerSeated(bool setSeated, PlayerControllerB localPlayer)
    {
        PlayerUtils.isSeatedInPickup = setSeated;
    }
}
