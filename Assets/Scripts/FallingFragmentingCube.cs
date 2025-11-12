using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FallingFragmentingCube : MonoBehaviour
{
    [Header("Cube Settings")]
    public float cubeSize = 4f;
    public int fragmentsPerAxis = 4;
    public float mass = 10f;

    [Header("Physics Settings")]
    public Vector3 gravity = new Vector3(0, -9.81f, 0);
    public float dt = 0.02f;
    public float fragmentationHeight = 5f;

    [Header("Fragment Settings")]
    public float fragmentMass = 0.1f;
    public float fragmentBounciness = 0.15f;
    public float fragmentRandomForce = 0.5f;
    public float groundLevel = 0f;
    public float restitution = 0.2f;
    public float groundFriction = 0.7f;

    [Header("Debug Visualization")]
    public bool showCollisionDebug = true;
    public bool showContactPoints = true;

    private List<FragmentCube> fragments;
    private bool isFragmented = false;
    private Vector3 velocity = Vector3.zero;

    private float initialHeight = 10f;
    private float currentHeight;

    void Start()
    {
        fragments = new List<FragmentCube>();
        CreateAllFragmentsAsCube();

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
        velocity += gravity * dt;
        currentHeight += velocity.y * dt;

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
            fragment.velocity = velocity + new Vector3(
                Random.Range(-fragmentRandomForce, fragmentRandomForce),
                Random.Range(0, fragmentRandomForce * 0.3f),
                Random.Range(-fragmentRandomForce, fragmentRandomForce)
            );
        }
    }

    void UpdateFragments()
    {
        // Update physics for all fragments
        foreach (FragmentCube fragment in fragments)
        {
            fragment.UpdatePhysics(gravity, dt, groundLevel);
        }

        // Check collisions between fragments
        for (int i = 0; i < fragments.Count; i++)
        {
            for (int j = i + 1; j < fragments.Count; j++)
            {
                CheckAndResolveCollision(fragments[i], fragments[j], restitution);
            }
        }

        // Update meshes
        foreach (FragmentCube fragment in fragments)
        {
            fragment.UpdateMesh();
        }
    }

    void CheckAndResolveCollision(FragmentCube a, FragmentCube b, float restitution)
    {
        Vector3 delta = b.position - a.position;
        float distance = delta.magnitude;
        float minDistance = (a.size + b.size) / 2f * 1.1f;

        if (distance < minDistance && distance > 0.001f)
        {
            Vector3 normal = delta.normalized;
            float overlap = minDistance - distance;

            // Visual debug
            if (showCollisionDebug)
            {
                Debug.DrawLine(a.position, b.position, Color.red, 0.1f);
                Debug.DrawRay(a.position, normal * a.size, Color.yellow, 0.1f);
            }

            // Separate the cubes
            float totalMass = a.mass + b.mass;
            float moveA = overlap * (b.mass / totalMass);
            float moveB = overlap * (a.mass / totalMass);

            a.position -= normal * moveA;
            b.position += normal * moveB;

            // Calculate relative velocity
            Vector3 relativeVelocity = b.velocity - a.velocity;
            float velocityAlongNormal = Vector3.Dot(relativeVelocity, normal);

            if (velocityAlongNormal > 0)
                return;

            // Calculate impulse
            float impulseScalar = -(1 + restitution) * velocityAlongNormal;
            impulseScalar /= (1 / a.mass + 1 / b.mass);

            Vector3 impulse = impulseScalar * normal;

            a.velocity -= impulse / a.mass;
            b.velocity += impulse / b.mass;

            // Contact point for torque
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
        if (!Application.isPlaying || fragments == null || !isFragmented)
            return;

        foreach (var fragment in fragments)
        {
            if (fragment.isAtRest)
            {
                Gizmos.color = Color.green;
            }
            else
            {
                Gizmos.color = fragment.isGrounded ? Color.yellow : Color.cyan;
            }
            
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

    public Vector3 initialPosition;
    public float size;
    public float momentOfInertia;

    // Debug info
    public bool isAtRest = false;
    public bool isGrounded = false;
    public Vector3 groundContactPoint;

    private float bounciness = 0.15f;
    private float restTimer = 0f;
    private int groundContactFrames = 0;
    private float energyLossPerBounce = 0.7f; // Lose 70% energy per bounce

    public void Initialize(Vector3 pos, float cubeSize, float m, Color color, float ground, float bounce)
    {
        position = pos;
        mass = m;
        size = cubeSize;
        cube = new CubeObject(cubeSize, cubeSize, cubeSize, color);
        velocity = Vector3.zero;
        bounciness = bounce;

        momentOfInertia = (1f / 6f) * mass * size * size;

        // Small initial spin
        angularVelocity = new Vector3(
            Random.Range(-1.5f, 1.5f),
            Random.Range(-1.5f, 1.5f),
            Random.Range(-1.5f, 1.5f)
        );
    }

    public void UpdatePhysics(Vector3 gravity, float dt, float groundY)
    {
        if (isAtRest) return;

        // Apply gravity
        velocity += gravity * dt;

        // Air drag (stronger)
        float airDrag = 0.2f;
        velocity -= velocity * airDrag * dt;

        // Move
        position += velocity * dt;

        // Compute vertices
        Vector3[] worldVertices = new Vector3[8];
        for (int i = 0; i < 8; i++)
            worldVertices[i] = Math3D.MultiplyMatrixVector3(rotation, cube.vertices[i]) + position;

        // Lowest vertex
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

        if (lowestY <= groundY + 0.01f)
        {
            isGrounded = true;
            groundContactFrames++;

            float penetration = groundY - lowestY;
            position.y += penetration;

            // Contact
            Vector3 contactPoint = worldVertices[lowestIdx];
            contactPoint.y = groundY;
            groundContactPoint = contactPoint;
            Vector3 r = contactPoint - position;

            // Velocity at contact
            Vector3 vContact = velocity + Vector3.Cross(angularVelocity, r);
            float vNormal = vContact.y;

            // Bounce (heavily damped)
            if (vNormal < -0.05f)
            {
                velocity.y = -vNormal * bounciness * 0.3f; // strong damping
                angularVelocity *= 0.6f;
            }
            else
            {
                velocity.y = 0f;
            }

            // Friction (strong horizontal slowdown)
            Vector3 vTangent = new Vector3(vContact.x, 0, vContact.z);
            if (vTangent.magnitude > 0.01f)
            {
                float friction = 10f; // super strong ground friction
                velocity -= vTangent.normalized * friction * dt;
                angularVelocity *= 0.8f;
            }

            // Count vertices on ground
            int verticesOnGround = 0;
            for (int i = 0; i < 8; i++)
                if (worldVertices[i].y <= groundY + 0.2f)
                    verticesOnGround++;

            // Damping for motion & spin
            velocity *= 0.8f;
            angularVelocity *= 0.7f;

            // Rest detection
            bool hasStableBase = verticesOnGround >= 3;
            bool movingSlowly = velocity.magnitude < 0.05f;
            bool rotatingSlowly = angularVelocity.magnitude < 0.1f;

            if (hasStableBase && movingSlowly && rotatingSlowly)
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
            {
                restTimer = 0f;
            }
        }
        else
        {
            isGrounded = false;
            groundContactFrames = 0;
            restTimer = 0f;
        }

        // Final rotation and angular damping
        if (!isAtRest)
        {
            if (angularVelocity.magnitude > 0.02f)
            {
                UpdateRotation(dt);
            }
            else
            {
                angularVelocity = Vector3.zero;
            }

            // Exponential angular damping
            float angularDrag = 6f; // very strong
            angularVelocity *= Mathf.Exp(-angularDrag * dt);

            // Hard clamp to kill infinite spin
            if (angularVelocity.magnitude < 0.02f)
                angularVelocity = Vector3.zero;
        }
    }

    void UpdateRotation(float dt)
    {
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


