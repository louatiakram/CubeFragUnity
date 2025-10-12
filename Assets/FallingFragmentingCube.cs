using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FallingFragmentingCube : MonoBehaviour
{
    [Header("Cube Settings")]
    public float cubeSize = 4f;
    public int fragmentsPerAxis = 4; // 4x4x4 = 64 petits cubes
    public float mass = 10f;

    [Header("Physics Settings")]
    public Vector3 gravity = new Vector3(0, -9.81f, 0);
    public float dt = 0.02f;
    public float fragmentationHeight = 5f; // Height to trigger fragmentation

    [Header("Fragment Settings")]
    public float fragmentMass = 0.1f;
    public float fragmentBounciness = 0.5f;
    public float fragmentRandomForce = 5f;
    public float groundLevel = 0f; // Ground level

    private List<FragmentCube> fragments;
    private bool isFragmented = false;
    private Vector3 velocity = Vector3.zero;

    // Initial drop position for the whole cube
    private float initialHeight = 10f;
    private float currentHeight;

    void Start()
    {
        fragments = new List<FragmentCube>();
        CreateAllFragmentsAsCube();

        // Disable single cube mesh because the cube is visualized by fragments
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null)
            mr.enabled = false;

        currentHeight = initialHeight;
    }

    void Update()
    {
        if (!isFragmented)
        {
            UpdatePreFragmentation();
        }
        else
        {
            UpdateFragments();
        }
    }

    void UpdatePreFragmentation()
    {
        // Simulate falling of the whole cube by lowering all fragments uniformly
        velocity += gravity * dt;
        currentHeight += velocity.y * dt;

        // Update fragments positions accordingly (static, no physics motion yet)
        foreach (FragmentCube frag in fragments)
        {
            Vector3 basePos = frag.initialPosition;
            frag.position = new Vector3(basePos.x, basePos.y + currentHeight, basePos.z);
            frag.UpdateMesh();
        }

        if (currentHeight <= fragmentationHeight)
        {
            TriggerFragmentation();
        }
    }

    void TriggerFragmentation()
    {
        isFragmented = true;

        foreach (FragmentCube fragment in fragments)
        {
            // Assign random initial velocities to fragments to simulate explosion
            fragment.velocity = velocity + new Vector3(
                Random.Range(-fragmentRandomForce, fragmentRandomForce),
                Random.Range(0, fragmentRandomForce),
                Random.Range(-fragmentRandomForce, fragmentRandomForce)
            );
        }
    }

    void UpdateFragments()
    {
        foreach (FragmentCube fragment in fragments)
        {
            fragment.UpdatePhysics(gravity, dt);
            fragment.UpdateMesh();
        }
    }

    void CreateAllFragmentsAsCube()
    {
        float fragmentSize = cubeSize / fragmentsPerAxis;
        float offset = (cubeSize - fragmentSize) / 2f;

        for (int x = 0; x < fragmentsPerAxis; x++)
        {
            for (int y = 0; y < fragmentsPerAxis; y++)
            {
                for (int z = 0; z < fragmentsPerAxis; z++)
                {
                    // Position relative to zero origin at start (will add height later on update)
                    Vector3 fragmentBasePos = new Vector3(
                        -offset + x * fragmentSize,
                        -offset + y * fragmentSize,
                        -offset + z * fragmentSize
                    );

                    FragmentCube fragment = new FragmentCube();
                    fragment.Initialize(fragmentBasePos + new Vector3(0, initialHeight, 0), fragmentSize, fragmentMass, GetRandomColor(), groundLevel, fragmentBounciness);
                    fragment.velocity = Vector3.zero;
                    fragment.initialPosition = fragmentBasePos; // Save base position for 'falling' adjustment

                    fragments.Add(fragment);
                    CreateFragmentGameObject(fragment, fragments.Count - 1);
                }
            }
        }
    }

    void CreateFragmentGameObject(FragmentCube fragment, int index)
    {
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
        return new Color(
            Random.Range(0.5f, 1f),
            Random.Range(0.2f, 0.8f),
            Random.Range(0.1f, 0.6f)
        );
    }
}

public class FragmentCube
{
    public CubeObject cube;
    public Vector3 position;
    public Vector3 velocity;
    public Vector3 angularVelocity;
    public Matrix4x4 rotation = Matrix4x4.identity;
    public float mass;
    public Mesh mesh;
    public MeshFilter meshFilter;

    public Vector3 initialPosition;  // Base position relative to parent before falling

    // Ground collision
    private float groundY = 0f;
    private float bounciness = 0.3f;
    private float friction = 0.95f;
    private bool isAtRest = false;
    private float minimumVelocity = 0.05f;

    public void Initialize(Vector3 pos, float size, float m, Color color, float ground, float bounce)
    {
        position = pos;
        mass = m;
        cube = new CubeObject(size, size, size, color);
        velocity = Vector3.zero;
        groundY = ground;
        bounciness = bounce;

        // Random initial rotation speed
        angularVelocity = new Vector3(
            Random.Range(-5f, 5f),
            Random.Range(-5f, 5f),
            Random.Range(-5f, 5f)
        );
    }

    public void UpdatePhysics(Vector3 gravity, float dt)
    {
        if (isAtRest) return;

        if (position.y > groundY)
        {
            velocity += gravity * dt;
        }

        position += velocity * dt;

        if (position.y <= groundY)
        {
            position.y = groundY;

            if (velocity.y < 0)
            {
                velocity.y = -velocity.y * bounciness;
            }

            velocity.x *= friction;
            velocity.z *= friction;

            if (Mathf.Abs(velocity.y) < minimumVelocity &&
                Mathf.Abs(velocity.x) < minimumVelocity &&
                Mathf.Abs(velocity.z) < minimumVelocity)
            {
                velocity = Vector3.zero;
                angularVelocity = Vector3.zero;
                isAtRest = true;
                position.y = groundY;
            }
        }

        if (!isAtRest)
        {
            UpdateRotation(dt);
        }
    }

    void UpdateRotation(float dt)
    {
        // Create antisymmetric Omega matrix from angular velocity
        Matrix4x4 Omega = Matrix4x4.zero;

        Omega[0, 1] = -angularVelocity.z; Omega[0, 2] = angularVelocity.y;
        Omega[1, 0] = angularVelocity.z; Omega[1, 2] = -angularVelocity.x;
        Omega[2, 0] = -angularVelocity.y; Omega[2, 1] = angularVelocity.x;

        // rotationUpdate = I + Omega * dt
        Matrix4x4 rotationUpdate = Math3D.Add(Matrix4x4.identity, Math3D.MultiplyScalar(Omega, dt));

        // rotation = rotationUpdate * rotation
        rotation = Math3D.MultiplyMatrix4x4(rotationUpdate, rotation);

        // Orthonormalize rotation matrix to prevent drift
        rotation = Math3D.GramSchmidt(rotation);

        // Apply angular friction
        angularVelocity *= 0.99f;
    }

    public void UpdateMesh()
    {
        if (mesh == null || meshFilter == null) return;

        Vector3[] transformedVertices = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            // Apply rotation matrix to cube.vertices[i] then translate by position
            transformedVertices[i] = Math3D.MultiplyMatrixVector3(rotation, cube.vertices[i]) + position;
        }

        mesh.Clear();
        mesh.vertices = transformedVertices;
        mesh.triangles = cube.triangles;
        mesh.RecalculateNormals();
    }

}
