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
        // 1) TEST DE FRACTURE GLOBALE À 0,0,0
        if (!hasBroken
            && Vector3.Distance(transform.position, Vector3.zero) < 0.001f)
        {
            // On casse tout d’un coup
            foreach (var c in constraints)
                c.isBroken = true;
            hasBroken = true;
        }

        // 2) Reset forces/torques
        Vector3[] forces = new Vector3[fragments.Count];
        Vector3[] torques = new Vector3[fragments.Count];

        // 3) Appliquer la gravité
        for (int i = 0; i < fragments.Count; i++)
            forces[i] += gravity * fragments[i].state.mass;

        // 4) Résolution des contraintes (simple impulse)
        foreach (var c in constraints)
        {
            if (c.isBroken) continue;  // ← Ne plus traiter les contraintes déjà cassées
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

            // fracture check local (optionnel, garde l’ancien seuil)
            if (Mathf.Abs(impulse) > fractureThreshold)
            {
                c.isBroken = true;
                continue;
            }
            fragments[i].state.P += -dir * impulse;
            fragments[j].state.P += dir * impulse;
        }

        // 5) Purger toutes les contraintes cassées
        constraints.RemoveAll(c => c.isBroken);

        // 6) Intégrer chaque fragment
        for (int i = 0; i < fragments.Count; i++)
            fragments[i].state.Integrate(forces[i], torques[i], dt);
    }   

    void UpdateMeshes()
    {
        foreach (var frag in fragments)
        {
            frag.UpdateMesh();
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