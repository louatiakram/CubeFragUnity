using UnityEngine;

/// Lightweight physics body (no Unity Rigidbody)
public class CustomRB
{
    public Vector3 Position { get; private set; }
    public Vector3 Velocity { get; private set; }
    public float Mass { get; private set; }

    private float gravity = 9.81f;

    public CustomRB(Vector3 startPos, float mass)
    {
        Position = startPos;
        Mass = Mathf.Max(mass, 0.01f);
        Velocity = Vector3.zero;
    }

    public void AddForce(Vector3 force)
    {
        Velocity += force / Mass;
    }

    public void ApplyGravity(float dt)
    {
        Velocity += Vector3.down * gravity * dt;
    }

    public void Integrate(float dt)
    {
        Position += Velocity * dt;
    }

    public void SetPosition(Vector3 pos) => Position = pos;
    public void SetVelocity(Vector3 vel) => Velocity = vel;
}
