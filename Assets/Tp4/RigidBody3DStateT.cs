using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RigidBody3DStateT : MonoBehaviour
{
    public CubeObjectT cube;
    public float a = 1, b = 1, c = 1, mass = 1f;
    public Vector3 position = Vector3.zero;
    public Vector3 P = Vector3.zero;
    public Vector3 L = Vector3.zero;
    public Matrix4x4 R = Matrix4x4.identity;
    public Matrix4x4 Ibody;
    public Matrix4x4 IbodyInv;
    public float dt = 0.02f;

    private Mesh mesh;

    void Start()
    {
        cube = new CubeObjectT(a, b, c, Color.green);

        float Ixx = (1f / 12f) * mass * (b * b + c * c);
        float Iyy = (1f / 12f) * mass * (a * a + c * c);
        float Izz = (1f / 12f) * mass * (a * a + b * b);

        Ibody = Matrix4x4.zero;
        Ibody[0, 0] = Ixx; Ibody[1, 1] = Iyy; Ibody[2, 2] = Izz; Ibody[3, 3] = 1;
        IbodyInv = Matrix4x4.zero;
        IbodyInv[0, 0] = 1 / Ixx; IbodyInv[1, 1] = 1 / Iyy; IbodyInv[2, 2] = 1 / Izz; IbodyInv[3, 3] = 1;

        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        GetComponent<MeshRenderer>().material.color = cube.color;
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

        P += F * dt;
        L += torque * dt;

        Vector3 v = P / mass;
        Vector3 omega = Math3D.MultiplyMatrixVector3(R * IbodyInv * R.transpose, L);

        position += v * dt;

        // Matrice Omega
        Matrix4x4 Omega = Matrix4x4.zero;
        Omega[0, 1] = -omega.z; Omega[0, 2] = omega.y;
        Omega[1, 0] = omega.z; Omega[1, 2] = -omega.x;
        Omega[2, 0] = -omega.y; Omega[2, 1] = omega.x;

        // Update rotation
        Matrix4x4 Rupdate = Math3D.Add(Matrix4x4.identity, Math3D.MultiplyScalar(Omega, dt));
        R = Math3D.MultiplyMatrix4x4(Rupdate, R);

        R = Math3D.GramSchmidt(R);

        // Mise à jour des vertices
        Vector3[] transformedVertices = new Vector3[8];
        for (int i = 0; i < 8; i++)
            transformedVertices[i] = Math3D.MultiplyMatrixVector3(R, cube.vertices[i]) + position;

        mesh.Clear();
        mesh.vertices = transformedVertices;
        mesh.triangles = cube.triangles;
        mesh.RecalculateNormals();
    }

    public RigidBodyState GetState()
    {
        RigidBodyState state;
        state.position = position;
        state.R = R;
        state.P = P;
        state.L = L;
        return state;
    }
}

public struct RigidBodyState
{
    public Vector3 position;
    public Matrix4x4 R;
    public Vector3 P;
    public Vector3 L;
}
