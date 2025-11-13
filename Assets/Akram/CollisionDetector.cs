using UnityEngine;
using System.Collections.Generic;

/// Collision detection system using AABB algorithm
/// Detects collisions with GameObjects that have ObstacleBounds component
/// Provides precise, manually-defined collision bounds
public class CollisionDetector : MonoBehaviour
{
    private struct AABBData
    {
        public Transform transform;
        public Vector3 min; // AABB minimum corner (world space)
        public Vector3 max; // AABB maximum corner (world space)
        public Vector3 center;
    }

    private List<AABBData> aabbs = new List<AABBData>();

    /// Refresh AABB cache from scene (finds all ObstacleBounds components)
    public void RefreshColliders()
    {
        aabbs.Clear();

        // Find ALL GameObjects with ObstacleBounds component
        var allObstacles = FindObjectsOfType<ObstacleBounds>();
        foreach (var obstacle in allObstacles)
        {
            // Skip if this is part of our plate system
            if (obstacle.transform.IsChildOf(transform) ||
                obstacle.transform == transform)
                continue;

            AddAABB(obstacle);
        }
    }

    void AddAABB(ObstacleBounds obstacle)
    {
        Vector3 min, max;
        obstacle.GetWorldAABB(out min, out max);

        aabbs.Add(new AABBData
        {
            transform = obstacle.transform,
            center = (min + max) * 0.5f,
            min = min,
            max = max
        });
    }

    /// Get count of detected obstacles (for debugging)
    public int GetObstacleCount()
    {
        return aabbs.Count;
    }

    /// Check sphere collision against all AABBs using AABB algorithm
    /// Returns true if collision detected, outputs normal, hit point (sphere contact) and the closest AABB point.
    public bool CheckSphereCollision(Vector3 spherePos, float sphereRadius,
        out Vector3 normal, out Vector3 hitPoint, out Vector3 closestPoint)
    {
        normal = Vector3.zero;
        hitPoint = Vector3.zero;
        closestPoint = Vector3.zero;

        bool hasCollision = false;

        foreach (var aabb in aabbs)
        {
            Vector3 n, p, cp;
            if (CheckSphereAABB(spherePos, sphereRadius, aabb, out n, out p, out cp))
            {
                normal = n;
                hitPoint = p;
                closestPoint = cp;
                hasCollision = true;
            }
        }
        return hasCollision;
    }

    /// Sphere vs AABB (Axis-Aligned Bounding Box) collision algorithm
    /// Classic AABB algorithm: find closest point on box to sphere center
    bool CheckSphereAABB(Vector3 spherePos, float sphereRadius, AABBData aabb,
        out Vector3 normal, out Vector3 hitPoint, out Vector3 closestPoint)
    {
        // Find closest point on AABB to sphere center
        closestPoint = new Vector3(
            Mathf.Clamp(spherePos.x, aabb.min.x, aabb.max.x),
            Mathf.Clamp(spherePos.y, aabb.min.y, aabb.max.y),
            Mathf.Clamp(spherePos.z, aabb.min.z, aabb.max.z)
        );

        // Calculate distance from sphere center to closest point
        Vector3 diff = spherePos - closestPoint;
        float distSq = diff.sqrMagnitude;

        // Check if sphere intersects AABB
        if (distSq < sphereRadius * sphereRadius)
        {
            float dist = Mathf.Sqrt(Mathf.Max(distSq, 1e-10f));
            if (dist > 1e-6f)
            {
                // Normal points from closest point to sphere center
                normal = diff / dist;
            }
            else
            {
                // Sphere center is inside AABB: calculate face normal
                // Find which face is closest
                Vector3 toMin = spherePos - aabb.min;
                Vector3 toMax = aabb.max - spherePos;
                float minDist = Mathf.Min(
                    toMin.x, toMin.y, toMin.z,
                    toMax.x, toMax.y, toMax.z
                );

                // Set normal based on closest face
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

    /// Draw debug gizmos for all detected AABBs
    public void DrawDebugGizmos()
    {
        Gizmos.color = new Color(0.2f, 1.0f, 0.3f, 0.6f);
        foreach (var aabb in aabbs)
        {
            Vector3 size = aabb.max - aabb.min;
            Gizmos.DrawWireCube(aabb.center, size);
        }
    }
}