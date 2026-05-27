using CompanyHauler.Utils;
using GameNetcodeStuff;
using UnityEngine;

namespace CompanyHauler.Scripts;

// An extended physics region, allowing the ability to still read whether the player is "within the physics regions bounds" even while the truck is past
// the tipping angle, also includes some minor optimisations over the base physics region.
public class HaulerPhysicsRegion : PlayerPhysicsRegion
{
    public HaulerController haulerController = null!;

    private bool removePlayerFromZoneNextFrame;
    private float checkZoneInterval;

    private bool addedRegionToList;

    public bool playerInZone;
    public bool isRegionActive;

    public bool parentPlayerBodies;
    public bool setPlayerParent;

    // notes:
    // playerInZone is always when the player is standing within the region.
    // hasLocalPlayer is for the "main physics region" when it's actually active and the local player is parented to it.

    // disablePhysicsRegion doesn't need to be used anywhere else other than IsPhysicsRegionActive, as it's a "master" to disable the region.
    // isRegionActive is weird, but if it is true, and setPlayerParent is true, it will attach to the player to it, if it is true but setPlayerParent is false, or if it is false, it will not
    // attach the player to it.


    private new void OnDestroy()
    {
        disablePhysicsRegion = true;
        TryRemovePhysicsRegionFromList();
        for (int i = 0; i < StartOfRound.Instance?.allPlayerScripts.Length; i++)
        {
            if (StartOfRound.Instance.allPlayerScripts[i].transform.parent == physicsTransform)
            {
                StartOfRound.Instance.allPlayerScripts[i].transform.SetParent(null);
                Debug.Log($"Hauler: Player {i} setting parent null since physics region was destroyed");
            }
        }
        if (!allowDroppingItems || itemDropCollider == null) return;
        GrabbableObject[] componentsInChildren = physicsTransform.GetComponentsInChildren<GrabbableObject>();
        for (int j = 0; j < componentsInChildren.Length; j++)
        {
            if (RoundManager.Instance.mapPropsContainer != null)
            {
                componentsInChildren[j].transform.SetParent(RoundManager.Instance.mapPropsContainer.transform, worldPositionStays: true);
            }
            else
            {
                componentsInChildren[j].transform.SetParent(null, worldPositionStays: true);
            }
            if (!componentsInChildren[j].isHeld)
            {
                componentsInChildren[j].FallToGround();
            }
        }
    }

    private new void OnTriggerStay(Collider other)
    {
        if (disablePhysicsRegion)
        {
            return;
        }
        PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
        if (localPlayer == null)
        {
            return;
        }
        if (parentPlayerBodies && other.gameObject.layer == 20)
        {
            PlayerControllerB playerBody = null!;
            if (other.gameObject.TryGetComponent<DeadBodyInfo>(out var bodyInfo))
            {
                playerBody = bodyInfo.playerScript;
            }
            if (playerBody != null && playerBody.deadBody != null && 
                !playerBody.deadBody.isParentedToPhysicsRegion && playerBody.deadBody.physicsParent != physicsTransform)
            {
                playerBody.deadBody.SetPhysicsParent(physicsTransform, physicsCollider);
            }
        }
        if (other.gameObject != localPlayer.gameObject)
        {
            return;
        }
        if (isRegionActive)
        {
            hasLocalPlayer = true;
            removePlayerNextFrame = false;
            checkInterval = 0f;
        }
        TryAddPhysicsRegionToList();
        if (VehicleUtils.IsPlayerSeatedInPickup()) return;
        playerInZone = true;
        removePlayerFromZoneNextFrame = false;
        checkZoneInterval = 0f;
    }

    private new bool IsPhysicsRegionActive()
    {
        return Vector3.Angle(transform.up, Vector3.up) <= maxTippingAngle;
    }

    private void TryRemovePhysicsRegionFromList()
    {
        if (!setPlayerParent)
            return;

        addedRegionToList = false;
        if (StartOfRound.Instance?.CurrentPlayerPhysicsRegions != null && 
            StartOfRound.Instance.CurrentPlayerPhysicsRegions.Contains(this))
        {
            StartOfRound.Instance.CurrentPlayerPhysicsRegions.Remove(this);
        }
    }

    private void TryAddPhysicsRegionToList()
    {
        if (!setPlayerParent || addedRegionToList || !isRegionActive)
            return;

        addedRegionToList = true;
        if (StartOfRound.Instance?.CurrentPlayerPhysicsRegions != null &&
            !StartOfRound.Instance.CurrentPlayerPhysicsRegions.Contains(this))
        {
            StartOfRound.Instance.CurrentPlayerPhysicsRegions.Add(this);
        }
    }

    private new void Update()
    {
        if (disablePhysicsRegion)
        {
            hasLocalPlayer = false;
            playerInZone = false;
            TryRemovePhysicsRegionFromList();
            return;
        }
        isRegionActive = IsPhysicsRegionActive();
        SetRegion(ref checkInterval, ref removePlayerNextFrame, ref hasLocalPlayer, true);
        SetRegion(ref checkZoneInterval, ref removePlayerFromZoneNextFrame, ref playerInZone, false);
    }

    private void SetRegion(ref float checksInterval, ref bool nextFrame, ref bool setPlayer, bool ignoreSeated)
    {
        if (!ignoreSeated && VehicleUtils.IsPlayerSeatedInPickup())
        {
            setPlayer = true;
            nextFrame = false;
            checksInterval = 0f;
            return;
        }
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
        if (setPlayerParent) TryRemovePhysicsRegionFromList();
    }
}
