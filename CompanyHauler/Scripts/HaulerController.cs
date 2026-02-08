using System.Collections;
using System.Collections.Generic;
using CompanyHauler.Utils;
using GameNetcodeStuff;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;

namespace CompanyHauler.Scripts;

public class HaulerController : VehicleController
{
    [Header("Player Animations")]

    public RuntimeAnimatorController originalController = null!;
    public AnimatorOverrideController overrideController = null!;
    public RuntimeAnimatorController originalController_pass = null!;
    public AnimatorOverrideController overrideController_pass = null!;

    public AnimationClip cruiserGearShiftClip = null!;
    public AnimationClip cruiserGearShiftIdleClip = null!;
    public AnimationClip cruiserSteeringClip = null!;
    public AnimationClip cruiserKeyInsertClip = null!;
    public AnimationClip cruiserKeyInsertAgainClip = null!;
    public AnimationClip cruiserKeyRemoveClip = null!;
    public AnimationClip cruiserKeyUntwistClip = null!;

    public AnimationClip haulerSitAndSteerNoHandsClip = null!;
    public AnimationClip haulerPassengerSitClip = null!;
    public AnimationClip haulerColumnShiftClip = null!;
    public AnimationClip haulerKeyInsertClip = null!;
    public AnimationClip haulerKeyInsertAgainClip = null!;
    public AnimationClip haulerKeyRemoveClip = null!;
    public AnimationClip haulerKeyUntwistClip = null!;

    public bool passReplaced;

    [Header("Vehicle Physics")]
    public List<WheelCollider> wheels = null!;
    public Collider truckColliderThingINeedForThatStupidAssBird = null!;

    private WheelHit[] wheelHits = new WheelHit[4];
    public float clampedLimitTruckVelocity;
    public float sidewaysSlip;
    public float forwardsSlip;
    public float wheelTorque;
    public float wheelBrakeTorque;
    public bool hasDeliveredVehicle;
    public float maxSteeringAngle = 35f;
    public float maxBrakingPower = 2000f;
    public float wheelRPM;

    [Space(5f)]
    [Header("Engine")]

    public bool tryingIgnition;

    [Header("Transmission")]

    public float diffRatio;
    public float forwardWheelSpeed;
    public float reverseWheelSpeed;

    [Header("Multiplayer")]

    public PlayerControllerB currentBL = null!;
    public PlayerControllerB currentBR = null!;
    public PlayerControllerB currentMiddle = null!;
    public InteractTrigger BLSeatTrigger = null!;
    public InteractTrigger BRSeatTrigger = null!;
    public InteractTrigger MiddleSeatTrigger = null!;

    public Transform[] BL_ExitPoints = null!;
    public Transform[] BR_ExitPoints = null!;

    public AnimatedObjectTrigger BLSideDoor = null!;
    public AnimatedObjectTrigger BRSideDoor = null!;

    public InteractTrigger BLSideDoorTrigger = null!;
    public InteractTrigger BRSideDoorTrigger = null!;

    public Vector3 syncedMovementSpeed;
    public float syncedSpeedometerFloat;
    public float syncedWheelRotation;
    public float syncedEngineRPM;
    public float syncedWheelRPM;
    public float syncedMotorTorque;
    public float syncedBrakeTorque;
    public float tyreStress;
    public bool wheelSlipping;
    public float syncCarEffectsInterval;
    public float syncWheelTorqueInterval;
    public float syncCarDrivetrainInterval;
    public float syncCarWheelSpeedInterval;
    public float syncCarDialsEffectInterval;

    public bool localPlayerInBLSeat;
    public bool localPlayerInBRSeat;
    public bool localPlayerInMiddleSeat;

    [Header("Effects")]

    private string[] haulerTooltips = new string[]
    {
        "Gas pedal: [W]",
        "Brake pedal: [S]",
        "Lurch: [Space]",
    };

    public GameObject screensContainer = null!;
    public TextMeshProUGUI dotMatrix = null!;
    public MeshRenderer leftDial = null!;
    public MeshRenderer rightDial = null!;

    public Transform leftDialTransform = null!;
    public Transform rightDialTransform = null!;

    public Image leftDialTickmarks = null!;
    public Image rightDialTickmarks = null!;

    public GameObject checkEngineLight = null!;
    public GameObject tractionControlLight = null!;

    public List<GameObject> haulerObjectsToDestroy = null!;
    public GameObject mirrorsContainer = null!;
    public InteractTrigger redButtonTrigger = null!;

    public float speedometerFloat;

    public bool lastKeyInIgnition = false;
    public bool checkEngineWasAlarmed = false;
    public bool tractionLightWasAlarmed = false;

    public float superHornCooldownTime;
    public float superHornCooldownAmount;

    public bool cablightToggle = false; // bad
    public bool cabinLightBoolean = false;

    [Header("Audio")]

    public AudioClip chimeSound = null!;
    public AudioClip chimeSoundCritical = null!;
    public AudioSource ChimeAudio = null!;

    public AudioClip TrainHornAudioClip = null!;
    public AudioSource TrainHornAudio = null!;
    public AudioSource TrainHornAudioDistant = null!;

    public AudioSource roofRainAudio = null!;

    [Header("Radio")]

    public float timeLastSyncedRadio;
    public float radioPingTimestamp;

    [Header("Materials")]

    public Material cabinLightOnMat = null!;
    public Material cabinLightOffMat = null!;

    public Material dialOnMat = null!;
    public Material dialOffMat = null!;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (StartOfRound.Instance.inShipPhase ||
            !IsServer)
            return;

