using UnityEngine;

/// Individual fragment piece behaviour with rotation physics
/// Uses CustomRB for realistic tumbling motion without Unity Rigidbody
public class PieceBehaviour : MonoBehaviour
{
    private Vector3 localCenter;
    private Vector3 dimensions;
    private float mass;
    private float boundingRadius;
    private float halfHeight;

    private CustomRB rb;
    private bool isInitialized = false;
    private bool justFractured = false;
    private int fractureFrameCount = 0;
    private bool isResting = false;
    private Transform supportTransform = null;
    private float angularVelocityMultiplier = 1f; // Angular velocity multiplier

    private const float RESTITUTION = 0.05f;
    private const float TIME_STEP = 1f / 60f;
    private const int FRACTURE_GRACE_FRAMES = 3;
    private const float REST_THRESHOLD = 0.1f;
    private const float ANGULAR_REST_THRESHOLD = 0.5f; // rad/s - increased to allow more visible rotation
    private const float ANGULAR_DAMPING = 0.4f; // Increased damping for slower rotation

    private float timeAccumulator = 0f;
    public Vector3 LocalCenter => localCenter;

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
        Vector3 normal,
        float impulseStrength,
        float impactRadius,
        Vector3 plateAngularVelocity,
        float torqueMultiplier,
        Quaternion initialRotation,
        float angularVelMultiplier)
    {
        rb = new CustomRB(startPos, mass, initialRotation, dimensions);
        rb.SetVelocity(startVel);
        angularVelocityMultiplier = angularVelMultiplier; // Store angular velocity multiplier

        // Start with NO rotation - only add rotation when fragment starts moving/falling
        rb.SetAngularVelocity(Vector3.zero);

        isInitialized = true;
        justFractured = true;
        fractureFrameCount = 0;
        isResting = false;
        supportTransform = null;

        ApplyFractureImpulse(impactPoint, normal, impulseStrength, impactRadius, torqueMultiplier);
    }

    void ApplyFractureImpulse(Vector3 impactPoint, Vector3 normal, float impulseStrength, float impactRadius, float torqueMultiplier)
    {
        Vector3 toFragment = rb.Position - impactPoint;
        float distance = toFragment.magnitude;
        float falloff = Mathf.Clamp01(1f - (distance / impactRadius));
        Vector3 n = (normal.sqrMagnitude > 1e-6f) ? normal.normalized : Vector3.up;

        if (falloff < 0.01f)
        {
            rb.AddForce(n * 0.05f);

            // Still add some rotation even for distant pieces
            Vector3 minTorque = new Vector3(
                Random.Range(-0.5f, 0.5f),
                Random.Range(-0.5f, 0.5f),
                Random.Range(-0.5f, 0.5f)
            ) * torqueMultiplier * 0.3f * angularVelocityMultiplier; // Apply angular velocity multiplier
            rb.AddTorque(minTorque);
            return;
        }

        // Calculate tangential direction
        Vector3 radial = toFragment;
        Vector3 tangent = radial - Vector3.Dot(radial, n) * n;
        if (tangent.sqrMagnitude < 1e-8f)
        {
            tangent = Vector3.Cross(n, Vector3.right);
            if (tangent.sqrMagnitude < 1e-6f)
                tangent = Vector3.Cross(n, Vector3.forward);
        }
        tangent = tangent.normalized;

        float effectiveFalloff = falloff * falloff * falloff;
        float effectiveImpulse = impulseStrength * effectiveFalloff;

        // Add randomness
        Vector3 randomOffset = Random.insideUnitSphere * 0.08f;
        Vector3 direction = (tangent + randomOffset).normalized;
        float impulseMagnitude = effectiveImpulse * mass;

        // Apply force at a point offset from center to create torque
        Vector3 randomPoint = rb.Position + Random.insideUnitSphere * boundingRadius * 0.5f;
        rb.AddForceAtPoint(direction * (impulseMagnitude / Mathf.Max(mass, 1e-6f)), randomPoint);

        // Add upward component
        rb.AddForce(n * (0.15f * effectiveFalloff));

        // Add significant random angular velocity for visible tumbling effect
        Vector3 randomTorque = new Vector3(
            Random.Range(-1.5f, 1.5f),
            Random.Range(-1.5f, 1.5f),
            Random.Range(-1.5f, 1.5f)
        ) * torqueMultiplier * effectiveFalloff * 2f * angularVelocityMultiplier; // Apply angular velocity multiplier

        rb.AddTorque(randomTorque);
    }

    public void PhysicsUpdate(float deltaTime, float gravity, CollisionDetector collisionDetector)
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
            // Check if the obstacle we're resting on still exists and is in the same position
            Vector3 min, max;
            collisionDetector.GetWorldAABB(supportTransform, out min, out max);
            const float EPS = 0.0005f;
            float expectedY = max.y + halfHeight + EPS;

            // If we've moved away from the surface (obstacle moved or removed), start falling again
            if (Mathf.Abs(rb.Position.y - expectedY) > 0.1f)
            {
                isResting = false;
                supportTransform = null;

                // Add rotation when we start falling again - use angular velocity multiplier
                Vector3 fallRotation = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f)
                ) * angularVelocityMultiplier; // Use angular velocity multiplier directly
                rb.SetAngularVelocity(fallRotation);
            }
            else
            {
                // Still properly resting - maintain position
                rb.SetPosition(new Vector3(rb.Position.x, expectedY, rb.Position.z));
                rb.SetAngularVelocity(Vector3.zero);
                rb.SetVelocity(Vector3.zero);

                transform.position = rb.Position;
                transform.rotation = rb.Rotation;
                return;
            }
        }

        timeAccumulator += Mathf.Min(deltaTime, 0.05f);
        while (timeAccumulator >= TIME_STEP)
        {
            timeAccumulator -= TIME_STEP;
            PhysicsStep(TIME_STEP, gravity, collisionDetector);
        }

        transform.position = rb.Position;
        transform.rotation = rb.Rotation;
    }

    void PhysicsStep(float dt, float gravity, CollisionDetector collisionDetector)
    {
        if (isResting) return;

        // Apply forces
        rb.ApplyGravity(dt);
        rb.ApplyAngularDamping(ANGULAR_DAMPING, dt);

        // Integrate physics
        Vector3 nextPos = rb.Position + rb.Velocity * dt;
        rb.Integrate(dt);

        if (!justFractured)
        {
            Vector3 normal, hitPoint, closestPoint;
            ObstacleBounds hitObstacle;
            if (collisionDetector.CheckSphereCollision(nextPos, boundingRadius, out normal, out hitPoint, out closestPoint, out hitObstacle))
            {
                ResolveCollision(ref nextPos, normal, hitPoint, hitObstacle);
            }
            else
            {
                // No collision detected - fragment is falling freely
                isResting = false;
                supportTransform = null;

                // Resume rotation when falling off an obstacle if no rotation is present
                if (rb.AngularVelocity.magnitude < 0.1f)
                {
                    Vector3 resumeTumble = new Vector3(
                        Random.Range(-1f, 1f),
                        Random.Range(-1f, 1f),
                        Random.Range(-1f, 1f)
                    ) * angularVelocityMultiplier; // Use angular velocity multiplier directly
                    rb.SetAngularVelocity(resumeTumble);
                }
            }
        }

        rb.SetPosition(nextPos);
    }

    void ResolveCollision(ref Vector3 nextPos, Vector3 normal, Vector3 hitPoint, ObstacleBounds hitObstacle)
    {
        float upDot = Vector3.Dot(normal, Vector3.up);
        const float EPS = 0.0005f;
        bool usedFlatSnap = false;

        // Check if we hit ground
        if (hitObstacle != null && hitObstacle.isGround && upDot > 0.7f)
        {
            Vector3 min, max;
            hitObstacle.GetWorldAABB(out min, out max);

            // Position fragment completely on top of ground
            float targetY = max.y + halfHeight + EPS;
            nextPos.y = targetY;
            usedFlatSnap = true;
            supportTransform = hitObstacle.transform;

            // Stop ALL movement when landing on ground
            rb.SetVelocity(Vector3.zero);
            rb.SetAngularVelocity(Vector3.zero);

            // Align rotation to be flat on ground (optional - makes it look more stable)
            // Comment out if you want fragments to keep their rotated orientation
            Quaternion flatRotation = Quaternion.FromToRotation(rb.Rotation * Vector3.up, Vector3.up) * rb.Rotation;
            rb.SetRotation(flatRotation);

            isResting = true;
        }
        else if (upDot > 0.7f)
        {
            // Hitting horizontal surface (not ground) - bounce with reduced velocity
            Vector3 min, max;
            hitObstacle.GetWorldAABB(out min, out max);
            float targetY = max.y + halfHeight + EPS;
            nextPos.y = targetY;
            usedFlatSnap = true;
            supportTransform = hitObstacle.transform;

            // Bounce with restitution
            Vector3 velocity = rb.Velocity;
            velocity.y = -velocity.y * RESTITUTION;
            rb.SetVelocity(velocity);

            // Stop rotation when resting on non-ground obstacle
            rb.SetAngularVelocity(Vector3.zero);

            isResting = false;
        }
        else if (upDot < -0.7f)
        {
            // Hitting ceiling
            Vector3 min, max;
            hitObstacle.GetWorldAABB(out min, out max);
            float targetY = min.y - halfHeight - EPS;
            nextPos.y = targetY;
            usedFlatSnap = true;

            Vector3 velocity = rb.Velocity;
            velocity.y = Mathf.Min(velocity.y, 0f);
            rb.SetVelocity(velocity);
        }
        else
        {
            // Hitting vertical surface
            float normalVelocity = Vector3.Dot(rb.Velocity, normal);
            if (normalVelocity < 0f)
            {
                rb.SetVelocity(rb.Velocity - (1f + RESTITUTION) * normalVelocity * normal);

                // Apply torque from wall collision
                Vector3 r = hitPoint - rb.Position;
                Vector3 torque = Vector3.Cross(r, normal * Mathf.Abs(normalVelocity) * mass);
                rb.AddTorque(torque * 0.3f * angularVelocityMultiplier); // Apply angular velocity multiplier
            }
        }

        // Go to rest state on ground when velocity is low
        if (usedFlatSnap && hitObstacle != null && hitObstacle.isGround && upDot > 0.7f)
        {
            float linearSpeed = rb.Velocity.magnitude;
            float angularSpeed = rb.AngularVelocity.magnitude;

            if (linearSpeed < REST_THRESHOLD && angularSpeed < ANGULAR_REST_THRESHOLD)
            {
                isResting = true;
                rb.SetVelocity(Vector3.zero);
                rb.SetAngularVelocity(Vector3.zero);
            }
        }
    }
}
