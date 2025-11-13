using UnityEngine;

/// Individual fragment piece behaviour
/// Manages physics and collision response for each fragment after fracture
public class PieceBehaviour : MonoBehaviour
{
    // Geometry properties
    private Vector3 localCenter;
    private float mass;
    private float boundingRadius;
    private float halfHeight; // world half-height (for flush vertical placement)

    // Physics state
    private Vector3 position;
    private Vector3 velocity;
    private bool isInitialized = false;
    private bool justFractured = false;
    private int fractureFrameCount = 0;
    private bool isResting = false; // Fragment is at rest on a surface

    // Physics constants
    private const float RESTITUTION = 0.05f;
    private const float FRICTION = 0.92f;       // Increased for more stable resting
    private const float TIME_STEP = 1f / 60f;
    private const int FRACTURE_GRACE_FRAMES = 3;
    private const float REST_THRESHOLD = 0.1f; // Speed below which fragment can rest
    private const float SLEEP_THRESHOLD = 0.05f;// Very low speed = sleeping

    private float timeAccumulator = 0f;

    public Vector3 LocalCenter => localCenter;

    public void SetupGeometry(Vector3 center, float m, float radius)
    {
        localCenter = center;
        mass = Mathf.Max(m, 0.01f);
        boundingRadius = radius;

        // Capture world-space half-height based on current scaling (the cube height equals thickness)
        halfHeight = 0.5f * transform.lossyScale.y;
    }

    /// Initialize physics state after fracture
    public void Initialize(Vector3 startPos, Vector3 startVel, Vector3 impactPoint, Vector3 normal, float impulseStrength, float impactRadius)
    {
        position = startPos;
        velocity = startVel;

        isInitialized = true;
        justFractured = true;
        fractureFrameCount = 0;
        isResting = false;

        ApplyFractureImpulse(impactPoint, normal, impulseStrength, impactRadius);
    }

    void ApplyFractureImpulse(Vector3 impactPoint, Vector3 normal, float impulseStrength, float impactRadius)
    {
        // Calculate distance from fragment to impact point
        Vector3 toFragment = position - impactPoint;
        float distance = toFragment.magnitude;

        // Calculate falloff - fragments far away fall naturally
        float falloff = Mathf.Clamp01(1f - (distance / impactRadius));

        // Normalize contact normal
        Vector3 n = (normal.sqrMagnitude > 1e-6f) ? normal.normalized : Vector3.up;

        if (falloff < 0.01f)
        {
            // Fragment too far from impact - just falls naturally with tiny nudge
            velocity += n * 0.05f;
            return;
        }

        // Calculate radial direction from impact point (in tangent plane)
        Vector3 radial = toFragment;
        Vector3 tangent = radial - Vector3.Dot(radial, n) * n;

        // Handle edge case: fragment at impact point
        if (tangent.sqrMagnitude < 1e-8f)
        {
            tangent = Vector3.Cross(n, Vector3.right);
            if (tangent.sqrMagnitude < 1e-6f)
                tangent = Vector3.Cross(n, Vector3.forward);
        }

        // Normalize tangent direction
        tangent = tangent.normalized;

        // Apply impulse with distance-based falloff
        float effectiveFalloff = falloff * falloff * falloff;
        float effectiveImpulse = impulseStrength * effectiveFalloff;

        // Add slight randomization for natural look
        Vector3 randomOffset = Random.insideUnitSphere * 0.08f;
        Vector3 direction = (tangent + randomOffset).normalized;

        // Apply velocity change based on mass
        float impulseMagnitude = effectiveImpulse * mass;
        velocity += direction * (impulseMagnitude / Mathf.Max(mass, 1e-6f));

        // Small normal component to lift off surface
        velocity += n * (0.15f * effectiveFalloff);
    }

    public void PhysicsUpdate(float deltaTime, float gravity, CollisionDetector collisionDetector)
    {
        if (!isInitialized) return;

        // Track frames since fracture
        if (justFractured)
        {
            fractureFrameCount++;
            if (fractureFrameCount >= FRACTURE_GRACE_FRAMES)
            {
                justFractured = false;
            }
        }

        // If fragment is sleeping (at rest), skip most physics
        if (isResting && velocity.sqrMagnitude < SLEEP_THRESHOLD * SLEEP_THRESHOLD)
        {
            // Just check if still supported, otherwise wake up
            Vector3 testNormal, testHit, testClosest;
            if (!collisionDetector.CheckSphereCollision(position, boundingRadius, out testNormal, out testHit, out testClosest))
            {
                // No longer on surface, wake up
                isResting = false;
            }
            else
            {
                // Still resting, keep position stable
                transform.position = position;
                return;
            }
        }

        timeAccumulator += Mathf.Min(deltaTime, 0.05f);

        while (timeAccumulator >= TIME_STEP)
        {
            timeAccumulator -= TIME_STEP;
            PhysicsStep(TIME_STEP, gravity, collisionDetector);
        }

        // Update visual position
        transform.position = position;
    }

