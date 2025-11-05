using UnityEngine;

public class DistancePointToPlane : MonoBehaviour
{
    // Définition du plan par son vecteur normal et sa distance D
    private Vector3 normal = new Vector3(1, 2, -1); // A = 1, B = 2, C = -1
    private float D = -5.0f; // Distance du plan à l'origine

    // Le point dont on veut calculer la distance au plan
    private Vector3 point = new Vector3(3, 2, 1); // P(x1 = 3, y1 = 2, z1 = 1)

    void Start()
    {
        // Calcul de la distance
        float distance = DistanceToPoint(point, normal, D);

        // Afficher la distance dans la console
        Debug.Log("Distance du point au plan : " + distance);
    }

    // Fonction qui calcule la distance d'un point à un plan
    float DistanceToPoint(Vector3 point, Vector3 normal, float D)
    {
        // On calcule le numérateur : |A*x1 + B*y1 + C*z1 + D|
        float numerator = Mathf.Abs(Vector3.Dot(normal, point) + D);

        // On calcule le dénominateur : sqrt(A^2 + B^2 + C^2)
        float denominator = normal.magnitude;

        // On retourne la distance
        return numerator / denominator;
    }
}
