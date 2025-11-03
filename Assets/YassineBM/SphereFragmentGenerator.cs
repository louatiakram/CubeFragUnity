using UnityEngine;
using System.Collections.Generic;

namespace YassineBM
{
    /// <summary>
    /// Generates sphere fragments procedurally
    /// Creates a fractured sphere by subdividing and splitting
    /// </summary>
    public class SphereFragmentGenerator
    {
        /// <summary>
        /// Generate a whole sphere mesh (before fracturing)
        /// </summary>
        public static Mesh GenerateWholeSphere(float radius, int subdivisions = 3)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            GenerateIcosphere(radius, subdivisions, vertices, triangles);
            
            Mesh sphereMesh = new Mesh();
            sphereMesh.name = "WholeSphere";
            sphereMesh.vertices = vertices.ToArray();
            sphereMesh.triangles = triangles.ToArray();
            
            // Generate UVs
            Vector2[] uvs = new Vector2[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 v = vertices[i].normalized;
                float u = 0.5f + Mathf.Atan2(v.z, v.x) / (2f * Mathf.PI);
                float v_coord = 0.5f - Mathf.Asin(v.y) / Mathf.PI;
                uvs[i] = new Vector2(u, v_coord);
            }
            sphereMesh.uv = uvs;
            
            sphereMesh.RecalculateNormals();
            sphereMesh.RecalculateBounds();
            sphereMesh.RecalculateTangents();
            
            return sphereMesh;
        }

        /// <summary>
        /// Generate sphere shards by actually slicing the whole sphere mesh
        /// More realistic - fragments come from the original mesh
        /// </summary>
        public static List<Mesh> GenerateShardsFromSlicing(float radius, int numShards, int seed = 0)
        {
            // First generate a whole sphere
            Mesh wholeSphere = GenerateWholeSphere(radius, 3);
            
            // Then slice it into fragments
            List<Mesh> fragments = MeshSlicer.SliceSphere(wholeSphere, numShards, seed);
            
            return fragments;
        }

        /// <summary>
        /// Generate sphere shards using icosphere subdivision and voronoi-like splitting
        /// (Original method - kept for compatibility)
        /// </summary>
        public static List<Mesh> GenerateShards(float radius, int numShards, int seed = 0)
        {
            Random.InitState(seed);
            List<Mesh> shards = new List<Mesh>();

            // Generate voronoi seed points on sphere surface
            List<Vector3> seedPoints = GenerateSphericalPoints(numShards, radius);

            // Create a base sphere mesh with good resolution
            List<Vector3> sphereVertices = new List<Vector3>();
            List<int> sphereTriangles = new List<int>();
            GenerateIcosphere(radius, 2, sphereVertices, sphereTriangles);

            // Assign each triangle to nearest seed point
            Dictionary<int, List<int>> seedToTriangles = new Dictionary<int, List<int>>();
            for (int i = 0; i < numShards; i++)
            {
                seedToTriangles[i] = new List<int>();
            }

            // Group triangles by nearest seed
            for (int i = 0; i < sphereTriangles.Count; i += 3)
            {
                Vector3 v0 = sphereVertices[sphereTriangles[i]];
                Vector3 v1 = sphereVertices[sphereTriangles[i + 1]];
                Vector3 v2 = sphereVertices[sphereTriangles[i + 2]];
                Vector3 centroid = (v0 + v1 + v2) / 3f;

                int nearestSeed = FindNearestSeedPoint(centroid, seedPoints);
                seedToTriangles[nearestSeed].Add(i);
            }

            // Create mesh for each shard
            for (int seedIndex = 0; seedIndex < numShards; seedIndex++)
            {
                if (seedToTriangles[seedIndex].Count == 0) continue;

                List<Vector3> shardVerts = new List<Vector3>();
                List<int> shardTris = new List<int>();
                Dictionary<int, int> vertexRemap = new Dictionary<int, int>();

                foreach (int triIndex in seedToTriangles[seedIndex])
                {
                    for (int j = 0; j < 3; j++)
                    {
                        int originalVertIndex = sphereTriangles[triIndex + j];
                        
                        if (!vertexRemap.ContainsKey(originalVertIndex))
                        {
                            vertexRemap[originalVertIndex] = shardVerts.Count;
                            shardVerts.Add(sphereVertices[originalVertIndex]);
                        }

                        shardTris.Add(vertexRemap[originalVertIndex]);
                    }
                }

                // Add center vertices for structural integrity
                Vector3 center = seedPoints[seedIndex] * 0.3f; // Pull towards center
                int centerIndex = shardVerts.Count;
                shardVerts.Add(center);

                // Connect boundary vertices to center
                // This creates a more solid shard
                List<int> additionalTris = new List<int>();
                for (int i = 0; i < shardTris.Count; i += 3)
                {
                    additionalTris.Add(shardTris[i]);
                    additionalTris.Add(shardTris[i + 1]);
                    additionalTris.Add(centerIndex);

                    additionalTris.Add(shardTris[i + 1]);
                    additionalTris.Add(shardTris[i + 2]);
                    additionalTris.Add(centerIndex);

                    additionalTris.Add(shardTris[i + 2]);
                    additionalTris.Add(shardTris[i]);
                    additionalTris.Add(centerIndex);
                }

                shardTris.AddRange(additionalTris);

                // Create mesh
                Mesh shardMesh = new Mesh();
                shardMesh.name = $"Shard_{seedIndex}";
                shardMesh.vertices = shardVerts.ToArray();
                shardMesh.triangles = shardTris.ToArray();
                
                // Generate UVs (simple spherical mapping)
                Vector2[] uvs = new Vector2[shardVerts.Count];
                for (int i = 0; i < shardVerts.Count; i++)
                {
                    Vector3 v = shardVerts[i].normalized;
                    float u = 0.5f + Mathf.Atan2(v.z, v.x) / (2f * Mathf.PI);
                    float v_coord = 0.5f - Mathf.Asin(v.y) / Mathf.PI;
                    uvs[i] = new Vector2(u, v_coord);
                }
                shardMesh.uv = uvs;
                
                // Recalculate mesh properties
                shardMesh.RecalculateNormals();
                shardMesh.RecalculateBounds();
                shardMesh.RecalculateTangents();

                shards.Add(shardMesh);
            }

            return shards;
        }

