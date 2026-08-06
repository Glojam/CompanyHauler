using CompanyHauler.Utils;
using GameNetcodeStuff;
using ScandalLib.Scripts;

namespace CompanyHauler.Scripts;

public class HaulerSeatAnimator : VehicleSeatAnimator
{
    public HaulerController vehicleController = null!;

    public override void OnPlayerLeaveGame()
    {
        if (seatTrigger == vehicleController.driverSeatTrigger) vehicleController.OnDriverLeaveGameServerRpc((int)seatTrigger.playerScriptInSpecialAnimation.playerClientId);
        else if (seatTrigger == vehicleController.passengerSeatTrigger) vehicleController.OnPassengerLeaveGameRpc((int)seatTrigger.playerScriptInSpecialAnimation.playerClientId);
        else if (seatTrigger == vehicleController.backLeftPassengerSeatTrigger) vehicleController.OnBackLeftPassengerLeaveGameRpc((int)seatTrigger.playerScriptInSpecialAnimation.playerClientId);
        else if (seatTrigger == vehicleController.backPassengerSeatTrigger) vehicleController.OnBackMiddlePassengerLeaveGameRpc((int)seatTrigger.playerScriptInSpecialAnimation.playerClientId);
        else if (seatTrigger == vehicleController.backRightPassengerSeatTrigger) vehicleController.OnBackRightPassengerLeaveGameRpc((int)seatTrigger.playerScriptInSpecialAnimation.playerClientId);
        else return;
    }

    public override void ResetPlayerData(PlayerControllerB player)
    {
        player.ladderCameraHorizontal = 0f;
    }
}
