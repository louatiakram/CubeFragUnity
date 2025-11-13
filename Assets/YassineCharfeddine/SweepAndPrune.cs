using System.Collections.Generic;
using UnityEngine;

public class SweepAndPrune2 : MonoBehaviour
{
    private List<RigidBody3DState> cubes = new List<RigidBody3DState>();

    // Collision visualization
    private LineRenderer collisionLineRenderer;
    public bool showCollisionLines = true;
    public Color collisionColor = Color.red;

    void Start()
    {
        // Create LineRenderer for collision visualization
        collisionLineRenderer = gameObject.AddComponent<LineRenderer>();
        collisionLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        collisionLineRenderer.startColor = collisionColor;
        collisionLineRenderer.endColor = collisionColor;
        collisionLineRenderer.startWidth = 0.08f;
        collisionLineRenderer.endWidth = 0.08f;
        collisionLineRenderer.useWorldSpace = true;
    }

    public void SetCubes(List<RigidBody3DState> cubeList)
    {
        cubes = cubeList;

        // Enable collider visualization for all cubes
        foreach (var cube in cubes)
        {
            cube.showCollider = true;
            cube.colliderColor = Color.yellow;
        }
    }

    void Update()
    {
        DetectCollisions();
    }

    public void DetectCollisions()
    {
        if (cubes.Count < 2) return;

        // Check collisions on X, Y, Z axes
        List<(RigidBody3DState, RigidBody3DState)> collisions = new List<(RigidBody3DState, RigidBody3DState)>();

        // X-axis
        List<IntervalEvent> xEvents = CreateAxisEvents(cubes, 0); // 0 = x-axis
        collisions.AddRange(FindCollisionsOnAxis(xEvents));

        // Y-axis  
        List<IntervalEvent> yEvents = CreateAxisEvents(cubes, 1); // 1 = y-axis
        collisions.AddRange(FindCollisionsOnAxis(yEvents));

        // Z-axis
        List<IntervalEvent> zEvents = CreateAxisEvents(cubes, 2); // 2 = z-axis
        collisions.AddRange(FindCollisionsOnAxis(zEvents));

        // Remove duplicates and handle collisions
        HandleCollisions(collisions);

        // Update collision visualization
        UpdateCollisionVisualization(collisions);
    }

    private void UpdateCollisionVisualization(List<(RigidBody3DState, RigidBody3DState)> collisions)
    {
        if (!showCollisionLines || collisionLineRenderer == null) return;

        List<Vector3> collisionLines = new List<Vector3>();

        // Count how many axes each pair overlaps on
        Dictionary<(RigidBody3DState, RigidBody3DState), int> overlapCount = new Dictionary<(RigidBody3DState, RigidBody3DState), int>();

        foreach (var pair in collisions)
        {
            if (overlapCount.ContainsKey(pair))
                overlapCount[pair]++;
            else
                overlapCount[pair] = 1;
        }

        foreach (var kvp in overlapCount)
        {
            if (kvp.Value == 3) // Colliding on X, Y, and Z axes
            {
                // Draw a line between colliding cubes
                collisionLines.Add(kvp.Key.Item1.position);
                collisionLines.Add(kvp.Key.Item2.position);

                // Change cube collider color to red when colliding
                kvp.Key.Item1.colliderColor = Color.red;
                kvp.Key.Item2.colliderColor = Color.red;
            }
            else
            {
                // Reset to yellow if not colliding on all axes
                if (kvp.Key.Item1.colliderColor == Color.red)
                    kvp.Key.Item1.colliderColor = Color.yellow;
                if (kvp.Key.Item2.colliderColor == Color.red)
                    kvp.Key.Item2.colliderColor = Color.yellow;
            }
        }

        collisionLineRenderer.positionCount = collisionLines.Count;
        if (collisionLines.Count > 0)
        {
            collisionLineRenderer.SetPositions(collisionLines.ToArray());
        }
        else
        {
            collisionLineRenderer.positionCount = 0;
        }
    }

