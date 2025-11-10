using UnityEngine;
using System.Collections.Generic;

public class OBBCollisionInfo
{
    public bool isColliding;
    public MyVector3 normal;
    public float penetrationDepth;
    public List<MyVector3> contactPoints = new List<MyVector3>();
}

public static class OBBCollision
{
    public static OBBCollisionInfo CheckOBBCollision(
        Transform transformA, MyVector3 sizeA,
        Transform transformB, MyVector3 sizeB)
    {
        OBBCollisionInfo info = new OBBCollisionInfo();

        // Obtenir les axes locaux des OBB depuis leurs rotations
        MyVector3[] axesA = GetOBBAxes(transformA);
        MyVector3[] axesB = GetOBBAxes(transformB);

        float minOverlap = float.MaxValue;
        MyVector3 bestAxis = MyVector3.Zero;

        // Test axes de face de A (3 axes)
        for (int i = 0; i < 3; i++)
        {
            if (!TestAxis(axesA[i], transformA, sizeA, transformB, sizeB, 
                         ref minOverlap, ref bestAxis))
            {
                info.isColliding = false;
                return info;
            }
        }

        // Test axes de face de B (3 axes)
        for (int i = 0; i < 3; i++)
        {
            if (!TestAxis(axesB[i], transformA, sizeA, transformB, sizeB, 
                         ref minOverlap, ref bestAxis))
            {
                info.isColliding = false;
                return info;
            }
        }

        // Test axes arête-arête (9 axes)
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                MyVector3 axis = MyVector3.Cross(axesA[i], axesB[j]);
                if (axis.SqrMagnitude() < 0.0001f) continue; // Arêtes parallèles

                axis.Normalize();
                if (!TestAxis(axis, transformA, sizeA, transformB, sizeB, 
                             ref minOverlap, ref bestAxis))
                {
                    info.isColliding = false;
                    return info;
                }
            }
        }

        // Collision détectée
        info.isColliding = true;
        info.penetrationDepth = minOverlap;

        // S'assurer que la normale pointe de A vers B
        MyVector3 posA = MyVector3.FromUnity(transformA.position);
        MyVector3 posB = MyVector3.FromUnity(transformB.position);
        MyVector3 centerToCenter = MyVector3.Subtract(posB, posA);

        if (MyVector3.Dot(bestAxis, centerToCenter) < 0)
            bestAxis = MyVector3.Negate(bestAxis);

        info.normal = bestAxis;

        // Génération des points de contact
        info.contactPoints = GenerateContactPoints(
            transformA, sizeA, transformB, sizeB, bestAxis);

        return info;
    }

    private static MyVector3[] GetOBBAxes(Transform transform)
    {
        // Obtenir les axes locaux depuis la rotation du transform
        return new MyVector3[]
        {
            MyVector3.FromUnity(transform.right),   // Axe X
            MyVector3.FromUnity(transform.up),      // Axe Y
            MyVector3.FromUnity(transform.forward)  // Axe Z
        };
    }

    private static bool TestAxis(
        MyVector3 axis, 
        Transform transformA, MyVector3 sizeA,
        Transform transformB, MyVector3 sizeB,
        ref float minOverlap, ref MyVector3 bestAxis)
    {
        // Projeter les deux OBB sur l'axe
        float minA, maxA, minB, maxB;
        ProjectOBB(transformA, sizeA, axis, out minA, out maxA);
        ProjectOBB(transformB, sizeB, axis, out minB, out maxB);

        // Vérifier le chevauchement
        float overlap = Mathf.Min(maxA, maxB) - Mathf.Max(minA, minB);

        if (overlap < 0)
            return false; // Axe séparateur trouvé

        // Suivre le chevauchement minimum
        if (overlap < minOverlap)
        {
            minOverlap = overlap;
            bestAxis = axis;
        }

        return true;
    }

    private static void ProjectOBB(
        Transform transform, MyVector3 size, MyVector3 axis,
        out float min, out float max)
    {
        // Obtenir les 8 vertices de l'OBB
        MyVector3[] vertices = GetOBBVertices(transform, size);

        // Projeter tous les vertices sur l'axe
        min = float.MaxValue;
        max = float.MinValue;

        foreach (MyVector3 vertex in vertices)
        {
            float projection = MyVector3.Dot(vertex, axis);
            min = Mathf.Min(min, projection);
            max = Mathf.Max(max, projection);
        }
    }

    public static MyVector3[] GetOBBVertices(Transform transform, MyVector3 size)
    {
        MyVector3[] localVertices = new MyVector3[8];
        MyVector3 halfSize = MyVector3.Scale(size, 0.5f);

        // Générer les 8 coins du cube en espace local
        for (int i = 0; i < 8; i++)
        {
            localVertices[i] = new MyVector3(
                (i & 1) == 0 ? -halfSize.x : halfSize.x,
                (i & 2) == 0 ? -halfSize.y : halfSize.y,
                (i & 4) == 0 ? -halfSize.z : halfSize.z
            );
        }

        // Transformer en espace monde
        MyVector3[] worldVertices = new MyVector3[8];
        for (int i = 0; i < 8; i++)
        {
            Vector3 localUnity = localVertices[i].ToUnity();
            Vector3 worldUnity = transform.TransformPoint(localUnity);
            worldVertices[i] = MyVector3.FromUnity(worldUnity);
        }

        return worldVertices;
    }

    private static List<MyVector3> GenerateContactPoints(
        Transform transformA, MyVector3 sizeA,
        Transform transformB, MyVector3 sizeB,
        MyVector3 normal)
    {
        List<MyVector3> contactPoints = new List<MyVector3>();

        // Obtenir vertices
        MyVector3[] verticesA = GetOBBVertices(transformA, sizeA);
        MyVector3[] verticesB = GetOBBVertices(transformB, sizeB);

        // Trouver les vertices de B qui sont à l'intérieur de A
        foreach (MyVector3 vertex in verticesB)
        {
            if (IsPointInsideOBB(vertex, transformA, sizeA))
            {
                contactPoints.Add(vertex);
            }
        }

        // Trouver les vertices de A qui sont à l'intérieur de B
        foreach (MyVector3 vertex in verticesA)
        {
            if (IsPointInsideOBB(vertex, transformB, sizeB))
            {
                contactPoints.Add(vertex);
            }
        }

        // Si pas de vertices à l'intérieur (contact arête-arête), utiliser le centre
        if (contactPoints.Count == 0)
        {
            MyVector3 posA = MyVector3.FromUnity(transformA.position);
            MyVector3 posB = MyVector3.FromUnity(transformB.position);
            MyVector3 center = MyVector3.Scale(MyVector3.Add(posA, posB), 0.5f);
            contactPoints.Add(center);
        }

        return contactPoints;
    }

    private static bool IsPointInsideOBB(MyVector3 point, Transform transform, MyVector3 size)
    {
        // Transformer le point en espace local
        Vector3 pointUnity = point.ToUnity();
        Vector3 localPointUnity = transform.InverseTransformPoint(pointUnity);
        MyVector3 localPoint = MyVector3.FromUnity(localPointUnity);

        MyVector3 halfSize = MyVector3.Scale(size, 0.5f);

        return Mathf.Abs(localPoint.x) <= halfSize.x &&
               Mathf.Abs(localPoint.y) <= halfSize.y &&
               Mathf.Abs(localPoint.z) <= halfSize.z;
    }
}
