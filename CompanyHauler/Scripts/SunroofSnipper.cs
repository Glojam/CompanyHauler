using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CompanyHauler.Scripts;

public class SunroofSnipper : MonoBehaviour
{
    public AudioSource snipAudio = null!;
    public AudioClip snipClip = null!;

    public void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Player"))
            return;

        PlayerControllerB playerCollidedWith = other.gameObject.GetComponent<PlayerControllerB>();
        if (playerCollidedWith == null ||
            playerCollidedWith.isPlayerDead ||
            !playerCollidedWith.isPlayerControlled ||
            playerCollidedWith != GameNetworkManager.Instance.localPlayerController)
            return;

        if (playerCollidedWith.inVehicleAnimation || playerCollidedWith.isCrouching)
            return;

        PlayDeathClipOnLocalClient();
        playerCollidedWith.KillPlayer(Vector3.up * 5f, spawnBody: true, CauseOfDeath.Snipping, 7, default, false);
    }

    private void PlayDeathClipOnLocalClient()
    {
        snipAudio.PlayOneShot(snipClip);
        PlayDeathClipRpc();
    }

    [Rpc(SendTo.NotMe, RequireOwnership = false)]
    public void PlayDeathClipRpc()
    {
        snipAudio.PlayOneShot(snipClip);
    }
}