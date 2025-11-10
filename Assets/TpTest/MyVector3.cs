using UnityEngine;

/// <summary>
/// Classe Vector3 personnalisée pour éviter d'utiliser les fonctionnalités Unity
/// Contient uniquement les opérations mathématiques de base
/// </summary>
public class MyVector3
{
    public float x, y, z;

    public MyVector3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public MyVector3() : this(0, 0, 0) { }

    // Constantes
    public static MyVector3 Zero => new MyVector3(0, 0, 0);
    public static MyVector3 One => new MyVector3(1, 1, 1);
    public static MyVector3 Up => new MyVector3(0, 1, 0);
    public static MyVector3 Down => new MyVector3(0, -1, 0);
    public static MyVector3 Left => new MyVector3(-1, 0, 0);
    public static MyVector3 Right => new MyVector3(1, 0, 0);
    public static MyVector3 Forward => new MyVector3(0, 0, 1);
    public static MyVector3 Back => new MyVector3(0, 0, -1);

    // Conversion depuis/vers Vector3 Unity (pour transform uniquement)
    public static MyVector3 FromUnity(Vector3 v)
    {
        return new MyVector3(v.x, v.y, v.z);
    }

    public Vector3 ToUnity()
    {
        return new Vector3(x, y, z);
    }

    // Opérations de base
    public static MyVector3 Add(MyVector3 a, MyVector3 b)
    {
        return new MyVector3(a.x + b.x, a.y + b.y, a.z + b.z);
    }

    public static MyVector3 Subtract(MyVector3 a, MyVector3 b)
    {
        return new MyVector3(a.x - b.x, a.y - b.y, a.z - b.z);
    }

    public static MyVector3 Scale(MyVector3 v, float s)
    {
        return new MyVector3(v.x * s, v.y * s, v.z * s);
    }

    public static MyVector3 Divide(MyVector3 v, float s)
    {
        if (Mathf.Abs(s) < 0.0001f) return Zero;
        return new MyVector3(v.x / s, v.y / s, v.z / s);
    }

    public static MyVector3 Negate(MyVector3 v)
    {
        return new MyVector3(-v.x, -v.y, -v.z);
    }

    // Produit scalaire
    public static float Dot(MyVector3 a, MyVector3 b)
    {
        return a.x * b.x + a.y * b.y + a.z * b.z;
    }

    // Produit vectoriel (cross product)
    public static MyVector3 Cross(MyVector3 a, MyVector3 b)
    {
        return new MyVector3(
            a.y * b.z - a.z * b.y,
            a.z * b.x - a.x * b.z,
            a.x * b.y - a.y * b.x
        );
    }

    // Magnitude (norme)
    public float Magnitude()
    {
        return Mathf.Sqrt(x * x + y * y + z * z);
    }

    public float SqrMagnitude()
    {
        return x * x + y * y + z * z;
    }

    // Normalisation
    public MyVector3 Normalized()
    {
        float mag = Magnitude();
        if (mag < 0.0001f) return Zero;
        return Divide(this, mag);
    }

    public void Normalize()
    {
        float mag = Magnitude();
        if (mag < 0.0001f)
        {
            x = y = z = 0;
            return;
        }
        x /= mag;
        y /= mag;
        z /= mag;
    }

    // Distance
    public static float Distance(MyVector3 a, MyVector3 b)
    {
        return Subtract(a, b).Magnitude();
    }

    public static float SqrDistance(MyVector3 a, MyVector3 b)
    {
        return Subtract(a, b).SqrMagnitude();
    }

    // Clamp composante par composante
    public static MyVector3 Clamp(MyVector3 v, MyVector3 min, MyVector3 max)
    {
        return new MyVector3(
            Mathf.Clamp(v.x, min.x, max.x),
            Mathf.Clamp(v.y, min.y, max.y),
            Mathf.Clamp(v.z, min.z, max.z)
        );
    }

    // Min/Max composante par composante
    public static MyVector3 Min(MyVector3 a, MyVector3 b)
    {
        return new MyVector3(
            Mathf.Min(a.x, b.x),
            Mathf.Min(a.y, b.y),
            Mathf.Min(a.z, b.z)
        );
    }

    public static MyVector3 Max(MyVector3 a, MyVector3 b)
    {
        return new MyVector3(
            Mathf.Max(a.x, b.x),
            Mathf.Max(a.y, b.y),
            Mathf.Max(a.z, b.z)
        );
    }

    // ToString pour debug
    public override string ToString()
    {
        return $"({x:F3}, {y:F3}, {z:F3})";
    }
}
