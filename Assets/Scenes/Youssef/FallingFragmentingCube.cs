using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FallingFragmentingCube : MonoBehaviour
{
    [Header("Cube Settings")]
    public float cubeSize = 4f;                      // Total cube size
    public int fragmentsPerAxis = 4;                 // Number of fragments per axis (resolution)
    public float mass = 10f;                         // Total mass of the cube

    [Header("Physics Settings")]
    public Vector3 gravity = new Vector3(0, -9.81f, 0);  // Gravity vector
    public float dt = 0.02f;                         // Physics timestep
    public float fragmentationHeight = 5f;           // Height at which the cube breaks apart

    [Header("Fragment Settings")]
    public float fragmentMass = 0.1f;                // Mass of each fragment
    public float fragmentBounciness = 0.15f;         // Bounciness for fragments
    public float fragmentRandomForce = 0.5f;         // Random force after fragmentation
    public float groundLevel = 0f;                   // Y position of the ground
    public float restitution = 0.2f;                 // Coefficient of restitution
    public float groundFriction = 0.7f;              // Friction on the ground

    [Header("Debug Visualization")]
    public bool showCollisionDebug = true;           // Show collision lines in Scene
    public bool showContactPoints = true;            // Show contact points on ground

    private List<FragmentCube> fragments;            // List of all fragments
    private bool isFragmented = false;               // Whether the cube has fragmented
    private Vector3 velocity = Vector3.zero;         // Velocity before fragmentation

    private float initialHeight = 10f;               // Start height
    private float currentHeight;                     // Current cube height

    void Start()
    {
        // Initialize fragments and hide the main cube mesh
        fragments = new List<FragmentCube>();
        CreateAllFragmentsAsCube();

        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null)
            mr.enabled = false;

        currentHeight = initialHeight;
    }

    void Update()
    {
        // Before and after fragmentation behavior
        if (!isFragmented)
            UpdatePreFragmentation();
        else
            UpdateFragments();
    }

    void UpdatePreFragmentation()
    {
        // Apply gravity to cube as a whole
        velocity += gravity * dt;
        currentHeight += velocity.y * dt;

        // Move all fragments as one solid cube
        foreach (FragmentCube frag in fragments)
        {
            Vector3 basePos = frag.initialPosition;
            frag.position = new Vector3(basePos.x, basePos.y + currentHeight, basePos.z);
            frag.UpdateMesh();
        }

        // Trigger fragmentation when cube hits threshold
        if (currentHeight <= fragmentationHeight)
            TriggerFragmentation();
    }

    void TriggerFragmentation()
    {
        isFragmented = true;

        // Add small random velocities to each fragment
        foreach (FragmentCube fragment in fragments)
        {
            fragment.velocity = velocity + new Vector3(
                Random.Range(-fragmentRandomForce, fragmentRandomForce),
                Random.Range(0, fragmentRandomForce * 0.3f),
                Random.Range(-fragmentRandomForce, fragmentRandomForce)
            );
        }
    }

    void UpdateFragments()
    {
        // Update each fragment’s physics
        foreach (FragmentCube fragment in fragments)
            fragment.UpdatePhysics(gravity, dt, groundLevel);

        // Check and resolve collisions between fragments
        for (int i = 0; i < fragments.Count; i++)
        {
            for (int j = i + 1; j < fragments.Count; j++)
                CheckAndResolveCollision(fragments[i], fragments[j], restitution);
        }

        // Update all meshes for rendering
        foreach (FragmentCube fragment in fragments)
            fragment.UpdateMesh();
    }

    void CheckAndResolveCollision(FragmentCube a, FragmentCube b, float restitution)
    {
        // Compute distance between two fragments
        Vector3 delta = b.position - a.position;
        float distance = delta.magnitude;
        float minDistance = (a.size + b.size) / 2f * 1.1f;

        // Collision check
        if (distance < minDistance && distance > 0.001f)
        {
            Vector3 normal = delta.normalized;
            float overlap = minDistance - distance;

            // Draw debug lines
            if (showCollisionDebug)
            {
                Debug.DrawLine(a.position, b.position, Color.red, 0.1f);
                Debug.DrawRay(a.position, normal * a.size, Color.yellow, 0.1f);
            }

            // Separate cubes based on mass
            float totalMass = a.mass + b.mass;
            float moveA = overlap * (b.mass / totalMass);
            float moveB = overlap * (a.mass / totalMass);

            a.position -= normal * moveA;
            b.position += normal * moveB;

            // Calculate impulse
            Vector3 relativeVelocity = b.velocity - a.velocity;
            float velocityAlongNormal = Vector3.Dot(relativeVelocity, normal);
            if (velocityAlongNormal > 0)
                return;

            float impulseScalar = -(1 + restitution) * velocityAlongNormal;
            impulseScalar /= (1 / a.mass + 1 / b.mass);
            Vector3 impulse = impulseScalar * normal;

            // Apply impulse
            a.velocity -= impulse / a.mass;
            b.velocity += impulse / b.mass;

            // Add rotational effects
            Vector3 contactPoint = a.position + normal * (a.size / 2f);
            Vector3 rA = contactPoint - a.position;
            Vector3 rB = contactPoint - b.position;
            float angularImpulse = 0.3f;
            a.angularVelocity += Vector3.Cross(rA, -impulse) * angularImpulse / a.momentOfInertia;
            b.angularVelocity += Vector3.Cross(rB, impulse) * angularImpulse / b.momentOfInertia;
        }
    }

    void OnDrawGizmos()
    {
        // Draw debug cubes and contact points
        if (!Application.isPlaying || fragments == null || !isFragmented)
            return;

        foreach (var fragment in fragments)
        {
            Gizmos.color = fragment.isAtRest ? Color.green :
                (fragment.isGrounded ? Color.yellow : Color.cyan);

            Gizmos.DrawWireCube(fragment.position, Vector3.one * fragment.size);

            if (showContactPoints && fragment.isGrounded)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(fragment.groundContactPoint, 0.05f);
                Gizmos.DrawLine(fragment.position, fragment.groundContactPoint);
            }
        }
    }

    void CreateAllFragmentsAsCube()
    {
        // Divide the cube into small fragments
        float fragmentSize = cubeSize / fragmentsPerAxis;
        float offset = (cubeSize - fragmentSize) / 2f;

        for (int x = 0; x < fragmentsPerAxis; x++)
        {
            for (int y = 0; y < fragmentsPerAxis; y++)
            {
                for (int z = 0; z < fragmentsPerAxis; z++)
                {
                    Vector3 fragmentBasePos = new Vector3(
                        -offset + x * fragmentSize,
                        -offset + y * fragmentSize,
                        -offset + z * fragmentSize
                    );

                    // Create and initialize each fragment
                    FragmentCube fragment = new FragmentCube();
                    fragment.Initialize(fragmentBasePos + new Vector3(0, initialHeight, 0), fragmentSize, fragmentMass, GetRandomColor(), groundLevel, fragmentBounciness);
                    fragment.velocity = Vector3.zero;
                    fragment.initialPosition = fragmentBasePos;

                    fragments.Add(fragment);
                    CreateFragmentGameObject(fragment, fragments.Count - 1);
                }
            }
        }
    }

    void CreateFragmentGameObject(FragmentCube fragment, int index)
    {
        // Create fragment GameObject and attach mesh
        GameObject fragmentGO = new GameObject($"Fragment_{index}");
        fragmentGO.transform.parent = transform;

        MeshFilter meshFilter = fragmentGO.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = fragmentGO.AddComponent<MeshRenderer>();

        meshRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        meshRenderer.material.color = fragment.cube.color;

        fragment.meshFilter = meshFilter;
        fragment.mesh = new Mesh();
        meshFilter.mesh = fragment.mesh;
    }

    Color GetRandomColor()
    {
        // Random pastel-like color
        return new Color(
            Random.Range(0.5f, 1f),
            Random.Range(0.2f, 0.8f),
            Random.Range(0.1f, 0.6f)
        );
    }
}

