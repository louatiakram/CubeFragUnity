using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FallingFragmentingCube : MonoBehaviour
{
    public float cubeSize = 4f;
    public int fragmentsPerAxis = 4;
    public Vector3 gravity = new Vector3(0, -9.81f, 0);
    public float dt = 0.02f;

    private CubeObjectT mainCube;
    private List<FragmentCube> fragments;
    private Mesh mesh;

    private Vector3 position;
    private Vector3 velocity;
    private bool isFragmented = false;

    void Start()
    {
        position = new Vector3(0, 10f, 0);  // Position initiale
        velocity = Vector3.zero;

        mainCube = new CubeObjectT(cubeSize, cubeSize, cubeSize, Color.red);
        fragments = new List<FragmentCube>();

        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        GetComponent<MeshRenderer>().material.color = mainCube.color;

        UpdateMainCubeMesh();
    }

    void Update()
    {
        if (!isFragmented)
            UpdateMainCube();
        else
            UpdateFragments();
    }

    void UpdateMainCube()
    {
        // 1. Appliquer la gravité et intégrer
        velocity += gravity * dt;
        position += velocity * dt;

        // 2. Synchroniser le Transform
        transform.position = position;

        // 3. Test de fragmentation au point (0,0,0)
        if (!isFragmented
            && Mathf.Abs(position.x) < 0.01f
            && Mathf.Abs(position.y) < 0.01f
            && Mathf.Abs(position.z) < 0.01f)
        {
            FragmentCube();
            return;
        }

        // 4. Mettre à jour le mesh du cube principal
        UpdateMainCubeMesh();
    }

    void FragmentCube()
    {
        isFragmented = true;
        GetComponent<MeshRenderer>().enabled = false;

        float fragmentSize = cubeSize / fragmentsPerAxis;
        float offset = (cubeSize - fragmentSize) / 2f;

        for (int x = 0; x < fragmentsPerAxis; x++)
            for (int y = 0; y < fragmentsPerAxis; y++)
                for (int z = 0; z < fragmentsPerAxis; z++)
                {
                    Vector3 fragPos = position + new Vector3(
                        -offset + x * fragmentSize,
                        -offset + y * fragmentSize,
                        -offset + z * fragmentSize
                    );

                    var frag = new FragmentCube();
                    frag.Initialize(fragPos, fragmentSize, 0.1f, GetRandomColor(), 0f, 0.5f);
                    frag.velocity = velocity + Random.insideUnitSphere * 5f;
                    fragments.Add(frag);

                    var go = new GameObject("Fragment");
                    go.transform.parent = transform;
                    frag.meshFilter = go.AddComponent<MeshFilter>();
                    var rend = go.AddComponent<MeshRenderer>();
                    rend.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    rend.material.color = frag.cube.color;
                    frag.mesh = new Mesh();
                    frag.meshFilter.mesh = frag.mesh;
                }
    }

    void UpdateFragments()
    {
        foreach (var frag in fragments)
        {
            frag.UpdatePhysics(gravity, dt);
            frag.UpdateMesh();
        }
    }

    void UpdateMainCubeMesh()
    {
        var verts = new Vector3[8];
        for (int i = 0; i < 8; i++)
            verts[i] = mainCube.vertices[i] + position;

        mesh.Clear();
        mesh.vertices = verts;
        mesh.triangles = mainCube.triangles;
        mesh.RecalculateNormals();
    }

    Color GetRandomColor()
    {
        return new Color(Random.value, Random.value, Random.value);
    }
}
