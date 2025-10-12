using UnityEngine;

public static class Math3D
{
    // Multiplication matrice × vecteur
    public static Vector3 MultiplyMatrixVector3(Matrix4x4 M, Vector3 v)
    {
        return new Vector3(
            M[0, 0] * v.x + M[0, 1] * v.y + M[0, 2] * v.z,
            M[1, 0] * v.x + M[1, 1] * v.y + M[1, 2] * v.z,
            M[2, 0] * v.x + M[2, 1] * v.y + M[2, 2] * v.z
        );
    }

    // Gram-Schmidt
    public static Matrix4x4 GramSchmidt(Matrix4x4 R)
    {
        Vector3 x = new Vector3(R[0, 0], R[1, 0], R[2, 0]);
        Vector3 y = new Vector3(R[0, 1], R[1, 1], R[2, 1]);
        Vector3 z = new Vector3(R[0, 2], R[1, 2], R[2, 2]);

        Vector3 u1 = x.normalized;
        Vector3 u2 = (y - Vector3.Dot(y, u1) * u1).normalized;
        Vector3 u3 = Vector3.Cross(u1, u2);

        Matrix4x4 Rnew = Matrix4x4.identity;
        Rnew[0, 0] = u1.x; Rnew[1, 0] = u1.y; Rnew[2, 0] = u1.z;
        Rnew[0, 1] = u2.x; Rnew[1, 1] = u2.y; Rnew[2, 1] = u2.z;
        Rnew[0, 2] = u3.x; Rnew[1, 2] = u3.y; Rnew[2, 2] = u3.z;

        return Rnew;
    }

    // Multiplication matrice × scalaire
    public static Matrix4x4 MultiplyScalar(Matrix4x4 M, float s)
    {
        Matrix4x4 result = Matrix4x4.zero;
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                result[i, j] = M[i, j] * s;
        return result;
    }

    // Addition matrice + matrice
    public static Matrix4x4 Add(Matrix4x4 A, Matrix4x4 B)
    {
        Matrix4x4 result = Matrix4x4.zero;
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                result[i, j] = A[i, j] + B[i, j];
        return result;
    }

    // Multiplication matrice × matrice
    public static Matrix4x4 MultiplyMatrix4x4(Matrix4x4 A, Matrix4x4 B)
    {
        Matrix4x4 result = Matrix4x4.zero;
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                for (int k = 0; k < 4; k++)
                    result[i, j] += A[i, k] * B[k, j];
        return result;
    }
}