// Handles individual cube fragment physics and rendering
public class FragmentCube
{
    public CubeObject cube;
    public Vector3 position, velocity, angularVelocity;
    public Matrix4x4 rotation = Matrix4x4.identity;
    public float mass, size, momentOfInertia;
    public Mesh mesh;
    public MeshFilter meshFilter;

    public Vector3 initialPosition;
    public bool isAtRest = false, isGrounded = false;
    public Vector3 groundContactPoint;

    private float bounciness = 0.15f;
    private float restTimer = 0f;
    private float energyLossPerBounce = 0.7f;

    public void Initialize(Vector3 pos, float cubeSize, float m, Color color, float ground, float bounce)
    {
        // Initialize fragment cube with physics and visuals
        position = pos;
        mass = m;
        size = cubeSize;
        cube = new CubeObject(cubeSize, cubeSize, cubeSize, color);
        velocity = Vector3.zero;
        bounciness = bounce;

        momentOfInertia = (1f / 6f) * mass * size * size;

        // Small random spin
        angularVelocity = new Vector3(
            Random.Range(-1.5f, 1.5f),
            Random.Range(-1.5f, 1.5f),
            Random.Range(-1.5f, 1.5f)
        );
    }

    public void UpdatePhysics(Vector3 gravity, float dt, float groundY)
    {
        // Skip if resting
        if (isAtRest) return;

        // Apply gravity and drag
        velocity += gravity * dt;
        velocity -= velocity * 0.2f * dt;

        // Move fragment
        position += velocity * dt;

        // Check for ground collision
        Vector3[] worldVertices = new Vector3[8];
        for (int i = 0; i < 8; i++)
            worldVertices[i] = Math3D.MultiplyMatrixVector3(rotation, cube.vertices[i]) + position;

        // Find lowest vertex
        float lowestY = float.MaxValue;
        int lowestIdx = -1;
        for (int i = 0; i < 8; i++)
        {
            if (worldVertices[i].y < lowestY)
            {
                lowestY = worldVertices[i].y;
                lowestIdx = i;
            }
        }

        // Ground collision and bounce logic
        if (lowestY <= groundY + 0.01f)
        {
            isGrounded = true;
            float penetration = groundY - lowestY;
            position.y += penetration;

            Vector3 contactPoint = worldVertices[lowestIdx];
            contactPoint.y = groundY;
            groundContactPoint = contactPoint;

            Vector3 r = contactPoint - position;
            Vector3 vContact = velocity + Vector3.Cross(angularVelocity, r);

            // Apply bounce
            if (vContact.y < -0.05f)
                velocity.y = -vContact.y * bounciness * 0.3f;
            else
                velocity.y = 0f;

            // Friction on ground
            Vector3 vTangent = new Vector3(vContact.x, 0, vContact.z);
            if (vTangent.magnitude > 0.01f)
                velocity -= vTangent.normalized * 10f * dt;

            // Damping and rest detection
            velocity *= 0.8f;
            angularVelocity *= 0.7f;

            if (velocity.magnitude < 0.05f && angularVelocity.magnitude < 0.1f)
            {
                restTimer += dt;
                if (restTimer > 0.25f)
                {
                    velocity = Vector3.zero;
                    angularVelocity = Vector3.zero;
                    isAtRest = true;
                    position.y = groundY + size / 2f;
                    rotation = Matrix4x4.identity;
                }
            }
            else
                restTimer = 0f;
        }
        else
        {
            isGrounded = false;
            restTimer = 0f;
        }

        // Update rotation
        if (!isAtRest && angularVelocity.magnitude > 0.02f)
            UpdateRotation(dt);
    }

    void UpdateRotation(float dt)
    {
        // Update rotation based on angular velocity
        Matrix4x4 Omega = Matrix4x4.zero;
        Omega[0, 1] = -angularVelocity.z; Omega[0, 2] = angularVelocity.y;
        Omega[1, 0] = angularVelocity.z; Omega[1, 2] = -angularVelocity.x;
        Omega[2, 0] = -angularVelocity.y; Omega[2, 1] = angularVelocity.x;

        Matrix4x4 rotationUpdate = Math3D.Add(Matrix4x4.identity, Math3D.MultiplyScalar(Omega, dt));
        rotation = Math3D.MultiplyMatrix4x4(rotationUpdate, rotation);
        rotation = Math3D.GramSchmidt(rotation);
    }

    public void UpdateMesh()
    {
        // Apply transformations to mesh vertices
        if (mesh == null || meshFilter == null) return;

        Vector3[] transformedVertices = new Vector3[8];
        for (int i = 0; i < 8; i++)
            transformedVertices[i] = Math3D.MultiplyMatrixVector3(rotation, cube.vertices[i]) + position;

        mesh.Clear();
        mesh.vertices = transformedVertices;
        mesh.triangles = cube.triangles;
        mesh.RecalculateNormals();
    }
}
