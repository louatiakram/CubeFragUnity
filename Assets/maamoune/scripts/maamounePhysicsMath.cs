using UnityEngine;
using System;

[System.Serializable]
public struct Vec3
{
    public float x, y, z;

    public Vec3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public static Vec3 operator +(Vec3 a, Vec3 b)
    {
        return new Vec3(a.x + b.x, a.y + b.y, a.z + b.z);
    }

    public static Vec3 operator -(Vec3 a, Vec3 b)
    {
        return new Vec3(a.x - b.x, a.y - b.y, a.z - b.z);
    }

    public static Vec3 operator *(Vec3 a, float s)
    {
        return new Vec3(a.x * s, a.y * s, a.z * s);
    }

    public static Vec3 operator *(float s, Vec3 a)
    {
        return a * s;
    }

    public static Vec3 operator /(Vec3 a, float s)
    {
        return new Vec3(a.x / s, a.y / s, a.z / s);
    }

    public float Magnitude()
    {
        return Mathf.Sqrt(x * x + y * y + z * z);
    }

    public Vec3 Normalized()
    {
        float mag = Magnitude();
        if (mag > 0.0001f)
            return this / mag;
        return new Vec3(0, 0, 0);
    }

    public static float Dot(Vec3 a, Vec3 b)
    {
        return a.x * b.x + a.y * b.y + a.z * b.z;
    }

    public static Vec3 Cross(Vec3 a, Vec3 b)
    {
        return new Vec3(
            a.y * b.z - a.z * b.y,
            a.z * b.x - a.x * b.z,
            a.x * b.y - a.y * b.x
        );
    }

    public static Vec3 Zero => new Vec3(0, 0, 0);
    public static Vec3 Up => new Vec3(0, 1, 0);
    public static Vec3 Down => new Vec3(0, -1, 0);

    public Vector3 ToUnityVec3()
    {
        return new Vector3(x, y, z);
    }

    public static Vec3 FromUnityVec3(Vector3 v)
    {
        return new Vec3(v.x, v.y, v.z);
    }

    public override string ToString()
    {
        return $"({x:F2}, {y:F2}, {z:F2})";
    }
}

[System.Serializable]
public struct Mat4
{
    public float[,] m; 

    public Mat4(bool identity)
    {
        m = new float[4, 4];
        if (identity)
        {
            for (int i = 0; i < 4; i++)
                m[i, i] = 1.0f;
        }
    }

    public static Mat4 Identity()
    {
        return new Mat4(true);
    }

    public static Mat4 Translate(Vec3 translation)
    {
        Mat4 mat = Identity();
        mat.m[0, 3] = translation.x;
        mat.m[1, 3] = translation.y;
        mat.m[2, 3] = translation.z;
        return mat;
    }

    public static Mat4 Rotate(Vec3 axis, float angleRadians)
    {
        Mat4 mat = Identity();
        Vec3 a = axis.Normalized();
        float c = Mathf.Cos(angleRadians);
        float s = Mathf.Sin(angleRadians);
        float t = 1.0f - c;

        mat.m[0, 0] = t * a.x * a.x + c;
        mat.m[0, 1] = t * a.x * a.y - s * a.z;
        mat.m[0, 2] = t * a.x * a.z + s * a.y;

        mat.m[1, 0] = t * a.x * a.y + s * a.z;
        mat.m[1, 1] = t * a.y * a.y + c;
        mat.m[1, 2] = t * a.y * a.z - s * a.x;

        mat.m[2, 0] = t * a.x * a.z - s * a.y;
        mat.m[2, 1] = t * a.y * a.z + s * a.x;
        mat.m[2, 2] = t * a.z * a.z + c;

        return mat;
    }

    public static Mat4 operator *(Mat4 a, Mat4 b)
    {
        Mat4 result = new Mat4(false);
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                result.m[i, j] = 0;
                for (int k = 0; k < 4; k++)
                {
                    result.m[i, j] += a.m[i, k] * b.m[k, j];
                }
            }
        }
        return result;
    }

    public Vec3 TransformPoint(Vec3 point)
    {
        float w = m[3, 0] * point.x + m[3, 1] * point.y + m[3, 2] * point.z + m[3, 3];
        return new Vec3(
            (m[0, 0] * point.x + m[0, 1] * point.y + m[0, 2] * point.z + m[0, 3]) / w,
            (m[1, 0] * point.x + m[1, 1] * point.y + m[1, 2] * point.z + m[1, 3]) / w,
            (m[2, 0] * point.x + m[2, 1] * point.y + m[2, 2] * point.z + m[2, 3]) / w
        );
    }
}
