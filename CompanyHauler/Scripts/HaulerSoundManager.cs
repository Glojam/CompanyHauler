using GameNetcodeStuff;
using UnityEngine;
using ScandalsTweaks.Utils;
using CompanyHauler.Patches;

namespace CompanyHauler.Scripts;

public class HaulerSoundManager : MonoBehaviour
{
    public HaulerController controller = null!;
    public float checkInterval;

    public void LateUpdate()
    {
        if (controller == null)
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
        bool perspectiveInCab = perspectiveData?.playerRidingInPickupCab == true;
        controller.roofRainAudioActive = GlobalUtilities.IsItRaining() && perspectiveInCab;
    }
}