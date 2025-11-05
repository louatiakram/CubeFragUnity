using UnityEngine;

public class ProjectionDistance : MonoBehaviour
{
    // Points A, B, et C
    private Vector3 pointA = new Vector3(1, 4, -5);
    private Vector3 pointB = new Vector3(2, -3, 8);
    private Vector3 pointC = new Vector3(2, 3, 6);

    void Start()
    {
        // Calcul du vecteur BA et du vecteur V (AC)
        Vector3 BA = pointB - pointA;
        Vector3 V = pointC - pointA;

        // Projection de BA sur V
        Vector3 projectionBAonV = ProjectVector(BA, V);

        // Distance entre B et la droite passant par A et C
        float distance = CalculateDistance(BA, projectionBAonV);

        // Afficher les résultats dans la console
        Debug.Log("Projection de BA sur V: " + projectionBAonV);
        Debug.Log("Distance entre B et la droite passant par A et C: " + distance);
    }

    // Méthode pour projeter un vecteur sur un autre
    Vector3 ProjectVector(Vector3 u, Vector3 v)
    {
        float dotProductUV = Vector3.Dot(u, v); // Produit scalaire u · v
        float dotProductVV = Vector3.Dot(v, v); // Produit scalaire v · v
        return (dotProductUV / dotProductVV) * v; // Projection de u sur v
    }

    // Méthode pour calculer la distance
    float CalculateDistance(Vector3 BA, Vector3 projection)
    {
        // Norme de BA
        float normBA = BA.magnitude;

        // Norme de la projection
        float normProjection = projection.magnitude;

        // Distance entre le point et la droite
        return Mathf.Sqrt(normBA * normBA - normProjection * normProjection);
    }
}
