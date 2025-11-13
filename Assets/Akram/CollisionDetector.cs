using UnityEngine;
using System.Collections.Generic;

/// Unified collision detection system
/// Handles both obstacle and fragment-to-fragment collisions
public class CollisionDetector : MonoBehaviour
{
    private struct ObstacleRef
    {
        public Transform transform;
        public ObstacleBounds bounds;
    }

    private readonly List<ObstacleRef> obstacles = new List<ObstacleRef>();
    private List<PieceBehaviour> fragments = new List<PieceBehaviour>();

    public void RefreshObstacles()
    {
        obstacles.Clear();
        var allObstacles = FindObjectsOfType<ObstacleBounds>();

        foreach (var ob in allObstacles)
        {
            if (ob.transform.IsChildOf(transform) || ob.transform == transform)
                continue;

            obstacles.Add(new ObstacleRef { transform = ob.transform, bounds = ob });
        }
    }

    public void RegisterFragments(List<PieceBehaviour> pieceList)
    {
        fragments = pieceList;
    }

    public int GetObstacleCount() => obstacles.Count;

    public void GetObstacleAABB(Transform obstacleTransform, out Vector3 min, out Vector3 max)
    {
        var bounds = obstacleTransform.GetComponent<ObstacleBounds>();
        if (bounds != null)
        {
            bounds.GetWorldAABB(out min, out max);
        }
        else
        {
            min = max = obstacleTransform.position;
        }
    }

    public bool CheckObstacleCollision(
        Vector3 spherePos,
        float sphereRadius,
        out Vector3 normal,
        out Vector3 hitPoint,
        out ObstacleBounds hitObstacle)
    {
        normal = Vector3.zero;
        hitPoint = Vector3.zero;
        hitObstacle = null;

        foreach (var obs in obstacles)
        {
            Vector3 min, max;
            obs.bounds.GetWorldAABB(out min, out max);

            Vector3 n, p;
            if (SphereAABBTest(spherePos, sphereRadius, min, max, out n, out p))
            {
                normal = n;
                hitPoint = p;
                hitObstacle = obs.bounds;
                return true;
            }
        }

        return false;
    }

    public bool CheckFragmentCollision(
        PieceBehaviour self,
        Vector3 spherePos,
        float sphereRadius,
        out Vector3 normal,
        out Vector3 hitPoint,
        out Vector3 relativeVelocity)
    {
        normal = Vector3.zero;
        hitPoint = Vector3.zero;
        relativeVelocity = Vector3.zero;

        foreach (var other in fragments)
        {
            if (other == self || other == null || !other.gameObject.activeInHierarchy)
                continue;

            Vector3 otherPos = other.transform.position;
            float otherRadius = other.BoundingRadius;
            float radiusSum = sphereRadius + otherRadius;

            Vector3 diff = spherePos - otherPos;
            float distSq = diff.sqrMagnitude;

            if (distSq < radiusSum * radiusSum && distSq > 1e-6f)
            {
                float dist = Mathf.Sqrt(distSq);
                normal = diff / dist;
                hitPoint = otherPos + normal * otherRadius;
                relativeVelocity = self.Velocity - other.Velocity;
                return true;
            }
        }

        return false;
    }

    private bool SphereAABBTest(
        Vector3 spherePos,
        float sphereRadius,
        Vector3 aabbMin,
        Vector3 aabbMax,
        out Vector3 normal,
        out Vector3 hitPoint)
    {
        Vector3 closestPoint = new Vector3(
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
                // Sphere center inside AABB
                Vector3 toMin = spherePos - aabbMin;
                Vector3 toMax = aabbMax - spherePos;
                float minDist = Mathf.Min(toMin.x, toMin.y, toMin.z, toMax.x, toMax.y, toMax.z);

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

    public void DrawDebugGizmos()
    {
        Gizmos.color = new Color(0.2f, 1.0f, 0.3f, 0.6f);
        foreach (var obs in obstacles)
        {
            Vector3 min, max;
            obs.bounds.GetWorldAABB(out min, out max);
            Vector3 center = (min + max) * 0.5f;
            Vector3 size = max - min;
            Gizmos.DrawWireCube(center, size);
        }
    }
}