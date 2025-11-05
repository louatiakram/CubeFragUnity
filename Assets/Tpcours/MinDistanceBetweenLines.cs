using UnityEngine;

public class MinDistanceBetweenLines : MonoBehaviour
{
    // Points A, B, C, D définissant les droites (AB) et (CD)
    public Vector3 A = new Vector3(1, 0, 4);
    public Vector3 B = new Vector3(-1, 1, 3);
    public Vector3 C = new Vector3(0, 5, 4);
    public Vector3 D = new Vector3(1, 2, 2);

    void Start()
    {
        // Vecteurs directionnels des droites
        Vector3 V1 = B - A; // Vecteur AB
        Vector3 V2 = D - C; // Vecteur CD

        // Vecteur entre les deux points d'origine
        Vector3 S1_minus_S2 = A - C;

        // Calcul des produits scalaires
        float V1_dot_V1 = Vector3.Dot(V1, V1);
        float V2_dot_V2 = Vector3.Dot(V2, V2);
        float V1_dot_V2 = Vector3.Dot(V1, V2);
        float S1_minus_S2_dot_V1 = Vector3.Dot(S1_minus_S2, V1);
        float S1_minus_S2_dot_V2 = Vector3.Dot(S1_minus_S2, V2);

        // Construction du système d'équations
        float[,] matrix = {
            { V1_dot_V1, -V1_dot_V2 },
            { -V1_dot_V2, V2_dot_V2 }
        };

        float[] constants = {
            -S1_minus_S2_dot_V1,
            S1_minus_S2_dot_V2
        };

        // Résolution du système (utilisation d'une inversion manuelle de matrice)
        float det = matrix[0, 0] * matrix[1, 1] - matrix[0, 1] * matrix[1, 0];

        if (Mathf.Abs(det) < 1e-6)
        {
            Debug.LogError("Les droites sont parallèles ou très proches.");
            return;
        }

        // Calcul de l'inverse de la matrice
        float invDet = 1.0f / det;
        float[,] inverseMatrix = {
            { matrix[1, 1] * invDet, -matrix[0, 1] * invDet },
            { -matrix[1, 0] * invDet, matrix[0, 0] * invDet }
        };

        // Résolution pour t1 et t2
        float t1 = inverseMatrix[0, 0] * constants[0] + inverseMatrix[0, 1] * constants[1];
        float t2 = inverseMatrix[1, 0] * constants[0] + inverseMatrix[1, 1] * constants[1];

        // Calcul des points sur les droites correspondants aux valeurs t1 et t2
        Vector3 P1 = A + t1 * V1;
        Vector3 P2 = C + t2 * V2;

        // Calcul de la distance minimale
        float distance = Vector3.Distance(P1, P2);

        // Affichage du résultat dans la console Unity
        Debug.Log($"La distance minimale entre les droites est : {distance}");
    }
}