        /// <summary>
        /// Generate evenly distributed points on a sphere using Fibonacci sphere
        /// </summary>
        private static List<Vector3> GenerateSphericalPoints(int count, float radius)
        {
            List<Vector3> points = new List<Vector3>();
            float goldenRatio = (1f + Mathf.Sqrt(5f)) / 2f;
            float angleIncrement = Mathf.PI * 2f * goldenRatio;

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / count;
                float inclination = Mathf.Acos(1f - 2f * t);
                float azimuth = angleIncrement * i;

                float x = Mathf.Sin(inclination) * Mathf.Cos(azimuth);
                float y = Mathf.Sin(inclination) * Mathf.Sin(azimuth);
                float z = Mathf.Cos(inclination);

                points.Add(new Vector3(x, y, z) * radius);
            }

            return points;
        }

        /// <summary>
        /// Find nearest seed point to a position
        /// </summary>
        private static int FindNearestSeedPoint(Vector3 position, List<Vector3> seedPoints)
        {
            int nearest = 0;
            float minDist = float.MaxValue;

            for (int i = 0; i < seedPoints.Count; i++)
            {
                float dist = (position - seedPoints[i]).sqrMagnitude;
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = i;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Generate icosphere mesh (subdivided icosahedron)
        /// </summary>
        private static void GenerateIcosphere(float radius, int subdivisions, List<Vector3> vertices, List<int> triangles)
        {
            // Create icosahedron base
            float t = (1f + Mathf.Sqrt(5f)) / 2f;

            vertices.Add(new Vector3(-1, t, 0).normalized * radius);
            vertices.Add(new Vector3(1, t, 0).normalized * radius);
            vertices.Add(new Vector3(-1, -t, 0).normalized * radius);
            vertices.Add(new Vector3(1, -t, 0).normalized * radius);

            vertices.Add(new Vector3(0, -1, t).normalized * radius);
            vertices.Add(new Vector3(0, 1, t).normalized * radius);
            vertices.Add(new Vector3(0, -1, -t).normalized * radius);
            vertices.Add(new Vector3(0, 1, -t).normalized * radius);

            vertices.Add(new Vector3(t, 0, -1).normalized * radius);
            vertices.Add(new Vector3(t, 0, 1).normalized * radius);
            vertices.Add(new Vector3(-t, 0, -1).normalized * radius);
            vertices.Add(new Vector3(-t, 0, 1).normalized * radius);

            // Base icosahedron faces
            List<int> faces = new List<int>
            {
                0, 11, 5,   0, 5, 1,    0, 1, 7,    0, 7, 10,   0, 10, 11,
                1, 5, 9,    5, 11, 4,   11, 10, 2,  10, 7, 6,   7, 1, 8,
                3, 9, 4,    3, 4, 2,    3, 2, 6,    3, 6, 8,    3, 8, 9,
                4, 9, 5,    2, 4, 11,   6, 2, 10,   8, 6, 7,    9, 8, 1
            };

            // Subdivide
            Dictionary<long, int> midPointCache = new Dictionary<long, int>();
            for (int i = 0; i < subdivisions; i++)
            {
                List<int> newFaces = new List<int>();
                for (int j = 0; j < faces.Count; j += 3)
                {
                    int v0 = faces[j];
                    int v1 = faces[j + 1];
                    int v2 = faces[j + 2];

                    int a = GetMiddlePoint(v0, v1, vertices, radius, midPointCache);
                    int b = GetMiddlePoint(v1, v2, vertices, radius, midPointCache);
                    int c = GetMiddlePoint(v2, v0, vertices, radius, midPointCache);

                    newFaces.Add(v0); newFaces.Add(a); newFaces.Add(c);
                    newFaces.Add(v1); newFaces.Add(b); newFaces.Add(a);
                    newFaces.Add(v2); newFaces.Add(c); newFaces.Add(b);
                    newFaces.Add(a); newFaces.Add(b); newFaces.Add(c);
                }
                faces = newFaces;
            }

            triangles.AddRange(faces);
        }

        /// <summary>
        /// Get middle point between two vertices on sphere surface
        /// </summary>
        private static int GetMiddlePoint(int v1, int v2, List<Vector3> vertices, float radius, Dictionary<long, int> cache)
        {
            long key = ((long)Mathf.Min(v1, v2) << 32) + Mathf.Max(v1, v2);
            if (cache.ContainsKey(key))
            {
                return cache[key];
            }

            Vector3 point1 = vertices[v1];
            Vector3 point2 = vertices[v2];
            Vector3 middle = ((point1 + point2) / 2f).normalized * radius;

            int index = vertices.Count;
            vertices.Add(middle);
            cache[key] = index;

            return index;
        }
    }
}

