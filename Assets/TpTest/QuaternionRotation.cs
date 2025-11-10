using UnityEngine;

public class QuaternionRotation
{
    public struct MyQuaternion
    {
        public float w, x, y, z;

        public MyQuaternion(float w, float x, float y, float z)
        {
            this.w = w;
            this.x = x;
            this.y = y;
            this.z = z;
        }

        // Quaternion identité
        public static MyQuaternion Identity => new MyQuaternion(1, 0, 0, 0);

        // Création depuis axe et angle
        public static MyQuaternion FromAxisAngle(MyVector3 axis, float angleDeg)
        {
            float angle = angleDeg * Mathf.Deg2Rad / 2f;
            MyVector3 normalized = axis.Normalized();
            return new MyQuaternion(
                Mathf.Cos(angle),
                normalized.x * Mathf.Sin(angle),
                normalized.y * Mathf.Sin(angle),
                normalized.z * Mathf.Sin(angle)
            );
        }

        // Multiplication de quaternions
        public static MyQuaternion Multiply(MyQuaternion q1, MyQuaternion q2)
        {
            return new MyQuaternion(
                q1.w * q2.w - q1.x * q2.x - q1.y * q2.y - q1.z * q2.z,
                q1.w * q2.x + q1.x * q2.w + q1.y * q2.z - q1.z * q2.y,
                q1.w * q2.y - q1.x * q2.z + q1.y * q2.w + q1.z * q2.x,
                q1.w * q2.z + q1.x * q2.y - q1.y * q2.x + q1.z * q2.w
            );
        }

        // Addition de quaternions
        public static MyQuaternion Add(MyQuaternion q1, MyQuaternion q2)
        {
            return new MyQuaternion(
                q1.w + q2.w,
                q1.x + q2.x,
                q1.y + q2.y,
                q1.z + q2.z
            );
        }

        // Multiplication par scalaire
        public static MyQuaternion Scale(MyQuaternion q, float s)
        {
            return new MyQuaternion(q.w * s, q.x * s, q.y * s, q.z * s);
        }

        // Norme du quaternion
        public float Magnitude()
        {
            return Mathf.Sqrt(w * w + x * x + y * y + z * z);
        }

        // Normalisation
        public MyQuaternion Normalized()
        {
            float mag = Magnitude();
            if (mag < 0.0001f) return Identity;
            return Scale(this, 1f / mag);
        }

        public void Normalize()
        {
            float mag = Magnitude();
            if (mag < 0.0001f)
            {
                w = 1;
                x = y = z = 0;
                return;
            }
            w /= mag;
            x /= mag;
            y /= mag;
            z /= mag;
        }

        // Conversion vers matrice 4x4
        public static float[,] ToMatrix(MyQuaternion q)
        {
            float[,] M = new float[4, 4];
            float xx = q.x * q.x, yy = q.y * q.y, zz = q.z * q.z;
            float xy = q.x * q.y, xz = q.x * q.z, yz = q.y * q.z;
            float wx = q.w * q.x, wy = q.w * q.y, wz = q.w * q.z;

            M[0, 0] = 1 - 2 * (yy + zz); M[0, 1] = 2 * (xy - wz);     M[0, 2] = 2 * (xz + wy);     M[0, 3] = 0;
            M[1, 0] = 2 * (xy + wz);     M[1, 1] = 1 - 2 * (xx + zz); M[1, 2] = 2 * (yz - wx);     M[1, 3] = 0;
            M[2, 0] = 2 * (xz - wy);     M[2, 1] = 2 * (yz + wx);     M[2, 2] = 1 - 2 * (xx + yy); M[2, 3] = 0;
            M[3, 0] = 0;                 M[3, 1] = 0;                 M[3, 2] = 0;                 M[3, 3] = 1;

            return M;
        }

        // Conversion vers matrice 3x3
        public Matrix3x3 ToMatrix3x3()
        {
            float xx = x * x, yy = y * y, zz = z * z;
            float xy = x * y, xz = x * z, yz = y * z;
            float wx = w * x, wy = w * y, wz = w * z;

            return new Matrix3x3(
                new Vector3(1 - 2 * (yy + zz), 2 * (xy - wz), 2 * (xz + wy)),
                new Vector3(2 * (xy + wz), 1 - 2 * (xx + zz), 2 * (yz - wx)),
                new Vector3(2 * (xz - wy), 2 * (yz + wx), 1 - 2 * (xx + yy))
            );
        }

        // Rotation d'un vecteur par ce quaternion
        public MyVector3 RotateVector(MyVector3 v)
        {
            // q * v * q^(-1)
            // Plus efficace: v' = v + 2 * cross(q.xyz, cross(q.xyz, v) + q.w * v)
            MyVector3 qvec = new MyVector3(x, y, z);
            MyVector3 cross1 = MyVector3.Cross(qvec, v);
            MyVector3 term = MyVector3.Add(cross1, MyVector3.Scale(v, w));
            MyVector3 cross2 = MyVector3.Cross(qvec, term);
            return MyVector3.Add(v, MyVector3.Scale(cross2, 2));
        }

        // Conversion depuis Unity Quaternion (pour interfacer avec Transform)
        public static MyQuaternion FromUnity(Quaternion q)
        {
            return new MyQuaternion(q.w, q.x, q.y, q.z);
        }

        // Conversion vers Unity Quaternion
        public Quaternion ToUnity()
        {
            return new Quaternion(x, y, z, w);
        }

        // Extraire les axes de rotation
        public MyVector3 Right()
        {
            return RotateVector(MyVector3.Right);
        }

        public MyVector3 Up()
        {
            return RotateVector(MyVector3.Up);
        }

        public MyVector3 Forward()
        {
            return RotateVector(MyVector3.Forward);
        }
    }
}
