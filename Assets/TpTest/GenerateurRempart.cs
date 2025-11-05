using UnityEngine;
using System.Collections.Generic;

public class GenerateurRempart : MonoBehaviour
{
    // Positions modifiables dans l'inspecteur
    public Vector3 positionCubeGauche = new Vector3(-5f, 0f, 0f);
    public Vector3 positionCubeDroit = new Vector3(5f, 0f, 0f);
    public Vector3 positionRectGauche = new Vector3(-2f, 0f, 0f);
    public Vector3 positionRectDroit = new Vector3(2f, 0f, 0f);

    public float tailleCube = 1f;
    public Vector3 tailleRectangle = new Vector3(4f, 1f, 0.5f);
    public List<GameObject> rempartObjects = new List<GameObject>();

    void Start()
    {
        GameObject cubeGauche = CreerObjet(positionCubeGauche, new Vector3(tailleCube, tailleCube, tailleCube), "CubeGauche", Color.blue);
        GameObject cubeDroit = CreerObjet(positionCubeDroit, new Vector3(tailleCube, tailleCube, tailleCube), "CubeDroit", Color.red);
        GameObject rectGauche = CreerObjet(positionRectGauche, tailleRectangle, "RectGauche", Color.green);
        GameObject rectDroit = CreerObjet(positionRectDroit, tailleRectangle, "RectDroit", Color.yellow);
    }

    GameObject CreerObjet(Vector3 position, Vector3 taille, string nom, Color couleur)
    {
        // Empêche chevauchement
        foreach (var go in rempartObjects)
        {
            RempartPhysique phys = go.GetComponent<RempartPhysique>();
            if (phys != null && RempartPhysique.CheckAABBCollision(position, taille, go.transform.position, phys.size))
            {
                Debug.LogWarning($"Chevauchement détecté, {nom} non créé.");
                return null;
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
        rp.size = taille;
        rempartObjects.Add(goNew);
        return goNew;
    }
}
