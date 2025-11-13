using UnityEngine;
using System.Collections.Generic;

/// Main controller for the plate fracture simulation
/// Manages intact plate falling and fracture into physics-simulated fragments
public class PlateManager : MonoBehaviour
{
    [Header("=== Visual Parameters ===")]
    [Tooltip("Controls pre-fracture separation and post-fracture scatter strength")]
    [Range(0f, 1.5f)] public float alpha = 0.8f;

    [Header("=== Physics Parameters ===")]
    [Tooltip("Downward fall velocity (m/s)")]
    public float fallSpeed = 8f;

    [Header("=== Rotation Parameters ===")]
    [Tooltip("Enable fragment rotation after fracture")]
    public bool enableFragmentRotation = true;
    [Tooltip("Angular velocity multiplier for fragments")]
    [Range(0f, 2f)] public float fragmentAngularVelocity = 0.8f;

    [Header("=== Plate Geometry ===")]
    [Range(2, 32)] public int gridResolution = 10;
    public float plateWidth = 6f;
    public float plateDepth = 6f;
    public float plateThickness = 0.12f;

    [Header("=== Initial State ===")]
    public Vector3 startPosition = new Vector3(0, 8f, 0);
    public Vector3 startRotation = Vector3.zero;

    [Header("=== Fracture Settings ===")]
    [Tooltip("Impact radius affecting fragments")]
    [Range(1f, 10f)] public float impactRadius = 3f;
    [Tooltip("Base impulse strength on fracture")]
    [Range(5f, 25f)] public float impulseStrength = 12f;
    [Tooltip("Torque multiplier for fragment rotation")]
    [Range(0f, 10f)] public float fragmentTorqueMultiplier = 3f;

    [Header("=== Debug ===")]
    public bool showDebugGizmos = true;

    // Components
    private PlateGeometry plateGeometry;
    private PlatePhysics platePhysics;
    private CollisionDetector collisionDetector;

    // State
    private List<PieceBehaviour> fragments = new List<PieceBehaviour>();
    private GameObject intactPlate;
    private bool isFractured = false;
    private bool hasLanded = false;

    void Awake()
    {
        InitializeComponents();
        CreateIntactPlate();
        CreateFragments();
    }

    void Start()
    {
        collisionDetector.RefreshObstacles();
        collisionDetector.RegisterFragments(fragments);

        if (collisionDetector.GetObstacleCount() == 0)
        {
            Debug.LogWarning("[PlateManager] No ObstacleBounds components found in scene!");
        }
    }

    void Update()
    {
        // Periodic obstacle refresh
        if (Time.frameCount % 30 == 0)
            collisionDetector.RefreshObstacles();

        if (!isFractured && !hasLanded)
        {
            UpdateIntactPlate();
            if (!isFractured) // Only apply alpha visualization if not fractured
                ApplyAlphaVisualization();
        }
        else if (isFractured)
        {
            UpdateFragments();
        }
    }

    void InitializeComponents()
    {
        plateGeometry = gameObject.AddComponent<PlateGeometry>();
        platePhysics = gameObject.AddComponent<PlatePhysics>();
        collisionDetector = gameObject.AddComponent<CollisionDetector>();

        plateGeometry.Initialize(plateWidth, plateDepth, plateThickness, gridResolution);
        platePhysics.Initialize(startPosition, fallSpeed, Quaternion.Euler(startRotation), Vector3.zero);
    }

    void CreateIntactPlate()
    {
        var hostMR = GetComponent<MeshRenderer>();
        if (!hostMR) hostMR = gameObject.AddComponent<MeshRenderer>();
        if (!GetComponent<MeshFilter>()) gameObject.AddComponent<MeshFilter>();

        intactPlate = new GameObject("IntactPlate");
        intactPlate.transform.SetParent(transform, false);

        var mf = intactPlate.AddComponent<MeshFilter>();
        var mr = intactPlate.AddComponent<MeshRenderer>();
        mf.sharedMesh = plateGeometry.CreatePlateMesh();
        mr.sharedMaterial = hostMR.sharedMaterial ?? new Material(Shader.Find("Standard"));
    }

    void CreateFragments()
    {
        var mat = intactPlate.GetComponent<MeshRenderer>().sharedMaterial;
        fragments = plateGeometry.CreateFragments(transform, mat, startPosition);
        SetFragmentsActive(false);
    }