    private List<IntervalEvent> CreateAxisEvents(List<RigidBody3DState> cubes, int axis)
    {
        List<IntervalEvent> events = new List<IntervalEvent>();

        foreach (var cube in cubes)
        {
            // Get cube bounds on this axis
            float min = GetCubeMin(cube, axis);
            float max = GetCubeMax(cube, axis);

            events.Add(new IntervalEvent(cube, min, true));
            events.Add(new IntervalEvent(cube, max, false));
        }

        // Sort events
        events.Sort((a, b) =>
        {
            if (a.point == b.point)
                return a.isStart ? -1 : 1;
            return a.point.CompareTo(b.point);
        });

        return events;
    }

    private List<(RigidBody3DState, RigidBody3DState)> FindCollisionsOnAxis(List<IntervalEvent> events)
    {
        List<(RigidBody3DState, RigidBody3DState)> collisions = new List<(RigidBody3DState, RigidBody3DState)>();
        List<RigidBody3DState> activeCubes = new List<RigidBody3DState>();

        foreach (var evt in events)
        {
            if (evt.isStart)
            {
                // Check for collisions with all active cubes
                foreach (var activeCube in activeCubes)
                {
                    collisions.Add((activeCube, evt.cube));
                }
                activeCubes.Add(evt.cube);
            }
            else
            {
                activeCubes.Remove(evt.cube);
            }
        }

        return collisions;
    }

    private void HandleCollisions(List<(RigidBody3DState, RigidBody3DState)> potentialCollisions)
    {
        // Count how many axes each pair overlaps on
        Dictionary<(RigidBody3DState, RigidBody3DState), int> overlapCount = new Dictionary<(RigidBody3DState, RigidBody3DState), int>();

        foreach (var pair in potentialCollisions)
        {
            if (overlapCount.ContainsKey(pair))
                overlapCount[pair]++;
            else
                overlapCount[pair] = 1;
        }

        // Only pairs that overlap on all 3 axes are actually colliding
        foreach (var kvp in overlapCount)
        {
            if (kvp.Value == 3) // Colliding on X, Y, and Z axes
            {
                HandleCubeCollision(kvp.Key.Item1, kvp.Key.Item2);
            }
        }
    }

    private void HandleCubeCollision(RigidBody3DState cube1, RigidBody3DState cube2)
    {
        // Simple collision response - you can make this more sophisticated
        Vector3 collisionNormal = (cube2.position - cube1.position).normalized;

        // Separate the cubes
        float separation = 0.1f;
        cube1.position -= collisionNormal * separation * 0.5f;
        cube2.position += collisionNormal * separation * 0.5f;

        // Exchange momentum (simplified)
        if (!cube1.isStatic && !cube2.isStatic)
        {
            Vector3 tempP = cube1.P;
            cube1.P = cube2.P;
            cube2.P = tempP;
        }
        else if (cube1.isStatic && !cube2.isStatic)
        {
            cube2.P = -cube2.P * 0.8f; // Bounce off static object
        }
        else if (!cube1.isStatic && cube2.isStatic)
        {
            cube1.P = -cube1.P * 0.8f; // Bounce off static object
        }

        Debug.Log($"Collision detected between {cube1.name} and {cube2.name}");
    }

    private float GetCubeMin(RigidBody3DState cube, int axis)
    {
        Vector3 min = cube.position - new Vector3(cube.a, cube.b, cube.c) * 0.5f;
        return axis == 0 ? min.x : (axis == 1 ? min.y : min.z);
    }

    private float GetCubeMax(RigidBody3DState cube, int axis)
    {
        Vector3 max = cube.position + new Vector3(cube.a, cube.b, cube.c) * 0.5f;
        return axis == 0 ? max.x : (axis == 1 ? max.y : max.z);
    }

    private struct IntervalEvent
    {
        public RigidBody3DState cube;
        public float point;
        public bool isStart;

        public IntervalEvent(RigidBody3DState cube, float point, bool isStart)
        {
            this.cube = cube;
            this.point = point;
            this.isStart = isStart;
        }
    }
}