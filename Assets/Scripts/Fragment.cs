// Fragment.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Fragment : MonoBehaviour
{
    public float mass = 1f;
    public Rigidbody rb { get; private set; }
    public Matrix4x4Manual localTransform; // computed manually

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = mass;
        // compute inertia tensor approx (for box or convex)
        // For prototype, rely on Unity inertia tensor but you can compute analytically for boxes/spheres.
    }

    public Vector3 WorldPointFromLocal(Vector3 localPoint)
    {
        // apply local transform manually
        var p = new Vector4(localPoint.x, localPoint.y, localPoint.z, 1f);
        var wp = Matrix4x4Manual.MulPoint(localTransform, p);
        return new Vector3(wp.x / wp.w, wp.y / wp.w, wp.z / wp.w);
    }
}
 