    void PhysicsStep(float dt, float gravity, CollisionDetector collisionDetector)
    {
        // Apply gravity
        velocity += Vector3.down * gravity * dt;

        // Integrate position
        Vector3 nextPos = position + velocity * dt;

        // Collision detection and response (skip immediately after fracture)
        if (!justFractured)
        {
            Vector3 normal, hitPoint, closestPoint;
            // Check collision at next position
            if (collisionDetector.CheckSphereCollision(nextPos, boundingRadius, out normal, out hitPoint, out closestPoint))
            {
                // Collision detected - resolve it
                ResolveCollision(ref nextPos, normal, hitPoint, closestPoint, dt, gravity, collisionDetector);
            }
            else
            {
                // Not colliding, so not resting
                isResting = false;
            }
        }

        position = nextPos;
    }

    void ResolveCollision(ref Vector3 nextPos, Vector3 normal, Vector3 hitPoint, Vector3 closestPoint, float dt, float gravity, CollisionDetector collisionDetector)
    {
        // --- Geometric snap for horizontal surfaces (eliminate hover gap) ---
        float upDot = Vector3.Dot(normal, Vector3.up);
        const float EPS = 0.0005f; // small epsilon to avoid z-fighting/sinking
        bool usedFlatSnap = false;

        if (upDot > 0.7f)
        {
            // Resting on top of a surface (floor-like): force exact flush
            float targetY = closestPoint.y + halfHeight + EPS;
            nextPos.y = targetY;
            usedFlatSnap = true;

            // Remove all normal component of velocity (both up & down)
            float vn = Vector3.Dot(velocity, normal);
            velocity -= vn * normal;
        }
        else if (upDot < -0.7f)
        {
            // Pressed against a ceiling: force exact flush
            float targetY = closestPoint.y - halfHeight - EPS;
            nextPos.y = targetY;
            usedFlatSnap = true;

            // Remove all normal component of velocity
            float vn = Vector3.Dot(velocity, normal);
            velocity -= vn * normal;
        }
        else
        {
            // For walls / steep surfaces, fall back to sphere-based pushout
            Vector3 penetration = nextPos - hitPoint;
            float penetrationDepth = Vector3.Dot(penetration, normal);
            if (penetrationDepth < 0f)
            {
                nextPos -= normal * penetrationDepth;
                nextPos += normal * 0.001f; // safety margin
            }
        }

        // Normal velocity response (for non-snapped cases); keep as-is
        float normalVelocity = Vector3.Dot(velocity, normal);
        if (!usedFlatSnap && normalVelocity < 0f)
        {
            // Remove normal component with restitution (bounce)
            velocity -= (1f + RESTITUTION) * normalVelocity * normal;
        }

        // Apply kinetic friction to tangential velocity
        Vector3 tangentVelocity = velocity - Vector3.Dot(velocity, normal) * normal;
        float tangentSpeed = tangentVelocity.magnitude;
        if (tangentSpeed > 0.001f)
        {
            // Dynamic friction
            Vector3 frictionDirection = tangentVelocity / tangentSpeed;
            float frictionMagnitude = Mathf.Min(FRICTION * tangentSpeed, tangentSpeed);
            velocity -= frictionDirection * frictionMagnitude;
        }

        // Resting logic: for horizontal surfaces, determine sleep based on tangent speed
        if (usedFlatSnap && upDot > 0.7f)
        {
            if (tangentSpeed < REST_THRESHOLD)
            {
                isResting = true;
                velocity = Vector3.zero; // settle
            }
        }
        else
        {
            // Only apply surface gravity if not resting
            if (!isResting)
            {
                // Project gravity along surface tangent for realistic sliding
                Vector3 gravityVector = Vector3.down * gravity;
                Vector3 tangentGravity = gravityVector - normal * Vector3.Dot(gravityVector, normal);
                velocity += tangentGravity * dt;
            }

            // If moving slowly onto a flat surface (non-snap path) allow resting
            if (upDot > 0.7f && velocity.magnitude < REST_THRESHOLD)
            {
                isResting = true;
                // Dampen and remove residual downward velocity
                velocity *= 0.1f;
                float downVel = Vector3.Dot(velocity, Vector3.down);
                if (downVel > 0f) velocity -= Vector3.down * downVel;
            }
        }

        // Additional check: iterative pushout if still penetrating.
        // IMPORTANT: Skip this when we used the flat snap, otherwise the sphere-based pushout
        // will lift the piece back up to the sphere height (creating the gap again).
        if (!usedFlatSnap)
        {
            Vector3 testNormal, testHit, testClosest;
            int iterations = 0;
            while (iterations < 5 && collisionDetector.CheckSphereCollision(nextPos, boundingRadius, out testNormal, out testHit, out testClosest))
            {
                Vector3 testPenetration = nextPos - testHit;
                float testDepth = Vector3.Dot(testPenetration, testNormal);
                if (testDepth < 0f)
                {
                    nextPos -= testNormal * testDepth;
                    nextPos += testNormal * 0.001f;
                }
                else
                {
                    break;
                }
                iterations++;
            }
        }
    }
}