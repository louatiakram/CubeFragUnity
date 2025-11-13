using UnityEngine;

/// Custom physics body with rotation (no Unity Rigidbody)
/// Implements realistic linear and angular dynamics
public class CustomRB
{
    public Vector3 Position { get; private set; }
    public Vector3 Velocity { get; private set; }
    public Quaternion Rotation { get; private set; }
    public Vector3 AngularVelocity { get; private set; }
    public float Mass { get; private set; }

    private Vector3 inertiaTensor;
    private const float GRAVITY = 9.81f;

    public CustomRB(Vector3 startPos, float mass, Quaternion startRot, Vector3 dimensions)
    {
        Position = startPos;
        Mass = Mathf.Max(mass, 0.01f);
        Rotation = startRot;
        Velocity = Vector3.zero;
        AngularVelocity = Vector3.zero;

        // Calculate inertia tensor for box: I = m/12 * (h² + d²)
        float m = Mass;
        inertiaTensor = new Vector3(
            m * (dimensions.y * dimensions.y + dimensions.z * dimensions.z) / 12f,
            m * (dimensions.x * dimensions.x + dimensions.z * dimensions.z) / 12f,
            m * (dimensions.x * dimensions.x + dimensions.y * dimensions.y) / 12f
        );

        // Prevent zero inertia
        inertiaTensor.x = Mathf.Max(inertiaTensor.x, 0.001f);
        inertiaTensor.y = Mathf.Max(inertiaTensor.y, 0.001f);
        inertiaTensor.z = Mathf.Max(inertiaTensor.z, 0.001f);
    }

    public void AddForce(Vector3 force)
    {
        Velocity += force / Mass;
    }

    public void AddTorque(Vector3 torque)
    {
        // τ = I * α → α = τ / I
        Vector3 angularAcceleration = new Vector3(
            torque.x / inertiaTensor.x,
            torque.y / inertiaTensor.y,
            torque.z / inertiaTensor.z
        );
        AngularVelocity += angularAcceleration;
    }

    public void AddForceAtPoint(Vector3 force, Vector3 point)
    {
        AddForce(force);
        Vector3 r = point - Position;
        Vector3 torque = Vector3.Cross(r, force);
        AddTorque(torque);
    }

    public void ApplyGravity(float dt)
    {
        Velocity += Vector3.down * GRAVITY * dt;
    }

    public void ApplyAngularDamping(float damping, float dt)
    {
        float factor = Mathf.Max(0f, 1f - damping * dt);
        AngularVelocity *= factor;
    }

    public void Integrate(float dt)
    {
        // Linear integration
        Position += Velocity * dt;

        // Angular integration
        if (AngularVelocity.sqrMagnitude > 1e-6f)
        {
            float angle = AngularVelocity.magnitude * dt;
            Vector3 axis = AngularVelocity.normalized;
            Quaternion deltaRotation = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, axis);
            Rotation = deltaRotation * Rotation;
            Rotation.Normalize();
        }
    }

    public void SetPosition(Vector3 pos) => Position = pos;
    public void SetVelocity(Vector3 vel) => Velocity = vel;
    public void SetRotation(Quaternion rot) => Rotation = rot;
    public void SetAngularVelocity(Vector3 angVel) => AngularVelocity = angVel;
}