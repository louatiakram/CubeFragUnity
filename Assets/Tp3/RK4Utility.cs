using UnityEngine;

public static class RK4Utility
{
    public static (Vector3, Vector3) RK4SolverMethod(Vector3 position, Vector3 velocity, Vector3 acceleration, float dt)
    {
        Vector3 k1 = dt*velocity;
        Vector3 l1 = dt*acceleration;

        Vector3 k2 = dt*(velocity+0.5f*l1); // Assuming constant acceleration
        Vector3 l2 = dt * (acceleration + 0.5f * l1);

        Vector3 k3 = dt * (velocity + 0.5f * l2); // Assuming constant acceleration
        Vector3 l3 = dt * (acceleration + 0.5f * l2);

        Vector3 k4 = dt * (velocity + 0.5f * l3); // Assuming constant acceleration
        Vector3 l4 = dt * (acceleration + 0.5f * l3);

        Vector3 newVelocity = velocity +  (k1 + 2.0f * k2 + 2.0f * k3 + k4)/6.0f;
        Vector3 newPosition = position +  (l1 + 2.0f * l2 + 2.0f * l3 + l4)/6.0f;
        return (newPosition, newVelocity);
    }

}
