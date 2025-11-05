using UnityEngine;

public static class MeshUtils
{
    // Crée un Mesh Unity à partir d'un CubeObject
    public static Mesh CreateMeshFromCubeObject(CubeObject cubeObj)
    {
        Mesh mesh = new Mesh();
        mesh.vertices = cubeObj.vertices;
        mesh.triangles = cubeObj.triangles;
        mesh.RecalculateNormals();
        return mesh;
    }
}
