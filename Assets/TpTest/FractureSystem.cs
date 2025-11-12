using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Système de Fracture pour Rigid Bodies - CORRIGÉ
/// </summary>
public class FractureSystem : MonoBehaviour
{
    [Header("Paramètres de Fracture")]
    public float seuilImpulsionFracture = 5f;
    public float energieTransferFactor = 0.8f;
    public int fragmentsParDirection = 2;

    public GenerateurRempart gestionnaireRempart;

    void Start()
    {
        if (gestionnaireRempart == null)
            gestionnaireRempart = FindObjectOfType<GenerateurRempart>();
    }

    public void TesterFracture(RempartPhysique objA, RempartPhysique objB,
                                MyVector3 pointContact, MyVector3 normale, float impulseMagnitude)
    {
        if (impulseMagnitude > seuilImpulsionFracture)
        {
            // ✅ CORRIGÉ: Appeler avec les bons arguments
            if (Random.value > 0.5f)
                FractureObjet(objA, pointContact, normale, impulseMagnitude);

            // ✅ CORRIGÉ: Inverser la normale correctement
            if (Random.value > 0.5f)
            {
                MyVector3 normaleInversee = MyVector3.Scale(normale, -1f);
                FractureObjet(objB, pointContact, normaleInversee, impulseMagnitude);
            }
        }
    }

    // ✅ CORRIGÉ: Signature de la méthode (4 arguments, pas 5)
    private void FractureObjet(RempartPhysique objet, MyVector3 pointContact,
                                MyVector3 normale, float impulseMagnitude)
    {
        Debug.Log($"💥 FRACTURE: {objet.gameObject.name} se casse!");

        // ✅ CORRIGÉ: Récupérer position et taille correctement
        Vector3 positionOrigine = objet.transform.position;
        Vector3 tailleOrigine = objet.sizeUnity;
        float masseOrigine = objet.masse;
        Color couleurOrigine = objet.GetComponent<Renderer>().material.color;

        // Créer les fragments
        int totalFragments = fragmentsParDirection * fragmentsParDirection * fragmentsParDirection;
        Vector3 tailleFragment = new Vector3(
            tailleOrigine.x / fragmentsParDirection,
            tailleOrigine.y / fragmentsParDirection,
            tailleOrigine.z / fragmentsParDirection
        );
        float masseFragment = masseOrigine / totalFragments;

        // Distribuer l'énergie
        float energieDisponible = impulseMagnitude * energieTransferFactor;
        float energieParFragment = energieDisponible / totalFragments;

        // ✅ Créer grille 3D de fragments
        for (int x = 0; x < fragmentsParDirection; x++)
        {
            for (int y = 0; y < fragmentsParDirection; y++)
            {
                for (int z = 0; z < fragmentsParDirection; z++)
                {
                    // Position du fragment
                    Vector3 offsetLocal = new Vector3(
                        (x - fragmentsParDirection / 2f) * tailleFragment.x,
                        (y - fragmentsParDirection / 2f) * tailleFragment.y,
                        (z - fragmentsParDirection / 2f) * tailleFragment.z
                    );
                    Vector3 posFragment = positionOrigine + offsetLocal;

                    // Créer le GameObject fragment
                    GameObject fragmentGO = new GameObject($"Fragment_{x}_{y}_{z}");
                    fragmentGO.transform.position = posFragment;

                    MeshFilter mf = fragmentGO.AddComponent<MeshFilter>();
                    MeshRenderer mr = fragmentGO.AddComponent<MeshRenderer>();

                    // ✅ CORRIGÉ: Créer le mesh correctement
                    CubeObject cubeData = new CubeObject(
                        tailleFragment.x,
                        tailleFragment.y,
                        tailleFragment.z,
                        couleurOrigine
                    );
                    mf.mesh = MeshUtils.CreateMeshFromCubeObject(cubeData);

                    mr.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mr.material.color = couleurOrigine;

                    // Ajouter RempartPhysique
                    RempartPhysique fragPhys = fragmentGO.AddComponent<RempartPhysique>();
                    fragPhys.masse = masseFragment;
                    fragPhys.sizeUnity = tailleFragment;
                    fragPhys.coefficientRestitution = 0.5f;

                    // Ajouter aux objets du gestionnaire
                    gestionnaireRempart.rempartObjects.Add(fragmentGO);

                    // ✅ CORRIGÉ: Appliquer impulsion (convertir Vector3 en MyVector3)
                    MyVector3 offsetLocalCustom = MyVector3.FromUnity(offsetLocal);
                    MyVector3 directionFragment = offsetLocalCustom.Magnitude() > 0.01f
                        ? offsetLocalCustom.Normalized()
                        : MyVector3.FromUnity(Random.onUnitSphere);

                    fragPhys.AppliquerImpulsion(directionFragment, energieParFragment);

                    // ✅ CORRIGÉ: Ajouter rotation aléatoire
                    MyVector3 axeRotation = MyVector3.FromUnity(Random.onUnitSphere);
                    MyVector3 torqueForce = MyVector3.Scale(axeRotation, energieParFragment * 0.5f);
                    fragPhys.AppliquerImpulsionAngulaire(
                        MyVector3.FromUnity(posFragment),
                        torqueForce
                    );
                }
            }
        }

        // Supprimer l'objet original
        gestionnaireRempart.rempartObjects.Remove(objet.gameObject);
        Destroy(objet.gameObject);
    }
}