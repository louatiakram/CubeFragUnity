// Matrix4x4Manual.cs
using UnityEngine;

public struct Matrix4x4Manual
{
    public float m00, m01, m02, m03;
    public float m10, m11, m12, m13;
    public float m20, m21, m22, m23;
    public float m30, m31, m32, m33;

    public static Matrix4x4Manual Identity()
    {
        var M = new Matrix4x4Manual();
        M.m00 = M.m11 = M.m22 = M.m33 = 1f;
        return M;
    }

    public static Matrix4x4Manual Translate(Vector3 t)
    {
        var M = Identity();
        M.m03 = t.x; M.m13 = t.y; M.m23 = t.z;
        return M;
    }

    public static Matrix4x4Manual RotateAxisAngle(Vector3 axis, float angleRad)
    {
        axis = axis.normalized;
        float c = Mathf.Cos(angleRad), s = Mathf.Sin(angleRad);
        float x = axis.x, y = axis.y, z = axis.z;
        // Rodrigues rotation
        var M = Identity();
        M.m00 = c + (1 - c) * x * x;
        M.m01 = (1 - c) * x * y - s * z;
        M.m02 = (1 - c) * x * z + s * y;
        M.m10 = (1 - c) * y * x + s * z;
        M.m11 = c + (1 - c) * y * y;
        M.m12 = (1 - c) * y * z - s * x;
        M.m20 = (1 - c) * z * x - s * y;
        M.m21 = (1 - c) * z * y + s * x;
        M.m22 = c + (1 - c) * z * z;
        return M;
    }

    public static Vector4 MulPoint(Matrix4x4Manual A, Vector4 p)
    {
        return new Vector4(
            A.m00 * p.x + A.m01 * p.y + A.m02 * p.z + A.m03 * p.w,
            A.m10 * p.x + A.m11 * p.y + A.m12 * p.z + A.m13 * p.w,
            A.m20 * p.x + A.m21 * p.y + A.m22 * p.z + A.m23 * p.w,
            A.m30 * p.x + A.m31 * p.y + A.m32 * p.z + A.m33 * p.w
        );
    }

    public static Matrix4x4Manual Mul(Matrix4x4Manual A, Matrix4x4Manual B)
    {
        var R = new Matrix4x4Manual();
        float[] a = { A.m00, A.m01, A.m02, A.m03, A.m10, A.m11, A.m12, A.m13, A.m20, A.m21, A.m22, A.m23, A.m30, A.m31, A.m32, A.m33 };
        float[] b = { B.m00, B.m01, B.m02, B.m03, B.m10, B.m11, B.m12, B.m13, B.m20, B.m21, B.m22, B.m23, B.m30, B.m31, B.m32, B.m33 };
        for (int r = 0; r < 4; r++) for (int c = 0; c < 4; c++)
            {
                float s = 0;
                for (int k = 0; k < 4; k++) s += a[r * 4 + k] * b[k * 4 + c];
                // assign to R
                switch (r * 4 + c)
                {
                    case 0: R.m00 = s; break;
                    case 1: R.m01 = s; break;
                    case 2: R.m02 = s; break;
                    case 3: R.m03 = s; break;
                    case 4: R.m10 = s; break;
                    case 5: R.m11 = s; break;
                    case 6: R.m12 = s; break;
                    case 7: R.m13 = s; break;
                    case 8: R.m20 = s; break;
                    case 9: R.m21 = s; break;
                    case 10: R.m22 = s; break;
                    case 11: R.m23 = s; break;
                    case 12: R.m30 = s; break;
                    case 13: R.m31 = s; break;
                    case 14: R.m32 = s; break;
                    case 15: R.m33 = s; break;
                }
            }
        return R;
    }

    public static Matrix4x4Manual Add(Matrix4x4Manual A, Matrix4x4Manual B)
    {
        Matrix4x4Manual R = new Matrix4x4Manual();
        R.m00 = A.m00 + B.m00; R.m01 = A.m01 + B.m01; R.m02 = A.m02 + B.m02; R.m03 = A.m03 + B.m03;
        R.m10 = A.m10 + B.m10; R.m11 = A.m11 + B.m11; R.m12 = A.m12 + B.m12; R.m13 = A.m13 + B.m13;
        R.m20 = A.m20 + B.m20; R.m21 = A.m21 + B.m21; R.m22 = A.m22 + B.m22; R.m23 = A.m23 + B.m23;
        R.m30 = A.m30 + B.m30; R.m31 = A.m31 + B.m31; R.m32 = A.m32 + B.m32; R.m33 = A.m33 + B.m33;
        return R;
    }

    public static Matrix4x4Manual MulScalar(Matrix4x4Manual M, float s)
    {
        Matrix4x4Manual R = new Matrix4x4Manual();
        R.m00 = M.m00 * s; R.m01 = M.m01 * s; R.m02 = M.m02 * s; R.m03 = M.m03 * s;
        R.m10 = M.m10 * s; R.m11 = M.m11 * s; R.m12 = M.m12 * s; R.m13 = M.m13 * s;
        R.m20 = M.m20 * s; R.m21 = M.m21 * s; R.m22 = M.m22 * s; R.m23 = M.m23 * s;
        R.m30 = M.m30 * s; R.m31 = M.m31 * s; R.m32 = M.m32 * s; R.m33 = M.m33 * s;
        return R;
    }

    public static Matrix4x4Manual GramSchmidt(Matrix4x4Manual M)
    {
        Vector3 x = new Vector3(M.m00, M.m10, M.m20);
        Vector3 y = new Vector3(M.m01, M.m11, M.m21);
        Vector3 z = new Vector3(M.m02, M.m12, M.m22);

        x = x.normalized;
        y = (y - Vector3.Dot(y, x) * x).normalized;
        z = Vector3.Cross(x, y);

        Matrix4x4Manual result = Identity();
        result.m00 = x.x; result.m10 = x.y; result.m20 = x.z;
        result.m01 = y.x; result.m11 = y.y; result.m21 = y.z;
        result.m02 = z.x; result.m12 = z.y; result.m22 = z.z;

        return result;
    }
    // Convert to Unity's position / quaternion (extract translation + rotation)
    public Vector3 ExtractTranslation() => new Vector3(m03, m13, m23);
    public Quaternion ExtractRotation()
    {
        // Extract rotation from 3x3 using Gram-Schmidt or Unity's Matrix4x4 conversion.
        Matrix4x4 u = new Matrix4x4();
        u.m00 = m00; u.m01 = m01; u.m02 = m02; u.m03 = m03;
        u.m10 = m10; u.m11 = m11; u.m12 = m12; u.m13 = m13;
        u.m20 = m20; u.m21 = m21; u.m22 = m22; u.m23 = m23;
        u.m30 = m30; u.m31 = m31; u.m32 = m32; u.m33 = m33;
        return u.rotation; // use Unity's conversion here (only for constructing quaternion)
    }
}
