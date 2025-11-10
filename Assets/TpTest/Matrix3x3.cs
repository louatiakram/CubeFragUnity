using UnityEngine;

public class Matrix3x3
{
    public float[,] m = new float[3, 3];

    public Matrix3x3(Vector3 row0, Vector3 row1, Vector3 row2)
    {
        m[0, 0] = row0.x; m[0, 1] = row0.y; m[0, 2] = row0.z;
        m[1, 0] = row1.x; m[1, 1] = row1.y; m[1, 2] = row1.z;
        m[2, 0] = row2.x; m[2, 1] = row2.y; m[2, 2] = row2.z;
    }

    public Vector3 MultiplyVector(Vector3 v)
    {
        return new Vector3(
            m[0, 0] * v.x + m[0, 1] * v.y + m[0, 2] * v.z,
            m[1, 0] * v.x + m[1, 1] * v.y + m[1, 2] * v.z,
            m[2, 0] * v.x + m[2, 1] * v.y + m[2, 2] * v.z
        );
    }

    public Vector3 InverseMultiplyVector(Vector3 v)
    {
        float det = Determinant();
        if (Mathf.Abs(det) < 0.0001f) return Vector3.zero;
        
        Matrix3x3 inv = Inverse();
        return inv.MultiplyVector(v);
    }

    private float Determinant()
    {
        return m[0, 0] * (m[1, 1] * m[2, 2] - m[1, 2] * m[2, 1])
             - m[0, 1] * (m[1, 0] * m[2, 2] - m[1, 2] * m[2, 0])
             + m[0, 2] * (m[1, 0] * m[2, 1] - m[1, 1] * m[2, 0]);
    }

    private Matrix3x3 Inverse()
    {
        float det = Determinant();
        float invDet = 1f / det;

        Matrix3x3 adj = new Matrix3x3(Vector3.zero, Vector3.zero, Vector3.zero);
        
        adj.m[0, 0] = (m[1, 1] * m[2, 2] - m[1, 2] * m[2, 1]) * invDet;
        adj.m[0, 1] = -(m[0, 1] * m[2, 2] - m[0, 2] * m[2, 1]) * invDet;
        adj.m[0, 2] = (m[0, 1] * m[1, 2] - m[0, 2] * m[1, 1]) * invDet;
        
        adj.m[1, 0] = -(m[1, 0] * m[2, 2] - m[1, 2] * m[2, 0]) * invDet;
        adj.m[1, 1] = (m[0, 0] * m[2, 2] - m[0, 2] * m[2, 0]) * invDet;
        adj.m[1, 2] = -(m[0, 0] * m[1, 2] - m[0, 2] * m[1, 0]) * invDet;
        
        adj.m[2, 0] = (m[1, 0] * m[2, 1] - m[1, 1] * m[2, 0]) * invDet;
        adj.m[2, 1] = -(m[0, 0] * m[2, 1] - m[0, 1] * m[2, 0]) * invDet;
        adj.m[2, 2] = (m[0, 0] * m[1, 1] - m[0, 1] * m[1, 0]) * invDet;
        
        return adj;
    }
}