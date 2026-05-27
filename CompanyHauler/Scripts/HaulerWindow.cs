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
            Plugin.Logger.LogWarning("Hauler: No window audio set for this window!");
            return;
        }
        if (windowOpen == null || windowClose == null)
        {
            Plugin.Logger.LogWarning("Hauler: No window audio clips set for this window!");
            return;
        }
        windowAudio.PlayOneShot(windowOpen);
    }

    public void PlayWindowCloseAudio()
    {
        if (windowAudio == null)
        {
            Plugin.Logger.LogWarning("Hauler: No window audio set for this window!");
            return;
        }
        if (windowOpen == null || windowClose == null)
        {
            Plugin.Logger.LogWarning("Hauler: No window audio clips set for this window!");
            return;
        }
        windowAudio.PlayOneShot(windowClose);
    }
}
