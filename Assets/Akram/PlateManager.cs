using UnityEngine;
using System.Collections.Generic;

/// Main manager for the fracturing plate system
/// Alpha now controls visual pre-separation of fragments before impact
public class PlateManager : MonoBehaviour
{
    [Header("=== Visual Parameters ===")]
    [Tooltip("α controls pre-separation look and scatter strength after impact")]
    [Range(0f, 1.5f)] public float alpha = 0.8f;

    [Header("=== Physics Parameters ===")]
    [Tooltip("Downward velocity (m/s)")]
    public float fallSpeed = 8f;

    [Header("=== Rotation Parameters ===")]
    [Tooltip("Fragments rotate only after fracture (intact plate falls straight)")]
    public bool enableFragmentRotation = true;
    [Tooltip("Controls angular velocity for fragment rotation (0=no rotation, 1=full speed)")]
    [Range(0f, 2f)] public float fragmentAngularVelocity = 0.8f;

    [Header("=== Plate Geometry ===")]
    [Range(2, 32)] public int gridResolution = 10;
    public float plateWidth = 6f;
    public float plateDepth = 6f;
    public float plateThickness = 0.12f;

    [Header("=== Initial State ===")]
    public Vector3 startPosition = new Vector3(0, 8f, 0);
    public Vector3 startRotation = Vector3.zero;

    [Header("=== Fracture Physics ===")]
    [Tooltip("Radius around impact point that affects fragments (in world units)")]
    [Range(1f, 10f)] public float impactRadius = 3f;
    [Tooltip("Base impulse strength on fracture")]
    [Range(5f, 25f)] public float impulseStrength = 12f;
    [Tooltip("Torque multiplier applied to fragments on impact")]
    [Range(0f, 10f)] public float fragmentTorqueMultiplier = 3f;

    [Header("=== Debug ===")]
    public bool showDebugGizmos = true;

    // Components
    private PlateGeometry plateGeometry;
    private PlatePhysics platePhysics;
    private CollisionDetector collisionDetector;

    // State
    private List<PieceBehaviour> pieces = new List<PieceBehaviour>();
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
        collisionDetector.RefreshColliders();
        collisionDetector.RegisterFragments(pieces);

        if (collisionDetector.GetObstacleCount() == 0)
        {
            Debug.LogWarning("[PlateManager] No obstacles with ObstacleBounds component found in scene!");
        }
    }

    void Update()
    {
        if (Time.frameCount % 30 == 0)
            collisionDetector.RefreshColliders();

        if (!isFractured && !hasLanded)
        {
            UpdateIntactPlate();
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
        platePhysics.Initialize(
            startPosition,
            fallSpeed,
            Quaternion.Euler(startRotation),
            Vector3.zero
        );
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
        pieces = plateGeometry.CreateFragments(transform, mat, startPosition);
        SetFragmentsActive(false);
    }

    /// Apply visual separation based on alpha (pre-fracture cracks)
    void ApplyAlphaVisualization()
    {
        if (alpha < 0.05f) return;

        // Calculate separation scale based on alpha
        float separationAmount = alpha * 0.05f; // Max 0.075 units separation

        float cellWidth = plateWidth / gridResolution;
        float cellDepth = plateDepth / gridResolution;

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
                if (index >= pieces.Count) break;

                Vector3 localCenter = gridOrigin + new Vector3(x * cellWidth, 0f, z * cellDepth);

                // Push fragments away from center based on alpha
                Vector3 offset = localCenter.normalized * separationAmount;

                // Apply to fragment's local position in intact plate visualization
                // This creates visible cracks before fracture
                pieces[index].transform.localPosition = localCenter + offset;

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

        Vector3 normal, hitPoint, closestPoint;
        ObstacleBounds hitObstacle;

        if (collisionDetector.CheckSphereCollision(
            platePhysics.Position,
            collisionRadius,
            out normal,
            out hitPoint,
            out closestPoint,
            out hitObstacle))
        {
            if (hitObstacle != null && hitObstacle.isGround)
            {
                hasLanded = true;

                Vector3 min, max;
                hitObstacle.GetWorldAABB(out min, out max);
                float groundTopY = max.y;
                float plateBottomY = platePhysics.Position.y - plateThickness * 0.5f;
                float offsetY = groundTopY - plateBottomY + plateThickness * 0.5f;

                Vector3 landedPosition = new Vector3(
                    platePhysics.Position.x,
                    platePhysics.Position.y + offsetY,
                    platePhysics.Position.z
                );

                intactPlate.transform.position = landedPosition;
                platePhysics.Initialize(landedPosition, 0f, platePhysics.Rotation, Vector3.zero);
            }
            else
            {
                FracturePlate(hitPoint, normal);
            }
        }
    }

    void UpdateFragments()
    {
        foreach (var piece in pieces)
        {
            piece.PhysicsUpdate(Time.deltaTime, fallSpeed, collisionDetector);
        }
    }

    void FracturePlate(Vector3 impactPoint, Vector3 normal)
    {
        isFractured = true;

        intactPlate.SetActive(false);
        SetFragmentsActive(true);

        // Alpha affects impulse strength (scatter effect)
        float finalImpulseStrength = impulseStrength * (1f + alpha * 0.5f);

        Vector3 plateAngularVelocity = enableFragmentRotation ? platePhysics.AngularVelocity : Vector3.zero;
        float effectiveTorqueMultiplier = enableFragmentRotation ? fragmentTorqueMultiplier : 0f;
        float effectiveAngularVelocity = enableFragmentRotation ? fragmentAngularVelocity : 0f;

        foreach (var piece in pieces)
        {
            Vector3 worldCenter = platePhysics.Position + platePhysics.Rotation * piece.LocalCenter;

            piece.Initialize(
                worldCenter,
                platePhysics.Velocity,
                impactPoint,
                normal,
                finalImpulseStrength,
                impactRadius,
                plateAngularVelocity,
                effectiveTorqueMultiplier,
                platePhysics.Rotation,
                effectiveAngularVelocity
            );
        }
    }

    void SetFragmentsActive(bool active)
    {
        foreach (var piece in pieces)
        {
            if (piece != null)
                piece.gameObject.SetActive(active);
        }
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        Vector3 center = Application.isPlaying ? platePhysics.Position : startPosition;

        Gizmos.color = Color.yellow;
        Gizmos.matrix = Application.isPlaying ?
            Matrix4x4.TRS(platePhysics.Position, platePhysics.Rotation, Vector3.one) :
            Matrix4x4.TRS(startPosition, Quaternion.Euler(startRotation), Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(plateWidth, plateThickness, plateDepth));
        Gizmos.matrix = Matrix4x4.identity;

        if (Application.isPlaying)
        {
            float collisionRadius = Mathf.Min(plateWidth, plateDepth) * 0.35f;
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawWireSphere(platePhysics.Position, collisionRadius);
        }

        if (Application.isPlaying && collisionDetector != null)
        {
            collisionDetector.DrawDebugGizmos();
        }
    }
}