using UnityEngine;
using System.Collections.Generic;

namespace YassineBM
{
    /// <summary>
    /// Splits a mesh along planes to create realistic fracture pieces
    /// Uses plane-based cutting to slice the original mesh
    /// </summary>
    public class MeshSlicer
    {
        private class Triangle
        {
            public Vector3 v0, v1, v2;
            public Vector2 uv0, uv1, uv2;
            
            public Triangle(Vector3 v0, Vector3 v1, Vector3 v2, Vector2 uv0, Vector2 uv1, Vector2 uv2)
            {
                this.v0 = v0; this.v1 = v1; this.v2 = v2;
                this.uv0 = uv0; this.uv1 = uv1; this.uv2 = uv2;
            }
        }

        /// <summary>
        /// Split a sphere mesh into realistic fragments using plane cuts
        /// </summary>
        public static List<Mesh> SliceSphere(Mesh originalMesh, int numberOfFragments, int seed = 0)
        {
            Random.InitState(seed);
            
            List<Mesh> fragments = new List<Mesh>();
            
            // Generate random cutting planes through the sphere
            List<Plane> cuttingPlanes = GenerateCuttingPlanes(numberOfFragments, seed);
            
            // Create regions based on plane intersections
            List<MeshRegion> regions = PartitionMeshByPlanes(originalMesh, cuttingPlanes);
            
            // Convert each region to a mesh
            foreach (var region in regions)
            {
                if (region.triangles.Count > 0)
                {
                    Mesh fragmentMesh = CreateMeshFromRegion(region);
                    fragments.Add(fragmentMesh);
                }
            }
            
            Debug.Log($"Sliced sphere into {fragments.Count} fragments using plane cuts");
            return fragments;
        }

        private class MeshRegion
        {
            public List<Triangle> triangles = new List<Triangle>();
            public Vector3 centroid = Vector3.zero;
            public int regionId;
        }

        /// <summary>
        /// Generate random planes that pass through or near the sphere center
        /// </summary>
        private static List<Plane> GenerateCuttingPlanes(int count, int seed)
        {
            Random.InitState(seed);
            List<Plane> planes = new List<Plane>();
            
            // Create planes with random orientations through the center
            for (int i = 0; i < Mathf.Sqrt(count * 2); i++)
            {
                Vector3 normal = Random.onUnitSphere;
                float offset = Random.Range(-0.5f, 0.5f); // Slight offset from center
                planes.Add(new Plane(normal, normal * offset));
            }
            
            return planes;
        }

        /// <summary>
        /// Partition mesh triangles into regions based on which side of planes they fall
        /// </summary>
        private static List<MeshRegion> PartitionMeshByPlanes(Mesh mesh, List<Plane> planes)
        {
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            Vector2[] uvs = mesh.uv;
            
            // Create initial triangles
            List<Triangle> allTriangles = new List<Triangle>();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v0 = vertices[triangles[i]];
                Vector3 v1 = vertices[triangles[i + 1]];
                Vector3 v2 = vertices[triangles[i + 2]];
                
                Vector2 uv0 = uvs[triangles[i]];
                Vector2 uv1 = uvs[triangles[i + 1]];
                Vector2 uv2 = uvs[triangles[i + 2]];
                
                allTriangles.Add(new Triangle(v0, v1, v2, uv0, uv1, uv2));
            }
            
            // Assign each triangle to a region based on plane sides
            Dictionary<int, MeshRegion> regionMap = new Dictionary<int, MeshRegion>();
            
            foreach (var tri in allTriangles)
            {
                Vector3 triCenter = (tri.v0 + tri.v1 + tri.v2) / 3f;
                int regionId = CalculateRegionId(triCenter, planes);
                
                if (!regionMap.ContainsKey(regionId))
                {
                    regionMap[regionId] = new MeshRegion { regionId = regionId };
                }
                
                regionMap[regionId].triangles.Add(tri);
            }
            
            // Calculate centroids
            foreach (var region in regionMap.Values)
            {
                Vector3 sum = Vector3.zero;
                int count = 0;
                foreach (var tri in region.triangles)
                {
                    sum += tri.v0 + tri.v1 + tri.v2;
                    count += 3;
                }
                region.centroid = sum / count;
            }
            
            return new List<MeshRegion>(regionMap.Values);
        }

        /// <summary>
        /// Calculate a unique region ID based on which side of each plane the point is on
        /// </summary>
        private static int CalculateRegionId(Vector3 point, List<Plane> planes)
        {
            int id = 0;
            for (int i = 0; i < planes.Count; i++)
            {
                if (planes[i].GetSide(point))
                {
                    id |= (1 << i); // Set bit i if on positive side
                }
            }
            return id;
        }

        /// <summary>
        /// Create a mesh from a collection of triangles
        /// </summary>
        private static Mesh CreateMeshFromRegion(MeshRegion region)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangleIndices = new List<int>();
            
            int vertexIndex = 0;
            foreach (var tri in region.triangles)
            {
                vertices.Add(tri.v0);
                vertices.Add(tri.v1);
                vertices.Add(tri.v2);
                
                uvs.Add(tri.uv0);
                uvs.Add(tri.uv1);
                uvs.Add(tri.uv2);
                
                triangleIndices.Add(vertexIndex++);
                triangleIndices.Add(vertexIndex++);
                triangleIndices.Add(vertexIndex++);
            }
            
            // Add interior faces to close the fragment
            AddInteriorFaces(vertices, uvs, triangleIndices, region);
            
            Mesh fragmentMesh = new Mesh();
            fragmentMesh.name = $"Fragment_{region.regionId}";
            fragmentMesh.vertices = vertices.ToArray();
            fragmentMesh.triangles = triangleIndices.ToArray();
            fragmentMesh.uv = uvs.ToArray();
            fragmentMesh.RecalculateNormals();
            fragmentMesh.RecalculateBounds();
            fragmentMesh.RecalculateTangents();
            
            return fragmentMesh;
        }

        /// <summary>
        /// Add interior faces to close open edges of the fragment
        /// </summary>
        private static void AddInteriorFaces(List<Vector3> vertices, List<Vector2> uvs, List<int> triangles, MeshRegion region)
        {
            // Find boundary edges and close them with faces pointing inward
            // This creates the "crack" surfaces
            
            // For simplicity, we'll add a center point and connect boundary vertices to it
            Vector3 centerPoint = region.centroid * 0.5f; // Pull slightly toward center
            int centerIndex = vertices.Count;
            vertices.Add(centerPoint);
            uvs.Add(new Vector2(0.5f, 0.5f));
            
            // Find edge vertices (simplified - connect some to center)
            int edgeVertexCount = Mathf.Min(region.triangles.Count, 20); // Limit interior faces
            for (int i = 0; i < edgeVertexCount; i++)
            {
                int triIndex = i % region.triangles.Count;
                Triangle tri = region.triangles[triIndex];
                
                // Add small interior triangle
                vertices.Add(tri.v0);
                uvs.Add(tri.uv0);
                
                vertices.Add(tri.v1);
                uvs.Add(tri.uv1);
                
                triangles.Add(vertices.Count - 2);
                triangles.Add(vertices.Count - 1);
                triangles.Add(centerIndex);
            }
        }
    }
}

