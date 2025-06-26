using GameNetcodeStuff;
using UnityEngine;

namespace CompanyHauler.Scripts;

public class SunroofSnipper : MonoBehaviour
{
    public AudioSource snipAudio;

    public AudioClip snipClip;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            PlayerControllerB component = other.gameObject.GetComponent<PlayerControllerB>();
            if (component == null) { return; }

            if (component == GameNetworkManager.Instance.localPlayerController)
            {
                component.KillPlayer(Vector3.up * 5f, spawnBody: true, CauseOfDeath.Snipped, 7);
                snipAudio.PlayOneShot(snipClip);
            }
        }
    }
}