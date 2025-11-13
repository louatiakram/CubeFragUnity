using UnityEngine;

/// Physics simulation for intact plate
/// Handles constant velocity falling with optional rotation
public class PlatePhysics : MonoBehaviour
{
    private Vector3 position;
    private Vector3 velocity;
    private Quaternion rotation;
    private Vector3 angularVelocity;
    private float fallSpeed;

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

    public void UpdateIntact(float deltaTime)
    {
        // Constant downward velocity
        velocity = Vector3.down * fallSpeed;
        position += velocity * deltaTime;

        // Integrate rotation if any angular velocity
        if (angularVelocity.sqrMagnitude > 0.001f)
        {
            Vector3 angularVelocityRad = angularVelocity * Mathf.Deg2Rad;
            float angle = angularVelocityRad.magnitude * deltaTime;
            Vector3 axis = angularVelocityRad.normalized;
            Quaternion deltaRotation = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, axis);
            rotation = deltaRotation * rotation;
            rotation.Normalize();
        }
    }
}