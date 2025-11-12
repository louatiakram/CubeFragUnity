using UnityEngine;

// Chain Link (Fragment/Rigid Body)
[System.Serializable]
public class ChainLink
{
    public Vec3 position;           // Center of mass position
    public Vec3 velocity;           // Linear velocity
    public Vec3 angularVelocity;    // Angular velocity
    public Mat4 orientation;        // Rotation matrix
    public float mass;              // Mass
    public float inertia;           // Moment of inertia (scalar for simplicity)
    public Vec3 force;              // Accumulated force
    public Vec3 torque;             // Accumulated torque
    public float size;              // Visual size of cube

    public ChainLink(Vec3 startPos, float linkMass, float linkSize)
    {
        position = startPos;
        velocity = Vec3.Zero;
        angularVelocity = Vec3.Zero;
        orientation = Mat4.Identity();
        mass = linkMass;
        size = linkSize;

        // Inertia for cube: I = (1/6) * m * size^2
        inertia = (1.0f / 6.0f) * mass * size * size;

        force = Vec3.Zero;
        torque = Vec3.Zero;
    }

    public void ClearForces()
    {
        force = Vec3.Zero;
        torque = Vec3.Zero;
    }

    public void AddForce(Vec3 f)
    {
        force = force + f;
    }

    public void AddTorque(Vec3 t)
    {
        torque = torque + t;
    }

    // Apply impulse at center of mass
    public void ApplyImpulse(Vec3 impulse)
    {
        velocity = velocity + (impulse / mass);
    }

    // Apply impulse at a point (creates torque)
    public void ApplyImpulseAtPoint(Vec3 impulse, Vec3 point)
    {
        // Linear component
        velocity = velocity + (impulse / mass);

        // Angular component
        Vec3 r = point - position;
        Vec3 angularImpulse = Vec3.Cross(r, impulse);
        angularVelocity = angularVelocity + (angularImpulse / inertia);
    }
}
