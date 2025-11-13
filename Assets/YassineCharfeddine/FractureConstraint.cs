using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class FractureConstraint
{
    public RigidBody3DState bodyA;
    public RigidBody3DState bodyB;
    public Vector3 anchor;
    public float breakThreshold = 500f;
    public bool isBroken = false;

    // Fracture energy storage
    public float lambda; // Lagrange multiplier
    public Vector3 forceA, torqueA, forceB, torqueB;

    // CFM/ERP parameters
    public float CFM = 0.0001f;
    public float ERP = 0.8f;
}

public struct RigidBodyState2
{
    public Vector3 position;
    public Matrix4x4 R;
    public Vector3 P;
    public Vector3 L;
}

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RigidBody3DState : MonoBehaviour
{
    public CubeObject cube;
    public float a = 1, b = 1, c = 1, mass = 1f;
    public Vector3 position = Vector3.zero;
    public Vector3 P = Vector3.zero;
    public Vector3 L = Vector3.zero;
    public Matrix4x4 R = Matrix4x4.identity;
    public Matrix4x4 Ibody;
    public Matrix4x4 IbodyInv;
    public float dt = 0.02f;

    // Fracture system
    public List<FractureConstraint> constraints = new List<FractureConstraint>();
    public float alpha = 1.0f; // Energy transfer parameter
    public bool isStatic = false;

    // Collider visualization
    private LineRenderer lineRenderer;
    public bool showCollider = true;
    public Color colliderColor = Color.yellow;

    private Mesh mesh;
    private List<FractureConstraint> constraintsToRemove = new List<FractureConstraint>();

    void Start()
    {
        cube = new CubeObject(a, b, c, Color.green);

        float Ixx = (1f / 12f) * mass * (b * b + c * c);
        float Iyy = (1f / 12f) * mass * (a * a + c * c);
        float Izz = (1f / 12f) * mass * (a * a + b * b);

        Ibody = Matrix4x4.zero;
        Ibody[0, 0] = Ixx; Ibody[1, 1] = Iyy; Ibody[2, 2] = Izz; Ibody[3, 3] = 1;
        IbodyInv = Matrix4x4.zero;
        IbodyInv[0, 0] = 1 / Ixx; IbodyInv[1, 1] = 1 / Iyy; IbodyInv[2, 2] = 1 / Izz; IbodyInv[3, 3] = 1;

        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().material = new Material(Shader.Find("Standard"));
        GetComponent<MeshRenderer>().material.color = cube.color;

        // Add LineRenderer for collider visualization
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = colliderColor;
        lineRenderer.endColor = colliderColor;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.useWorldSpace = true;

        UpdateColliderVisualization();
    }

    void Update()
    {
        if (!isStatic)
        {
            Vector3 F = Vector3.zero;
            Vector3 torque = Vector3.zero;

            // Apply gravity
            F += new Vector3(0, -9.81f * mass, 0);

            if (Input.GetKey(KeyCode.A)) F += new Vector3(0, 0, 10);
            if (Input.GetKey(KeyCode.B))
            {
                Vector3 F2 = new Vector3(0, 10, 0);
                F += F2;
                Vector3 r2 = new Vector3(a / 2, 0, 0);
                torque += Vector3.Cross(r2, F2);
            }
            if (Input.GetKey(KeyCode.C))
            {
                Vector3 F3 = new Vector3(10, 0, 0);
                F += F3;
                Vector3 r3 = new Vector3(0, b / 2, 0);
                torque += Vector3.Cross(r3, F3);
            }

            P += F * dt;
            L += torque * dt;

            Vector3 v = P / mass;
            Vector3 omega = Math3D2.MultiplyMatrixVector3(R * IbodyInv * R.transpose, L);

            position += v * dt;

            // SIMPLE FLOOR LIMIT
            float floorHeight = -1f;
            float halfHeight = b / 2f;
            if (position.y - halfHeight < floorHeight)
            {
                position.y = floorHeight + halfHeight;
                if (P.y < 0) P.y = 0;
            }

            // Matrice Omega
            Matrix4x4 Omega = Matrix4x4.zero;
            Omega[0, 1] = -omega.z; Omega[0, 2] = omega.y;
            Omega[1, 0] = omega.z; Omega[1, 2] = -omega.x;
            Omega[2, 0] = -omega.y; Omega[2, 1] = omega.x;

            // Update rotation
            Matrix4x4 Rupdate = Math3D2.Add(Matrix4x4.identity, Math3D.MultiplyScalar(Omega, dt));
            R = Math3D2.MultiplyMatrix4x4(Rupdate, R);
            R = Math3D2.GramSchmidt(R);
        }

        // Solve constraints and fracture
        SolveConstraints();

        // Update mesh
        Vector3[] transformedVertices = new Vector3[8];
        for (int i = 0; i < 8; i++)
            transformedVertices[i] = Math3D2.MultiplyMatrixVector3(R, cube.vertices[i]) + position;

        mesh.Clear();
        mesh.vertices = transformedVertices;
        mesh.triangles = cube.triangles;
        mesh.RecalculateNormals();

        // Update collider visualization
        if (showCollider)
        {
            UpdateColliderVisualization();
        }
        else
        {
            lineRenderer.positionCount = 0;
        }
    }

    private void UpdateColliderVisualization()
    {
        if (!showCollider || lineRenderer == null) return;

        // Calculate the 8 corners of the cube in world space
        Vector3[] corners = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            corners[i] = Math3D2.MultiplyMatrixVector3(R, cube.vertices[i]) + position;
        }

        // Create wireframe lines (12 edges of the cube)
        Vector3[] wireframe = new Vector3[24];

        // Bottom face
        wireframe[0] = corners[0]; wireframe[1] = corners[1];
        wireframe[2] = corners[1]; wireframe[3] = corners[2];
        wireframe[4] = corners[2]; wireframe[5] = corners[3];
        wireframe[6] = corners[3]; wireframe[7] = corners[0];

        // Top face
        wireframe[8] = corners[4]; wireframe[9] = corners[5];
        wireframe[10] = corners[5]; wireframe[11] = corners[6];
        wireframe[12] = corners[6]; wireframe[13] = corners[7];
        wireframe[14] = corners[7]; wireframe[15] = corners[4];

        // Vertical edges
        wireframe[16] = corners[0]; wireframe[17] = corners[4];
        wireframe[18] = corners[1]; wireframe[19] = corners[5];
        wireframe[20] = corners[2]; wireframe[21] = corners[6];
        wireframe[22] = corners[3]; wireframe[23] = corners[7];

        lineRenderer.positionCount = wireframe.Length;
        lineRenderer.SetPositions(wireframe);
        lineRenderer.startColor = colliderColor;
        lineRenderer.endColor = colliderColor;
    }

    private void SolveConstraints()
    {
        constraintsToRemove.Clear();
        float h = dt;

        foreach (var constraint in constraints)
        {
            if (constraint.isBroken) continue;

            // Simplified constraint solving
            Vector3 delta = constraint.bodyB.position - constraint.bodyA.position;
            Vector3 relativeVel = constraint.bodyB.P / constraint.bodyB.mass - constraint.bodyA.P / constraint.bodyA.mass;

            // Calculate constraint force (simplified spring-damper)
            float stiffness = constraint.ERP / (h * constraint.CFM);
            float damping = (1f - constraint.ERP) / constraint.ERP;

            constraint.lambda = stiffness * delta.magnitude + damping * Vector3.Dot(relativeVel, delta.normalized);

            // Store forces for fracture direction
            constraint.forceA = delta.normalized * constraint.lambda;
            constraint.forceB = -constraint.forceA;

            Vector3 rA = constraint.anchor - constraint.bodyA.position;
            Vector3 rB = constraint.anchor - constraint.bodyB.position;
            constraint.torqueA = Vector3.Cross(rA, constraint.forceA);
            constraint.torqueB = Vector3.Cross(rB, constraint.forceB);

            // Check for fracture
            if (Mathf.Abs(constraint.lambda) > constraint.breakThreshold)
            {
                FractureConstraint(constraint, h);
                constraintsToRemove.Add(constraint);
            }
        }

        // Remove broken constraints
        foreach (var constraint in constraintsToRemove)
        {
            constraints.Remove(constraint);
        }
    }

    private void FractureConstraint(FractureConstraint constraint, float h)
    {
        // Calculate stored potential energy
        float K = constraint.ERP / (h * constraint.CFM);
        float compliance = 1.0f / K;
        float potentialEnergy = 0.5f * (1.0f / (h * h)) * constraint.lambda * compliance * constraint.lambda;

        // Apply fracture impulses
        ApplyFractureImpulse(constraint.bodyA, constraint, potentialEnergy, h);
        ApplyFractureImpulse(constraint.bodyB, constraint, potentialEnergy, h);

        constraint.isBroken = true;
        Debug.Log($"Constraint fractured! Energy: {potentialEnergy}");
    }

    private void ApplyFractureImpulse(RigidBody3DState body, FractureConstraint constraint, float energy, float h)
    {
        if (body.isStatic) return;

        Vector3 impulseDir = ComputeImpulseDirection(body, constraint);
        float mG = ComputeEffectiveMass(body, impulseDir, constraint.anchor);
        float mu = Mathf.Sqrt(2.0f * alpha * mG * energy);

        // Apply impulse
        Vector3 linearImpulse = impulseDir * mu;
        Vector3 r = constraint.anchor - body.position;
        Vector3 angularImpulse = Vector3.Cross(r, linearImpulse);

        body.P += linearImpulse;
        body.L += angularImpulse;
    }

    private Vector3 ComputeImpulseDirection(RigidBody3DState body, FractureConstraint constraint)
    {
        Vector3 force = (body == constraint.bodyA) ? constraint.forceA : constraint.forceB;
        Vector3 torque = (body == constraint.bodyA) ? constraint.torqueA : constraint.torqueB;

        if (torque.sqrMagnitude > 1e-6f)
        {
            Vector3 direction = -Vector3.Cross(torque, force).normalized;
            if (direction.sqrMagnitude > 1e-6f)
                return direction;
        }

        Vector3 randomOrtho = Vector3.Cross(force, Random.onUnitSphere).normalized;
        return (randomOrtho.sqrMagnitude > 1e-6f) ? randomOrtho : Vector3.up;
    }

    private float ComputeEffectiveMass(RigidBody3DState body, Vector3 direction, Vector3 point)
    {
        Vector3 r = point - body.position;
        Vector3 torqueAxis = Vector3.Cross(r, direction);

        Vector3 worldInertia = Math3D2.MultiplyMatrixVector3(body.R * body.IbodyInv * body.R.transpose, torqueAxis);
        float angularEffect = Vector3.Dot(torqueAxis, worldInertia);
        float linearEffect = 1.0f / body.mass;

        return 1.0f / (linearEffect + angularEffect);
    }

    public RigidBodyState GetState()
    {
        RigidBodyState state;
        state.position = position;
        state.R = R;
        state.P = P;
        state.L = L;
        return state;
    }
}