    void ApplyAlphaVisualization()
    {
        if (alpha < 0.05f) return;

        float cellWidth = plateWidth / gridResolution;
        float cellDepth = plateDepth / gridResolution;
        float separationAmount = alpha * 0.05f;

        Vector3 gridOrigin = new Vector3(
            -plateWidth * 0.5f + cellWidth * 0.5f,
            0f,
            -plateDepth * 0.5f + cellDepth * 0.5f
        );

        int index = 0;
        for (int z = 0; z < gridResolution; z++)
        {
            for (int x = 0; x < gridResolution; x++)
            {
                if (index >= fragments.Count) break;

                Vector3 localCenter = gridOrigin + new Vector3(x * cellWidth, 0f, z * cellDepth);
                Vector3 offset = localCenter.normalized * separationAmount;
                fragments[index].transform.localPosition = localCenter + offset;

                index++;
            }
        }
    }

    void UpdateIntactPlate()
    {
        platePhysics.UpdateIntact(Time.deltaTime);
        intactPlate.transform.position = platePhysics.Position;
        intactPlate.transform.rotation = platePhysics.Rotation;

        float collisionRadius = Mathf.Min(plateWidth, plateDepth) * 0.35f;
        Vector3 normal, hitPoint;
        ObstacleBounds hitObstacle;

        if (collisionDetector.CheckObstacleCollision(
            platePhysics.Position,
            collisionRadius,
            out normal,
            out hitPoint,
            out hitObstacle))
        {
            if (hitObstacle != null && hitObstacle.isGround)
            {
                // Land intact on ground
                hasLanded = true;
                Vector3 min, max;
                hitObstacle.GetWorldAABB(out min, out max);
                float offsetY = max.y + plateThickness * 0.5f - (platePhysics.Position.y - plateThickness * 0.5f);
                Vector3 landedPos = new Vector3(
                    platePhysics.Position.x,
                    platePhysics.Position.y + offsetY,
                    platePhysics.Position.z
                );
                intactPlate.transform.position = landedPos;
                platePhysics.Initialize(landedPos, 0f, platePhysics.Rotation, Vector3.zero);
            }
            else
            {
                // Fracture on obstacle impact
                FracturePlate(hitPoint, normal);
            }
        }
    }

    void UpdateFragments()
    {
        foreach (var fragment in fragments)
        {
            fragment.PhysicsUpdate(Time.deltaTime, collisionDetector);
        }
    }

    void FracturePlate(Vector3 impactPoint, Vector3 normal)
    {
        isFractured = true;
        intactPlate.SetActive(false);
        SetFragmentsActive(true);

        float finalImpulse = impulseStrength * (1f + alpha * 0.5f);
        float effectiveTorque = enableFragmentRotation ? fragmentTorqueMultiplier : 0f;
        float effectiveAngularVel = enableFragmentRotation ? fragmentAngularVelocity : 0f;

        foreach (var fragment in fragments)
        {
            Vector3 worldCenter = platePhysics.Position + platePhysics.Rotation * fragment.LocalCenter;

            fragment.Initialize(
                worldCenter,
                platePhysics.Velocity,
                impactPoint,
                normal,
                finalImpulse,
                impactRadius,
                effectiveTorque,
                platePhysics.Rotation,
                effectiveAngularVel
            );
        }
    }

    void SetFragmentsActive(bool active)
    {
        foreach (var fragment in fragments)
        {
            if (fragment != null)
                fragment.gameObject.SetActive(active);
        }
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        Vector3 center = Application.isPlaying ? platePhysics.Position : startPosition;
        Quaternion rotation = Application.isPlaying ? platePhysics.Rotation : Quaternion.Euler(startRotation);

        // Plate bounds
        Gizmos.color = Color.yellow;
        Gizmos.matrix = Matrix4x4.TRS(center, rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(plateWidth, plateThickness, plateDepth));
        Gizmos.matrix = Matrix4x4.identity;

        // Collision sphere
        if (Application.isPlaying)
        {
            float collisionRadius = Mathf.Min(plateWidth, plateDepth) * 0.35f;
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawWireSphere(platePhysics.Position, collisionRadius);
        }

        // Obstacles
        if (Application.isPlaying && collisionDetector != null)
        {
            collisionDetector.DrawDebugGizmos();
        }
    }
}