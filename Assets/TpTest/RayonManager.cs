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
    public GenerateurRempart gestionnaireRempart;

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

        // Déplacement projectiles
        for (int i = projectiles.Count - 1; i >= 0; i--)
        {
            var proj = projectiles[i];

            // Convertir en types custom
            MyVector3 currentPos = MyVector3.FromUnity(proj.go.transform.position);
            MyVector3 dirCustom = MyVector3.FromUnity(proj.dir);
            MyVector3 displacement = MyVector3.Scale(dirCustom.Normalized(), vitesse * Time.deltaTime);
            MyVector3 newPos = MyVector3.Add(currentPos, displacement);

            proj.go.transform.position = newPos.ToUnity();

            if (TesterCollisionRayon(newPos, rayonSphère))
            {
                Destroy(proj.go);
                projectiles.RemoveAt(i);
            }
        }

        // Gestion collision entre objets
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

    bool TesterCollisionRayon(MyVector3 centreRayon, float rayon)
    {
        foreach (GameObject obj in gestionnaireRempart.rempartObjects)
        {
            RempartPhysique rp = obj?.GetComponent<RempartPhysique>();
            if (rp == null) continue;

            // Utiliser collision sphère-OBB custom
            MyVector3 sizeCustom = MyVector3.FromUnity(rp.sizeUnity);
            MyVector3 closestPoint = ClosestPointOnOBB(centreRayon, obj.transform, sizeCustom);
            float distance = MyVector3.Distance(centreRayon, closestPoint);

            if (distance < rayon)
            {
                MyVector3 objPos = MyVector3.FromUnity(obj.transform.position);
                MyVector3 dir = MyVector3.Subtract(objPos, centreRayon);
                dir.Normalize();

                // Appliquer impulsion linéaire
                rp.AppliquerImpulsion(dir, intensiteImpulsion);

                // Appliquer impulsion angulaire au point de contact
                MyVector3 force = MyVector3.Scale(dir, intensiteImpulsion);
                rp.AppliquerImpulsionAngulaire(closestPoint, force);

                Debug.Log($"Rayon impacte {obj.name}");
                return true;
            }
        }
        return false;
    }

    MyVector3 ClosestPointOnOBB(MyVector3 point, Transform transform, MyVector3 size)
    {
        // Transformer le point en espace local
        Vector3 pointUnity = point.ToUnity();
        Vector3 localPointUnity = transform.InverseTransformPoint(pointUnity);
        MyVector3 localPoint = MyVector3.FromUnity(localPointUnity);

        MyVector3 halfSize = MyVector3.Scale(size, 0.5f);

        // Clamper aux limites de la boîte
        MyVector3 clampMin = MyVector3.Negate(halfSize);
        MyVector3 clampMax = halfSize;
        MyVector3 closestLocal = MyVector3.Clamp(localPoint, clampMin, clampMax);

        // Transformer de retour en espace monde
        Vector3 closestLocalUnity = closestLocal.ToUnity();
        Vector3 closestWorldUnity = transform.TransformPoint(closestLocalUnity);

        return MyVector3.FromUnity(closestWorldUnity);
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
