using UnityEngine;
using System.Collections.Generic;

/// Collision detection system using AABB algorithm
/// Detects collisions with GameObjects that have ObstacleBounds component
/// Computes AABBs dynamically so moving obstacles are always up to date.
public class CollisionDetector : MonoBehaviour
{
    private struct AABBRef
    {
        public Transform transform;
        public ObstacleBounds obstacle;
    }

    private readonly List<AABBRef> obstacles = new List<AABBRef>();

    /// Refresh obstacle list from scene (no min/max caching)
    public void RefreshColliders()
    {
        obstacles.Clear();

        var all = FindObjectsOfType<ObstacleBounds>();
        foreach (var ob in all)
        {
            // Skip anything under the plate root
            if (ob.transform.IsChildOf(transform) || ob.transform == transform)
                continue;

            obstacles.Add(new AABBRef { transform = ob.transform, obstacle = ob });
        }
    }

    /// Get count of detected obstacles (for debugging)
    public int GetObstacleCount() => obstacles.Count;

    /// Public helper to get current AABB of a specific obstacle.
    public void GetWorldAABB(Transform obstacleTransform, out Vector3 min, out Vector3 max)
    {
        var ob = obstacleTransform.GetComponent<ObstacleBounds>();
        if (ob != null)
        {
            ob.GetWorldAABB(out min, out max);
        }
        else
        {
            // Fallback: empty box at transform position
            min = max = obstacleTransform.position;
        }
    }

    /// Check sphere collision against all AABBs (computed on demand).
    /// Returns true if collision detected and provides:
    /// - normal: contact normal (sphere vs AABB)
    /// - hitPoint: sphere contact point
    /// - closestPoint: closest point on the AABB
    /// - hitObstacle: Transform of the obstacle we hit (for attachment/follow)
    public bool CheckSphereCollision(
        Vector3 spherePos,
        float sphereRadius,
        out Vector3 normal,
        out Vector3 hitPoint,
        out Vector3 closestPoint,
        out Transform hitObstacle)
    {
        normal = Vector3.zero;
        hitPoint = Vector3.zero;
        closestPoint = Vector3.zero;
        hitObstacle = null;

        bool hasCollision = false;

        foreach (var a in obstacles)
        {
            Vector3 min, max;
            a.obstacle.GetWorldAABB(out min, out max);

            Vector3 n, p, cp;
            if (CheckSphereAABB(spherePos, sphereRadius, min, max, out n, out p, out cp))
            {
                // (Simple strategy) keep the last hit as the current one.
                normal = n;
                hitPoint = p;
                closestPoint = cp;
                hitObstacle = a.transform;
                hasCollision = true;
            }
        }

        return hasCollision;
    }

    /// Sphere vs AABB (Axis-Aligned Bounding Box) collision algorithm.
    /// Computes with provided min/max (already in world space).
    private bool CheckSphereAABB(
        Vector3 spherePos,
        float sphereRadius,
        Vector3 aabbMin,
        Vector3 aabbMax,
        out Vector3 normal,
        out Vector3 hitPoint,
        out Vector3 closestPoint)
    {
        // Find closest point on AABB to sphere center
        closestPoint = new Vector3(
            Mathf.Clamp(spherePos.x, aabbMin.x, aabbMax.x),
            Mathf.Clamp(spherePos.y, aabbMin.y, aabbMax.y),
            Mathf.Clamp(spherePos.z, aabbMin.z, aabbMax.z)
        );

        // Calculate distance from sphere center to closest point
        Vector3 diff = spherePos - closestPoint;
        float distSq = diff.sqrMagnitude;

        if (distSq < sphereRadius * sphereRadius)
        {
            float dist = Mathf.Sqrt(Mathf.Max(distSq, 1e-10f));
            if (dist > 1e-6f)
            {
                normal = diff / dist;
            }
            else
            {
                // Sphere center inside AABB: choose nearest face normal
                Vector3 toMin = spherePos - aabbMin;
                Vector3 toMax = aabbMax - spherePos;
                float minDist = Mathf.Min(
                    toMin.x, toMin.y, toMin.z,
                    toMax.x, toMax.y, toMax.z
                );

                if (Mathf.Approximately(minDist, toMin.x)) normal = Vector3.left;
                else if (Mathf.Approximately(minDist, toMax.x)) normal = Vector3.right;
                else if (Mathf.Approximately(minDist, toMin.y)) normal = Vector3.down;
                else if (Mathf.Approximately(minDist, toMax.y)) normal = Vector3.up;
                else if (Mathf.Approximately(minDist, toMin.z)) normal = Vector3.back;
                else normal = Vector3.forward;
            }

            hitPoint = spherePos - normal * sphereRadius;
            return true;
        }

        normal = Vector3.zero;
        hitPoint = Vector3.zero;
        return false;
    }

    /// Draw debug gizmos for all detected obstacles (dynamic AABBs)
    public void DrawDebugGizmos()
    {
        Gizmos.color = new Color(0.2f, 1.0f, 0.3f, 0.6f);
        foreach (var a in obstacles)
        {
            Vector3 min, max;
            a.obstacle.GetWorldAABB(out min, out max);
            var center = (min + max) * 0.5f;
            var size = max - min;
            Gizmos.DrawWireCube(center, size);
        }
    }
}