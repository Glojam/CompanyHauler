using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CompanyHauler.Utils;
using GameNetcodeStuff;
using ScandalsTweaks.Utils;
using ScandalLib.Patches;
using ScandalLib.Scripts;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;
using System.Runtime.CompilerServices;
using ScandalLib.Interfaces;
using System.Threading;

namespace CompanyHauler.Scripts;

public class HaulerController : VehicleController
{
    [Header("DEBUG")]

    public bool isInTestMode = false;

    public float debugMotorTorque;
    public float debugBrakeTorque;
    public float debugSteeringAngle;

    [Header("Networking")]

    private bool receivedClientSyncData;

    public bool syncedDrivePedalPressed;
    public bool syncedBrakePedalPressed;

    public float tyreStress;
    public bool wheelSlipping;
    public bool wheelSkidActive;

    public float syncedEngineRPM;
    public float syncedFrontWheelRPM;
    public float syncedBackWheelRPM;
    public float syncedWheelRPM;

    public int syncedCarHP;

    public float syncEffectsInterval;
    public float syncTorqueInterval;
    public float syncDrivetrainInterval;

    public float syncedSteeringRotation;
    public float syncedCurrentMotorTorque;
    public float syncedCurrentBrakeTorque;

    private float syncedSongTime;

    [Header("Player")]

    public ExtendedPhysicsRegion vehicleZone = null!;
    public ExtendedPlayerZone vehicleCabZone = null!;

    public PlayerControllerB lastDriver = null!;
    public PlayerControllerB playerWhoShifted = null!;
    public PlayerControllerB? currentBackLeftPassenger = null!;
    public PlayerControllerB? currentBackMiddlePassenger = null!;
    public PlayerControllerB? currentBackRightPassenger = null!;

    public HaulerSeatAnimator frontLeftSeat = null!;
    public HaulerSeatAnimator frontRightSeat = null!;
    public HaulerSeatAnimator backLeftSeat = null!;
    public HaulerSeatAnimator backSeat = null!;
    public HaulerSeatAnimator backRightSeat = null!;

    public Transform[] backLeftExitPoints = null!;
    public Transform[] backRightExitPoints = null!;

    public AnimatedObjectTrigger backLeftDoor = null!;
    public AnimatedObjectTrigger backRightDoor = null!;

    public InteractTrigger backLeftDoorTrigger = null!;
    public InteractTrigger backRightDoorTrigger = null!;

    public InteractTrigger backLeftPassengerSeatTrigger = null!;
    public InteractTrigger backPassengerSeatTrigger = null!;
    public InteractTrigger backRightPassengerSeatTrigger = null!;

    public bool localPlayerInBackLeftPassengerSeat;
    public bool localPlayerInMiddlePassengerSeat;
    public bool localPlayerInBackRightPassengerSeat;

    [Header("Physics")]

    public HaulerCollisionTrigger collisionTrigger = null!;
    public float timeSinceLastCollision;

    public List<WheelCollider> allWheels = null!;
    private WheelHit[] wheelHits = new WheelHit[4];

    public Rigidbody playerPhysicsBody = null!;

    public AnimationCurve steeringCurve = null!;

    public AnimationCurve engineCurve = null!;
    public AnimationCurve enginePowerCurve = null!;

    public float torqueBoost = 1.25f;
    public float enginePower;
    public float engineReversePower;

    public bool forwardSlipping;
    public bool sidewaySlipping;

    public Coroutine shiftGearCoroutine = null!;
    public float[] gearRatios = null!;
    public float diffRatio;
    public int currentGear;

    public float upShiftThreshold;
    public float downShiftThreshold;

    public float lastShiftTime;
    public float shiftCooldown;
    public float shiftTime;

    public float steeringWheelAnimValue;

    public float normalisedCarHP;

    // START
    public float currentSteeringWheelAnimValue;
    public float steeringReturnSpeed;
    public float steeringSpeed;

    public float carDeacceleration;
    public float minTorque;
    public float maxReverseTorque;
    public float maxForwardTorque;

    // BOOST START
    public float inclineBoost;
    public float minInclineBoost;
    public float maxInclineBoost;
    public float maxInclineBoostAngle = 32f;

    public bool allowBoostTorque;

    public float forwardBoostSpeed;
    public float reverseBoostSpeed;
    public float boostReturnSpeed;

    public float boostMultiplierLimit;
    public float forwardBoostThreshold;
    public float reverseBoostThreshold;
    public float boostMultiplier;
    // BOOST END

    public float reverseCarAcceleration;

    public float brakeAcceleration;
    public float maxBrakeTorque;
    public float maxParkingBrakeTorque;

    public float currentMotorTorque;
    public float currentBrakeTorque;
    // END

    public float steeringAngle;
    public float maxSteeringAngle;

    public float forwardWheelSpeed;
    public float reverseWheelSpeed;

    public float fRpmDiff;
    public float bRpmDiff;

    public float frontWheelsRPM;
    public float backWheelsRPM;

    public float wheelRPM;
    public float frontWheelRPM;
    public float backWheelRPM;

    public bool backWheelsGrounded;
    public bool allWheelsGrounded;
    public bool allWheelsAirborne;

    public float forwardsSlip;
    public float sidewaysSlip;

    public bool hasDeliveredVehicle;

    [Header("VFX")]

    public TextMeshPro radioScreen = null!;
    //private Coroutine radioOnCoroutine = null!;

    public Collider[] weatherEffectBlockers = null!;

    public InteractTrigger startIgnitionTrigger = null!;
    public InteractTrigger stopIgnitionTrigger = null!;

    public Animator ignitionAnimator = null!;
    public GameObject carKeyContainer = null!;
    public GameObject keyItemHolder = null!;
    public Transform ignitionKeyPosition = null!;

    private static readonly Vector3 ignitionKeyScale = new Vector3(1f, 1f, 1f);
    private static readonly Vector3 keyPosLocal = new Vector3(0.05029682f, 0.1181492f, -0.09794867f);
    private static readonly Vector3 keyPosServer = new Vector3(0.0414306f, 0.09218378f, -0.09290992f);
    private static readonly Vector3 keyRotLocal = new Vector3(29.962f, -2.063f, -3.106f);
    private static readonly Vector3 keyRotServer = new Vector3(29.427f, -5.158f, -53.66f);

    public HaulerWindow frontLeftWindow = null!;
    public HaulerWindow frontRightWindow = null!;
    public HaulerWindow backLeftWindow = null!;
    public HaulerWindow backRightWindow = null!;

    // GAUGE CLUSTER START
    public Material needleOnMaterial = null!;
    public Material needleOffMaterial = null!;

    public Material tachometerOnMaterial = null!;
    public Material tachometerOffMaterial = null!;

    public Material speedometerOnMaterial = null!;
    public Material speedometerOffMaterial = null!;

    public GameObject gaugeLightContainer = null!;

    public Image speedometerImage = null!;
    public Image tachometerImage = null!;

    public MeshRenderer speedometerMesh = null!;
    public MeshRenderer tachometerMesh = null!;

    public Transform speedometerTransform = null!;
    public Transform tachometerTransform = null!;

    public Collider startIgnitionCollider = null!;
    public Collider stopIgnitionCollider = null!;

    public float speedometerFloat;
    public float tachometerFloat;

    // symbols
    private Coroutine dashboardSweepCoroutine = null!;
    private int currentSweepStage;
    private bool hasSweepedDashboard;

    public SpriteRenderer tractionControlSymbol = null!;
    private bool tractionControlLightActive;

    public SpriteRenderer checkEngineSymbol = null!;
    private bool hasPlayedCheckEngineWarning;
    private bool checkEngineLightActive;

    public SpriteRenderer coolantLevelSymbol = null!;
    private bool coolantLevelLightActive;

    public SpriteRenderer immobiliserSymbol = null!;
    public SpriteRenderer leftSignalSymbol = null!;
    public SpriteRenderer hazardSignalSymbol = null!;
    public SpriteRenderer rightSignalSymbol = null!;

    public SpriteRenderer dippedBeamSymbol = null!;
    private bool dippedBeamLightActive;

    public SpriteRenderer mainBeamSymbol = null!;
    private bool mainBeamLightActive;

    public SpriteRenderer oilLevelSymbol = null!;
    private bool oilLevelLightActive;

    public SpriteRenderer parkingBrakeSymbol = null!;
    private bool parkingBrakeLightActive;

    public SpriteRenderer batteryLowSymbol = null!;
    private bool batteryLowLightActive;

    public bool brakeLightsOn;
    public GameObject centerMountedLightContainer = null!;
    public MeshRenderer centerMountedLight = null!;
    public Material backLightOffMat = null!;

    public Transform tempPushTransform = null!;

    public MeshRenderer windshieldMesh = null!;
    public AnimatedObjectTrigger windowWipers = null!;
    public bool automaticWipersOn;

    private bool setAudiosMuted;

    public bool tailgateOpen;

    public bool sunroofOpen;

    public bool isCabinLightToggled;
    public bool isCabinLightOn;

    public float playerSteeringWheelAnimFloat;
    public float syncedPlayerSteeringAnim;

    public float shiftSpeed = 25f;

    public bool gaugesOn;
    public bool headlampsOn;

    public bool disableAnimations;
    public bool inIgnitionAnimation;
    public bool ignitionOn;

    // animations
    internal const string STEERING_WHEEL_SPEED = "steeringWheelTurnSpeed";
    internal const string ANIMATION_SPEED = "animationSpeed";
    internal const string IGNITION_ANIM = "SAIgnition_Anim";
    internal const string CAR_ANIM = "SA_CarAnim";
    internal const string JUMP_WHILE_IN_CAR = "SA_JumpInCar";
    internal const string CAR_MOTION_TIME = "SA_CarMotionTime";

    // triggers
    private const string DOOR_ENTER_HOVERTIP = "Use door : [LMB]";
    private const string DOOR_EXIT_HOVERTIP = "Exit : [LMB]";

    private const string HOOD_ANIMATION_BOOL = "hoodOpen";

    // player
    private const string PLAYER_MOVEMENT = "Move";

    [Header("Destruction")]

    public GameObject[] disableOnDestroy = null!;

    public GameObject mainBodyContainer = null!;
    public GameObject sunroofContainer = null!;
    public GameObject hoodDoorContainer = null!;
    public GameObject frontLeftDoorContainer = null!;
    public GameObject frontRightDoorContainer = null!;
    public GameObject backLeftDoorContainer = null!;
    public GameObject backRightDoorContainer = null!;

    [Header("Audio")]

    public AudioSource[] allVehicleAudios = null!;

    public AudioSource roofRainAudio = null!;

    public AudioSource cabinAudio = null!;
    public AudioClip ignitionChime = null!;
    public AudioClip chimeSoundWarning = null!;
    public AudioSource carKeyAudio = null!;

    public AudioClip dashButtonPress = null!;
    public AudioSource windshieldAudio = null!;
    public AudioSource roofInteractionAudio = null!;
    public AudioSource leftDashboardAudio = null!;
    public AudioSource centerDashboardAudio = null!;
    public AudioSource rightDashboardAudio = null!;

    public bool roofRainAudioActive;
    public bool hasPlayedIgnitionChime;
    public float timeLastSyncedRadio;
    public float radioPingTimestamp;


