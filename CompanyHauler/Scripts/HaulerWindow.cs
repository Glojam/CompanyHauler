using UnityEngine;

namespace CompanyHauler.Scripts;

public class HaulerWindow : MonoBehaviour
{
    public HaulerController haulerController = null!;

    public AudioSource windowAudio = null!;
    public AudioClip windowOpen = null!;
    public AudioClip windowClose = null!;
    public bool isWindowOpen;

    public void OnWindowOpen()
    {
        isWindowOpen = true;
    }

    public void OnWindowClose()
    {
        isWindowOpen = false;
    }

    public void PlayWindowOpenAudio()
    {
        if (windowAudio == null)
        {
            Plugin.LogWarning($"No window audio source set for {gameObject.name}");
            return;
        }
        if (windowOpen == null || windowClose == null)
        {
            Plugin.LogWarning($"No window audio clips set for {gameObject.name}");
            return;
        }
        windowAudio.PlayOneShot(windowOpen);
    }

    public void PlayWindowCloseAudio()
    {
        if (windowAudio == null)
        {
            Plugin.LogWarning($"No window audio source set for {gameObject.name}");
            return;
        }
        if (windowOpen == null || windowClose == null)
        {
            Plugin.LogWarning($"No window audio clips set for {gameObject.name}");
            return;
        }
        windowAudio.PlayOneShot(windowClose);
    }
}
