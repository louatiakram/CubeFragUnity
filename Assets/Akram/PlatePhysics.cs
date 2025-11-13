using UnityEngine;

/// Manages physics simulation for the intact plate
/// Handles position, velocity, and gravity integration
public class PlatePhysics : MonoBehaviour
{
    private Vector3 position;
    private Vector3 velocity;
    private float fallSpeed;

    public Vector3 Position => position;
    public Vector3 Velocity => velocity;

    public void Initialize(Vector3 startPos, float speed)
    {
        position = startPos;
        fallSpeed = Mathf.Max(speed, 0f);
        velocity = Vector3.down * fallSpeed;
    }

    /// Update physics for intact plate (simple falling motion)
    public void UpdateIntact(float deltaTime)
    {
        // Constant downward velocity
        velocity = Vector3.down * fallSpeed;
        position += velocity * deltaTime;
    }
}