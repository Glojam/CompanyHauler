using GameNetcodeStuff;
using UnityEngine;
using CompanyHauler.Utils;
using CompanyHauler.Scripts;

namespace CompanyHauler.Behaviour;

public class HaulerCameraManager : MonoBehaviour
{
    public HaulerController mainController = null!;

    public MeshRenderer leftMirrorMesh = null!;
    public MeshRenderer rightMirrorMesh = null!;

    public MeshRenderer[] meshMirrors = null!;
    public Camera[] cameraMirrors = null!;

    public float cameraFramerate;
    public int camerasToRenderPerFrame = 1;

    private float elapsed;
    private int nextCameraToRender = 0;
    private float cameraRenderCountRemainder = 0f;


    /// <summary>
    ///  Available from Black Mesa, licensed under MIT License.
    ///  Source: https://github.com/PlasteredCrab/BlackMesa/commit/59738a8107bc7c6846a175fcd4420b4da80483d2
    /// </summary>
    public void LateUpdate()
    {
        if (mainController == null)
            return;

        if (!mainController.IsSpawned || mainController.carDestroyed)
            return;

        PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
        if (player == null)
            return;

        for (var j = 0; j < cameraMirrors.Length; j++)
        {
            if (cameraMirrors[j] == null)
                continue;
            cameraMirrors[j].enabled = false;
        }

        if (!HaulerConfig.haulerMirror.Value ||
            !PlayerUtils.isSeatedInPickup)
        {
            leftMirrorMesh.enabled = false;
            rightMirrorMesh.enabled = false;
            return;
        }

        elapsed += Time.deltaTime;
        if (elapsed < 1f / cameraFramerate)
            return;
        elapsed = 0f;

        leftMirrorMesh.enabled = true;
        rightMirrorMesh.enabled = true;

        var activeCamCount = 0;
        for (var i = 0; i < cameraMirrors.Length; i++)
        {
            if (!meshMirrors[i].IsVisibleToPlayersLocalCamera(player.gameplayCamera))
                continue;
            activeCamCount++;
        }

        var renderCountIncrement = (float)camerasToRenderPerFrame * activeCamCount / cameraMirrors.Length;
        cameraRenderCountRemainder += renderCountIncrement;

        var stopIndex = (nextCameraToRender + cameraMirrors.Length - 1) % cameraMirrors.Length;
        while (cameraRenderCountRemainder >= 0)
        {
            if (meshMirrors[nextCameraToRender].IsVisibleToPlayersLocalCamera(player.gameplayCamera))
            {
                cameraMirrors[nextCameraToRender].enabled = true;
                cameraRenderCountRemainder--;
            }
            nextCameraToRender = (nextCameraToRender + 1) % cameraMirrors.Length;
            if (nextCameraToRender == stopIndex)
                break;
        }
    }
}