        // health
        baseCarHP = CompanyHauler.BoundConfig.haulerHealth.Value;
        carHP = baseCarHP;
        carFragility = 1f;
        SyncHaulerDataServerRpc(carHP);
    }

    // Additional things to do on awake
    public new void Awake()
    {
        if (References.truckController == null)
            References.truckController = this;

        base.Awake();

        ontopOfTruckCollider = truckColliderThingINeedForThatStupidAssBird;

        redButtonTrigger.interactable = false;
        superHornCooldownTime = superHornCooldownAmount;
        //backDoorOpen = true; // (will redo later)
    }

    private void SetTruckStats()
    {
        // drivetrain
        gear = CarGearShift.Park;
        MaxEngineRPM = 400f;
        MinEngineRPM = 80f;
        engineIntensityPercentage = 400f;
        EngineTorque = 1100f;
        carAcceleration = 300f;
        idleSpeed = 15f;

        // physics
        mainRigidbody.automaticCenterOfMass = false;
        mainRigidbody.centerOfMass = new Vector3(0f, -0.25f, 0.5f);
        mainRigidbody.automaticInertiaTensor = false;
        mainRigidbody.maxDepenetrationVelocity = 1f;
        speed = 55;
        stability = 0.55f;

        carMaxSpeed = 60f;
        mainRigidbody.maxLinearVelocity = carMaxSpeed;
        mainRigidbody.maxAngularVelocity = 4f;
        pushForceMultiplier = 28f;
        pushVerticalOffsetAmount = 1f;
        steeringWheelTurnSpeed = 4.75f;
        torqueForce = 2.5f;

        SetWheelFriction();

        FrontLeftWheel.wheelDampingRate = 0.7f;
        FrontRightWheel.wheelDampingRate = 0.7f;
        BackRightWheel.wheelDampingRate = 0.7f;
        BackLeftWheel.wheelDampingRate = 0.7f;

        FrontLeftWheel.mass = 45f;
        FrontRightWheel.mass = 45f;
        BackLeftWheel.mass = 45f;
        BackRightWheel.mass = 45f;
    }

    private new void SetWheelFriction()
    {
        WheelFrictionCurve forwardFrictionCurve = new WheelFrictionCurve
        {
            extremumSlip = 0.6f,
            extremumValue = 0.75f,
            asymptoteSlip = 0.8f,
            asymptoteValue = 0.5f,
            stiffness = 1f,
        };
        FrontRightWheel.forwardFriction = forwardFrictionCurve;
        FrontLeftWheel.forwardFriction = forwardFrictionCurve;
        BackRightWheel.forwardFriction = forwardFrictionCurve;
        BackLeftWheel.forwardFriction = forwardFrictionCurve;
        WheelFrictionCurve sidewaysFrictionCurve = new WheelFrictionCurve
        {
            extremumSlip = 0.7f,
            extremumValue = 1f,
            asymptoteSlip = 0.8f,
            asymptoteValue = 0.75f,
            stiffness = 0.75f,
        };
        FrontRightWheel.sidewaysFriction = sidewaysFrictionCurve;
        FrontLeftWheel.sidewaysFriction = sidewaysFrictionCurve;
        BackRightWheel.sidewaysFriction = sidewaysFrictionCurve;
        BackLeftWheel.sidewaysFriction = sidewaysFrictionCurve;
    }

    // Additional things to do on start
    public new void Start()
    {
        chanceToStartIgnition = 5f;
        FrontLeftWheel.brakeTorque = 2000f;
        FrontRightWheel.brakeTorque = 2000f;
        BackLeftWheel.brakeTorque = 2000f;
        BackRightWheel.brakeTorque = 2000f;

        lastKeyInIgnition = true;
        currentRadioClip = new System.Random(StartOfRound.Instance.randomMapSeed).Next(0, radioClips.Length);
        decals = new DecalProjector[24];

        checkEngineLight.SetActive(false);
        tractionControlLight.SetActive(false);
        mirrorsContainer.SetActive(CompanyHauler.BoundConfig.haulerMirror.Value);

        if (!StartOfRound.Instance.inShipPhase)
            return;

        hasBeenSpawned = true;
        magnetedToShip = true;
        loadedVehicleFromSave = true;
        hasDeliveredVehicle = true;
        inDropshipAnimation = false;
        base.transform.position = StartOfRound.Instance.magnetPoint.position + StartOfRound.Instance.magnetPoint.forward * 7f;
        StartMagneting();
    }



    // Sync the hosts health value to prevent desync
    public void SendClientSyncData()
    {
        if (!IsServer) return;
        SyncHaulerDataServerRpc(carHP);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SyncHaulerDataServerRpc(int carHealth)
    {
        SyncHaulerDataClientRpc(carHealth);
    }

    [ClientRpc]
    public void SyncHaulerDataClientRpc(int carHealth)
    {
        if (IsServer)
            return;

        baseCarHP = carHealth;
        carHP = baseCarHP;
        carFragility = 1f;
    }



    // Visual effects
    private new void SetCarEffects(float setSteering)
    {
        setSteering = IsOwner ? setSteering : 0f;
        steeringWheelAnimFloat = Mathf.Clamp(steeringWheelAnimFloat + setSteering * steeringWheelTurnSpeed * Time.deltaTime / 6f, -1f, 1f);
        steeringWheelAnimator.SetFloat("steeringWheelTurnSpeed", Mathf.Clamp((steeringWheelAnimFloat + 1f) / 2f, 0f, 1f));

        SetCarAutomaticShifter();
        SetCarLightingEffects();
        SetCarAudioEffects();
        CalculateTyreSlip();
        SetCarDashboardDials();

        MatchWheelMeshToCollider(leftWheelMesh, FrontLeftWheel);
        MatchWheelMeshToCollider(rightWheelMesh, FrontRightWheel);
        MatchWheelMeshToCollider(backLeftWheelMesh, BackLeftWheel);
        MatchWheelMeshToCollider(backRightWheelMesh, BackRightWheel);

        if (IsOwner)
        {
            SyncCarEffectsToOtherClients();
            SyncCarDialsEffectsToOtherClients();
            SyncCarDrivetrainToOtherClients();
            SyncCarWheelSpeedToOtherClients();
            SyncCarWheelTorqueToOtherClients();
            speedometerFloat = Mathf.Lerp(speedometerFloat, ((BackLeftWheel.rotationSpeed + BackRightWheel.rotationSpeed) / 2f), 50 * Time.deltaTime / 2f);
            if (!syncedExtremeStress && underExtremeStress && extremeStressAudio.volume > 0.35f)
            {
                syncedExtremeStress = true;
                SyncExtremeStressServerRpc(underExtremeStress);
            }
            else if (syncedExtremeStress && !underExtremeStress && extremeStressAudio.volume < 0.5f)
            {
                syncedExtremeStress = false;
                SyncExtremeStressServerRpc(underExtremeStress);
            }
        }
        else
        {
            steeringWheelAnimFloat = Mathf.MoveTowards(steeringWheelAnimFloat, syncedWheelRotation, steeringWheelTurnSpeed * Time.deltaTime / 6f);
            speedometerFloat = Mathf.Lerp(speedometerFloat, syncedSpeedometerFloat, 50 * Time.deltaTime / 2f);
        }
    }

    public new void MatchWheelMeshToCollider(MeshRenderer wheelMesh, WheelCollider wheelCollider)
    {
        Vector3 position;
        Quaternion rotation;
        wheelCollider.GetWorldPose(out position, out rotation);

        wheelMesh.transform.rotation = rotation;
        wheelMesh.transform.position = position;
    }



    // Networking stuff
    public void SyncCarEffectsToOtherClients()
    {
        if (syncCarEffectsInterval > 0.02f)
        {
            if (syncedWheelRotation != steeringWheelAnimFloat)
            {
                syncCarEffectsInterval = 0f;
                syncedWheelRotation = steeringWheelAnimFloat;
                SyncCarEffectsServerRpc(steeringWheelAnimFloat);
                return;
            }
        }
        else
        {
            syncCarEffectsInterval += Time.deltaTime;
        }
    }

    public void SyncCarDialsEffectsToOtherClients()
    {
        if (syncCarDialsEffectInterval > 0.14f)
        {
            if (syncedSpeedometerFloat != speedometerFloat)
            {
                syncCarDialsEffectInterval = 0f;
                syncedSpeedometerFloat = speedometerFloat;
                SyncCarDashDialServerRpc(speedometerFloat);
                return;
            }
        }
        else
        {
            syncCarDialsEffectInterval += Time.deltaTime;
        }
    }

    public void SyncCarDrivetrainToOtherClients()
    {
        if (!ignitionStarted)
            return;

        if (syncCarDrivetrainInterval > 0.12f)
        {
            int engineSpeedToSync = Mathf.RoundToInt(EngineRPM / 10f);
            if (syncedEngineRPM != engineSpeedToSync)
            {
                syncCarDrivetrainInterval = 0f;
                syncedEngineRPM = engineSpeedToSync;
                SyncCarDrivetrainServerRpc((float)engineSpeedToSync);
                return;
            }
        }
        else
        {
            syncCarDrivetrainInterval += Time.deltaTime;
        }
    }

    public void SyncCarWheelSpeedToOtherClients()
    {
        if (syncCarWheelSpeedInterval > 0.185f)
        {
            if (syncedWheelRPM != wheelRPM)
            {
                syncCarWheelSpeedInterval = 0f;
                syncedWheelRPM = wheelRPM;
                SyncCarWheelSpeedServerRpc(wheelRPM);
                return;
            }
        }
        else
        {
            syncCarWheelSpeedInterval += Time.deltaTime;
        }
    }

    public void SyncCarWheelTorqueToOtherClients()
    {
        if (syncWheelTorqueInterval > 0.16f)
        {
            if (syncedMotorTorque != FrontLeftWheel.motorTorque || syncedBrakeTorque != FrontLeftWheel.brakeTorque)
            {
                syncWheelTorqueInterval = 0f;
                syncedMotorTorque = FrontLeftWheel.motorTorque;
                syncedBrakeTorque = FrontLeftWheel.brakeTorque;
                SyncWheelTorqueServerRpc(FrontLeftWheel.motorTorque, FrontLeftWheel.brakeTorque);
                return;
            }
        }
        else
        {
            syncWheelTorqueInterval += Time.deltaTime;
        }
    }



    [ServerRpc(RequireOwnership = false)]
    public void SyncCarDrivetrainServerRpc(float engineSpeed)
    {
        SyncCarDrivetrainClientRpc(engineSpeed);
    }

    [ClientRpc]
    public void SyncCarDrivetrainClientRpc(float engineSpeed)
    {
        if (IsOwner)
            return;

        syncedEngineRPM = engineSpeed * 10f;
    }



    [ServerRpc(RequireOwnership = false)]
    public void SyncCarWheelSpeedServerRpc(float wheelSpeed)
    {
        SyncCarWheelSpeedClientRpc(wheelSpeed);
    }

    [ClientRpc]
    public void SyncCarWheelSpeedClientRpc(float wheelSpeed)
    {
        if (IsOwner)
            return;

        syncedWheelRPM = wheelSpeed;
    }



    [ServerRpc(RequireOwnership = false)]
    public void SyncCarDashDialServerRpc(float speedFloat)
    {
        SyncCarDashDialClientRpc(speedFloat);
    }

    [ClientRpc]
    public void SyncCarDashDialClientRpc(float speedFloat)
    {
        if (IsOwner)
            return;

        syncedSpeedometerFloat = speedFloat;
    }



    [ServerRpc(RequireOwnership = false)]
    public void SyncCarEffectsServerRpc(float wheelRotation)
    {
        SyncCarEffectsClientRpc(wheelRotation);
    }

    [ClientRpc]
    public void SyncCarEffectsClientRpc(float wheelRotation)
    {
        if (IsOwner)
            return;

        syncedWheelRotation = wheelRotation;
    }



    [ServerRpc(RequireOwnership = false)]
    public void SyncWheelTorqueServerRpc(float motorTorque, float brakeTorque)
    {
        SyncWheelTorqueClientRpc(motorTorque, brakeTorque);
    }

    [ClientRpc]
    public void SyncWheelTorqueClientRpc(float motorTorque, float brakeTorque)
    {
        if (IsOwner)
            return;

        syncedMotorTorque = motorTorque;
        syncedBrakeTorque = brakeTorque;
    }



    // Visual functions
    public void SetCarAutomaticShifter()
    {
        switch (gear)
        {
            case CarGearShift.Park:
                {
                    gearStickAnimValue = Mathf.MoveTowards(gearStickAnimValue, 1f, 15f * Time.deltaTime * (Time.realtimeSinceStartup - timeAtLastGearShift));
                    break;
                }
            case CarGearShift.Reverse:
                {
                    gearStickAnimValue = Mathf.MoveTowards(gearStickAnimValue, 0.5f, 15f * Time.deltaTime * (Time.realtimeSinceStartup - timeAtLastGearShift));
                    break;
                }
            case CarGearShift.Drive:
                {
                    gearStickAnimValue = Mathf.MoveTowards(gearStickAnimValue, 0f, 15f * Time.deltaTime * (Time.realtimeSinceStartup - timeAtLastGearShift));
                    break;
                }
        }
        gearStickAnimator.SetFloat("gear", Mathf.Clamp(gearStickAnimValue, 0.01f, 0.99f));
    }

    public void SetCarLightingEffects()
    {
        // Scandal note - in vanilla, these seem to be used as the "reverse" lights, on the Hauler,
        // the light mesh includes the center high mounted brake light, so I'm assuming they're meant to work as brake lights.

        bool isBraking = ((FrontLeftWheel.brakeTorque + FrontRightWheel.brakeTorque) / 2f) > 100f && ignitionStarted && gear != CarGearShift.Park;
        if (backLightsOn != isBraking)
        {
            backLightsOn = isBraking;
            backLightsMesh.material = isBraking ? backLightOnMat : headlightsOffMat;
            backLightsContainer.SetActive(isBraking);
        }
    }

    public void SetCarAudioEffects()
    {
        bool raining = TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Rainy ||
                TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Flooded ||
                TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Stormy;

        float highestAudio1 = Mathf.Clamp((EngineRPM / engineIntensityPercentage), 0.65f, 1.15f);
        float highestAudio2 = Mathf.Clamp((EngineRPM / engineIntensityPercentage), 0.7f, 1.5f);
        float wheelSpeed = Mathf.Abs(wheelRPM);
        float highestTyre = Mathf.Clamp(wheelSpeed / (180f * 0.35f), 0f, 1f);
        carEngine2AudioActive = ignitionStarted;
        carRollingAudioActive = (FrontLeftWheel.isGrounded || FrontRightWheel.isGrounded || BackLeftWheel.isGrounded || BackRightWheel.isGrounded) && wheelSpeed > 10f;
        if (!ignitionStarted)
        {
            highestAudio1 = 1f;
        }
        SetVehicleAudioProperties(engineAudio1, carEngine1AudioActive, 0.7f, highestAudio1, 2f, useVolumeInsteadOfPitch: false, 0.7f);
        SetVehicleAudioProperties(engineAudio2, carEngine2AudioActive, 0.7f, highestAudio2, 3f, useVolumeInsteadOfPitch: false, 0.5f);
        SetVehicleAudioProperties(rollingAudio, carRollingAudioActive, 0f, highestTyre, 5f, useVolumeInsteadOfPitch: true);
        SetVehicleAudioProperties(extremeStressAudio, underExtremeStress, 0.2f, 1f, 3f, useVolumeInsteadOfPitch: true);
        SetRadioValues();

        // Roof rain
        if (raining && !roofRainAudio.isPlaying)
        {
            roofRainAudio.Play();
        }
        else if (!raining && roofRainAudio.isPlaying)
        {
            roofRainAudio.Stop();
        }

        if (engineAudio1.volume > 0.3f && 
            engineAudio1.isPlaying && 
            Time.realtimeSinceStartup - timeAtLastEngineAudioPing > 2f)
        {
            timeAtLastEngineAudioPing = Time.realtimeSinceStartup;
            if (EngineRPM > 130f)
            {
                RoundManager.Instance.PlayAudibleNoise(engineAudio1.transform.position, 32f, 0.75f, 0, noiseIsInsideClosedShip: false, 2692);
            }
            if (EngineRPM > 60f && EngineRPM < 130f)
            {
                RoundManager.Instance.PlayAudibleNoise(engineAudio1.transform.position, 25f, 0.6f, 0, noiseIsInsideClosedShip: false, 2692);
            }
            else if (!ignitionStarted)
            {
                RoundManager.Instance.PlayAudibleNoise(engineAudio1.transform.position, 15f, 0.6f, 0, noiseIsInsideClosedShip: false, 2692);
            }
            else
            {
                RoundManager.Instance.PlayAudibleNoise(engineAudio1.transform.position, 11f, 0.5f, 0, noiseIsInsideClosedShip: false, 2692);
            }
        }

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

            if (hornAudio.pitch < 0.02f)
                hornAudio.Stop();
        }
    }

    public void CalculateTyreSlip()
    {
        if (IsOwner)
        {
            float vehicleSpeed = Vector3.Dot(Vector3.Normalize(mainRigidbody.velocity * 1000f), transform.forward);
            float wheelSpeed = Mathf.Abs(wheelRPM);
            bool audioActive = vehicleSpeed > -0.6f && vehicleSpeed < 0.4f && (averageVelocity.magnitude > 4f || (wheelSpeed > 65f));
            if (BackLeftWheel.isGrounded && BackRightWheel.isGrounded)
            {
                WheelHit[] wheelHits = new WheelHit[2];
                BackLeftWheel.GetGroundHit(out wheelHits[0]);
                BackRightWheel.GetGroundHit(out wheelHits[1]);
                if ((BackLeftWheel.motorTorque > 900f && BackRightWheel.motorTorque > 900f) && 
                    (Mathf.Abs(wheelHits[0].forwardSlip) > 0.2f || Mathf.Abs(wheelHits[1].forwardSlip) > 0.2f))
                {
                    vehicleSpeed = Mathf.Max(vehicleSpeed, 0.8f);
                    audioActive = true;

                    if (averageVelocity.magnitude > 8f && !tireSparks.isPlaying)
                    {
                        tireSparks.Play(true);
                    }
                }
                else
                {
                    audioActive = false;
                    tireSparks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
            else
            {
                audioActive = false;
                tireSparks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            SetVehicleAudioProperties(skiddingAudio, audioActive, 0f, vehicleSpeed, 3f, true, 1f);
            if ((Mathf.Abs(tyreStress - vehicleSpeed) > 0.04f) || wheelSlipping != audioActive)
            {
                SetTyreStressOnServerRpc(vehicleSpeed, audioActive);
            }
        }
        else
        {
            if (wheelSlipping)
            {
                if (averageVelocity.magnitude > 8f && !tireSparks.isPlaying)
                    tireSparks.Play(true);
            }
            else
            {
                tireSparks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            SetVehicleAudioProperties(skiddingAudio, wheelSlipping, 0f, tyreStress, 3f, true, 1f);
        }
    }

    public void SetCarDashboardDials()
    {
        // Event when key is added/removed (battery is on)
        if (lastKeyInIgnition != keyIsInIgnition)
        {
            lastKeyInIgnition = keyIsInIgnition;
            Color dialColor = keyIsInIgnition ? new Color(0.33f, 0.84f, 0.83f) : new Color(0.11f, 0.33f, 0.33f);
            Material[] dialMat = keyIsInIgnition ? [dialOnMat] : [dialOffMat];
            screensContainer.SetActive(keyIsInIgnition);
            leftDial.materials = dialMat;
            rightDial.materials = dialMat;
            leftDialTickmarks.color = dialColor;
            rightDialTickmarks.color = dialColor;
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
        //rightDialTransform.localEulerAngles = new Vector3(Mathf.Lerp(11f, -203f, Mathf.Abs((BackLeftWheel.rotationSpeed + BackRightWheel.rotationSpeed) / 2f) / 5000f), 270f, -90f);
        //leftDialTransform.localRotation = Quaternion.Euler(Mathf.Lerp(-219f, -10f, Mathf.Abs(EngineRPM) / (MaxEngineRPM / 2.5f)) + (ignitionStarted ? 25f : 0f), 90f, 90f);
        // Gauges
        if (BackLeftWheel != null && BackRightWheel != null)
        {
            leftDialTransform.localRotation = Quaternion.Euler(Mathf.Lerp(-219f, -10f, Mathf.Abs(EngineRPM) / (MaxEngineRPM / 2.5f)) + (ignitionStarted ? 25f : 0f), 90f, 90f);
            rightDialTransform.localRotation = Quaternion.Euler(Mathf.Lerp(11f, -203f, Mathf.Abs(speedometerFloat) / 5000f), 270f, -90f);
        }
    }



    // Networking stuff
    [ServerRpc(RequireOwnership = false)]
    public void SetTyreStressOnServerRpc(float wheelStress, bool wheelSkidding)
    {
        SetTyreStressOnLocalClientRpc(wheelStress, wheelSkidding);
    }

    [ClientRpc]
    public void SetTyreStressOnLocalClientRpc(float wheelStress, bool wheelSkidding)
    {
        if (IsOwner)
            return;

        tyreStress = wheelStress;
        wheelSlipping = wheelSkidding;
    }



    // Physics & Update function
    public new void FixedUpdate()
    {
        for (int i = 0; i < wheels.Count; i++)
        {
            if (wheels[i].GetGroundHit(out var hit))
            {
                wheelHits[i] = hit;
            }
            else
            {
                wheelHits[i] = default;
            }
        }

        if (!StartOfRound.Instance.inShipPhase && !loadedVehicleFromSave && !hasDeliveredVehicle)
        {
            if (itemShip == null && References.itemShip != null)
                itemShip = References.itemShip;

            if (itemShip != null)
            {
                if (itemShip.untetheredVehicle)
                {
                    inDropshipAnimation = false;

                    mainRigidbody.MovePosition(itemShip.deliverVehiclePoint.position);
                    mainRigidbody.MoveRotation(itemShip.deliverVehiclePoint.rotation);

                    syncedPosition = transform.position;
                    syncedRotation = transform.rotation;

                    hasBeenSpawned = true;
                    hasDeliveredVehicle = true;
                }
                else if (itemShip.deliveringVehicle)
                {
                    inDropshipAnimation = true;

                    mainRigidbody.isKinematic = true;
                    mainRigidbody.MovePosition(itemShip.deliverVehiclePoint.position);
                    mainRigidbody.MoveRotation(itemShip.deliverVehiclePoint.rotation);

                    syncedPosition = transform.position;
                    syncedRotation = transform.rotation;
                }
            }
            else if (itemShip == null)
            {
                inDropshipAnimation = false;

                mainRigidbody.isKinematic = true;
                mainRigidbody.MovePosition(StartOfRound.Instance.notSpawnedPosition.position + Vector3.forward * 30f);

                syncedPosition = transform.position;
                syncedRotation = transform.rotation;
            }
        }
        if (magnetedToShip)
        {
            syncedPosition = transform.position;
            syncedRotation = transform.rotation;
            mainRigidbody.MovePosition(Vector3.Lerp(magnetStartPosition, StartOfRound.Instance.elevatorTransform.position + magnetTargetPosition, magnetPositionCurve.Evaluate(magnetTime)));
            mainRigidbody.MoveRotation(Quaternion.Lerp(magnetStartRotation, magnetTargetRotation, magnetRotationCurve.Evaluate(magnetRotationTime)));
            averageVelocityAtMagnetStart = Vector3.Lerp(averageVelocityAtMagnetStart, Vector3.ClampMagnitude(averageVelocityAtMagnetStart, 4f), 4f * Time.deltaTime);

            if (!finishedMagneting)
                magnetStartPosition += Vector3.ClampMagnitude(averageVelocityAtMagnetStart, 5f) * Time.fixedDeltaTime;
        }
        else
        {
            if (!base.IsOwner && !inDropshipAnimation)
            {
                gameObject.GetComponent<Rigidbody>().isKinematic = true;
                Mathf.Clamp(syncSpeedMultiplier * Vector3.Distance(transform.position, syncedPosition), 1.3f, 300f);
                Vector3 position = Vector3.Lerp(transform.position, syncedPosition, Time.fixedDeltaTime * syncSpeedMultiplier);
                mainRigidbody.MovePosition(position);
                mainRigidbody.MoveRotation(Quaternion.Lerp(transform.rotation, syncedRotation, syncRotationSpeed));
                truckVelocityLastFrame = mainRigidbody.velocity;
            }
        }

        averageVelocity += (mainRigidbody.velocity - averageVelocity) / (movingAverageLength + 1);
        ragdollPhysicsBody.Move(
            transform.position, 
            transform.rotation);
        windwiperPhysicsBody1.Move(
            windwiper1.position, 
            windwiper1.rotation);
        windwiperPhysicsBody2.Move(
            windwiper2.position, 
            windwiper2.rotation);

        if (!hasBeenSpawned || carDestroyed)
            return;

        Vector3 uprightForce = Vector3.Cross(Quaternion.AngleAxis(mainRigidbody.angularVelocity.magnitude * 57.29578f * stability / speed, mainRigidbody.angularVelocity) * base.transform.up, Vector3.up);
        mainRigidbody.AddTorque(uprightForce * speed * speed);

        if (FrontLeftWheel.enabled && FrontRightWheel.enabled && BackLeftWheel.enabled && BackRightWheel.enabled)
        {
            FrontLeftWheel.steerAngle = maxSteeringAngle * steeringWheelAnimFloat;
            FrontRightWheel.steerAngle = maxSteeringAngle * steeringWheelAnimFloat;
            if (IsOwner)
            {
                wheelRPM = (FrontLeftWheel.rpm + FrontRightWheel.rpm + BackLeftWheel.rpm + BackRightWheel.rpm) / 4f;
                float vehicleStress = 0f;
                if (ignitionStarted)
                {
                    if (brakePedalPressed)
                    {
                        FrontLeftWheel.brakeTorque = 2000f;
                        FrontRightWheel.brakeTorque = 2000f;
                        BackLeftWheel.brakeTorque = 2000f;
                        BackRightWheel.brakeTorque = 2000f;
                    }
                    else
                    {
                        FrontLeftWheel.brakeTorque = 0f;
                        FrontRightWheel.brakeTorque = 0f;
                        BackLeftWheel.brakeTorque = 0f;
                        BackRightWheel.brakeTorque = 0f;
                    }
                }
                if (drivePedalPressed && ignitionStarted)
                {
                    switch (gear)
                    {
                        case CarGearShift.Park:
                            {
                                vehicleStress += 1.2f;
                                lastStressType += "; Accelerating while in park";
                                break;
                            }
                        case CarGearShift.Reverse:
                            {
                                FrontLeftWheel.motorTorque = 0f - EngineTorque;
                                FrontRightWheel.motorTorque = 0f - EngineTorque;
                                BackLeftWheel.motorTorque = 0f - EngineTorque;
                                BackRightWheel.motorTorque = 0f - EngineTorque;
                                break;
                            }
                        case CarGearShift.Drive:
                            {
                                FrontLeftWheel.motorTorque = Mathf.Clamp(Mathf.MoveTowards(FrontLeftWheel.motorTorque, EngineTorque, carAcceleration * Time.deltaTime), 325f, 1000f);
                                FrontRightWheel.motorTorque = FrontLeftWheel.motorTorque;
                                BackLeftWheel.motorTorque = FrontLeftWheel.motorTorque;
                                BackRightWheel.motorTorque = FrontLeftWheel.motorTorque;
                                break;
                            }
                    }
                }
                else if (!drivePedalPressed && ignitionStarted && gear != CarGearShift.Park)
                {
                    float idleDirection = 1f;
                    if (gear == CarGearShift.Reverse)
                    {
                        idleDirection = -1f;
                    }
                    FrontLeftWheel.motorTorque = idleSpeed * idleDirection;
                    FrontRightWheel.motorTorque = idleSpeed * idleDirection;
                    BackLeftWheel.motorTorque = idleSpeed * idleDirection;
                    BackRightWheel.motorTorque = idleSpeed * idleDirection;
                }
                if (gear == CarGearShift.Park || !ignitionStarted)
                {
                    if (BackLeftWheel.isGrounded && BackRightWheel.isGrounded && averageVelocity.magnitude > 18f)
                    {
                        vehicleStress += Mathf.Clamp(((averageVelocity.magnitude * 165f) - 200f) / 150f, 0f, 4f);
                        lastStressType += "; In park while at high speed";
                    }
                    FrontLeftWheel.motorTorque = 0f;
                    FrontRightWheel.motorTorque = 0f;
                    BackLeftWheel.motorTorque = 0f;
                    BackRightWheel.motorTorque = 0f;

                    FrontLeftWheel.brakeTorque = 2000f;
                    FrontRightWheel.brakeTorque = 2000f;
                    BackLeftWheel.brakeTorque = 2000f;
                    BackRightWheel.brakeTorque = 2000f;
                }
                SetInternalStress(vehicleStress);
                stressPerSecond = vehicleStress;
            }
            else
            {
                wheelRPM = Mathf.Lerp(wheelRPM, syncedWheelRPM, Time.deltaTime * 8f);

                FrontLeftWheel.motorTorque = syncedMotorTorque;
                FrontRightWheel.motorTorque = syncedMotorTorque;
                BackLeftWheel.motorTorque = syncedMotorTorque;
                BackRightWheel.motorTorque = syncedMotorTorque;

                FrontLeftWheel.brakeTorque = syncedBrakeTorque;
                FrontRightWheel.brakeTorque = syncedBrakeTorque;
                BackLeftWheel.brakeTorque = syncedBrakeTorque;
                BackRightWheel.brakeTorque = syncedBrakeTorque;
            }
        }

        if (ignitionStarted)
        {
            if (FrontLeftWheel.enabled && FrontRightWheel.enabled && BackLeftWheel.enabled && BackRightWheel.enabled)
            {
                forwardWheelSpeed = 6000f;
                reverseWheelSpeed = -6000f;

                FrontLeftWheel.rotationSpeed = Mathf.Clamp(FrontLeftWheel.rotationSpeed, reverseWheelSpeed, forwardWheelSpeed);
                FrontRightWheel.rotationSpeed = Mathf.Clamp(FrontRightWheel.rotationSpeed, reverseWheelSpeed, forwardWheelSpeed);
                BackLeftWheel.rotationSpeed = Mathf.Clamp(BackLeftWheel.rotationSpeed, reverseWheelSpeed, forwardWheelSpeed);
                BackRightWheel.rotationSpeed = Mathf.Clamp(BackRightWheel.rotationSpeed, reverseWheelSpeed, forwardWheelSpeed);
            }
            if (IsOwner)
            {
                EngineRPM = Mathf.Abs(((FrontLeftWheel.rpm + FrontRightWheel.rpm + BackLeftWheel.rpm + BackRightWheel.rpm) / 4f));
            }
            else
            {
                EngineRPM = Mathf.Lerp(EngineRPM, syncedEngineRPM, Time.deltaTime * 5f);
            }
        }
        else
        {
            EngineRPM = Mathf.Lerp(EngineRPM, 0f, 3f * Time.deltaTime);
        }
    }

    public new void Update()
    {
        if (destroyNextFrame)
        {
            if (IsOwner)
            {
                UnityEngine.Object.Destroy(base.windwiperPhysicsBody1.gameObject);
                UnityEngine.Object.Destroy(base.windwiperPhysicsBody2.gameObject);
                UnityEngine.Object.Destroy(base.ragdollPhysicsBody.gameObject);
                UnityEngine.Object.Destroy(base.gameObject);
            }
            return;
        }
        if (NetworkObject != null && !NetworkObject.IsSpawned)
        {
            physicsRegion.disablePhysicsRegion = true;

            if (StartOfRound.Instance.CurrentPlayerPhysicsRegions.Contains(physicsRegion))
                StartOfRound.Instance.CurrentPlayerPhysicsRegions.Remove(physicsRegion);

            if (localPlayerInControl || localPlayerInPassengerSeat || localPlayerInBLSeat || localPlayerInBRSeat || localPlayerInMiddleSeat)
                GameNetworkManager.Instance.localPlayerController.CancelSpecialTriggerAnimations();

            GrabbableObject[] itemsInTruck = physicsRegion.physicsTransform.GetComponentsInChildren<GrabbableObject>();
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

        if (!hasBeenSpawned)
            return;

        if (magnetedToShip)
        {
            if (!StartOfRound.Instance.magnetOn)
            {
                magnetedToShip = false;
                StartOfRound.Instance.isObjectAttachedToMagnet = false;
                CollectItemsInHauler();
                return;
            }
            magnetTime = Mathf.Min(magnetTime + Time.deltaTime, 1f);
            magnetRotationTime = Mathf.Min(magnetTime + Time.deltaTime * 0.75f, 1f);
            // Subjective change, but I feel like the hauler, with its enormous HP, shouldn't regen in orbit (i'll add health saving later)
            //if (StartOfRound.Instance.inShipPhase)
            //{
            //    carHP = baseCarHP;
            //}
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
            if (IsOwner && !carDestroyed && !StartOfRound.Instance.isObjectAttachedToMagnet && StartOfRound.Instance.magnetOn && Vector3.Distance(transform.position, StartOfRound.Instance.magnetPoint.position) < 10f && !Physics.Linecast(transform.position, StartOfRound.Instance.magnetPoint.position, 256, QueryTriggerInteraction.Ignore))
            {
                StartMagneting();
                return;
            }
            if (IsOwner)
            {
                if (enabledCollisionForAllPlayers)
                {
                    enabledCollisionForAllPlayers = false;
                    DisableVehicleCollisionForAllPlayers();
                }
                if (!inDropshipAnimation) SyncCarPositionToOtherClients();
            }
            else
            {
                if (!enabledCollisionForAllPlayers)
                {
                    enabledCollisionForAllPlayers = true;
                    EnableVehicleCollisionForAllPlayers();
                }
            }
        }

        ReactToDamage();

        if (carDestroyed)
            return;

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

        driverSideDoorTrigger.interactable = Time.realtimeSinceStartup - timeSinceSpringingDriverSeat > 1.6f;
        passengerSideDoorTrigger.interactable = Time.realtimeSinceStartup - timeSinceSpringingDriverSeat > 1.6f;
        BLSideDoorTrigger.interactable = Time.realtimeSinceStartup - timeSinceSpringingDriverSeat > 1.6f;
        BRSideDoorTrigger.interactable = Time.realtimeSinceStartup - timeSinceSpringingDriverSeat > 1.6f;

        if (keyIsInDriverHand && currentDriver != null)
        {
            keyObject.enabled = true;
            Transform transform = ((!localPlayerInControl) ? currentDriver.serverItemHolder : currentDriver.localItemHolder);
            keyObject.transform.rotation = transform.rotation;
            keyObject.transform.Rotate(rotationOffset);
            keyObject.transform.position = transform.position;
            Vector3 vector = positionOffset;
            vector = transform.rotation * vector;
            keyObject.transform.position += vector;
        }
        else
        {
            if (Time.realtimeSinceStartup - timeAtLastGearShift < 1.7f && currentDriver != null)
            {
                currentDriver.playerBodyAnimator.SetFloat("SA_CarMotionTime", gearStickAnimValue);
            }
            if (localPlayerInControl && ignitionStarted && keyIgnitionCoroutine == null)
            {
                GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetFloat("animationSpeed", steeringWheelAnimFloat + 0.5f);
                if (Time.realtimeSinceStartup - timeAtLastGearShift < 1.7f)
                {
                    GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger("SA_CarAnim", 5);
                }
                else
                {
                    GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger("SA_CarAnim", 1);
                }
            }
            if (keyIsInIgnition)
            {
                keyObject.enabled = true;
                if (ignitionStarted)
                {
                    keyObject.transform.position = ignitionTurnedPosition.position;
                    keyObject.transform.rotation = ignitionTurnedPosition.rotation;
                }
                else
                {
                    keyObject.transform.position = ignitionNotTurnedPosition.position;
                    keyObject.transform.rotation = ignitionNotTurnedPosition.rotation;
                }
                //else if (!ignitionStarted && !tryingIgnition)
                //{
                //    keyObject.transform.position = ignitionNotTurnedPosition.position;
                //    keyObject.transform.rotation = ignitionNotTurnedPosition.rotation;
                //}
                //else if (tryingIgnition)
                //{
                //    keyObject.transform.position = ignitionTurnedPosition.position;
                //    keyObject.transform.rotation = ignitionTurnedPosition.rotation;
                //}
            }
            else
            {
                keyObject.enabled = false;
            }
        }

        SetCarEffects(steeringAnimValue);

        if (localPlayerInControl && ignitionStarted)
        {
            GetVehicleInput();
            return;
        }
        moveInputVector = Vector2.zero;
    }

    private new void ReactToDamage()
    {
        healthMeter.localScale = new Vector3(1f, 1f, Mathf.Lerp(healthMeter.localScale.z, Mathf.Clamp((float)carHP / (float)baseCarHP, 0.01f, 1f), 6f * Time.deltaTime));

        if (carHP < (baseCarHP/3.75) && Time.realtimeSinceStartup - timeAtLastDamage > 8f)
        {
            timeAtLastDamage = Time.realtimeSinceStartup;
            carHP++;
        }

        if (!IsOwner)
            return;

        if (carHP < 3)
        {
            if (!isHoodOnFire)
            {
                if (!hoodPoppedUp)
                {
                    hoodPoppedUp = true;
                    SetHoodOpenLocalClient(setOpen: hoodPoppedUp);
                }
                isHoodOnFire = true;
                hoodFireAudio.Play();
                hoodFireParticle.Play();
                SetHoodOnFireServerRpc(isHoodOnFire);
            }
        }
        else if (isHoodOnFire)
        {
            hoodPoppedUp = false;
            isHoodOnFire = false;
            hoodFireAudio.Stop();
            hoodFireParticle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
            SetHoodOnFireServerRpc(isHoodOnFire);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetHoodOnFireServerRpc(bool onFire)
    {
        SetHoodOnFireClientRpc(onFire);
    }

    [ClientRpc]
    public void SetHoodOnFireClientRpc(bool onFire)
    {
        if (IsOwner)
            return;

        isHoodOnFire = onFire;
        if (isHoodOnFire)
        {
            hoodFireAudio.Play();
            hoodFireParticle.Play();
            return;
        }
        hoodFireAudio.Stop();
        hoodFireParticle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
    }

    public new void LateUpdate()
    {
        if (carDestroyed)
            return;

        if (localPlayerInControl && !setControlTips)
        {
            setControlTips = true;
            HUDManager.Instance.ChangeControlTipMultiple(haulerTooltips, false, null);
        }

        // Mute hauler audios while in orbit
        if (magnetedToShip && (StartOfRound.Instance.inShipPhase || !StartOfRound.Instance.shipDoorsEnabled))
        {
            hornAudio.mute = true;
            engineAudio1.mute = true;
            engineAudio2.mute = true;
            ChimeAudio.mute = true;
            rollingAudio.mute = true;
            skiddingAudio.mute = true;

            TrainHornAudio.mute = true;
            TrainHornAudioDistant.mute = true;
            roofRainAudio.mute = true;

            turbulenceAudio.mute = true;
            hoodFireAudio.mute = true;
            extremeStressAudio.mute = true;
            pushAudio.mute = true;
            radioAudio.mute = true;
            radioInterference.mute = true;
        }
        else
        {
            hornAudio.mute = false;
            engineAudio1.mute = false;
            engineAudio2.mute = false;
            ChimeAudio.mute = false;
            rollingAudio.mute = false;
            skiddingAudio.mute = false;

            TrainHornAudio.mute = false;
            TrainHornAudioDistant.mute = false;
            roofRainAudio.mute = false;

            turbulenceAudio.mute = false;
            hoodFireAudio.mute = false;
            extremeStressAudio.mute = false;
            pushAudio.mute = false;
            radioAudio.mute = false;
            radioInterference.mute = false;
        }

        if (currentDriver != null && References.lastDriver != currentDriver && !magnetedToShip)
            References.lastDriver = currentDriver;
    }

    public new void StartMagneting()
    {
        gameObject.GetComponent<Rigidbody>().isKinematic = true;
        magnetTime = 0f;
        magnetRotationTime = 0f;
        StartOfRound.Instance.isObjectAttachedToMagnet = true;
        StartOfRound.Instance.attachedVehicle = this;
        magnetedToShip = true;
        averageVelocityAtMagnetStart = averageVelocity;
        RoundManager.Instance.tempTransform.eulerAngles = new Vector3(0f, transform.eulerAngles.y, 0f);
        float num = Vector3.Angle(RoundManager.Instance.tempTransform.forward, -StartOfRound.Instance.magnetPoint.forward);
        Vector3 eulerAngles = transform.eulerAngles;
        if (num < 47f || num > 133f)
        {
            if (eulerAngles.y < 0f)
            {
                eulerAngles.y -= 46f - num;
            }
            else
            {
                eulerAngles.y += 46f - num;
            }
        }
        eulerAngles.y = Mathf.Round(eulerAngles.y / 90f) * 90f;
        eulerAngles.z = Mathf.Round(eulerAngles.z / 90f) * 90f;
        eulerAngles.x += UnityEngine.Random.Range(-5f, 5f);
        magnetTargetRotation = Quaternion.Euler(eulerAngles);
        magnetStartRotation = transform.rotation;
        Quaternion rotation = transform.rotation;
        transform.rotation = magnetTargetRotation;
        magnetTargetPosition = boundsCollider.ClosestPoint(StartOfRound.Instance.magnetPoint.position) - transform.position;
        if (magnetTargetPosition.y >= boundsCollider.bounds.extents.y)
        {
            magnetTargetPosition.y -= boundsCollider.bounds.extents.y / 2f;
        }
        else if (magnetTargetPosition.y <= boundsCollider.bounds.extents.y * 0.4f)
        {
            magnetTargetPosition.y += boundsCollider.bounds.extents.y / 2f;
        }
        magnetTargetPosition = StartOfRound.Instance.magnetPoint.position - magnetTargetPosition;
        magnetTargetPosition.z = Mathf.Min(-20.4f, magnetTargetPosition.z);
        magnetTargetPosition.y = Mathf.Max(2.5f, magnetStartPosition.y);
        magnetTargetPosition = StartOfRound.Instance.elevatorTransform.InverseTransformPoint(magnetTargetPosition);
        transform.rotation = rotation;
        magnetStartPosition = transform.position;

        CollectItemsInHauler();

        if (StartOfRound.Instance.inShipPhase)
        {
            return;
        }
        if (GameNetworkManager.Instance.localPlayerController == null)
        {
            return;
        }
        if (!IsOwner)
        {
            return;
        }
        MagnetCarServerRpc(magnetTargetPosition, eulerAngles, (int)GameNetworkManager.Instance.localPlayerController.playerClientId);
    }

    [ServerRpc]
    public new void MagnetCarServerRpc(Vector3 targetPosition, Vector3 targetRotation, int playerWhoSent)
    {
        MagnetCarClientRpc(targetPosition, targetRotation, playerWhoSent);
    }

    [ClientRpc]
    public new void MagnetCarClientRpc(Vector3 targetPosition, Vector3 targetRotation, int playerWhoSent)
    {
        if ((int)GameNetworkManager.Instance.localPlayerController.playerClientId == playerWhoSent)
            return;

        magnetedToShip = true;
        magnetTime = 0f;
        magnetRotationTime = 0f;
        StartOfRound.Instance.isObjectAttachedToMagnet = true;
        StartOfRound.Instance.attachedVehicle = this;
        magnetStartPosition = transform.position;
        magnetStartRotation = transform.rotation;
        magnetTargetPosition = targetPosition;
        magnetTargetRotation = Quaternion.Euler(targetRotation);
        CollectItemsInHauler();
    }

    public void CollectItemsInHauler()
    {
        Collider[] array = Physics.OverlapSphere(transform.position, 25f, 64, QueryTriggerInteraction.Collide);
        for (int i = 0; i < array.Length; i++)
        {
            GrabbableObject itemInTruck = array[i].GetComponent<GrabbableObject>();
            if (itemInTruck != null && !itemInTruck.isHeld && !itemInTruck.isHeldByEnemy && array[i].transform.parent == transform)
            {
                if (References.lastDriver != null)
                {
                    References.lastDriver.SetItemInElevator(magnetedToShip, magnetedToShip, itemInTruck);
                }
                else if (References.lastDriver == null && GameNetworkManager.Instance.localPlayerController != null)
                {
                    GameNetworkManager.Instance.localPlayerController?.SetItemInElevator(magnetedToShip, magnetedToShip, itemInTruck);
                }
            }
        }
    }



    // Set radio time consistently across owner and non-owners
    [ServerRpc(RequireOwnership = false)]
    public void SyncRadioTimeServerRpc(float songTime)
    {
        SyncRadioTimeClientRpc(songTime);
    }

    [ClientRpc]
    public void SyncRadioTimeClientRpc(float songTime)
    {
        if (IsHost)
            return;

        currentSongTime = songTime;
        SetRadioTime();
    }

    public void SetRadioTime()
    {
        if (radioAudio.clip == null)
            return;

        radioAudio.time = Mathf.Clamp(currentSongTime % radioAudio.clip.length, 0.01f, radioAudio.clip.length - 0.1f);
    }

    // Additional stuff to do for the radio
    public new void SetRadioValues()
    {
        if (!radioOn)
        {
            currentSongTime = 0f;
            return;
        }

        if (IsHost)
        {
            currentSongTime += Time.deltaTime;
            if (Time.realtimeSinceStartup - timeLastSyncedRadio > 1f)
            {
                timeLastSyncedRadio = Time.realtimeSinceStartup;
                SyncRadioTimeServerRpc(currentSongTime);
            }
        }

        if (IsServer && radioAudio.isPlaying && Time.realtimeSinceStartup > radioPingTimestamp)
        {
            radioPingTimestamp = (Time.realtimeSinceStartup + 1f);
            RoundManager.Instance.PlayAudibleNoise(radioAudio.transform.position, 16f, Mathf.Min((radioAudio.volume + radioInterference.volume) * 0.5f, 0.9f), 0, false, 2692);
        }

        if (!radioAudio.isPlaying)
            radioAudio.Play();

        if (IsOwner)
        {
            float signal = UnityEngine.Random.Range(0, 100);
            float signalTurbulence = (3f - radioSignalQuality - 1.5f) * radioSignalTurbulence;
            radioSignalDecreaseThreshold = Mathf.Clamp(radioSignalDecreaseThreshold + Time.deltaTime * signalTurbulence, 0f, 100f);
            if (signal > radioSignalDecreaseThreshold)
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
                if (radioSignalQuality < 1.2f && UnityEngine.Random.Range(0, 100) < 6)
                {
                    radioSignalQuality = Mathf.Min(radioSignalQuality + 1.5f, 3f);
                    radioSignalDecreaseThreshold = Mathf.Min(radioSignalDecreaseThreshold + 30f, 100f);
                }
                SetRadioSignalQualityServerRpc((int)Mathf.Round(radioSignalQuality));
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



    // Position networking
    private void SyncCarPositionToOtherClients()
    {
        gameObject.GetComponent<Rigidbody>().isKinematic = false;
        if (syncCarPositionInterval >= (0.12f * (averageVelocity.magnitude / 200f)))
        {
            if (Vector3.Distance(syncedPosition, transform.position) > 0.01f)
            {
                syncCarPositionInterval = 0f;
                syncedPosition = transform.position;
                syncedRotation = transform.rotation;
                syncedMovementSpeed = averageVelocity;
                SyncCarPositionServerRpc(transform.position, transform.eulerAngles, averageVelocity);
                return;
            }
            if (Vector3.Angle(transform.forward, syncedRotation * Vector3.forward) > 2f)
            {
                syncCarPositionInterval = 0f;
                syncedPosition = transform.position;
                syncedRotation = transform.rotation;
                syncedMovementSpeed = averageVelocity;
                SyncCarPositionServerRpc(transform.position, transform.eulerAngles, averageVelocity);
                return;
            }
        }
        else
        {
            syncCarPositionInterval += Time.deltaTime;
        }
        syncCarPositionInterval = Mathf.Clamp(syncCarPositionInterval, 0.002f, 0.2f);
    }

    [ServerRpc]
    public void SyncCarPositionServerRpc(Vector3 carPosition, Vector3 carRotation, Vector3 averageSpeed)
    {
        SyncCarPositionClientRpc(carPosition, carRotation, averageSpeed);
    }

    [ClientRpc]
    public void SyncCarPositionClientRpc(Vector3 carPosition, Vector3 carRotation, Vector3 averageSpeed)
    {
        if (IsOwner)
            return;

        syncedPosition = carPosition;
        syncedRotation = Quaternion.Euler(carRotation);
        syncedMovementSpeed = averageSpeed;
    }



    // Vehicle inputs
    public new void GetVehicleInput()
    {
        if (currentDriver == null)
            return;

        if (currentDriver.isTypingChat)
            return;

        if (currentDriver.quickMenuManager.isMenuOpen)
            return;

        moveInputVector = IngamePlayerSettings.Instance.playerInput.actions.FindAction("Move").ReadValue<Vector2>();
        steeringAnimValue = moveInputVector.x;
        drivePedalPressed = moveInputVector.y > 0.1f;
        brakePedalPressed = moveInputVector.y < -0.1f;

        // Automatically center the wheel, if the user has it their config set to true
        if (moveInputVector.x == 0f && (CompanyHauler.BoundConfig.haulerAutoCenter.Value))
        {
            steeringWheelAnimFloat = Mathf.MoveTowards(steeringWheelAnimFloat, 0, steeringWheelTurnSpeed * Time.deltaTime / 6f);
        }
    }



    // Ignition stuff
    public new void StartTryCarIgnition()
    {
        if (!localPlayerInControl)
            return;

        if (ignitionStarted)
            return;

        if (keyIgnitionCoroutine != null)
        {
            StopCoroutine(keyIgnitionCoroutine);
        }
        keyIgnitionCoroutine = StartCoroutine(TryIgnition(isLocalDriver: true));
        TryIgnitionServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId, keyIsInIgnition);
    }

    private new IEnumerator TryIgnition(bool isLocalDriver)
    {
        if (keyIsInIgnition)
        {
            if (isLocalDriver)
            {
                if (GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.GetInteger("SA_CarAnim") == 3)
                {
                    GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger("SA_CarAnim", 2);
                }
                else
                {
                    GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger("SA_CarAnim", 12);
                }
            }
            yield return new WaitForSeconds(0.02f);
            currentDriver?.movementAudio.PlayOneShot(twistKey);
            SetKeyIgnitionValues(trying: true, keyInHand: true, keyInSlot: true);
            yield return new WaitForSeconds(0.1467f);
        }
        else
        {
            if (isLocalDriver)
            {
                GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger("SA_CarAnim", 2);
            }

            SetKeyIgnitionValues(trying: false, keyInHand: true, keyInSlot: false);
            yield return new WaitForSeconds(0.6f);
            currentDriver?.movementAudio.PlayOneShot(insertKey);
            SetKeyIgnitionValues(trying: false, keyInHand: true, keyInSlot: true);
            SetFrontCabinLightOn(setOn: keyIsInIgnition);
            ChimeAudio.PlayOneShot(chimeSound);
            yield return new WaitForSeconds(0.2f);
            currentDriver?.movementAudio.PlayOneShot(twistKey);
            SetKeyIgnitionValues(trying: true, keyInHand: true, keyInSlot: true);
        }

        SetKeyIgnitionValues(trying: true, keyInHand: true, keyInSlot: true);
        SetFrontCabinLightOn(setOn: keyIsInIgnition);

        if (!isLocalDriver)
        {
            keyIgnitionCoroutine = null;
            yield break;
        }

        engineAudio1.Stop();
        engineAudio1.clip = revEngineStart;
        engineAudio1.volume = 0.7f;
        engineAudio1.PlayOneShot(engineRev);
        carEngine1AudioActive = true;
        RevCarServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);

        yield return new WaitForSeconds(UnityEngine.Random.Range(0.4f, 1.1f));
        if ((float)UnityEngine.Random.Range(0, 100) < chanceToStartIgnition)
        {
            GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger("SA_CarAnim", 1);
            SetKeyIgnitionValues(trying: false, keyInHand: false, keyInSlot: true);
            SetIgnition(started: true);
            SetFrontCabinLightOn(setOn: keyIsInIgnition);
            StartIgnitionServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
        }
        else
        {
            chanceToStartIgnition += 15f;
        }
        keyIgnitionCoroutine = null;
    }

    [ServerRpc(RequireOwnership = false)]
    public new void TryIgnitionServerRpc(int driverId, bool setKeyInSlot)
    {
        TryIgnitionClientRpc(driverId, setKeyInSlot);
    }

    [ClientRpc]
    public new void TryIgnitionClientRpc(int driverId, bool setKeyInSlot)
    {
        if ((int)GameNetworkManager.Instance.localPlayerController.playerClientId == driverId)
            return;

        if (ignitionStarted)
            return;

        if (keyIgnitionCoroutine != null)
        {
            StopCoroutine(keyIgnitionCoroutine);
        }
        SetKeyIgnitionValues(trying: false, keyInHand: false, keyInSlot: setKeyInSlot);
        SetFrontCabinLightOn(setKeyInSlot);
        keyIgnitionCoroutine = StartCoroutine(TryIgnition(isLocalDriver: false));
    }



    [ServerRpc(RequireOwnership = false)]
    public new void RevCarServerRpc(int driverId)
    {
        RevCarClientRpc(driverId);
    }

    [ClientRpc]
    public new void RevCarClientRpc(int driverId)
    {
        if ((int)GameNetworkManager.Instance.localPlayerController.playerClientId == driverId)
            return;

        engineAudio1.Stop();
        engineAudio1.clip = revEngineStart;
        engineAudio1.volume = 0.7f;
        engineAudio1.PlayOneShot(engineRev);
        carEngine1AudioActive = true;
    }



    public new void CancelTryCarIgnition()
    {
        if (!localPlayerInControl)
            return;

        if (ignitionStarted)
            return;

        GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger("SA_CarAnim", keyIsInIgnition ? 3 : 0);
        CancelIgnitionAnimation(ignitionOn: false);
        SetFrontCabinLightOn(setOn: keyIsInIgnition);
        CancelTryIgnitionServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId, keyIsInIgnition);
    }

    [ServerRpc(RequireOwnership = false)]
    public new void CancelTryIgnitionServerRpc(int driverId, bool setKeyInSlot)
    {
        CancelTryIgnitionClientRpc(driverId, setKeyInSlot);
    }

    [ClientRpc]
    public new void CancelTryIgnitionClientRpc(int driverId, bool setKeyInSlot)
    {
        if ((int)GameNetworkManager.Instance.localPlayerController.playerClientId == driverId)
            return;

        // Account for netlag when the key is first inserted
        if (setKeyInSlot == true && (keyIsInIgnition != setKeyInSlot))
        {
            currentDriver?.movementAudio.PlayOneShot(insertKey);
            ChimeAudio.PlayOneShot(chimeSound);
        }
        SetKeyIgnitionValues(trying: false, keyInHand: false, keyInSlot: setKeyInSlot);
        SetFrontCabinLightOn(setOn: keyIsInIgnition);
        CancelIgnitionAnimation(ignitionOn: false);
    }



    [ServerRpc(RequireOwnership = false)]
    public new void StartIgnitionServerRpc(int driverId)
    {
        StartIgnitionClientRpc(driverId);
    }

    [ClientRpc]
    public new void StartIgnitionClientRpc(int driverId)
    {
        if ((int)GameNetworkManager.Instance.localPlayerController.playerClientId == driverId)
            return;

        SetKeyIgnitionValues(trying: false, keyInHand: false, keyInSlot: true);
        SetIgnition(started: true);
        SetFrontCabinLightOn(setOn: keyIsInIgnition);
        CancelIgnitionAnimation(ignitionOn: true);
    }

    public new void SetIgnition(bool started)
    {
        SetFrontCabinLightOn(keyIsInIgnition);
        if (started == ignitionStarted)
        {
            return;
        }
        ignitionStarted = started;
        carEngine1AudioActive = started;
        if (started)
        {
            startKeyIgnitionTrigger.SetActive(false);
            removeKeyIgnitionTrigger.SetActive(true);
            carExhaustParticle.Play();
            engineAudio1.Stop();
            engineAudio1.PlayOneShot(engineStartSuccessful);
            engineAudio1.clip = engineRun;
            return;
        }
        startKeyIgnitionTrigger.SetActive(true);
        removeKeyIgnitionTrigger.SetActive(false);
        carExhaustParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    public new void RemoveKeyFromIgnition()
    {
        if (!localPlayerInControl)
            return;

        if (!ignitionStarted)
            return;

        if (keyIgnitionCoroutine != null)
        {
            StopCoroutine(keyIgnitionCoroutine);
        }
        keyIgnitionCoroutine = StartCoroutine(RemoveKey());
        chanceToStartIgnition = UnityEngine.Random.Range(-40, -10);
        RemoveKeyFromIgnitionServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
        GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger("SA_CarAnim", 6);
    }

    private new IEnumerator RemoveKey()
    {
        yield return new WaitForSeconds(0.26f);
        SetKeyIgnitionValues(trying: false, keyInHand: true, keyInSlot: false);
        currentDriver?.movementAudio.PlayOneShot(removeKey);
        SetIgnition(started: false);
        yield return new WaitForSeconds(0.73f);
        SetKeyIgnitionValues(trying: false, keyInHand: false, keyInSlot: false);
        keyIgnitionCoroutine = null;
    }

    [ServerRpc(RequireOwnership = false)]
    public new void RemoveKeyFromIgnitionServerRpc(int driverId)
    {
        RemoveKeyFromIgnitionClientRpc(driverId);
    }

    [ClientRpc]
    public new void RemoveKeyFromIgnitionClientRpc(int driverId)
    {
        if ((int)GameNetworkManager.Instance.localPlayerController.playerClientId == driverId)
            return;

        if (!ignitionStarted)
            return;

        if (keyIgnitionCoroutine != null)
        {
            StopCoroutine(keyIgnitionCoroutine);
        }
        keyIgnitionCoroutine = StartCoroutine(RemoveKey());
    }



    public void CancelIgnitionAnimation(bool ignitionOn)
    {
        if (keyIgnitionCoroutine != null)
        {
            StopCoroutine(keyIgnitionCoroutine);
            keyIgnitionCoroutine = null;
        }
        carEngine1AudioActive = ignitionOn;
        keyIsInDriverHand = false;
        tryingIgnition = false;
    }

    public void SetKeyIgnitionValues(bool trying, bool keyInHand, bool keyInSlot)
    {
        tryingIgnition = trying;
        keyIsInDriverHand = keyInHand;
        keyIsInIgnition = keyInSlot;
    }

    // BACK-MIDDLE PASSENGER METHODS //////////////////////////

    public void OnMiddleExit()
    {
        MiddleSeatTrigger.interactable = true;
        localPlayerInMiddleSeat = false;
        currentMiddle = null!;
        SetVehicleCollisionForPlayer(setEnabled: true, GameNetworkManager.Instance.localPlayerController);
        Middle_LeaveVehicleServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId, GameNetworkManager.Instance.localPlayerController.transform.position);
    }

    public void ExitMiddleSideSeat(bool isLeftSeat)
    {
        if (localPlayerInMiddleSeat)
        {
            int num = CanExitBackSeats(isLeftSeat);
            Transform[] exitPointList = isLeftSeat ? BL_ExitPoints : BR_ExitPoints;
            if (num != -1)
            {
                GameNetworkManager.Instance.localPlayerController.TeleportPlayer(exitPointList[num].position);
            }
            else
            {
                GameNetworkManager.Instance.localPlayerController.TeleportPlayer(exitPointList[1].position);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void Middle_LeaveVehicleServerRpc(int playerId, Vector3 exitPoint)
    {
        Middle_LeaveVehicleClientRpc(playerId, exitPoint);
    }

    [ClientRpc]
    public void Middle_LeaveVehicleClientRpc(int playerId, Vector3 exitPoint)
    {
        PlayerControllerB playerControllerB = StartOfRound.Instance.allPlayerScripts[playerId];
        if (!(playerControllerB == GameNetworkManager.Instance.localPlayerController))
        {
            playerControllerB.TeleportPlayer(exitPoint);
            currentMiddle = null!;
            if (!base.IsOwner)
            {
                SetVehicleCollisionForPlayer(setEnabled: true, GameNetworkManager.Instance.localPlayerController);
            }
            MiddleSeatTrigger.interactable = true;
        }
    }

    public void SetMiddlePassengerInCar(PlayerControllerB player)
    {
        if (player == GameNetworkManager.Instance.localPlayerController)
        {
            localPlayerInMiddleSeat = true;
            //SetVehicleCollisionForPlayer(false, player);
            int passengerId = (int)player.playerClientId;
            SetVehicleCollisionForPlayerServerRpc(false, passengerId);
        }
        else
        {
            MiddleSeatTrigger.interactable = false;
        }
        currentMiddle = player;
    }

    // BACK-LEFT PASSENGER METHODS //////////////////////////

    public void OnBLExit()
    {
        BLSeatTrigger.interactable = true;
        localPlayerInBLSeat = false;
        currentBL = null!;
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
            currentBL = null!;
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
            SetVehicleCollisionForPlayerServerRpc(false, passengerId);
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
        currentBR = null!;
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
            currentBR = null!;
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
            SetVehicleCollisionForPlayerServerRpc(false, passengerId);
        }
        else
        {
            BRSeatTrigger.interactable = false;
        }
        currentBR = player;
    }

    // The 2 below methods disable collisions for passengers that enter

    [ServerRpc(RequireOwnership = false)]
    public void SetVehicleCollisionForPlayerServerRpc(bool setEnabled, int passengerId)
    {
        SetVehicleCollisionForPlayerClientRpc(setEnabled, passengerId);
    }

    [ClientRpc]
    public void SetVehicleCollisionForPlayerClientRpc(bool setEnabled, int passengerId)
    {
        PlayerControllerB passengerPlayer = StartOfRound.Instance.allPlayerScripts[passengerId];
        SetVehicleCollisionForPlayer(setEnabled: setEnabled, passengerPlayer);
    }

    // Interestingly, this is an oversight for the Cruiser passenger, which needs to be fixed for the Hauler
    public new void SetPassengerInCar(PlayerControllerB player)
    {
        if (player == GameNetworkManager.Instance.localPlayerController)
        {
            //localPlayerInPassengerSeat = true;
            int passengerId = (int)player.playerClientId;
            SetVehicleCollisionForPlayerServerRpc(false, passengerId);
        }
        base.SetPassengerInCar(player);
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



    // Damage stuff...
    // Scandal Note - just using the new keyword does not mean it "overrides" the function, and therefore just plopping code in a function that just uses the new keyword
    // (without being explicitly called from this controller) will never be called unless (called from here, or redirected by a harmony prefix)
    public new void OnCollisionEnter(Collision collision)
    {
        if (!IsOwner || 
            magnetedToShip || 
            !hasBeenSpawned || 
            collision.collider.gameObject.layer != 8)
        {
            return;
        }

        float carBumpForce = 0f;
        int carBumpForceContacts = collision.GetContacts(contacts);
        Vector3 zero = Vector3.zero;
        for (int i = 0; i < carBumpForceContacts; i++)
        {
            if (contacts[i].impulse.magnitude > carBumpForce)
            {
                carBumpForce = contacts[i].impulse.magnitude;
            }
            zero += contacts[i].point;
        }

        zero /= (float)carBumpForceContacts;
        carBumpForce /= Time.fixedDeltaTime;
        if (carBumpForce < minimalBumpForce || averageVelocity.magnitude < 4f)
        {
            if (carBumpForceContacts > 3 && averageVelocity.magnitude > 2.5f)
            {
                SetInternalStress(0.35f);
                lastStressType = "Scraping";
            }
            return;
        }

        float setVolume = 0.5f;
        int audioType = -1;
        if (averageVelocity.magnitude > 27f)
        {
            if (carHP < 3)
            {
                DealPermanentDamage(Mathf.Max(carHP - 1, 2));
                return;
            }
            DealPermanentDamage(carHP - 2);
        }
        if (carBumpForce > maximumBumpForce && averageVelocity.magnitude > 11f)
        {
            audioType = 2;
            setVolume = Mathf.Clamp((carBumpForce - maximumBumpForce) / 20000f, 0.8f, 1f);
            setVolume = Mathf.Clamp(setVolume + UnityEngine.Random.Range(-0.15f, 0.25f), 0.7f, 1f);
            DealPermanentDamage(2);
        }
        else if (carBumpForce > mediumBumpForce && averageVelocity.magnitude > 3f)
        {
            audioType = 1;
            setVolume = Mathf.Clamp((carBumpForce - mediumBumpForce) / (maximumBumpForce - mediumBumpForce), 0.67f, 1f);
            setVolume = Mathf.Clamp(setVolume + UnityEngine.Random.Range(-0.15f, 0.25f), 0.5f, 1f);
            DealPermanentDamage(1);
        }
        else if (averageVelocity.magnitude > 1.5f)
        {
            audioType = 0;
            setVolume = Mathf.Clamp((carBumpForce - mediumBumpForce) / (maximumBumpForce - mediumBumpForce), 0.25f, 1f);
            setVolume = Mathf.Clamp(setVolume + UnityEngine.Random.Range(-0.15f, 0.25f), 0.25f, 1f);
        }
        if (audioType != -1)
        {
            PlayCollisionAudio(zero, audioType, setVolume);
            if (carBumpForce > maximumBumpForce + 10000f && averageVelocity.magnitude > 19f)
            {
                DamagePlayerInVehicle(Vector3.ClampMagnitude(-collision.relativeVelocity, 60f), averageVelocity.magnitude);
                BreakWindshield();
                CarCollisionServerRpc(Vector3.ClampMagnitude(-collision.relativeVelocity, 60f), averageVelocity.magnitude);
                DealPermanentDamage(2);
            }
            else
            {
                CarBumpServerRpc(Vector3.ClampMagnitude(-collision.relativeVelocity, 40f));
            }
        }
    }

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
                CarBumpServerRpc(averageVelocity * 0.7f);
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
                        enemyHitSpeed = 6f;
                        _ = carReactToPlayerHitMultiplier;
                    }
                    else if (obstacleSize <= 2f)
                    {
                        enemyHitSpeed = 16f;
                        _ = carReactToPlayerHitMultiplier;
                    }
                    else
                    {
                        enemyHitSpeed = 21f;
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
                            CarBumpServerRpc(averageVelocity);
                            mainRigidbody.velocity = Vector3.Normalize(-impulse * 100000000f) * 9f;
                            PlayerControllerB playerControllerB = ((currentDriver == null) ? currentPassenger : currentDriver);
                            if (vel.magnitude > 2f && dealDamage)
                            {
                                enemyScript.HitEnemyOnLocalClient(2, Vector3.zero, playerControllerB, playHitSFX: true, 331);
                            }
                            result = true;
                            if (obstacleSize > 2f)
                            {
                                DealPermanentDamage(1, position);
                            }
                        }
                    }
                    else
                    {
                        mainRigidbody.AddForce(Vector3.Normalize(-impulse * 1E+09f) * (carReactToPlayerHitMultiplier - 220f), ForceMode.Impulse);
                        if (dealDamage)
                        {
                            DealPermanentDamage(1, position);
                        }
                        PlayerControllerB playerWhoHit = ((currentDriver == null) ? currentPassenger : currentDriver);
                        enemyScript.HitEnemyOnLocalClient(12, Vector3.zero, playerWhoHit, false, -1);
                    }
                    PlayCollisionAudio(position, 5, 1f);
                    return result;
                }
            default:
                return false;
        }
    }



    [ServerRpc(RequireOwnership = false)]
    public new void CarBumpServerRpc(Vector3 vel)
    {
        CarBumpClientRpc(vel);
    }

    [ClientRpc]
    public new void CarBumpClientRpc(Vector3 vel)
    {
        if (GameNetworkManager.Instance.localPlayerController.physicsParent != physicsRegion.physicsTransform)
            return;

        if (localPlayerInControl || localPlayerInPassengerSeat || localPlayerInBLSeat || localPlayerInBRSeat || localPlayerInMiddleSeat)
            return;

        if (vel.magnitude >= 50f)
            return;

        GameNetworkManager.Instance.localPlayerController.externalForceAutoFade += vel;
    }



    // More damage stuff...
    private new void SetInternalStress(float carStressIncrease = 0f)
    {
        if (StartOfRound.Instance.testRoom != null || !StartOfRound.Instance.inShipPhase)
        {
            if (carStressIncrease <= 0f)
            {
                carStressChange = Mathf.Clamp(carStressChange - Time.deltaTime, -0.25f, 0.5f);
            }
            else
            {
                carStressChange = Mathf.Clamp(carStressChange + Time.deltaTime * carStressIncrease, 0f, 10f);
            }
            underExtremeStress = carStressIncrease >= 1f;
            carStress = Mathf.Clamp(carStress + carStressChange, 0f, 100f);
            if (carStress > 7f)
            {
                carStress = 0f;
                DealPermanentDamage(2);
                lastDamageType = "Stress";
            }
        }
    }

    private new void DealPermanentDamage(int damageAmount, Vector3 damagePosition = default(Vector3))
    {
        if ((StartOfRound.Instance.testRoom != null || !StartOfRound.Instance.inShipPhase) && !magnetedToShip && !carDestroyed && IsOwner)
        {
            timeAtLastDamage = Time.realtimeSinceStartup;
            carHP -= damageAmount;
            if (carHP <= 0)
            {
                DestroyCar();
                DestroyCarServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
            }
            else
            {
                DealDamageServerRpc(damageAmount, (int)GameNetworkManager.Instance.localPlayerController.playerClientId);
            }
        }
    }



    // Post destruction stuff...
    [ServerRpc(RequireOwnership = false)]
    public new void DestroyCarServerRpc(int sentByClient)
    {
        DestroyCarClientRpc(sentByClient);
    }

    [ClientRpc]
    public new void DestroyCarClientRpc(int sentByClient)
    {
        if ((int)GameNetworkManager.Instance.localPlayerController.playerClientId == sentByClient)
            return;

        if (carDestroyed)
            return;

        DestroyCar();
    }

    public new void DestroyCar()
    {
        if (carDestroyed)
            return;

        carDestroyed = true;
        magnetedToShip = false;
        StartOfRound.Instance.isObjectAttachedToMagnet = false;
        CollectItemsInHauler();
        underExtremeStress = false;
        keyObject.enabled = false;
        engineAudio1.Stop();
        engineAudio2.Stop();
        turbulenceAudio.Stop();
        rollingAudio.Stop();
        radioAudio.Stop();
        extremeStressAudio.Stop();
        honkingHorn = false;
        hornAudio.Stop();
        tireSparks.Stop();
        skiddingAudio.Stop();
        turboBoostAudio.Stop();
        turboBoostParticle.Stop();
        RoundManager.Instance.PlayAudibleNoise(engineAudio1.transform.position, 20f, 0.8f, 0, noiseIsInsideClosedShip: false, 2692);

        FrontLeftWheel.motorTorque = 0f;
        FrontRightWheel.motorTorque = 0f;
        BackLeftWheel.motorTorque = 0f;
        BackRightWheel.motorTorque = 0f;

        FrontLeftWheel.brakeTorque = 0f;
        FrontRightWheel.brakeTorque = 0f;
        BackLeftWheel.brakeTorque = 0f;
        BackRightWheel.brakeTorque = 0f;

        leftWheelMesh.enabled = false;
        rightWheelMesh.enabled = false;
        backLeftWheelMesh.enabled = false;
        backRightWheelMesh.enabled = false;
        carHoodAnimator.gameObject.GetComponentInChildren<MeshRenderer>().enabled = false;
        backDoorContainer.SetActive(value: false);
        headlightsContainer.SetActive(value: false);
        BreakWindshield();
        destroyedTruckMesh.SetActive(value: true);
        mainBodyMesh.gameObject.SetActive(value: false);
        foreach (GameObject rip in haulerObjectsToDestroy) { rip.SetActive(false); }
        WheelCollider[] componentsInChildren = base.gameObject.GetComponentsInChildren<WheelCollider>();
        for (int j = 0; j < componentsInChildren.Length; j++)
        {
            componentsInChildren[j].enabled = false;
        }
        mainRigidbody.ResetCenterOfMass();
        mainRigidbody.AddForceAtPosition(Vector3.up * 1560f, hoodFireAudio.transform.position - Vector3.up, ForceMode.Impulse);

        // Kill backseat players if the car explodes
        if (localPlayerInControl || localPlayerInPassengerSeat || localPlayerInBLSeat || localPlayerInBRSeat || localPlayerInMiddleSeat)
        {
            Debug.Log($"Killing player with force magnitude of : {(Vector3.up * 27f + 20f * UnityEngine.Random.insideUnitSphere).magnitude}");
            GameNetworkManager.Instance.localPlayerController.KillPlayer(Vector3.up * 27f + 20f * UnityEngine.Random.insideUnitSphere, spawnBody: true, CauseOfDeath.Blast, 6, Vector3.up * 1.5f);
        }
        InteractTrigger[] componentsInChildren2 = base.gameObject.GetComponentsInChildren<InteractTrigger>();
        for (int k = 0; k < componentsInChildren2.Length; k++)
        {
            componentsInChildren2[k].interactable = false;
            componentsInChildren2[k].CancelAnimationExternally();
        }
        Landmine.SpawnExplosion(base.transform.position + base.transform.forward + Vector3.up * 1.5f, spawnExplosionEffect: true, 6f, 10f, 30, 200f, truckDestroyedExplosion, goThroughCar: true);
    }



    // Collision stuff...
    [ServerRpc(RequireOwnership = false)]
    public new void CarCollisionServerRpc(Vector3 vel, float magn)
    {
        CarCollisionClientRpc(vel, magn);
    }

    [ClientRpc]
    public new void CarCollisionClientRpc(Vector3 vel, float magn)
    {
        if (IsOwner)
            return;

        DamagePlayerInVehicle(vel, magn);
        BreakWindshield();
    }

    private new void DamagePlayerInVehicle(Vector3 vel, float magnitude)
    {
        // Damage backseat players if the car hits something, like the front seats do
        if (localPlayerInBRSeat || localPlayerInBLSeat || localPlayerInMiddleSeat || localPlayerInPassengerSeat || localPlayerInControl)
        {
            if (magnitude > 28f)
            {
                GameNetworkManager.Instance.localPlayerController.KillPlayer(vel, spawnBody: true, CauseOfDeath.Inertia, 0, base.transform.up * 0.77f);
            }
            else if (magnitude > 24f)
            {
                HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
                if (GameNetworkManager.Instance.localPlayerController.health < 20)
                {
                    GameNetworkManager.Instance.localPlayerController.KillPlayer(vel, spawnBody: true, CauseOfDeath.Inertia, 0, base.transform.up * 0.77f);
                }
                else
                {
                    GameNetworkManager.Instance.localPlayerController.DamagePlayer(40, hasDamageSFX: true, callRPC: true, CauseOfDeath.Inertia, 0, fallDamage: false, vel);
                }
            }
            else
            {
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                GameNetworkManager.Instance.localPlayerController.DamagePlayer(30, hasDamageSFX: true, callRPC: true, CauseOfDeath.Inertia, 0, fallDamage: false, vel);
            }
        }
        else if (physicsRegion.physicsTransform == GameNetworkManager.Instance.localPlayerController.physicsParent && GameNetworkManager.Instance.localPlayerController.overridePhysicsParent == null)
        {
            HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
            GameNetworkManager.Instance.localPlayerController.DamagePlayer(10, hasDamageSFX: true, callRPC: true, CauseOfDeath.Inertia, 0, fallDamage: false, vel);
            GameNetworkManager.Instance.localPlayerController.externalForceAutoFade += vel;
        }
    }


    public new void OnDisable()
    {
        DisableControl();
        if (localPlayerInControl || localPlayerInPassengerSeat || localPlayerInBLSeat || localPlayerInBRSeat || localPlayerInMiddleSeat)
        {
            GameNetworkManager.Instance.localPlayerController.CancelSpecialTriggerAnimations();
        }
        GrabbableObject[] itemsInTruck = physicsRegion.physicsTransform.GetComponentsInChildren<GrabbableObject>();
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
        physicsRegion.disablePhysicsRegion = true;
        if (StartOfRound.Instance.CurrentPlayerPhysicsRegions.Contains(physicsRegion))
        {
            StartOfRound.Instance.CurrentPlayerPhysicsRegions.Remove(physicsRegion);
        }
    }

    // Disable backrow players from pushing car while seated
    public new void PushTruckWithArms()
    {
        if (magnetedToShip)
            return;

        if (!Physics.Raycast(GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position, GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.forward, out base.hit, 10f, 1073742656, QueryTriggerInteraction.Ignore))
            return;

        if (GameNetworkManager.Instance.localPlayerController.physicsParent != null)
            return;

        if (GameNetworkManager.Instance.localPlayerController.overridePhysicsParent != null)
            return;

        Vector3 point = hit.point;
        Vector3 forward = GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.forward;

        if (!IsOwner)
        {
            PushTruckServerRpc(point, forward);
            return;
        }

        mainRigidbody.AddForceAtPosition(Vector3.Normalize(forward * 1000f) * UnityEngine.Random.Range(40f, 50f) * pushForceMultiplier, point - mainRigidbody.transform.up * pushVerticalOffsetAmount, ForceMode.Impulse);
        PushTruckFromOwnerServerRpc(point);
    }

    // Hauler can't boost
    public new void AddTurboBoost() {}

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

    // Animation overrides for the gear shifter
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

    // Animation override networking for the gear shifter
    [ClientRpc]
    public void ReplaceGearshiftAnimClientRpc(int playerId)
    {
        PlayerControllerB driverPlayer = StartOfRound.Instance.allPlayerScripts[playerId];
        originalController = driverPlayer.playerBodyAnimator.runtimeAnimatorController;
        overrideController = new AnimatorOverrideController(originalController);

        driverPlayer.playerBodyAnimator.SetBool("SA_JumpInCar", true);

        overrideController["PullGearstick"] = haulerColumnShiftClip;
        overrideController["SitAndSteer"] = cruiserSteeringClip;
        overrideController["SitAndSteerRightHandOnGearstick"] = cruiserSteeringClip;
        overrideController["SitAndSteerNoHands"] = haulerSitAndSteerNoHandsClip;
        overrideController["Key_Insert"] = haulerKeyInsertClip;
        overrideController["Key_InsertAgain"] = haulerKeyInsertAgainClip;
        overrideController["Key_Remove"] = haulerKeyRemoveClip;
        overrideController["Key_Untwist"] = haulerKeyUntwistClip;

        driverPlayer.playerBodyAnimator.runtimeAnimatorController = overrideController;

        CompanyHauler.Logger.LogDebug("Replaced gearshifter animation clip.");
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



    // TODO: this isn't used yet, doesn't look quite right ingame
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