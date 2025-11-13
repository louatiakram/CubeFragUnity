using UnityEngine;

/// Individual fragment with full physics simulation
/// Handles gravity, rotation, and collision response (frictionless)
public class PieceBehaviour : MonoBehaviour
{
    // Geometry
    private Vector3 localCenter;
    private Vector3 dimensions;
    private float mass;
    private float boundingRadius;
    private float halfHeight;

    // Physics state
    private CustomRB rb;
    private bool isInitialized = false;
    private bool justFractured = false;
    private int fractureFrameCount = 0;
    private bool isResting = false;
    private Transform supportTransform = null;
    private float angularVelocityScale = 1f;

    // Constants
    private const float RESTITUTION = 0.3f;
    private const float TIME_STEP = 1f / 60f;
    private const int FRACTURE_GRACE_FRAMES = 3;
    private const float REST_LINEAR_THRESHOLD = 0.15f;
    private const float REST_ANGULAR_THRESHOLD = 1.0f;
    private const float ANGULAR_DAMPING = 0.3f;
    private const float COLLISION_EPSILON = 0.0005f;

    private float timeAccumulator = 0f;

    // Public properties
    public Vector3 LocalCenter => localCenter;
    public float BoundingRadius => boundingRadius;
    public Vector3 Velocity => rb?.Velocity ?? Vector3.zero;

    public void SetupGeometry(Vector3 center, float m, float radius, Vector3 dims)
    {
        localCenter = center;
        mass = Mathf.Max(m, 0.01f);
        boundingRadius = radius;
        dimensions = dims;
        halfHeight = dims.y * 0.5f;
    }

    public void Initialize(
        Vector3 startPos,
        Vector3 startVel,
        Vector3 impactPoint,
        Vector3 impactNormal,
        float impulseStrength,
        float impactRadius,
        float torqueMultiplier,
        Quaternion initialRotation,
        float angularVelScale)
    {
        rb = new CustomRB(startPos, mass, initialRotation, dimensions);
        rb.SetVelocity(startVel);
        rb.SetAngularVelocity(Vector3.zero);

        angularVelocityScale = angularVelScale;
        isInitialized = true;
        justFractured = true;
        fractureFrameCount = 0;
        isResting = false;
        supportTransform = null;

        ApplyFractureImpulse(impactPoint, impactNormal, impulseStrength, impactRadius, torqueMultiplier);
    }

    private void ApplyFractureImpulse(Vector3 impactPoint, Vector3 normal, float strength, float radius, float torqueScale)
    {
        Vector3 toFragment = rb.Position - impactPoint;
        float distance = toFragment.magnitude;
        float falloff = Mathf.Clamp01(1f - (distance / radius));
        Vector3 n = normal.sqrMagnitude > 1e-6f ? normal.normalized : Vector3.up;

        // Distant fragments get minimal impulse
        if (falloff < 0.01f)
        {
            rb.AddForce(n * 0.05f);
            Vector3 minTorque = Random.insideUnitSphere * torqueScale * 0.3f * angularVelocityScale;
            rb.AddTorque(minTorque);
            return;
        }

        // Calculate tangential direction
        Vector3 tangent = toFragment - Vector3.Dot(toFragment, n) * n;
        if (tangent.sqrMagnitude < 1e-8f)
        {
            tangent = Vector3.Cross(n, Vector3.right);
            if (tangent.sqrMagnitude < 1e-6f)
                tangent = Vector3.Cross(n, Vector3.forward);
        }
        tangent.Normalize();

        float effectiveFalloff = falloff * falloff * falloff;
        float effectiveImpulse = strength * effectiveFalloff;

        // Add randomness and apply force
        Vector3 direction = (tangent + Random.insideUnitSphere * 0.08f).normalized;
        Vector3 randomPoint = rb.Position + Random.insideUnitSphere * boundingRadius * 0.5f;
        rb.AddForceAtPoint(direction * effectiveImpulse, randomPoint);

        // Add upward component
        rb.AddForce(n * (0.15f * effectiveFalloff));

        // Add rotation
        Vector3 torque = Random.insideUnitSphere * torqueScale * effectiveFalloff * 2f * angularVelocityScale;
        rb.AddTorque(torque);
    }

    public void PhysicsUpdate(float deltaTime, CollisionDetector collisionDetector)
    {
        if (!isInitialized) return;

        if (justFractured)
        {
            fractureFrameCount++;
            if (fractureFrameCount >= FRACTURE_GRACE_FRAMES)
                justFractured = false;
        }

        if (isResting && supportTransform != null)
        {
            CheckRestingState(collisionDetector);
            if (isResting)
            {
                transform.position = rb.Position;
                transform.rotation = rb.Rotation;
                return;
            }
        }

        timeAccumulator += Mathf.Min(deltaTime, 0.05f);
        while (timeAccumulator >= TIME_STEP)
        {
            timeAccumulator -= TIME_STEP;
            PhysicsStep(TIME_STEP, collisionDetector);
        }

        transform.position = rb.Position;
        transform.rotation = rb.Rotation;
    }

