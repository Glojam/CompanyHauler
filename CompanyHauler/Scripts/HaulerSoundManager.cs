using GameNetcodeStuff;
using UnityEngine;
using CompanyHauler.Patches;
using ScandalsTweaks.Utils;

namespace CompanyHauler.Scripts;

public class HaulerSoundManager : MonoBehaviour
{
    public HaulerController controller = null!;
    public float checkInterval;

    public void Update()
    {
        if (controller == null || !controller.IsSpawned)
            return;

        if (checkInterval < 0.5f)
        {
            checkInterval += Time.deltaTime;
            return;
        }
        checkInterval = 0f;

        PlayerControllerB localPlayerController = GameNetworkManager.Instance.localPlayerController;
        if (localPlayerController == null)
            return;

        PlayerControllerB perspectivePlayer = localPlayerController;
        if (localPlayerController.isPlayerDead && localPlayerController.spectatedPlayerScript != null)
        {
            perspectivePlayer = localPlayerController.spectatedPlayerScript;
        }

        var perspectiveData = PlayerControllerBPatches.playerData[perspectivePlayer];
        bool perspectiveInCab = perspectiveData?.playerRidingInPickupCab ?? false;
        bool perspectiveInVehicle = perspectiveInCab;
        UpdateRainAudio(perspectiveInVehicle);
    }

    public void UpdateRainAudio(bool perspectiveInPickup = false)
    {
        if (controller.carDestroyed)
            return;
        controller.roofRainAudioActive = Utilities.IsItRaining() && perspectiveInPickup;
    }
}