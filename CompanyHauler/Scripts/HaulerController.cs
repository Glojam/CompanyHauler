using GameNetcodeStuff;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace CompanyHauler.Scripts;

public class HaulerController : VehicleController
{
    public bool localPlayerInBLSeat;

    public bool localPlayerInBRSeat;

    public PlayerControllerB currentBL;

    public PlayerControllerB currentBR;

    public InteractTrigger BLSeatTrigger;

    public InteractTrigger BRSeatTrigger;

    public Transform[] BL_ExitPoints;

    public Transform[] BR_ExitPoints;

    public AnimatedObjectTrigger BLSideDoor;

    public AnimatedObjectTrigger BRSideDoor;

    public InteractTrigger BLSideDoorTrigger;

    public InteractTrigger BRSideDoorTrigger;

    public AudioClip chimeSound;

    public AudioClip chimeSoundCritical;

    public AudioSource ChimeAudio;

    public bool doorChimeDebounce = false;

    public bool cablightToggle = false;

    public Material cabinLightOnMat;

    public Material cabinLightOffMat;

    public GameObject screensContainer;

    public TextMeshProUGUI dotMatrix;

    public MeshRenderer leftDial;

    public MeshRenderer rightDial;

    public Transform leftDialTransform;
        
    public Transform rightDialTransform;

    public GameObject checkEngineLight;

    public GameObject tractionControlLight;

    public Material dialOnMat;

    public Material dialOffMat;

    public Image leftDialTickmarks;
        
    public Image rightDialTickmarks;

    private bool lastKeyInIgnition = false;

    private bool checkEngineWasAlarmed = false;

    private bool tractionLightWasAlarmed = false;

    // BACK-LEFT PASSENGER METHODS //////////////////////////

