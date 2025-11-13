using UnityEngine;
using System.Collections.Generic;

/// Handles geometry generation for the plate and fragments
/// Manages grid subdivision and mesh creation
/// Updated to pass dimension information for inertia calculations
public class PlateGeometry : MonoBehaviour
{
    private float width;
    private float depth;
    private float thickness;
    private int gridResolution;

    public void Initialize(float w, float d, float t, int grid)
    {
        width = w;
        depth = d;
        thickness = t;
        gridResolution = grid;
    }

    /// Creates the mesh for the intact plate
    public Mesh CreatePlateMesh()
    {
        Vector3 halfSize = new Vector3(width * 0.5f, thickness * 0.5f, depth * 0.5f);

        Vector3[] vertices = {
            new Vector3(-halfSize.x, -halfSize.y, -halfSize.z),
            new Vector3(halfSize.x, -halfSize.y, -halfSize.z),
            new Vector3(halfSize.x, -halfSize.y, halfSize.z),
            new Vector3(-halfSize.x, -halfSize.y, halfSize.z),
            new Vector3(-halfSize.x, halfSize.y, -halfSize.z),
            new Vector3(halfSize.x, halfSize.y, -halfSize.z),
            new Vector3(halfSize.x, halfSize.y, halfSize.z),
            new Vector3(-halfSize.x, halfSize.y, halfSize.z)
        };

        int[] triangles = {
            0,2,1, 0,3,2,  // Bottom
            4,5,6, 4,6,7,  // Top
            3,6,2, 3,7,6,  // Front
            0,1,5, 0,5,4,  // Back
            0,4,7, 0,7,3,  // Left
            1,2,6, 1,6,5   // Right
        };

        Mesh mesh = new Mesh { name = "IntactPlateMesh" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    /// Creates grid of fragment pieces
    public List<PieceBehaviour> CreateFragments(Transform parent, Material material, Vector3 startPos)
    {
        List<PieceBehaviour> fragments = new List<PieceBehaviour>();

        float cellWidth = width / gridResolution;
        float cellDepth = depth / gridResolution;

        Vector3 gridOrigin = new Vector3(
            -width * 0.5f + cellWidth * 0.5f,
            0f,
            -depth * 0.5f + cellDepth * 0.5f
        );

        float totalVolume = width * depth * thickness;
        float cellVolume = cellWidth * cellDepth * thickness;

        for (int z = 0; z < gridResolution; z++)
        {
            for (int x = 0; x < gridResolution; x++)
            {
                Vector3 localCenter = gridOrigin + new Vector3(
                    x * cellWidth,
                    0f,
                    z * cellDepth
                );

                GameObject fragmentGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fragmentGO.name = $"Fragment_{x}_{z}";
                fragmentGO.transform.SetParent(parent, false);
                fragmentGO.transform.localPosition = localCenter;

                Vector3 scale = new Vector3(
                    cellWidth * 0.98f,
                    thickness,
                    cellDepth * 0.98f
                );
                fragmentGO.transform.localScale = scale;

                // Remove Unity collider (we use custom collision detection)
                var col = fragmentGO.GetComponent<Collider>();
                if (col) DestroyImmediate(col);

                // Apply material
                var mr = fragmentGO.GetComponent<MeshRenderer>();
                mr.sharedMaterial = material;

                // Add custom behaviour with dimension information
                var piece = fragmentGO.AddComponent<PieceBehaviour>();
                piece.SetupGeometry(
                    localCenter,
                    cellVolume / Mathf.Max(totalVolume, 1e-6f),
                    CalculateBoundingRadius(cellWidth, thickness, cellDepth),
                    scale // Pass dimensions for inertia calculation
                );

                fragments.Add(piece);
            }
        }

        return fragments;
    }

    float CalculateBoundingRadius(float w, float h, float d)
    {
        return 0.5f * Mathf.Sqrt(w * w + h * h + d * d);
    }
}