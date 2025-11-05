using UnityEngine;

public class RempartPhysique : MonoBehaviour
{
    public float masse = 2f;
    public Vector3 vitesse = Vector3.zero;
    public float friction = 0.98f; // Pour ralentir progressivement
    public Vector3 size = new Vector3(1f, 1f, 1f); // à setter au spawn

    void Update()
    {
        transform.position += vitesse * Time.deltaTime;
        vitesse *= friction;
    }

    // Physique maison : collision avec un autre objet
    public void ResoudreCollision(RempartPhysique autre)
    {
        if (CheckAABBCollision(transform.position, size, autre.transform.position, autre.size))
        {
            Vector3 direction = (transform.position - autre.transform.position).normalized;
            // Correction basique : reculer chacun (simulation « rebond »)
            float force = 5f;
            vitesse += direction * force / masse;
            autre.vitesse -= direction * force / autre.masse;
        }
    }

    // Collisions AABB "from scratch"
    public static bool CheckAABBCollision(Vector3 posA, Vector3 sizeA, Vector3 posB, Vector3 sizeB)
    {
        Vector3 minA = posA - sizeA / 2;
        Vector3 maxA = posA + sizeA / 2;
        Vector3 minB = posB - sizeB / 2;
        Vector3 maxB = posB + sizeB / 2;
        return (minA.x <= maxB.x && maxA.x >= minB.x)
            && (minA.y <= maxB.y && maxA.y >= minB.y)
            && (minA.z <= maxB.z && maxA.z >= minB.z);
    }

    public void AppliquerImpulsion(Vector3 direction, float intensite)
    {
        vitesse += direction.normalized * (intensite / masse);
    }
}
