using UnityEngine;
using System.Collections.Generic;

public class RayonManager : MonoBehaviour
{
    public Vector3 startPosition = new Vector3(0, 0, -8);
    public Vector3 direction = Vector3.forward;
    public float vitesse = 20f;
    public float rayonSphère = 0.2f;
    public Color couleurRayon = Color.magenta;
    public float intensiteImpulsion = 15f;

    private List<ProjectileInfo> projectiles = new List<ProjectileInfo>();
    public GenerateurRempart gestionnaireRempart; // Référence à obtenir dans l’éditeur ou via FindObjectOfType

    void Start()
    {
        if (gestionnaireRempart == null)
            gestionnaireRempart = FindObjectOfType<GenerateurRempart>();
    }

    void Update()
    {
        // Tir
        if (Input.GetMouseButtonDown(0))
            SpawnRayon();
        // Déplacement/projectiles
        for (int i = projectiles.Count - 1; i >= 0; i--)
        {
            var proj = projectiles[i];
            proj.go.transform.position += proj.dir.normalized * vitesse * Time.deltaTime;
            if (TesterCollisionRayon(proj.go.transform.position, rayonSphère))
            {
                Destroy(proj.go);
                projectiles.RemoveAt(i);
            }
        }
        // Gestion collision cubes/rectangles entre eux
        var objs = gestionnaireRempart.rempartObjects;
        for (int i = 0; i < objs.Count; i++)
        {
            RempartPhysique physA = objs[i]?.GetComponent<RempartPhysique>();
            if (physA == null) continue;
            for (int j = i + 1; j < objs.Count; j++)
            {
                RempartPhysique physB = objs[j]?.GetComponent<RempartPhysique>();
                if (physB == null) continue;
                physA.ResoudreCollision(physB);
            }
        }
    }

    void SpawnRayon()
    {
        GameObject go = CreerSphere(startPosition, rayonSphère, couleurRayon);
        projectiles.Add(new ProjectileInfo(go, direction));
    }
    private class ProjectileInfo
    {
        public GameObject go;
        public Vector3 dir;
        public ProjectileInfo(GameObject g, Vector3 d) { go = g; dir = d; }
    }
    bool TesterCollisionRayon(Vector3 centreRayon, float rayon)
    {
        foreach (GameObject obj in gestionnaireRempart.rempartObjects)
        {
            RempartPhysique rp = obj?.GetComponent<RempartPhysique>();
            if (rp == null) continue;
            if (RempartPhysique.CheckAABBCollision(centreRayon, Vector3.one * rayon * 2, obj.transform.position, rp.size))
            {
                Vector3 dir = (obj.transform.position - centreRayon).normalized;
                rp.AppliquerImpulsion(dir, intensiteImpulsion);
                Debug.Log($"Rayon impacte {obj.name}");
                return true;
            }
        }
        return false;
    }
    GameObject CreerSphere(Vector3 pos, float rayon, Color col)
    {
        GameObject go = new GameObject("Rayon");
        go.transform.position = pos;
        MeshFilter mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        CubeObject c = new CubeObject(rayon * 2, rayon * 2, rayon * 2, col);
        mf.mesh = MeshUtils.CreateMeshFromCubeObject(c);
        mr.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mr.material.color = col;
        return go;
    }
}
