using UnityEngine;
using System.Collections.Generic;

public class GenerateurRempart : MonoBehaviour
{
    // Positions modifiables dans l'inspecteur (Unity Vector3 pour l'inspector)
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

    void CreerObjet(Vector3 position, Vector3 taille, string nom, Color couleur)
    {
        // Convertir en types custom
        MyVector3 posCustom = MyVector3.FromUnity(position);
        MyVector3 tailleCustom = MyVector3.FromUnity(taille);

        // Vérifier chevauchement avec OBB
        foreach (var go in rempartObjects)
        {
            RempartPhysique phys = go.GetComponent<RempartPhysique>();
            if (phys != null)
            {
                // Créer transform temporaire pour vérification
                GameObject temp = new GameObject();
                temp.transform.position = position;

                // Obtenir la taille de l'objet existant depuis son inspector
                Vector3 existingSize = phys.sizeUnity;
                MyVector3 existingSizeCustom = MyVector3.FromUnity(existingSize);

                OBBCollisionInfo info = OBBCollision.CheckOBBCollision(
                    temp.transform, tailleCustom,
                    go.transform, existingSizeCustom
                );

                Destroy(temp);

                if (info.isColliding)
                {
                    Debug.LogWarning($"Chevauchement détecté, {nom} non créé.");
                    return;
                }
            }
        }

        // Créer l'objet
        GameObject goNew = new GameObject(nom);
        goNew.transform.position = position;

        MeshFilter mf = goNew.AddComponent<MeshFilter>();
        MeshRenderer mr = goNew.AddComponent<MeshRenderer>();

        CubeObject obj = new CubeObject(taille.x, taille.y, taille.z, couleur);
        mf.mesh = MeshUtils.CreateMeshFromCubeObject(obj);

        mr.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mr.material.color = couleur;

        RempartPhysique rp = goNew.AddComponent<RempartPhysique>();
        rp.sizeUnity = taille; // Visible dans l'inspector

        rempartObjects.Add(goNew);
    }
}
