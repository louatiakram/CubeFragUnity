using UnityEngine;
using System.Collections.Generic;
using static QuaternionRotation;

/// <summary>
/// Physique d'un fragment individuel - VERSION CORRIGÉE
/// Inclut: position, rotation (matrices 4x4), masse, inertie, centre d'inertie
/// </summary>
public class FragmentPhysicsScript : MonoBehaviour
{
    [Header("Fragment Properties")]
    public float masse = 0.1f;
    public Vector3 sizeLocal = new Vector3(1f, 1f, 1f);
    public Vector3 centreInertieLocal = Vector3.zero;

    [Header("Physics")]
    public Vector3 position;
    public Matrix4x4 rotationMatrix = Matrix4x4.identity;
    public Vector3 velocity = Vector3.zero;
    public Vector3 vitesseAngulaire = Vector3.zero;

    private Vector3 quantiteMouvement;
    private Vector3 momentAngulaire;
    private Matrix3x3 tensorInertieLocal;
    private Matrix3x3 tensorInertieInverseWorld;

    private Vector3 accumulatedForce = Vector3.zero;
    private Vector3 accumulatedTorque = Vector3.zero;

    private Mesh mesh;
    private CubeObject cubeData;

    void Start()
    {
        CalculerTensorInertie();
        CreateMesh();
    }

    void FixedUpdate()
    {
        // Intégration RK2 simple pour rapidité
        float dt = Time.fixedDeltaTime;

        // Appliquer forces et torques
        Vector3 acceleration = accumulatedForce / masse;
        quantiteMouvement += accumulatedForce * dt;
        momentAngulaire += accumulatedTorque * dt;

        // Mettre à jour vitesses
        velocity = quantiteMouvement / masse;
        UpdateAngularVelocity();

        // Intégrer position et rotation
        position += velocity * dt;
        IntegrateRotation(dt);

        // Collision avec sol
        CheckGroundCollision();

        // Mise à jour mesh
        UpdateMeshTransform();

        // Réinitialiser forces
        accumulatedForce = Vector3.zero;
        accumulatedTorque = Vector3.zero;
    }

    private void CalculerTensorInertie()
    {
        float lx = sizeLocal.x, ly = sizeLocal.y, lz = sizeLocal.z;
        float Ixx = (masse / 12f) * (ly * ly + lz * lz);
        float Iyy = (masse / 12f) * (lx * lx + lz * lz);
        float Izz = (masse / 12f) * (lx * lx + ly * ly);

        tensorInertieLocal = new Matrix3x3(
            new Vector3(Ixx, 0, 0),
            new Vector3(0, Iyy, 0),
            new Vector3(0, 0, Izz)
        );
    }

    private void UpdateAngularVelocity()
    {
        // ω = I^-1 * L
        Matrix3x3 R = RotationMatrixToMatrix3x3(rotationMatrix);
        Matrix3x3 RT = TransposeMatrix3x3(R);

        Matrix3x3 IWorld = MultiplyMatrix3x3(MultiplyMatrix3x3(R, tensorInertieLocal), RT);
        tensorInertieInverseWorld = InverseMatrix3x3(IWorld);

        Vector3 L_unity = momentAngulaire;
        Vector3 omega_unity = ApplyMatrix3x3ToVector3(tensorInertieInverseWorld, L_unity);
        vitesseAngulaire = omega_unity;
    }

    private void IntegrateRotation(float dt)
    {
        // dR/dt = [ω]_x * R  où [ω]_x est la matrice antisymétrique
        Matrix4x4 omegaMatrix = AntisymmetricMatrix(vitesseAngulaire);

        // ✅ CORRIGÉ: Multiplier par dt correctement
        Matrix4x4 omegaMatrixDt = MultiplyMatrix4x4ScalarCorrect(omegaMatrix, dt);

        // R_new = (I + [ω]_x * dt) * R
        Matrix4x4 rotUpdate = Matrix4x4.identity;
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                rotUpdate[i, j] += omegaMatrixDt[i, j];

        rotationMatrix = MultiplyMatrix4x4(rotUpdate, rotationMatrix);

        // Orthonormaliser pour éviter drift
        rotationMatrix = GramSchmidt(rotationMatrix);
    }

