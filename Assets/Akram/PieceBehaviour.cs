
using UnityEngine;

/// Individual fragment piece behaviour using CustomRB physics
public class PieceBehaviour : MonoBehaviour
{
    private Vector3 localCenter;
    private float mass;
    private float boundingRadius;
    private float halfHeight;

    private CustomRB rb;
    private bool isInitialized = false;
    private bool justFractured = false;
    private int fractureFrameCount = 0;
    private bool isResting = false;
    private Transform supportTransform = null;

    private const float RESTITUTION = 0.05f;
    private const float TIME_STEP = 1f / 60f;
    private const int FRACTURE_GRACE_FRAMES = 3;
    private const float REST_THRESHOLD = 0.1f;
    private const float SLEEP_THRESHOLD = 0.05f;

    private float timeAccumulator = 0f;
    public Vector3 LocalCenter => localCenter;

    public void SetupGeometry(Vector3 center, float m, float radius)
    {
        localCenter = center;
        mass = Mathf.Max(m, 0.01f);
        boundingRadius = radius;
        halfHeight = 0.5f * transform.lossyScale.y;
    }

    public void Initialize(Vector3 startPos, Vector3 startVel, Vector3 impactPoint, Vector3 normal, float impulseStrength, float impactRadius)
    {
        rb = new CustomRB(startPos, mass);
        rb.SetVelocity(startVel);
        isInitialized = true;
        justFractured = true;
        fractureFrameCount = 0;
        isResting = false;
        supportTransform = null;
        ApplyFractureImpulse(impactPoint, normal, impulseStrength, impactRadius);
    }

    void ApplyFractureImpulse(Vector3 impactPoint, Vector3 normal, float impulseStrength, float impactRadius)
    {
        Vector3 toFragment = rb.Position - impactPoint;
        float distance = toFragment.magnitude;
        float falloff = Mathf.Clamp01(1f - (distance / impactRadius));
        Vector3 n = (normal.sqrMagnitude > 1e-6f) ? normal.normalized : Vector3.up;

        if (falloff < 0.01f)
        {
            rb.AddForce(n * 0.05f);
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

        rb.AddForce(direction * (impulseMagnitude / Mathf.Max(mass, 1e-6f)));
        rb.AddForce(n * (0.15f * effectiveFalloff));
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
            float targetY = max.y + halfHeight + EPS;
            rb.SetPosition(new Vector3(rb.Position.x, targetY, rb.Position.z));
            transform.position = rb.Position;
            return; // Skip physics update when resting
        }

        timeAccumulator += Mathf.Min(deltaTime, 0.05f);
        while (timeAccumulator >= TIME_STEP)
        {
            timeAccumulator -= TIME_STEP;
            PhysicsStep(TIME_STEP, gravity, collisionDetector);
        }

        transform.position = rb.Position;
    }

    void PhysicsStep(float dt, float gravity, CollisionDetector collisionDetector)
    {
        // Skip physics if resting
        if (isResting) return;

        rb.ApplyGravity(dt);
        Vector3 nextPos = rb.Position + rb.Velocity * dt;

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
                isResting = false;
                supportTransform = null;
            }
        }

        rb.SetPosition(nextPos);
    }

    void ResolveCollision(ref Vector3 nextPos, Vector3 normal, Vector3 hitPoint, ObstacleBounds hitObstacle)
    {
        float upDot = Vector3.Dot(normal, Vector3.up);
        const float EPS = 0.0005f;
        bool usedFlatSnap = false;

        // Check if we hit ground (only stop on objects marked as ground)
        if (hitObstacle != null && hitObstacle.isGround && upDot > 0.7f)
        {
            Vector3 min, max;
            hitObstacle.GetWorldAABB(out min, out max);
            float targetY = max.y + halfHeight + EPS;
            nextPos.y = targetY;
            usedFlatSnap = true;
            supportTransform = hitObstacle.transform;

            // Stop all movement when landing on ground
            rb.SetVelocity(Vector3.zero);
            isResting = true;
        }
        else if (upDot > 0.7f)
        {
            // Hitting a horizontal surface that's NOT ground - bounce but don't stop completely
            Vector3 min, max;
            hitObstacle.GetWorldAABB(out min, out max);
            float targetY = max.y + halfHeight + EPS;
            nextPos.y = targetY;
            usedFlatSnap = true;
            supportTransform = hitObstacle.transform;

            // Remove vertical velocity but keep horizontal movement
            Vector3 velocity = rb.Velocity;
            velocity.y = 0f;
            rb.SetVelocity(velocity);

            // Don't set isResting for non-ground surfaces
            isResting = false;
        }
        else if (upDot < -0.7f)
        {
            // Hitting ceiling - stop vertical movement
            Vector3 min, max;
            hitObstacle.GetWorldAABB(out min, out max);
            float targetY = min.y - halfHeight - EPS;
            nextPos.y = targetY;
            usedFlatSnap = true;

            // Remove vertical velocity component
            Vector3 velocity = rb.Velocity;
            velocity.y = Mathf.Min(velocity.y, 0f); // Only stop upward movement
            rb.SetVelocity(velocity);
        }
        else
        {
            // Hitting vertical surface - stop movement into the surface but allow sliding
            float normalVelocity = Vector3.Dot(rb.Velocity, normal);
            if (normalVelocity < 0f)
            {
                rb.SetVelocity(rb.Velocity - (1f + RESTITUTION) * normalVelocity * normal);
            }
        }

        // Only go to rest state on ground objects
        if (usedFlatSnap && hitObstacle != null && hitObstacle.isGround && upDot > 0.7f && rb.Velocity.magnitude < REST_THRESHOLD)
        {
            isResting = true;
            rb.SetVelocity(Vector3.zero);
        }
    }
}
