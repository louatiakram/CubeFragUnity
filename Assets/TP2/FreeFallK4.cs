using UnityEngine;

public class FreeFallK4 : MonoBehaviour
{
    [Header("Physical parameters")]
    public Vector3 velocity = Vector3.zero;
    public float gravity = 9.81f;

    [Tooltip("Coefficient de frottement visqueux (kg/s)")]
    public float damping = 0.5f;

    [Tooltip("Si vrai, dt = Time.fixedDeltaTime (recommandé). Sinon utilisez 'customDt'.")]
    public bool useFixedDeltaTime = true;
    public float customDt = 0.002f;

    [Header("Initial / Visual")]
    public Vector3 startPosition = new Vector3(0f, 5f, 0f);
    public Color cubeColor = Color.red;

    // Internals
    private Vector3 position;
    private CubeObject cubeObject;

    void Start()
    {
        position = startPosition;
        velocity = Vector3.zero;

        cubeObject = new CubeObject(position, cubeColor, "FreeFallCube");
        ApplyTranslation(position);
    }

    void FixedUpdate()
    {
        float dt = useFixedDeltaTime ? Time.fixedDeltaTime : customDt;

        // Intégration RK4
        RK4Step(dt);

        // Collision sol
        if (position.y <= 0f)
        {
            position.y = 0f;
            velocity = Vector3.zero;
        }

        ApplyTranslation(position);
    }

    // ----------- RK4 Integration ------------
    void RK4Step(float dt)
    {
        // k1
        Vector3 k1_v = Acceleration(position, velocity);
        Vector3 k1_p = velocity;

        // k2
        Vector3 k2_v = Acceleration(position + 0.5f * dt * k1_p,
                                    velocity + 0.5f * dt * k1_v);
        Vector3 k2_p = velocity + 0.5f * dt * k1_v;

        // k3
        Vector3 k3_v = Acceleration(position + 0.5f * dt * k2_p,
                                    velocity + 0.5f * dt * k2_v);
        Vector3 k3_p = velocity + 0.5f * dt * k2_v;

        // k4
        Vector3 k4_v = Acceleration(position + dt * k3_p,
                                    velocity + dt * k3_v);
        Vector3 k4_p = velocity + dt * k3_v;

        // Update position & velocity
        position += dt / 6f * (k1_p + 2f * k2_p + 2f * k3_p + k4_p);
        velocity += dt / 6f * (k1_v + 2f * k2_v + 2f * k3_v + k4_v);
    }
        // Forces initiales
          
        

    // ----------- Acceleration function ------------
    Vector3 Acceleration(Vector3 pos, Vector3 vel)
    {
        return Vector3.down * gravity - damping * vel;
    }

    // ----------- Apply Translation Matrix ------------
    void ApplyTranslation(Vector3 t)
    {
        float[,] T = new float[,]
        {
            {1f, 0f, 0f, t.x},
            {0f, 1f, 0f, t.y},
            {0f, 0f, 1f, t.z},
            {0f, 0f, 0f, 1f}
        };

        cubeObject.ApplyMatrix(T);
    }

    private void OnDestroy()
    {
        if (cubeObject != null) cubeObject.Destroy();
    }
}
