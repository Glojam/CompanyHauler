using UnityEngine;

namespace CompanyHauler.Scripts;

public class HaulerAnimationEvents : MonoBehaviour
{
    public HaulerController haulerController = null!;

    public void OnSunroofOpen()
    {
        haulerController.sunroofOpen = true;
    }

    public void OnSunroofClose()
    {
        haulerController.sunroofOpen = false;
    }
}
