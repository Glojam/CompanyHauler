using GameNetcodeStuff;
using ScandalLib.Scripts;

namespace CompanyHauler.Scripts;

public class HaulerSeatAnimator : VehicleSeatAnimator
{
    public HaulerController vehicleController = null!;

    public override void OnPlayerLeaveGame()
    {
        if (seatTrigger == vehicleController.driverSeatTrigger) vehicleController.OnDriverLeaveGameServerRpc(seatTrigger.playerScriptInSpecialAnimation);
        else if (seatTrigger == vehicleController.passengerSeatTrigger) vehicleController.OnPassengerLeaveGameRpc(seatTrigger.playerScriptInSpecialAnimation);
        else if (seatTrigger == vehicleController.backLeftPassengerSeatTrigger) vehicleController.OnBackLeftPassengerLeaveGameRpc(seatTrigger.playerScriptInSpecialAnimation);
        else if (seatTrigger == vehicleController.backPassengerSeatTrigger) vehicleController.OnBackMiddlePassengerLeaveGameRpc(seatTrigger.playerScriptInSpecialAnimation);
        else if (seatTrigger == vehicleController.backRightPassengerSeatTrigger) vehicleController.OnBackRightPassengerLeaveGameRpc(seatTrigger.playerScriptInSpecialAnimation);
        else return;
    }

    public override void ResetPlayerData(PlayerControllerB player)
    {
        player.ladderCameraHorizontal = 0f;
    }
}
