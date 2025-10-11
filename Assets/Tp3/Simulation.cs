using UnityEngine;

public class Simulation : MonoBehaviour
{
    public GameObject mass1;
    public GameObject mass2;

    // Physics parameters
    public float m1 = 1f;
    public float m2 = 1f;
    public float k1 = 20f;
    public float k2 = 20f;
    public float c1 = 1f;
    public float c2 = 1f;
    public float L0 = 2f;
    public float dt = 0.02f;
    public float gravity = 9.81f;

    // States (y = position, v = velocity)
    private Vector3 pos1;
    private Vector3 pos2;
    private Vector3 vel1; 
    private Vector3 vel2;


    void Start()
    {
        // Initialize positions (for example stacked vertically)
        pos1 = mass1.transform.position;
        pos2 = mass2.transform.position;
        vel1 = Vector3.zero;
        vel2 = Vector3.zero;
    }

    void Update()
    {
        RK4Step();
        UpdateMassPositions();
    }

    // Returns derivatives: (dy1, dv1, dy2, dv2)
    Vector4 Derivative(Vector4 state)
    {
        float y1 = state.x, v1 = state.y;
        float y2 = state.z, v2 = state.w;

        // Spring 1: attached to fixed point at y=0
        float F1 = -k1 * (y1 - L0) - c1 * v1;

        // Spring 2: between mass1 & mass2
        float spring12 = -k2 * ((y2 - y1) - L0);
        F1 += -spring12; // reaction force
        float F2 = spring12 - c2 * v2;

        // Gravity
        F1 += -m1 * gravity;
        F2 += -m2 * gravity;

        float a1 = F1 / m1;
        float a2 = F2 / m2;

        return new Vector4(v1, a1, v2, a2);
    }

    void RK4Step()
    {
        Vector4 state = new Vector4(pos1.y, vel1.y, pos2.y, vel2.y);

        Vector4 k1 = dt * Derivative(state);
        Vector4 k2 = dt * Derivative(state + 0.5f * k1);
        Vector4 k3 = dt * Derivative(state + 0.5f * k2);
        Vector4 k4 = dt * Derivative(state + k3);

        Vector4 delta = (k1 + 2 * k2 + 2 * k3 + k4) / 6f;

        pos1.y += delta.x;
        vel1.y += delta.y;
        pos2.y += delta.z;
        vel2.y += delta.w;
    }

    void UpdateMassPositions()
    {
        mass1.transform.position = pos1;
        mass2.transform.position = pos2;
    }
}
