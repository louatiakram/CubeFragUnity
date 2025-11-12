using UnityEngine;
using System.Collections.Generic;

public class GenerateurRempart : MonoBehaviour
{
    public Vector3 positionCubeGauche = new Vector3(-5f, 0f, 0f);
    public Vector3 positionCubeDroit = new Vector3(5f, 0f, 0f);
    public Vector3 positionRectGauche = new Vector3(-2f, 0f, 0f);
    public Vector3 positionRectDroit = new Vector3(2f, 0f, 0f);

    public float tailleCube = 1f;
    public Vector3 tailleRectangle = new Vector3(4f, 1f, 0.5f);
    public List<GameObject> rempartObjects = new List<GameObject>();

    void Start()
    {
        CreerObjet(positionCubeGauche, new Vector3(tailleCube, tailleCube, tailleCube), "CubeGauche", Color.blue);
        CreerObjet(positionCubeDroit, new Vector3(tailleCube, tailleCube, tailleCube), "CubeDroit", Color.red);
        CreerObjet(positionRectGauche, tailleRectangle, "RectGauche", Color.green);
        CreerObjet(positionRectDroit, tailleRectangle, "RectDroit", Color.yellow);
    }

    private List<(RempartPhysique obj, float min, float max)> GetPotentialPairsX()
    {
        List<(RempartPhysique obj, float min, float max)> intervals = new List<(RempartPhysique, float, float)>();

        // Créer les intervalles sur X pour chaque objet
        foreach (var go in rempartObjects)
        {
            if (go == null) continue;
            RempartPhysique phys = go.GetComponent<RempartPhysique>();
            if (phys == null) continue;

            Vector3 pos = go.transform.position;
            Vector3 size = phys.sizeUnity;
            float minX = pos.x - size.x * 0.5f;
            float maxX = pos.x + size.x * 0.5f;

            intervals.Add((phys, minX, maxX));
        }

        return intervals;
    }

    void FixedUpdate()
    {
        // ✅ Version OPTIMISÉE avec Sweep and Prune
        var intervalsX = GetPotentialPairsX();

        // Trier par min X
        intervalsX.Sort((a, b) => a.min.CompareTo(b.min));

        // Sweep : détecter les chevauchements sur X
        for (int i = 0; i < intervalsX.Count; i++)
        {
            var (physA, minA, maxA) = intervalsX[i];

            // Tester seulement avec les objets qui commencent avant que A se termine
            for (int j = i + 1; j < intervalsX.Count && intervalsX[j].min <= maxA; j++)
            {
                var (physB, minB, maxB) = intervalsX[j];

                // ✅ Test AABB complet (Y et Z aussi)
                if (!TestAABBRapide(physA.transform, physA.sizeUnity,
                                    physB.transform, physB.sizeUnity))
                {
                    continue;
                }

                // ✅ Test OBB (narrow phase)
                MyVector3 sizeA = MyVector3.FromUnity(physA.sizeUnity);
                MyVector3 sizeB = MyVector3.FromUnity(physB.sizeUnity);
                OBBCollisionInfo info = OBBCollision.CheckOBBCollision(
                    physA.transform, sizeA,
                    physB.transform, sizeB
                );

                if (info.isColliding)
                {
                    physA.ResoudreCollision(physB);
                }
            }
        }
    }

    private bool TestAABBRapide(Transform transformA, Vector3 sizeA, Transform transformB, Vector3 sizeB)
    {
        Vector3 posA = transformA.position;
        Vector3 posB = transformB.position;
        Vector3 minA = posA - sizeA * 0.5f;
        Vector3 maxA = posA + sizeA * 0.5f;
        Vector3 minB = posB - sizeB * 0.5f;
        Vector3 maxB = posB + sizeB * 0.5f;

        return (minA.x <= maxB.x && maxA.x >= minB.x) &&
               (minA.y <= maxB.y && maxA.y >= minB.y) &&
               (minA.z <= maxB.z && maxA.z >= minB.z);
    }

    void CreerObjet(Vector3 position, Vector3 taille, string nom, Color couleur)
    {
        foreach (var go in rempartObjects)
        {
            RempartPhysique phys = go.GetComponent<RempartPhysique>();
            if (phys != null)
            {
                GameObject temp = new GameObject();
                temp.transform.position = position;
                temp.transform.rotation = Quaternion.identity;

                MyVector3 sizeA = MyVector3.FromUnity(taille);
                MyVector3 sizeB = MyVector3.FromUnity(phys.sizeUnity);

                OBBCollisionInfo info = OBBCollision.CheckOBBCollision(
                    temp.transform, sizeA, go.transform, sizeB
                );
                Object.DestroyImmediate(temp);

                if (info.isColliding)
                {
                    Debug.LogWarning($"Chevauchement détecté, {nom} non créé.");
                    return;
                }
            }
        }

        GameObject goNew = new GameObject(nom);
        goNew.transform.position = position;

        MeshFilter mf = goNew.AddComponent<MeshFilter>();
        MeshRenderer mr = goNew.AddComponent<MeshRenderer>();

        CubeObject obj = new CubeObject(taille.x, taille.y, taille.z, couleur);
        mf.mesh = MeshUtils.CreateMeshFromCubeObject(obj);

        mr.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mr.material.color = couleur;

        RempartPhysique rp = goNew.AddComponent<RempartPhysique>();
        rp.sizeUnity = taille;
        rp.centreInertieLocal = Vector3.zero; // ✅ Centre d'inertie au centre géométrique

        rempartObjects.Add(goNew);
    }
}
