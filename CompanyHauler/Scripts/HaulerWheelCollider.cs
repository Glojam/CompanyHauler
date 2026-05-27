using UnityEngine;

namespace CompanyHauler.Scripts;
public class HaulerWheelCollider : MonoBehaviour
{
    public float wheelRadius = 0.84f;
    public float wheelInertia = 30f;

    public float suspensionLength = 0.6f;
    public float minSuspensionLength = 0.25f;
    public float maxSuspensionLength = 0.7f;

    public float springRate = 50000.0f;
    public float damperRate = 2500.0f;
    public LayerMask collisionLayers = Physics.AllLayers;
    public AnimationCurve slipCurve = AnimationCurve.Linear(0, 0, 1, 1);

    public struct WheelColliderHit
    {
        public bool grounded;
        public Vector3 point;
        public Vector3 normal;
        public float force;
        public float forwardSlip;
        public float sidewaysSlip;
        public Collider collider;
    }

    public Transform cachedTransform = default!;
    public Transform visualTransform = default!;
    public Rigidbody cachedRigidbody = default!;

    private float fixedDeltaTime = 0.02f;

    private Vector3 cachedPosition = Vector3.zero;
    private Vector3 wheelForward = Vector3.zero;
    private Vector3 wheelRight = Vector3.zero;
    private Vector3 wheelUp = Vector3.zero;

    public bool isGrounded = false;
    private RaycastHit hitResult = default;

    private float suspensionVelocity = 0.0f;
    private float bumpStopOffset = 0.0f;
    private float previousSuspensionLength = 0.0f;
    private float currentSuspensionLength = 0.0f;

    private Vector3 projectedForward = Vector3.zero;
    private Vector3 projectedRight = Vector3.zero;

    private Vector3 worldVelocity = Vector3.zero;
    private Vector2 localVelocity = Vector2.zero;

    private Vector2 slip = Vector2.zero;

    private Vector2 localGravityForce = Vector2.zero;
    private Vector2 localVelocityForce = Vector2.zero;

    private Vector2 localTireForce = Vector2.zero;
    private Vector3 worldTireForce = Vector3.zero;

    private Quaternion localRotation = Quaternion.identity;
    private Vector3 rightNormal;

    public float visualRotation = 0.0f;

    public float rollingResistance = 0.0f;
    public float frictionCoefficient = 0.0f;
    public float surfaceGripMultiplier = 0.0f;
    public float wheelRPM = 0.0f;

    public float SteerAngle = 0.0f;
    private float Load = 0.0f;
    public float AngularVelocity = 0.0f;
    public float MotorTorque = 0.0f;
    public float BrakeTorque = 0.0f;

    public void OnEnable()
    {
        currentSuspensionLength = suspensionLength;
    }

    public bool GetGroundHit(out WheelColliderHit hit)
    {
        if (!isGrounded)
        {
            hit = default;
            return false;
        }
        hit = new WheelColliderHit
        {
            grounded = true,
            point = hitResult.point,
            normal = hitResult.normal,
            force = Load,
            forwardSlip = slip.y,
            sidewaysSlip = slip.x,
            collider = hitResult.collider
        };
        return true;
    }

