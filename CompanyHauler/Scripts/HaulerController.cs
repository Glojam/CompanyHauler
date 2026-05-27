using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CompanyHauler.Utils;
using GameNetcodeStuff;
using ScandalsTweaks.Utils;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;

namespace CompanyHauler.Scripts;

public class HaulerController : VehicleController
{
    [Header("DEBUG")]

    public bool isInTestMode = false;

    public float debugMotorTorque;
    public float debugBrakeTorque;
    public float debugSteeringAngle;

    [Header("Networking")]

    public Vector2 syncedMoveInputVector;
    public bool syncedDrivePedalPressed;
    public bool syncedBrakePedalPressed;

    internal bool syncedTyreSlipping;
    internal float syncedTyreStress;

    internal float syncedEngineRPM;
    internal float syncedFrontWheelRPM;
    internal float syncedBackWheelRPM;
    internal float syncedWheelRPM;

    internal int syncedCarHP;

    internal float syncEffectsInterval;
    internal float syncTorqueInterval;
    internal float syncDrivetrainInterval;

    internal float syncedSteeringWheelRotation;
    internal float syncedCurrentMotorTorque;
    internal float syncedCurrentBrakeTorque;

    [Header("Player")]

    public HaulerPhysicsRegion vehicleZone = null!;
    public HaulerPlayerZone vehicleCabZone = null!;

    public PlayerControllerB lastDriver = null!;
    public PlayerControllerB playerWhoShifted = null!;
    public PlayerControllerB? currentBackLeftPassenger = null!;
    public PlayerControllerB? currentMiddlePassenger = null!;
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

    // animations
    internal const string STEERING_WHEEL_SPEED = "steeringWheelTurnSpeed";
    internal const string ANIMATION_SPEED = "animationSpeed";
    internal const string IGNITION_ANIM = "SAIgnition_Anim";
    internal const string CAR_ANIM = "SA_CarAnim";
    internal const string JUMP_WHILE_IN_CAR = "SA_JumpInCar";
    internal const string CAR_MOTION_TIME = "SA_CarMotionTime";

    [Header("Physics")]

    public HaulerWheelCollider frontLeftWheel = null!;
    public HaulerWheelCollider frontRightWheel = null!;
    public HaulerWheelCollider backLeftWheel = null!;
    public HaulerWheelCollider backRightWheel = null!;

    public HaulerCollisionTrigger collisionTrigger = null!;
    public float timeSinceLastCollision;

    public List<WheelCollider> allWheels = null!;
    public WheelHit[] wheelHits = new WheelHit[4];

    public Rigidbody playerPhysicsBody = null!;

    public Vector3 previousVehiclePosition;
    public Quaternion previousVehicleRotation;

    public AnimationCurve engineCurve = null!;
    public AnimationCurve enginePowerCurve = null!;

    public float torqueBoost = 1.25f;
    public float enginePower;
    public float engineReversePower;

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
    private Coroutine radioOnCoroutine = null!;

    public Collider[] weatherEffectBlockers = null!;

    public InteractTrigger startIgnitionTrigger = null!;
    public InteractTrigger stopIgnitionTrigger = null!;

    public Animator ignitionAnimator = null!;
    public GameObject carKeyContainer = null!;
    public GameObject keyItemHolder = null!;
    public Transform ignitionKeyPosition = null!;

    private Vector3 ignitionKeyScale = new Vector3(1f, 1f, 1f);
    private Vector3 keyPosLocal = new Vector3(0.05029682f, 0.1181492f, -0.09794867f);
    private Vector3 keyPosServer = new Vector3(0.0414306f, 0.09218378f, -0.09290992f);
    private Vector3 keyRotLocal = new Vector3(29.962f, -2.063f, -3.106f);
    private Vector3 keyRotServer = new Vector3(29.427f, -5.158f, -53.66f);

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

    public float speedometerFloat;
    public float tachometerFloat;

    // symbols
    private Coroutine dashboardSymbolCoroutine = null!;
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
    // GAUGE CLUSTER END

    public bool brakeLightsOn;
    public GameObject centerMountedLightContainer = null!;
    public MeshRenderer centerMountedLight = null!;
    public Material backLightOffMat = null!;

    public MeshRenderer windshieldMesh = null!;
    public AnimatedObjectTrigger windowWipers = null!;
    public PlayAudioAnimationEvent windowWipersEvent = null!;
    public bool automaticWipersOn;

    private new string[] carTooltips = new string[3]
    {
        "Gas pedal: [W]",
        "Brake pedal: [S]",
        "Boost: [Space]",
    };

    public bool tailgateOpen;

    public bool sunroofOpen;

    public bool cabLightToggled;

    public float playerSteeringWheelAnimFloat;
    public float syncedPlayerSteeringAnim;

    public float shiftSpeed = 25f;

    public bool gaugesOn;
    public bool headlampsOn;

    public bool disableAnimations;
    public bool inIgnitionAnimation;
    public bool accessoryMode;
    public bool twistingKey;

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


    public void OnEnable()
    {
        //ConfigureSubSteps(speedThreshold: 10f, stepsBelowThreshold: 20, stepsAboveThreshold: 10);
        References.pickupController = this;
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
        ragdollPhysicsBody.interpolation = RigidbodyInterpolation.Interpolate;
        windwiperPhysicsBody1.interpolation = RigidbodyInterpolation.Interpolate;
        windwiperPhysicsBody2.interpolation = RigidbodyInterpolation.Interpolate;
        playerPhysicsBody.interpolation = RigidbodyInterpolation.None;
        playerPhysicsBody.freezeRotation = true;
        backDoorOpen = true; // hacky shit
        base.Awake();

        physicsRegion.priority = 1;
        syncedPosition = transform.position;
        syncedRotation = transform.rotation;

        SetTruckStats();
    }


    public new void SetBackDoorOpen(bool open)
    {
        tailgateOpen = open;
    }


