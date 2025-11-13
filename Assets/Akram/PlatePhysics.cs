using UnityEngine;

/// Manages physics simulation for the intact plate
/// Handles position, velocity, rotation, and angular velocity
/// Implements rotational dynamics without Unity Rigidbody
public class PlatePhysics : MonoBehaviour
{
    private Vector3 position;
    private Vector3 velocity;
    private float fallSpeed;

    // Rotation state
    private Quaternion rotation;
    private Vector3 angularVelocity; // degrees per second

    public Vector3 Position => position;
    public Vector3 Velocity => velocity;
    public Quaternion Rotation => rotation;
    public Vector3 AngularVelocity => angularVelocity;

    public void Initialize(Vector3 startPos, float speed, Quaternion startRot, Vector3 angVel)
    {
        position = startPos;
        fallSpeed = Mathf.Max(speed, 0f);
        velocity = Vector3.down * fallSpeed;
        rotation = startRot;
        angularVelocity = angVel;
    }

    /// Update physics for intact plate (falling motion with rotation)
    public void UpdateIntact(float deltaTime)
    {
        // Linear motion - constant downward velocity
        velocity = Vector3.down * fallSpeed;
        position += velocity * deltaTime;

        // Rotational motion - integrate angular velocity
        if (angularVelocity.sqrMagnitude > 0.001f)
        {
            // Convert angular velocity from degrees/sec to radians/sec
            Vector3 angularVelocityRad = angularVelocity * Mathf.Deg2Rad;

            // Calculate rotation axis and angle
            float angle = angularVelocityRad.magnitude * deltaTime;
            Vector3 axis = angularVelocityRad.normalized;

            // Create incremental rotation quaternion
            Quaternion deltaRotation = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, axis);

            // Apply rotation
            rotation = deltaRotation * rotation;
            rotation.Normalize(); // Prevent drift
        }
    }
}