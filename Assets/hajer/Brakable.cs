using System.Collections.Generic;
using UnityEngine;

public class Brakable : MonoBehaviour
{
    [Header("Fragments")]
    [SerializeField] private GameObject mug;
    [SerializeField] private GameObject brokenMug;
    [SerializeField] private List<Transform> fragmentTransforms;

    [Header("Constraint Settings")]
    [SerializeField] private float stiffness = 100f;
    [SerializeField] private float breakThreshold = 0.5f;
    [SerializeField] private float mass = 1f;
    [SerializeField] private Vector3 initialVelocity = Vector3.zero;

    private BoxCollider boxCollider;

    private class FragmentData
    {
        public Transform transform;
        public Vector3 velocity;
        public List<FragmentData> connections = new List<FragmentData>();
        public Dictionary<FragmentData, float> restLengths = new Dictionary<FragmentData, float>();
        public bool physicsEnabled = false;
        public float mass;

        public FragmentData(Transform t, float m)
        {
            transform = t;
            mass = m;
            velocity = Vector3.zero;
        }

        public void InitConnections(List<FragmentData> others)
        {
            foreach (var other in others)
            {
                if (other == this) continue;
                float dist = Vector3.Distance(transform.position, other.transform.position);
                connections.Add(other);
                restLengths[other] = dist;
            }
        }

        public void EnablePhysics(Vector3 initialVel)
        {
            physicsEnabled = true;
            velocity = initialVel;
        }

        public void UpdateConstraints(float stiffness, float breakThreshold)
        {
            if (!physicsEnabled) return;

            List<FragmentData> brokenLinks = new List<FragmentData>();

            foreach (var other in connections)
            {
                float restLength = restLengths[other];
                Vector3 dir = other.transform.position - transform.position;
                float currentLength = dir.magnitude;
                float x = currentLength - restLength;

                if (Mathf.Abs(x) > breakThreshold)
                {
                    float energy = 0.5f * stiffness * x * x;
                    float deltaV = Mathf.Sqrt((2f * energy) / mass);
                    velocity += dir.normalized * deltaV;
                    brokenLinks.Add(other);
                }
            }

            foreach (var broken in brokenLinks)
                connections.Remove(broken);
        }

        public void ApplyMatrixTransform()
        {
            if (!physicsEnabled) return;

            Matrix4x4 translation = Matrix4x4.Translate(velocity * Time.deltaTime);
            Vector3 newPos = translation.MultiplyPoint3x4(transform.position);
            transform.position = newPos;
        }
    }

    private List<FragmentData> fragments = new List<FragmentData>();

    private void Start()
    {
        boxCollider = GetComponent<BoxCollider>();
        mug.SetActive(true);
        brokenMug.SetActive(false);

        fragments.Clear();
        foreach (var t in fragmentTransforms)
        {
            var frag = new FragmentData(t, mass);
            fragments.Add(frag);
        }

        foreach (var frag in fragments)
        {
            frag.InitConnections(fragments);
        }
    }

    [ContextMenu("Break")]
    public void Break()
    {
        mug.SetActive(false);
        brokenMug.SetActive(true);
        boxCollider.enabled = false;

        foreach (var frag in fragments)
        {
            frag.EnablePhysics(initialVelocity);
        }
    }

    private void Update()
    {
        foreach (var frag in fragments)
        {
            frag.UpdateConstraints(stiffness, breakThreshold);
            frag.ApplyMatrixTransform();
        }
    }
}
