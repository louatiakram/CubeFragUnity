using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FractureSimulator : MonoBehaviour
{
    [Header("Maillage initial")]
    public float objectSize = 6f;
    public int cellsPerAxis = 3;
    public Color baseColor = Color.gray;

    [Header("Physique")]
    public Vector3 gravity = new Vector3(0, -9.81f, 0);
    public float dt = 0.02f;
    public float fractureThreshold = 10f;

    private List<FragmentR> fragments;
    private List<Constraint> constraints;
    private bool hasBroken = false;

    void Start()
    {
        GenerateFragments();
        BuildConstraints();
    }

    void Update()
    {
        SimulateStep();
        UpdateMeshes();
    }

    void GenerateFragments()
    {
        fragments = new List<FragmentR>();
        float cellSize = objectSize / cellsPerAxis;
        float offset = objectSize * 0.5f - cellSize * 0.5f;

        for (int x = 0; x < cellsPerAxis; x++) for (int y = 0; y < cellsPerAxis; y++) for (int z = 0; z < cellsPerAxis; z++)
                {
                    Vector3 pos = transform.position + new Vector3(
                        -offset + x * cellSize,
                        -offset + y * cellSize + 5f,
                        -offset + z * cellSize
                    );
                    Matrix4x4 I = Matrix4x4.zero;
                    float m = 1f;
                    float Ixx = m * (cellSize * cellSize * 2f) / 12f;
                    I[0, 0] = Ixx; I[1, 1] = Ixx; I[2, 2] = Ixx; I[3, 3] = 1;

                    FragmentR frag = new FragmentR(m, pos, I, new CubeObjectT(cellSize, cellSize, cellSize, baseColor));
                    fragments.Add(frag);

                    GameObject go = new GameObject("Frag");
                    go.transform.parent = transform;
                    frag.meshFilter = go.AddComponent<MeshFilter>();
                    var rend = go.AddComponent<MeshRenderer>();
                    rend.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    rend.material.color = baseColor;
                    frag.mesh = new Mesh();
                    frag.meshFilter.mesh = frag.mesh;
                }
    }

    void BuildConstraints()
    {
        constraints = new List<Constraint>();
        for (int i = 0; i < fragments.Count; i++) for (int j = i + 1; j < fragments.Count; j++)
            {
                // distance centre à centre
                float d = Vector3.Distance(fragments[i].state.position, fragments[j].state.position);
                if (d <= (objectSize / cellsPerAxis) * 1.01f)
                {
                    constraints.Add(new Constraint(i, j, fractureThreshold));
                }
            }
    }

    void SimulateStep()
    {
        // Détermine le centre du cube (fragment central)
        Vector3 cubeCenter = fragments[fragments.Count / 2].state.position;

        if (!hasBroken &&
    Mathf.Abs(cubeCenter.x) < 0.01f &&
    Mathf.Abs(cubeCenter.y) < 0.01f &&
    Mathf.Abs(cubeCenter.z) < 0.01f)
        {
            foreach (var c in constraints)
                c.isBroken = true;
            hasBroken = true;
            for (int i = 0; i < fragments.Count; i++)
            {
                Vector3 randomImpulse = Random.onUnitSphere * 5f;
                fragments[i].state.P += randomImpulse * fragments[i].state.mass;
            }
            gravity = Vector3.zero;  // Arrêter la gravité si souhaité
                                     // NE PAS mettre dt=0, sinon simulation bloquée
        }



        // (le reste de la fonction ne change pas)
        Vector3[] forces = new Vector3[fragments.Count];
        Vector3[] torques = new Vector3[fragments.Count];

        for (int i = 0; i < fragments.Count; i++)
            forces[i] += gravity * fragments[i].state.mass;

        foreach (var c in constraints)
        {
            if (c.isBroken) continue;
            int i = c.a, j = c.b;
            Vector3 relPos = fragments[j].state.position - fragments[i].state.position;
            Vector3 dir = relPos.normalized;
            float relVel = Vector3.Dot(
                (fragments[j].state.P / fragments[j].state.mass) -
                (fragments[i].state.P / fragments[i].state.mass),
                dir
            );
            float impulse = -(1f + 0.3f) * relVel
                            / (1 / fragments[i].state.mass + 1 / fragments[j].state.mass);

            if (Mathf.Abs(impulse) > fractureThreshold)
            {
                c.isBroken = true;
                continue;
            }
            fragments[i].state.P += -dir * impulse;
            fragments[j].state.P += dir * impulse;
        }

        constraints.RemoveAll(c => c.isBroken);

        for (int i = 0; i < fragments.Count; i++)
            fragments[i].state.Integrate(forces[i], torques[i], dt);
        Vector3 centerPos = Vector3.zero;
        for (int i = 0; i < fragments.Count; i++)
            centerPos += fragments[i].state.position;
        centerPos /= fragments.Count;
        transform.position = centerPos;
    }


    void UpdateMeshes()
    {
        

        foreach (var frag in fragments)
        {
            frag.UpdateMesh();
            // Synchronisation de la position Unity
            frag.meshFilter.transform.localPosition = frag.state.position - transform.position;
        }
    }


}

public class Constraint
{
    public int a, b;
    public float threshold;
    public bool isBroken;
    public Constraint(int a, int b, float t) { this.a = a; this.b = b; threshold = t; }
}