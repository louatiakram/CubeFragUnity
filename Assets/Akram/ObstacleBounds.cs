
using UnityEngine;

/// Attach this to any GameObject that should act as an obstacle for the plate
/// Defines precise AABB bounds for collision detection
/// No Collider component needed - just MeshFilter/MeshRenderer + this script
public class ObstacleBounds : MonoBehaviour
{
    [Header("=== AABB Bounds ===")]
    [Tooltip("Size of the collision box (leave at zero to use mesh bounds)")]
    public Vector3 boundsSize = Vector3.zero;

    [Tooltip("Center offset of the collision box")]
    public Vector3 boundsCenter = Vector3.zero;

    [Header("=== Ground Settings ===")]
    [Tooltip("If checked, this object is treated as ground - plate will stop falling when hitting this")]
    public bool isGround = false;

    [Header("=== Debug ===")]
    public bool showGizmo = true;
    public Color gizmoColor = new Color(1f, 0.5f, 0f, 0.8f);

    /// Get the AABB min/max in world space
    public void GetWorldAABB(out Vector3 min, out Vector3 max)
    {
        Vector3 center = transform.TransformPoint(boundsCenter);
        Vector3 size = boundsSize;

        // If size is zero, auto-calculate from mesh bounds
        if (size.sqrMagnitude < 0.001f)
        {
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                Bounds localBounds = meshFilter.sharedMesh.bounds;

                // Transform all 8 corners to world space
                Vector3 localCenter = localBounds.center;
                Vector3 extents = localBounds.extents;

                Vector3[] corners = new Vector3[8];
                corners[0] = transform.TransformPoint(localCenter + new Vector3(-extents.x, -extents.y, -extents.z));
                corners[1] = transform.TransformPoint(localCenter + new Vector3(extents.x, -extents.y, -extents.z));
                corners[2] = transform.TransformPoint(localCenter + new Vector3(-extents.x, extents.y, -extents.z));
                corners[3] = transform.TransformPoint(localCenter + new Vector3(extents.x, extents.y, -extents.z));
                corners[4] = transform.TransformPoint(localCenter + new Vector3(-extents.x, -extents.y, extents.z));
                corners[5] = transform.TransformPoint(localCenter + new Vector3(extents.x, -extents.y, extents.z));
                corners[6] = transform.TransformPoint(localCenter + new Vector3(-extents.x, extents.y, extents.z));
                corners[7] = transform.TransformPoint(localCenter + new Vector3(extents.x, extents.y, extents.z));

                // Calculate tight AABB
                min = corners[0];
                max = corners[0];

                for (int i = 1; i < 8; i++)
                {
                    min = Vector3.Min(min, corners[i]);
                    max = Vector3.Max(max, corners[i]);
                }
                return;
            }
        }

        // Use manual bounds
        Vector3 worldSize = Vector3.Scale(size, transform.lossyScale);
        Vector3 halfSize = worldSize * 0.5f;

        min = center - halfSize;
        max = center + halfSize;
    }

    void OnDrawGizmos()
    {
        if (!showGizmo) return;

        Vector3 min, max;
        GetWorldAABB(out min, out max);

        Gizmos.color = gizmoColor;
        Vector3 center = (min + max) * 0.5f;
        Vector3 size = max - min;
        Gizmos.DrawWireCube(center, size);
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;

        Vector3 min, max;
        GetWorldAABB(out min, out max);

        // Draw filled box when selected
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);
        Vector3 center = (min + max) * 0.5f;
        Vector3 size = max - min;
        Gizmos.DrawCube(center, size);

        // Draw wire on top
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(center, size);
    }
}