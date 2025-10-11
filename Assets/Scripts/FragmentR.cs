// FragmentT.cs
using UnityEngine;

public class FragmentR
{
    public RigidBody3DStateR state;
    public CubeObjectT meshData;
    public MeshFilter meshFilter;
    public Mesh mesh;

    public FragmentR(float mass, Vector3 pos, Matrix4x4 I, CubeObjectT meshData)
    {
        this.state = new RigidBody3DStateR(mass, pos, Vector3.zero, Vector3.zero, I);
        this.meshData = meshData;
    }

    public void UpdateMesh()
    {
        Vector3[] verts = new Vector3[meshData.vertices.Length];
        for (int i = 0; i < verts.Length; i++)
            verts[i] = Math3D.MultiplyMatrixVector3(state.R, meshData.vertices[i]) + state.position;

        mesh.Clear();
        mesh.vertices = verts;
        mesh.triangles = meshData.triangles;
        mesh.RecalculateNormals();
    }
}
