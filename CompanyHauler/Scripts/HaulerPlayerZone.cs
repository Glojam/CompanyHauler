using CompanyHauler.Utils;
using GameNetcodeStuff;
using UnityEngine;

namespace CompanyHauler.Scripts;

public class HaulerPlayerZone : MonoBehaviour
{
    public HaulerController haulerController = null!;

    public Transform physicsTransform = null!;
    public Collider physicsCollider = null!;

    private float playerInsideInterval;
    private bool playerInsideThisFrame;

    private bool removePlayerFromZoneNextFrame;
    private float checkZoneInterval;

    public bool unsetInZoneWhileSeated;
    public bool setInZoneWhileSeated;

    public bool playerInZone;
    public bool disableZone;


    public void OnEnable()
    {
        if (setInZoneWhileSeated && unsetInZoneWhileSeated)
        {
            Plugin.LogWarning("'Set in zone' and 'Unset in zone' are set simulteanously! this will cause issues");
            Plugin.LogWarning("Fallback to set behaviour 'Set zone --> not seated'");
            setInZoneWhileSeated = false;
            unsetInZoneWhileSeated = true;
        }
        else if (!setInZoneWhileSeated && !unsetInZoneWhileSeated)
        {
            Plugin.LogWarning("'Set in zone' and 'Unset in zone' are unset simulteanously! this will cause issues");
            Plugin.LogWarning("Fallback to set behaviour 'Set zone --> not seated'");
            setInZoneWhileSeated = false;
            unsetInZoneWhileSeated = true;
        }
    }

    public void OnDestroy()
    {
        disableZone = true;
    }

    public void OnTriggerStay(Collider other)
    {
        if (disableZone)
        {
            return;
        }
        PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
        if (localPlayer == null)
        {
            return;
        }
        if (!other.gameObject.CompareTag("Player"))
        {
            return;
        }
        if (other.gameObject != localPlayer.gameObject)
        {
            return;
        }
        playerInsideThisFrame = true;
        playerInsideInterval = 0f;
    }


    public void Update()
    {
        if (disableZone)
        {
            physicsCollider.enabled = false;
            return;
        }
        if (VehicleUtils.IsPlayerSeatedInPickup(GameNetworkManager.Instance.localPlayerController, haulerController))
        {
            if (setInZoneWhileSeated)
            {
                playerInZone = true;
            }
            else if (unsetInZoneWhileSeated)
            {
                playerInZone = false;
            }
            removePlayerFromZoneNextFrame = false;
            checkZoneInterval = 0f;
            return;
        }
        UpdatePlayerZone();
        SetPlayerZone(ref checkZoneInterval, ref removePlayerFromZoneNextFrame, ref playerInZone);
    }

    public void UpdatePlayerZone()
    {
        if (!playerInsideThisFrame)
        {
            return;
        }
        if (playerInsideThisFrame)
        {
            SetPlayerZoneActive();
        }
        if (playerInsideInterval <= 0.15f)
        {
            playerInsideInterval += Time.deltaTime;
            return;
        }
        playerInsideThisFrame = false;
        playerInsideInterval = 0f;
    }

    private void SetPlayerZoneActive()
    {
        checkZoneInterval = 0f;
        removePlayerFromZoneNextFrame = false;
        playerInZone = true;
    }

    public void SetPlayerZone(ref float checkInterval, ref bool removeNextFrame, ref bool hasPlayer)
    {
        if (!hasPlayer)
        {
            return;
        }
        if (checkInterval <= 0.15f)
        {
            checkInterval += Time.deltaTime;
            return;
        }
        if (!removeNextFrame)
        {
            removeNextFrame = true;
            return;
        }
        removeNextFrame = false;
        checkInterval = 0f;
        hasPlayer = false;
    }
}
