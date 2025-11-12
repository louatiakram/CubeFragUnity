using UnityEngine;

namespace YassineBM
{
    /// <summary>
    /// Custom 4x4 matrix implementation for transformations
    /// </summary>
    public struct Matrix4x4Custom
    {
        // Matrix data stored in row-major order
        public float m00, m01, m02, m03;
        public float m10, m11, m12, m13;
        public float m20, m21, m22, m23;
        public float m30, m31, m32, m33;

        public Matrix4x4Custom(
            float m00, float m01, float m02, float m03,
            float m10, float m11, float m12, float m13,
            float m20, float m21, float m22, float m23,
            float m30, float m31, float m32, float m33)
        {
            this.m00 = m00; this.m01 = m01; this.m02 = m02; this.m03 = m03;
            this.m10 = m10; this.m11 = m11; this.m12 = m12; this.m13 = m13;
            this.m20 = m20; this.m21 = m21; this.m22 = m22; this.m23 = m23;
            this.m30 = m30; this.m31 = m31; this.m32 = m32; this.m33 = m33;
        }

        public static Matrix4x4Custom Identity()
        {
            return new Matrix4x4Custom(
                1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1
            );
        }

        public static Matrix4x4Custom Translation(Vector3 translation)
        {
            return new Matrix4x4Custom(
                1, 0, 0, translation.x,
                0, 1, 0, translation.y,
                0, 0, 1, translation.z,
                0, 0, 0, 1
            );
        }

        public static Matrix4x4Custom RotationX(float angleRadians)
        {
            float cos = Mathf.Cos(angleRadians);
            float sin = Mathf.Sin(angleRadians);
            return new Matrix4x4Custom(
                1, 0, 0, 0,
                0, cos, -sin, 0,
                0, sin, cos, 0,
                0, 0, 0, 1
            );
        }

        public static Matrix4x4Custom RotationY(float angleRadians)
        {
            float cos = Mathf.Cos(angleRadians);
            float sin = Mathf.Sin(angleRadians);
            return new Matrix4x4Custom(
                cos, 0, sin, 0,
                0, 1, 0, 0,
                -sin, 0, cos, 0,
                0, 0, 0, 1
            );
        }

        public static Matrix4x4Custom RotationZ(float angleRadians)
        {
            float cos = Mathf.Cos(angleRadians);
            float sin = Mathf.Sin(angleRadians);
            return new Matrix4x4Custom(
                cos, -sin, 0, 0,
                sin, cos, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1
            );
        }

        public static Matrix4x4Custom Scale(Vector3 scale)
        {
            return new Matrix4x4Custom(
                scale.x, 0, 0, 0,
                0, scale.y, 0, 0,
                0, 0, scale.z, 0,
                0, 0, 0, 1
            );
        }

        // Matrix multiplication
        public static Matrix4x4Custom operator *(Matrix4x4Custom a, Matrix4x4Custom b)
        {
            Matrix4x4Custom result = new Matrix4x4Custom();
            result.m00 = a.m00 * b.m00 + a.m01 * b.m10 + a.m02 * b.m20 + a.m03 * b.m30;
            result.m01 = a.m00 * b.m01 + a.m01 * b.m11 + a.m02 * b.m21 + a.m03 * b.m31;
            result.m02 = a.m00 * b.m02 + a.m01 * b.m12 + a.m02 * b.m22 + a.m03 * b.m32;
            result.m03 = a.m00 * b.m03 + a.m01 * b.m13 + a.m02 * b.m23 + a.m03 * b.m33;

            result.m10 = a.m10 * b.m00 + a.m11 * b.m10 + a.m12 * b.m20 + a.m13 * b.m30;
            result.m11 = a.m10 * b.m01 + a.m11 * b.m11 + a.m12 * b.m21 + a.m13 * b.m31;
            result.m12 = a.m10 * b.m02 + a.m11 * b.m12 + a.m12 * b.m22 + a.m13 * b.m32;
            result.m13 = a.m10 * b.m03 + a.m11 * b.m13 + a.m12 * b.m23 + a.m13 * b.m33;

            result.m20 = a.m20 * b.m00 + a.m21 * b.m10 + a.m22 * b.m20 + a.m23 * b.m30;
            result.m21 = a.m20 * b.m01 + a.m21 * b.m11 + a.m22 * b.m21 + a.m23 * b.m31;
            result.m22 = a.m20 * b.m02 + a.m21 * b.m12 + a.m22 * b.m22 + a.m23 * b.m32;
            result.m23 = a.m20 * b.m03 + a.m21 * b.m13 + a.m22 * b.m23 + a.m23 * b.m33;

            result.m30 = a.m30 * b.m00 + a.m31 * b.m10 + a.m32 * b.m20 + a.m33 * b.m30;
            result.m31 = a.m30 * b.m01 + a.m31 * b.m11 + a.m32 * b.m21 + a.m33 * b.m31;
            result.m32 = a.m30 * b.m02 + a.m31 * b.m12 + a.m32 * b.m22 + a.m33 * b.m32;
            result.m33 = a.m30 * b.m03 + a.m31 * b.m13 + a.m32 * b.m23 + a.m33 * b.m33;

            return result;
        }

        // Transform a point
        public Vector3 MultiplyPoint(Vector3 point)
        {
            float x = m00 * point.x + m01 * point.y + m02 * point.z + m03;
            float y = m10 * point.x + m11 * point.y + m12 * point.z + m13;
            float z = m20 * point.x + m21 * point.y + m22 * point.z + m23;
            return new Vector3(x, y, z);
        }

        // Transform a vector (no translation)
        public Vector3 MultiplyVector(Vector3 vector)
        {
            float x = m00 * vector.x + m01 * vector.y + m02 * vector.z;
            float y = m10 * vector.x + m11 * vector.y + m12 * vector.z;
            float z = m20 * vector.x + m21 * vector.y + m22 * vector.z;
            return new Vector3(x, y, z);
        }

        // Extract position from matrix
        public Vector3 GetPosition()
        {
            return new Vector3(m03, m13, m23);
        }

        // Extract rotation (simplified - assumes no scale/shear)
        public Quaternion GetRotation()
        {
            // Convert rotation matrix to quaternion
            float trace = m00 + m11 + m22;
            Quaternion q = new Quaternion();

            if (trace > 0)
            {
                float s = 0.5f / Mathf.Sqrt(trace + 1.0f);
                q.w = 0.25f / s;
                q.x = (m21 - m12) * s;
                q.y = (m02 - m20) * s;
                q.z = (m10 - m01) * s;
            }
            else
            {
                if (m00 > m11 && m00 > m22)
                {
                    float s = 2.0f * Mathf.Sqrt(1.0f + m00 - m11 - m22);
                    q.w = (m21 - m12) / s;
                    q.x = 0.25f * s;
                    q.y = (m01 + m10) / s;
                    q.z = (m02 + m20) / s;
                }
                else if (m11 > m22)
                {
                    float s = 2.0f * Mathf.Sqrt(1.0f + m11 - m00 - m22);
                    q.w = (m02 - m20) / s;
                    q.x = (m01 + m10) / s;
                    q.y = 0.25f * s;
                    q.z = (m12 + m21) / s;
                }
                else
                {
                    float s = 2.0f * Mathf.Sqrt(1.0f + m22 - m00 - m11);
                    q.w = (m10 - m01) / s;
                    q.x = (m02 + m20) / s;
                    q.y = (m12 + m21) / s;
                    q.z = 0.25f * s;
                }
            }

            return q;
        }
    }
}

