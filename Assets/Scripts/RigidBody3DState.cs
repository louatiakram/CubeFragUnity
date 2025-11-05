using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RigidBody3DState : MonoBehaviour
{
    public CubeObject cube;
    public float a = 1, b = 1, c = 1, mass = 1f;
    public Vector3 position = Vector3.zero;
    public Vector3 P = Vector3.zero; // Linear momentum
    public Vector3 L = Vector3.zero; // Angular momentum
    public Matrix4x4Manual R = Matrix4x4Manual.Identity(); // Orientation matrix (Manual)
    public Matrix4x4Manual Ibody;
    public Matrix4x4Manual IbodyInv;
    public float dt = 0.02f;

    private Mesh mesh;

    void Start()
    {
        cube = new CubeObject(a, b, c, Color.green);

        float Ixx = (1f / 12f) * mass * (b * b + c * c);
        float Iyy = (1f / 12f) * mass * (a * a + c * c);
        float Izz = (1f / 12f) * mass * (a * a + b * b);

        Ibody = new Matrix4x4Manual();
        Ibody.m00 = Ixx; Ibody.m11 = Iyy; Ibody.m22 = Izz; Ibody.m33 = 1f;
        IbodyInv = new Matrix4x4Manual();
        IbodyInv.m00 = 1f / Ixx; IbodyInv.m11 = 1f / Iyy; IbodyInv.m22 = 1f / Izz; IbodyInv.m33 = 1f;

        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = cube.color;
        GetComponent<MeshRenderer>().material = mat;
    }

    void Update()
    {
        Vector3 F = Vector3.zero;
        Vector3 torque = Vector3.zero;

        if (Input.GetKey(KeyCode.A)) F += new Vector3(0, 0, 10);
        if (Input.GetKey(KeyCode.B))
        {
            Vector3 F2 = new Vector3(0, 10, 0);
            F += F2;
            Vector3 r2 = new Vector3(a / 2, 0, 0);
            torque += Vector3.Cross(r2, F2);
        }
        if (Input.GetKey(KeyCode.C))
        {
            Vector3 F3 = new Vector3(10, 0, 0);
            F += F3;
            Vector3 r3 = new Vector3(0, b / 2, 0);
            torque += Vector3.Cross(r3, F3);
        }

        // Update linear and angular momentum
        P += F * dt;
        L += torque * dt;

        Vector3 v = P / mass;

        // Compute world inertia inverse: R * IbodyInv * R^T
        Matrix4x4Manual RT = Transpose(R);
        Matrix4x4Manual temp = Matrix4x4Manual.Mul(R, IbodyInv);
        Matrix4x4Manual Iinv = Matrix4x4Manual.Mul(temp, RT);

        Vector3 omega = MultiplyMatrixVector3Manual(Iinv, L);

        position += v * dt;

        // Build antisymmetric Omega matrix for angular velocity omega
        Matrix4x4Manual Omega = new Matrix4x4Manual();
        Omega.m00 = 0; Omega.m01 = -omega.z; Omega.m02 = omega.y; Omega.m03 = 0;
        Omega.m10 = omega.z; Omega.m11 = 0; Omega.m12 = -omega.x; Omega.m13 = 0;
        Omega.m20 = -omega.y; Omega.m21 = omega.x; Omega.m22 = 0; Omega.m23 = 0;
        Omega.m30 = 0; Omega.m31 = 0; Omega.m32 = 0; Omega.m33 = 0;

        // Update rotation matrix: R = (I + Omega * dt) * R
        Matrix4x4Manual I = Matrix4x4Manual.Identity();
        Matrix4x4Manual OmegaDt = MultiplyScalar(Omega, dt);
        Matrix4x4Manual Rupdate = Matrix4x4Manual.Mul(Add(I, OmegaDt), R);
        R = GramSchmidt(Rupdate);

        // Update mesh vertices
        Vector3[] transformedVertices = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            transformedVertices[i] = MultiplyMatrixVector3Manual(R, cube.vertices[i]) + position;
        }

        mesh.Clear();
        mesh.vertices = transformedVertices;
        mesh.triangles = cube.triangles;
        mesh.RecalculateNormals();
    }

    // Helper method to transpose Matrix4x4Manual
    Matrix4x4Manual Transpose(Matrix4x4Manual M)
    {
        Matrix4x4Manual T = new Matrix4x4Manual();
        T.m00 = M.m00; T.m01 = M.m10; T.m02 = M.m20; T.m03 = M.m30;
        T.m10 = M.m01; T.m11 = M.m11; T.m12 = M.m21; T.m13 = M.m31;
        T.m20 = M.m02; T.m21 = M.m12; T.m22 = M.m22; T.m23 = M.m32;
        T.m30 = M.m03; T.m31 = M.m13; T.m32 = M.m23; T.m33 = M.m33;
        return T;
    }

    // Helper method: multiply Matrix4x4Manual by scalar
    Matrix4x4Manual MultiplyScalar(Matrix4x4Manual M, float s)
    {
        Matrix4x4Manual R = new Matrix4x4Manual();
        R.m00 = M.m00 * s; R.m01 = M.m01 * s; R.m02 = M.m02 * s; R.m03 = M.m03 * s;
        R.m10 = M.m10 * s; R.m11 = M.m11 * s; R.m12 = M.m12 * s; R.m13 = M.m13 * s;
        R.m20 = M.m20 * s; R.m21 = M.m21 * s; R.m22 = M.m22 * s; R.m23 = M.m23 * s;
        R.m30 = M.m30 * s; R.m31 = M.m31 * s; R.m32 = M.m32 * s; R.m33 = M.m33 * s;
        return R;
    }

    // Helper method: add two Matrix4x4Manual
    Matrix4x4Manual Add(Matrix4x4Manual A, Matrix4x4Manual B)
    {
        Matrix4x4Manual R = new Matrix4x4Manual();
        R.m00 = A.m00 + B.m00; R.m01 = A.m01 + B.m01; R.m02 = A.m02 + B.m02; R.m03 = A.m03 + B.m03;
        R.m10 = A.m10 + B.m10; R.m11 = A.m11 + B.m11; R.m12 = A.m12 + B.m12; R.m13 = A.m13 + B.m13;
        R.m20 = A.m20 + B.m20; R.m21 = A.m21 + B.m21; R.m22 = A.m22 + B.m22; R.m23 = A.m23 + B.m23;
        R.m30 = A.m30 + B.m30; R.m31 = A.m31 + B.m31; R.m32 = A.m32 + B.m32; R.m33 = A.m33 + B.m33;
        return R;
    }

    // Helper method: multiply Matrix4x4Manual by Vector3 (ignore translation)
    Vector3 MultiplyMatrixVector3Manual(Matrix4x4Manual M, Vector3 v)
    {
        float x = M.m00 * v.x + M.m01 * v.y + M.m02 * v.z;
        float y = M.m10 * v.x + M.m11 * v.y + M.m12 * v.z;
        float z = M.m20 * v.x + M.m21 * v.y + M.m22 * v.z;
        return new Vector3(x, y, z);
    }

    // Gram-Schmidt orthonormalization for rotation matrix (3x3 part only)
    Matrix4x4Manual GramSchmidt(Matrix4x4Manual M)
    {
        Vector3 x = new Vector3(M.m00, M.m10, M.m20);
        Vector3 y = new Vector3(M.m01, M.m11, M.m21);
        Vector3 z = new Vector3(M.m02, M.m12, M.m22);

        x = x.normalized;
        y = (y - Vector3.Dot(y, x) * x).normalized;
        z = Vector3.Cross(x, y);

        Matrix4x4Manual result = Matrix4x4Manual.Identity();
        result.m00 = x.x; result.m10 = x.y; result.m20 = x.z;
        result.m01 = y.x; result.m11 = y.y; result.m21 = y.z;
        result.m02 = z.x; result.m12 = z.y; result.m22 = z.z;

        return result;
    }

    public RigidBodyState GetState()
    {
        RigidBodyState state;
        state.position = position;
        // Convert your manual matrix to Unity's for external use
        state.R = ToUnityMatrix(R);
        state.P = P;
        state.L = L;
        return state;
    }

    // Convert Matrix4x4Manual to Unity Matrix4x4
    Matrix4x4 ToUnityMatrix(Matrix4x4Manual M)
    {
        Matrix4x4 u = new Matrix4x4();
        u.m00 = M.m00; u.m01 = M.m01; u.m02 = M.m02; u.m03 = M.m03;
        u.m10 = M.m10; u.m11 = M.m11; u.m12 = M.m12; u.m13 = M.m13;
        u.m20 = M.m20; u.m21 = M.m21; u.m22 = M.m22; u.m23 = M.m23;
        u.m30 = M.m30; u.m31 = M.m31; u.m32 = M.m32; u.m33 = M.m33;
        return u;
    }
}

public struct RigidBodyState
{
    public Vector3 position;
    public Matrix4x4 R;
    public Vector3 P;
    public Vector3 L;
}
