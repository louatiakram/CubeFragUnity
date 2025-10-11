using UnityEngine;

public class RigidBody3DStateR 
{
    public Vector3 position, P, L;
    public Matrix4x4 R;
    public float mass;
    public Matrix4x4 Ibody, IbodyInv;

    public RigidBody3DStateR(float mass, Vector3 initPos, Vector3 initP, Vector3 initL,
                            Matrix4x4 Ibody)
    {
        this.mass = mass;
        position = initPos; P = initP; L = initL;
        R = Matrix4x4.identity;
        this.Ibody = Ibody;
        IbodyInv = Matrix4x4.zero;
        IbodyInv[0, 0] = 1f / Ibody[0, 0];
        IbodyInv[1, 1] = 1f / Ibody[1, 1];
        IbodyInv[2, 2] = 1f / Ibody[2, 2];
        IbodyInv[3, 3] = 1;
    }

    public void Integrate(Vector3 force, Vector3 torque, float dt)
    {
        // Linéaire
        P += force * dt;
        Vector3 v = P / mass;
        position += v * dt;

        // Angulaire
        L += torque * dt;
        Matrix4x4 worldInertiaInv = R * IbodyInv * R.transpose;
        Vector3 omega = Math3D.MultiplyMatrixVector3(worldInertiaInv, L);

        // Rotation update
        Matrix4x4 Omega = Matrix4x4.zero;
        Omega[0, 1] = -omega.z; Omega[0, 2] = omega.y;
        Omega[1, 0] = omega.z; Omega[1, 2] = -omega.x;
        Omega[2, 0] = -omega.y; Omega[2, 1] = omega.x;

        Matrix4x4 Rupd = Math3D.Add(Matrix4x4.identity, Math3D.MultiplyScalar(Omega, dt));
        R = Math3D.MultiplyMatrix4x4(Rupd, R);
        R = Math3D.GramSchmidt(R);
    }
}
