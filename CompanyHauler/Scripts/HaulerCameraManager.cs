using GameNetcodeStuff;
using UnityEngine;
using CompanyHauler.Utils;
using CompanyHauler.Scripts;

namespace CompanyHauler.Behaviour;

public class HaulerCameraManager : MonoBehaviour
{
    public HaulerController mainController = null!;

    public MeshRenderer[] meshMirrors = null!;
    public Camera[] cameraMirrors = null!;

    private bool disableRendering;
    private bool renderingEnabled;

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
        {
            return;
        }
        if (cameraMirrors == null || meshMirrors == null)
        {
            return;
        }
        if (!mainController.IsSpawned || mainController.destroyNextFrame || mainController.carDestroyed)
        {
            return;
        }
        PlayerControllerB playerController = GameNetworkManager.Instance.localPlayerController;
        if (playerController == null)
        {
            return;
        }
        disableRendering = !UserConfig.haulerMirror.Value || !mainController.IsPlayerSeatedInCar();
        if (disableRendering)
        {
            renderingEnabled = false;
            for (var i = 0; i < cameraMirrors.Length; i++)
            {
                if (cameraMirrors[i] == null || meshMirrors[i] == null)
                    continue;
                cameraMirrors[i].enabled = false;
                meshMirrors[i].enabled = false;
            }
            return;
        }
        if (!renderingEnabled)
        {
            renderingEnabled = true;
            for (var i = 0; i < cameraMirrors.Length; i++)
            {
                if (cameraMirrors[i] == null || meshMirrors[i] == null)
                    continue;
                cameraMirrors[i].enabled = false;
                meshMirrors[i].enabled = true;
            }
        }
        var activeCamCount = 0;
        for (var i = 0; i < cameraMirrors.Length; i++)
        {
            if (cameraMirrors[i] == null || meshMirrors[i] == null)
                continue;
            cameraMirrors[i].enabled = false;
            if (meshMirrors[i].IsVisibleToPlayersLocalCamera(playerController.gameplayCamera))
                activeCamCount++;
        }
        elapsed += Time.deltaTime;
        if (elapsed < 1f / cameraFramerate)
            return;
        elapsed = 0f;
        var renderCountIncrement = (float)camerasToRenderPerFrame * activeCamCount / cameraMirrors.Length;
        cameraRenderCountRemainder += renderCountIncrement;
        var stopIndex = (nextCameraToRender + cameraMirrors.Length - 1) % cameraMirrors.Length;
        while (cameraRenderCountRemainder >= 1f)
        {
            bool validCamera = meshMirrors[nextCameraToRender] != null && cameraMirrors[nextCameraToRender] != null;
            if (validCamera && meshMirrors[nextCameraToRender].IsVisibleToPlayersLocalCamera(playerController.gameplayCamera))
            {
                meshMirrors[nextCameraToRender].enabled = true;
                cameraMirrors[nextCameraToRender].enabled = true;
                cameraRenderCountRemainder--;
            }
            nextCameraToRender = (nextCameraToRender + 1) % cameraMirrors.Length;
            if (nextCameraToRender == stopIndex)
                break;
        }
    }
}