    private void CheckGroundCollision()
    {
        float groundY = 0f;
        float demiHauteur = sizeLocal.y * 0.5f;

        if (position.y - demiHauteur < groundY)
        {
            position.y = groundY + demiHauteur;
            velocity.y = 0;
            vitesseAngulaire *= 0.8f;
        }
    }

    private void CreateMesh()
    {
        cubeData = new CubeObject(sizeLocal.x, sizeLocal.y, sizeLocal.z, GetRandomColor());

        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null) mf = gameObject.AddComponent<MeshFilter>();

        mesh = new Mesh();
        mf.mesh = mesh;

        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr == null) mr = gameObject.AddComponent<MeshRenderer>();

        mr.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mr.material.color = cubeData.color;
    }

    private void UpdateMeshTransform()
    {
        if (mesh == null || cubeData == null) return;

        Vector3[] vertices = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            Vector3 localVert = cubeData.vertices[i];
            Vector3 rotatedVert = ApplyMatrix4x4ToVector3(rotationMatrix, localVert);
            vertices[i] = rotatedVert + position;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = cubeData.triangles;
        mesh.RecalculateNormals();
    }

    public void AddForce(Vector3 force)
    {
        accumulatedForce += force;
    }

    public void AddImpulsion(Vector3 impulsion)
    {
        quantiteMouvement += impulsion;
        velocity = quantiteMouvement / masse;
    }

    public void AddTorque(Vector3 torque)
    {
        accumulatedTorque += torque;
    }

    // ========== Helpers matricielles ==========

    private Matrix4x4 AntisymmetricMatrix(Vector3 v)
    {
        Matrix4x4 m = Matrix4x4.zero;
        m[0, 1] = -v.z; m[0, 2] = v.y;
        m[1, 0] = v.z; m[1, 2] = -v.x;
        m[2, 0] = -v.y; m[2, 1] = v.x;
        m[3, 3] = 1;
        return m;
    }

    // ✅ CORRIGÉ: Multiplication Matrix4x4 * scalar
    private Matrix4x4 MultiplyMatrix4x4ScalarCorrect(Matrix4x4 m, float scalar)
    {
        Matrix4x4 result = new Matrix4x4();
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                result[i, j] = m[i, j] * scalar;
        return result;
    }

    // ✅ CORRIGÉ: Multiplication Matrix4x4 * Matrix4x4
    private Matrix4x4 MultiplyMatrix4x4(Matrix4x4 a, Matrix4x4 b)
    {
        Matrix4x4 result = new Matrix4x4();
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
            {
                result[i, j] = 0;
                for (int k = 0; k < 4; k++)
                    result[i, j] += a[i, k] * b[k, j];
            }
        return result;
    }

    private Matrix4x4 GramSchmidt(Matrix4x4 mat)
    {
        // Orthonormaliser les 3 premières colonnes
        Vector4 col0 = mat.GetColumn(0);
        Vector4 col1 = mat.GetColumn(1);
        Vector4 col2 = mat.GetColumn(2);

        col0.Normalize();
        col1 = col1 - Vector4.Dot(col0, col1) * col0;
        col1.Normalize();
        col2 = col2 - Vector4.Dot(col0, col2) * col0 - Vector4.Dot(col1, col2) * col1;
        col2.Normalize();

        Matrix4x4 result = Matrix4x4.identity;
        result.SetColumn(0, col0);
        result.SetColumn(1, col1);
        result.SetColumn(2, col2);
        return result;
    }

    private Vector3 ApplyMatrix4x4ToVector3(Matrix4x4 m, Vector3 v)
    {
        Vector4 v4 = new Vector4(v.x, v.y, v.z, 0);
        Vector4 result = m * v4;
        return new Vector3(result.x, result.y, result.z);
    }

    private Vector3 ApplyMatrix3x3ToVector3(Matrix3x3 m, Vector3 v)
    {
        return new Vector3(
            m.m[0, 0] * v.x + m.m[0, 1] * v.y + m.m[0, 2] * v.z,
            m.m[1, 0] * v.x + m.m[1, 1] * v.y + m.m[1, 2] * v.z,
            m.m[2, 0] * v.x + m.m[2, 1] * v.y + m.m[2, 2] * v.z
        );
    }

    // ✅ CORRIGÉ: Utiliser m[i,j] au lieu de m.m[i,j] pour Matrix4x4
    private Matrix3x3 RotationMatrixToMatrix3x3(Matrix4x4 m)
    {
        return new Matrix3x3(
            new Vector3(m[0, 0], m[0, 1], m[0, 2]),
            new Vector3(m[1, 0], m[1, 1], m[1, 2]),
            new Vector3(m[2, 0], m[2, 1], m[2, 2])
        );
    }

    private Matrix3x3 TransposeMatrix3x3(Matrix3x3 m)
    {
        return new Matrix3x3(
            new Vector3(m.m[0, 0], m.m[1, 0], m.m[2, 0]),
            new Vector3(m.m[0, 1], m.m[1, 1], m.m[2, 1]),
            new Vector3(m.m[0, 2], m.m[1, 2], m.m[2, 2])
        );
    }

    private Matrix3x3 MultiplyMatrix3x3(Matrix3x3 a, Matrix3x3 b)
    {
        Matrix3x3 result = new Matrix3x3(Vector3.zero, Vector3.zero, Vector3.zero);
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
            {
                result.m[i, j] = 0;
                for (int k = 0; k < 3; k++)
                    result.m[i, j] += a.m[i, k] * b.m[k, j];
            }
        return result;
    }

    private Matrix3x3 InverseMatrix3x3(Matrix3x3 mat)
    {
        float det = mat.m[0, 0] * (mat.m[1, 1] * mat.m[2, 2] - mat.m[1, 2] * mat.m[2, 1]) -
                    mat.m[0, 1] * (mat.m[1, 0] * mat.m[2, 2] - mat.m[1, 2] * mat.m[2, 0]) +
                    mat.m[0, 2] * (mat.m[1, 0] * mat.m[2, 1] - mat.m[1, 1] * mat.m[2, 0]);

        if (Mathf.Abs(det) < 0.0001f) return mat;

        float invDet = 1f / det;
        Matrix3x3 inv = new Matrix3x3(Vector3.zero, Vector3.zero, Vector3.zero);
        inv.m[0, 0] = (mat.m[1, 1] * mat.m[2, 2] - mat.m[1, 2] * mat.m[2, 1]) * invDet;
        inv.m[0, 1] = (mat.m[0, 2] * mat.m[2, 1] - mat.m[0, 1] * mat.m[2, 2]) * invDet;
        inv.m[0, 2] = (mat.m[0, 1] * mat.m[1, 2] - mat.m[0, 2] * mat.m[1, 1]) * invDet;
        inv.m[1, 0] = (mat.m[1, 2] * mat.m[2, 0] - mat.m[1, 0] * mat.m[2, 2]) * invDet;
        inv.m[1, 1] = (mat.m[0, 0] * mat.m[2, 2] - mat.m[0, 2] * mat.m[2, 0]) * invDet;
        inv.m[1, 2] = (mat.m[0, 2] * mat.m[1, 0] - mat.m[0, 0] * mat.m[1, 2]) * invDet;
        inv.m[2, 0] = (mat.m[1, 0] * mat.m[2, 1] - mat.m[1, 1] * mat.m[2, 0]) * invDet;
        inv.m[2, 1] = (mat.m[0, 1] * mat.m[2, 0] - mat.m[0, 0] * mat.m[2, 1]) * invDet;
        inv.m[2, 2] = (mat.m[0, 0] * mat.m[1, 1] - mat.m[0, 1] * mat.m[1, 0]) * invDet;
        return inv;
    }

    private Color GetRandomColor()
    {
        return new Color(Random.Range(0.3f, 1f), Random.Range(0.3f, 1f), Random.Range(0.3f, 1f));
    }
}