    public void FixedUpdate()
    {
        return;
        fixedDeltaTime = Time.fixedDeltaTime;
        cachedPosition = cachedTransform.position;
        Quaternion steerRotation = Quaternion.Euler(0.0f, SteerAngle, 0.0f);
        Quaternion combinedRotation = cachedTransform.rotation * steerRotation;
        wheelForward = combinedRotation * Vector3.forward;
        wheelRight = combinedRotation * Vector3.right;
        wheelUp = combinedRotation * Vector3.up;

        isGrounded = Physics.Raycast(cachedPosition, -wheelUp, out hitResult, suspensionLength + wheelRadius, collisionLayers, QueryTriggerInteraction.Ignore);

        previousSuspensionLength = currentSuspensionLength;
        currentSuspensionLength = isGrounded ? hitResult.distance - wheelRadius : suspensionLength;
        bumpStopOffset = Mathf.Max(0f, minSuspensionLength - currentSuspensionLength);
        currentSuspensionLength = Mathf.Clamp(currentSuspensionLength, minSuspensionLength, maxSuspensionLength);
        suspensionVelocity = (previousSuspensionLength - currentSuspensionLength) / fixedDeltaTime;

        float bumpStopForce = 0f;
        if (isGrounded && bumpStopOffset > 0.02f)
        {
            bumpStopForce = bumpStopOffset * springRate * 2f;
            Vector3 pointVelocity = cachedRigidbody.GetPointVelocity(cachedPosition);
            float velocityIntoGround = Vector3.Dot(pointVelocity, -hitResult.normal);
            if (velocityIntoGround > 0f)
            {
                float impulseMagnitude = velocityIntoGround * cachedRigidbody.mass * 0.25f;
                impulseMagnitude = Mathf.Min(impulseMagnitude, cachedRigidbody.mass * 5f);
                cachedRigidbody.AddForceAtPosition(hitResult.normal * impulseMagnitude, cachedPosition, ForceMode.Impulse);
            }
        }

        Load = ((suspensionLength - currentSuspensionLength) * springRate) + (suspensionVelocity * damperRate);
        Load = Load > 0 ? Load : 0;
        if (isGrounded) cachedRigidbody.AddForceAtPosition(hitResult.normal * (Load + bumpStopForce), cachedPosition);

        projectedForward = Vector3.Cross(hitResult.normal, -wheelRight);
        projectedRight = Vector3.Cross(hitResult.normal, wheelForward);

        worldVelocity = cachedRigidbody.GetPointVelocity(cachedPosition);
        localVelocity.x = Vector3.Dot(worldVelocity, projectedRight);
        localVelocity.y = Vector3.Dot(worldVelocity, projectedForward);

        slip.x = -(localVelocity.x);
        slip.y = -(localVelocity.y - AngularVelocity * wheelRadius);

        AngularVelocity = AngularVelocity + MotorTorque / wheelInertia * fixedDeltaTime;
        AngularVelocity *= 1.0f - (rollingResistance * fixedDeltaTime);

        float absAngularSlip = (slip.y >= 0 ? slip.y : -slip.y) / wheelRadius;
        float frictionTorque = -Mathf.Clamp(localVelocityForce.y, -Mathf.Abs(localTireForce.y), Mathf.Abs(localTireForce.y)) * wheelRadius;
        AngularVelocity = AngularVelocity + Mathf.Clamp(frictionTorque / wheelInertia * fixedDeltaTime, -absAngularSlip, absAngularSlip);

        float absAngularVelocity = (AngularVelocity >= 0 ? AngularVelocity : -AngularVelocity);
        float absBrakeTorque = BrakeTorque > 0 ? BrakeTorque : -BrakeTorque;
        float signedBrakeTorque = -absBrakeTorque * Mathf.Sign(AngularVelocity);
        AngularVelocity = AngularVelocity + Mathf.Clamp(signedBrakeTorque / wheelInertia * fixedDeltaTime, -absAngularVelocity, absAngularVelocity);
        wheelRPM = AngularVelocity * 60f / (2f * Mathf.PI);

        float absBrakeForce = absBrakeTorque / wheelRadius;
        float dotProduct = Vector3.Dot(hitResult.normal, -Physics.gravity.normalized);

        Vector3 gravityForce = -Physics.gravity.normalized * (dotProduct > 1E-5f ? Load / dotProduct : 10000.0f);
        localGravityForce.x = Vector3.Dot(gravityForce, projectedRight);
        localGravityForce.y = Mathf.Clamp(Vector3.Dot(gravityForce, projectedForward), -absBrakeForce, absBrakeForce);

        float slipValue = slipCurve.Evaluate(Mathf.Clamp01(slip.magnitude));
        localVelocityForce = slip * ((Load / Physics.gravity.magnitude) / fixedDeltaTime) * slipValue;

        localTireForce = localVelocityForce + localGravityForce;
        localTireForce = Vector3.ClampMagnitude(localTireForce, Load * frictionCoefficient * surfaceGripMultiplier);

        worldTireForce = projectedForward * localTireForce.y + projectedRight * localTireForce.x;
        if (isGrounded) cachedRigidbody.AddForceAtPosition(worldTireForce, cachedPosition);

        visualTransform.localPosition = new Vector3(0.0f, -currentSuspensionLength, 0.0f);
        visualRotation = Mathf.Repeat(visualRotation + AngularVelocity * Mathf.Rad2Deg * fixedDeltaTime, 360.0f);
        visualTransform.localRotation = Quaternion.Euler(visualRotation, SteerAngle, 0.0f);
    }
}