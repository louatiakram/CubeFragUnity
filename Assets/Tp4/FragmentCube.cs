
using UnityEngine;

public class FragmentCube
{
    public CubeObjectT cube;
    public Vector3 position;
    public Vector3 velocity;
    public Vector3 angularVelocity;
    public Matrix4x4 rotation = Matrix4x4.identity;
    public float mass;
    public Mesh mesh;
    public MeshFilter meshFilter;

    // Variables pour la collision au sol
    private float groundY = 0f;
    private float bounciness = 0.3f;
    private float friction = 0.95f;
    private bool isAtRest = false;
    private float minimumVelocity = 0.05f; // Seuil pour considérer qu'un cube est au repos

    public void Initialize(Vector3 pos, float size, float m, Color color, float ground, float bounce)
    {
        position = pos;
        mass = m;
        cube = new CubeObjectT(size, size, size, color);
        velocity = Vector3.zero;
        groundY = ground;
        bounciness = bounce;

        // Rotation aléatoire initiale
        angularVelocity = new Vector3(
            Random.Range(-5f, 5f),
            Random.Range(-5f, 5f),
            Random.Range(-5f, 5f)
        );
    }

    public void UpdatePhysics(Vector3 gravity, float dt)
    {
        // Si le cube est au repos, ne pas appliquer de physique
        if (isAtRest) return;

        // Appliquer la gravité seulement si on n'est pas au sol
        if (position.y > groundY)
        {
            velocity += gravity * dt;
        }

        // Mettre à jour la position
        position += velocity * dt;

        // Collision avec le sol
        if (position.y <= groundY)
        {
            position.y = groundY; // Fixer exactement au niveau du sol

            // Si la vitesse verticale est vers le bas, la inverser avec rebond
            if (velocity.y < 0)
            {
                velocity.y = -velocity.y * bounciness;
            }

            // Appliquer la friction sur les axes horizontaux
            velocity.x *= friction;
            velocity.z *= friction;

            // Vérifier si le cube doit s'arrêter définitivement
            if (Mathf.Abs(velocity.y) < minimumVelocity &&
                Mathf.Abs(velocity.x) < minimumVelocity &&
                Mathf.Abs(velocity.z) < minimumVelocity)
            {
                // Arrêter complètement le cube
                velocity = Vector3.zero;
                angularVelocity = Vector3.zero;
                isAtRest = true;
                position.y = groundY; // S'assurer qu'il reste exactement au sol
            }
        }

        // Mettre à jour la rotation seulement si pas au repos
        if (!isAtRest)
        {
            UpdateRotation(dt);
        }
    }

    void UpdateRotation(float dt)
    {
        // Créer la matrice antisymétrique Omega
        Matrix4x4 Omega = Matrix4x4.zero;
        Omega[0, 1] = -angularVelocity.z; Omega[0, 2] = angularVelocity.y;
        Omega[1, 0] = angularVelocity.z; Omega[1, 2] = -angularVelocity.x;
        Omega[2, 0] = -angularVelocity.y; Omega[2, 1] = angularVelocity.x;

        // Mettre à jour la rotation
        Matrix4x4 rotationUpdate = Math3D.Add(Matrix4x4.identity, Math3D.MultiplyScalar(Omega, dt));
        rotation = Math3D.MultiplyMatrix4x4(rotationUpdate, rotation);

        // Orthonormaliser pour éviter la dérive
        rotation = Math3D.GramSchmidt(rotation);

        // Réduire progressivement la vitesse angulaire (friction angulaire)
        angularVelocity *= 0.99f;
    }

    public void UpdateMesh()
    {
        if (mesh == null || meshFilter == null) return;

        Vector3[] transformedVertices = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            // Appliquer la rotation puis la translation
            transformedVertices[i] = Math3D.MultiplyMatrixVector3(rotation, cube.vertices[i]) + position;
        }

        mesh.Clear();
        mesh.vertices = transformedVertices;
        mesh.triangles = cube.triangles;
        mesh.RecalculateNormals();
    }
}