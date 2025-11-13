using UnityEngine;
using System.Collections.Generic;

/// Collision detection system using AABB algorithm
/// Detects collisions with GameObjects that have ObstacleBounds component
/// AND fragment-to-fragment collisions
public class CollisionDetector : MonoBehaviour
{
    private struct AABBRef
    {
        public Transform transform;
        public ObstacleBounds obstacle;
    }

    private readonly List<AABBRef> obstacles = new List<AABBRef>();
    private List<PieceBehaviour> fragments = new List<PieceBehaviour>();

    /// Refresh obstacle list from scene
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

    /// Register fragments for fragment-to-fragment collision
    public void RegisterFragments(List<PieceBehaviour> pieceList)
    {
        fragments = pieceList;
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

    /// Check sphere collision against all AABBs (obstacles only)
    public bool CheckSphereCollision(
        Vector3 spherePos,
        float sphereRadius,
        out Vector3 normal,
        out Vector3 hitPoint,
        out Vector3 closestPoint,
        out ObstacleBounds hitObstacle)
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
                normal = n;
                hitPoint = p;
                closestPoint = cp;
                hitObstacle = a.obstacle;
                hasCollision = true;
            }
        }

        return hasCollision;
    }

    /// Check fragment-to-fragment collision
    public bool CheckFragmentCollision(
        PieceBehaviour checkingFragment,
        Vector3 spherePos,
        float sphereRadius,
        out Vector3 normal,
        out Vector3 hitPoint,
        out Vector3 relativeVelocity)
    {
        normal = Vector3.zero;
        hitPoint = Vector3.zero;
        relativeVelocity = Vector3.zero;

        bool hasCollision = false;

        foreach (var other in fragments)
        {
            if (other == checkingFragment || other == null || !other.gameObject.activeInHierarchy)
                continue;

            Vector3 otherPos = other.transform.position;
            float otherRadius = other.GetBoundingRadius();

            // Sphere-sphere collision check
            Vector3 diff = spherePos - otherPos;
            float distSq = diff.sqrMagnitude;
            float radiusSum = sphereRadius + otherRadius;

            if (distSq < radiusSum * radiusSum && distSq > 1e-6f)
            {
                float dist = Mathf.Sqrt(distSq);
                normal = diff / dist;
                hitPoint = otherPos + normal * otherRadius;
                relativeVelocity = checkingFragment.GetVelocity() - other.GetVelocity();
                hasCollision = true;
                break; // Only handle one collision per frame
            }
        }

        return hasCollision;
    }

    /// Sphere vs AABB collision algorithm
    private bool CheckSphereAABB(
        Vector3 spherePos,
        float sphereRadius,
        Vector3 aabbMin,
        Vector3 aabbMax,
        out Vector3 normal,
        out Vector3 hitPoint,
        out Vector3 closestPoint)
    {
        closestPoint = new Vector3(
            Mathf.Clamp(spherePos.x, aabbMin.x, aabbMax.x),
            Mathf.Clamp(spherePos.y, aabbMin.y, aabbMax.y),
            Mathf.Clamp(spherePos.z, aabbMin.z, aabbMax.z)
        );

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

    /// Draw debug gizmos for all detected obstacles
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