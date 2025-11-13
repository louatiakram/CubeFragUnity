using UnityEngine;
using System.Collections.Generic;

public class CubeCrashController : MonoBehaviour
{
    public float alpha = 1.0f;
    private List<RigidBody3DState> cubes = new List<RigidBody3DState>();
    private SweepAndPrune2 collisionDetector;

    [Header("Moving Cube (Red) Properties")]
    public Vector3 movingCubePosition = new Vector3(-8, 2, 0);
    public Vector3 movingCubeVelocity = new Vector3(20, 0, 0);
    public float movingCubeMass = 1500f;
    public Vector3 movingCubeSize = new Vector3(2f, 1f, 4f);

    [Header("Stationary Cube 1 (Blue) Properties")]
    public Vector3 stationaryCube1Position = new Vector3(0, 1, -1.5f);
    public Vector3 stationaryCube1Velocity = Vector3.zero;
    public float stationaryCube1Mass = 1200f;
    public Vector3 stationaryCube1Size = new Vector3(2f, 1f, 4f);

    [Header("Stationary Cube 2 (Green) Properties")]
    public Vector3 stationaryCube2Position = new Vector3(0, 1, 1.5f);
    public Vector3 stationaryCube2Velocity = Vector3.zero;
    public float stationaryCube2Mass = 1200f;
    public Vector3 stationaryCube2Size = new Vector3(2f, 1f, 4f);

    [Header("Constraint Properties")]
    public float constraintBreakThreshold = 800f;

    [Header("Global Properties")]
    public float floorHeight = -1f;

    [Header("Visualization Settings")]
    public bool showColliders = true;
    public bool showCollisionLines = true;

    void Start()
    {
        CreateFloor();
        CreateCollisionDetector();
        CreateCubeCrashScene();
    }

    void CreateCollisionDetector()
    {
        GameObject collisionObj = new GameObject("CollisionDetector");
        collisionDetector = collisionObj.AddComponent<SweepAndPrune2>();
    }

