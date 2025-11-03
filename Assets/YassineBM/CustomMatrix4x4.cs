using UnityEngine;

namespace YassineBM
{
    /// <summary>
    /// Custom 4x4 Matrix implementation for manual transformations
    /// No Unity shortcuts allowed - pure matrix math
    /// </summary>
    public struct CustomMatrix4x4
    {
        public float m00, m01, m02, m03;
        public float m10, m11, m12, m13;
        public float m20, m21, m22, m23;
        public float m30, m31, m32, m33;

        public CustomMatrix4x4(float m00, float m01, float m02, float m03,
                               float m10, float m11, float m12, float m13,
                               float m20, float m21, float m22, float m23,
                               float m30, float m31, float m32, float m33)
        {
            this.m00 = m00; this.m01 = m01; this.m02 = m02; this.m03 = m03;
            this.m10 = m10; this.m11 = m11; this.m12 = m12; this.m13 = m13;
            this.m20 = m20; this.m21 = m21; this.m22 = m22; this.m23 = m23;
            this.m30 = m30; this.m31 = m31; this.m32 = m32; this.m33 = m33;
        }

        /// <summary>
        /// Creates an identity matrix
        /// </summary>
        public static CustomMatrix4x4 Identity()
        {
            return new CustomMatrix4x4(
                1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1
            );
        }

        /// <summary>
        /// Creates a translation matrix
        /// </summary>
        public static CustomMatrix4x4 Translation(float x, float y, float z)
        {
            return new CustomMatrix4x4(
                1, 0, 0, x,
                0, 1, 0, y,
                0, 0, 1, z,
                0, 0, 0, 1
            );
        }

        /// <summary>
        /// Creates a translation matrix from Vector3
        /// </summary>
        public static CustomMatrix4x4 Translation(Vector3 t)
        {
            return Translation(t.x, t.y, t.z);
        }

        /// <summary>
        /// Creates a rotation matrix using axis-angle representation (Rodrigues formula)
        /// </summary>
        public static CustomMatrix4x4 Rotation(Vector3 axis, float angleRadians)
        {
            axis = axis.normalized;
            float c = Mathf.Cos(angleRadians);
            float s = Mathf.Sin(angleRadians);
            float t = 1 - c;

            float x = axis.x;
            float y = axis.y;
            float z = axis.z;

            return new CustomMatrix4x4(
                t * x * x + c,     t * x * y - s * z, t * x * z + s * y, 0,
                t * x * y + s * z, t * y * y + c,     t * y * z - s * x, 0,
                t * x * z - s * y, t * y * z + s * x, t * z * z + c,     0,
                0,                 0,                 0,                 1
            );
        }

        /// <summary>
        /// Creates a rotation matrix around X axis
        /// </summary>
        public static CustomMatrix4x4 RotationX(float angleRadians)
        {
            float c = Mathf.Cos(angleRadians);
            float s = Mathf.Sin(angleRadians);
            return new CustomMatrix4x4(
                1, 0,  0, 0,
                0, c, -s, 0,
                0, s,  c, 0,
                0, 0,  0, 1
            );
        }

        /// <summary>
        /// Creates a rotation matrix around Y axis
        /// </summary>
        public static CustomMatrix4x4 RotationY(float angleRadians)
        {
            float c = Mathf.Cos(angleRadians);
            float s = Mathf.Sin(angleRadians);
            return new CustomMatrix4x4(
                 c, 0, s, 0,
                 0, 1, 0, 0,
                -s, 0, c, 0,
                 0, 0, 0, 1
            );
        }

        /// <summary>
        /// Creates a rotation matrix around Z axis
        /// </summary>
        public static CustomMatrix4x4 RotationZ(float angleRadians)
        {
            float c = Mathf.Cos(angleRadians);
            float s = Mathf.Sin(angleRadians);
            return new CustomMatrix4x4(
                c, -s, 0, 0,
                s,  c, 0, 0,
                0,  0, 1, 0,
                0,  0, 0, 1
            );
        }

        /// <summary>
        /// Creates a uniform scale matrix
        /// </summary>
        public static CustomMatrix4x4 Scale(float s)
        {
            return new CustomMatrix4x4(
                s, 0, 0, 0,
                0, s, 0, 0,
                0, 0, s, 0,
                0, 0, 0, 1
            );
        }

        /// <summary>
        /// Creates a non-uniform scale matrix
        /// </summary>
        public static CustomMatrix4x4 Scale(float sx, float sy, float sz)
        {
            return new CustomMatrix4x4(
                sx, 0,  0,  0,
                0,  sy, 0,  0,
                0,  0,  sz, 0,
                0,  0,  0,  1
            );
        }

        /// <summary>
        /// Matrix multiplication
        /// </summary>
        public static CustomMatrix4x4 Multiply(CustomMatrix4x4 a, CustomMatrix4x4 b)
        {
            CustomMatrix4x4 result = new CustomMatrix4x4();
            
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

        /// <summary>
        /// Transform a point (w=1)
        /// </summary>
        public Vector3 TransformPoint(Vector3 point)
        {
            float x = m00 * point.x + m01 * point.y + m02 * point.z + m03;
            float y = m10 * point.x + m11 * point.y + m12 * point.z + m13;
            float z = m20 * point.x + m21 * point.y + m22 * point.z + m23;
            float w = m30 * point.x + m31 * point.y + m32 * point.z + m33;

            if (Mathf.Abs(w) > 0.0001f)
            {
                return new Vector3(x / w, y / w, z / w);
            }
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Transform a direction (w=0)
        /// </summary>
        public Vector3 TransformDirection(Vector3 direction)
        {
            float x = m00 * direction.x + m01 * direction.y + m02 * direction.z;
            float y = m10 * direction.x + m11 * direction.y + m12 * direction.z;
            float z = m20 * direction.x + m21 * direction.y + m22 * direction.z;
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Extract translation from matrix
        /// </summary>
        public Vector3 GetTranslation()
        {
            return new Vector3(m03, m13, m23);
        }

        /// <summary>
        /// Operator overload for matrix multiplication
        /// </summary>
        public static CustomMatrix4x4 operator *(CustomMatrix4x4 a, CustomMatrix4x4 b)
        {
            return Multiply(a, b);
        }

        /// <summary>
        /// Convert to Unity's Matrix4x4 (for mesh manipulation only)
        /// </summary>
        public Matrix4x4 ToUnityMatrix()
        {
            Matrix4x4 m = new Matrix4x4();
            m.m00 = m00; m.m01 = m01; m.m02 = m02; m.m03 = m03;
            m.m10 = m10; m.m11 = m11; m.m12 = m12; m.m13 = m13;
            m.m20 = m20; m.m21 = m21; m.m22 = m22; m.m23 = m23;
            m.m30 = m30; m.m31 = m31; m.m32 = m32; m.m33 = m33;
            return m;
        }
    }
}

