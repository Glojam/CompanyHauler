using GameNetcodeStuff;
using UnityEngine;

namespace CompanyHauler.Scripts;

public class HaulerPhysicsRegion : PlayerPhysicsRegion
{
    public HaulerController haulerController = null!;

    private float playerInsideInterval;
    private bool playerInsideThisFrame;

    private bool addedRegionToList;

    public bool playerInZone;
    public bool isRegionActive;

    public bool parentPlayerBodies;

    public new void OnDestroy()
    {
        disablePhysicsRegion = true;
        if (StartOfRound.Instance.CurrentPlayerPhysicsRegions.Contains(this))
        {
            StartOfRound.Instance.CurrentPlayerPhysicsRegions.Remove(this);
        }
        for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
        {
            PlayerControllerB playerController = StartOfRound.Instance.allPlayerScripts[i];
            if (playerController.transform.parent == physicsTransform)
            {
                Transform playerTransform = playerController.isInElevator ? playerController.playersManager.elevatorTransform : playerController.playersManager.playersContainer;
                playerController.transform.SetParent(playerTransform);
                Plugin.LogWarning($"Player {i} setting parent since physics region was destroyed");
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

    public new void OnTriggerStay(Collider other)
    {
        if (disablePhysicsRegion)
        {
            return;
        }
        PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
        if (localPlayer == null)
        {
            hasLocalPlayer = false;
            playerInZone = false;
            return;
        }
        string tag = other.gameObject.tag;
        if (parentPlayerBodies && tag.StartsWith("PlayerRagdoll"))
        {
            PlayerControllerB playerControllerB = null!;
            if (other.gameObject.TryGetComponent<DeadBodyInfo>(out var deadBodyInfo))
            {
                playerControllerB = deadBodyInfo.playerScript;
            }
            if (playerControllerB != null && playerControllerB.deadBody != null &&
                !playerControllerB.deadBody.isParentedToPhysicsRegion)
            {
                playerControllerB.deadBody.SetPhysicsParent(physicsTransform, physicsCollider);
            }
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

    public void SetPhysicsRegionActive()
    {
        if (!isRegionActive)
        {
            if (addedRegionToList)
                TryRemovePhysicsRegionFromList();
            return;
        }
        checkInterval = 0f;
        removePlayerNextFrame = false;
        TryAddPhysicsRegionToList();
    }

    public void SetPlayerZoneActive()
    {
        playerInZone = true;
    }

    public new bool IsPhysicsRegionActive()
    {
        return Vector3.Angle(transform.up, Vector3.up) <= maxTippingAngle && !disablePhysicsRegion;
    }

    public void TryRemovePhysicsRegionFromList()
    {
        StartOfRound.Instance?.CurrentPlayerPhysicsRegions?.Remove(this);
        addedRegionToList = false;
        hasLocalPlayer = false;
        removePlayerNextFrame = false;
        checkInterval = 0f;
    }

    public void TryAddPhysicsRegionToList()
    {
        if (addedRegionToList || !isRegionActive)
            return;

        if (StartOfRound.Instance.CurrentPlayerPhysicsRegions != null &&
            !StartOfRound.Instance.CurrentPlayerPhysicsRegions.Contains(this))
        {
            StartOfRound.Instance.CurrentPlayerPhysicsRegions.Add(this);
            addedRegionToList = true;
            hasLocalPlayer = true;
        }
    }

    public new void Update()
    {
        if (disablePhysicsRegion)
        {
            physicsCollider.enabled = false;
            return;
        }
        isRegionActive = IsPhysicsRegionActive();
        UpdatePhysicsRegion();
        SetPhysicsRegionAndZone(ref checkInterval, ref removePlayerNextFrame, ref hasLocalPlayer, true);
    }

    public void UpdatePhysicsRegion()
    {
        if (!playerInsideThisFrame)
        {
            if (playerInZone)
            {
                playerInZone = false;
            }
            return;
        }
        if (playerInsideThisFrame)
        {
            SetPhysicsRegionActive();
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

    public void SetPhysicsRegionAndZone(ref float interval, ref bool removeNextFrame, ref bool hasPlayer, bool removeFromPhysicsList = false)
    {
        if (!hasPlayer)
        {
            return;
        }
        if (interval <= 0.15f)
        {
            interval += Time.deltaTime;
            return;
        }
        if (!removeNextFrame)
        {
            removeNextFrame = true;
            return;
        }
        removeNextFrame = false;
        interval = 0f;
        hasPlayer = false;
        if (removeFromPhysicsList)
            TryRemovePhysicsRegionFromList();
    }
}