    // Additional things to do on start
    public new void Start()
    {
        StartCoroutine(SetCarRainCollisions());
        if (maxBrakeTorque > maxParkingBrakeTorque)
            maxBrakeTorque = maxParkingBrakeTorque;
        carHP = baseCarHP;

        FrontLeftWheel.brakeTorque = maxParkingBrakeTorque;
        FrontRightWheel.brakeTorque = maxParkingBrakeTorque;
        BackLeftWheel.brakeTorque = maxParkingBrakeTorque;
        BackRightWheel.brakeTorque = maxParkingBrakeTorque;

        currentRadioClip = new System.Random(StartOfRound.Instance.randomMapSeed).Next(0, radioClips.Length);
        radioAudio.clip = radioClips[currentRadioClip];
        decals = new DecalProjector[24];

        if (!StartOfRound.Instance.inShipPhase)
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

    public IEnumerator SetCarRainCollisions()
    {
        yield return new WaitForSeconds(4f);

        var particleTriggers = new[]
        {
            GlobalReferences.rainParticles,
            GlobalReferences.rainHitParticles,
            GlobalReferences.stormyRainParticles,
            GlobalReferences.stormyRainHitParticles,
            GlobalReferences.wesleyHurricaneRainParticles,
            GlobalReferences.wesleyHurricaneRainHitParticles,
            GlobalReferences.wesleyHurricaneSandParticles,
            GlobalReferences.wesleyForsakenRainParticles,
            GlobalReferences.wesleyForsakenRainHitParticles
        };

        for (int i = 0; i < particleTriggers.Length; i++)
        {
            if (particleTriggers[i] == null)
            {
                Plugin.Logger.LogDebug("Hauler: Weather particle or Trigger is null!");
                continue;
            }

            var trigger = particleTriggers[i]!.trigger;
            for (int j = 0; j < weatherEffectBlockers.Length; j++)
            {
                int index = trigger.colliderCount + j;
                trigger.SetCollider(index, weatherEffectBlockers[j]);
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

        timeAtLastGearShift = Time.realtimeSinceStartup;
        playerWhoShifted = GameNetworkManager.Instance.localPlayerController;
        int gearAudioIndex = 0;
        if (setGear != (int)CarGearShift.Park)
        {
            gearAudioIndex = setGear > (int)gear ? 1 : 2;
        }
        gear = (CarGearShift)setGear;
        gearStickAudio.PlayOneShot(gearStickAudios[gearAudioIndex]);
        ShiftToGearRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId, setGear, gearAudioIndex);
    }

    [Rpc(SendTo.NotMe, RequireOwnership = false)]
    public void ShiftToGearRpc(int playerId, int setGear, int gearIndex)
    {
        timeAtLastGearShift = Time.realtimeSinceStartup;
        playerWhoShifted = StartOfRound.Instance.allPlayerScripts[playerId];
        gear = (CarGearShift)setGear;
        gearStickAudio.PlayOneShot(gearStickAudios[gearIndex]);
    }


    // --- CAB LIGHTING ---
    public new void SetFrontCabinLightOn(bool setOn)
    {
        if (setOn && cabLightToggled)
            return;

        frontCabinLightContainer.SetActive(setOn);
        frontCabinLightMesh.material = setOn ? headlightsOnMat : headlightsOffMat;
    }

    public void ToggleCabinLights()
    {
        cabLightToggled = !cabLightToggled;
        ToggleCabinLightsLocalClient(setToggle: cabLightToggled, accessoryMode);
        ToggleCabinLightsRpc(setToggle: cabLightToggled, accessoryMode);
    }

    public void ToggleCabinLightsLocalClient(bool setToggle, bool accessoryMode)
    {
        if (setToggle)
        {
            SetFrontCabinLightOn(setOn: false);
            return;
        }
        SetFrontCabinLightOn(setOn: accessoryMode);
    }

    [Rpc(SendTo.NotMe, RequireOwnership = false)]
    public void ToggleCabinLightsRpc(bool setToggle, bool accessoriesOnThisFrame)
    {
        cabLightToggled = setToggle;
        ToggleCabinLightsLocalClient(setToggle: cabLightToggled, accessoriesOnThisFrame);
    }

    // --- TRY IGNITION METHOD ---
    public new void StartTryCarIgnition()
    {
        if (!localPlayerInControl ||
            ignitionStarted)
            return;

        CancelIgnitionCoroutine();
        disableAnimations = true;
        inIgnitionAnimation = true;
        keyIgnitionCoroutine = StartCoroutine(TryIgnition(isLocalDriver: true));
        TryIgnitionRpc(keyIsInIgnition, accessoryMode);
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
            SetKeyIgnitionValues(keyTwisting: false, keyInHand: true, keyInSlot: true);
            if (currentDriver.playerBodyAnimator.GetInteger(CAR_ANIM) == 3 ||
                currentDriver.playerBodyAnimator.GetInteger(CAR_ANIM) == 13)
                currentDriver.playerBodyAnimator.SetInteger(CAR_ANIM, 2);
            else
                currentDriver.playerBodyAnimator.SetInteger(CAR_ANIM, 12);
            int animIndex = currentDriver.playerBodyAnimator.GetInteger(CAR_ANIM);
            ignitionAnimator.SetInteger(IGNITION_ANIM, animIndex);
            yield return new WaitForSeconds(0.02f);
            carKeyAudio.PlayOneShot(twistKey);
            SetKeyIgnitionValues(keyTwisting: true, keyInHand: true, keyInSlot: true);
            yield return new WaitForSeconds(0.1467f);
        }
        else
        {
            currentDriver?.playerBodyAnimator.SetInteger(CAR_ANIM, 2);
            ignitionAnimator.SetInteger(IGNITION_ANIM, 2);
            SetKeyIgnitionValues(keyTwisting: false, keyInHand: true, keyInSlot: false);
            yield return new WaitForSeconds(0.6f);
            carKeyAudio.PlayOneShot(insertKey);
            SetKeyIgnitionValues(keyTwisting: false, keyInHand: true, keyInSlot: true);
            yield return new WaitForSeconds(0.2f);
            carKeyAudio.PlayOneShot(twistKey);
            SetKeyIgnitionValues(keyTwisting: true, keyInHand: true, keyInSlot: true);
            yield return new WaitForSeconds(0.18f);
        }
        SetKeyIgnitionValues(keyTwisting: true, keyInHand: true, keyInSlot: true);
        if (!isLocalDriver) yield break;
        bool shiftInterlock = gear == CarGearShift.Park;
        if (shiftInterlock) PlayIgnitionAudio();
        accessoryMode = true;
        if (!hasSweepedDashboard &&
            dashboardSymbolCoroutine == null)
        {
            dashboardSymbolCoroutine = StartCoroutine(TryDashboardSweep());
        }
        SetKeyIgnitionValues(keyTwisting: true, keyInHand: true, keyInSlot: true);
        SetFrontCabinLightOn(setOn: accessoryMode);
        SetDashboardGaugesOn(on: true);
        TryStartIgnitionRpc(shiftInterlock);
        if (!shiftInterlock) yield break;
        yield return new WaitForSeconds(Random.Range(0.7f, 1.4f));
        if ((float)Random.Range(0, 100) < chanceToStartIgnition)
        {
            inIgnitionAnimation = false;
            accessoryMode = true;
            currentDriver?.playerBodyAnimator.SetInteger(CAR_ANIM, 1);
            SetKeyIgnitionValues(keyTwisting: false, keyInHand: false, keyInSlot: true);
            SetIgnition(started: true, cabLightOn: true);
            SetFrontCabinLightOn(setOn: accessoryMode);
            SetDashboardGaugesOn(on: true);
            CancelIgnitionAnimation(ignitionOn: true, setIgnitionAnim: true);
            StartIgnitionRpc();
        }
        else
        {
            chanceToStartIgnition += 14f;
            chanceToStartIgnition = Mathf.Clamp(chanceToStartIgnition, 0f, 99f);
        }
        yield break;
    }

    [Rpc(SendTo.NotMe, RequireOwnership = false)]
    public void TryIgnitionRpc(bool setKeyInSlot, bool accessoriesActive)
    {
        if (ignitionStarted)
            return;

        CancelIgnitionCoroutine();
        disableAnimations = true;
        inIgnitionAnimation = true;
        SetKeyIgnitionValues(keyTwisting: false, keyInHand: false, keyInSlot: setKeyInSlot);
        if (accessoryMode != accessoriesActive)
        {
            accessoryMode = accessoriesActive;
            SetFrontCabinLightOn(accessoryMode);
        }
        keyIgnitionCoroutine = StartCoroutine(TryIgnition(isLocalDriver: false));
    }

    [Rpc(SendTo.NotMe, RequireOwnership = false)]
    public void TryStartIgnitionRpc(bool shiftInterlock)
    {
        if (shiftInterlock) PlayIgnitionAudio();
        accessoryMode = true;
        if (!hasSweepedDashboard &&
            dashboardSymbolCoroutine == null)
        {
            dashboardSymbolCoroutine = StartCoroutine(TryDashboardSweep());
        }
        SetKeyIgnitionValues(keyTwisting: true, keyInHand: true, keyInSlot: true);
        SetFrontCabinLightOn(setOn: accessoryMode);
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
        if (!localPlayerInControl ||
            ignitionStarted ||
            keyIgnitionCoroutine == null)
            return;

        CancelIgnitionAnimation(ignitionOn: false, setIgnitionAnim: false);
        disableAnimations = true;
        inIgnitionAnimation = false;

        // hopefully fix a bug where the wrong animation can play?
        PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
        if (localPlayer.playerBodyAnimator.GetInteger(CAR_ANIM) == 2 && keyIsInIgnition)
            localPlayer.playerBodyAnimator.SetInteger(CAR_ANIM, 3);
        else if (localPlayer.playerBodyAnimator.GetInteger(CAR_ANIM) == 12 && keyIsInIgnition)
            localPlayer.playerBodyAnimator.SetInteger(CAR_ANIM, 3);
        else
        {
            if (localPlayer.playerBodyAnimator.GetInteger(CAR_ANIM) == 0 && keyIsInIgnition)
                localPlayer.playerBodyAnimator.SetInteger(CAR_ANIM, 13); // an extra transition state to go from sitting --> untwist quickly
            else
                localPlayer.playerBodyAnimator.SetInteger(CAR_ANIM, keyIsInIgnition ? 3 : 0);
        }
        int playerAnimIndex = localPlayer.playerBodyAnimator.GetInteger(CAR_ANIM);
        int ignitionAnimIndex = playerAnimIndex;
        if (playerAnimIndex == 13) ignitionAnimIndex = 3;
        ignitionAnimator.SetInteger(IGNITION_ANIM, ignitionAnimIndex);

        CancelTryIgnitionRpc(keyIsInIgnition, accessoryMode, playerAnimIndex, ignitionAnimIndex);
    }

    [Rpc(SendTo.NotMe, RequireOwnership = false)]
    public void CancelTryIgnitionRpc(bool setKeyInSlot, bool accessoriesActive, int playerAnimIndex, int ignitionAnimIndex)
    {
        CancelIgnitionAnimation(ignitionOn: false, setIgnitionAnim: false);
        disableAnimations = true;
        inIgnitionAnimation = false;

        currentDriver?.playerBodyAnimator.SetInteger(CAR_ANIM, playerAnimIndex);
        ignitionAnimator.SetInteger(IGNITION_ANIM, ignitionAnimIndex);

        // account for netlag when the key is first inserted
        if (setKeyInSlot && !keyIsInIgnition)
        {
            carKeyAudio.PlayOneShot(insertKey);
        }
        SetKeyIgnitionValues(keyTwisting: false, keyInHand: false, keyInSlot: setKeyInSlot);
        SetDashboardGaugesOn(on: accessoriesActive);
        if (accessoryMode != accessoriesActive)
        {
            accessoryMode = accessoriesActive;
            SetFrontCabinLightOn(accessoryMode);
        }
    }


    // --- START IGNITION METHOD ---
    [Rpc(SendTo.NotMe, RequireOwnership = false)]
    public void StartIgnitionRpc()
    {
        disableAnimations = false;
        inIgnitionAnimation = false;
        currentDriver?.playerBodyAnimator.SetInteger(CAR_ANIM, 1);
        SetKeyIgnitionValues(keyTwisting: false, keyInHand: false, keyInSlot: true);
        SetIgnition(started: true, cabLightOn: true);
        SetFrontCabinLightOn(setOn: accessoryMode);
        SetDashboardGaugesOn(on: true);
        CancelIgnitionAnimation(ignitionOn: true, setIgnitionAnim: true);
    }

    public void SetIgnition(bool started, bool cabLightOn)
    {
        SetFrontCabinLightOn(cabLightOn);
        carEngine1AudioActive = started;
        if (started)
        {
            disableAnimations = false;
            inIgnitionAnimation = false;

            startKeyIgnitionTrigger.SetActive(false);
            removeKeyIgnitionTrigger.SetActive(true);

            if (started == ignitionStarted)
                return;

            ignitionStarted = true;
            carExhaustParticle.Play();
            engineAudio1.Stop();
            engineAudio1.PlayOneShot(engineStartSuccessful);
            engineAudio1.clip = engineRun;
            return;
        }
        startKeyIgnitionTrigger.SetActive(true);
        removeKeyIgnitionTrigger.SetActive(false);
        ignitionStarted = false;
        carExhaustParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }


    // --- REMOVE IGNITION METHOD ---
    public new void RemoveKeyFromIgnition()
    {
        if (!localPlayerInControl ||
            !ignitionStarted)
            return;

        if (inIgnitionAnimation)
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
        ignitionAnimator.SetInteger(IGNITION_ANIM, 6);
        yield return new WaitForSeconds(0.28f);
        SetKeyIgnitionValues(keyTwisting: false, keyInHand: true, keyInSlot: false);
        carKeyAudio.PlayOneShot(removeKey);
        SetIgnition(started: false, cabLightOn: false);
        accessoryMode = false;
        hasPlayedIgnitionChime = false;
        CancelDashboardSweep();
        SetDashboardGaugesOn(on: false);
        yield return new WaitForSeconds(0.73f);
        SetKeyIgnitionValues(keyTwisting: false, keyInHand: false, keyInSlot: false);
        keyIgnitionCoroutine = null;
        yield break;
    }

    [Rpc(SendTo.NotMe, RequireOwnership = false)]
    public void RemoveKeyFromIgnitionRpc()
    {
        if (!ignitionStarted)
            return;

        CancelIgnitionCoroutine();
        keyIgnitionCoroutine = StartCoroutine(RemoveKey());
    }


    // --- MISC IGNITION STUFF ---
    public void CancelIgnitionAnimation(bool ignitionOn, bool setIgnitionAnim)
    {
        CancelIgnitionCoroutine();
        carEngine1AudioActive = ignitionOn;
        twistingKey = false;
        keyIsInDriverHand = false;
        if (setIgnitionAnim) ignitionAnimator.SetInteger(IGNITION_ANIM, ignitionOn ? 1 : 0);
    }

    internal void CancelIgnitionCoroutine()
    {
        if (keyIgnitionCoroutine != null)
        {
            StopCoroutine(keyIgnitionCoroutine);
            keyIgnitionCoroutine = null;
        }
    }

    public void SetKeyIgnitionValues(bool keyTwisting, bool keyInHand, bool keyInSlot)
    {
        twistingKey = keyTwisting;
        keyIsInDriverHand = keyInHand;
        keyIsInIgnition = keyInSlot;
    }


    // --- GENERAL REPEAT METHODS ---
    public void SetTriggerHoverTip(InteractTrigger trigger, string tip)
    {
        trigger.hoverTip = tip;
    }


    // --- CANCEL OCCUPANT METHOD ---
    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void CancelSetPlayerInVehicleClientRpc(int playerId)
    {
        if ((int)GameNetworkManager.Instance.localPlayerController.playerClientId != playerId)
            return;

        HUDManager.Instance.DisplayTip("Kicked from vehicle",
            "You have been forcefully kicked to prevent a softlock!");
    }


    // --- DRIVER OCCUPANT METHODS ---
    public void SetDriverInCar()
    {
        SetDriverInCarServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    protected void SetDriverInCarServerRpc(int playerId, RpcParams rpcParams = default)
    {
        PlayerControllerB playerController = StartOfRound.Instance.allPlayerScripts[playerId];
        if (playerController == null ||
            playerController.isPlayerDead ||
            !playerController.isPlayerControlled ||
            currentDriver != null)
        {
            return;
        }
        currentDriver = playerController;
        NetworkObject.ChangeOwnership(rpcParams.Receive.SenderClientId);
        SetDriverInCarOwnerRpc(playerController);
    }

    [Rpc(SendTo.Owner, RequireOwnership = false)]
    protected void SetDriverInCarOwnerRpc(NetworkBehaviourReference playerNetObjRef)
    {
        PlayerUtils.disableAnimationSync = true;
        frontLeftSeat.SetLocalPlayerIntoSeat();
        ActivateControl();
        SetTriggerHoverTip(driverSideDoorTrigger, "Exit : [LMB]");
        startIgnitionTrigger.isBeingHeldByPlayer = false;
        stopIgnitionTrigger.isBeingHeldByPlayer = false;
        CancelIgnitionCoroutine();
        GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetFloat(ANIMATION_SPEED, 0.5f);
        playerSteeringWheelAnimFloat = 0.5f;
        syncedPlayerSteeringAnim = 0.5f;
        if (keyIsInIgnition) GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        else if (ignitionStarted) GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger(CAR_ANIM, 1);
        else GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        int animIndex = GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.GetInteger(CAR_ANIM);
        if (driverSideDoor.boolValue) driverSideDoor.TriggerAnimation(GameNetworkManager.Instance.localPlayerController);
        SetDriverInCarNotOwnerRpc(GameNetworkManager.Instance.localPlayerController, accessoryMode, keyIsInIgnition, ignitionStarted, animIndex);
    }

    [Rpc(SendTo.NotOwner, RequireOwnership = false)]
    public void SetDriverInCarNotOwnerRpc(NetworkBehaviourReference playerNetObjRef, bool accessoriesActive, bool setKeyInSlot, bool engineStarted, int currentAnimIndex)
    {
        if (!playerNetObjRef.TryGet(out PlayerControllerB playerObj))
        {
            Plugin.Logger.LogError("SetDriverIntoCarNotOwnerRpc failed to find player object reference from network behaviour!");
            return;
        }
        currentDriver = playerObj;
        frontLeftSeat.SetPlayerAnimations(playerObj, false);
        startIgnitionTrigger.isBeingHeldByPlayer = false;
        stopIgnitionTrigger.isBeingHeldByPlayer = false;
        CancelIgnitionCoroutine();
        playerObj.playerBodyAnimator.SetFloat(ANIMATION_SPEED, 0.5f);
        playerSteeringWheelAnimFloat = 0.5f;
        syncedPlayerSteeringAnim = 0.5f;
        SetDashboardGaugesOn(on: gaugesOn);
        accessoryMode = accessoriesActive;
        SetFrontCabinLightOn(setOn: accessoriesActive);
        keyIsInIgnition = setKeyInSlot;
        ignitionStarted = engineStarted;
        playerObj.playerBodyAnimator.SetInteger(CAR_ANIM, currentAnimIndex);
    }

    public void OnDriverExitCar()
    {
        if (!IsSpawned ||
            NetworkManager == null ||
            !NetworkManager.IsListening)
        {
            return;
        }
        PlayerUtils.disableAnimationSync = false;
        localPlayerInControl = false;
        SetTriggerHoverTip(driverSideDoorTrigger, "Use door : [LMB]");
        disableAnimations = !ignitionStarted;
        inIgnitionAnimation = false;
        startIgnitionTrigger.isBeingHeldByPlayer = false;
        stopIgnitionTrigger.isBeingHeldByPlayer = false;
        GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        GlobalUtilities.ResetHUDToolTips(GameNetworkManager.Instance.localPlayerController);
        if (currentDriver != GameNetworkManager.Instance.localPlayerController)
        {
            HUDManager.Instance.DisplayTip("Err?",
                "This state should not occur! aborting!");
            return;
        }
        DisableControl();
        CancelIgnitionAnimation(ignitionOn: ignitionStarted, setIgnitionAnim: true);
        SetIgnition(started: ignitionStarted, cabLightOn: accessoryMode);
        chanceToStartIgnition = 10f;
        syncedPosition = transform.position;
        syncedRotation = transform.rotation;
        OnDriverExitCarRpc(
            GameNetworkManager.Instance.localPlayerController,
            syncedPosition,
            syncedRotation,
            keyIsInIgnition,
            ignitionStarted,
            accessoryMode);
    }

    [Rpc(SendTo.NotMe, RequireOwnership = false)]
    public void OnDriverExitCarRpc(NetworkBehaviourReference playerNetObjRef, Vector3 carLocation, Quaternion carRotation, bool setKeyInSlot, bool engineStarted, bool accessoriesActive)
    {
        if (IsServer) NetworkObject.ChangeOwnership(NetworkManager.ServerClientId);
        if (!playerNetObjRef.TryGet(out PlayerControllerB playerObj))
        {
            Plugin.Logger.LogError("OnDriverExitRpc failed to find player object reference from network behaviour!");
            return;
        }
        syncedPosition = carLocation;
        syncedRotation = carRotation;
        drivePedalPressed = false;
        brakePedalPressed = false;
        currentDriver = null;
        frontLeftSeat.ReturnPlayerAnimations(playerObj, false);
        keyIsInIgnition = setKeyInSlot;
        ignitionStarted = engineStarted;
        if (ignitionStarted && !carExhaustParticle.isEmitting) carExhaustParticle.Play();
        else if (!ignitionStarted && carExhaustParticle.isEmitting) carExhaustParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        disableAnimations = !ignitionStarted;
        inIgnitionAnimation = false;
        startIgnitionTrigger.isBeingHeldByPlayer = false;
        stopIgnitionTrigger.isBeingHeldByPlayer = false;
        CancelIgnitionAnimation(ignitionOn: ignitionStarted, setIgnitionAnim: true);
        SetIgnition(started: ignitionStarted, cabLightOn: accessoriesActive);
        SetDashboardGaugesOn(on: accessoriesActive);
        chanceToStartIgnition = 10f;
    }

    public new void ExitDriverSideSeat()
    {
        if (!localPlayerInControl)
            return;

        GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        int exitPoint = CanExitCar(exitPoints: driverSideExitPoints);
        if (exitPoint == 0) if (!driverSideDoor.boolValue) driverSideDoor.TriggerAnimation(GameNetworkManager.Instance.localPlayerController);
        if (exitPoint != -1)
        {
            GameNetworkManager.Instance.localPlayerController.TeleportPlayer(driverSideExitPoints[exitPoint].position);
            return;
        }
        GameNetworkManager.Instance.localPlayerController.TeleportPlayer(driverSideExitPoints[1].position);
    }


    // --- PASSENGER OCCUPANT METHODS ---
    public void SetPassengerInCar()
    {
        SetPassengerInCarServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    protected void SetPassengerInCarServerRpc(int playerId, RpcParams rpcParams = default)
    {
        PlayerControllerB playerController = StartOfRound.Instance.allPlayerScripts[playerId];
        if (playerController == null ||
            playerController.isPlayerDead ||
            !playerController.isPlayerControlled ||
            currentPassenger != null)
        {
            return;
        }
        currentPassenger = playerController;
        SetPassengerInCarRpc(playerController);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    protected void SetPassengerInCarRpc(NetworkBehaviourReference playerNetObjRef)
    {
        if (!playerNetObjRef.TryGet(out PlayerControllerB playerObj))
        {
            Plugin.Logger.LogError("SetPassengerInCarRpc failed to find player object reference from network behaviour!");
            return;
        }
        if (playerObj == GameNetworkManager.Instance.localPlayerController)
        {
            PlayerUtils.disableAnimationSync = true;
            frontRightSeat.SetLocalPlayerIntoSeat();
            localPlayerInPassengerSeat = true;
            SetTriggerHoverTip(passengerSideDoorTrigger, "Exit : [LMB]");
            if (passengerSideDoor.boolValue) passengerSideDoor.TriggerAnimation(GameNetworkManager.Instance.localPlayerController);
        }
        else
        {
            frontRightSeat.SetPlayerAnimations(playerObj, false);
        }
        currentPassenger = playerObj;
        playerObj.playerBodyAnimator.SetFloat(ANIMATION_SPEED, 0.5f);
    }

    public void OnPassengerExitCar()
    {
        if (!IsSpawned ||
            NetworkManager == null ||
            !NetworkManager.IsListening)
        {
            return;
        }
        PlayerUtils.disableAnimationSync = false;
        localPlayerInPassengerSeat = false;
        SetTriggerHoverTip(passengerSideDoorTrigger, "Use door : [LMB]");
        GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        currentPassenger = null;
        OnPassengerExitCarRpc(GameNetworkManager.Instance.localPlayerController, GameNetworkManager.Instance.localPlayerController.transform.position);
    }

    [Rpc(SendTo.NotMe, RequireOwnership = false)]
    public void OnPassengerExitCarRpc(NetworkBehaviourReference playerNetObjRef, Vector3 exitPoint)
    {
        if (!playerNetObjRef.TryGet(out PlayerControllerB playerObj))
        {
            Plugin.Logger.LogError("OnPassengerExitCarRpc failed to find player object reference from network behaviour!");
            return;
        }
        frontRightSeat.ReturnPlayerAnimations(playerObj, false);
        playerObj.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        playerObj.TeleportPlayer(exitPoint, false, 0f, false, true);
        currentPassenger = null;
    }

    public new void ExitPassengerSideSeat()
    {
        if (!localPlayerInPassengerSeat)
            return;

        GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        int exitPoint = CanExitCar(exitPoints: passengerSideExitPoints);
        if (exitPoint == 0) if (!passengerSideDoor.boolValue) passengerSideDoor.TriggerAnimation(GameNetworkManager.Instance.localPlayerController);
        if (exitPoint != -1)
        {
            GameNetworkManager.Instance.localPlayerController.TeleportPlayer(passengerSideExitPoints[exitPoint].position);
            return;
        }
        GameNetworkManager.Instance.localPlayerController.TeleportPlayer(passengerSideExitPoints[1].position);
    }


    // --- BACK LEFT PASSENGER OCCUPANT METHODS ---
    public void SetBackLeftPassengerInCar()
    {
        SetBackLeftPassengerInCarServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    protected void SetBackLeftPassengerInCarServerRpc(int playerId, RpcParams rpcParams = default)
    {
        PlayerControllerB playerController = StartOfRound.Instance.allPlayerScripts[playerId];
        if (playerController == null ||
            playerController.isPlayerDead ||
            !playerController.isPlayerControlled ||
            currentBackLeftPassenger != null)
        {
            return;
        }
        currentBackLeftPassenger = playerController;
        SetBackLeftPassengerInCarRpc(playerController);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    protected void SetBackLeftPassengerInCarRpc(NetworkBehaviourReference playerNetObjRef)
    {
        if (!playerNetObjRef.TryGet(out PlayerControllerB playerObj))
        {
            Plugin.Logger.LogError("SetBackLeftPassengerInCarRpc failed to find player object reference from network behaviour!");
            return;
        }
        if (playerObj == GameNetworkManager.Instance.localPlayerController)
        {
            PlayerUtils.disableAnimationSync = true;
            backLeftSeat.SetLocalPlayerIntoSeat();
            localPlayerInBackLeftPassengerSeat = true;
            SetTriggerHoverTip(backLeftDoorTrigger, "Exit : [LMB]");
            if (backLeftDoor.boolValue) backLeftDoor.TriggerAnimation(GameNetworkManager.Instance.localPlayerController);
        }
        else
        {
            backLeftSeat.SetPlayerAnimations(playerObj, false);
        }
        currentBackLeftPassenger = playerObj;
        playerObj.playerBodyAnimator.SetFloat(ANIMATION_SPEED, 0.5f);
    }

    public void OnBackLeftPassengerExitCar()
    {
        if (!IsSpawned ||
            NetworkManager == null ||
            !NetworkManager.IsListening)
        {
            return;
        }
        PlayerUtils.disableAnimationSync = false;
        localPlayerInBackLeftPassengerSeat = false;
        SetTriggerHoverTip(backLeftDoorTrigger, "Use door : [LMB]");
        GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        currentBackLeftPassenger = null;
        OnBackLeftPassengerExitCarRpc(GameNetworkManager.Instance.localPlayerController, GameNetworkManager.Instance.localPlayerController.transform.position);
    }

    [Rpc(SendTo.NotMe, RequireOwnership = false)]
    public void OnBackLeftPassengerExitCarRpc(NetworkBehaviourReference playerNetObjRef, Vector3 exitPoint)
    {
        if (!playerNetObjRef.TryGet(out PlayerControllerB playerObj))
        {
            Plugin.Logger.LogError("OnBackLeftPassengerExitCarRpc failed to find player object reference from network behaviour!");
            return;
        }
        backLeftSeat.ReturnPlayerAnimations(playerObj, false);
        playerObj.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        playerObj.TeleportPlayer(exitPoint, false, 0f, false, true);
        currentBackLeftPassenger = null;
    }

    public void ExitBackLeftPassengerSideSeat()
    {
        if (!localPlayerInBackLeftPassengerSeat)
            return;

        GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        int exitPoint = CanExitCar(exitPoints: backLeftExitPoints);
        if (exitPoint == 0) if (!backLeftDoor.boolValue) backLeftDoor.TriggerAnimation(GameNetworkManager.Instance.localPlayerController);
        if (exitPoint != -1)
        {
            GameNetworkManager.Instance.localPlayerController.TeleportPlayer(backLeftExitPoints[exitPoint].position);
            return;
        }
        GameNetworkManager.Instance.localPlayerController.TeleportPlayer(backLeftExitPoints[1].position);
    }


    // --- MIDDLE PASSENGER OCCUPANT METHODS ---
    public void SetMiddlePassengerInCar()
    {
        SetMiddlePassengerInCarServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    protected void SetMiddlePassengerInCarServerRpc(int playerId, RpcParams rpcParams = default)
    {
        PlayerControllerB playerController = StartOfRound.Instance.allPlayerScripts[playerId];
        if (playerController == null ||
            playerController.isPlayerDead ||
            !playerController.isPlayerControlled ||
            currentMiddlePassenger != null)
        {
            return;
        }
        currentMiddlePassenger = playerController;
        SetMiddlePassengerInCarRpc(playerController);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    protected void SetMiddlePassengerInCarRpc(NetworkBehaviourReference playerNetObjRef)
    {
        if (!playerNetObjRef.TryGet(out PlayerControllerB playerObj))
        {
            Plugin.Logger.LogError("SetMiddlePassengerInCarRpc failed to find player object reference from network behaviour!");
            return;
        }
        if (playerObj == GameNetworkManager.Instance.localPlayerController)
        {
            PlayerUtils.disableAnimationSync = true;
            backSeat.SetLocalPlayerIntoSeat();
            localPlayerInMiddlePassengerSeat = true;
            SetTriggerHoverTip(backLeftDoorTrigger, "Exit : [LMB]");
            SetTriggerHoverTip(backRightDoorTrigger, "Exit : [LMB]");
            if (backLeftDoor.boolValue) backLeftDoor.TriggerAnimation(GameNetworkManager.Instance.localPlayerController);
            if (backRightDoor.boolValue) backRightDoor.TriggerAnimation(GameNetworkManager.Instance.localPlayerController);
        }
        else
        {
            backSeat.SetPlayerAnimations(playerObj, false);
        }
        currentMiddlePassenger = playerObj;
        playerObj.playerBodyAnimator.SetFloat(ANIMATION_SPEED, 0.5f);
    }

    public void OnMiddlePassengerExitCar()
    {
        if (!IsSpawned ||
            NetworkManager == null ||
            !NetworkManager.IsListening)
        {
            return;
        }
        PlayerUtils.disableAnimationSync = false;
        localPlayerInMiddlePassengerSeat = false;
        SetTriggerHoverTip(backLeftDoorTrigger, "Use door : [LMB]");
        SetTriggerHoverTip(backRightDoorTrigger, "Use door : [LMB]");
        GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        currentMiddlePassenger = null;
        OnMiddlePassengerExitCarRpc(GameNetworkManager.Instance.localPlayerController, GameNetworkManager.Instance.localPlayerController.transform.position);
    }

    [Rpc(SendTo.NotMe, RequireOwnership = false)]
    public void OnMiddlePassengerExitCarRpc(NetworkBehaviourReference playerNetObjRef, Vector3 exitPoint)
    {
        if (!playerNetObjRef.TryGet(out PlayerControllerB playerObj))
        {
            Plugin.Logger.LogError("OnMiddlePassengerExitCarRpc failed to find player object reference from network behaviour!");
            return;
        }
        backSeat.ReturnPlayerAnimations(playerObj, false);
        playerObj.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        playerObj.TeleportPlayer(exitPoint, false, 0f, false, true);
        currentMiddlePassenger = null;
    }

    public void ExitMiddlePassengerSideSeat(bool offSide)
    {
        if (!localPlayerInMiddlePassengerSeat)
            return;

        GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        Transform[] exitSidePoints = offSide ? backRightExitPoints : backLeftExitPoints;
        int exitPoint = CanExitCar(exitPoints: exitSidePoints);
        AnimatedObjectTrigger door = offSide ? backRightDoor : backLeftDoor;
        if (exitPoint == 0) if (!door.boolValue) door.TriggerAnimation(GameNetworkManager.Instance.localPlayerController);
        if (exitPoint != -1)
        {
            GameNetworkManager.Instance.localPlayerController.TeleportPlayer(exitSidePoints[exitPoint].position);
            return;
        }
        GameNetworkManager.Instance.localPlayerController.TeleportPlayer(exitSidePoints[1].position);
    }


    // --- BACK RIGHT PASSENGER OCCUPANT METHODS ---
    public void SetBackRightPassengerInCar()
    {
        SetBackRightPassengerInCarServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    protected void SetBackRightPassengerInCarServerRpc(int playerId, RpcParams rpcParams = default)
    {
        PlayerControllerB playerController = StartOfRound.Instance.allPlayerScripts[playerId];
        if (playerController == null ||
            playerController.isPlayerDead ||
            !playerController.isPlayerControlled ||
            currentBackRightPassenger != null)
        {
            return;
        }
        currentBackRightPassenger = playerController;
        SetBackRightPassengerInCarRpc(playerController);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    protected void SetBackRightPassengerInCarRpc(NetworkBehaviourReference playerNetObjRef)
    {
        if (!playerNetObjRef.TryGet(out PlayerControllerB playerObj))
        {
            Plugin.Logger.LogError("SetBackRightPassengerInCarRpc failed to find player object reference from network behaviour!");
            return;
        }
        if (playerObj == GameNetworkManager.Instance.localPlayerController)
        {
            PlayerUtils.disableAnimationSync = true;
            backRightSeat.SetLocalPlayerIntoSeat();
            localPlayerInBackRightPassengerSeat = true;
            SetTriggerHoverTip(backRightDoorTrigger, "Exit : [LMB]");
            if (backRightDoor.boolValue) backRightDoor.TriggerAnimation(GameNetworkManager.Instance.localPlayerController);
        }
        else
        {
            backRightSeat.SetPlayerAnimations(playerObj, false);
        }
        currentBackRightPassenger = playerObj;
        playerObj.playerBodyAnimator.SetFloat(ANIMATION_SPEED, 0.5f);
    }

    public void OnBackRightPassengerExitCar()
    {
        if (!IsSpawned ||
            NetworkManager == null ||
            !NetworkManager.IsListening)
        {
            return;
        }
        PlayerUtils.disableAnimationSync = false;
        localPlayerInBackRightPassengerSeat = false;
        SetTriggerHoverTip(backRightDoorTrigger, "Use door : [LMB]");
        GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        currentBackRightPassenger = null;
        OnBackRightPassengerExitCarRpc(GameNetworkManager.Instance.localPlayerController, GameNetworkManager.Instance.localPlayerController.transform.position);
    }

    [Rpc(SendTo.NotMe, RequireOwnership = false)]
    public void OnBackRightPassengerExitCarRpc(NetworkBehaviourReference playerNetObjRef, Vector3 exitPoint)
    {
        if (!playerNetObjRef.TryGet(out PlayerControllerB playerObj))
        {
            Plugin.Logger.LogError("OnBackRightPassengerExitCarRpc failed to find player object reference from network behaviour!");
            return;
        }
        backRightSeat.ReturnPlayerAnimations(playerObj, false);
        playerObj.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        playerObj.TeleportPlayer(exitPoint, false, 0f, false, true);
        currentBackRightPassenger = null;
    }

    public void ExitBackRightPassengerSideSeat()
    {
        if (!localPlayerInBackRightPassengerSeat)
            return;

        GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger(CAR_ANIM, 0);
        int exitPoint = CanExitCar(exitPoints: backRightExitPoints);
        if (exitPoint == 0) if (!backRightDoor.boolValue) backRightDoor.TriggerAnimation(GameNetworkManager.Instance.localPlayerController);
        if (exitPoint != -1)
        {
            GameNetworkManager.Instance.localPlayerController.TeleportPlayer(backRightExitPoints[exitPoint].position);
            return;
        }
        GameNetworkManager.Instance.localPlayerController.TeleportPlayer(backRightExitPoints[1].position);
    }


    // --- LEAVE OCCUPANT MID-GAME METHODS ---
    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void OnDriverLeaveGameServerRpc(int playerId)
    {
        PlayerControllerB playerController = StartOfRound.Instance.allPlayerScripts[playerId];
        if (playerController == null)
        {
            return;
        }
        NetworkObject.ChangeOwnership(NetworkManager.ServerClientId);
        OnDriverLeave(playerController, ignitionStarted, keyIsInIgnition, accessoryMode);
        OnDriverLeaveGameRpc(playerId, syncedPosition, syncedRotation, ignitionStarted, keyIsInIgnition, accessoryMode);
    }

    public void OnDriverLeave(PlayerControllerB playerController, bool setIgnitionState, bool setKeyInSlot, bool preIgnition)
    {
        drivePedalPressed = false;
        brakePedalPressed = false;
        currentDriver = null;

        accessoryMode = preIgnition;
        SetDashboardGaugesOn(on: accessoryMode);
        keyIsInIgnition = setKeyInSlot;
        ignitionStarted = setIgnitionState;

        if (ignitionStarted && !carExhaustParticle.isEmitting) carExhaustParticle.Play();
        else if (!ignitionStarted && carExhaustParticle.isEmitting) carExhaustParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        disableAnimations = !ignitionStarted;
        inIgnitionAnimation = false;

        startIgnitionTrigger.isBeingHeldByPlayer = false;
        stopIgnitionTrigger.isBeingHeldByPlayer = false;

        frontLeftSeat.ReturnPlayerAnimations(playerController, false);
        playerController.TeleportPlayer(StartOfRound.Instance.notSpawnedPosition.position, false, 0f, false, true);

        CancelIgnitionAnimation(ignitionOn: ignitionStarted, setIgnitionAnim: true);
        SetIgnition(started: ignitionStarted, cabLightOn: accessoryMode);
    }

    [Rpc(SendTo.NotServer, RequireOwnership = false)]
    public void OnDriverLeaveGameRpc(int playerId, Vector3 carLocation, Quaternion carRotation, bool setIgnitionState, bool setKeyInSlot, bool preIgnition)
    {
        PlayerControllerB playerController = StartOfRound.Instance.allPlayerScripts[playerId];
        if (playerController == null)
        {
            return;
        }
        syncedPosition = carLocation;
        syncedRotation = carRotation;
        OnDriverLeave(playerController, setIgnitionState, setKeyInSlot, preIgnition);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void OnPassengerLeaveGameRpc(int playerId)
    {
        PlayerControllerB playerController = StartOfRound.Instance.allPlayerScripts[playerId];
        if (playerController == null)
        {
            return;
        }
        frontRightSeat.ReturnPlayerAnimations(playerController, false);
        playerController.TeleportPlayer(StartOfRound.Instance.notSpawnedPosition.position, false, 0f, false, true);
        currentPassenger = null!;
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void OnBackLeftPassengerLeaveGameRpc(int playerId)
    {
        PlayerControllerB playerController = StartOfRound.Instance.allPlayerScripts[playerId];
        if (playerController == null)
        {
            return;
        }
        backLeftSeat.ReturnPlayerAnimations(playerController, false);
        playerController.TeleportPlayer(StartOfRound.Instance.notSpawnedPosition.position, false, 0f, false, true);
        currentBackLeftPassenger = null!;
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void OnMiddlePassengerLeaveGameRpc(int playerId)
    {
        PlayerControllerB playerController = StartOfRound.Instance.allPlayerScripts[playerId];
        if (playerController == null)
        {
            return;
        }
        backSeat.ReturnPlayerAnimations(playerController, false);
        playerController.TeleportPlayer(StartOfRound.Instance.notSpawnedPosition.position, false, 0f, false, true);
        currentMiddlePassenger = null!;
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void OnBackRightPassengerLeaveGameRpc(int playerId)
    {
        PlayerControllerB playerController = StartOfRound.Instance.allPlayerScripts[playerId];
        if (playerController == null)
        {
            return;
        }
        backRightSeat.ReturnPlayerAnimations(playerController, false);
        playerController.TeleportPlayer(StartOfRound.Instance.notSpawnedPosition.position, false, 0f, false, true);
        currentBackRightPassenger = null!;
    }


    // --- OCCUPANT EXITING METHODS ---
    private int CanExitCar(Transform[] exitPoints)
    {
        for (int j = 0; j < exitPoints.Length; j++)
        {
            if (!CheckExitPointInvalid(GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position, exitPoints[j].position, exitCarLayerMask, QueryTriggerInteraction.Ignore))
            {
                return j;
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
        InputSystem.actions.FindAction("Jump", false).performed += DoTurboBoost;
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
        InputSystem.actions.FindAction("Jump", false).performed -= DoTurboBoost;
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

    public new void GetVehicleInput()
    {
        PlayerControllerB localDriver = GameNetworkManager.Instance.localPlayerController;
        if (localDriver == null)
            return;

        if (localDriver.isTypingChat ||
            localDriver.quickMenuManager.isMenuOpen)
            return;

        SyncVehicleInput();

        moveInputVector = IngamePlayerSettings.Instance.playerInput.actions.FindAction("Move", false).ReadValue<Vector2>();
        moveInputVector.x = Mathf.Round(moveInputVector.x);
        moveInputVector.y = Mathf.Round(moveInputVector.y);

        steeringAnimValue = moveInputVector.x;

        if (steeringAnimValue == 0)
            steeringWheelAnimValue = NormaliseFloat(Mathf.Lerp(steeringWheelAnimValue, 0, steeringReturnSpeed * Time.deltaTime));
        else
            steeringWheelAnimValue = NormaliseFloat(Mathf.Lerp(steeringWheelAnimValue, steeringAnimValue, steeringSpeed * Time.deltaTime));

        drivePedalPressed = moveInputVector.y > 0.1f && shiftGearCoroutine == null;
        brakePedalPressed = moveInputVector.y < -0.1f;
    }

    private void SyncVehicleInput()
    {
        if (syncedMoveInputVector != moveInputVector ||
            (syncedDrivePedalPressed != drivePedalPressed || syncedBrakePedalPressed != brakePedalPressed))
        {
            syncedMoveInputVector = moveInputVector;
            syncedDrivePedalPressed = drivePedalPressed;
            syncedBrakePedalPressed = brakePedalPressed;
            SyncPlayerInputsRpc(moveInputVector, drivePedalPressed, brakePedalPressed);
        }
    }

    [Rpc(SendTo.NotOwner, RequireOwnership = false)]
    public void SyncPlayerInputsRpc(Vector2 playerInput, bool gasPressed, bool brakePressed)
    {
        syncedMoveInputVector = playerInput;
        syncedDrivePedalPressed = gasPressed;
        syncedBrakePedalPressed = brakePressed;
    }

    // --- PLAYER-VEHICLE COLLISION --- 
    // None of this is needed, with the way we have collisions for players-vehicles setup, we don't need to toggle layers anymore, and 
    // we don't have to worry about buggy host-interactions anymore with our setup.
    public new void EnableVehicleCollisionForAllPlayers()
    {
        Plugin.Logger.LogDebug("Hauler: Attempted to enable collision, but this is unnecessary!");
        return;
    }

    public new void DisableVehicleCollisionForAllPlayers()
    {
        Plugin.Logger.LogDebug("Hauler: Attempted to disable collision, but this is unnecessary!");
        return;
    }

    public new void SetVehicleCollisionForPlayer(bool setEnabled, PlayerControllerB player)
    {
        Plugin.Logger.LogDebug("Hauler: Attempted to set collision, but this is unnecessary!");
        return;
    }


    // --- VEHICLE STATS --- 
    private void SetTruckStats()
    {
        gear = CarGearShift.Park;
        MaxEngineRPM = 4500f;
        MinEngineRPM = 900f;
        engineIntensityPercentage = 180f;
        EngineTorque = 2800f;
        engineReversePower = -6000f;
        carAcceleration = 400f;
        carDeacceleration = 2900f;
        reverseCarAcceleration = 2800f;
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
        mainRigidbody.centerOfMass = new Vector3(0f, -0.2f, 0.7f); // more biased towards the front, loose rear end
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

        movingAverageLength = 23;
        carHitPlayerForceFraction = 30f;
        carReactToPlayerHitMultiplier = 2850f;

        jumpForce = 5250f;
        pushForceMultiplier = 78f;
        pushVerticalOffsetAmount = 1.1f;
        steeringWheelTurnSpeed = 4.35f;
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
    public new void SetWheelFriction()
    {
        WheelFrictionCurve forwardFrictionCurve = new WheelFrictionCurve
        {
            extremumSlip = 0.6f,
            extremumValue = 0.9f,
            asymptoteSlip = 0.78f,
            asymptoteValue = 0.66f,
            stiffness = 1.05f,
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
        SetCarTyreSlipEffects();
        SetCarDashboard();
        SetCarLighting();
        SetCarAutomaticShifter();
        SetCarAudioEffects();
        SetCarKeyEffects();

        if (IsOwner)
        {
            SyncCarEffects();
            return;
        }
        steeringWheelAnimFloat = Mathf.Lerp(steeringWheelAnimFloat, syncedSteeringWheelRotation, 6f * Time.deltaTime);
        playerSteeringWheelAnimFloat = Mathf.MoveTowards(playerSteeringWheelAnimFloat, syncedPlayerSteeringAnim, steeringWheelTurnSpeed * Time.deltaTime / 6f);
    }


    // --- MISC EFFECTS ---
    // tyre skid effects
    public void SetCarTyreSlipEffects()
    {
        if (IsOwner)
        {
            float vehicleSpeed = Vector3.Dot(Vector3.Normalize(mainRigidbody.velocity * 1000f), transform.forward);
            float wheelSpeed = Mathf.Abs(backWheelRPM);
            bool audioActive = false;

            if (backWheelsGrounded)
            {
                bool forwardSlipping = currentMotorTorque > 800f && Mathf.Abs(forwardsSlip) > 0.35f;
                bool sidewaySlipping = currentMotorTorque > 800f && Mathf.Abs(sidewaysSlip) > 0.2f;
                if ((forwardSlipping || sidewaySlipping) && wheelSpeed >= 350f)
                {
                    vehicleSpeed = Mathf.Max(vehicleSpeed, 0.8f);
                    audioActive = true;

                    if (averageVelocity.magnitude > 8f && !tireSparks.isPlaying)
                        tireSparks.Play(true);
                }
                else
                {
                    audioActive = false;
                    if (tireSparks.isEmitting)
                        tireSparks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
            else
            {
                audioActive = false;
                if (tireSparks.isEmitting)
                    tireSparks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            SetVehicleAudioProperties(skiddingAudio, audioActive, 0f, vehicleSpeed, 3f, true, 1f);
            if (Mathf.Abs(syncedTyreStress - vehicleSpeed) > 0.02f || syncedTyreSlipping != audioActive)
            {
                syncedTyreStress = vehicleSpeed;
                syncedTyreSlipping = audioActive;
                SetCarTyreStressRpc(vehicleSpeed, audioActive);
            }
            return;
        }

        if (syncedTyreSlipping && averageVelocity.magnitude > 8f && !tireSparks.isPlaying)
        {
            tireSparks.Play(true);
        }
        else if (!syncedTyreSlipping && tireSparks.isEmitting)
        {
            tireSparks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        SetVehicleAudioProperties(skiddingAudio, syncedTyreSlipping, 0f, syncedTyreStress, 3f, true, 1f);
    }

    [Rpc(SendTo.NotOwner, RequireOwnership = false)]
    public void SetCarTyreStressRpc(float stress, bool wheelSkidding)
    {
        syncedTyreStress = stress;
        syncedTyreSlipping = wheelSkidding;
    }

    private void SetCarKeyEffects()
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

            keyParent = localPlayerInControl ? currentDriver.localItemHolder : currentDriver.serverItemHolder;
            posOffset = localPlayerInControl ? keyPosLocal : keyPosServer;
            rotOffset = localPlayerInControl ? keyRotLocal : keyRotServer;

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

    private IEnumerator TryDashboardSweep()
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
        dashboardSymbolCoroutine = null!;
        yield break;
    }

    private void CancelDashboardSweep()
    {
        if (dashboardSymbolCoroutine != null)
        {
            StopCoroutine(dashboardSymbolCoroutine);
            dashboardSymbolCoroutine = null!;
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
            bool tryingIgnition = engineAudio1.volume > 0.1f && twistingKey;
            speedometerFloat = Mathf.Lerp(speedometerFloat, 0f, 6f * Time.deltaTime);
            tachometerFloat = Mathf.Lerp(tachometerFloat, tryingIgnition ? 0.075f : 0f, 4.5f * Time.deltaTime);
        }
        SetDashboardSymbols();
        SetRadioScreen();
        if (!accessoryMode)
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
        if (!accessoryMode && !radioOn)
        {
            radioScreen.text = null!;
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

    private void SetCarAutomaticShifter()
    {
        switch (gear)
        {
            case CarGearShift.Park:
                {
                    gearStickAnimValue = Mathf.MoveTowards(gearStickAnimValue, 0f, shiftSpeed * Time.deltaTime * (Time.realtimeSinceStartup - timeAtLastGearShift));
                    break;
                }
            case CarGearShift.Reverse:
                {
                    gearStickAnimValue = Mathf.MoveTowards(gearStickAnimValue, 0.5f, shiftSpeed * Time.deltaTime * (Time.realtimeSinceStartup - timeAtLastGearShift));
                    break;
                }
            case CarGearShift.Drive:
                {
                    gearStickAnimValue = Mathf.MoveTowards(gearStickAnimValue, 1f, shiftSpeed * Time.deltaTime * (Time.realtimeSinceStartup - timeAtLastGearShift));
                    break;
                }
        }
        gearStickAnimator.SetFloat("currentGear", Mathf.Clamp(gearStickAnimValue, 0f, 1f));
    }


    // --- MISC SYNC METHODS ---
    public void SyncCarEffects()
    {
        if (syncEffectsInterval > 0.045f)
        {
            if (syncedSteeringWheelRotation != steeringWheelAnimFloat)
            {
                syncEffectsInterval = 0f;
                syncedSteeringWheelRotation = steeringWheelAnimFloat;
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

    [Rpc(SendTo.NotOwner, RequireOwnership = false)]
    public void SyncCarEffectsRpc(float wheelRotation, float playerSteering)
    {
        syncedSteeringWheelRotation = wheelRotation;
        syncedPlayerSteeringAnim = playerSteering;
    }


    // --- VEHICLE AUDIO METHODS ---
    /// <summary>
    ///  Available from EnemySoundFixes, licensed under GNU General Public License.
    ///  Source: https://github.com/ButteryStancakes/EnemySoundFixes/blob/master/Patches/CruiserPatches.cs
    /// </summary>
    private new void SetVehicleAudioProperties(AudioSource audio, bool audioActive, float lowest, float highest, float lerpSpeed, bool useVolumeInsteadOfPitch = false, float onVolume = 1f)
    {
        if (audioActive && ((audio == rollingAudio || audio == skiddingAudio) && (magnetedToShip || allWheelsAirborne)))
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
        if (engineAudio1.volume > 0.3f && engineAudio1.isPlaying && (Time.realtimeSinceStartup - timeAtLastEngineAudioPing > 2f))
        {
            timeAtLastEngineAudioPing = Time.realtimeSinceStartup;
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

            if (Time.realtimeSinceStartup - timeAtLastHornPing > 2f)
            {
                timeAtLastHornPing = Time.realtimeSinceStartup;
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
    public void SyncRadioTimeRpc(float songTime)
    {
        currentSongTime = songTime;
        SetRadioTime();
    }

    public void SetRadioTime()
    {
        if (radioAudio.clip == null || !radioOn) return;
        radioAudio.time = Mathf.Clamp(currentSongTime % radioAudio.clip.length, 0f, radioAudio.clip.length);
    }


    // --- RADIO CHANNEL ---
    public new void ChangeRadioStation()
    {
        if (radioClips.Length == 0)
        {
            Plugin.Logger.LogWarning("Hauler: No music found! are you using CruiserTunes to remove the original tracks?");
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
        SetRadioStationRpc(currentRadioClip, radioSignalQuality, radioSignalDecreaseThreshold);
    }

    [Rpc(SendTo.NotMe, RequireOwnership = false)]
    public void SetRadioStationRpc(int radioStation, float signalQuality, float signalDecrease)
    {
        if (radioClips.Length == 0)
        {
            Plugin.Logger.LogWarning("Hauler: No music found! are you using CruiserTunes to remove the original tracks?");
            return;
        }
        currentRadioClip = radioStation;
        radioSignalQuality = signalQuality;
        radioSignalDecreaseThreshold = signalDecrease;
        SetRadioOnLocalClient(on: true, setClip: true);
    }


    // --- RADIO TOGGLE ---
    public new void SwitchRadio()
    {
        if (radioClips.Length == 0)
        {
            Plugin.Logger.LogWarning("Hauler: No music found! are you using CruiserTunes to remove the original tracks?");
            return;
        }
        SetRadioOnLocalClient(on: !radioOn, setClip: false);
        SetRadioRpc(radioOn, currentRadioClip, radioSignalQuality, radioSignalDecreaseThreshold);
    }

    [Rpc(SendTo.NotMe, RequireOwnership = false)]
    public void SetRadioRpc(bool on, int radioStation, float signalQuality, float signalDecrease)
    {
        if (radioClips.Length == 0)
        {
            Plugin.Logger.LogWarning("Hauler: No music found! are you using CruiserTunes to remove the original tracks?");
            return;
        }
        currentRadioClip = radioStation;
        radioSignalQuality = signalQuality;
        radioSignalDecreaseThreshold = signalDecrease;
        SetRadioOnLocalClient(on: on, setClip: false);
    }


    // --- RADIO VALUES ---
    public new void SetRadioValues()
    {
        if (!radioOn || radioAudio.clip == null)
        {
            if (radioAudio.isPlaying) radioAudio.Stop();
            if (radioInterference.isPlaying) radioInterference.Stop();
            if (currentSongTime > 0f) currentSongTime = 0f;
            return;
        }
        if (IsServer)
        {
            currentSongTime = radioAudio.time;
            if (Time.realtimeSinceStartup - timeLastSyncedRadio > 1f)
            {
                timeLastSyncedRadio = Time.realtimeSinceStartup;
                SyncRadioTimeRpc(currentSongTime);
            }
            if (radioAudio.isPlaying && Time.realtimeSinceStartup > radioPingTimestamp)
            {
                radioPingTimestamp = (Time.realtimeSinceStartup + 1f);
                RoundManager.Instance.PlayAudibleNoise(radioAudio.transform.position, 16f, Mathf.Min((radioAudio.volume + radioInterference.volume) * 0.5f, 0.9f), 0, false, 2692);
            }
        }
        if (IsOwner)
        {
            float random = Random.Range(0, 100);
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
            if (Time.realtimeSinceStartup - changeRadioSignalTime > 0.3f)
            {
                changeRadioSignalTime = Time.realtimeSinceStartup;
                if (radioSignalQuality < 1.2f && Random.Range(0, 100) < 6)
                {
                    radioSignalQuality = Mathf.Min(radioSignalQuality + 1.5f, 3f);
                    radioSignalDecreaseThreshold = Mathf.Min(radioSignalDecreaseThreshold + 30f, 100f);
                }
                SetRadioSignalQualityRpc((int)Mathf.Round(radioSignalQuality), radioSignalDecreaseThreshold);
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

    [Rpc(SendTo.NotOwner, RequireOwnership = false)]
    public void SetRadioSignalQualityRpc(int signalQuality, float signalDecrease)
    {
        radioSignalQuality = signalQuality;
        radioSignalDecreaseThreshold = signalDecrease;
    }

    public new void SetRadioOnLocalClient(bool on, bool setClip = true)
    {
        Plugin.Logger.LogDebug($"Hauler: Radio called with on? {on}, setClip? {setClip}");
        radioOn = on;
        if (on)
        {
            if (setClip || radioAudio.clip == null)
            {
                if (radioAudio.clip == null) Plugin.Logger.LogDebug("Hauler: Setting station, was null!");
                radioAudio.clip = radioClips[currentRadioClip];
                Plugin.Logger.LogDebug($"Hauler: Set radio clip to {currentRadioClip}, station? {radioAudio.clip.name}");
            }
            currentSongTime = 0f;
            SetRadioTime();
            radioAudio.Play();
            radioInterference.Play();
            return;
        }
        radioAudio.Stop();
        radioInterference.Stop();
        Plugin.Logger.LogDebug("Hauler: Stop radio playback!");
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
                    if (obstacleSize <= 1f)
                    {
                        return false;
                    }
                    float enemyHitSpeed;
                    if (obstacleSize <= 2f)
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
                                enemyScript.HitEnemyOnLocalClient(2, Vector3.zero, playerControllerB, playHitSFX: true, 331);
                            }
                            result = true;
                            if (obstacleSize > 2f) DealPermanentDamage(1, position);
                        }
                    }
                    else
                    {
                        mainRigidbody.AddForce(Vector3.Normalize(-impulse * 1E+09f) * (carReactToPlayerHitMultiplier - 220f), ForceMode.Impulse);
                        if (dealDamage)
                        {
                            DealPermanentDamage(1, position);
                        }
                        if (enemyScript is GiantKiwiAI)
                        {
                            PlayerControllerB playerWhoHit = currentDriver != null ? currentDriver : currentPassenger;
                            enemyScript.HitEnemyOnLocalClient(12, Vector3.zero, playerWhoHit, false, -1);
                        }
                        else
                        {
                            enemyScript.KillEnemyOnOwnerClient();
                        }
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

        if (Time.realtimeSinceStartup - timeSinceLastCollision < 0.1f)
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
                lastStressType = "Scraping";
            }
            timeSinceLastCollision = Time.realtimeSinceStartup;
            return;
        }

        float collisionVolume = 0.5f;
        int audioType = -1;

        if (averageVelocity.magnitude > 27f)
        {
            if (carHP < 3)
            {
                DestroyCarRpc();
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
            timeSinceLastCollision = Time.realtimeSinceStartup + 0.25f;
        }

        if (collisionImpulse >= minimalBumpForce && collisionImpulse < mediumBumpForce &&
            averageVelocity.magnitude > 3f)
        {
            audioType = 0;
            collisionVolume = Mathf.Clamp((collisionImpulse - minimalBumpForce) / (mediumBumpForce - minimalBumpForce), 0.25f, 1f);
            collisionVolume = Mathf.Clamp(collisionVolume + Random.Range(-0.15f, 0.25f), 0.25f, 1f);
        }
        else if (collisionImpulse >= mediumBumpForce && collisionImpulse < maximumBumpForce &&
            averageVelocity.magnitude > 6f)
        {
            audioType = 1;
            collisionVolume = Mathf.Clamp((collisionImpulse - mediumBumpForce) / (maximumBumpForce - mediumBumpForce), 0.67f, 1f);
            collisionVolume = Mathf.Clamp(collisionVolume + Random.Range(-0.15f, 0.25f), 0.5f, 1f);
        }
        else if (collisionImpulse >= maximumBumpForce &&
            averageVelocity.magnitude > 12f)
        {
            audioType = 2;
            collisionVolume = Mathf.Clamp((collisionImpulse - maximumBumpForce) / 20000f, 0.8f, 1f);
            collisionVolume = Mathf.Clamp(collisionVolume + Random.Range(-0.15f, 0.25f), 0.7f, 1f);
            DealPermanentDamage(1);
            timeSinceLastCollision = Time.realtimeSinceStartup + 0.2f;
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
            timeSinceLastCollision = Time.realtimeSinceStartup + 0.33f;
        }
    }

    public void CarBump(Vector3 vel)
    {
        CarBumpLocalClient(vel);
        CarBumpRpc(vel);
    }

    public void CarBumpLocalClient(Vector3 vel)
    {
        if (VehicleUtils.IsPlayerSeatedInPickup() && vel.magnitude > 50f)
        {
            GameNetworkManager.Instance.localPlayerController.externalForceAutoFade += vel;
            return;
        }
        if (!VehicleUtils.IsPlayerInPickupBounds(this))
            return;
        vel = Vector3.ClampMagnitude(vel, 30f);
        GameNetworkManager.Instance.localPlayerController.externalForceAutoFade += vel;
    }

    [Rpc(SendTo.NotOwner, RequireOwnership = false)]
    public void CarBumpRpc(Vector3 vel)
    {
        CarBumpLocalClient(vel);
    }

    [Rpc(SendTo.NotOwner, RequireOwnership = false)]
    public void CarCollisionRpc(Vector3 vel)
    {
        DamagePlayerInVehicle(vel);
        BreakWindshield();
    }

    public void DamagePlayerInVehicle(Vector3 vel)
    {
        if (VehicleUtils.IsPlayerSeatedInPickup())
        {
            if (vel.magnitude > 30f)
            {
                if (GameNetworkManager.Instance.localPlayerController.health < 48)
                {
                    GameNetworkManager.Instance.localPlayerController.KillPlayer(vel, true, CauseOfDeath.Inertia, 0, base.transform.up * 0.77f, false);
                    return;
                }
                GameNetworkManager.Instance.localPlayerController.DamagePlayer(40, true, true, CauseOfDeath.Inertia, 0, false, vel);
                return;
            }
            if (vel.magnitude <= 24f)
            {
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                GameNetworkManager.Instance.localPlayerController.DamagePlayer(30, true, true, CauseOfDeath.Inertia, 0, false, vel);
                return;
            }
            HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
            if (GameNetworkManager.Instance.localPlayerController.health < 20)
            {
                GameNetworkManager.Instance.localPlayerController.KillPlayer(vel, true, CauseOfDeath.Inertia, 0, base.transform.up * 0.77f, false);
                return;
            }
            GameNetworkManager.Instance.localPlayerController.DamagePlayer(20, true, true, CauseOfDeath.Inertia, 0, false, vel);
            return;
        }
        if (!VehicleUtils.IsPlayerInPickupBounds(this))
            return;
        if (GameNetworkManager.Instance.localPlayerController.health <= 40)
        {
            GameNetworkManager.Instance.localPlayerController.KillPlayer(vel, spawnBody: true, CauseOfDeath.Inertia, 0, transform.up * 0.77f, false);
            return;
        }
        GameNetworkManager.Instance.localPlayerController.DamagePlayer(30, hasDamageSFX: true, callRPC: true, CauseOfDeath.Inertia, 0, fallDamage: false, vel);
        GameNetworkManager.Instance.localPlayerController.externalForceAutoFade += vel;
    }

    private new void BreakWindshield()
    {
        if (windshieldBroken)
            return;

        glassParticle.Play();
        windshieldBroken = true;
        windshieldMesh.enabled = false;
        windowWipersEvent.enabled = false;
        windshieldAudio.PlayOneShot(windshieldBreak);
    }

    public new void PlayCollisionAudio(Vector3 setPosition, int audioType, float setVolume)
    {
        if (Time.realtimeSinceStartup - audio1Time > Time.realtimeSinceStartup - audio2Time)
        {
            bool audioTime = Time.realtimeSinceStartup - audio1Time >= collisionAudio1.clip.length * 0.8f;
            if (audio1Type <= audioType || audioTime)
            {
                audio1Time = Time.realtimeSinceStartup;
                audio1Type = audioType;
                collisionAudio1.transform.position = setPosition;
                CarCollisionSFXRpc(collisionAudio1.transform.localPosition, 0, audioType, setVolume);
                PlayRandomClipAndPropertiesFromAudio(collisionAudio1, setVolume, audioTime, audioType);
            }
        }
        else
        {
            bool audioTime = Time.realtimeSinceStartup - audio2Time >= collisionAudio2.clip.length * 0.8f;
            if (audio1Type <= audioType || audioTime)
            {
                audio2Time = Time.realtimeSinceStartup;
                audio2Type = audioType;
                collisionAudio2.transform.position = setPosition;
                CarCollisionSFXRpc(collisionAudio2.transform.localPosition, 1, audioType, setVolume);
                PlayRandomClipAndPropertiesFromAudio(collisionAudio2, setVolume, audioTime, audioType);
            }
        }
    }

    [Rpc(SendTo.NotOwner, RequireOwnership = false)]
    public void CarCollisionSFXRpc(Vector3 audioPosition, int audio, int audioType, float vol)
    {
        AudioSource audioSource;
        if (audio == 0)
        {
            audioSource = collisionAudio1;
        }
        else
        {
            audioSource = collisionAudio2;
        }
        bool audioFinished = audioSource.clip.length - audioSource.time < 0.2f;
        audioSource.transform.localPosition = audioPosition;
        PlayRandomClipAndPropertiesFromAudio(audioSource, vol, audioFinished, audioType);
    }

    private new void PlayRandomClipAndPropertiesFromAudio(AudioSource source, float volume, bool isAudioFinished, int collisionType)
    {
        if (!isAudioFinished)
        {
            source.Stop();
        }

        AudioClip[] selectedClips;
        switch (collisionType)
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

        AudioClip chosenClip = selectedClips[Random.Range(0, selectedClips.Length)];

        if (chosenClip == source.clip && Random.Range(0, 10) <= 5)
        {
            chosenClip = selectedClips[Random.Range(0, selectedClips.Length)];
        }

        if (isAudioFinished)
        {
            source.pitch = Random.Range(0.8f, 1.2f);
        }

        source.clip = chosenClip;
        source.PlayOneShot(chosenClip, volume);

        if (ignitionStarted)
        {
            if (collisionType >= 2)
            {
                RoundManager.Instance.PlayAudibleNoise(engineAudio1.transform.position, 18f + volume * 7f, 0.6f, 0, noiseIsInsideClosedShip: false, 106217);
            }
            else if (collisionType >= 1)
            {
                RoundManager.Instance.PlayAudibleNoise(engineAudio1.transform.position, 12f + volume * 7f, 0.6f, 0, noiseIsInsideClosedShip: false, 106217);
            }
        }

        if (collisionType == -1)
        {
            selectedClips = minCollisions;
            chosenClip = selectedClips[Random.Range(0, selectedClips.Length)];
            source.PlayOneShot(chosenClip);
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
            lastDamageType = "Stress";
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
            SyncCarHealthRpc(carHP);
            DestroyCarRpc();
            DestroyCar();
            return;
        }
        DealDamageRpc(carHP);
    }

    [Rpc(SendTo.NotOwner, RequireOwnership = false)]
    public void DealDamageRpc(int carHealth)
    {
        timeAtLastDamage = Time.realtimeSinceStartup;
        carHP = carHealth;
        syncedCarHP = carHP;
    }


    // --- DESTRUCTION ---
    [Rpc(SendTo.NotOwner, RequireOwnership = false)]
    public void DestroyCarRpc()
    {
        DestroyCar();
    }

    public new void DestroyCar()
    {
        if (carDestroyed)
            return;

        carDestroyed = true;
        UnMagnetCar();
        RemoveCarRainCollision();
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

    private void UnMagnetCar()
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
        underExtremeStress = false;
        engineAudio1.Stop();
        engineAudio2.Stop();
        turbulenceAudio.Stop();
        pushAudio.Stop();
        miscAudio.Stop();
        steeringWheelAudio.Stop();
        gearStickAudio.Stop();
        rollingAudio.Stop();
        radioAudio.Stop();
        radioInterference.Stop();
        extremeStressAudio.Stop();
        carKeyAudio.Stop();
        honkingHorn = false;
        hornAudio.Stop();
        skiddingAudio.Stop();
        cabinAudio.Stop();
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
        accessoryMode = false;
        radioScreen.text = null;
        if (carExhaustParticle.isEmitting) carExhaustParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        keyIsInIgnition = false;
        keyIsInDriverHand = false;
        twistingKey = false;
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
        if (!VehicleUtils.IsPlayerSeatedInPickup())
            return;
        GameNetworkManager.Instance.localPlayerController.KillPlayer(Vector3.up * 27f + 20f * Random.insideUnitSphere, spawnBody: true, CauseOfDeath.Blast, 6, Vector3.up * 1.5f, false);
    }

    private void DisableInteractions()
    {
        InteractTrigger[] interactTriggers = this.gameObject.GetComponentsInChildren<InteractTrigger>();
        for (int i = 0; i < interactTriggers.Length; i++)
        {
            interactTriggers[i].interactable = false;
            interactTriggers[i].CancelAnimationExternally();
        }
    }

    private void ResetOccupants()
    {
        currentDriver = null!;
        currentPassenger = null!;
        currentBackLeftPassenger = null!;
        currentMiddlePassenger = null!;
        currentBackRightPassenger = null!;
    }


    // --- REMOVAL MISC ---
    public void RemoveCarRainCollision()
    {
        var particleTriggers = new[]
        {
            GlobalReferences.rainParticles,
            GlobalReferences.rainHitParticles,
            GlobalReferences.stormyRainParticles,
            GlobalReferences.stormyRainHitParticles,
            GlobalReferences.wesleyHurricaneRainParticles,
            GlobalReferences.wesleyHurricaneRainHitParticles,
            GlobalReferences.wesleyHurricaneSandParticles,
            GlobalReferences.wesleyForsakenRainParticles,
            GlobalReferences.wesleyForsakenRainHitParticles
        };

        foreach (var particle in particleTriggers)
        {
            if (particle == null)
            {
                Plugin.Logger.LogDebug("ScanVan: Weather particle or Trigger is null!");
                continue;
            }

            var trigger = particle.trigger;
            for (int j = trigger.colliderCount - 1; j >= 0; j--)
            {
                var collider = (Collider)trigger.GetCollider(j);
                if (weatherEffectBlockers.Contains(collider))
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
            SetPreviousVehiclePosition();
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
            SetPreviousVehiclePosition();
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
            SetPreviousVehiclePosition();
            return;
        }

        ApplyAntiSlipForce();
        CalculateWheelSlip(calculatePhysics: true);
        SetPreviousVehiclePosition();
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

    private void SetPreviousVehiclePosition()
    {
        previousVehiclePosition = mainRigidbody.position;
        previousVehicleRotation = mainRigidbody.rotation;
    }

    private void SetVehicleToDropship()
    {
        if (StartOfRound.Instance.inShipPhase ||
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
        Plugin.Logger.LogDebug($"Hauler: Set 'mainRigidbody' kinematic to: {setKinematic}");
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
        //Mathf.Clamp(syncSpeedMultiplier * Vector3.Distance(mainRigidbody.position, syncVel), 1.3f, 300f);
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

    [Rpc(SendTo.NotOwner, RequireOwnership = false)]
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
        Vector3 upwardForce = Vector3.Cross(Quaternion.AngleAxis(mainRigidbody.angularVelocity.magnitude * 57.29578f * stability / speed, mainRigidbody.angularVelocity) * transform.up, Vector3.up);
        mainRigidbody.AddTorque(upwardForce * speed * speed);
    }

    private void MovePhysicsBodies()
    {
        ragdollPhysicsBody.Move(
          transform.position,
          transform.rotation);
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

    [Rpc(SendTo.NotOwner, RequireOwnership = false)]
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
            float engineSpeed = NormaliseFloat(Mathf.Round(EngineRPM));

            float wheelSyncRPM = NormaliseFloat(Mathf.Round(wheelRPM));
            float fWheelSyncRPM = NormaliseFloat(Mathf.Round(frontWheelRPM));
            float bWheelSyncRPM = NormaliseFloat(Mathf.Round(backWheelRPM));

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

    [Rpc(SendTo.NotOwner, RequireOwnership = false)]
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

    [Rpc(SendTo.NotOwner, RequireOwnership = false)]
    public void SyncCarPositionRpc(Vector3 carPosition, Vector3 carRotation)
    {
        syncCarPositionInterval = 0f;
        syncedPosition = carPosition;
        syncedRotation = Quaternion.Euler(carRotation);
    }
    // --- SYNC POSITION END ---


    private void ApplySteering()
    {
        steeringAngle = maxSteeringAngle * steeringWheelAnimFloat;
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

        ApplyTorqueDifference();
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
        float wheelSpeed = absWheelSpeed * selectedGear * diffRatio;

        // hill assist
        float slopeAngle = Vector3.Angle(transform.forward, Vector3.ProjectOnPlane(transform.forward, Vector3.up)); // get the slope angle
        float dot = Vector3.Dot(transform.forward, Vector3.up);

        float load = Mathf.Clamp01(Mathf.Abs(EngineRPM - wheelSpeed) / 800f); // rpm difference between the wheels and the engine
        float assistValue = Mathf.InverseLerp(110f, 0f, absWheelSpeed); // rpm the assist is active up until
        float assist = Mathf.Clamp01(dot) * assistValue * load; // assist rate

        float slopeValue = 0f;
        if (dot > 0f) // only apply uphill
        {
            slopeValue = Mathf.Clamp01(slopeAngle / maxInclineBoostAngle);
        }
        inclineBoost = Mathf.Lerp(minInclineBoost, maxInclineBoost, slopeValue * assistValue);
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
                        vehicleStress += 1.2f;
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
        frontWheelRPM = (NormaliseFloat(FrontLeftWheel.rpm) + NormaliseFloat(FrontRightWheel.rpm)) / 2f;
        backWheelRPM = (NormaliseFloat(BackLeftWheel.rpm) + NormaliseFloat(BackRightWheel.rpm)) / 2f;
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
                    if (drivePedalPressed) currentMotorTorque = Mathf.Clamp(Mathf.MoveTowards(currentMotorTorque, maxForwardTorque, (carAcceleration * inclineBoost) * Time.deltaTime), minTorque * inclineBoost, maxForwardTorque);
                    else currentMotorTorque = Mathf.MoveTowards(currentMotorTorque, idleSpeed, (carDeacceleration / inclineBoost) * Time.deltaTime);
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

        // hacky way of having a "locked differential"
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
    public float NormaliseFloat(float num)
    {
        if (float.IsNaN(num) || float.IsInfinity(num) ||
            float.IsNegativeInfinity(num) || float.IsPositiveInfinity(num))
            return 0f;
        return num;
    }

    private void MatchWheelMeshToCollider(MeshRenderer wheelMesh, WheelCollider wheelCollider, float wheelSpeed, float steeringInput = 0f)
    {
        Vector3 position = wheelCollider.transform.position;
        if (Physics.Raycast(position, -wheelCollider.transform.up, out hit, wheelCollider.suspensionDistance + wheelCollider.radius, 2305))
        {
            wheelMesh.transform.position = hit.point + wheelCollider.transform.up * wheelCollider.radius;
        }
        else
        {
            wheelMesh.transform.position = position - wheelCollider.transform.up * wheelCollider.suspensionDistance;
        }
        //wheelCollider.GetWorldPose(out Vector3 wheelPosition, out Quaternion wheelRotation);
        //wheelMesh.transform.position = wheelPosition;
        //wheelMesh.transform.rotation = wheelRotation;
        wheelMesh.transform.localRotation = Quaternion.Euler(wheelSpeed, steeringInput, 0.0f);
    }


    // --- UPDATE ---
    public new void Update()
    {
        if (destroyNextFrame)
        {
            if (IsOwner)
            {
                Destroy(base.windwiperPhysicsBody1.gameObject);
                Destroy(base.windwiperPhysicsBody2.gameObject);
                Destroy(base.ragdollPhysicsBody.gameObject);
                Destroy(this.playerPhysicsBody.gameObject);
                Destroy(base.gameObject);
            }
            return;
        }
        if (NetworkObject != null && !NetworkObject.IsSpawned)
        {
            RemoveCarRainCollision();

            vehicleZone.disablePhysicsRegion = true;

            if (StartOfRound.Instance.CurrentPlayerPhysicsRegions.Contains(vehicleZone))
                StartOfRound.Instance.CurrentPlayerPhysicsRegions.Remove(vehicleZone);

            if (localPlayerInControl || localPlayerInPassengerSeat ||
                localPlayerInBackLeftPassengerSeat || localPlayerInMiddlePassengerSeat || localPlayerInBackRightPassengerSeat)
                GameNetworkManager.Instance.localPlayerController.CancelSpecialTriggerAnimations();

            GrabbableObject[] itemsInTruck = vehicleZone.physicsTransform.GetComponentsInChildren<GrabbableObject>();
            for (int i = 0; i < itemsInTruck.Length; i++)
            {
                if (RoundManager.Instance.mapPropsContainer != null)
                {
                    itemsInTruck[i].transform.SetParent(RoundManager.Instance.mapPropsContainer.transform, worldPositionStays: true);
                }
                else
                {
                    itemsInTruck[i].transform.SetParent(null, worldPositionStays: true);
                }

                if (!itemsInTruck[i].isHeld)
                    itemsInTruck[i].FallToGround(false, false, default(Vector3));
            }
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
                turbulenceAudio.PlayOneShot(maxCollisions[Random.Range(0, maxCollisions.Length)]);
            }
        }
        else
        {
            finishedMagneting = false;
            if (StartOfRound.Instance.attachedVehicle == this)
            {
                StartOfRound.Instance.attachedVehicle = null;
            }
            //if (IsOwner)
            //{
            //    if (enabledCollisionForAllPlayers)
            //    {
            //        enabledCollisionForAllPlayers = false;
            //        DisableVehicleCollisionForAllPlayers();
            //    }
            //}
            //else
            //{
            //    if (!enabledCollisionForAllPlayers)
            //    {
            //        enabledCollisionForAllPlayers = true;
            //        EnableVehicleCollisionForAllPlayers();
            //    }
            //}
        }

        ReactToDamage();

        if (carDestroyed)
        {
            return;
        }

        //HUDManager.Instance.enableConsoleLogging = true;
        //HUDManager.Instance.SetDebugText($"isPlayerInCab? {vehicleCabZone.playerInZone}\n " +
        //                                 $"isPlayerOnTruck? {vehicleZone.playerInZone}\n " +
        //                                 $"isPlayerParented? {GameNetworkManager.Instance.localPlayerController.physicsParent}\n" +
        //                                 $"isHasLocalPlayer? {vehicleZone.hasLocalPlayer}");

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
    [Rpc(SendTo.NotOwner, RequireOwnership = false)]
    private void SyncCarHealthRpc(int carHealth)
    {
        timeAtLastDamage = Time.realtimeSinceStartup;
        syncedCarHP = carHealth;
        carHP = syncedCarHP;
    }

    [Rpc(SendTo.NotOwner, RequireOwnership = false)]
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

    [Rpc(SendTo.NotOwner, RequireOwnership = false)]
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
        carHoodAnimator.SetBool("hoodOpen", carHoodOpen);
        SetHoodOpenRpc(open: true);
    }

    // used for when the hood is 'on fire'
    public new void SetHoodOpenLocalClient(bool setOpen)
    {
        if (carHoodOpen && carHoodOpen == setOpen)
            return;

        carHoodOpen = setOpen;
        carHoodAnimator.SetBool("hoodOpen", setOpen);
    }

    [Rpc(SendTo.NotMe, RequireOwnership = false)]
    public void SetHoodOpenRpc(bool open)
    {
        carHoodOpen = open;
        carHoodAnimator.SetBool("hoodOpen", carHoodOpen);
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

        currentDriver.playerBodyAnimator.SetFloat(ANIMATION_SPEED, playerSteeringWheelAnimFloat); // player steering animation
        currentDriver.playerBodyAnimator.SetFloat(CAR_MOTION_TIME, gearStickAnimValue); // vehicle gearstick --> player gearstick animation position

        int currentAnimIndex = 1;
        if (playerWhoShifted == currentDriver && Time.realtimeSinceStartup - timeAtLastGearShift < 1.35f) currentAnimIndex = 5;
        currentDriver.playerBodyAnimator.SetInteger(CAR_ANIM, currentAnimIndex);
    }


    // --- AFTER UPDATE ---
    public new void LateUpdate()
    {
        if (carDestroyed)
        {
            return;
        }
        if (localPlayerInControl && !setControlTips)
        {
            setControlTips = true;
            HUDManager.Instance.ChangeControlTipMultiple(carTooltips, false, null);
        }

        if (currentDriver != null && lastDriver != currentDriver && !magnetedToShip)
            lastDriver = currentDriver;

        if (honkingHorn && hornAudio.isPlaying && hornAudio.pitch < 1f)
            hornAudio.Stop();

        //bool inOrbit = magnetedToShip && (StartOfRound.Instance.inShipPhase || !StartOfRound.Instance.shipDoorsEnabled);
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
            !playerController.isPlayerControlled) return;
        if (playerController.isTypingChat ||
            playerController.quickMenuManager.isMenuOpen) return;

        if (!localPlayerInControl || !ignitionStarted ||
            jumpingInCar || keyIsInDriverHand) return;

        Vector2 dir = InputSystem.actions.FindAction("Move", false).ReadValue<Vector2>();
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
            StartCoroutine(jerkCarUpward(dir));
        }
    }

    private new IEnumerator jerkCarUpward(Vector3 dir)
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

    [Rpc(SendTo.NotOwner, RequireOwnership = false)]
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
        if (vehicleZone.playerInZone)
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

        if (IsOwner)
        {
            mainRigidbody.AddForceAtPosition(Vector3.Normalize(forward * 1000f) * Random.Range(40f, 50f) * pushForceMultiplier, point - mainRigidbody.transform.up * pushVerticalOffsetAmount, ForceMode.Impulse);
            PushTruckFromOwnerRpc(point);
            return;
        }
        PushTruckRpc(point, forward);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void PushTruckRpc(Vector3 pushPosition, Vector3 dir)
    {
        pushAudio.transform.position = pushPosition;
        pushAudio.Play();
        turbulenceAmount = Mathf.Min(turbulenceAmount + 0.5f, 2f);
        if (IsOwner)
        {
            mainRigidbody.AddForceAtPosition(Vector3.Normalize(dir * 1000f) * Random.Range(40f, 50f) * pushForceMultiplier, pushPosition - mainRigidbody.transform.up * pushVerticalOffsetAmount, ForceMode.Impulse);
        }
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void PushTruckFromOwnerRpc(Vector3 pos)
    {
        pushAudio.transform.position = pos;
        pushAudio.Play();
        turbulenceAmount = Mathf.Min(turbulenceAmount + 0.5f, 2f);
    }


    // --- REMOVAL ---
    public new void OnDisable()
    {
        RemoveCarRainCollision();
        DisableControl();
        if (localPlayerInControl || localPlayerInPassengerSeat ||
            localPlayerInBackLeftPassengerSeat || localPlayerInMiddlePassengerSeat || localPlayerInBackRightPassengerSeat)
        {
            GameNetworkManager.Instance.localPlayerController.CancelSpecialTriggerAnimations();
        }
        GrabbableObject[] itemsInTruck = vehicleZone.physicsTransform.GetComponentsInChildren<GrabbableObject>();
        for (int i = 0; i < itemsInTruck.Length; i++)
        {
            if (RoundManager.Instance.mapPropsContainer != null)
            {
                itemsInTruck[i].transform.SetParent(RoundManager.Instance.mapPropsContainer.transform, worldPositionStays: true);
            }
            else
            {
                itemsInTruck[i].transform.SetParent(null, worldPositionStays: true);
            }

            if (!itemsInTruck[i].isHeld)
                itemsInTruck[i].FallToGround(false, false, default(Vector3));
        }
        vehicleZone.disablePhysicsRegion = true;
        if (StartOfRound.Instance.CurrentPlayerPhysicsRegions.Contains(vehicleZone))
        {
            StartOfRound.Instance.CurrentPlayerPhysicsRegions.Remove(vehicleZone);
        }
        References.pickupController = null!;
    }


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
}