using UnityEngine;

public class CubeObjectI
{
    public Vector3[] vertices;
    public int[] triangles;
    public Color color;

    public CubeObjectI(float a, float b, float c, Color cubeColor)
    {
        color = cubeColor;
        vertices = new Vector3[8];
        float hx = a / 2, hy = b / 2, hz = c / 2;

        vertices[0] = new Vector3(-hx, -hy, -hz);
        vertices[1] = new Vector3(hx, -hy, -hz);
        vertices[2] = new Vector3(hx, hy, -hz);
        vertices[3] = new Vector3(-hx, hy, -hz);
        vertices[4] = new Vector3(-hx, -hy, hz);
        vertices[5] = new Vector3(hx, -hy, hz);
        vertices[6] = new Vector3(hx, hy, hz);
        vertices[7] = new Vector3(-hx, hy, hz);

        triangles = new int[]
        {
            0,2,1, 0,3,2,
            4,5,6, 4,6,7,
            0,1,5, 0,5,4,
            2,3,7, 2,7,6,
            1,2,6, 1,6,5,
            0,4,7, 0,7,3
        };
    }
}