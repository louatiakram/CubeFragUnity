using UnityEngine;

/// Individual fragment piece behaviour with rotation physics
/// Uses CustomRB for realistic tumbling motion without Unity Rigidbody
/// NOW WITH FRAGMENT-TO-FRAGMENT COLLISION
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
    private float angularVelocityMultiplier = 1f;

    private const float RESTITUTION = 0.3f; // Bounciness (no friction)
    private const float TIME_STEP = 1f / 60f;
    private const int FRACTURE_GRACE_FRAMES = 3;
    private const float REST_THRESHOLD = 0.15f;
    private const float ANGULAR_REST_THRESHOLD = 1.0f;
    private const float ANGULAR_DAMPING = 0.3f;

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

    public float GetBoundingRadius() => boundingRadius;
    public Vector3 GetVelocity() => rb != null ? rb.Velocity : Vector3.zero;

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
        angularVelocityMultiplier = angularVelMultiplier;

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
            Vector3 minTorque = new Vector3(
                Random.Range(-0.5f, 0.5f),
                Random.Range(-0.5f, 0.5f),
                Random.Range(-0.5f, 0.5f)
            ) * torqueMultiplier * 0.3f * angularVelocityMultiplier;
            rb.AddTorque(minTorque);
            return;
        }

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

        Vector3 randomOffset = Random.insideUnitSphere * 0.08f;
        Vector3 direction = (tangent + randomOffset).normalized;
        float impulseMagnitude = effectiveImpulse * mass;

        Vector3 randomPoint = rb.Position + Random.insideUnitSphere * boundingRadius * 0.5f;
        rb.AddForceAtPoint(direction * (impulseMagnitude / Mathf.Max(mass, 1e-6f)), randomPoint);

        rb.AddForce(n * (0.15f * effectiveFalloff));

        Vector3 randomTorque = new Vector3(
            Random.Range(-2f, 2f),
            Random.Range(-2f, 2f),
            Random.Range(-2f, 2f)
        ) * torqueMultiplier * effectiveFalloff * angularVelocityMultiplier;

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
            Vector3 min, max;
            collisionDetector.GetWorldAABB(supportTransform, out min, out max);
            const float EPS = 0.0005f;
            float expectedY = max.y + halfHeight + EPS;

            if (Mathf.Abs(rb.Position.y - expectedY) > 0.1f)
            {
                isResting = false;
                supportTransform = null;

                Vector3 fallRotation = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f)
                ) * angularVelocityMultiplier;
                rb.SetAngularVelocity(fallRotation);
            }
            else
            {
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

        rb.ApplyGravity(dt);
        rb.ApplyAngularDamping(ANGULAR_DAMPING, dt);

        Vector3 nextPos = rb.Position + rb.Velocity * dt;
        rb.Integrate(dt);

        if (!justFractured)
        {
            // Check obstacle collision first
            Vector3 normal, hitPoint, closestPoint;
            ObstacleBounds hitObstacle;
            if (collisionDetector.CheckSphereCollision(nextPos, boundingRadius, out normal, out hitPoint, out closestPoint, out hitObstacle))
            {
                ResolveObstacleCollision(ref nextPos, normal, hitPoint, hitObstacle);
            }
            // Then check fragment collision
            else
            {
                Vector3 fragNormal, fragHitPoint, relativeVel;
                if (collisionDetector.CheckFragmentCollision(this, nextPos, boundingRadius, out fragNormal, out fragHitPoint, out relativeVel))
                {
                    ResolveFragmentCollision(ref nextPos, fragNormal, relativeVel);
                }
                else
                {
                    isResting = false;
                    supportTransform = null;

                    if (rb.AngularVelocity.magnitude < 0.1f)
                    {
                        Vector3 resumeTumble = new Vector3(
                            Random.Range(-1f, 1f),
                            Random.Range(-1f, 1f),
                            Random.Range(-1f, 1f)
                        ) * angularVelocityMultiplier;
                        rb.SetAngularVelocity(resumeTumble);
                    }
                }
            }
        }

        rb.SetPosition(nextPos);
    }

    void ResolveFragmentCollision(ref Vector3 nextPos, Vector3 normal, Vector3 relativeVel)
    {
        // Frictionless elastic collision response
        float normalVel = Vector3.Dot(relativeVel, normal);

        if (normalVel < 0f)
        {
            // Reflect velocity with restitution (no friction - only normal component changes)
            Vector3 velocityChange = -(1f + RESTITUTION) * normalVel * normal;
            rb.SetVelocity(rb.Velocity + velocityChange);

            // Add some rotation from collision
            Vector3 torque = Vector3.Cross(normal, relativeVel) * 0.1f * angularVelocityMultiplier;
            rb.AddTorque(torque);
        }

        // Separate fragments slightly to prevent overlap
        nextPos += normal * 0.01f;
    }

    void ResolveObstacleCollision(ref Vector3 nextPos, Vector3 normal, Vector3 hitPoint, ObstacleBounds hitObstacle)
    {
        float upDot = Vector3.Dot(normal, Vector3.up);
        const float EPS = 0.0005f;
        bool usedFlatSnap = false;

        if (hitObstacle != null && hitObstacle.isGround && upDot > 0.7f)
        {
            Vector3 min, max;
            hitObstacle.GetWorldAABB(out min, out max);

            float targetY = max.y + halfHeight + EPS;
            nextPos.y = targetY;
            usedFlatSnap = true;
            supportTransform = hitObstacle.transform;

            rb.SetVelocity(Vector3.zero);
            rb.SetAngularVelocity(Vector3.zero);

            Quaternion flatRotation = Quaternion.FromToRotation(rb.Rotation * Vector3.up, Vector3.up) * rb.Rotation;
            rb.SetRotation(flatRotation);

            isResting = true;
        }
        else if (upDot > 0.7f)
        {
            Vector3 min, max;
            hitObstacle.GetWorldAABB(out min, out max);
            float targetY = max.y + halfHeight + EPS;
            nextPos.y = targetY;
            usedFlatSnap = true;
            supportTransform = hitObstacle.transform;

            Vector3 velocity = rb.Velocity;
            velocity.y = -velocity.y * RESTITUTION;
            rb.SetVelocity(velocity);

            rb.SetAngularVelocity(Vector3.zero);
            isResting = false;
        }
        else if (upDot < -0.7f)
        {
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
            // Vertical wall - frictionless bounce
            float normalVelocity = Vector3.Dot(rb.Velocity, normal);
            if (normalVelocity < 0f)
            {
                // Pure reflection with no friction
                rb.SetVelocity(rb.Velocity - (1f + RESTITUTION) * normalVelocity * normal);

                Vector3 r = hitPoint - rb.Position;
                Vector3 torque = Vector3.Cross(r, normal * Mathf.Abs(normalVelocity) * mass);
                rb.AddTorque(torque * 0.2f * angularVelocityMultiplier);
            }
        }

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