    void CreateFloor()
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.transform.localScale = new Vector3(5, 1, 5);
        floor.transform.position = new Vector3(0, floorHeight, 0);
        floor.GetComponent<Renderer>().material.color = Color.gray;
    }

    void CreateCubeCrashScene()
    {
        cubes.Clear();

        // Create moving cube (red)
        GameObject movingCubeObj = new GameObject("MovingCube");
        RigidBody3DState movingCube = movingCubeObj.AddComponent<RigidBody3DState>();
        movingCube.cube = new CubeObject(movingCubeSize.x, movingCubeSize.y, movingCubeSize.z, Color.red);
        movingCube.position = movingCubePosition;
        movingCube.P = movingCubeVelocity * movingCubeMass;
        movingCube.mass = movingCubeMass;
        movingCube.a = movingCubeSize.x;
        movingCube.b = movingCubeSize.y;
        movingCube.c = movingCubeSize.z;
        movingCube.alpha = alpha;
        cubes.Add(movingCube);

        // Create first stationary cube (blue)
        GameObject stationaryCube1Obj = new GameObject("StationaryCube1");
        RigidBody3DState stationaryCube1 = stationaryCube1Obj.AddComponent<RigidBody3DState>();
        stationaryCube1.cube = new CubeObject(stationaryCube1Size.x, stationaryCube1Size.y, stationaryCube1Size.z, Color.blue);
        stationaryCube1.position = stationaryCube1Position;
        stationaryCube1.P = stationaryCube1Velocity * stationaryCube1Mass;
        stationaryCube1.mass = stationaryCube1Mass;
        stationaryCube1.a = stationaryCube1Size.x;
        stationaryCube1.b = stationaryCube1Size.y;
        stationaryCube1.c = stationaryCube1Size.z;
        stationaryCube1.isStatic = false;
        stationaryCube1.alpha = alpha;
        cubes.Add(stationaryCube1);

        // Create second stationary cube (green)
        GameObject stationaryCube2Obj = new GameObject("StationaryCube2");
        RigidBody3DState stationaryCube2 = stationaryCube2Obj.AddComponent<RigidBody3DState>();
        stationaryCube2.cube = new CubeObject(stationaryCube2Size.x, stationaryCube2Size.y, stationaryCube2Size.z, Color.green);
        stationaryCube2.position = stationaryCube2Position;
        stationaryCube2.P = stationaryCube2Velocity * stationaryCube2Mass;
        stationaryCube2.mass = stationaryCube2Mass;
        stationaryCube2.a = stationaryCube2Size.x;
        stationaryCube2.b = stationaryCube2Size.y;
        stationaryCube2.c = stationaryCube2Size.z;
        stationaryCube2.isStatic = false;
        stationaryCube2.alpha = alpha;
        cubes.Add(stationaryCube2);

        // Create constraint between stationary cubes
        FractureConstraint constraint = new FractureConstraint();
        constraint.bodyA = stationaryCube1;
        constraint.bodyB = stationaryCube2;
        constraint.anchor = (stationaryCube1Position + stationaryCube2Position) * 0.5f;
        constraint.breakThreshold = constraintBreakThreshold;

        stationaryCube1.constraints.Add(constraint);
        stationaryCube2.constraints.Add(constraint);

        // Set cubes for collision detection
        if (collisionDetector != null)
        {
            collisionDetector.SetCubes(cubes);
            collisionDetector.showCollisionLines = showCollisionLines;
        }

        ToggleColliderVisualization(showColliders);
    }

    void Update()
    {
        // Change alpha values with number keys
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetAlphaForAll(0f);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetAlphaForAll(0.5f);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetAlphaForAll(1.0f);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetAlphaForAll(1.5f);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SetAlphaForAll(2.0f);

        // Reset scene with new properties
        if (Input.GetKeyDown(KeyCode.R)) ResetScene();

        // Toggle collider visualization
        if (Input.GetKeyDown(KeyCode.V))
        {
            showColliders = !showColliders;
            ToggleColliderVisualization(showColliders);
        }

        // Toggle collision lines
        if (Input.GetKeyDown(KeyCode.L))
        {
            showCollisionLines = !showCollisionLines;
            ToggleCollisionLines(showCollisionLines);
        }
    }

    void SetAlphaForAll(float newAlpha)
    {
        foreach (var cube in cubes)
        {
            cube.alpha = newAlpha;
        }
        alpha = newAlpha;
        Debug.Log($"Alpha set to: {newAlpha}");
    }

    void ResetScene()
    {
        foreach (var cube in cubes)
        {
            Destroy(cube.gameObject);
        }
        cubes.Clear();
        CreateCubeCrashScene();
    }

    void ToggleColliderVisualization(bool show)
    {
        foreach (var cube in cubes)
        {
            cube.showCollider = show;
        }
    }

    void ToggleCollisionLines(bool show)
    {
        if (collisionDetector != null)
        {
            collisionDetector.showCollisionLines = show;
        }
    }

    public void TriggerCrash()
    {
        ResetScene();
    }

    public void UpdateCubeProperties(int cubeIndex, Vector3 newPosition, Vector3 newVelocity, float newMass, Vector3 newSize)
    {
        if (cubeIndex >= 0 && cubeIndex < cubes.Count)
        {
            RigidBody3DState cube = cubes[cubeIndex];
            cube.position = newPosition;
            cube.P = newVelocity * newMass;
            cube.mass = newMass;
            cube.a = newSize.x;
            cube.b = newSize.y;
            cube.c = newSize.z;

            // Recalculate inertia tensors
            float Ixx = (1f / 12f) * cube.mass * (cube.b * cube.b + cube.c * cube.c);
            float Iyy = (1f / 12f) * cube.mass * (cube.a * cube.a + cube.c * cube.c);
            float Izz = (1f / 12f) * cube.mass * (cube.a * cube.a + cube.b * cube.b);

            cube.Ibody[0, 0] = Ixx;
            cube.Ibody[1, 1] = Iyy;
            cube.Ibody[2, 2] = Izz;

            cube.IbodyInv[0, 0] = 1 / Ixx;
            cube.IbodyInv[1, 1] = 1 / Iyy;
            cube.IbodyInv[2, 2] = 1 / Izz;
        }
    }
}