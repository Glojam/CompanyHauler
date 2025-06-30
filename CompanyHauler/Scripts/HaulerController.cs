using System.Collections;
using System.Collections.Generic;
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

    public bool cablightToggle = false; // bad

    private bool cabinLightBoolean = false;

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

    public AnimationClip cruiserGearShiftClip;

    public AnimationClip haulerColumnShiftClip;

    public AnimationClip cruiserGearShiftIdleClip;

    public AnimationClip cruiserSteeringClip;

    private RuntimeAnimatorController originalController = null!;

    private AnimatorOverrideController overrideController = null!;

    private RuntimeAnimatorController originalController_pass = null!;

    private AnimatorOverrideController overrideController_pass = null!;

    private bool passReplaced;

    public AnimationClip cruiserKeyInsertClip;

    public AnimationClip cruiserKeyInsertAgainClip;

    public AnimationClip cruiserKeyRemoveClip;

    public AnimationClip cruiserKeyUntwistClip;

    public AnimationClip haulerKeyInsertClip;

    public AnimationClip haulerKeyInsertAgainClip;

    public AnimationClip haulerKeyRemoveClip;

    public AnimationClip haulerKeyUntwistClip;

    public AnimationClip haulerPassengerSitClip;

    public AudioClip TrainHornAudioClip;

    public AudioSource TrainHornAudio;

    public AudioSource TrainHornAudioDistant;

    private float superHornCooldownTime;

    public float superHornCooldownAmount;

    public InteractTrigger redButtonTrigger;

    public AudioSource roofRainAudio;

    public List<GameObject> haulerObjectsToDestroy;

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
            //SetVehicleCollisionForPlayer(false, player);
            int passengerId = (int)player.playerClientId;
            SetVehicleCollisionForPlayerServerRPC(false, passengerId);
        }
        else
        {
            BLSeatTrigger.interactable = false;
        }
        currentBL = player;

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
        if (BRSideDoor.boolValue)
        {
            BRSideDoor.SetBoolOnClientOnly(setTo: false);
        }
        if (player == GameNetworkManager.Instance.localPlayerController)
        {
            localPlayerInBRSeat = true;
            //SetVehicleCollisionForPlayer(false, player);
            int passengerId = (int)player.playerClientId;
            SetVehicleCollisionForPlayerServerRPC(false, passengerId);
        }
        else
        {
            BRSeatTrigger.interactable = false;
        }
        currentBR = player;
    }

    // The 2 below methods disable collisions for passengers that enter

    [ServerRpc(RequireOwnership = false)]
    public void SetVehicleCollisionForPlayerServerRPC(bool setEnabled, int passengerId)
    {
        SetVehicleCollisionForPlayerClientRPC(setEnabled, passengerId);
    }

    [ClientRpc]
    public void SetVehicleCollisionForPlayerClientRPC(bool setEnabled, int passengerId)
    {
        PlayerControllerB passengerPlayer = StartOfRound.Instance.allPlayerScripts[passengerId];
        SetVehicleCollisionForPlayer(setEnabled: setEnabled, passengerPlayer);
    }

    // Interestingly, this is an oversight for the Cruiser passenger, which needs to be fixed for the Hauler
    public new void SetPassengerInCar(PlayerControllerB player)
    {
        base.SetPassengerInCar(player);
        if (player == GameNetworkManager.Instance.localPlayerController)
        {
            //localPlayerInPassengerSeat = true;
            int passengerId = (int)player.playerClientId;
            SetVehicleCollisionForPlayerServerRPC(false, passengerId);
        }
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

        if (!redButtonTrigger.interactable)
        {
            if (superHornCooldownTime <= 0f)
            {
                redButtonTrigger.interactable = true;
                return;
            }
            redButtonTrigger.disabledHoverTip = $"[Recharging: {(int)superHornCooldownTime} sec.]";
            superHornCooldownTime -= Time.deltaTime;
        }

        // Re-enable the rearside door triggers after getting in
        BLSideDoorTrigger.interactable = Time.realtimeSinceStartup - timeSinceSpringingDriverSeat > 1.6f;
        BRSideDoorTrigger.interactable = Time.realtimeSinceStartup - timeSinceSpringingDriverSeat > 1.6f;

        // Event when key is added/removed (battery is on)
        if (lastKeyInIgnition != keyIsInIgnition)
        {
            setDashDials();
            lastKeyInIgnition = keyIsInIgnition;
        }

        // Traction control light
        if (tireSparks.isEmitting && !tractionLightWasAlarmed && keyIsInIgnition)
        {
            tractionLightWasAlarmed = true;
            tractionControlLight.SetActive(true);
        }
        else if (!tireSparks.isEmitting && tractionLightWasAlarmed)
        {
            tractionLightWasAlarmed = false;
            tractionControlLight.SetActive(false);
        }

        bool raining =  TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Rainy ||
                        TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Flooded ||
                        TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Stormy;

        // Roof rain
        if (raining && !roofRainAudio.isPlaying) 
        { 
            roofRainAudio.Play();
        }
        else if (!raining && roofRainAudio.isPlaying)
        {
            roofRainAudio.Stop();
        }

        // Check engine light
        if ((float)carHP / baseCarHP < 0.5f && !checkEngineWasAlarmed && keyIsInIgnition)
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

    public new void Awake()
    {
        base.Awake();
        redButtonTrigger.interactable = false;
        superHornCooldownTime = superHornCooldownAmount;
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

    // ?? not sure, just added to be consistemt
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
        TryChimeSoundServerRpc();
        base.StartTryCarIgnition();
    }
    public new void RemoveKeyFromIgnition()
    {
        EndDoorChimeDebounceServerRpc();
        base.RemoveKeyFromIgnition();
    }

    [ServerRpc(RequireOwnership = false)]
    public void TryChimeSoundServerRpc()
    {
        TryChimeSoundClientRpc();
    }
    [ClientRpc]
    public void TryChimeSoundClientRpc()
    {
        if (!doorChimeDebounce)
        {
            ChimeAudio.PlayOneShot(chimeSound);
            doorChimeDebounce = true;
        }
    }
    [ServerRpc(RequireOwnership = false)]
    public void EndDoorChimeDebounceServerRpc()
    {
        EndDoorChimeDebounceClientRpc();
    }
    [ClientRpc]
    public void EndDoorChimeDebounceClientRpc()
    {
        doorChimeDebounce = false;
    }

    // Cabin light
    public void CabinLightToggle()
    {
        SetFrontCabinLightOnServerRpc();
    }
    [ServerRpc(RequireOwnership =false)]   
    public void SetFrontCabinLightOnServerRpc()
    {
        SetFrontCabinLightOnClientRpc();
    }
    [ClientRpc]
    public void SetFrontCabinLightOnClientRpc()
    {
        cabinLightBoolean = !cabinLightBoolean;
        SetFrontCabinLightOn(!cabinLightBoolean);
    }

    public void ReplaceGearshiftAnimLocalClient()
    {
        //Debug.Log("THIS client is the driver.");
        int playerId = (int)GameNetworkManager.Instance.localPlayerController.playerClientId;
        ReplaceGearshiftAnimServerRpc(playerId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReplaceGearshiftAnimServerRpc(int playerId)
    {
        ReplaceGearshiftAnimClientRpc(playerId);
    }

    // Animation overrides for the gear shifter
    // For some reason... other players CANT SEE THIS!! WHY?????
    [ClientRpc]
    public void ReplaceGearshiftAnimClientRpc(int playerId)
    {
        //Debug.Log("Received request to replace animations for player: " + playerId.ToString());
        PlayerControllerB driverPlayer = StartOfRound.Instance.allPlayerScripts[playerId];
        // Reset the sitting state w/ the jump parameter
        driverPlayer.playerBodyAnimator.SetBool("SA_JumpInCar", true);
        CompanyHauler.Logger.LogDebug(driverPlayer);
        originalController = driverPlayer.playerBodyAnimator.runtimeAnimatorController;
        overrideController = new AnimatorOverrideController(originalController);
        overrideController["PullGearstick"] = haulerColumnShiftClip;
        overrideController["SitAndSteerRightHandOnGearstick"] = cruiserSteeringClip;
        overrideController["Key_Insert"] = haulerKeyInsertClip;
        overrideController["Key_InsertAgain"] = haulerKeyInsertAgainClip;
        overrideController["Key_Remove"] = haulerKeyRemoveClip;
        overrideController["Key_Untwist"] = haulerKeyUntwistClip;
        driverPlayer.playerBodyAnimator.runtimeAnimatorController = overrideController;
        //Debug.Log("Assigned override controller with: " + overrideController["PullGearstick"].name);
        CompanyHauler.Logger.LogDebug("Replaced geasrhifter animation clip.");
        //Debug.Log("Replaced gear shifter animation clip for player: " + playerId.ToString());
    }

    public void ReturnGearshiftAnimLocalClient()
    {
        int playerId = (int)GameNetworkManager.Instance.localPlayerController.playerClientId;
        ReturnGearshiftAnimServerRpc(playerId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReturnGearshiftAnimServerRpc(int playerId)
    {
        ReturnGearshiftAnimClientRpc(playerId);
    }

    [ClientRpc]
    public void ReturnGearshiftAnimClientRpc(int playerId)
    {
        PlayerControllerB driverPlayer = StartOfRound.Instance.allPlayerScripts[playerId];
        driverPlayer.playerBodyAnimator.runtimeAnimatorController = originalController;
        // Below is a very stupid hack used to prevent the sitting animation from not looping
        driverPlayer.playerBodyAnimator.SetBool("SA_Truck", true);
        // Below is a very stupid hack used to prevent several animations from not playing
        driverPlayer.playerBodyAnimator.SetBool("SA_JumpInCar", true);
        CompanyHauler.Logger.LogDebug("Reverted to the original gearshift animation clip.");
    }

    public void ReplacePassengerAnimLocalClient()
    {
        int passenger = (int)GameNetworkManager.Instance.localPlayerController.playerClientId;
        ReplacePassengerAnimServerRpc(passenger);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReplacePassengerAnimServerRpc(int passenger)
    {
        ReplacePassengerAnimClientRpc(passenger);
    }

    // TODO this isn't used yet, doesn't look quite right ingame
    [ClientRpc]
    public void ReplacePassengerAnimClientRpc(int passenger)
    {
        // SetPassengerInCar gets called twice for some reason, this is a debounce
        // Possible downside: future passengers don't do cool animation if front seater died whilst sitting instead of exiting
        if (passReplaced) { return; }
        // Below is a very stupid hack used to prevent getting in cruiser to break hauler anims
        PlayerControllerB passengerPlayer = StartOfRound.Instance.allPlayerScripts[passenger];
        passengerPlayer.playerBodyAnimator.SetBool("SA_JumpInCar", true);
        originalController_pass = passengerPlayer.playerBodyAnimator.runtimeAnimatorController;
        overrideController_pass = new AnimatorOverrideController(originalController_pass);
        overrideController_pass["SitAndSteerNoHands"] = haulerPassengerSitClip;
        passengerPlayer.playerBodyAnimator.runtimeAnimatorController = overrideController_pass;
        CompanyHauler.Logger.LogDebug("Replaced passenger animation clip.");
        passReplaced = true;
    }

    public void ReturnPassengerAnimLocalClient()
    {
        int passenger = (int)GameNetworkManager.Instance.localPlayerController.playerClientId;
        ReturnPassengerAnimServerRpc(passenger);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReturnPassengerAnimServerRpc(int passenger)
    {
        ReturnPassengerAnimClientRpc(passenger);
    }

    [ClientRpc]
    public void ReturnPassengerAnimClientRpc(int passenger)
    {
        PlayerControllerB passengerPlayer = StartOfRound.Instance.allPlayerScripts[passenger];
        passengerPlayer.playerBodyAnimator.runtimeAnimatorController = originalController_pass;
        // Below is a very stupid hack used to prevent the sitting animation from not looping
        passengerPlayer.playerBodyAnimator.SetBool("SA_Truck", true);
        // Below is a very stupid hack used to prevent several animations from not playing
        passengerPlayer.playerBodyAnimator.SetBool("SA_JumpInCar", true);
        CompanyHauler.Logger.LogDebug("Reverted to the original passenger animation clip.");
        passReplaced = false;
    }

    public void TrainHornLocalClient()
    {
        TrainHornServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void TrainHornServerRpc()
    {
        TrainHornClientRpc();
    }

    [ClientRpc]
    public void TrainHornClientRpc()
    {
        TrainHornAudio.Play();
        TrainHornAudioDistant.Play();
        superHornCooldownTime = superHornCooldownAmount;
        redButtonTrigger.interactable = false;
        WalkieTalkie.TransmitOneShotAudio(TrainHornAudio, TrainHornAudioClip);
        float distToPlayer = Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, base.transform.position);
        if (distToPlayer < 20f)
        {
            HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
        }
        // Alert the horde
        StartCoroutine(AlertHorde());
    }

    public IEnumerator AlertHorde()
    {
        for (int i = 0; i < 50; i++)
        {
            RoundManager.Instance.PlayAudibleNoise(GameNetworkManager.Instance.localPlayerController.transform.position, 1050f, 1050f, 0, false, 19027);
            yield return new WaitForSeconds(0.1f);
        }
    }

}