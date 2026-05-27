using CompanyHauler.Utils;
using GameNetcodeStuff;
using UnityEngine;

namespace CompanyHauler.Scripts;

// A stripped down version of the HaulerPhysicsRegion, this is just used for the cabin space to determine whether a player is simply just "standing in the cab"
// Ignores seated players.
public class HaulerPlayerZone : MonoBehaviour
{
    public HaulerController haulerController = null!;

    public Transform physicsTransform = null!;
    public Collider physicsCollider = null!;

    private bool removePlayerFromZoneNextFrame;
    private float checkZoneInterval;

    public bool unsetInZoneWhileSeated; // unused, but will implement for other vehicles later
    public bool setInZoneWhileSeated;

    public bool playerInZone;
    public bool disableZone;


    private void OnEnable()
    {
        if (setInZoneWhileSeated && unsetInZoneWhileSeated)
        {
            Plugin.Logger.LogWarning("Hauler: 'Set in zone' and 'Unset in zone' are set simulteanously! this will cause issues!");
            Plugin.Logger.LogWarning("Hauler: Fallback to set behaviour 'Set zone --> not seated'");
            setInZoneWhileSeated = false;
            unsetInZoneWhileSeated = true;
        }
        else if (!setInZoneWhileSeated && !unsetInZoneWhileSeated)
        {
            Plugin.Logger.LogWarning("Hauler: 'Set in zone' and 'Unset in zone' are unset simulteanously! this will cause issues!");
            Plugin.Logger.LogWarning("Hauler: Fallback to set behaviour 'Set zone --> not seated'");
            setInZoneWhileSeated = false;
            unsetInZoneWhileSeated = true;
        }
    }

    private void OnDestroy()
    {
        disableZone = true;
    }

    private void OnTriggerStay(Collider other)
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
        if (VehicleUtils.IsPlayerSeatedInPickup())
        {
            return;
        }
        if (other.gameObject != localPlayer.gameObject)
        {
            return;
        }
        playerInZone = true;
        removePlayerFromZoneNextFrame = false;
        checkZoneInterval = 0f;
    }


    private void Update()
    {
        if (disableZone)
        {
            playerInZone = false;
            return;
        }
        if (VehicleUtils.IsPlayerSeatedInPickup())
        {
            if (setInZoneWhileSeated)
            {
                playerInZone = true;
                removePlayerFromZoneNextFrame = false;
                checkZoneInterval = 0f;
                return;
            }
            else if (unsetInZoneWhileSeated)
            {
                playerInZone = false;
                removePlayerFromZoneNextFrame = false;
                checkZoneInterval = 0f;
                return;
            }
        }
        SetRemoval(ref checkZoneInterval, ref removePlayerFromZoneNextFrame, ref playerInZone);
    }

    private void SetRemoval(ref float checksInterval, ref bool nextFrame, ref bool setPlayer)
    {
        if (!setPlayer)
        {
            return;
        }
        if (checksInterval <= 0.15f)
        {
            checksInterval += Time.deltaTime;
            return;
        }
        if (!nextFrame)
        {
            nextFrame = true;
            return;
        }
        nextFrame = false;
        checksInterval = 0f;
        setPlayer = false;
    }
}