    public void OnBLExit()
    {
        BLSeatTrigger.interactable = true;
        localPlayerInBLSeat = false;
        currentBL = null;
        SetVehicleCollisionForPlayer(setEnabled: true, GameNetworkManager.Instance.localPlayerController);
        BL_LeaveVehicleServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId, GameNetworkManager.Instance.localPlayerController.transform.position);
    }

    public void ExitBLSideSeat()
    {
        if (localPlayerInBLSeat)
        {
            int num = CanExitBackSeats(isLeftSeat: true);
            if (num != -1)
            {
                GameNetworkManager.Instance.localPlayerController.TeleportPlayer(BL_ExitPoints[num].position);
            }
            else
            {
                GameNetworkManager.Instance.localPlayerController.TeleportPlayer(BL_ExitPoints[1].position);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void BL_LeaveVehicleServerRpc(int playerId, Vector3 exitPoint)
    {
        BL_LeaveVehicleClientRpc(playerId, exitPoint);
    }

    [ClientRpc]
    public void BL_LeaveVehicleClientRpc(int playerId, Vector3 exitPoint)
    {
        PlayerControllerB playerControllerB = StartOfRound.Instance.allPlayerScripts[playerId];
        if (!(playerControllerB == GameNetworkManager.Instance.localPlayerController))
        {
            playerControllerB.TeleportPlayer(exitPoint);
            currentBL = null;
            if (!base.IsOwner)
            {
                SetVehicleCollisionForPlayer(setEnabled: true, GameNetworkManager.Instance.localPlayerController);
            }
            BLSeatTrigger.interactable = true;
        }
    }

    public void SetBLPassengerInCar(PlayerControllerB player)
    {
        if (BLSideDoor.boolValue)
        {
            BLSideDoor.SetBoolOnClientOnly(setTo: false);
        }
        if (player == GameNetworkManager.Instance.localPlayerController)
        {
            localPlayerInBLSeat = true;
        }
        else
        {
            BLSeatTrigger.interactable = false;
        }
        currentBL = player;
        SetVehicleCollisionForPlayerServerRPC(false);
    }

    // BACK-RIGHT PASSENGER METHODS //////////////////////////

    public void OnBRExit()
    {
        BRSeatTrigger.interactable = true;
        localPlayerInBRSeat = false;
        currentBR = null;
        SetVehicleCollisionForPlayer(setEnabled: true, GameNetworkManager.Instance.localPlayerController);
        BR_LeaveVehicleServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId, GameNetworkManager.Instance.localPlayerController.transform.position);
    }

    public void ExitBRSideSeat()
    {
        if (localPlayerInBRSeat)
        {
            int num = CanExitBackSeats(isLeftSeat: false);
            if (num != -1)
            {
                GameNetworkManager.Instance.localPlayerController.TeleportPlayer(BR_ExitPoints[num].position);
            }
            else
            {
                GameNetworkManager.Instance.localPlayerController.TeleportPlayer(BR_ExitPoints[1].position);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void BR_LeaveVehicleServerRpc(int playerId, Vector3 exitPoint)
    {
        BR_LeaveVehicleClientRpc(playerId, exitPoint);
    }

    [ClientRpc]
    public void BR_LeaveVehicleClientRpc(int playerId, Vector3 exitPoint)
    {
        PlayerControllerB playerControllerB = StartOfRound.Instance.allPlayerScripts[playerId];
        if (!(playerControllerB == GameNetworkManager.Instance.localPlayerController))
        {
            playerControllerB.TeleportPlayer(exitPoint);
            currentBR = null;
            if (!base.IsOwner)
            {
                SetVehicleCollisionForPlayer(setEnabled: true, GameNetworkManager.Instance.localPlayerController);
            }
            BRSeatTrigger.interactable = true;
        }
    }

    public void SetBRPassengerInCar(PlayerControllerB player)
    {
        if (BRSideDoor.boolValue)
        {
            BRSideDoor.SetBoolOnClientOnly(setTo: false);
        }
        if (player == GameNetworkManager.Instance.localPlayerController)
        {
            localPlayerInBRSeat = true;
        }
        else
        {
            BRSeatTrigger.interactable = false;
        }
        currentBR = player;
        SetVehicleCollisionForPlayerServerRPC(false);
    }

    // The 2 below methods disable collisions for passengers that enter
    // Interestingly, this is an oversight for the Cruiser passenger

    [ServerRpc(RequireOwnership = false)]
    public void SetVehicleCollisionForPlayerServerRPC(bool setEnabled)
    {
        SetVehicleCollisionForPlayer(setEnabled: setEnabled, GameNetworkManager.Instance.localPlayerController);
        SetVehicleCollisionForPlayerClientRPC(setEnabled);
    }

    [ClientRpc]
    public void SetVehicleCollisionForPlayerClientRPC(bool setEnabled)
    {
        SetVehicleCollisionForPlayer(setEnabled: setEnabled, GameNetworkManager.Instance.localPlayerController);
    }

    // Interestingly, this is an oversight for the Cruiser passenger, so I fixed it for the Hauler at least
    public new void SetPassengerInCar(PlayerControllerB player)
    {
        base.SetPassengerInCar(player);
        SetVehicleCollisionForPlayerServerRPC(false);
    }

    // ADDITIONAL METHODS //////////////////////////

    public int CanExitBackSeats(bool isLeftSeat)
    {
        Transform[] exitPointList = isLeftSeat ? BL_ExitPoints : BR_ExitPoints;
        for (int i = 0; i < exitPointList.Length; i++)
        {
            if (!Physics.Linecast(GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position, exitPointList[i].position, exitCarLayerMask, QueryTriggerInteraction.Ignore))
            {
                return i;
            }
        }
        return -1;
    }

    [ClientRpc]
    public new void CarBumpClientRpc(Vector3 vel)
    {
        if (physicsRegion.physicsTransform == GameNetworkManager.Instance.localPlayerController.physicsParent && ((!localPlayerInControl && !localPlayerInPassengerSeat && !localPlayerInBLSeat && !localPlayerInBRSeat) || !(vel.magnitude < 50f)))
        {
            GameNetworkManager.Instance.localPlayerController.externalForceAutoFade += vel;
        }
    }

    // Additional things to do on update
    public new void Update()
    {
        base.Update();
        if (carDestroyed) { return; }
        if (destroyNextFrame) { return; }

        // Re-enable the door triggers after getting in
        BLSideDoorTrigger.interactable = Time.realtimeSinceStartup - timeSinceSpringingDriverSeat > 1.6f;
        BRSideDoorTrigger.interactable = Time.realtimeSinceStartup - timeSinceSpringingDriverSeat > 1.6f;

        // Event when key is added/removed (battery is on)
        if (lastKeyInIgnition != keyIsInIgnition)
        {
            setDashDials();
            lastKeyInIgnition = keyIsInIgnition;
        }

        // Traction control light
        bool slipping = (FrontLeftWheel.motorTorque > 900f && FrontRightWheel.motorTorque > 900f);
        if (slipping && !tractionLightWasAlarmed && keyIsInIgnition)
        {
            tractionLightWasAlarmed = true;
            tractionControlLight.SetActive(true);
        }
        else if (!slipping && tractionLightWasAlarmed)
        {
            tractionLightWasAlarmed = false;
            tractionControlLight.SetActive(false);
        }

        // Check engine light
        if ((float)carHP/baseCarHP < 0.5f && !checkEngineWasAlarmed && keyIsInIgnition)
        {
            checkEngineWasAlarmed = true;
            checkEngineLight.SetActive(true);
            ChimeAudio.PlayOneShot(chimeSoundCritical);
        }
        else if ((float)carHP / baseCarHP > 0.5f && checkEngineWasAlarmed)
        {
            checkEngineWasAlarmed = false;
            checkEngineLight.SetActive(false);
        }

        // Time on dash
        if (keyIsInIgnition)
        {
            dotMatrix.text = HUDManager.Instance.clockNumber.text.Trim().Replace("\n", " ");
        }

        // Gauges
        if (FrontLeftWheel != null)
        {
            leftDialTransform.localEulerAngles = new Vector3(Mathf.Lerp(-219f, -10f, Mathf.Abs(EngineRPM) / (MaxEngineRPM / 2.5f)) + (ignitionStarted ? 25f : 0f), 90f, 90f);
            rightDialTransform.localEulerAngles = new Vector3(Mathf.Lerp(11f, -203f, Mathf.Abs(FrontLeftWheel.rotationSpeed) / 5000f), 270f, -90f);
        }
    }

    // Additional things to do on start
    public new void Start()
    {
        base.Start();
        setDashDials();
        checkEngineLight.SetActive(false);
        tractionControlLight.SetActive(false);
    }

    public void setDashDials()
    {
        screensContainer.SetActive(keyIsInIgnition);
        leftDial.materials = keyIsInIgnition ? [dialOnMat] : [dialOffMat];
        rightDial.materials = keyIsInIgnition ? [dialOnMat] : [dialOffMat];
        leftDialTickmarks.color = keyIsInIgnition ? new Color(0.33f, 0.84f, 0.83f) : new Color(0.11f, 0.33f, 0.33f);
        rightDialTickmarks.color = keyIsInIgnition ? new Color(0.33f, 0.84f, 0.83f) : new Color(0.11f, 0.33f, 0.33f);
    }

    // Kill backseat players if the car explodes
    public new void DestroyCar()
    {
        if (!carDestroyed)
        {
            if (localPlayerInBLSeat || localPlayerInBRSeat)
            {
                GameNetworkManager.Instance.localPlayerController.KillPlayer(Vector3.up * 27f + 20f * UnityEngine.Random.insideUnitSphere, spawnBody: true, CauseOfDeath.Blast, 6, Vector3.up * 1.5f);
            }
        }
        base.DestroyCar();
    }

    // Damage backseat players if the car hits something, like the front seats do
    public new void DamagePlayerInVehicle(Vector3 vel, float magnitude)
    {
        bool isInBackRow = localPlayerInBLSeat || localPlayerInBRSeat;
        bool prevIsPassenger = localPlayerInPassengerSeat;
        if (isInBackRow)
        {
            localPlayerInPassengerSeat = true;
        }
        base.DamagePlayerInVehicle(vel, magnitude);
        localPlayerInPassengerSeat = prevIsPassenger;
    }

    // ??
    public new void OnDisable()
    {
        bool isInBackRow = localPlayerInBLSeat || localPlayerInBRSeat;
        bool prevIsPassenger = localPlayerInPassengerSeat;
        if (isInBackRow)
        {
            localPlayerInPassengerSeat = true;
        }
        base.OnDisable();
        localPlayerInPassengerSeat = prevIsPassenger;
    }

    // Disable backrow players from pushing car while seated
    public new void PushTruckWithArms()
    {
        bool isInBackRow = localPlayerInBLSeat || localPlayerInBRSeat;
        bool prevIsPassenger = localPlayerInPassengerSeat;
        if (isInBackRow)
        {
            localPlayerInPassengerSeat = true;
        }
        base.PushTruckWithArms();
        localPlayerInPassengerSeat = prevIsPassenger;
    }

    // Hauler can't boost
    public new void AddTurboBoost() {}

    // Methods below to add door chime
    public new void StartTryCarIgnition()
    {
        if (!doorChimeDebounce)
        {
            ChimeAudio.PlayOneShot(chimeSound);
            doorChimeDebounce = true;
        }
        base.StartTryCarIgnition();
    }
    public new void RemoveKeyFromIgnition()
    {
        doorChimeDebounce = false;
        base.RemoveKeyFromIgnition();
    }

    // Cabin light
    public void CabinLightToggle()
    {
        SetFrontCabinLightOn(!cablightToggle);
    }
}