    private void CheckRestingState(CollisionDetector collisionDetector)
    {
        Vector3 min, max;
        collisionDetector.GetObstacleAABB(supportTransform, out min, out max);
        float expectedY = max.y + halfHeight + COLLISION_EPSILON;

        if (Mathf.Abs(rb.Position.y - expectedY) > 0.1f)
        {
            // Support moved or removed - start falling
            isResting = false;
            supportTransform = null;
            rb.SetAngularVelocity(Random.insideUnitSphere * angularVelocityScale);
        }
        else
        {
            // Still resting
            rb.SetPosition(new Vector3(rb.Position.x, expectedY, rb.Position.z));
            rb.SetVelocity(Vector3.zero);
            rb.SetAngularVelocity(Vector3.zero);
        }
    }

    private void PhysicsStep(float dt, CollisionDetector collisionDetector)
    {
        if (isResting) return;

        rb.ApplyGravity(dt);
        rb.ApplyAngularDamping(ANGULAR_DAMPING, dt);

        Vector3 nextPos = rb.Position + rb.Velocity * dt;
        rb.Integrate(dt);

        if (!justFractured)
        {
            HandleCollisions(ref nextPos, collisionDetector);
        }

        rb.SetPosition(nextPos);
    }

    private void HandleCollisions(ref Vector3 nextPos, CollisionDetector collisionDetector)
    {
        // Check obstacle collision first
        Vector3 normal, hitPoint;
        ObstacleBounds hitObstacle;

        if (collisionDetector.CheckObstacleCollision(nextPos, boundingRadius, out normal, out hitPoint, out hitObstacle))
        {
            ResolveObstacleCollision(ref nextPos, normal, hitObstacle);
            return;
        }

        // Check fragment collision
        Vector3 relativeVel;
        if (collisionDetector.CheckFragmentCollision(this, nextPos, boundingRadius, out normal, out hitPoint, out relativeVel))
        {
            ResolveFragmentCollision(ref nextPos, normal, relativeVel);
            return;
        }

        // No collision - resume rotation if stopped
        isResting = false;
        supportTransform = null;
        if (rb.AngularVelocity.magnitude < 0.1f)
        {
            rb.SetAngularVelocity(Random.insideUnitSphere * angularVelocityScale);
        }
    }

    private void ResolveFragmentCollision(ref Vector3 nextPos, Vector3 normal, Vector3 relativeVel)
    {
        float normalVel = Vector3.Dot(relativeVel, normal);

        if (normalVel < 0f)
        {
            // Frictionless bounce
            Vector3 velocityChange = -(1f + RESTITUTION) * normalVel * normal;
            rb.SetVelocity(rb.Velocity + velocityChange);

            // Add rotation from impact
            Vector3 torque = Vector3.Cross(normal, relativeVel) * 0.1f * angularVelocityScale;
            rb.AddTorque(torque);
        }

        // Separate fragments
        nextPos += normal * 0.01f;
    }

    private void ResolveObstacleCollision(ref Vector3 nextPos, Vector3 normal, ObstacleBounds obstacle)
    {
        float upDot = Vector3.Dot(normal, Vector3.up);
        Vector3 min, max;
        obstacle.GetWorldAABB(out min, out max);

        if (upDot > 0.7f)
        {
            // Horizontal surface
            float targetY = max.y + halfHeight + COLLISION_EPSILON;
            nextPos.y = targetY;
            supportTransform = obstacle.transform;

            if (obstacle.isGround)
            {
                // Landing on ground - stop completely
                rb.SetVelocity(Vector3.zero);
                rb.SetAngularVelocity(Vector3.zero);
                Quaternion flatRotation = Quaternion.FromToRotation(rb.Rotation * Vector3.up, Vector3.up) * rb.Rotation;
                rb.SetRotation(flatRotation);
                isResting = true;
            }
            else
            {
                // Bounce off non-ground surface
                Vector3 vel = rb.Velocity;
                vel.y = -vel.y * RESTITUTION;
                rb.SetVelocity(vel);
                rb.SetAngularVelocity(Vector3.zero);
            }

            // Check if should go to rest
            if (obstacle.isGround && rb.Velocity.magnitude < REST_LINEAR_THRESHOLD &&
                rb.AngularVelocity.magnitude < REST_ANGULAR_THRESHOLD)
            {
                isResting = true;
                rb.SetVelocity(Vector3.zero);
                rb.SetAngularVelocity(Vector3.zero);
            }
        }
        else if (upDot < -0.7f)
        {
            // Ceiling
            float targetY = min.y - halfHeight - COLLISION_EPSILON;
            nextPos.y = targetY;
            Vector3 vel = rb.Velocity;
            vel.y = Mathf.Min(vel.y, 0f);
            rb.SetVelocity(vel);
        }
        else
        {
            // Vertical wall - frictionless bounce
            float normalVel = Vector3.Dot(rb.Velocity, normal);
            if (normalVel < 0f)
            {
                rb.SetVelocity(rb.Velocity - (1f + RESTITUTION) * normalVel * normal);
                Vector3 r = nextPos - rb.Position;
                Vector3 torque = Vector3.Cross(r, normal * Mathf.Abs(normalVel) * mass);
                rb.AddTorque(torque * 0.2f * angularVelocityScale);
            }
        }
    }
}