using GameNetcodeStuff;
using ScandalLib.Interfaces;
using ScandalsTweaks.Scripts;

namespace CompanyHauler.Scripts;

public class HaulerEnemyManager : SVehicleEnemyManager
{
    public HaulerController vehicleController = null!;

    public override bool ShouldAllowSightThroughVehicle(EnemyAI enemyScript, PlayerControllerB playerScript)
    {
        return vehicleController != null && playerScript is IPlayerManager playerInterface && (vehicleController.IsPlayerSeated(playerScript) || playerInterface.CurrentPlayerZone == vehicleController.vehicleCabZone);
    }
}