    // Thanks TomatoBird8.
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // this replaces any calls to this method with the method body (the code in the method)
    // static is for if you don't need the instance
    public static bool IsInShipPhaseAndNotLoadingLevel()
    {
        return StartOfRound.Instance.inShipPhase && !StartOfRound.Instance.beganLoadingNewLevel;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)] // this replaces any calls to this method with the method body (the code in the method)
    public static bool IsInShipPhaseAndLoadingLevel()
    {
        return StartOfRound.Instance.inShipPhase &&
               StartOfRound.Instance.beganLoadingNewLevel;
    }

    public void OnEnable()
    {
        //ConfigureSubSteps(speedThreshold: 10f, stepsBelowThreshold: 20, stepsAboveThreshold: 10);
        VehicleUtils.pickupController = this;
    }

    //public void ConfigureSubSteps(float speedThreshold, int stepsBelowThreshold, int stepsAboveThreshold)
    //{
    //    FrontLeftWheel.ConfigureVehicleSubsteps(speedThreshold, stepsBelowThreshold, stepsAboveThreshold);
    //    FrontRightWheel.ConfigureVehicleSubsteps(speedThreshold, stepsBelowThreshold, stepsAboveThreshold);
    //    BackLeftWheel.ConfigureVehicleSubsteps(speedThreshold, stepsBelowThreshold, stepsAboveThreshold);
    //    BackRightWheel.ConfigureVehicleSubsteps(speedThreshold, stepsBelowThreshold, stepsAboveThreshold);
    //}

    // Additional things to do on awake
    public new void Awake()
    {
        ragdollPhysicsBody.interpolation = RigidbodyInterpolation.None;
        windwiperPhysicsBody1.interpolation = RigidbodyInterpolation.None;
        windwiperPhysicsBody2.interpolation = RigidbodyInterpolation.None;
        playerPhysicsBody.interpolation = RigidbodyInterpolation.None;
        playerPhysicsBody.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ | 
                                        RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
        backDoorOpen = true; // hacky shit
        base.Awake();

        vehicleZone.priority = 1;
        syncedPosition = transform.position;
        syncedRotation = transform.rotation;

        InitializePickup();
    }


    public new void SetBackDoorOpen(bool open)
    {
        tailgateOpen = open;
    }


    // Additional things to do on start
    public new void Start()
    {
        StartCoroutine(SetRainCollision());
        if (maxBrakeTorque > maxParkingBrakeTorque)
            maxBrakeTorque = maxParkingBrakeTorque - 100f;
        carHP = baseCarHP;

        FrontLeftWheel.brakeTorque = maxParkingBrakeTorque;
        FrontRightWheel.brakeTorque = maxParkingBrakeTorque;
        BackLeftWheel.brakeTorque = maxParkingBrakeTorque;
        BackRightWheel.brakeTorque = maxParkingBrakeTorque;

        currentRadioClip = new System.Random(StartOfRound.Instance.randomMapSeed).Next(0, radioClips.Length);
        radioAudio.clip = radioClips[currentRadioClip];
        decals = new DecalProjector[24];

        if (!StartOfRound.Instance.inShipPhase || IsInShipPhaseAndLoadingLevel())
            return;

        magnetedToShip = true;
        loadedVehicleFromSave = true;
        hasDeliveredVehicle = true;
        inDropshipAnimation = false;
        hasBeenSpawned = true;
        SetVehicleKinematic(setKinematic: true);
        transform.position = StartOfRound.Instance.magnetPoint.position + StartOfRound.Instance.magnetPoint.forward * 7f;
        StartMagneting();
    }

    // --- SYNC DATA ---
    public void SendClientSyncData(ulong clientId)
    {
        if (magnetedToShip)
        {
            Vector3 eulerAngles = magnetTargetRotation.eulerAngles;
            MagnetCarRpc(magnetTargetPosition, eulerAngles, magnetStartPosition, magnetStartRotation, RoundManager.Instance.tempTransform.eulerAngles, averageVelocityAtMagnetStart);
        }
        if (carDestroyed)
        {
            SyncDestroyedCarForClientRpc(carHP, RpcTarget.Single(clientId, RpcTargetUse.Temp));
            return;
        }
        ClientSyncDataRpc(carHP, keyIsInIgnition, ignitionStarted, ignitionOn, isCabinLightOn, isCabinLightToggled, headlampsOn, windshieldBroken, (int)gear, EngineRPM, wheelRPM, frontWheelRPM, backWheelRPM, currentMotorTorque, currentBrakeTorque, radioOn, radioTurnedOnBefore, currentRadioClip, RpcTarget.Single(clientId, RpcTargetUse.Temp));
        /*
        ReceiveClientSyncDataRpc(
            setIgnition: ignitionStarted, setKeyInSlot: keyIsInIgnition, isIgnitionOn: isIgnitionOn,
            setWindshield: windshieldBroken,
            setSunroof: sunroofOpen,
            setTailgate: tailgateOpen,
            setFrontLeftDoor: driverSideDoor.boolValue, setFrontRightDoor: passengerSideDoor.boolValue,
            setBackLeftDoor: backLeftDoor.boolValue, setBackRightDoor: backRightDoor.boolValue,
            setFrontLeftWindow: frontLeftWindow.windowOpen, setFrontRightWindow: frontRightWindow.windowOpen,
            setBackLeftWindow: backLeftWindow.windowOpen, setBackRightWindow: backRightWindow.windowOpen,
            setHood: carHoodOpen,
            setSteering: steeringWheelAnimFloat, setPlayerSteering: playerSteeringWheelAnimFloat,
            carHealth: carHP,
            setCabinLight: frontCabinLightContainer.activeSelf,
            setCabinLightSwitch: cabLightToggled);
        */
    }

    [Rpc(SendTo.SpecifiedInParams, RequireOwnership = false)]
    public void SyncDestroyedCarForClientRpc(int setCarHealth, RpcParams rpcParams = default)
    {
        if (receivedClientSyncData)
        {
            return;
        }
        receivedClientSyncData = true;
        carHP = setCarHealth;
        syncedCarHP = setCarHealth;
        timeAtLastDamage = Time.realtimeSinceStartup;
        DestroyCar();
    }

    [Rpc(SendTo.SpecifiedInParams, RequireOwnership = false)]
    public void ClientSyncDataRpc(int setCarHealth, bool setKeyInSlot,
        bool setStarted, bool setIgnitionOn,
        bool setCabinLightOn, bool setCabinLightSwitch,
        bool setHeadlightsOn, bool setWindowBroken,
        int currentGear, float engineSpeed, float wheelSpeed, 
        float frontWheelSpeed, float backWheelSpeed,
        float setMotorTorque, float setBrakeTorque, 
        bool setRadioOn, bool setRadioOnBefore,
        int radioClip,
        RpcParams rpcParams = default)
    {
        if (receivedClientSyncData)
        {
            return;
        }
        receivedClientSyncData = true;
        if (!windshieldBroken && setWindowBroken)
            BreakWindshield();
        if (headlampsOn != setHeadlightsOn)
        {
            headlampsOn = setHeadlightsOn;
            headlightsContainer.SetActive(setHeadlightsOn);
            SetHeadlightMaterial(setHeadlightsOn);
        }
        if ((int)gear != currentGear)
        {
            timeAtLastGearShift = Time.realtimeSinceStartup;
            gear = (CarGearShift)currentGear;
        }
        carHP = setCarHealth;
        syncedCarHP = setCarHealth;
        disableAnimations = !setStarted;
        ignitionOn = setIgnitionOn;
        SetIgnitionKeyValues(setKeyInHand: false, setKeyInSlot: setKeyInSlot);
        isCabinLightToggled = setCabinLightSwitch;
        SetIgnition(setStarted, setCabinLightOn);
        SetDashboardGaugesOn(setIgnitionOn);
        if (setStarted)
        {
            dashboardSweepCoroutine = StartCoroutine(DashboardSweepCoroutine());
        }
        ignitionAnimator.SetInteger(IGNITION_ANIM, setStarted ? 1 : 0);
        radioTurnedOnBefore = setRadioOnBefore;
        currentRadioClip = radioClip;
        if (!radioOn && setRadioOn)
        {
            SetRadioOnLocalClient(on: true, setClip: true);
        }
        currentMotorTorque = setMotorTorque;
        currentBrakeTorque = setBrakeTorque;
        syncedEngineRPM = engineSpeed;
        syncedWheelRPM = wheelSpeed;
        syncedFrontWheelRPM = frontWheelSpeed;
        syncedBackWheelRPM = backWheelSpeed;
    }

    /*
    [Rpc(SendTo.NotMe, RequireOwnership = false)]
    public void ReceiveClientSyncDataRpc(
        bool setIgnition, bool setKeyInSlot, bool isIgnitionOn, 
        bool setWindshield, 
        bool setSunroof,
        bool setTailgate, 
        bool setFrontLeftDoor, bool setFrontRightDoor, 
        bool setBackLeftDoor, bool setBackRightDoor, 
        bool setFrontLeftWindow, bool setFrontRightWindow,
        bool setBackLeftWindow, bool setBackRightWindow,
        bool setHood, 
        float setSteering, float setPlayerSteering,
        int carHealth,
        bool setCabinLight,
        bool setCabinLightSwitch)
    {
        if (receivedSyncData)
        {
            return;
        }
        receivedSyncData = true;
        keyIsInIgnition = setKeyInSlot;
        ignitionOn = isIgnitionOn;
        hasPlayedIgnitionChime = isIgnitionOn;
        SetDashboardGaugesOn(on: ignitionOn);
        cabLightToggled = setCabinLightSwitch;
        SetIgnition(started: setIgnition, cabLightOn: setCabinLight);
        if (setWindshield) BreakWindshield();
        //if (setSunroof && sunroofContainer.TryGetComponent<AnimatedObjectTrigger>(out var sunroofEvent))
        //{
        //    sunroofEvent.TriggerAnimation();
        //}
    }
    */

    public IEnumerator SetRainCollision()
    {
        yield return new WaitForSeconds(4f);

        var particleTriggers = new[]
        {
            ScandalLib.Utils.References.rainParticles,
            ScandalLib.Utils.References.rainHitParticles,
            ScandalLib.Utils.References.stormyRainParticles,
            ScandalLib.Utils.References.stormyRainHitParticles,
            ScandalLib.Utils.References.wesleyHurricaneRainParticles,
            ScandalLib.Utils.References.wesleyHurricaneRainHitParticles,
            ScandalLib.Utils.References.wesleyHurricaneSandParticles,
            ScandalLib.Utils.References.wesleyForsakenRainParticles,
            ScandalLib.Utils.References.wesleyForsakenRainHitParticles,
            ScandalLib.Utils.References.kenjiAcidRainParticles,
            ScandalLib.Utils.References.kenjiAcidRainHitParticles,
            ScandalLib.Utils.References.kenjiAcidStormyRainParticles,
            ScandalLib.Utils.References.kenjiAcidStormyRainHitParticles
        };
        foreach (var particle in particleTriggers)
        {
            if (particle == null)
                continue;
            var trigger = particle.trigger;
            foreach (var blocker in weatherEffectBlockers)
            {
                if (blocker != null)
                    trigger.AddCollider(blocker);
            }
        }
        yield break;
    }


    // --- GEAR SHIFT ---
    public void ChangeGear_Forward(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (!localPlayerInControl)
            return;

        int currentGear = (int)gear;
        if (currentGear < 3)
        {
            ShiftToGearAndSync(currentGear + 1);
        }
    }

    public void ChangeGear_Backward(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (!localPlayerInControl)
            return;

        int currentGear = (int)gear;
        if (currentGear > 1)
        {
            ShiftToGearAndSync(currentGear - 1);
        }
    }

    public new void ShiftToGearAndSync(int setGear)
    {
        if (gear == (CarGearShift)setGear)
            return;

        PlayerControllerB playerController = GameNetworkManager.Instance.localPlayerController;
        timeAtLastGearShift = Time.realtimeSinceStartup;
        playerWhoShifted = playerController;
        int gearAudioIndex = 0;
        if (setGear != (int)CarGearShift.Park)
        {
            gearAudioIndex = setGear > (int)gear ? 1 : 2;
        }
        gear = (CarGearShift)setGear;
        gearStickAudio.PlayOneShot(gearStickAudios[gearAudioIndex]);
        ShiftToGearRpc((int)playerController.playerClientId, setGear, gearAudioIndex);
    }

    [Rpc(SendTo.NotMe, RequireOwnership = false)]
    public void ShiftToGearRpc(int playerId, int setGear, int gearIndex)
    {
        PlayerControllerB playerController = StartOfRound.Instance.allPlayerScripts[playerId];
        timeAtLastGearShift = Time.realtimeSinceStartup;
        playerWhoShifted = playerController;
        gear = (CarGearShift)setGear;
        gearStickAudio.PlayOneShot(gearStickAudios[gearIndex]);
    }


    // --- CAB LIGHTING ---
    public new void SetFrontCabinLightOn(bool setOn)
    {
        if (setOn && isCabinLightToggled)
            return;

        isCabinLightOn = setOn;
        frontCabinLightContainer.SetActive(setOn);
        frontCabinLightMesh.material = setOn ? headlightsOnMat : headlightsOffMat;
    }

    public void ToggleCabinLights()
    {
        ToggleCabinLightsOwnerRpc();
    }

    [Rpc(SendTo.Owner, RequireOwnership = false)]
    public void ToggleCabinLightsOwnerRpc()
    {
        isCabinLightToggled = !isCabinLightToggled;
        ToggleCabinLightsLocalClient(setToggle: isCabinLightToggled, ignitionOn);
        ToggleCabinLightsRpc(setToggle: isCabinLightToggled, isIgnitionOn: ignitionOn);
    }

    [Rpc(SendTo.NotMe, RequireOwnership = false)]
    public void ToggleCabinLightsRpc(bool setToggle, bool isIgnitionOn)
    {
        isCabinLightToggled = setToggle;
        ToggleCabinLightsLocalClient(setToggle: isCabinLightToggled, isIgnitionOn: isIgnitionOn);
    }

    public void ToggleCabinLightsLocalClient(bool setToggle, bool isIgnitionOn)
    {
        if (setToggle)
        {
            SetFrontCabinLightOn(setOn: false);
            return;
        }
        SetFrontCabinLightOn(setOn: isIgnitionOn);
    }


    // --- TRY IGNITION METHOD ---
    public new void StartTryCarIgnition()
    {
        if (!localPlayerInControl)
            return;

        if (ignitionStarted ||
            inIgnitionAnimation ||
            (inIgnitionAnimation && startIgnitionTrigger.isBeingHeldByPlayer))
            return;

        CancelIgnitionCoroutine();
        disableAnimations = true;
        inIgnitionAnimation = true;
        keyIgnitionCoroutine = StartCoroutine(TryIgnition(isLocalDriver: true));
        TryIgnitionRpc(
            setKeyInSlot: keyIsInIgnition, 
            isIgnitionOn: ignitionOn);
    }

    private new IEnumerator TryIgnition(bool isLocalDriver)
    {
        if (currentDriver == null)
        {
            keyIgnitionCoroutine = null;
            yield break;
        }
        if (keyIsInIgnition)
        {
            SetIgnitionKeyValues(setKeyInHand: true, setKeyInSlot: true);
            currentDriver.playerBodyAnimator.SetInteger(CAR_ANIM, 12);
            ignitionAnimator.SetInteger(IGNITION_ANIM, 12);
            yield return new WaitForSeconds(0.02f);
            carKeyAudio.PlayOneShot(twistKey);
            SetIgnitionKeyValues(setKeyInHand: true, setKeyInSlot: true);
            yield return new WaitForSeconds(0.1467f);
        }
        else
        {
            SetIgnitionKeyValues(setKeyInHand: true, setKeyInSlot: false);
            currentDriver.playerBodyAnimator.SetInteger(CAR_ANIM, 2);
            ignitionAnimator.SetInteger(IGNITION_ANIM, 2);
            yield return new WaitForSeconds(0.6f);
            carKeyAudio.PlayOneShot(insertKey);
            SetIgnitionKeyValues(setKeyInHand: true, setKeyInSlot: true);
            yield return new WaitForSeconds(0.2f);
            carKeyAudio.PlayOneShot(twistKey);
            SetIgnitionKeyValues(setKeyInHand: true, setKeyInSlot: true);
            yield return new WaitForSeconds(0.18f);
        }
        SetIgnitionKeyValues(setKeyInHand: true, setKeyInSlot: true);
        if (!isLocalDriver) yield break;
        bool shiftInterlock = gear == CarGearShift.Park;
        if (shiftInterlock) PlayIgnitionAudio();
        if (!ignitionOn)
        {
            ignitionOn = true;
            if (dashboardSweepCoroutine == null && !hasSweepedDashboard)
            {
                dashboardSweepCoroutine = StartCoroutine(DashboardSweepCoroutine());
            }
        }
        SetIgnitionKeyValues(setKeyInHand: true, setKeyInSlot: true);
        SetFrontCabinLightOn(setOn: ignitionOn);
        SetDashboardGaugesOn(on: true);
        TryStartIgnitionRpc(shiftInterlock);
        if (!shiftInterlock) yield break;
        yield return new WaitForSeconds(UnityEngine.Random.Range(0.7f, 1.4f));
        if ((float)UnityEngine.Random.Range(0, 100) < chanceToStartIgnition)
        {
            CancelIgnitionAnimation(isStarted: true, setIgnitionAnim: true);
            disableAnimations = false;
            inIgnitionAnimation = false;
            currentDriver?.playerBodyAnimator.SetInteger(CAR_ANIM, 1);
            SetIgnitionKeyValues(setKeyInHand: false, setKeyInSlot: true);
            SetIgnition(setStarted: true, setCabinLight: true);
            SetDashboardGaugesOn(on: true);
            StartIgnitionRpc();
        }
        else
        {
            chanceToStartIgnition += 14f;
            chanceToStartIgnition = Mathf.Clamp(chanceToStartIgnition, 0f, 101f);
        }
        yield break;
    }

    [Rpc(SendTo.NotOwner)]
    public void TryIgnitionRpc(bool setKeyInSlot, bool isIgnitionOn)
    {
        CancelIgnitionCoroutine();
        disableAnimations = true;
        inIgnitionAnimation = true;
        SetIgnitionKeyValues(setKeyInHand: false, setKeyInSlot: setKeyInSlot);
        if (ignitionOn != isIgnitionOn)
        {
            ignitionOn = isIgnitionOn;
            SetFrontCabinLightOn(setOn: isIgnitionOn);
            if (dashboardSweepCoroutine == null && !hasSweepedDashboard)
            {
                dashboardSweepCoroutine = StartCoroutine(DashboardSweepCoroutine());
            }
        }
        keyIgnitionCoroutine = StartCoroutine(TryIgnition(isLocalDriver: false));
    }

    [Rpc(SendTo.NotOwner)]
    public void TryStartIgnitionRpc(bool shiftInterlock)
    {
        if (shiftInterlock) PlayIgnitionAudio();
        if (!ignitionOn)
        {
            ignitionOn = true;
            if (dashboardSweepCoroutine == null && !hasSweepedDashboard)
            {
                dashboardSweepCoroutine = StartCoroutine(DashboardSweepCoroutine());
            }
        }
        SetIgnitionKeyValues(setKeyInHand: true, setKeyInSlot: true);
        SetFrontCabinLightOn(setOn: ignitionOn);
        SetDashboardGaugesOn(on: true);
    }

    private void PlayIgnitionAudio()
    {
        if (!hasPlayedIgnitionChime)
        {
            hasPlayedIgnitionChime = true;
            cabinAudio.Play();
        }
        engineAudio1.Stop();
        engineAudio1.clip = revEngineStart;
        engineAudio1.volume = 0.7f;
        engineAudio1.PlayOneShot(engineRev);
        carEngine1AudioActive = true;
        engineAudio1.pitch = 1f;
    }


    // --- CANCEL IGNITION METHOD ---
    public new void CancelTryCarIgnition()
    {
        if (!localPlayerInControl)
            return;

        if (ignitionStarted ||
            !inIgnitionAnimation ||
            (!inIgnitionAnimation && startIgnitionTrigger.isBeingHeldByPlayer))
            return;

        PlayerControllerB playerController = GameNetworkManager.Instance.localPlayerController;
        int playerAnimIndex = playerController.playerBodyAnimator.GetInteger(CAR_ANIM);
        if (playerAnimIndex == 0 && keyIsInIgnition)
            playerController.playerBodyAnimator.SetInteger(CAR_ANIM, 13);
        else if ((playerAnimIndex == 2 || playerAnimIndex == 12) && keyIsInIgnition)
            playerController.playerBodyAnimator.SetInteger(CAR_ANIM, 3);
        else
            playerController.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        int playerCarAnimIndex = playerController.playerBodyAnimator.GetInteger(CAR_ANIM);

        CancelIgnitionAnimation(isStarted: false, setIgnitionAnim: false);
        carEngine1AudioActive = false;
        disableAnimations = true;
        inIgnitionAnimation = false;

        int ignitionAnimIndex = playerCarAnimIndex;
        if (playerAnimIndex == 13) ignitionAnimIndex = 3;
        ignitionAnimator.SetInteger(IGNITION_ANIM, ignitionAnimIndex);

        CancelTryIgnitionRpc(
            setKeyInSlot: keyIsInIgnition, 
            isIgnitionOn: ignitionOn, 
            playerAnimIndex: playerCarAnimIndex, 
            ignitionAnimIndex: ignitionAnimIndex);
    }

    [Rpc(SendTo.NotOwner)]
    public void CancelTryIgnitionRpc(bool setKeyInSlot, bool isIgnitionOn, int playerAnimIndex, int ignitionAnimIndex)
    {
        CancelIgnitionAnimation(isStarted: false, setIgnitionAnim: false);
        carEngine1AudioActive = false;
        disableAnimations = true;
        inIgnitionAnimation = false;

        currentDriver?.playerBodyAnimator.SetInteger(CAR_ANIM, playerAnimIndex);
        ignitionAnimator.SetInteger(IGNITION_ANIM, ignitionAnimIndex);

        if (setKeyInSlot && !keyIsInIgnition)
        {
            carKeyAudio.PlayOneShot(insertKey);
        }
        SetIgnitionKeyValues(setKeyInHand: false, setKeyInSlot: setKeyInSlot);
        SetDashboardGaugesOn(on: isIgnitionOn);
        if (ignitionOn != isIgnitionOn)
        {
            ignitionOn = isIgnitionOn;
            SetFrontCabinLightOn(setOn: ignitionOn);
            if (dashboardSweepCoroutine == null && !hasSweepedDashboard)
            {
                dashboardSweepCoroutine = StartCoroutine(DashboardSweepCoroutine());
            }
        }
    }


    // --- START IGNITION METHOD ---
    [Rpc(SendTo.NotOwner)]
    public void StartIgnitionRpc()
    {
        CancelIgnitionAnimation(isStarted: true, setIgnitionAnim: true);
        disableAnimations = false;
        inIgnitionAnimation = false;
        currentDriver?.playerBodyAnimator.SetInteger(CAR_ANIM, 1);
        SetIgnitionKeyValues(setKeyInHand: false, setKeyInSlot: true);
        SetIgnition(setStarted: true, setCabinLight: true);
        if (!ignitionOn)
        {
            ignitionOn = true;
            if (dashboardSweepCoroutine == null && !hasSweepedDashboard)
            {
                dashboardSweepCoroutine = StartCoroutine(DashboardSweepCoroutine());
            }
        }
        SetDashboardGaugesOn(on: true);
    }

    public void SetIgnition(bool setStarted, bool setCabinLight)
    {
        SetFrontCabinLightOn(setCabinLight);
        if (ignitionStarted == setStarted)
        {
            return;
        }
        ignitionStarted = setStarted;
        carEngine1AudioActive = setStarted;
        if (setStarted)
        {
            startIgnitionCollider.enabled = false;
            stopIgnitionCollider.enabled = true;
            startIgnitionTrigger.currentCooldownValue = startIgnitionTrigger.cooldownTime;
            stopIgnitionTrigger.currentCooldownValue = stopIgnitionTrigger.cooldownTime;
            carExhaustParticle.Play();
            engineAudio1.Stop();
            engineAudio1.PlayOneShot(engineStartSuccessful);
            engineAudio1.clip = engineRun;
            return;
        }
        startIgnitionCollider.enabled = true;
        stopIgnitionCollider.enabled = false;
        startIgnitionTrigger.currentCooldownValue = startIgnitionTrigger.cooldownTime;
        stopIgnitionTrigger.currentCooldownValue = stopIgnitionTrigger.cooldownTime;
        carExhaustParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }


    // --- REMOVE IGNITION METHOD ---
    public new void RemoveKeyFromIgnition()
    {
        if (!localPlayerInControl)
            return;

        if (!ignitionStarted || inIgnitionAnimation)
            return;

        CancelIgnitionCoroutine();
        keyIgnitionCoroutine = StartCoroutine(RemoveKey());
        chanceToStartIgnition = 10f;
        RemoveKeyFromIgnitionRpc();
    }

    private new IEnumerator RemoveKey()
    {
        disableAnimations = true;
        inIgnitionAnimation = false;
        currentDriver?.playerBodyAnimator.SetInteger(CAR_ANIM, 6);
        playerSteeringWheelAnimFloat = 0.5f;
        syncedPlayerSteeringAnim = 0.5f;
        ignitionAnimator.SetInteger(IGNITION_ANIM, 6);
        yield return new WaitForSeconds(0.28f);
        SetIgnitionKeyValues(setKeyInHand: true, setKeyInSlot: false);
        carKeyAudio.PlayOneShot(removeKey);
        SetIgnition(setStarted: false, setCabinLight: false);
        ignitionOn = false;
        hasPlayedIgnitionChime = false;
        CancelDashboardSweep();
        SetDashboardGaugesOn(on: false);
        yield return new WaitForSeconds(0.73f);
        SetIgnitionKeyValues(setKeyInHand: false, setKeyInSlot: false);
        currentDriver?.playerBodyAnimator.SetFloat(ANIMATION_SPEED, 0.5f);
        keyIgnitionCoroutine = null;
        yield break;
    }

    [Rpc(SendTo.NotOwner)]
    public void RemoveKeyFromIgnitionRpc()
    {
        CancelIgnitionCoroutine();
        keyIgnitionCoroutine = StartCoroutine(RemoveKey());
        chanceToStartIgnition = 10f;
    }


    // --- MISC IGNITION STUFF ---
    public void CancelIgnitionAnimation(bool isStarted = false, bool setIgnitionAnim = false)
    {
        CancelIgnitionCoroutine();
        keyIsInDriverHand = false;
        if (setIgnitionAnim) ignitionAnimator.SetInteger(IGNITION_ANIM, isStarted ? 1 : 0);
    }

    public void CancelIgnitionCoroutine()
    {
        if (keyIgnitionCoroutine != null)
        {
            StopCoroutine(keyIgnitionCoroutine);
            keyIgnitionCoroutine = null;
        }
    }

    public void SetIgnitionKeyValues(bool setKeyInHand, bool setKeyInSlot)
    {
        keyIsInDriverHand = setKeyInHand;
        keyIsInIgnition = setKeyInSlot;
    }


    // --- GENERAL REPEAT METHODS ---
    public void SetTriggerHoverTip(InteractTrigger trigger, string tip)
    {
        trigger.hoverTip = tip;
    }


    // --- CANCEL OCCUPANT METHOD ---
    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void CancelSetPlayerInVehicleRpc(NetworkBehaviourReference playerNetObjRef)
    {
        if (!playerNetObjRef.TryGet(out PlayerControllerB playerController))
        {
            Plugin.LogError("CancelSetPlayerInVehicleRpc failed to find player reference from network behaviour!");
            return;
        }
        if (!VehicleUtils.IsPlayerLocal(playerController))
            return;
        InteractTriggerPatches.CancelVehicleSeatInteraction();
    }


    // --- DRIVER OCCUPANT METHODS ---
    public void SetDriverInVehicle()
    {
        SetDriverInVehicleServerRpc(GameNetworkManager.Instance.localPlayerController);
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void SetDriverInVehicleServerRpc(NetworkBehaviourReference playerNetObjRef, RpcParams rpcParams = default)
    {
        if (!playerNetObjRef.TryGet(out PlayerControllerB playerController))
        {
            Plugin.LogError("SetDriverInVehicleServerRpc failed to find player reference from network behaviour!");
            return;
        }
        if (playerController.isPlayerDead ||
            !playerController.isPlayerControlled ||
            currentDriver != null)
        {
            CancelSetPlayerInVehicleRpc(playerNetObjRef);
            return;
        }
        currentDriver = playerController;
        NetworkObject.ChangeOwnership(rpcParams.Receive.SenderClientId);
        SetDriverInVehicleOwnerRpc();
    }

    [Rpc(SendTo.Owner, RequireOwnership = false)]
    public void SetDriverInVehicleOwnerRpc()
    {
        frontLeftSeat.SetLocalPlayerIntoSeat();
        ActivateControl();
        disableAnimations = !ignitionStarted;
        inIgnitionAnimation = false;
        SetTriggerHoverTip(driverSideDoorTrigger, DOOR_EXIT_HOVERTIP);
        startIgnitionTrigger.isBeingHeldByPlayer = false;
        stopIgnitionTrigger.isBeingHeldByPlayer = false;
        CancelIgnitionAnimation(ignitionStarted, true);
        PlayerControllerB playerController = GameNetworkManager.Instance.localPlayerController;
        if (ignitionStarted) playerController.playerBodyAnimator.SetInteger(CAR_ANIM, 1);
        else playerController.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        int playerAnimIndex = playerController.playerBodyAnimator.GetInteger(CAR_ANIM);
        playerController.playerBodyAnimator.SetFloat(ANIMATION_SPEED, 0.5f);
        playerSteeringWheelAnimFloat = 0.5f;
        syncedPlayerSteeringAnim = 0.5f;
        if (ignitionOn && (dashboardSweepCoroutine == null && !hasSweepedDashboard))
        {
            dashboardSweepCoroutine = StartCoroutine(DashboardSweepCoroutine());
        }
        else if (!ignitionOn && (dashboardSweepCoroutine != null || hasSweepedDashboard))
        {
            CancelDashboardSweep();
        }
        SetIgnition(ignitionStarted, ignitionOn);
        SetDashboardGaugesOn(on: ignitionOn);
        if (driverSideDoor.boolValue) driverSideDoor.TriggerAnimation(playerController);
        SetHudTooltips();
        SetDriverInVehicleNotOwnerRpc(
            playerController,
            ignitionOn,
            keyIsInIgnition,
            ignitionStarted,
            playerAnimIndex);
    }

    private string GetBindingKey(InputAction action)
    {
        string group = StartOfRound.Instance.localPlayerUsingController
            ? "Gamepad"
            : "KeyboardAndMouse";

        string binding = action.GetBindingDisplayString(group: group);
        return string.IsNullOrEmpty(binding) ||
               binding.Contains("Not-Bound")
            ? "Unbound"
            : binding;
    }

    private void SetHudTooltips()
    {
        if (setControlTips)
            return;

        VehicleControls vehicleControls = Plugin.VehicleControlsInstance;
        string gasKey = GetBindingKey(vehicleControls.GasPedalKey);
        string brakeKey = GetBindingKey(vehicleControls.BrakePedalKey);
        string jumpKey = GetBindingKey(vehicleControls.JumpKey);

        carTooltips = new string[3]
        {
            $"Gas pedal : [{gasKey}]",
            $"Brake pedal : [{brakeKey}]",
            $"Boost : [{jumpKey}]",
        };

        HUDManager.Instance.ChangeControlTipMultiple(carTooltips, false, null);
        setControlTips = true;
    }

    [Rpc(SendTo.NotOwner)]
    public void SetDriverInVehicleNotOwnerRpc(NetworkBehaviourReference playerNetObjRef, bool isIgnitionOn, bool setKeyInSlot, bool setStarted, int playerAnimIndex)
    {
        if (!playerNetObjRef.TryGet(out PlayerControllerB playerController))
        {
            Plugin.LogError("SetDriverInVehicleNotOwnerRpc failed to find player reference from network behaviour!");
            return;
        }
        currentDriver = playerController;
        driverSeatTrigger.interactable = false;
        frontLeftSeat.SetPlayerAnimations(playerController, false);
        drivePedalPressed = false;
        brakePedalPressed = false;
        disableAnimations = !setStarted;
        inIgnitionAnimation = false;
        startIgnitionTrigger.isBeingHeldByPlayer = false;
        stopIgnitionTrigger.isBeingHeldByPlayer = false;
        CancelIgnitionAnimation(setStarted, true);
        playerController.playerBodyAnimator.SetInteger(CAR_ANIM, playerAnimIndex);
        playerController.playerBodyAnimator.SetFloat(ANIMATION_SPEED, 0.5f);
        steeringAnimValue = 0f;
        playerSteeringWheelAnimFloat = 0.5f;
        syncedPlayerSteeringAnim = 0.5f;
        keyIsInIgnition = setKeyInSlot;
        if (isIgnitionOn && !ignitionOn)
        {
            ignitionOn = true;
            if (dashboardSweepCoroutine == null && !hasSweepedDashboard)
            {
                dashboardSweepCoroutine = StartCoroutine(DashboardSweepCoroutine());
            }
        }
        else if (!isIgnitionOn && ignitionOn)
        {
            ignitionOn = false;
            if (dashboardSweepCoroutine != null || hasSweepedDashboard)
            {
                CancelDashboardSweep();
            }
        }
        SetIgnition(setStarted, isIgnitionOn);
        SetDashboardGaugesOn(on: isIgnitionOn);
    }

    public void OnDriverExitVehicle()
    {
        if (!IsSpawned ||
            NetworkManager == null ||
            !NetworkManager.IsListening)
        {
            return;
        }
        if (!VehicleUtils.IsPlayerLocal(currentDriver))
        {
            return;
        }
        DisableControl();
        disableAnimations = !ignitionStarted;
        inIgnitionAnimation = false;
        SetTriggerHoverTip(driverSideDoorTrigger, DOOR_ENTER_HOVERTIP);
        startIgnitionTrigger.isBeingHeldByPlayer = false;
        stopIgnitionTrigger.isBeingHeldByPlayer = false;
        CancelIgnitionAnimation(ignitionStarted, true);
        PlayerControllerB playerController = GameNetworkManager.Instance.localPlayerController;
        playerController.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        playerController.playerBodyAnimator.SetFloat(ANIMATION_SPEED, 0.5f);
        playerSteeringWheelAnimFloat = 0.5f;
        syncedPlayerSteeringAnim = 0.5f;
        if (ignitionOn && (dashboardSweepCoroutine == null && !hasSweepedDashboard))
        {
            dashboardSweepCoroutine = StartCoroutine(DashboardSweepCoroutine());
        }
        else if (!ignitionOn && (dashboardSweepCoroutine != null || hasSweepedDashboard))
        {
            CancelDashboardSweep();
        }
        SetIgnition(ignitionStarted, ignitionOn);
        SetDashboardGaugesOn(on: ignitionOn);
        chanceToStartIgnition = 10f;
        syncedPosition = transform.position;
        syncedRotation = transform.rotation;
        OnDriverExitVehicleServerRpc(
            playerController,
            syncedPosition,
            syncedRotation,
            keyIsInIgnition,
            ignitionStarted,
            ignitionOn);
    }

    [Rpc(SendTo.Server)]
    public void OnDriverExitVehicleServerRpc(NetworkBehaviourReference playerNetObjRef, Vector3 carLocation, Quaternion carRotation, bool setKeyInSlot, bool setStarted, bool isIgnitionOn)
    {
        OnDriverExitVehicleRpc(playerNetObjRef, carLocation, carRotation, setKeyInSlot, setStarted, isIgnitionOn);
        NetworkObject.ChangeOwnership(NetworkManager.ServerClientId);
    }

    [Rpc(SendTo.NotOwner, RequireOwnership = false)]
    public void OnDriverExitVehicleRpc(NetworkBehaviourReference playerNetObjRef, Vector3 carLocation, Quaternion carRotation, bool setKeyInSlot, bool setStarted, bool isIgnitionOn)
    {
        if (!playerNetObjRef.TryGet(out PlayerControllerB playerController))
        {
            Plugin.LogError("OnDriverExitVehicleRpc failed to find player reference from network behaviour!");
            return;
        }
        if (VehicleUtils.IsPlayerLocal(playerController))
        {
            Plugin.LogDebug("OnDriverExitVehicleRpc player argument was the previous occupant.");
            return;
        }
        syncedPosition = carLocation;
        syncedRotation = carRotation;
        frontLeftSeat.ReturnPlayerAnimations(playerController, false);
        currentDriver = null;
        driverSeatTrigger.interactable = true;
        drivePedalPressed = false;
        brakePedalPressed = false;
        disableAnimations = !setStarted;
        inIgnitionAnimation = false;
        startIgnitionTrigger.isBeingHeldByPlayer = false;
        stopIgnitionTrigger.isBeingHeldByPlayer = false;
        CancelIgnitionAnimation(setStarted, true);
        chanceToStartIgnition = 10f;
        playerController.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        playerController.playerBodyAnimator.SetFloat(ANIMATION_SPEED, 0.5f);
        steeringAnimValue = 0f;
        playerSteeringWheelAnimFloat = 0.5f;
        syncedPlayerSteeringAnim = 0.5f;
        keyIsInIgnition = setKeyInSlot;
        if (isIgnitionOn && !ignitionOn)
        {
            ignitionOn = true;
            if (dashboardSweepCoroutine == null && !hasSweepedDashboard)
            {
                dashboardSweepCoroutine = StartCoroutine(DashboardSweepCoroutine());
            }
        }
        else if (!isIgnitionOn && ignitionOn)
        {
            ignitionOn = false;
            if (dashboardSweepCoroutine != null || hasSweepedDashboard)
            {
                CancelDashboardSweep();
            }
        }
        SetIgnition(setStarted, isIgnitionOn);
        SetDashboardGaugesOn(on: isIgnitionOn);
    }

    public new void ExitDriverSideSeat()
    {
        if (!localPlayerInControl)
            return;

        PlayerControllerB playerController = GameNetworkManager.Instance.localPlayerController;
        int exitPoint = CanExitVehicle(exitPoints: driverSideExitPoints);
        if (exitPoint == 0) if (!driverSideDoor.boolValue) driverSideDoor.TriggerAnimation(playerController);
        if (exitPoint != -1)
        {
            playerController.TeleportPlayer(driverSideExitPoints[exitPoint].position);
            return;
        }
        playerController.TeleportPlayer(driverSideExitPoints[1].position);
    }


    // --- PASSENGER OCCUPANT METHODS ---
    public void SetPassengerInVehicle()
    {
        SetPassengerInVehicleServerRpc(GameNetworkManager.Instance.localPlayerController);
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void SetPassengerInVehicleServerRpc(NetworkBehaviourReference playerNetObjRef)
    {
        if (!playerNetObjRef.TryGet(out PlayerControllerB playerController))
        {
            Plugin.LogError("SetPassengerInVehicleServerRpc failed to find player reference from network behaviour!");
            return;
        }
        if (playerController.isPlayerDead ||
            !playerController.isPlayerControlled ||
            currentPassenger != null)
        {
            CancelSetPlayerInVehicleRpc(playerNetObjRef);
            return;
        }
        currentPassenger = playerController;
        SetPassengerInVehicleRpc(playerController);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void SetPassengerInVehicleRpc(NetworkBehaviourReference playerNetObjRef)
    {
        if (!playerNetObjRef.TryGet(out PlayerControllerB playerController))
        {
            Plugin.LogError("SetPassengerInVehicleRpc failed to find player reference from network behaviour!");
            return;
        }
        PlayerControllerB localPlayerController = GameNetworkManager.Instance.localPlayerController;
        playerController.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        playerController.playerBodyAnimator.SetFloat(ANIMATION_SPEED, 0.5f);
        if (playerController == localPlayerController)
        {
            frontRightSeat.SetLocalPlayerIntoSeat();
            localPlayerInPassengerSeat = true;
            SetTriggerHoverTip(passengerSideDoorTrigger, DOOR_EXIT_HOVERTIP);
            if (passengerSideDoor.boolValue) passengerSideDoor.TriggerAnimation(localPlayerController);
        }
        else
        {
            passengerSeatTrigger.interactable = false;
            frontRightSeat.SetPlayerAnimations(playerController, false);
        }
        currentPassenger = playerController;
    }

    public void OnPassengerExitVehicle()
    {
        if (!IsSpawned ||
            NetworkManager == null ||
            !NetworkManager.IsListening)
        {
            return;
        }
        if (!VehicleUtils.IsPlayerLocal(currentPassenger))
        {
            return;
        }
        localPlayerInPassengerSeat = false;
        SetTriggerHoverTip(passengerSideDoorTrigger, DOOR_ENTER_HOVERTIP);
        PlayerControllerB playerController = GameNetworkManager.Instance.localPlayerController;
        playerController.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        playerController.playerBodyAnimator.SetFloat(ANIMATION_SPEED, 0.5f);
        currentPassenger = null;
        OnPassengerExitVehicleRpc(
            playerController,
            playerController.transform.position);
    }

    [Rpc(SendTo.NotMe, RequireOwnership = false)]
    public void OnPassengerExitVehicleRpc(NetworkBehaviourReference playerNetObjRef, Vector3 exitPoint)
    {
        if (!playerNetObjRef.TryGet(out PlayerControllerB playerController))
        {
            Plugin.LogError("OnPassengerExitVehicleRpc failed to find player reference from network behaviour!");
            return;
        }
        frontRightSeat.ReturnPlayerAnimations(playerController, false);
        playerController.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        playerController.playerBodyAnimator.SetFloat(ANIMATION_SPEED, 0.5f);
        playerController.TeleportPlayer(exitPoint, false, 0f, false, true);
        currentPassenger = null;
        passengerSeatTrigger.interactable = true;
    }

    public new void ExitPassengerSideSeat()
    {
        if (!localPlayerInPassengerSeat)
            return;

        PlayerControllerB playerController = GameNetworkManager.Instance.localPlayerController;
        int exitPoint = CanExitVehicle(exitPoints: passengerSideExitPoints);
        if (exitPoint == 0) if (!passengerSideDoor.boolValue) passengerSideDoor.TriggerAnimation(playerController);
        if (exitPoint != -1)
        {
            playerController.TeleportPlayer(passengerSideExitPoints[exitPoint].position);
            return;
        }
        playerController.TeleportPlayer(passengerSideExitPoints[1].position);
    }


    // --- BACK LEFT PASSENGER OCCUPANT METHODS ---
    public void SetBackLeftPassengerInVehicle()
    {
        SetBackLeftPassengerInVehicleServerRpc(GameNetworkManager.Instance.localPlayerController);
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void SetBackLeftPassengerInVehicleServerRpc(NetworkBehaviourReference playerNetObjRef)
    {
        if (!playerNetObjRef.TryGet(out PlayerControllerB playerController))
        {
            Plugin.LogError("SetBackLeftPassengerInVehicleServerRpc failed to find player reference from network behaviour!");
            return;
        }
        if (playerController.isPlayerDead ||
            !playerController.isPlayerControlled ||
            currentBackLeftPassenger != null)
        {
            CancelSetPlayerInVehicleRpc(playerNetObjRef);
            return;
        }
        currentBackLeftPassenger = playerController;
        SetBackLeftPassengerInVehicleRpc(playerController);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void SetBackLeftPassengerInVehicleRpc(NetworkBehaviourReference playerNetObjRef)
    {
        if (!playerNetObjRef.TryGet(out PlayerControllerB playerController))
        {
            Plugin.LogError("SetBackLeftPassengerInVehicleRpc failed to find player reference from network behaviour!");
            return;
        }
        PlayerControllerB localPlayerController = GameNetworkManager.Instance.localPlayerController;
        playerController.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        playerController.playerBodyAnimator.SetFloat(ANIMATION_SPEED, 0.5f);
        if (playerController == localPlayerController)
        {
            backLeftSeat.SetLocalPlayerIntoSeat();
            localPlayerInBackLeftPassengerSeat = true;
            SetTriggerHoverTip(backLeftDoorTrigger, DOOR_EXIT_HOVERTIP);
            if (backLeftDoor.boolValue) backLeftDoor.TriggerAnimation(localPlayerController);
        }
        else
        {
            backLeftPassengerSeatTrigger.interactable = false;
            backLeftSeat.SetPlayerAnimations(playerController, false);
        }
        currentBackLeftPassenger = playerController;
    }

    public void OnBackLeftPassengerExitVehicle()
    {
        if (!IsSpawned ||
            NetworkManager == null ||
            !NetworkManager.IsListening)
        {
            return;
        }
        if (!VehicleUtils.IsPlayerLocal(currentBackLeftPassenger))
        {
            return;
        }
        localPlayerInBackLeftPassengerSeat = false;
        SetTriggerHoverTip(backLeftDoorTrigger, DOOR_ENTER_HOVERTIP);
        PlayerControllerB playerController = GameNetworkManager.Instance.localPlayerController;
        playerController.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        playerController.playerBodyAnimator.SetFloat(ANIMATION_SPEED, 0.5f);
        currentBackLeftPassenger = null;
        OnBackLeftPassengerExitVehicleRpc(
            playerController,
            playerController.transform.position);
    }

    [Rpc(SendTo.NotMe, RequireOwnership = false)]
    public void OnBackLeftPassengerExitVehicleRpc(NetworkBehaviourReference playerNetObjRef, Vector3 exitPoint)
    {
        if (!playerNetObjRef.TryGet(out PlayerControllerB playerController))
        {
            Plugin.LogError("OnBackLeftPassengerExitVehicleRpc failed to find player reference from network behaviour!");
            return;
        }
        backLeftSeat.ReturnPlayerAnimations(playerController, false);
        playerController.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        playerController.playerBodyAnimator.SetFloat(ANIMATION_SPEED, 0.5f);
        playerController.TeleportPlayer(exitPoint, false, 0f, false, true);
        currentBackLeftPassenger = null;
        backLeftPassengerSeatTrigger.interactable = true;
    }

    public void ExitBackLeftPassengerSideSeat()
    {
        if (!localPlayerInBackLeftPassengerSeat)
            return;

        PlayerControllerB playerController = GameNetworkManager.Instance.localPlayerController;
        int exitPoint = CanExitVehicle(exitPoints: backLeftExitPoints);
        if (exitPoint == 0) if (!backLeftDoor.boolValue) backLeftDoor.TriggerAnimation(playerController);
        if (exitPoint != -1)
        {
            playerController.TeleportPlayer(backLeftExitPoints[exitPoint].position);
            return;
        }
        playerController.TeleportPlayer(backLeftExitPoints[1].position);
    }


    // --- MIDDLE PASSENGER OCCUPANT METHODS ---
    public void SetBackMiddlePassengerInVehicle()
    {
        SetBackMiddlePassengerInVehicleServerRpc(GameNetworkManager.Instance.localPlayerController);
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void SetBackMiddlePassengerInVehicleServerRpc(NetworkBehaviourReference playerNetObjRef)
    {
        if (!playerNetObjRef.TryGet(out PlayerControllerB playerController))
        {
            Plugin.LogError("SetBackMiddlePassengerInVehicleServerRpc failed to find player reference from network behaviour!");
            return;
        }
        if (playerController.isPlayerDead ||
            !playerController.isPlayerControlled ||
            currentBackMiddlePassenger != null)
        {
            CancelSetPlayerInVehicleRpc(playerNetObjRef);
            return;
        }
        currentBackMiddlePassenger = playerController;
        SetBackMiddlePassengerInVehicleRpc(playerController);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void SetBackMiddlePassengerInVehicleRpc(NetworkBehaviourReference playerNetObjRef)
    {
        if (!playerNetObjRef.TryGet(out PlayerControllerB playerController))
        {
            Plugin.LogError("SetBackMiddlePassengerInVehicleRpc failed to find player reference from network behaviour!");
            return;
        }
        PlayerControllerB localPlayerController = GameNetworkManager.Instance.localPlayerController;
        playerController.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        playerController.playerBodyAnimator.SetFloat(ANIMATION_SPEED, 0.5f);
        if (playerController == localPlayerController)
        {
            backSeat.SetLocalPlayerIntoSeat();
            localPlayerInMiddlePassengerSeat = true;
            SetTriggerHoverTip(backLeftDoorTrigger, DOOR_EXIT_HOVERTIP);
            SetTriggerHoverTip(backRightDoorTrigger, DOOR_EXIT_HOVERTIP);
            if (backLeftDoor.boolValue) backLeftDoor.TriggerAnimation(localPlayerController);
            if (backRightDoor.boolValue) backRightDoor.TriggerAnimation(localPlayerController);
        }
        else
        {
            backPassengerSeatTrigger.interactable = false;
            backSeat.SetPlayerAnimations(playerController, false);
        }
        currentBackMiddlePassenger = playerController;
    }

    public void OnBackMiddlePassengerExitVehicle()
    {
        if (!IsSpawned ||
            NetworkManager == null ||
            !NetworkManager.IsListening)
        {
            return;
        }
        if (!VehicleUtils.IsPlayerLocal(currentBackMiddlePassenger))
        {
            return;
        }
        localPlayerInMiddlePassengerSeat = false;
        SetTriggerHoverTip(backLeftDoorTrigger, DOOR_ENTER_HOVERTIP);
        SetTriggerHoverTip(backRightDoorTrigger, DOOR_ENTER_HOVERTIP);
        PlayerControllerB playerController = GameNetworkManager.Instance.localPlayerController;
        playerController.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        playerController.playerBodyAnimator.SetFloat(ANIMATION_SPEED, 0.5f);
        currentBackMiddlePassenger = null;
        OnBackMiddlePassengerExitVehicleRpc(
            playerController,
            playerController.transform.position);
    }

    [Rpc(SendTo.NotMe, RequireOwnership = false)]
    public void OnBackMiddlePassengerExitVehicleRpc(NetworkBehaviourReference playerNetObjRef, Vector3 exitPoint)
    {
        if (!playerNetObjRef.TryGet(out PlayerControllerB playerController))
        {
            Plugin.LogError("OnBackMiddlePassengerExitVehicleRpc failed to find player reference from network behaviour!");
            return;
        }
        backSeat.ReturnPlayerAnimations(playerController, false);
        playerController.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        playerController.playerBodyAnimator.SetFloat(ANIMATION_SPEED, 0.5f);
        playerController.TeleportPlayer(exitPoint, false, 0f, false, true);
        currentBackMiddlePassenger = null;
        backPassengerSeatTrigger.interactable = true;
    }

    public void ExitMiddlePassengerSideSeat(bool offSide)
    {
        if (!localPlayerInMiddlePassengerSeat)
            return;

        PlayerControllerB playerController = GameNetworkManager.Instance.localPlayerController;
        Transform[] exitSidePoints = offSide ? backRightExitPoints : backLeftExitPoints;
        int exitPoint = CanExitVehicle(exitPoints: exitSidePoints);
        AnimatedObjectTrigger door = offSide ? backRightDoor : backLeftDoor;
        if (exitPoint == 0) if (!door.boolValue) door.TriggerAnimation(playerController);
        if (exitPoint != -1)
        {
            playerController.TeleportPlayer(exitSidePoints[exitPoint].position);
            return;
        }
        playerController.TeleportPlayer(exitSidePoints[1].position);
    }


    // --- BACK RIGHT PASSENGER OCCUPANT METHODS ---
    public void SetBackRightPassengerInVehicle()
    {
        SetBackRightPassengerInVehicleServerRpc(GameNetworkManager.Instance.localPlayerController);
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void SetBackRightPassengerInVehicleServerRpc(NetworkBehaviourReference playerNetObjRef)
    {
        if (!playerNetObjRef.TryGet(out PlayerControllerB playerController))
        {
            Plugin.LogError("SetBackRightPassengerInVehicleServerRpc failed to find player reference from network behaviour!");
            return;
        }
        if (playerController.isPlayerDead ||
            !playerController.isPlayerControlled ||
            currentBackRightPassenger != null)
        {
            CancelSetPlayerInVehicleRpc(playerNetObjRef);
            return;
        }
        currentBackRightPassenger = playerController;
        SetBackRightPassengerInVehicleRpc(playerController);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void SetBackRightPassengerInVehicleRpc(NetworkBehaviourReference playerNetObjRef)
    {
        if (!playerNetObjRef.TryGet(out PlayerControllerB playerController))
        {
            Plugin.LogError("SetBackRightPassengerInVehicleRpc failed to find player reference from network behaviour!");
            return;
        }
        PlayerControllerB localPlayerController = GameNetworkManager.Instance.localPlayerController;
        playerController.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        playerController.playerBodyAnimator.SetFloat(ANIMATION_SPEED, 0.5f);
        if (playerController == localPlayerController)
        {
            backRightSeat.SetLocalPlayerIntoSeat();
            localPlayerInBackRightPassengerSeat = true;
            SetTriggerHoverTip(backRightDoorTrigger, DOOR_EXIT_HOVERTIP);
            if (backRightDoor.boolValue) backRightDoor.TriggerAnimation(localPlayerController);
        }
        else
        {
            backRightPassengerSeatTrigger.interactable = false;
            backRightSeat.SetPlayerAnimations(playerController, false);
        }
        currentBackRightPassenger = playerController;
    }

    public void OnBackRightPassengerExitVehicle()
    {
        if (!IsSpawned ||
            NetworkManager == null ||
            !NetworkManager.IsListening)
        {
            return;
        }
        if (!VehicleUtils.IsPlayerLocal(currentBackRightPassenger))
        {
            return;
        }
        localPlayerInBackRightPassengerSeat = false;
        SetTriggerHoverTip(backRightDoorTrigger, DOOR_ENTER_HOVERTIP);
        PlayerControllerB playerController = GameNetworkManager.Instance.localPlayerController;
        playerController.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        playerController.playerBodyAnimator.SetFloat(ANIMATION_SPEED, 0.5f);
        currentBackRightPassenger = null;
        OnBackRightPassengerExitVehicleRpc(
            playerController,
            playerController.transform.position);
    }

    [Rpc(SendTo.NotMe, RequireOwnership = false)]
    public void OnBackRightPassengerExitVehicleRpc(NetworkBehaviourReference playerNetObjRef, Vector3 exitPoint)
    {
        if (!playerNetObjRef.TryGet(out PlayerControllerB playerController))
        {
            Plugin.LogError("OnBackRightPassengerExitVehicleRpc failed to find player reference from network behaviour!");
            return;
        }
        backRightSeat.ReturnPlayerAnimations(playerController, false);
        playerController.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        playerController.playerBodyAnimator.SetFloat(ANIMATION_SPEED, 0.5f);
        playerController.TeleportPlayer(exitPoint, false, 0f, false, true);
        currentBackRightPassenger = null;
        backRightPassengerSeatTrigger.interactable = true;
    }

    public void ExitBackRightPassengerSideSeat()
    {
        if (!localPlayerInBackRightPassengerSeat)
            return;

        PlayerControllerB playerController = GameNetworkManager.Instance.localPlayerController;
        int exitPoint = CanExitVehicle(exitPoints: backRightExitPoints);
        if (exitPoint == 0) if (!backRightDoor.boolValue) backRightDoor.TriggerAnimation(playerController);
        if (exitPoint != -1)
        {
            playerController.TeleportPlayer(backRightExitPoints[exitPoint].position);
            return;
        }
        playerController.TeleportPlayer(backRightExitPoints[1].position);
    }


    // --- LEAVE OCCUPANT MID-GAME METHODS ---
    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void OnDriverLeaveGameServerRpc(NetworkBehaviourReference playerNetObjRef)
    {
        if (!playerNetObjRef.TryGet(out PlayerControllerB playerController))
        {
            Plugin.LogError("OnDriverLeaveGameServerRpc failed to find player reference from network behaviour!");
            return;
        }
        NetworkObject.ChangeOwnership(NetworkManager.ServerClientId);
        OnDriverLeave(
            playerController, 
            ignitionStarted, 
            keyIsInIgnition, 
            ignitionOn);
        OnDriverLeaveGameRpc(
            playerNetObjRef, 
            syncedPosition, 
            syncedRotation, 
            ignitionStarted, 
            keyIsInIgnition, 
            ignitionOn);
    }

    public void OnDriverLeave(PlayerControllerB playerController, bool setStarted, bool setKeyInSlot, bool isIgnitionOn)
    {
        frontLeftSeat.ReturnPlayerAnimations(playerController, false);
        currentDriver = null;
        drivePedalPressed = false;
        brakePedalPressed = false;
        disableAnimations = !setStarted;
        inIgnitionAnimation = false;
        startIgnitionTrigger.isBeingHeldByPlayer = false;
        stopIgnitionTrigger.isBeingHeldByPlayer = false;
        CancelIgnitionAnimation(setStarted, true);
        chanceToStartIgnition = 10f;
        playerController.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        playerController.playerBodyAnimator.SetFloat(ANIMATION_SPEED, 0.5f);
        playerController.TeleportPlayer(StartOfRound.Instance.notSpawnedPosition.position, false, 0f, false, true);
        steeringAnimValue = 0f;
        playerSteeringWheelAnimFloat = 0.5f;
        syncedPlayerSteeringAnim = 0.5f;
        keyIsInIgnition = setKeyInSlot;
        if (isIgnitionOn && !ignitionOn)
        {
            ignitionOn = true;
            if (dashboardSweepCoroutine == null && !hasSweepedDashboard)
            {
                dashboardSweepCoroutine = StartCoroutine(DashboardSweepCoroutine());
            }
        }
        else if (!isIgnitionOn && ignitionOn)
        {
            ignitionOn = false;
            if (dashboardSweepCoroutine != null || hasSweepedDashboard)
            {
                CancelDashboardSweep();
            }
        }
        SetIgnition(setStarted, isIgnitionOn);
        SetDashboardGaugesOn(on: isIgnitionOn);
    }

    [Rpc(SendTo.NotServer, RequireOwnership = false)]
    public void OnDriverLeaveGameRpc(NetworkBehaviourReference playerNetObjRef, Vector3 carLocation, Quaternion carRotation, bool setStarted, bool setKeyInSlot, bool isIgnitionOn)
    {
        if (!playerNetObjRef.TryGet(out PlayerControllerB playerController))
        {
            Plugin.LogError("OnDriverLeaveGameRpc failed to find player reference from network behaviour!");
            return;
        }
        syncedPosition = carLocation;
        syncedRotation = carRotation;
        OnDriverLeave(playerController, setStarted, setKeyInSlot, isIgnitionOn);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void OnPassengerLeaveGameRpc(NetworkBehaviourReference playerNetObjRef)
    {
        if (!playerNetObjRef.TryGet(out PlayerControllerB playerController))
        {
            Plugin.LogError("OnPassengerLeaveGameRpc failed to find player reference from network behaviour!");
            return;
        }
        frontRightSeat.ReturnPlayerAnimations(playerController, false);
        playerController.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        playerController.TeleportPlayer(StartOfRound.Instance.notSpawnedPosition.position, false, 0f, false, true);
        currentPassenger = null!;
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void OnBackLeftPassengerLeaveGameRpc(NetworkBehaviourReference playerNetObjRef)
    {
        if (!playerNetObjRef.TryGet(out PlayerControllerB playerController))
        {
            Plugin.LogError("OnBackLeftPassengerLeaveGameRpc failed to find player reference from network behaviour!");
            return;
        }
        backLeftSeat.ReturnPlayerAnimations(playerController, false);
        playerController.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        playerController.TeleportPlayer(StartOfRound.Instance.notSpawnedPosition.position, false, 0f, false, true);
        currentBackLeftPassenger = null!;
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void OnBackMiddlePassengerLeaveGameRpc(NetworkBehaviourReference playerNetObjRef)
    {
        if (!playerNetObjRef.TryGet(out PlayerControllerB playerController))
        {
            Plugin.LogError("OnBackMiddlePassengerLeaveGameRpc failed to find player reference from network behaviour!");
            return;
        }
        backSeat.ReturnPlayerAnimations(playerController, false);
        playerController.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        playerController.TeleportPlayer(StartOfRound.Instance.notSpawnedPosition.position, false, 0f, false, true);
        currentBackMiddlePassenger = null!;
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void OnBackRightPassengerLeaveGameRpc(NetworkBehaviourReference playerNetObjRef)
    {
        if (!playerNetObjRef.TryGet(out PlayerControllerB playerController))
        {
            Plugin.LogError("OnBackRightPassengerLeaveGameRpc failed to find player reference from network behaviour!");
            return;
        }
        backRightSeat.ReturnPlayerAnimations(playerController, false);
        playerController.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        playerController.TeleportPlayer(StartOfRound.Instance.notSpawnedPosition.position, false, 0f, false, true);
        currentBackRightPassenger = null!;
    }


    // --- OCCUPANT EXITING METHODS ---
    private int CanExitVehicle(Transform[] exitPoints)
    {
        for (int i = 0; i < exitPoints.Length; i++)
        {
            if (!CheckExitPointInvalid(GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position, exitPoints[i].position, exitCarLayerMask, QueryTriggerInteraction.Ignore))
            {
                return i;
            }
        }
        return -1;
    }

    public bool CheckExitPointInvalid(Vector3 playerPos, Vector3 exitPoint, int layerMask, QueryTriggerInteraction interaction)
    {
        if (Physics.Linecast(playerPos, exitPoint, layerMask, interaction))
        {
            return true;
        }

        if (Physics.CheckCapsule(exitPoint, exitPoint + Vector3.up, 0.5f, layerMask, interaction))
        {
            return true;
        }

        LayerMask maskAndVehicle = layerMask | LayerMask.GetMask("Vehicle");

        if (!Physics.Linecast(exitPoint, exitPoint + Vector3.down * 4f, maskAndVehicle, interaction))
        {
            return true;
        }

        return false;
    }


    // --- PLAYER CONTROL ---
    public new void ActivateControl()
    {
        Plugin.VehicleControlsInstance.JumpKey.performed += DoTurboBoost;
        Plugin.VehicleControlsInstance.ToggleMagnetKey.performed += ActivateMagnet;

        if (isInTestMode)
        {
            InputSystem.actions.FindAction("Emote2", false).performed += ChangeGear_Forward;
            InputSystem.actions.FindAction("Emote1", false).performed += ChangeGear_Backward;
        }

        currentDriver = GameNetworkManager.Instance.localPlayerController;
        localPlayerInControl = true;
        steeringAnimValue = 0f;
        steeringWheelAnimValue = 0f;
        drivePedalPressed = false;
        brakePedalPressed = false;
    }

    private new void DisableControl()
    {
        Plugin.VehicleControlsInstance.JumpKey.performed -= DoTurboBoost;
        Plugin.VehicleControlsInstance.ToggleMagnetKey.performed -= ActivateMagnet;

        if (isInTestMode)
        {
            InputSystem.actions.FindAction("Emote2", false).performed -= ChangeGear_Forward;
            InputSystem.actions.FindAction("Emote1", false).performed -= ChangeGear_Backward;
        }

        currentDriver = null;
        localPlayerInControl = false;
        steeringAnimValue = 0f;
        steeringWheelAnimValue = 0f;
        drivePedalPressed = false;
        brakePedalPressed = false;
    }


    // --- KEYBINDS ---

    /// <summary>
    ///  Available from BetterVehicleControls, licensed under MIT License.
    ///  Source: https://github.com/1A3Dev/LC-BetterVehicleControls/blob/master/source/Patches/VehicleController_Patch.cs#L217
    /// </summary>
    public void ActivateMagnet(InputAction.CallbackContext context)
    {
        // i fucking hate this feature, i hate how it has become an expectation, it is objectively not quality of life, it obsoletes the magnet lever completely, that's boring.
        if (!context.performed)
            return;

        float magnetDistance = Vector3.Distance(transform.position, StartOfRound.Instance.magnetPoint.position);
        if (magnetDistance >= 30f)
        {
            Plugin.LogDebug($"Pickup is too far away from the magnet to toggle it. Distance: {magnetDistance}");
            return;
        }

        ScandalLib.Utils.References.magnetTrigger!.TriggerAnimation(GameNetworkManager.Instance.localPlayerController);
        /*
        string newState = ScandalsTweaks.Utils.References.magnetTrigger.boolValue ? "Activated" : "Deactivated";
        HUDManager.Instance.DisplayGlobalNotification($"Ship Magnet {newState}");
        */
    }


    public new void GetVehicleInput()
    {
        PlayerControllerB localDriver = GameNetworkManager.Instance.localPlayerController;
        if (localDriver == null)
            return;

        if (localDriver.isTypingChat ||
            localDriver.quickMenuManager.isMenuOpen)
            return;

        SyncVehicleInput();

        moveInputVector = IngamePlayerSettings.Instance.playerInput.actions.FindAction(PLAYER_MOVEMENT, false).ReadValue<Vector2>();
        moveInputVector.x = StartOfRound.Instance.localPlayerUsingController ? moveInputVector.x : Mathf.Round(moveInputVector.x);
        moveInputVector.y = Mathf.Round(moveInputVector.y);

        steeringAnimValue = moveInputVector.x;

        if (steeringAnimValue == 0)
            steeringWheelAnimValue = Mathf.Lerp(steeringWheelAnimValue, 0, steeringReturnSpeed * Time.deltaTime);
        else
            steeringWheelAnimValue = Mathf.Lerp(steeringWheelAnimValue, steeringAnimValue, steeringSpeed * Time.deltaTime);
        TryRecenterSteeringWheel();

        float gasInputValue = Plugin.VehicleControlsInstance.GasPedalKey.ReadValue<float>();
        float brakeInputValue = Plugin.VehicleControlsInstance.BrakePedalKey.ReadValue<float>();

        drivePedalPressed = gasInputValue > 0.1f && shiftGearCoroutine == null;
        brakePedalPressed = brakeInputValue > 0.1f;
    }

    private void TryRecenterSteeringWheel()
    {
        if (!UserConfig.RecenterWheel.Value)
            return;
        if (Mathf.Abs(moveInputVector.x) > 0.1f)
            return;
        steeringWheelAnimFloat = Mathf.MoveTowards(steeringWheelAnimFloat, 0f, steeringWheelTurnSpeed * Time.deltaTime / 6f);
    }

    private void SyncVehicleInput()
    {
        if (syncedDrivePedalPressed != drivePedalPressed || 
            syncedBrakePedalPressed != brakePedalPressed)
        {
            syncedDrivePedalPressed = drivePedalPressed;
            syncedBrakePedalPressed = brakePedalPressed;
            SyncPlayerInputsRpc(drivePedalPressed, brakePedalPressed);
        }
    }

    [Rpc(SendTo.NotOwner)]
    public void SyncPlayerInputsRpc(bool gasPressed, bool brakePressed)
    {
        syncedDrivePedalPressed = gasPressed;
        syncedBrakePedalPressed = brakePressed;
    }

    // --- PLAYER-VEHICLE COLLISION --- 
    // None of this is needed, with the way we have collisions for players-vehicles setup, we don't need to toggle layers anymore, and 
    // we don't have to worry about buggy host-interactions anymore with our setup.
    public new void EnableVehicleCollisionForAllPlayers()
    {
        Plugin.LogDebug("Attempted to enable collision, but this is unnecessary!");
        return;
    }

    public new void DisableVehicleCollisionForAllPlayers()
    {
        Plugin.LogDebug("Attempted to disable collision, but this is unnecessary!");
        return;
    }

    public new void SetVehicleCollisionForPlayer(bool setEnabled, PlayerControllerB player)
    {
        Plugin.LogDebug("Attempted to set collision, but this is unnecessary!");
        return;
    }


    // --- VEHICLE STATS --- 
    private void InitializePickup()
    {
        gear = CarGearShift.Park;
        MaxEngineRPM = 4500f;
        MinEngineRPM = 900f;
        brakeSpeed = 2300f;
        engineIntensityPercentage = 180f;
        EngineTorque = 2800f;
        engineReversePower = -6000f;
        carAcceleration = 470f;
        carDeacceleration = 3900f;
        reverseCarAcceleration = 3000f;
        idleSpeed = 60f;

        minimalBumpForce = 10000f;
        mediumBumpForce = 38000f;
        maximumBumpForce = 89000f;

        gearRatios = new float[6];
        gearRatios[0] = -4.8f; // reverse
        gearRatios[1] = 5.8f; // first gear
        gearRatios[2] = 3.2f; // second gear
        gearRatios[3] = 2.3f; // third gear
        gearRatios[4] = 1.7f; // fourth gear
        gearRatios[5] = 1.3f; // fifth gear
        diffRatio = 4.4f; // final drive

        upShiftThreshold = 3800f;
        downShiftThreshold = 1400f;

        shiftCooldown = 0.5f;
        shiftTime = 0.25f;

        maxBrakeTorque = 4100f;
        maxParkingBrakeTorque = 4400f;

        minInclineBoost = 1.6f;
        maxInclineBoost = 2.15f;
        maxInclineBoostAngle = 32f;

        carMaxSpeed = 50f;
        mainRigidbody.maxLinearVelocity = carMaxSpeed;
        mainRigidbody.maxAngularVelocity = 4f;

        mainRigidbody.automaticCenterOfMass = false;
        mainRigidbody.centerOfMass = new Vector3(0f, -0.6f, 0.7f); // more biased towards the front, loose rear end
        mainRigidbody.automaticInertiaTensor = false;

        speed = 55;
        stability = 0.55f;

        // maximum weight each wheel can hold up
        // set here so each side has consistent sprung-mass
        FrontLeftWheel.sprungMass = 220f;
        FrontRightWheel.sprungMass = 220f;
        BackLeftWheel.sprungMass = 210f;
        BackRightWheel.sprungMass = 210f;

        forwardWheelSpeed = 5000f;
        reverseWheelSpeed = -5000f;

        syncSpeedMultiplier = 10f;
        syncRotationSpeed = 0.2f;

        movingAverageLength = 20;
        carHitPlayerForceFraction = 30f;
        carReactToPlayerHitMultiplier = 2850f;

        turboBoostForce = 12000f;
        turboBoostUpwardForce = 28800f;

        jumpForce = 3300f;
        pushForceMultiplier = 80f;
        pushVerticalOffsetAmount = 1.1f;
        steeringWheelTurnSpeed = 4.65f;
        torqueForce = 1.85f;

        SetWheelFriction();

        JointSpring suspensionSpring = new JointSpring
        {
            spring = 20000f,
            damper = 3600f,
            targetPosition = 0.5f,
        };

        FrontRightWheel.suspensionSpring = suspensionSpring;
        FrontLeftWheel.suspensionSpring = suspensionSpring;
        BackRightWheel.suspensionSpring = suspensionSpring;
        BackLeftWheel.suspensionSpring = suspensionSpring;

        float dampRate = 0.1f;
        FrontLeftWheel.wheelDampingRate = dampRate;
        FrontRightWheel.wheelDampingRate = dampRate;
        BackRightWheel.wheelDampingRate = dampRate;
        BackLeftWheel.wheelDampingRate = dampRate;

        float wheelMass = 120f;
        FrontLeftWheel.mass = wheelMass;
        FrontRightWheel.mass = wheelMass;
        BackLeftWheel.mass = wheelMass;
        BackRightWheel.mass = wheelMass;

        float forceApp = 0.05f;
        FrontLeftWheel.forceAppPointDistance = forceApp;
        FrontRightWheel.forceAppPointDistance = forceApp;
        BackLeftWheel.forceAppPointDistance = forceApp;
        BackRightWheel.forceAppPointDistance = forceApp;

        backDoorOpen = true; // too lazy to transpile enemy protection to remove this as a condition, so just set it to true all the time

        steeringReturnSpeed = 10f;
        steeringSpeed = 8f;

        gearStickAudio.volume = 0.7f;
    }

    // Tyre friction

    public float asymSlip = 0.87f;
    public float asymVlaue = 0.94f;

    public new void SetWheelFriction()
    {
        WheelFrictionCurve forwardFrictionCurve = new WheelFrictionCurve
        {
            extremumSlip = 5f,
            extremumValue = 3f,
            asymptoteSlip = asymSlip,
            asymptoteValue = asymVlaue,
            stiffness = 1.1f,
        };
        FrontRightWheel.forwardFriction = forwardFrictionCurve;
        FrontLeftWheel.forwardFriction = forwardFrictionCurve;
        BackRightWheel.forwardFriction = forwardFrictionCurve;
        BackLeftWheel.forwardFriction = forwardFrictionCurve;
        WheelFrictionCurve sidewaysFrictionCurve = new WheelFrictionCurve
        {
            extremumSlip = 0.6f,
            extremumValue = 1f,
            asymptoteSlip = 0.75f,
            asymptoteValue = 0.82f,
            stiffness = 0.75f,
        };
        FrontRightWheel.sidewaysFriction = sidewaysFrictionCurve;
        FrontLeftWheel.sidewaysFriction = sidewaysFrictionCurve;
        BackRightWheel.sidewaysFriction = sidewaysFrictionCurve;
        BackLeftWheel.sidewaysFriction = sidewaysFrictionCurve;
    }


    // --- VEHICLE VFX --- 
    private new void SetCarEffects(float setSteering)
    {
        // steering
        setSteering = IsOwner ? setSteering : 0f;

        steeringWheelAnimFloat = Mathf.Clamp(steeringWheelAnimFloat + setSteering * steeringWheelTurnSpeed * Time.deltaTime / 6f, -1f, 1f);
        float playerSteer = Mathf.Clamp((steeringWheelAnimFloat + 1f) / 2f, 0f, 1f) - steeringWheelAnimator.GetFloat(STEERING_WHEEL_SPEED);
        steeringWheelAnimator.SetFloat(STEERING_WHEEL_SPEED, Mathf.Clamp((steeringWheelAnimFloat + 1f) / 2f, 0f, 1f));

        // grab the players current steering animation float
        if (IsOwner && localPlayerInControl && currentDriver != null)
            playerSteeringWheelAnimFloat = currentDriver.playerBodyAnimator.GetFloat(ANIMATION_SPEED) + playerSteer * -2f;

        // misc
        SetCarTyreEffects();
        SetCarDashboard();
        SetCarLighting();
        SetCarGearstick();
        SetCarAudioEffects();
        bool attachedToShipAndInShipPhase = magnetedToShip && (IsInShipPhaseAndNotLoadingLevel() || !StartOfRound.Instance.shipDoorsEnabled);
        if (setAudiosMuted != attachedToShipAndInShipPhase)
        {
            SetVehicleAudioMuted(attachedToShipAndInShipPhase);
            setAudiosMuted = attachedToShipAndInShipPhase;
        }
        /*
        if (attachedToShipAndInShipPhase && windshieldBroken)
        {
            RegenerateWindshield();
        }
        */
        SetCarIgnitionKey();

        if (IsOwner)
        {
            SyncCarEffectsToOtherClients();
            if (syncedExtremeStress != underExtremeStress)
            {
                syncedExtremeStress = underExtremeStress;
                SyncExtremeStressRpc(underExtremeStress);
            }
            return;
        }
        steeringWheelAnimFloat = Mathf.Lerp(steeringWheelAnimFloat, syncedSteeringRotation, steeringSpeed * Time.deltaTime);
        playerSteeringWheelAnimFloat = Mathf.Lerp(playerSteeringWheelAnimFloat, syncedPlayerSteeringAnim, steeringSpeed * Time.deltaTime);
    }


    // --- MISC EFFECTS ---
    // tyre skid effects
    public void SetCarTyreEffects()
    {
        if (IsOwner)
        {
            float skiddingHighest = Vector3.Dot(Vector3.Normalize(mainRigidbody.velocity * 1000f), transform.forward);
            bool skiddingAudioActive = skiddingHighest > -0.6f && skiddingHighest < 0.4f && (averageVelocity.magnitude > 4f || backWheelRPM > 400f);
            if (backWheelsGrounded)
            {
                bool tyreSlip = forwardSlipping || sidewaySlipping;
                if (tyreSlip)
                {
                    skiddingAudioActive = true;
                    skiddingHighest = Mathf.Max(skiddingHighest, 0.8f);
                    if (averageVelocity.magnitude > 8f && !tireSparks.isPlaying)
                    {
                        tireSparks.Play(true);
                    }
                }
                else
                {
                    if (tireSparks.isEmitting)
                    {
                        tireSparks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    }
                }
            }
            else
            {
                skiddingAudioActive = false;
                if (tireSparks.isEmitting)
                {
                    tireSparks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
            bool sparksEmitting = tireSparks.isEmitting;
            if (Mathf.Abs(tyreStress - skiddingHighest) > 0.02f ||
                wheelSlipping != skiddingAudioActive ||
                wheelSkidActive != sparksEmitting)
            {
                tyreStress = skiddingHighest;
                wheelSlipping = skiddingAudioActive;
                wheelSkidActive = sparksEmitting;
                SetTyreStressRpc(skiddingHighest, skiddingAudioActive, sparksEmitting);
            }
            SetVehicleAudioProperties(skiddingAudio, skiddingAudioActive, 0f, skiddingHighest, 3f, true, 1f);
            return;
        }
        if (wheelSkidActive && !tireSparks.isPlaying)
            tireSparks.Play(true);
        else if (!wheelSkidActive && tireSparks.isEmitting)
            tireSparks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        SetVehicleAudioProperties(skiddingAudio, wheelSlipping, 0f, tyreStress, 3f, true, 1f);
    }

    [Rpc(SendTo.NotOwner)]
    public void SetTyreStressRpc(float wheelStress, bool wheelSkidding, bool sparksEmitting)
    {
        tyreStress = wheelStress;
        wheelSlipping = wheelSkidding;
        wheelSkidActive = sparksEmitting;
    }

    public void SetCarIgnitionKey(bool localUseBodyHands = false)
    {
        if (currentDriver == null || keyIsInIgnition)
        {
            if (keyObject.enabled != keyIsInIgnition)
                keyObject.enabled = keyIsInIgnition;

            if (keyObject.transform.parent != ignitionKeyPosition)
                keyObject.transform.SetParent(ignitionKeyPosition);
            keyObject.transform.localScale = ignitionKeyScale;

            if (keyItemHolder.transform.parent != carKeyContainer.transform)
                keyItemHolder.transform.SetParent(carKeyContainer.transform, false);
            keyItemHolder.transform.localScale = Vector3.one;

            keyItemHolder.transform.localPosition = Vector3.zero;
            keyItemHolder.transform.localRotation = Quaternion.identity;

            keyObject.transform.localPosition = Vector3.zero;
            keyObject.transform.localRotation = Quaternion.identity;
            return;
        }

        if (keyIsInDriverHand)
        {
            if (!keyObject.enabled)
                keyObject.enabled = true;

            Transform keyParent;
            Vector3 posOffset, rotOffset;
            bool useLocalHands = localPlayerInControl && !localUseBodyHands;

            keyParent = useLocalHands ? currentDriver.localItemHolder : currentDriver.serverItemHolder;
            posOffset = useLocalHands ? keyPosLocal : keyPosServer;
            rotOffset = useLocalHands ? keyRotLocal : keyRotServer;

            if (keyItemHolder.transform.parent != keyParent.parent)
                keyItemHolder.transform.SetParent(keyParent.parent, false);
            keyItemHolder.transform.localScale = Vector3.one;

            keyItemHolder.transform.localPosition = Vector3.zero;
            keyItemHolder.transform.localRotation = Quaternion.identity;

            if (keyObject.transform.parent != keyItemHolder.transform)
                keyObject.transform.SetParent(keyItemHolder.transform);

            keyObject.transform.localPosition = posOffset;
            keyObject.transform.localRotation = Quaternion.Euler(rotOffset);
        }
        else
        {
            if (keyObject.enabled)
                keyObject.enabled = false;

            if (keyObject.transform.parent != ignitionKeyPosition)
                keyObject.transform.SetParent(ignitionKeyPosition);
            keyObject.transform.localScale = ignitionKeyScale;

            if (keyItemHolder.transform.parent != carKeyContainer.transform)
                keyItemHolder.transform.SetParent(carKeyContainer.transform, false);
            keyItemHolder.transform.localScale = Vector3.one;

            keyItemHolder.transform.localPosition = Vector3.zero;
            keyItemHolder.transform.localRotation = Quaternion.identity;

            keyObject.transform.localPosition = Vector3.zero;
            keyObject.transform.localRotation = Quaternion.identity;
        }
    }

    private void SetDashboardSymbols()
    {
        SetSymbolActive(checkEngineSymbol, checkEngineLightActive);
        SetSymbolActive(tractionControlSymbol, tractionControlLightActive);
        SetSymbolActive(coolantLevelSymbol, coolantLevelLightActive);

        SetSymbolActive(dippedBeamSymbol, dippedBeamLightActive);
        SetSymbolActive(mainBeamSymbol, mainBeamLightActive);

        SetSymbolActive(oilLevelSymbol, oilLevelLightActive);
        SetSymbolActive(parkingBrakeSymbol, parkingBrakeLightActive);
        SetSymbolActive(batteryLowSymbol, batteryLowLightActive);
    }

    private void SetSymbolActive(SpriteRenderer symbolSprite, bool spriteActive)
    {
        if (symbolSprite.enabled == spriteActive)
            return;
        symbolSprite.enabled = spriteActive;
    }

    private IEnumerator DashboardSweepCoroutine()
    {
        SetSymbolActive(leftSignalSymbol, true);
        SetSymbolActive(hazardSignalSymbol, true);
        SetSymbolActive(rightSignalSymbol, true);

        currentSweepStage = 0;
        yield return new WaitForSeconds(0.48f);
        SetSymbolActive(immobiliserSymbol, false);

        currentSweepStage = 1;
        yield return new WaitForSeconds(0.35f);
        SetSymbolActive(leftSignalSymbol, false);
        SetSymbolActive(hazardSignalSymbol, false);
        SetSymbolActive(rightSignalSymbol, false);

        currentSweepStage = 2;
        hasSweepedDashboard = true;
        dashboardSweepCoroutine = null!;
        yield break;
    }

    private void CancelDashboardSweep()
    {
        if (dashboardSweepCoroutine != null)
        {
            StopCoroutine(dashboardSweepCoroutine);
            dashboardSweepCoroutine = null!;
        }

        SetSymbolActive(leftSignalSymbol, false);
        SetSymbolActive(hazardSignalSymbol, false);
        SetSymbolActive(rightSignalSymbol, false);
        SetSymbolActive(immobiliserSymbol, false);

        currentSweepStage = -1;
        hasSweepedDashboard = false;
    }

    private void SetWarningLampsOff()
    {
        checkEngineLightActive = false;
        tractionControlLightActive = false;
        coolantLevelLightActive = false;
        oilLevelLightActive = false;
        parkingBrakeLightActive = false;
        batteryLowLightActive = false;
        dippedBeamLightActive = false;
        mainBeamLightActive = false;
    }

    private void SetCarDashboard()
    {
        // cluster
        speedometerTransform.localRotation = Quaternion.Euler(0f, 155f * speedometerFloat, 0f);
        tachometerTransform.localRotation = Quaternion.Euler(0f, 154.5f * tachometerFloat, 0f);

        if (ignitionStarted)
        {
            float speedometerRot = Mathf.Abs(wheelRPM) / 900f;
            float tachometerRot = EngineRPM / MaxEngineRPM;

            speedometerFloat = Mathf.Lerp(speedometerFloat, speedometerRot, 4f * Time.deltaTime);
            tachometerFloat = Mathf.Lerp(tachometerFloat, tachometerRot, 5f * Time.deltaTime);
        }
        else
        {
            bool tryingIgnition = engineAudio1.volume > 0.1f && keyIgnitionCoroutine != null;
            speedometerFloat = Mathf.Lerp(speedometerFloat, 0f, 6f * Time.deltaTime);
            tachometerFloat = Mathf.Lerp(tachometerFloat, tryingIgnition ? 0.075f : 0f, 4.5f * Time.deltaTime);
        }
        SetDashboardSymbols();
        SetRadioScreen();
        if (!ignitionOn)
        {
            SetWarningLampsOff();
            hasPlayedCheckEngineWarning = false;
            return;
        }
        SetWarningLamps();
    }

    // radio screen
    private void SetRadioScreen()
    {
        // allow the radio screen to stay on if the radio is still actively playing
        if (!ignitionOn && !radioOn)
        {
            radioScreen.text = null!;
            return;
        }
        if (RoundManager.Instance.currentLevel != null && 
            !RoundManager.Instance.currentLevel.planetHasTime)
        {
            radioScreen.text = DateTime.Now.ToString("h:mm tt"); // users computer time
            return;
        }
        radioScreen.text = HUDManager.Instance.clockNumber.text.Trim().Replace("\n", " "); // current in-game time
    }

    private void SetWarningLamps()
    {
        // check engine light
        if (currentSweepStage < 2)
        {
            checkEngineLightActive = true;
        }
        else
        {
            if (normalisedCarHP <= 0.5f)
            {
                if (!checkEngineLightActive)
                {
                    checkEngineLightActive = true;
                }
                if (hasPlayedIgnitionChime && !hasPlayedCheckEngineWarning && !cabinAudio.isPlaying)
                {
                    hasPlayedCheckEngineWarning = true;
                    cabinAudio.PlayOneShot(chimeSoundWarning);
                }
            }
            else if (normalisedCarHP > 0.5f && checkEngineLightActive)
            {
                checkEngineLightActive = false;
                hasPlayedCheckEngineWarning = false;
            }
        }

        // traction control light
        tractionControlLightActive = (tireSparks.isEmitting && ignitionStarted && gear == CarGearShift.Drive) ||
            (!ignitionStarted && currentSweepStage < 2);
        // coolant level light
        coolantLevelLightActive = (!ignitionStarted) ||
            (ignitionStarted && normalisedCarHP <= 0.72f);
        // oil pressure light
        oilLevelLightActive = (!ignitionStarted) ||
            (ignitionStarted && normalisedCarHP <= 0.36f);
        // parking brake light
        parkingBrakeLightActive = gear == CarGearShift.Park;
        // battery light
        batteryLowLightActive = !ignitionStarted;

        // beams
        dippedBeamLightActive = headlampsOn;
        mainBeamLightActive = headlampsOn;
    }

    private void SetCarLighting()
    {
        SetBackLights();
    }

    private void SetBackLights()
    {
        bool brakeLightsActive = brakePedalPressed && ignitionStarted;
        bool backLightsActive = headlampsOn || brakeLightsActive;
        if (backLightsOn != backLightsActive)
        {
            backLightsOn = backLightsActive;
            backLightsMesh.material = backLightsOn ? backLightOnMat : backLightOffMat;
            backLightsContainer.SetActive(backLightsOn);
        }
        if (brakeLightsOn != brakeLightsActive)
        {
            brakeLightsOn = brakeLightsActive;
            centerMountedLight.material = brakeLightsOn ? backLightOnMat : backLightOffMat;
            centerMountedLightContainer.SetActive(brakeLightsOn);
        }
    }

    private void SetCarGearstick()
    {
        float timeSinceStartup = Time.realtimeSinceStartup;
        switch (gear)
        {
            case CarGearShift.Park:
                {
                    gearStickAnimValue = Mathf.MoveTowards(gearStickAnimValue, 0f, shiftSpeed * Time.deltaTime * (timeSinceStartup - timeAtLastGearShift));
                    break;
                }
            case CarGearShift.Reverse:
                {
                    gearStickAnimValue = Mathf.MoveTowards(gearStickAnimValue, 0.5f, shiftSpeed * Time.deltaTime * (timeSinceStartup - timeAtLastGearShift));
                    break;
                }
            case CarGearShift.Drive:
                {
                    gearStickAnimValue = Mathf.MoveTowards(gearStickAnimValue, 1f, shiftSpeed * Time.deltaTime * (timeSinceStartup - timeAtLastGearShift));
                    break;
                }
        }
        gearStickAnimator.SetFloat("currentGear", Mathf.Clamp(gearStickAnimValue, 0f, 1f));
    }


    // --- MISC SYNC METHODS ---
    public void SyncCarEffectsToOtherClients()
    {
        if (syncEffectsInterval > 0.045f)
        {
            if (syncedSteeringRotation != steeringWheelAnimFloat)
            {
                syncEffectsInterval = 0f;
                syncedSteeringRotation = steeringWheelAnimFloat;
                syncedPlayerSteeringAnim = playerSteeringWheelAnimFloat;
                SyncCarEffectsRpc(steeringWheelAnimFloat, playerSteeringWheelAnimFloat);
                return;
            }
        }
        else
        {
            syncEffectsInterval += Time.deltaTime;
        }
    }

    [Rpc(SendTo.NotOwner)]
    public void SyncCarEffectsRpc(float wheelRotation, float playerSteering)
    {
        syncedSteeringRotation = wheelRotation;
        syncedPlayerSteeringAnim = playerSteering;
    }


    // --- VEHICLE AUDIO METHODS ---
    /// <summary>
    ///  Available from EnemySoundFixes, licensed under GNU General Public License.
    ///  Source: https://github.com/ButteryStancakes/EnemySoundFixes/blob/master/Patches/CruiserPatches.cs
    /// </summary>
    private new void SetVehicleAudioProperties(AudioSource audio, bool audioActive, float lowest, float highest, float lerpSpeed, bool useVolumeInsteadOfPitch = false, float onVolume = 1f)
    {
        if (audioActive && ((audio == rollingAudio || audio == skiddingAudio) && allWheelsAirborne))
            audioActive = false;

        if (!audioActive)
        {
            if (useVolumeInsteadOfPitch)
            {
                audio.volume = Mathf.Lerp(audio.volume, 0f, lerpSpeed * Time.deltaTime);
            }
            else
            {
                audio.volume = Mathf.Lerp(audio.volume, 0f, 4f * Time.deltaTime);
                audio.pitch = Mathf.Lerp(audio.pitch, lowest, 4f * Time.deltaTime);
            }
            if (audio.isPlaying)
            {
                if (audio.volume <= 0.001f)
                    audio.Stop();
            }
            return;
        }
        if (!audio.isPlaying)
        {
            audio.Play();
        }
        if (useVolumeInsteadOfPitch)
        {
            audio.volume = Mathf.Max(Mathf.Lerp(audio.volume, highest, lerpSpeed * Time.deltaTime), lowest);
            return;
        }
        audio.volume = Mathf.Lerp(audio.volume, onVolume, 20f * Time.deltaTime);
        audio.pitch = Mathf.Lerp(audio.pitch, highest, lerpSpeed * Time.deltaTime);
    }

    public void SetCarAudioEffects()
    {
        float timeSinceStartup = Time.realtimeSinceStartup;
        //float highestAudio1 = Mathf.Clamp((EngineRPM / engineIntensityPercentage), 0.65f, 1.15f);
        //float highestAudio2 = Mathf.Clamp((EngineRPM / engineIntensityPercentage), 0.7f, 1.5f);
        float engineAudioAnimCurve = engineCurve.Evaluate(EngineRPM / MaxEngineRPM);
        //float highestAudio1 = ignitionStarted ? Mathf.Clamp(engineAudioAnimCurve, 0.65f, 1.15f) * 1.35f : 1f;
        float highestAudio1 = ignitionStarted ? Mathf.Clamp(engineAudioAnimCurve, 0.65f, 1.15f) : 1f; // 0.65f, 1.15f
        float highestAudio2 = Mathf.Clamp(engineAudioAnimCurve, 0.7f, 1.5f);
        float wheelSpeed = Mathf.Abs(wheelRPM);
        float highestTyre = Mathf.Clamp(wheelSpeed / 63f, 0f, 1f); // 63f = 180 * 0.35f, 180 = engineIntensityPercentage
        carEngine2AudioActive = ignitionStarted;
        carRollingAudioActive = !allWheelsAirborne && wheelSpeed > 10f;
        if (!ignitionStarted)
        {
            highestAudio1 = 1f;
        }
        SetVehicleAudioProperties(engineAudio1, carEngine1AudioActive, 0.7f, highestAudio1, 2f, useVolumeInsteadOfPitch: false, 0.7f);
        SetVehicleAudioProperties(engineAudio2, carEngine2AudioActive, 0.7f, highestAudio2, 3f, useVolumeInsteadOfPitch: false, 0.5f);
        SetVehicleAudioProperties(rollingAudio, carRollingAudioActive, 0f, highestTyre, 5f, useVolumeInsteadOfPitch: true);
        SetVehicleAudioProperties(extremeStressAudio, underExtremeStress, 0.2f, 1f, 3f, useVolumeInsteadOfPitch: true);
        SetRadioValues();
        if (engineAudio1.volume > 0.3f && engineAudio1.isPlaying && (timeSinceStartup - timeAtLastEngineAudioPing > 2f))
        {
            timeAtLastEngineAudioPing = timeSinceStartup;
            if (EngineRPM > 2800f)
            {
                RoundManager.Instance.PlayAudibleNoise(engineAudio1.transform.position, 32f, 0.75f, 0, false, 2692);
            }
            if (EngineRPM > 1200f)
            {
                RoundManager.Instance.PlayAudibleNoise(engineAudio1.transform.position, 25f, 0.6f, 0, false, 2692);
            }
            else if (!ignitionStarted)
            {
                RoundManager.Instance.PlayAudibleNoise(engineAudio1.transform.position, 15f, 0.6f, 0, false, 2692);
            }
            else
            {
                RoundManager.Instance.PlayAudibleNoise(engineAudio1.transform.position, 11f, 0.5f, 0, false, 2692);
            }
        }

        SetVehicleAudioProperties(roofRainAudio, roofRainAudioActive, 0, 1f, 3f, useVolumeInsteadOfPitch: true);
        roofRainAudio.spatialBlend = Mathf.MoveTowards(roofRainAudio.spatialBlend, roofRainAudioActive ? 0f : 1f, 4f * Time.deltaTime);

        turbulenceAudio.volume = Mathf.Lerp(turbulenceAudio.volume, Mathf.Min(1f, turbulenceAmount), 10f * Time.deltaTime);
        turbulenceAmount = Mathf.Max(turbulenceAmount - Time.deltaTime, 0f);
        if (turbulenceAudio.volume > 0.02f)
        {
            if (!turbulenceAudio.isPlaying)
                turbulenceAudio.Play();
        }
        else if (turbulenceAudio.isPlaying)
            turbulenceAudio.Stop();

        if (honkingHorn)
        {
            hornAudio.pitch = 1f;
            if (!hornAudio.isPlaying)
                hornAudio.Play();
            if (timeSinceStartup - timeAtLastHornPing > 2f)
            {
                timeAtLastHornPing = timeSinceStartup;
                RoundManager.Instance.PlayAudibleNoise(hornAudio.transform.position, 28f, 0.85f, 0, noiseIsInsideClosedShip: false, 106217);
            }
        }
        else
        {
            hornAudio.pitch = Mathf.Max(hornAudio.pitch - Time.deltaTime * 6f, 0.01f);
            if (hornAudio.pitch < 0.02f && hornAudio.isPlaying)
                hornAudio.Stop();
        }
    }


    // --- RADIO TIME SYNC ---
    [Rpc(SendTo.NotServer, RequireOwnership = false)]
    public void SyncRadioTimeRpc(float songTime, float syncedTime, float staticTime)
    {
        float timeSinceStartup = Time.realtimeSinceStartup;
        currentSongTime = songTime;
        syncedSongTime = syncedTime;
        timeLastSyncedRadio = timeSinceStartup;
        SetRadioTime(timeSinceStartup);
        SetRadioStaticTime(staticTime, timeSinceStartup);
    }

    public void SetRadioTime(float timeSinceStartup)
    {
        if (radioAudio.clip == null)
            return;

        float setTime = (syncedSongTime + (timeSinceStartup - timeLastSyncedRadio)) % radioAudio.clip.length;
        if (Mathf.Abs(setTime - radioAudio.time) > 1f)
        {
            radioAudio.time = setTime;
        }
    }

    public void SetRadioStaticTime(float staticTime, float timeSinceStartup)
    {
        float setTime = (staticTime + (timeSinceStartup - timeLastSyncedRadio)) % radioInterference.clip.length;
        if (Mathf.Abs(setTime - radioInterference.time) > 1f)
        {
            radioInterference.time = setTime;
        }
    }


    // --- RADIO CHANNEL ---
    public new void ChangeRadioStation()
    {
        if (radioClips.Length == 0)
        {
            Plugin.LogWarning("No music found! are you using CruiserTunes to remove the original tracks?");
            return;
        }
        currentRadioClip = (currentRadioClip + 1) % radioClips.Length;
        switch ((int)Mathf.Round(radioSignalQuality))
        {
            case 0:
                radioSignalQuality = 3f;
                radioSignalDecreaseThreshold = 90f;
                break;
            case 1:
                radioSignalQuality = 2f;
                radioSignalDecreaseThreshold = 70f;
                break;
            case 2:
                radioSignalQuality = 1f;
                radioSignalDecreaseThreshold = 30f;
                break;
            case 3:
                radioSignalQuality = 1f;
                radioSignalDecreaseThreshold = 10f;
                break;
        }
        SetRadioOnLocalClient(on: true, setClip: true);
        float setTime = Mathf.Clamp(currentSongTime % radioAudio.clip.length, 0.01f, radioAudio.clip.length - 0.1f);
        timeLastSyncedRadio = Time.realtimeSinceStartup;
        radioAudio.time = setTime;
        SetRadioStationRpc(currentRadioClip, setTime, radioSignalQuality, radioSignalDecreaseThreshold);
    }

    [Rpc(SendTo.NotMe, RequireOwnership = false)]
    public void SetRadioStationRpc(int radioStation, float setTime, float signalQuality, float signalDecrease)
    {
        if (radioClips.Length == 0)
        {
            Plugin.LogWarning("No music found! are you using CruiserTunes to remove the original tracks?");
            return;
        }
        currentRadioClip = radioStation;
        radioSignalQuality = signalQuality;
        radioSignalDecreaseThreshold = signalDecrease;
        SetRadioOnLocalClient(on: true, setClip: true);
        timeLastSyncedRadio = Time.realtimeSinceStartup;
        radioAudio.time = setTime;
    }


    // --- RADIO TOGGLE --- 
    public new void SwitchRadio()
    {
        if (radioClips.Length == 0)
        {
            Plugin.LogWarning("No music found! are you using CruiserTunes to remove the original tracks?");
            return;
        }
        SetRadioOnLocalClient(on: !radioOn, setClip: false);
        float setTime = Mathf.Clamp(currentSongTime % radioAudio.clip.length, 0.01f, radioAudio.clip.length - 0.1f);
        timeLastSyncedRadio = Time.realtimeSinceStartup;
        radioAudio.time = setTime;
        SetRadioRpc(radioOn, currentRadioClip, setTime, radioSignalQuality, radioSignalDecreaseThreshold);
    }

    [Rpc(SendTo.NotMe, RequireOwnership = false)]
    public void SetRadioRpc(bool setRadioOn, int radioStation, float setTime, float signalQuality, float signalDecrease)
    {
        if (radioClips.Length == 0)
        {
            Plugin.LogWarning("No music found! are you using CruiserTunes to remove the original tracks?");
            return;
        }
        currentRadioClip = radioStation;
        radioSignalQuality = signalQuality;
        radioSignalDecreaseThreshold = signalDecrease;
        SetRadioOnLocalClient(on: setRadioOn, setClip: false);
        timeLastSyncedRadio = Time.realtimeSinceStartup;
        radioAudio.time = setTime;
    }


    // --- RADIO VALUES ---
    public new void SetRadioValues()
    {
        float timeSinceStartup = Time.realtimeSinceStartup;
        if (NetworkManager.IsServer && radioTurnedOnBefore)
        {
            currentSongTime += Time.deltaTime;
            if (timeSinceStartup - timeLastSyncedRadio > 1f)
            {
                timeLastSyncedRadio = timeSinceStartup;
                syncedSongTime = radioAudio.time;
                SyncRadioTimeRpc(currentSongTime, syncedSongTime, radioInterference.time);
            }
        }
        if (!radioOn || radioAudio.clip == null)
        {
            if (radioAudio.isPlaying) radioAudio.Stop();
            if (radioInterference.isPlaying) radioInterference.Stop();
            return;
        }
        if (!radioTurnedOnBefore) radioTurnedOnBefore = true;
        if (radioAudio.isPlaying && timeSinceStartup > radioPingTimestamp)
        {
            radioPingTimestamp = (timeSinceStartup + 1f);
            RoundManager.Instance.PlayAudibleNoise(radioAudio.transform.position, 16f, Mathf.Min((radioAudio.volume + radioInterference.volume) * 0.5f, 0.9f), 0, false, 106217);
        }
        if (IsOwner)
        {
            float random = UnityEngine.Random.Range(0, 100);
            float radioSignal = (3f - radioSignalQuality - 1.5f) * radioSignalTurbulence;
            radioSignalDecreaseThreshold = Mathf.Clamp(radioSignalDecreaseThreshold + Time.deltaTime * radioSignal, 0f, 100f);
            if (random > radioSignalDecreaseThreshold)
            {
                radioSignalQuality = Mathf.Clamp(radioSignalQuality - Time.deltaTime, 0f, 3f);
            }
            else
            {
                radioSignalQuality = Mathf.Clamp(radioSignalQuality + Time.deltaTime, 0f, 3f);
            }
            if (timeSinceStartup - changeRadioSignalTime > 0.3f)
            {
                changeRadioSignalTime = timeSinceStartup;
                if (radioSignalQuality < 1.2f && UnityEngine.Random.Range(0, 100) < 6)
                {
                    radioSignalQuality = Mathf.Min(radioSignalQuality + 1.5f, 3f);
                    radioSignalDecreaseThreshold = Mathf.Min(radioSignalDecreaseThreshold + 30f, 100f);
                }
                SetRadioSignalQualityRpc(radioSignalQuality, radioSignalDecreaseThreshold);
            }
        }
        switch ((int)Mathf.Round(radioSignalQuality))
        {
            case 3:
                radioAudio.volume = Mathf.Lerp(radioAudio.volume, 1f, 2f * Time.deltaTime);
                radioInterference.volume = Mathf.Lerp(radioInterference.volume, 0f, 2f * Time.deltaTime);
                break;
            case 2:
                radioAudio.volume = Mathf.Lerp(radioAudio.volume, 0.85f, 2f * Time.deltaTime);
                radioInterference.volume = Mathf.Lerp(radioInterference.volume, 0.4f, 2f * Time.deltaTime);
                break;
            case 1:
                radioAudio.volume = Mathf.Lerp(radioAudio.volume, 0.6f, 2f * Time.deltaTime);
                radioInterference.volume = Mathf.Lerp(radioInterference.volume, 0.8f, 2f * Time.deltaTime);
                break;
            case 0:
                radioAudio.volume = Mathf.Lerp(radioAudio.volume, 0.4f, 2f * Time.deltaTime);
                radioInterference.volume = Mathf.Lerp(radioInterference.volume, 1f, 2f * Time.deltaTime);
                break;
        }
    }

    [Rpc(SendTo.NotOwner)]
    public void SetRadioSignalQualityRpc(float signalQuality, float signalDecrease)
    {
        radioSignalQuality = signalQuality;
        radioSignalDecreaseThreshold = signalDecrease;
    }

    public new void SetRadioOnLocalClient(bool on, bool setClip = true)
    {
        Plugin.LogDebug($"Radio called with setRadioOn? {on}, setClip? {setClip}");
        radioOn = on;
        if (on)
        {
            if (setClip || radioAudio.clip == null)
            {
                if (radioAudio.clip == null) Plugin.LogDebug("Setting station, was null!");
                radioAudio.clip = radioClips[currentRadioClip];
                Plugin.LogDebug($"Set radio clip to {currentRadioClip}, station? {radioAudio.clip.name}");
            }
            radioAudio.Play();
            radioInterference.Play();
            return;
        }
        radioAudio.Stop();
        radioInterference.Stop();
        Plugin.LogDebug("Stop radio playback!");
    }


    // --- COLLISION ---
    public new bool CarReactToObstacle(Vector3 vel, Vector3 position, Vector3 impulse, CarObstacleType type, float obstacleSize = 1f, EnemyAI enemyScript = null!, bool dealDamage = true)
    {
        switch (type)
        {
            case CarObstacleType.Object:
                if (carHP < 10)
                {
                    mainRigidbody.AddForceAtPosition(Vector3.up * torqueForce + vel, position, ForceMode.Impulse);
                }
                else
                {
                    mainRigidbody.AddForceAtPosition((Vector3.up * torqueForce + vel) * 0.5f, position, ForceMode.Impulse);
                }
                CarBump(averageVelocity * 0.7f);
                if (dealDamage)
                {
                    DealPermanentDamage(1, position);
                }
                return true;
            case CarObstacleType.Player:
                PlayCollisionAudio(position, 5, Mathf.Clamp(vel.magnitude / 7f, 0.65f, 1f));
                if (vel.magnitude < 4.25f)
                {
                    mainRigidbody.velocity = Vector3.Normalize(-impulse * 100000000f) * 9f;
                    DealPermanentDamage(1);
                    return true;
                }
                mainRigidbody.AddForceAtPosition(Vector3.up * torqueForce, position, ForceMode.VelocityChange);
                return false;
            case CarObstacleType.Enemy:
                {
                    float enemyHitSpeed;
                    if (obstacleSize <= 1f)
                    {
                        enemyHitSpeed = 2f;
                        _ = carReactToPlayerHitMultiplier;
                    }
                    else if (obstacleSize <= 2f)
                    {
                        enemyHitSpeed = 9f;
                        _ = carReactToPlayerHitMultiplier;
                    }
                    else
                    {
                        enemyHitSpeed = 15f;
                        _ = carReactToPlayerHitMultiplier;
                    }
                    vel = Vector3.Scale(vel, new Vector3(1f, 0f, 1f));
                    mainRigidbody.AddForceAtPosition(Vector3.up * torqueForce, position, ForceMode.VelocityChange);
                    bool result = false;
                    if (vel.magnitude < enemyHitSpeed)
                    {
                        if (obstacleSize <= 1f)
                        {
                            mainRigidbody.AddForce(Vector3.Normalize(-impulse * 1E+09f) * 4f, ForceMode.Impulse);
                            if (vel.magnitude > 1f)
                            {
                                enemyScript.KillEnemyOnOwnerClient();
                            }
                        }
                        else
                        {
                            CarBump(averageVelocity);
                            mainRigidbody.velocity = Vector3.Normalize(-impulse * 100000000f) * 9f;
                            PlayerControllerB playerControllerB = currentDriver != null ? currentDriver : currentPassenger;
                            if (vel.magnitude > 2f && dealDamage)
                            {
                                enemyScript.HitEnemyOnLocalClient(2, Vector3.zero, playerControllerB, playHitSFX: true, 332);
                            }
                            result = true;
                            if (obstacleSize > 2f) DealPermanentDamage(3, position);
                        }
                    }
                    else
                    {
                        mainRigidbody.AddForce(Vector3.Normalize(-impulse * 1E+09f) * (carReactToPlayerHitMultiplier - 220f), ForceMode.Impulse);
                        if (dealDamage)
                        {
                            DealPermanentDamage(5, position);
                        }
                        PlayerControllerB playerWhoHit = currentDriver != null ? currentDriver : currentPassenger;
                        enemyScript.HitEnemyOnLocalClient(22, Vector3.zero, playerWhoHit, playHitSFX: true, 332);
                    }
                    PlayCollisionAudio(position, 5, 1f);
                    return result;
                }
            default:
                return false;
        }
    }

    public new void OnCollisionEnter(Collision collision)
    {
        if (!IsOwner)
            return;

        if (magnetedToShip || !hasBeenSpawned)
            return;

        if (collision.collider.gameObject.layer != 8)
            return;

        float timeSinceStartup = Time.realtimeSinceStartup;
        if (timeSinceStartup - timeSinceLastCollision < 0.1f)
            return;

        float collisionImpulse = 0f;
        int contactCount = collision.GetContacts(contacts);
        Vector3 setPosition = Vector3.zero;

        for (int i = 0; i < contactCount; i++)
        {
            if (contacts[i].impulse.magnitude > collisionImpulse)
            {
                collisionImpulse = contacts[i].impulse.magnitude;
            }
            setPosition += contacts[i].point;
        }

        setPosition /= (float)contactCount;
        collisionImpulse /= Time.fixedDeltaTime;

        if (collisionImpulse < minimalBumpForce || averageVelocity.magnitude < 4f)
        {
            if (contactCount > 3 && averageVelocity.magnitude > 2.5f)
            {
                SetInternalStress(0.25f);
                lastStressType = "; Scraping";
            }
            timeSinceLastCollision = timeSinceStartup;
            return;
        }

        float collisionVolume = 0.5f;
        int audioType = -1;

        if (averageVelocity.magnitude > 27f)
        {
            if (carHP < 3)
            {
                timeAtLastDamage = Time.realtimeSinceStartup;
                syncedCarHP = carHP;
                DestroyCarRpc(carHP);
                DestroyCar();
                return;
            }

            audioType = 2;
            collisionVolume = Mathf.Clamp((collisionImpulse - maximumBumpForce) / 20000f, 0.8f, 1f);
            collisionVolume = Mathf.Clamp(collisionVolume + UnityEngine.Random.Range(-0.15f, 0.25f), 0.7f, 1f);
            PlayCollisionAudio(setPosition, audioType, collisionVolume);

            DealPermanentDamage(6);
            CarCollisionRpc(Vector3.ClampMagnitude(-collision.relativeVelocity, 60f));
            BreakWindshield();
            timeSinceLastCollision = timeSinceStartup + 0.25f;
            return;
        }

        if (collisionImpulse >= minimalBumpForce && collisionImpulse < mediumBumpForce &&
            averageVelocity.magnitude > 3f)
        {
            audioType = 0;
            collisionVolume = Mathf.Clamp((collisionImpulse - minimalBumpForce) / (mediumBumpForce - minimalBumpForce), 0.25f, 1f);
            collisionVolume = Mathf.Clamp(collisionVolume + UnityEngine.Random.Range(-0.15f, 0.25f), 0.25f, 1f);
        }
        else if (collisionImpulse >= mediumBumpForce && collisionImpulse < maximumBumpForce &&
            averageVelocity.magnitude > 6f)
        {
            audioType = 1;
            collisionVolume = Mathf.Clamp((collisionImpulse - mediumBumpForce) / (maximumBumpForce - mediumBumpForce), 0.67f, 1f);
            collisionVolume = Mathf.Clamp(collisionVolume + UnityEngine.Random.Range(-0.15f, 0.25f), 0.5f, 1f);
        }
        else if (collisionImpulse >= maximumBumpForce &&
            averageVelocity.magnitude > 12f)
        {
            audioType = 2;
            collisionVolume = Mathf.Clamp((collisionImpulse - maximumBumpForce) / 20000f, 0.8f, 1f);
            collisionVolume = Mathf.Clamp(collisionVolume + UnityEngine.Random.Range(-0.15f, 0.25f), 0.7f, 1f);
            DealPermanentDamage(1);
            timeSinceLastCollision = timeSinceStartup + 0.2f;
        }

        if (audioType != -1)
        {
            PlayCollisionAudio(setPosition, audioType, collisionVolume);
            if (collisionImpulse > maximumBumpForce + 15000f && averageVelocity.magnitude > 22f)
            {
                CarCollisionRpc(Vector3.ClampMagnitude(-collision.relativeVelocity, 60f));
                DamagePlayerInVehicle(Vector3.ClampMagnitude(-collision.relativeVelocity, 60f));
                BreakWindshield();
                DealPermanentDamage(2);
            }
            else
            {
                CarBump(Vector3.ClampMagnitude(-collision.relativeVelocity, 40f));
            }
            timeSinceLastCollision = timeSinceStartup + 0.33f;
        }
    }

    public bool IsPlayerSeatedInCar()
    {
        return localPlayerInControl || 
               localPlayerInPassengerSeat || 
               localPlayerInBackLeftPassengerSeat || 
               localPlayerInMiddlePassengerSeat || 
               localPlayerInBackRightPassengerSeat;
    }

    public void CarBump(Vector3 vel)
    {
        CarBumpRpc(vel);
        CarBumpLocalClient(vel);
    }

    public void CarBumpLocalClient(Vector3 vel)
    {
        PlayerControllerB playerController = GameNetworkManager.Instance.localPlayerController;
        if (IsPlayerSeatedInCar() && vel.magnitude > 40f)
        {
            playerController.externalForceAutoFade += vel;
            return;
        }
        if (!IsPlayerAttached(playerController))
        {
            return;
        }
        vel = Vector3.ClampMagnitude(vel, 30f);
        playerController.externalForceAutoFade += vel;
    }

    [Rpc(SendTo.NotOwner)]
    public void CarBumpRpc(Vector3 vel)
    {
        CarBumpLocalClient(vel);
    }

    [Rpc(SendTo.NotOwner)]
    public void CarCollisionRpc(Vector3 vel)
    {
        DamagePlayerInVehicle(vel);
        BreakWindshield();
    }

    public void DamagePlayerInVehicle(Vector3 vel)
    {
        PlayerControllerB playerController = GameNetworkManager.Instance.localPlayerController;
        if (IsPlayerSeatedInCar())
        {
            if (vel.magnitude > 30f)
            {
                if (playerController.health < 48)
                {
                    playerController.KillPlayer(vel, true, CauseOfDeath.Inertia, 0, base.transform.up * 0.77f, false);
                    return;
                }
                playerController.DamagePlayer(40, true, true, CauseOfDeath.Inertia, 0, false, vel);
                return;
            }
            if (vel.magnitude <= 24f)
            {
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                playerController.DamagePlayer(30, true, true, CauseOfDeath.Inertia, 0, false, vel);
                return;
            }
            HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
            if (playerController.health < 20)
            {
                playerController.KillPlayer(vel, true, CauseOfDeath.Inertia, 0, base.transform.up * 0.77f, false);
                return;
            }
            playerController.DamagePlayer(20, true, true, CauseOfDeath.Inertia, 0, false, vel);
            return;
        }
        if (!IsPlayerAttached(playerController))
        {
            return;
        }
        if (playerController.health <= 40)
        {
            playerController.KillPlayer(vel, spawnBody: true, CauseOfDeath.Inertia, 0, base.transform.up * 0.77f, false);
            return;
        }
        playerController.DamagePlayer(30, hasDamageSFX: true, callRPC: true, CauseOfDeath.Inertia, 0, fallDamage: false, vel);
        playerController.externalForceAutoFade += vel;
    }

    private new void BreakWindshield()
    {
        if (windshieldBroken)
        {
            return;
        }
        glassParticle.Play();
        windshieldBroken = true;
        windshieldMesh.enabled = false;
        windshieldAudio.PlayOneShot(windshieldBreak);
    }

    private void RegenerateWindshield()
    {
        if (!windshieldBroken)
        {
            return;
        }
        windshieldBroken = false;
        windshieldMesh.enabled = true;
    }

    public new void PlayCollisionAudio(Vector3 setPosition, int audioType, float setVolume)
    {
        float timeSinceStartup = Time.realtimeSinceStartup;
        bool audioTime;
        if (timeSinceStartup - audio1Time > timeSinceStartup - audio2Time)
        {
            audioTime = timeSinceStartup - audio1Time >= collisionAudio1.clip.length * 0.8f;
            if (audio1Type <= audioType || audioTime)
            {
                audio1Time = timeSinceStartup;
                audio1Type = audioType;
                collisionAudio1.transform.position = setPosition;
                CarCollisionSFXRpc(collisionAudio1.transform.localPosition, 0, audioType, audioTime, setVolume);
                PlayRandomClipAndPropertiesFromAudio(collisionAudio1, setVolume, audioTime, audioType);
            }
        }
        else
        {
            audioTime = timeSinceStartup - audio2Time >= collisionAudio2.clip.length * 0.8f;
            if (audio2Type <= audioType || audioTime)
            {
                audio2Time = timeSinceStartup;
                audio2Type = audioType;
                collisionAudio2.transform.position = setPosition;
                CarCollisionSFXRpc(collisionAudio2.transform.localPosition, 1, audioType, audioTime, setVolume);
                PlayRandomClipAndPropertiesFromAudio(collisionAudio2, setVolume, audioTime, audioType);
            }
        }
    }

    [Rpc(SendTo.NotOwner)]
    public void CarCollisionSFXRpc(Vector3 audioPosition, int audio, int audioType, bool audioTime, float vol)
    {
        float timeSinceStartup = Time.realtimeSinceStartup;
        AudioSource audioSource;
        //bool audioTime;
        if (audio != 0)
        {
            audio2Time = timeSinceStartup;
            audio2Type = audioType;
            audioSource = collisionAudio2;
            //audioTime = timeSinceStartup - audio2Time >= audioSource.clip.length * 0.8f;
        }
        else
        {
            audio1Time = timeSinceStartup;
            audio1Type = audioType;
            audioSource = collisionAudio1;
            //audioTime = timeSinceStartup - audio1Time >= audioSource.clip.length * 0.8f;
        }
        audioSource.transform.localPosition = audioPosition;
        PlayRandomClipAndPropertiesFromAudio(audioSource, vol, audioTime, audioType);
    }

    private new void PlayRandomClipAndPropertiesFromAudio(AudioSource audio, float setVolume, bool audioFinished, int audioType)
    {
        if (!audioFinished)
        {
            audio.Stop();
        }
        AudioClip[] selectedClips;
        switch (audioType)
        {
            case 0:
                selectedClips = minCollisions;
                turbulenceAmount = Mathf.Min(turbulenceAmount + 0.4f, 2f);
                break;
            case 1:
                selectedClips = medCollisions;
                turbulenceAmount = Mathf.Min(turbulenceAmount + 0.75f, 2f);
                break;
            case 2:
                selectedClips = maxCollisions;
                turbulenceAmount = Mathf.Min(turbulenceAmount + 1.4f, 2f);
                break;
            default:
                selectedClips = obstacleCollisions;
                turbulenceAmount = Mathf.Min(turbulenceAmount + 0.75f, 2f);
                break;
        }
        AudioClip chosenClip = selectedClips[UnityEngine.Random.Range(0, selectedClips.Length)];
        if (chosenClip == audio.clip && UnityEngine.Random.Range(0, 10) <= 5)
        {
            chosenClip = selectedClips[UnityEngine.Random.Range(0, selectedClips.Length)];
        }
        if (audioFinished)
        {
            audio.pitch = UnityEngine.Random.Range(0.8f, 1.2f);
        }
        audio.clip = chosenClip;
        audio.PlayOneShot(chosenClip, setVolume);
        if (audioType >= 2)
        {
            RoundManager.Instance.PlayAudibleNoise(engineAudio1.transform.position, 18f + setVolume * 7f, 0.6f, 0, noiseIsInsideClosedShip: false, 106217);
        }
        else if (audioType >= 1)
        {
            RoundManager.Instance.PlayAudibleNoise(engineAudio1.transform.position, 12f + setVolume * 7f, 0.6f, 0, noiseIsInsideClosedShip: false, 106217);
        }
        if (audioType == -1)
        {
            selectedClips = minCollisions;
            chosenClip = selectedClips[UnityEngine.Random.Range(0, selectedClips.Length)];
            audio.PlayOneShot(chosenClip);
        }
    }

    public new void SetInternalStress(float carStressIncrease = 0f)
    {
        if (!IsOwner || carDestroyed)
        {
            return;
        }

        if (carStressIncrease <= 0f) carStressChange = Mathf.Clamp(carStressChange - Time.fixedDeltaTime, -0.25f, 0.5f);
        else carStressChange = Mathf.Clamp(carStressChange + Time.fixedDeltaTime * carStressIncrease, 0f, 10f);

        underExtremeStress = (carStressIncrease >= 1f);
        carStress = Mathf.Clamp(carStress + carStressChange, 0f, 100f);

        if (carStress > 7f)
        {
            carStress = 0f;
            DealPermanentDamage(2, default(Vector3));
            lastDamageType = "; Stress";
        }
    }

    public new void DealPermanentDamage(int damageAmount, Vector3 damagePosition = default(Vector3))
    {
        if (!IsOwner || carDestroyed)
        {
            return;
        }
        timeAtLastDamage = Time.realtimeSinceStartup;
        carHP -= damageAmount;
        syncedCarHP = carHP;
        if (carHP <= 0)
        {
            DestroyCarRpc(carHP);
            DestroyCar();
            return;
        }
        DealDamageRpc(carHP);
    }

    [Rpc(SendTo.NotOwner)]
    public void DealDamageRpc(int carHealth)
    {
        timeAtLastDamage = Time.realtimeSinceStartup;
        carHP = carHealth;
        syncedCarHP = carHP;
    }


    // --- DESTRUCTION ---
    [Rpc(SendTo.NotOwner)]
    public void DestroyCarRpc(int setHealth)
    {
        timeAtLastDamage = Time.realtimeSinceStartup;
        carHP = setHealth;
        syncedCarHP = carHP;
        DestroyCar();
    }

    public new void DestroyCar()
    {
        if (carDestroyed)
            return;

        carDestroyed = true;
        DeMagnetCar();
        RemoveRainCollision();
        StopAudiosPlayback();
        StopParticleVFX();
        BreakWindshield();

        RoundManager.Instance.PlayAudibleNoise(engineAudio1.transform.position, 20f, 0.8f, 0, noiseIsInsideClosedShip: false, 106217);

        DisableWheelCollider(FrontLeftWheel, leftWheelMesh);
        DisableWheelCollider(FrontRightWheel, rightWheelMesh);
        DisableWheelCollider(BackLeftWheel, backLeftWheelMesh);
        DisableWheelCollider(BackRightWheel, backRightWheelMesh);

        DisableObjectsOnDestroy();
        destroyedTruckMesh.SetActive(true);

        SetExplosionForce(forceMultiplier: 1560f, explosionPos: hoodFireAudio.transform.position);

        DisableIgnition();
        DisableDrivetrain();

        ResetControl();
        KillOccupants();

        DisableInteractions();

        ResetOccupants();

        Landmine.SpawnExplosion(transform.position + transform.forward + Vector3.up * 1.5f, spawnExplosionEffect: true, 10f, 13f, 40, 400f, truckDestroyedExplosion, goThroughCar: true);
    }

    private void DeMagnetCar()
    {
        if (!magnetedToShip || StartOfRound.Instance.attachedVehicle != this)
            return;

        magnetedToShip = false;
        StartOfRound.Instance.attachedVehicle = null;
        StartOfRound.Instance.isObjectAttachedToMagnet = false;
        CollectItemsInTruck();
    }

    private void StopAudiosPlayback()
    {
        carEngine1AudioActive = false;
        engineAudio1.Stop();
        carEngine2AudioActive = false;
        engineAudio2.Stop();
        turbulenceAmount = 0f;
        turbulenceAudio.Stop();
        pushAudio.Stop();
        miscAudio.Stop();
        steeringWheelAudio.Stop();
        gearStickAudio.Stop();
        rollingAudio.Stop();
        carRollingAudioActive = false;
        radioOn = false;
        radioTurnedOnBefore = false;
        radioAudio.Stop();
        radioInterference.Stop();
        underExtremeStress = false;
        extremeStressAudio.Stop();
        carKeyAudio.Stop();
        honkingHorn = false;
        hornAudio.Stop();
        skiddingAudio.Stop();
        cabinAudio.Stop();
        roofRainAudioActive = false;
        roofRainAudio.Stop();
        setAudiosMuted = false;
        SetVehicleAudioMuted(false);
    }

    private void StopParticleVFX()
    {
        tireSparks.Stop();
    }

    private void DisableWheelCollider(WheelCollider wheelCollider, MeshRenderer wheelMesh)
    {
        if (wheelCollider == null || !wheelCollider.enabled)
            return;

        wheelCollider.motorTorque = 0f;
        wheelCollider.brakeTorque = 0f;
        wheelCollider.enabled = false;
        wheelMesh.enabled = false;
    }

    private void DisableObjectsOnDestroy()
    {
        for (int obj = 0; obj < disableOnDestroy.Length; obj++)
        {
            if (!disableOnDestroy[obj].activeSelf)
                continue;
            disableOnDestroy[obj].SetActive(false);
        }
        mainBodyContainer.SetActive(false);
        sunroofContainer.SetActive(false);
        hoodDoorContainer.SetActive(false);
        frontLeftDoorContainer.SetActive(false);
        frontRightDoorContainer.SetActive(false);
        backLeftDoorContainer.SetActive(false);
        backRightDoorContainer.SetActive(false);
        backDoorContainer.SetActive(false);

        frontCabinLightContainer.SetActive(false);
        headlightsContainer.SetActive(false);
        centerMountedLightContainer.SetActive(false);
        backLightsContainer.SetActive(false);
    }

    private void SetExplosionForce(float forceMultiplier, Vector3 explosionPos)
    {
        mainRigidbody.ResetCenterOfMass();
        mainRigidbody.AddForceAtPosition(Vector3.up * forceMultiplier, explosionPos - Vector3.up, ForceMode.Impulse);
    }

    private void DisableIgnition()
    {
        CancelIgnitionCoroutine();
        ignitionStarted = false;
        ignitionOn = false;
        radioScreen.text = null;
        if (carExhaustParticle.isEmitting) carExhaustParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        keyIsInIgnition = false;
        keyIsInDriverHand = false;
    }

    private void DisableDrivetrain()
    {
        EngineRPM = 0f;
        frontWheelRPM = 0f;
        frontWheelsRPM = 0f;
        backWheelRPM = 0f;
        backWheelsRPM = 0f;
        wheelRPM = 0f;
    }

    private void ResetControl()
    {
        steeringAnimValue = 0f;
        steeringWheelAnimValue = 0f;
        drivePedalPressed = false;
        brakePedalPressed = false;
        moveInputVector = Vector2.zero;
    }

    private void KillOccupants()
    {
        if (!IsPlayerSeatedInCar())
            return;
        GameNetworkManager.Instance.localPlayerController.KillPlayer(Vector3.up * 27f + 20f * UnityEngine.Random.insideUnitSphere, spawnBody: true, CauseOfDeath.Blast, 6, Vector3.up * 1.5f, false);
    }

    private void DisableInteractions()
    {
        InteractTrigger[] interactTriggers = this.gameObject.GetComponentsInChildren<InteractTrigger>();
        for (int i = 0; i < interactTriggers.Length; i++)
        {
            if (interactTriggers[i].isLadder)
                continue;
            interactTriggers[i].interactable = false;
            interactTriggers[i].CancelAnimationExternally();
        }
    }

    private void ResetOccupants()
    {
        currentDriver = null!;
        currentPassenger = null!;
        currentBackLeftPassenger = null!;
        currentBackMiddlePassenger = null!;
        currentBackRightPassenger = null!;
    }


    // --- REMOVAL MISC ---
    public void RemoveRainCollision()
    {
        var particleTriggers = new[]
        {
            ScandalLib.Utils.References.rainParticles,
            ScandalLib.Utils.References.rainHitParticles,
            ScandalLib.Utils.References.stormyRainParticles,
            ScandalLib.Utils.References.stormyRainHitParticles,
            ScandalLib.Utils.References.wesleyHurricaneRainParticles,
            ScandalLib.Utils.References.wesleyHurricaneRainHitParticles,
            ScandalLib.Utils.References.wesleyHurricaneSandParticles,
            ScandalLib.Utils.References.wesleyForsakenRainParticles,
            ScandalLib.Utils.References.wesleyForsakenRainHitParticles,
            ScandalLib.Utils.References.kenjiAcidRainParticles,
            ScandalLib.Utils.References.kenjiAcidRainHitParticles,
            ScandalLib.Utils.References.kenjiAcidStormyRainParticles,
            ScandalLib.Utils.References.kenjiAcidStormyRainHitParticles
        };

        foreach (var particle in particleTriggers)
        {
            if (particle == null)
            {
                Plugin.LogDebug("Weather particle or Trigger is null.");
                continue;
            }

            var trigger = particle.trigger;
            for (int j = trigger.colliderCount - 1; j >= 0; j--)
            {
                var collider = trigger.GetCollider(j);
                if (collider != null && weatherEffectBlockers.Contains(collider))
                {
                    trigger.RemoveCollider(j);
                }
            }
        }
    }


    // --- PHYSICS UPDATE --- 
    public new void FixedUpdate()
    {
        SetVehicleToDropship();
        ApplyCorrectionForce();
        SetVehicleToFixedPosition();
        TryAttachToShipMagnet();

        MovePhysicsBodies();
        CalculateVehicleVelocity();
        SyncCarPhysicsToOtherClients();

        if (carDestroyed)
        {
            return;
        }

        ApplySteering();
        ApplyWheelForces();

        SetVFXWheelSpeed();

        MatchWheelMeshToCollider(leftWheelMesh, FrontLeftWheel, frontWheelsRPM, steeringAngle);
        MatchWheelMeshToCollider(rightWheelMesh, FrontRightWheel, frontWheelsRPM, steeringAngle);
        MatchWheelMeshToCollider(backLeftWheelMesh, BackLeftWheel, backWheelsRPM);
        MatchWheelMeshToCollider(backRightWheelMesh, BackRightWheel, backWheelsRPM);

        allWheelsAirborne = !FrontLeftWheel.isGrounded &&
                            !FrontRightWheel.isGrounded &&
                            !BackLeftWheel.isGrounded &&
                            !BackRightWheel.isGrounded;

        allWheelsGrounded = FrontLeftWheel.isGrounded &&
                            FrontRightWheel.isGrounded &&
                            BackLeftWheel.isGrounded &&
                            BackRightWheel.isGrounded;

        backWheelsGrounded = BackLeftWheel.isGrounded &&
                             BackRightWheel.isGrounded;

        if (!IsOwner)
        {
            SetCarPhysicsValuesOnClient();
            SetTorqueForces();
            CalculateWheelSlip();
            return;
        }

        UpdateCarStress();
        UpdateTransmission();
        UpdateEngineRPMFromWheels();
        SetTorqueForces(useSynced: false);
        UpdateInclineCompensation();

        SyncDrivetrain();
        SyncWheelTorque();

        if (mainRigidbody.IsSleeping() || magnetedToShip || allWheelsAirborne)
        {
            CalculateWheelSlip();
            return;
        }

        ApplyAntiSlipForce();
        CalculateWheelSlip(calculatePhysics: true);
    }

    private void SetCarPhysicsValuesOnClient()
    {
        float targetRpm = ignitionStarted ? syncedEngineRPM : 0f;
        EngineRPM = Mathf.Lerp(EngineRPM, targetRpm, 3f * Time.fixedDeltaTime);

        enginePower = 0f;

        inclineBoost = 1f;
        currentGear = 1;

        frontWheelRPM = syncedFrontWheelRPM;
        backWheelRPM = syncedBackWheelRPM;
        wheelRPM = syncedWheelRPM;

        forwardWheelSpeed = 8000f;
        reverseWheelSpeed = -8000f;
    }

    private void SetVehicleToDropship()
    {
        if (hasBeenSpawned ||
            IsInShipPhaseAndNotLoadingLevel() ||
            loadedVehicleFromSave ||
            hasDeliveredVehicle)
            return;

        if (itemShip == null && References.itemShip != null)
            itemShip = References.itemShip;
        
        if (itemShip == null)
        {
            inDropshipAnimation = false;
            SetVehicleKinematic(setKinematic: true);
            mainRigidbody.MovePosition(StartOfRound.Instance.notSpawnedPosition.position + Vector3.forward * 30f);
            syncedPosition = mainRigidbody.position;
            syncedRotation = mainRigidbody.rotation;
            return;
        }
        if (itemShip.untetheredVehicle)
        {
            inDropshipAnimation = false;
            mainRigidbody.MovePosition(itemShip.deliverVehiclePoint.position);
            mainRigidbody.MoveRotation(itemShip.deliverVehiclePoint.rotation);
            syncedPosition = mainRigidbody.position;
            syncedRotation = mainRigidbody.rotation;
            hasBeenSpawned = true;
            hasDeliveredVehicle = true;
        }
        else if (itemShip.deliveringVehicle)
        {
            inDropshipAnimation = true;
            SetVehicleKinematic(setKinematic: true);
            mainRigidbody.MovePosition(itemShip.deliverVehiclePoint.position);
            mainRigidbody.MoveRotation(itemShip.deliverVehiclePoint.rotation);
            syncedPosition = mainRigidbody.position;
            syncedRotation = mainRigidbody.rotation;
        }
    }

    private void SetVehicleKinematic(bool setKinematic)
    {
        if (mainRigidbody.isKinematic == setKinematic)
            return;

        mainRigidbody.isKinematic = setKinematic;
        Plugin.LogDebug($"Set 'mainRigidbody' kinematic to: {setKinematic}");
    }

    private void SetVehicleToFixedPosition()
    {
        // magnet/client sync
        if (magnetedToShip)
        {
            SetVehicleKinematic(setKinematic: true);
            syncedPosition = mainRigidbody.position;
            syncedRotation = mainRigidbody.rotation;
            mainRigidbody.MovePosition(Vector3.Lerp(magnetStartPosition, StartOfRound.Instance.elevatorTransform.position + magnetTargetPosition, magnetPositionCurve.Evaluate(magnetTime)));
            mainRigidbody.MoveRotation(Quaternion.Lerp(magnetStartRotation, magnetTargetRotation, magnetRotationCurve.Evaluate(magnetRotationTime)));
            averageVelocityAtMagnetStart = Vector3.Lerp(averageVelocityAtMagnetStart, Vector3.ClampMagnitude(averageVelocityAtMagnetStart, 4f), 4f * Time.fixedDeltaTime);
            if (!finishedMagneting) magnetStartPosition += Vector3.ClampMagnitude(averageVelocityAtMagnetStart, 5f) * Time.fixedDeltaTime;
            return;
        }

        if (IsOwner || inDropshipAnimation)
            return;

        SetVehicleKinematic(setKinematic: true);
        Vector3 syncVel = syncedPosition + (averageVelocity * Time.fixedDeltaTime);
        Vector3 position = Vector3.Lerp(mainRigidbody.position, syncVel, Time.fixedDeltaTime * syncSpeedMultiplier);
        mainRigidbody.MovePosition(position);
        mainRigidbody.MoveRotation(Quaternion.Lerp(mainRigidbody.rotation, syncedRotation, syncRotationSpeed));
        truckVelocityLastFrame = mainRigidbody.velocity;
    }


    // --- AUTOPILOT MAGNET START ---
    private void TryAttachToShipMagnet()
    {
        if (magnetedToShip)
            return;

        if (!IsOwner || carDestroyed ||
            StartOfRound.Instance.isObjectAttachedToMagnet ||
            StartOfRound.Instance.attachedVehicle != null ||
            !StartOfRound.Instance.magnetOn ||
            Vector3.Distance(transform.position, StartOfRound.Instance.magnetPoint.position) >= 10f)
            return;

        if (!Physics.Linecast(transform.position, StartOfRound.Instance.magnetPoint.position, 256, QueryTriggerInteraction.Ignore))
        {
            StartMagneting();
            return;
        }
    }

    public new void StartMagneting()
    {
        if (!IsOwner)
            return;

        SetVehicleKinematic(setKinematic: true);
        magnetedToShip = true;
        magnetTime = 0f;
        magnetRotationTime = 0f;
        StartOfRound.Instance.isObjectAttachedToMagnet = true;
        StartOfRound.Instance.attachedVehicle = this;
        averageVelocityAtMagnetStart = averageVelocity;
        RoundManager.Instance.tempTransform.eulerAngles = new Vector3(0f, mainRigidbody.rotation.eulerAngles.y, 0f);
        Vector3 tempRotation = RoundManager.Instance.tempTransform.eulerAngles;

        Vector3 eulerAngles = transform.eulerAngles;
        eulerAngles.y = Mathf.Round((eulerAngles.y + 90f) / 180f) * 180f - 90f;
        eulerAngles.z = Mathf.Round(eulerAngles.z / 90f) * 90f;
        float x = Mathf.Repeat(eulerAngles.x + UnityEngine.Random.Range(-5f, 5f) + 180, 360) - 180;
        eulerAngles.x = Mathf.Clamp(x, -20f, 20f);
        magnetTargetRotation = Quaternion.Euler(eulerAngles);

        Vector3 offset = new(0f, -0.5f, -boundsCollider.size.x * 0.5f * boundsCollider.transform.lossyScale.x);
        Vector3 localPos = StartOfRound.Instance.magnetPoint.position + offset;
        magnetTargetPosition = StartOfRound.Instance.elevatorTransform.InverseTransformPoint(localPos);

        magnetStartPosition = transform.position;
        magnetStartRotation = transform.rotation;

        Quaternion rotation = magnetTargetRotation;
        transform.rotation = rotation;

        CollectItemsInTruck();
        if (StartOfRound.Instance.inShipPhase) return;
        if (GameNetworkManager.Instance.localPlayerController == null) return;
        MagnetCarRpc(magnetTargetPosition, eulerAngles, magnetStartPosition, magnetStartRotation, tempRotation, averageVelocityAtMagnetStart);
    }

    [Rpc(SendTo.NotOwner)]
    public void MagnetCarRpc(Vector3 targetPosition, Vector3 targetRotation, Vector3 startPosition, Quaternion startRotation, Vector3 tempRotation, Vector3 avgVel)
    {
        SetVehicleKinematic(setKinematic: true);

        magnetedToShip = true;
        magnetTime = 0f;
        magnetRotationTime = 0f;
        averageVelocityAtMagnetStart = avgVel;
        RoundManager.Instance.tempTransform.eulerAngles = tempRotation;

        StartOfRound.Instance.isObjectAttachedToMagnet = true;
        StartOfRound.Instance.attachedVehicle = this;

        magnetStartPosition = startPosition;
        magnetStartRotation = startRotation;

        magnetTargetPosition = targetPosition;
        magnetTargetRotation = Quaternion.Euler(targetRotation);
        CollectItemsInTruck();
    }
    // --- AUTOPILOT MAGNET END ---


    private void ApplyCorrectionForce()
    {
        Vector3 upwardForce = Vector3.Cross(Quaternion.AngleAxis(mainRigidbody.angularVelocity.magnitude * 57.29578f * stability / speed, mainRigidbody.angularVelocity) * base.transform.up, Vector3.up);
        mainRigidbody.AddTorque(upwardForce * speed * speed);
    }

    private void MovePhysicsBodies()
    {
        ragdollPhysicsBody.Move(
          base.transform.position,
          base.transform.rotation);
        windwiperPhysicsBody1.Move(
          windwiper1.position,
          windwiper1.rotation);
        windwiperPhysicsBody2.Move(
          windwiper2.position,
          windwiper2.rotation);
        playerPhysicsBody.transform.localPosition = Vector3.zero;
        playerPhysicsBody.transform.localRotation = Quaternion.identity;
    }

    private void CalculateVehicleVelocity()
    {
        if (averageCount > movingAverageLength)
        {
            averageVelocity += (mainRigidbody.velocity - averageVelocity) / (float)(movingAverageLength + 1);
        }
        else
        {
            averageCount++;
            averageVelocity += mainRigidbody.velocity;
            if (averageCount == movingAverageLength)
            {
                averageVelocity /= (float)averageCount;
            }
        }
    }


    // --- DRIVETRAIN SYNC ---
    public void SyncWheelTorque()
    {
        if (syncTorqueInterval >= 0.14f)
        {
            float fWheelSyncRPM = Mathf.Round(currentMotorTorque);
            float bWheelSyncRPM = Mathf.Round(currentBrakeTorque);

            if (syncedCurrentMotorTorque != fWheelSyncRPM ||
                syncedCurrentBrakeTorque != bWheelSyncRPM)
            {
                syncTorqueInterval = 0f;
                syncedCurrentMotorTorque = currentMotorTorque;
                syncedCurrentBrakeTorque = currentBrakeTorque;
                SyncWheelTorqueRpc(currentMotorTorque, currentBrakeTorque);
                return;
            }
        }
        else
        {
            syncTorqueInterval += Time.fixedDeltaTime;
        }
    }

    [Rpc(SendTo.NotOwner)]
    public void SyncWheelTorqueRpc(float motorTorque, float brakeTorque)
    {
        syncedCurrentMotorTorque = motorTorque;
        syncedCurrentBrakeTorque = brakeTorque;
    }


    public void SyncDrivetrain()
    {
        float syncThreshold = 0.15f * averageVelocity.magnitude;
        syncThreshold = Mathf.Clamp(syncThreshold, 0.15f, 0.21f);
        if (syncDrivetrainInterval >= syncThreshold)
        {
            float engineSpeed = Mathf.Round(EngineRPM);

            float wheelSyncRPM = Mathf.Round(wheelRPM);
            float fWheelSyncRPM = Mathf.Round(frontWheelRPM);
            float bWheelSyncRPM = Mathf.Round(backWheelRPM);

            if (syncedWheelRPM != wheelSyncRPM ||
                syncedEngineRPM != engineSpeed)
            {
                syncDrivetrainInterval = 0f;

                syncedFrontWheelRPM = fWheelSyncRPM;
                syncedBackWheelRPM = bWheelSyncRPM;

                syncedWheelRPM = wheelSyncRPM;
                syncedEngineRPM = engineSpeed;

                SyncDrivetrainRpc(frontWheelRPM, backWheelRPM, wheelRPM, syncedEngineRPM);
                return;
            }
        }
        else
        {
            syncDrivetrainInterval += Time.fixedDeltaTime;
        }
    }

    [Rpc(SendTo.NotOwner)]
    public void SyncDrivetrainRpc(float frontWheelSpeed, float backWheelSpeed, float wheelSpeed, float engineSpeed)
    {
        syncedFrontWheelRPM = frontWheelSpeed;
        syncedBackWheelRPM = backWheelSpeed;
        syncedWheelRPM = wheelSpeed;
        syncedEngineRPM = engineSpeed;
    }


    // --- SYNC POSITION START ---
    public new void SyncCarPhysicsToOtherClients()
    {
        if (!IsOwner || magnetedToShip || inDropshipAnimation)
            return;

        SetVehicleKinematic(setKinematic: false);
        if (syncCarPositionInterval > 0.12f)
        {
            if (Vector3.Distance(syncedPosition, transform.position) > 0.02f)
            {
                syncCarPositionInterval = 0f;
                syncedPosition = transform.position;
                syncedRotation = transform.rotation;
                SyncCarPositionRpc(transform.position, transform.eulerAngles);
                return;
            }
            if (Vector3.Angle(transform.forward, syncedRotation * Vector3.forward) > 2f)
            {
                syncCarPositionInterval = 0f;
                syncedPosition = transform.position;
                syncedRotation = transform.rotation;
                SyncCarPositionRpc(transform.position, transform.eulerAngles);
                return;
            }
        }
        else
        {
            syncCarPositionInterval += Time.fixedDeltaTime;
        }
    }

    [Rpc(SendTo.NotOwner)]
    public void SyncCarPositionRpc(Vector3 carPosition, Vector3 carRotation)
    {
        syncCarPositionInterval = 0f;
        syncedPosition = carPosition;
        syncedRotation = Quaternion.Euler(carRotation);
    }
    // --- SYNC POSITION END ---


    private void ApplySteering()
    {
        //steeringAngle = maxSteeringAngle * steeringWheelAnimFloat;
        steeringAngle = steeringCurve.Evaluate(Mathf.Abs(steeringWheelAnimFloat)) * (maxSteeringAngle * Mathf.Sign(steeringWheelAnimFloat));
        FrontLeftWheel.steerAngle = steeringAngle;
        FrontRightWheel.steerAngle = steeringAngle;
    }

    private void ApplyWheelForces()
    {
        if (!IsOwner)
        {
            // front wheels
            SetTorqueToWheelCollider(FrontLeftWheel, currentMotorTorque, currentBrakeTorque);
            SetTorqueToWheelCollider(FrontRightWheel, currentMotorTorque, currentBrakeTorque);

            // back wheels
            SetTorqueToWheelCollider(BackLeftWheel, currentMotorTorque, currentBrakeTorque);
            SetTorqueToWheelCollider(BackRightWheel, currentMotorTorque, currentBrakeTorque);

            SetWheelRotationVelocity();
            return;
        }
        // front wheels
        SetTorqueToWheelCollider(FrontLeftWheel, currentMotorTorque, currentBrakeTorque);
        SetTorqueToWheelCollider(FrontRightWheel, currentMotorTorque, currentBrakeTorque);
        fRpmDiff = FrontRightWheel.rpm - FrontLeftWheel.rpm; // difference in rpm

        // back wheels
        SetTorqueToWheelCollider(BackLeftWheel, currentMotorTorque, currentBrakeTorque);
        SetTorqueToWheelCollider(BackRightWheel, currentMotorTorque, currentBrakeTorque);
        bRpmDiff = BackLeftWheel.rpm - BackRightWheel.rpm; // difference in rpm

        //ApplyTorqueDifference();
        SetWheelRotationVelocity();
    }

    private void SetTorqueToWheelCollider(WheelCollider wheelCollider, float motorForce, float brakeForce)
    {
        wheelCollider.motorTorque = motorForce;
        wheelCollider.brakeTorque = brakeForce;
    }

    private void SetWheelRotationVelocity()
    {
        // rotation speed-limiter
        FrontLeftWheel.rotationSpeed = Mathf.Clamp(FrontLeftWheel.rotationSpeed, reverseWheelSpeed, forwardWheelSpeed);
        FrontRightWheel.rotationSpeed = Mathf.Clamp(FrontRightWheel.rotationSpeed, reverseWheelSpeed, forwardWheelSpeed);
        BackLeftWheel.rotationSpeed = Mathf.Clamp(BackLeftWheel.rotationSpeed, reverseWheelSpeed, forwardWheelSpeed);
        BackRightWheel.rotationSpeed = Mathf.Clamp(BackRightWheel.rotationSpeed, reverseWheelSpeed, forwardWheelSpeed);
    }

    private void SetVFXWheelSpeed()
    {
        float fWheelSpeed = Mathf.Round(frontWheelRPM / 2f) * 2f;
        float bWheelSpeed = Mathf.Round(backWheelRPM / 2f) * 2f;
        frontWheelsRPM = Mathf.Repeat(frontWheelsRPM + (fWheelSpeed * 0.15f) * Mathf.Rad2Deg * Time.fixedDeltaTime, 360f);
        backWheelsRPM = Mathf.Repeat(backWheelsRPM + (bWheelSpeed * 0.25f) * Mathf.Rad2Deg * Time.fixedDeltaTime, 360f);
    }

    private void CalculateWheelSlip(bool calculatePhysics = false)
    {
        if (!calculatePhysics)
        {
            forwardsSlip = 0f;
            sidewaysSlip = 0f;
            return;
        }
        for (int i = 0; i < allWheels.Count; i++)
        {
            if (allWheels[i].GetGroundHit(out var hit))
            {
                wheelHits[i] = hit;
            }
            else
            {
                wheelHits[i] = default;
            }
        }
        forwardsSlip = (wheelHits[2].forwardSlip + wheelHits[3].forwardSlip) * 0.5f;
        sidewaysSlip = (wheelHits[2].sidewaysSlip + wheelHits[3].sidewaysSlip) * 0.5f;
        forwardSlipping = currentMotorTorque > 800f && Mathf.Abs(forwardsSlip) > 0.35f;
        sidewaySlipping = currentMotorTorque > 800f && Mathf.Abs(sidewaysSlip) > 0.2f;
    }

    private void UpdateInclineCompensation()
    {
        if (!IsOwner || !ignitionStarted || gear != CarGearShift.Drive)
        {
            inclineBoost = 1f;
            return;
        }

        float selectedGear = Mathf.Abs(gearRatios[currentGear]);
        float absWheelSpeed = Mathf.Abs(wheelRPM);

        // hill assist
        float uphillDot = Vector3.Dot(transform.forward, Vector3.up);
        float inclineAngle = Vector3.Angle(transform.forward, Vector3.ProjectOnPlane(transform.forward, Vector3.up)); // get the slope angle
        float assistFactor = Mathf.InverseLerp(110f, 0f, absWheelSpeed); // rpm the assist is active up until
        float inclineFactor = uphillDot > 0f ? Mathf.Clamp01(inclineAngle / maxInclineBoostAngle) * assistFactor : 0f; // only apply uphill

        inclineBoost = Mathf.Lerp(minInclineBoost, maxInclineBoost, inclineFactor);
    }

    private void UpdateTransmission()
    {
        switch (gear)
        {
            case CarGearShift.Park:
                currentGear = 1;

                forwardWheelSpeed = 5000f;
                reverseWheelSpeed = -5000f;
                break;
            case CarGearShift.Reverse:
                currentGear = 0;

                // this has to be inverted for reverse
                forwardWheelSpeed = MaxEngineRPM / (gearRatios[Mathf.Clamp(currentGear, gearRatios.Length - 1, 1)] * diffRatio) * (360f / 60f);
                reverseWheelSpeed = MaxEngineRPM / (gearRatios[0] * diffRatio) * (360f / 60f);
                break;
            case CarGearShift.Drive:
                if (currentGear < 1) // do not let the current gear drop below its minimum
                    currentGear = 1;

                // ensure we don't set a reverse speed on the forward speed
                forwardWheelSpeed = MaxEngineRPM / (gearRatios[Mathf.Clamp(currentGear, 1, gearRatios.Length - 1)] * diffRatio) * (360f / 60f);
                // 0 in our array is always reverse, so use zero for the backwards speed
                reverseWheelSpeed = MaxEngineRPM / (gearRatios[0] * diffRatio) * (360f / 60f);

                if (shiftGearCoroutine != null)
                    break;

                if (Time.realtimeSinceStartup - lastShiftTime > shiftCooldown)
                {
                    if (EngineRPM >= upShiftThreshold && currentGear < gearRatios.Length - 1)
                    {
                        lastShiftTime = Time.realtimeSinceStartup;
                        shiftGearCoroutine = StartCoroutine(ChangeGear(true));
                    }
                    else if (EngineRPM <= downShiftThreshold && currentGear > 1)
                    {
                        lastShiftTime = Time.realtimeSinceStartup;
                        shiftGearCoroutine = StartCoroutine(ChangeGear(false));
                    }
                }
                break;
        }
    }

    private IEnumerator ChangeGear(bool shiftUp)
    {
        yield return new WaitForSeconds(shiftTime);

        if (shiftUp) currentGear++;
        else currentGear--;

        shiftGearCoroutine = null!;
        yield break;
    }

    private void UpdateCarStress()
    {
        if (!ignitionStarted)
        {
            return;
        }
        float vehicleStress = 0f;
        switch (gear)
        {
            case CarGearShift.Park:
                {
                    if (drivePedalPressed)
                    {
                        vehicleStress += 3.25f;
                        lastStressType += "; Accelerating while in park";
                    }
                    else if (!allWheelsAirborne && Mathf.Abs(wheelRPM) > 150f)
                    {
                        vehicleStress += Mathf.Clamp((Mathf.Abs(wheelRPM) - 100f) / 350f, 0f, 1.3f);
                        lastStressType += "; In park while at high speed";
                    }
                    break;
                }
        }
        SetInternalStress(vehicleStress);
        stressPerSecond = vehicleStress;
    }

    private void UpdateEngineRPMFromWheels()
    {
        frontWheelRPM = (FrontLeftWheel.rpm + FrontRightWheel.rpm) / 2f;
        backWheelRPM = (BackLeftWheel.rpm + BackRightWheel.rpm) / 2f;
        wheelRPM = (frontWheelRPM + backWheelRPM) / 2f;
        float wheelSpeed = Mathf.Abs(wheelRPM);

        if (!ignitionStarted)
        {
            EngineRPM = Mathf.Lerp(EngineRPM, 0f, 3f * Time.fixedDeltaTime);
            return;
        }

        float selectedGear = Mathf.Abs(gearRatios[currentGear]);
        //enginePower = enginePowerCurve.Evaluate(EngineRPM / MaxEngineRPM) * EngineTorque * (selectedGear * diffRatio) * 5252f / EngineRPM;
        enginePower = enginePowerCurve.Evaluate(EngineRPM / MaxEngineRPM) * EngineTorque * diffRatio * 5252f / EngineRPM;

        switch (gear)
        {
            case CarGearShift.Park:
                {
                    EngineRPM = Mathf.Lerp(EngineRPM, drivePedalPressed ? MinEngineRPM + 2500f : MinEngineRPM, drivePedalPressed ? 0.45f * Time.fixedDeltaTime : 5f * Time.fixedDeltaTime);
                    break;
                }
            case CarGearShift.Reverse:
                {
                    EngineRPM = Mathf.Clamp(wheelSpeed * selectedGear * diffRatio, MinEngineRPM, MaxEngineRPM);
                    //EngineRPM = Mathf.Lerp(EngineRPM, Mathf.Clamp(wheelSpeed * selectedGear * diffRatio, MinEngineRPM, MaxEngineRPM), 5f * Time.fixedDeltaTime);
                    break;
                }
            case CarGearShift.Drive:
                {
                    EngineRPM = Mathf.Clamp(wheelSpeed * selectedGear * diffRatio, MinEngineRPM, MaxEngineRPM);
                    //EngineRPM = Mathf.Lerp(EngineRPM, Mathf.Clamp(wheelSpeed * selectedGear * diffRatio, MinEngineRPM, MaxEngineRPM), 5f * Time.fixedDeltaTime);
                    break;
                }
        }
        //EngineRPM = Mathf.Abs(wheelRPM); // vanilla-esque implementation
        // vanilla derives the "engine rpm" from the just the wheels rpm, which is fundementally wrong
    }

    private void SetTorqueForces(bool useSynced = true)
    {
        if (!ignitionStarted)
        {
            currentMotorTorque = 0f;
            currentBrakeTorque = gear == CarGearShift.Park ? maxParkingBrakeTorque : maxBrakeTorque;
            return;
        }
        if (useSynced)
        {
            currentMotorTorque = syncedCurrentMotorTorque;
            currentBrakeTorque = syncedCurrentBrakeTorque;
            return;
        }
        switch (gear)
        {
            case CarGearShift.Park:
                {
                    boostMultiplier = 0f;
                    maxForwardTorque = 0f;
                    currentMotorTorque = 0f;
                    currentBrakeTorque = Mathf.MoveTowards(currentBrakeTorque, maxParkingBrakeTorque, brakeAcceleration * Time.fixedDeltaTime);
                    break;
                }
            case CarGearShift.Reverse:
                {
                    if (allowBoostTorque)
                    {
                        if (wheelRPM > reverseBoostThreshold)
                        {
                            boostMultiplier = Mathf.MoveTowards(boostMultiplier, boostMultiplierLimit, reverseBoostSpeed * Time.fixedDeltaTime);
                        }
                        else
                        {
                            boostMultiplier = Mathf.MoveTowards(boostMultiplier, 1f, boostReturnSpeed * Time.fixedDeltaTime);
                        }
                    }
                    else boostMultiplier = 1f;

                    maxForwardTorque = 0f;
                    currentMotorTorque = drivePedalPressed ? engineReversePower : -idleSpeed;
                    //currentTorque = drivePedalPressed ? Mathf.Clamp(Mathf.MoveTowards(currentTorque, -maxReverseTorque, ((reverseCarAcceleration / 4f) * boostMultiplier) * Time.fixedDeltaTime), -maxReverseTorque, -minTorque) : -idleSpeed;
                    currentBrakeTorque = Mathf.MoveTowards(currentBrakeTorque, brakePedalPressed ? maxBrakeTorque : 0f, brakeAcceleration * Time.fixedDeltaTime);
                    break;
                }
            case CarGearShift.Drive:
                {
                    if (allowBoostTorque)
                    {
                        if (wheelRPM < forwardBoostThreshold)
                        {
                            boostMultiplier = Mathf.MoveTowards(boostMultiplier, boostMultiplierLimit, forwardBoostSpeed * Time.fixedDeltaTime);
                        }
                        else
                        {
                            boostMultiplier = Mathf.MoveTowards(boostMultiplier, 1f, boostReturnSpeed * Time.fixedDeltaTime);
                        }
                    }
                    else boostMultiplier = 1f;

                    maxForwardTorque = (enginePower * torqueBoost) * inclineBoost;
                    if (drivePedalPressed) currentMotorTorque = Mathf.Clamp(Mathf.MoveTowards(currentMotorTorque, maxForwardTorque, (carAcceleration * inclineBoost) * Time.fixedDeltaTime), minTorque * inclineBoost, maxForwardTorque);
                    else currentMotorTorque = Mathf.MoveTowards(currentMotorTorque, idleSpeed, (carDeacceleration / inclineBoost) * Time.fixedDeltaTime);
                    //currentTorque = drivePedalPressed ? Mathf.Clamp(Mathf.MoveTowards(currentTorque, maxForwardTorque, ((carAcceleration / 4f) * boostMultiplier) * Time.fixedDeltaTime), minTorque, maxForwardTorque) : idleSpeed;
                    currentBrakeTorque = Mathf.MoveTowards(currentBrakeTorque, brakePedalPressed ? maxBrakeTorque : 0f, brakeAcceleration * Time.fixedDeltaTime);
                    break;
                }
        }
    }

    private void ApplyTorqueDifference()
    {
        if (!ignitionStarted || gear == CarGearShift.Park || averageVelocity.magnitude > 10f)
            return;

        FrontRightWheel.motorTorque -= fRpmDiff * 0.5f;
        FrontLeftWheel.motorTorque += fRpmDiff * 0.5f;
        BackLeftWheel.motorTorque -= bRpmDiff * 0.5f;
        BackRightWheel.motorTorque += bRpmDiff * 0.5f;
    }

    private void ApplyAntiSlipForce()
    {
        Vector3 groundNormal = Vector3.zero;
        for (int i = 0; i < wheelHits.Length; i++)
        {
            groundNormal += wheelHits[i].normal;
        }
        groundNormal = groundNormal.normalized;

        if (!allWheelsGrounded || Vector3.Angle(-groundNormal, Physics.gravity) > 30f)
            return;

        Vector3 carFrontHillDirection = Vector3.ProjectOnPlane(transform.forward, groundNormal).normalized;
        Vector3 hillGravity = -groundNormal * Physics.gravity.magnitude;

        Vector3 force = hillGravity - Physics.gravity; //apply the difference between real gravity and the 'hill' downward gravity

        //if we're not in park, don't apply forces in the forward or backward direction (car should still roll down hills)
        if (gear != CarGearShift.Park)
        {
            force = Vector3.ProjectOnPlane(force, carFrontHillDirection);
        }
        mainRigidbody.AddForce(force, ForceMode.Acceleration);
    }


    // --- HELPER FUNCTIONS ---
    private void MatchWheelMeshToCollider(MeshRenderer wheelMesh, WheelCollider wheelCollider, float wheelSpeed, float steeringInput = 0f)
    {
        /*
        Vector3 position = wheelCollider.transform.position;
        if (Physics.Raycast(position, -wheelCollider.transform.up, out hit, wheelCollider.suspensionDistance + wheelCollider.radius, 2305))
        {
            wheelMesh.transform.position = hit.point + wheelCollider.transform.up * wheelCollider.radius;
        }
        else
        {
            wheelMesh.transform.position = position - wheelCollider.transform.up * wheelCollider.suspensionDistance;
        }
        */

        /*
        wheelCollider.GetWorldPose(out Vector3 wheelPosition, out Quaternion wheelRotation);
        wheelMesh.transform.position = wheelPosition;
        wheelMesh.transform.rotation = wheelRotation;
        */
        wheelCollider.GetWorldPose(out Vector3 wheelPosition, out _);
        wheelMesh.transform.position = wheelPosition;
        wheelMesh.transform.localRotation = Quaternion.Euler(wheelSpeed, steeringInput, 0.0f);
    }


    // --- UPDATE ---
    public new void Update()
    {
        if (destroyNextFrame)
        {
            Destroy(base.windwiperPhysicsBody1.gameObject);
            Destroy(base.windwiperPhysicsBody2.gameObject);
            Destroy(base.ragdollPhysicsBody.gameObject);
            if (IsOwner)
            {
                Destroy(base.gameObject);
            }
            return;
        }
        if (NetworkObject != null && !NetworkObject.IsSpawned)
        {
            OnVehicleDisable();
            destroyNextFrame = true;
            return;
        }
        if (magnetedToShip)
        {
            if (!StartOfRound.Instance.magnetOn)
            {
                magnetedToShip = false;
                if (StartOfRound.Instance.attachedVehicle == this)
                {
                    StartOfRound.Instance.isObjectAttachedToMagnet = false;
                }
                CollectItemsInTruck();
                return;
            }
            magnetTime = Mathf.Min(magnetTime + Time.deltaTime, 1f);
            magnetRotationTime = Mathf.Min(magnetTime + Time.deltaTime * 0.75f, 1f);
            if (!finishedMagneting && magnetTime > 0.7f)
            {
                finishedMagneting = true;
                turbulenceAmount = 2f;
                turbulenceAudio.volume = 0.6f;
                turbulenceAudio.PlayOneShot(maxCollisions[UnityEngine.Random.Range(0, maxCollisions.Length)]);
            }
        }
        else
        {
            finishedMagneting = false;
            if (StartOfRound.Instance.attachedVehicle == this)
            {
                StartOfRound.Instance.attachedVehicle = null;
            }
        }

        ReactToDamage();
        if (carDestroyed)
        {
            return;
        }

        SetCarEffects(steeringWheelAnimValue);
        UpdateOccupantAnimations();
        if (localPlayerInControl && ignitionStarted)
        {
            GetVehicleInput();
            return;
        }
        moveInputVector = Vector2.zero;
        steeringAnimValue = 0f;
        steeringWheelAnimValue = 0f;
    }


    // --- MISC DAMAGE ---
    private new void ReactToDamage()
    {
        normalisedCarHP = (float)carHP / baseCarHP;
        healthMeter.localScale = new Vector3(1f, 1f, Mathf.Lerp(healthMeter.localScale.z, Mathf.Clamp(normalisedCarHP, 0.01f, 1f), 6f * Time.deltaTime));

        if (!IsOwner)
            return;

        if (carHP < 18 && Time.realtimeSinceStartup - timeAtLastDamage > 21f)
        {
            timeAtLastDamage = Time.realtimeSinceStartup;
            carHP++;
            syncedCarHP = carHP;
            SyncCarHealthRpc(carHP);
        }
        if (carHP < 9)
        {
            if (!isHoodOnFire)
                SetHoodFireAndSync(setOnFire: true);
        }
        else if (isHoodOnFire && carHP >= 9)
        {
            SetHoodFireAndSync(setOnFire: false);
        }
    }


    // --- DAMAGE/HEALTH SYNC ---
    [Rpc(SendTo.NotOwner)]
    private void SyncCarHealthRpc(int carHealth)
    {
        timeAtLastDamage = Time.realtimeSinceStartup;
        syncedCarHP = carHealth;
        carHP = syncedCarHP;
    }

    [Rpc(SendTo.NotOwner)]
    public void SyncExtremeStressRpc(bool underStress)
    {
        if (carDestroyed)
        {
            underExtremeStress = false;
        }
        else
        {
            underExtremeStress = underStress;
        }
    }


    // --- HOOD FIRE VFX ---
    private void SetHoodFireAndSync(bool setOnFire)
    {
        SetHoodOnFireLocalClient(setOnFire);
        SetHoodOnFireRpc(setOnFire);
    }

    [Rpc(SendTo.NotOwner)]
    public void SetHoodOnFireRpc(bool onFire)
    {
        SetHoodOnFireLocalClient(onFire);
    }

    private void SetHoodOnFireLocalClient(bool setOnFire)
    {
        isHoodOnFire = setOnFire;
        if (setOnFire)
        {
            hoodFireAudio.Play();
            hoodFireParticle.Play();
            if (!carHoodOpen && !carDestroyed) SetHoodOpenLocalClient(setOpen: true);
            return;
        }
        hoodFireAudio.Stop();
        hoodFireParticle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
    }


    // --- HOOD INTERACTION ---
    public new void ToggleHoodOpenLocalClient()
    {
        carHoodOpen = !carHoodOpen;
        carHoodAnimator.SetBool(HOOD_ANIMATION_BOOL, carHoodOpen);
        SetHoodOpenRpc(open: carHoodOpen);
    }

    // used for when the hood is 'on fire'
    public new void SetHoodOpenLocalClient(bool setOpen)
    {
        if (carHoodOpen == setOpen)
            return;

        carHoodOpen = setOpen;
        carHoodAnimator.SetBool(HOOD_ANIMATION_BOOL, setOpen);
    }

    [Rpc(SendTo.NotMe, RequireOwnership = false)]
    public void SetHoodOpenRpc(bool open)
    {
        carHoodOpen = open;
        carHoodAnimator.SetBool(HOOD_ANIMATION_BOOL, carHoodOpen);
    }


    // --- OCCUPANT ANIMATIONS ---
    private void UpdateOccupantAnimations()
    {
        if (currentDriver == null || currentDriver.playerBodyAnimator == null)
            return;

        if (disableAnimations ||
            keyIgnitionCoroutine != null ||
            !ignitionStarted)
            return;

        int currentAnimIndex = 1;
        if (playerWhoShifted == currentDriver && Time.realtimeSinceStartup - timeAtLastGearShift < 1.35f) currentAnimIndex = 5;
        currentDriver.playerBodyAnimator.SetInteger(CAR_ANIM, currentAnimIndex);

        currentDriver.playerBodyAnimator.SetFloat(ANIMATION_SPEED, playerSteeringWheelAnimFloat); // player steering animation
        currentDriver.playerBodyAnimator.SetFloat(CAR_MOTION_TIME, gearStickAnimValue); // vehicle gearstick --> player gearstick animation position
    }


    // --- AFTER UPDATE ---
    public new void LateUpdate()
    {
        if (currentDriver != null && lastDriver != currentDriver && !magnetedToShip)
            lastDriver = currentDriver;
    }

    public void SetVehicleAudioMuted(bool setMuted)
    {
        for (int i = 0; i < allVehicleAudios.Length; i++)
        {
            allVehicleAudios[i].mute = setMuted;
        }
    }


    // --- ITEM COLLECTION SAFETY ---
    public new void CollectItemsInTruck()
    {
        Collider[] array = Physics.OverlapSphere(transform.position, 25f, 64, QueryTriggerInteraction.Collide);
        for (int i = 0; i < array.Length; i++)
        {
            GrabbableObject itemInTruck = array[i].GetComponent<GrabbableObject>();
            if (itemInTruck == null ||
                itemInTruck.isHeld ||
                itemInTruck.isHeldByEnemy ||
                itemInTruck.transform.parent != transform)
                continue;

            if (lastDriver == null)
            {
                GameNetworkManager.Instance.localPlayerController?.SetItemInElevator(magnetedToShip, magnetedToShip, itemInTruck);
                continue;
            }
            lastDriver.SetItemInElevator(magnetedToShip, magnetedToShip, itemInTruck);
        }
    }


    // --- WEEDKILLER FUNCTIONALITY ---
    public new void AddEngineOil()
    {
        int setEngineHealth = Mathf.Min(carHP + 4, baseCarHP);
        AddEngineOilOnLocalClient(setEngineHealth);
        AddEngineOilRpc(setEngineHealth);
    }

    public new void AddEngineOilOnLocalClient(int setCarHP)
    {
        hoodAudio.PlayOneShot(pourOil);
        carHP = setCarHP;
    }

    [Rpc(SendTo.NotMe, RequireOwnership = false)]
    public void AddEngineOilRpc(int setHP)
    {
        AddEngineOilOnLocalClient(setHP);
    }


    // --- ROCK ABILITY ---
    private new void DoTurboBoost(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        PlayerControllerB playerController = GameNetworkManager.Instance.localPlayerController;
        if (playerController == null ||
            playerController.isPlayerDead ||
            !playerController.isPlayerControlled) 
            return;

        if (!localPlayerInControl)
            return;

        if (playerController.isTypingChat ||
            playerController.quickMenuManager.isMenuOpen) 
            return;

        if (!ignitionStarted || jumpingInCar || keyIgnitionCoroutine != null)
            return;

        Vector2 dir = InputSystem.actions.FindAction(PLAYER_MOVEMENT, false).ReadValue<Vector2>();
        UseTurboBoostLocalClient(dir);
        UseTurboBoostRpc();
    }

    public new void UseTurboBoostLocalClient(Vector2 dir = default(Vector2))
    {
        currentDriver?.playerBodyAnimator.SetTrigger(JUMP_WHILE_IN_CAR);
        currentDriver?.movementAudio.PlayOneShot(jumpInCarSFX);
        if (IsOwner)
        {
            jumpingInCar = true;
            StartCoroutine(JerkCarUpward(dir));
        }
    }

    private IEnumerator JerkCarUpward(Vector3 dir)
    {
        if (!IsOwner)
        {
            jumpingInCar = false;
            yield break;
        }
        yield return new WaitForSeconds(0.16f);
        Vector3 jerkForce = transform.TransformDirection(new Vector3(dir.x, 0f, dir.y));
        mainRigidbody.AddForce(jerkForce * turboBoostForce * 0.22f + Vector3.up * turboBoostUpwardForce * 0.1f, ForceMode.Impulse);
        mainRigidbody.AddForceAtPosition(Vector3.up * jumpForce, hoodFireAudio.transform.position - Vector3.up * 2f, ForceMode.Impulse);
        yield return new WaitForSeconds(0.15f);
        jumpingInCar = false;
        yield break;
    }

    [Rpc(SendTo.NotOwner)]
    public void UseTurboBoostRpc()
    {
        UseTurboBoostLocalClient(default(Vector2));
    }


    // --- PUSH METHODS ---
    public new void PushTruckWithArms()
    {
        if (magnetedToShip)
            return;

        if (GameNetworkManager.Instance.localPlayerController.overridePhysicsParent != null)
            return;

        if (GameNetworkManager.Instance.localPlayerController.physicsParent == physicsRegion.physicsTransform)
            return;

        if (!Physics.Raycast(
            GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position,
            GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.forward,
            out hit,
            10f,
            1073742656,
            QueryTriggerInteraction.Ignore))
            return;

        Vector3 point = hit.point;
        Vector3 forward = GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.forward;

        tempPushTransform.position = point;
        Vector3 hitPoint = tempPushTransform.localPosition;

        float turbulence = Mathf.Min(turbulenceAmount + 0.5f, 2f);

        if (IsOwner)
        {
            PlayPushAudio(hitPoint, turbulence);
            mainRigidbody.AddForceAtPosition(Vector3.Normalize(forward * 1000f) * UnityEngine.Random.Range(40f, 50f) * pushForceMultiplier, tempPushTransform.position - mainRigidbody.transform.up * pushVerticalOffsetAmount, ForceMode.Impulse);
            PushTruckFromOwnerRpc(hitPoint, turbulence);
            return;
        }
        PushTruckRpc(hitPoint, forward, turbulence);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void PushTruckRpc(Vector3 pushPosition, Vector3 dir, float turbulence)
    {
        PlayPushAudio(pushPosition, turbulence);
        if (IsOwner)
        {
            tempPushTransform.localPosition = pushPosition;
            mainRigidbody.AddForceAtPosition(Vector3.Normalize(dir * 1000f) * UnityEngine.Random.Range(40f, 50f) * pushForceMultiplier, tempPushTransform.position - mainRigidbody.transform.up * pushVerticalOffsetAmount, ForceMode.Impulse);
        }
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void PushTruckFromOwnerRpc(Vector3 pushPosition, float turbulence)
    {
        PlayPushAudio(pushPosition, turbulence);
    }

    public void PlayPushAudio(Vector3 pos, float turbulence)
    {
        pushAudio.transform.localPosition = pos;
        pushAudio.Play();
        turbulenceAmount = turbulence;
    }


    // --- REMOVAL ---

    public void OnVehicleDisable()
    {
        RemoveRainCollision();
        DisableControl();
        vehicleZone.disablePhysicsRegion = true;
        if (StartOfRound.Instance.CurrentPlayerPhysicsRegions.Contains(vehicleZone))
        {
            StartOfRound.Instance.CurrentPlayerPhysicsRegions.Remove(vehicleZone);
        }
        for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
        {
            PlayerControllerB playerController = StartOfRound.Instance.allPlayerScripts[i];
            if (playerController.transform.parent == vehicleZone.physicsTransform)
            {
                Transform playerTransform = playerController.isInElevator ? playerController.playersManager.elevatorTransform : playerController.playersManager.playersContainer;
                playerController.transform.SetParent(playerTransform);
                Plugin.LogWarning($"Player {i} setting parent since vehicle was disabled");
            }
        }
        PlayerControllerB localPlayerController = GameNetworkManager.Instance.localPlayerController;
        if (IsPlayerSeatedInCar())
        {
            localPlayerController?.CancelSpecialTriggerAnimations();
        }
        GrabbableObject[] componentsInChildren = physicsRegion.physicsTransform.GetComponentsInChildren<GrabbableObject>();
        for (int i = 0; i < componentsInChildren.Length; i++)
        {
            if (RoundManager.Instance.mapPropsContainer != null)
            {
                componentsInChildren[i].transform.SetParent(RoundManager.Instance.mapPropsContainer.transform, worldPositionStays: true);
            }
            else
            {
                componentsInChildren[i].transform.SetParent(null, worldPositionStays: true);
            }
            if (!componentsInChildren[i].isHeld)
            {
                componentsInChildren[i].FallToGround(false, false, default(Vector3));
            }
        }
    }

    public new void OnDisable()
    {
        OnVehicleDisable();
    }


    // --- MISC ---
    public void PlayLeftDashboardButtonPress()
    {
        leftDashboardAudio.PlayOneShot(dashButtonPress);
        PlayLeftDashboardButtonPressRpc();
    }

    [Rpc(SendTo.NotMe, RequireOwnership = false)]
    private void PlayLeftDashboardButtonPressRpc()
    {
        leftDashboardAudio.PlayOneShot(dashButtonPress);
    }


    public void PlayCenterDashboardButtonPress()
    {
        centerDashboardAudio.PlayOneShot(dashButtonPress);
        PlayCenterDashboardButtonPressRpc();
    }

    [Rpc(SendTo.NotMe, RequireOwnership = false)]
    private void PlayCenterDashboardButtonPressRpc()
    {
        centerDashboardAudio.PlayOneShot(dashButtonPress);
    }


    public void PlayRoofInteractionButtonPress()
    {
        roofInteractionAudio.PlayOneShot(dashButtonPress);
        PlayRoofInteractionButtonPressRpc();
    }

    [Rpc(SendTo.NotMe, RequireOwnership = false)]
    private void PlayRoofInteractionButtonPressRpc()
    {
        roofInteractionAudio.PlayOneShot(dashButtonPress);
    }


    // --- HEADLAMPS ---
    public new void ToggleHeadlightsLocalClient()
    {
        headlampsOn = !headlampsOn;
        headlightsContainer.SetActive(headlampsOn);
        leftDashboardAudio.PlayOneShot(headlightsToggleSFX);
        SetHeadlightMaterial(headlightsContainer.activeSelf);
        ToggleHeadlightsRpc(headlampsOn);
    }

    [Rpc(SendTo.NotMe, RequireOwnership = false)]
    public void ToggleHeadlightsRpc(bool setLightsOn)
    {
        headlampsOn = setLightsOn;
        headlightsContainer.SetActive(headlampsOn);
        leftDashboardAudio.PlayOneShot(headlightsToggleSFX);
        SetHeadlightMaterial(headlampsOn);
    }

    public new void SetHeadlightMaterial(bool on)
    {
        Material headlightMat = on ? headlightsOnMat : headlightsOffMat;

        var mats = mainBodyMesh.sharedMaterials;
        mats[3] = headlightMat;
        mainBodyMesh.sharedMaterials = mats;

        mats = lod1Mesh.sharedMaterials;
        mats[2] = headlightMat;
        lod1Mesh.sharedMaterials = mats;

        mats = lod2Mesh.sharedMaterials;
        mats[1] = headlightMat;
        lod2Mesh.sharedMaterials = mats;
    }

    internal void SetDashboardGaugesOn(bool on)
    {
        if (gaugesOn == on)
            return;

        gaugesOn = on;
        gaugeLightContainer.SetActive(gaugesOn);

        speedometerMesh.material = on ? needleOnMaterial : needleOffMaterial;
        tachometerMesh.material = on ? needleOnMaterial : needleOffMaterial;

        speedometerImage.material = on ? speedometerOnMaterial : speedometerOffMaterial;
        tachometerImage.material = on ? tachometerOnMaterial : tachometerOffMaterial;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsPlayerOnOutside(PlayerControllerB playerScript)
    {
        return IsPlayerAttached(playerScript) &&
               !IsPlayerInCab(playerScript) &&
               !IsPlayerSeated(playerScript);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsPlayerInCab(PlayerControllerB player)
    {
        return player != null && player is IPlayerManager playerInterface &&
            playerInterface.CurrentPlayerZone != null && playerInterface.CurrentPlayerZone == vehicleCabZone;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsCabinEnclosed()
    {
        return !driverSideDoor.boolValue && !passengerSideDoor.boolValue &&
               !backLeftDoor.boolValue && !backRightDoor.boolValue &&
               !frontLeftWindow.isWindowOpen && !frontRightWindow.isWindowOpen &&
               !backLeftWindow.isWindowOpen && !backRightWindow.isWindowOpen &&
               !windshieldBroken && !sunroofOpen;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsPlayerInCabinOrSeated(PlayerControllerB player)
    {
        return player != null && player is IPlayerManager playerInterface &&
            ((playerInterface.CurrentPlayerZone != null && playerInterface.CurrentPlayerZone == vehicleCabZone) ||
            (player.overridePhysicsParent != null && player.overridePhysicsParent == transform));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsPlayerInCabin(PlayerControllerB player)
    {
        return player != null && player is IPlayerManager playerInterface &&
            playerInterface.CurrentPlayerZone != null && playerInterface.CurrentPlayerZone == vehicleCabZone &&
            player.overridePhysicsParent == null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsPlayerAttached(PlayerControllerB player)
    {
        return player != null && player is IPlayerManager playerInterface &&
            playerInterface.CurrentPlayerPhysicsRegion != null && playerInterface.CurrentPlayerPhysicsRegion == vehicleZone;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsPlayerSeated(PlayerControllerB player)
    {
        return player != null && player.overridePhysicsParent != null && player.overridePhysicsParent == transform;
    }
}