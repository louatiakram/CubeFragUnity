using UnityEngine;
using System.Collections.Generic;

/// Main manager for the fracturing plate system
/// Handles plate lifecycle: intact → fracture on collision → physics simulation
/// Compatible with ObstacleBounds-based collision detection
/// Now includes rotation physics for realistic tumbling
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
    [Range(0f, 1f)] public float fragmentAngularVelocity = 0.5f;

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
    [Range(0f, 10f)] public float fragmentTorqueMultiplier = 3f; // Increased default

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

        // Warn if no obstacles found
        if (collisionDetector.GetObstacleCount() == 0)
        {
            Debug.LogWarning("[PlateManager] No obstacles with ObstacleBounds component found in scene! Add ObstacleBounds to objects you want the plate to collide with.");
        }
    }

    void Update()
    {
        // Refresh obstacle list periodically
        if (Time.frameCount % 30 == 0)
            collisionDetector.RefreshColliders();

        if (!isFractured && !hasLanded)
        {
            UpdateIntactPlate();
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
            Vector3.zero // No rotation for intact plate
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

    void UpdateIntactPlate()
    {
        // Physics update with rotation
        platePhysics.UpdateIntact(Time.deltaTime);

        // Visual update
        intactPlate.transform.position = platePhysics.Position;
        intactPlate.transform.rotation = platePhysics.Rotation;

        // Collision detection using oriented bounding box
        float plateRadius = 0.5f * Mathf.Sqrt(plateWidth * plateWidth + plateDepth * plateDepth);
        Vector3 normal, hitPoint, closestPoint;
        ObstacleBounds hitObstacle;

        // Use much tighter collision radius - only detect when plate edges are very close
        float collisionRadius = Mathf.Min(plateWidth, plateDepth) * 0.35f; // Use smaller dimension, heavily reduced

        if (collisionDetector.CheckSphereCollision(
            platePhysics.Position,
            collisionRadius,
            out normal,
            out hitPoint,
            out closestPoint,
            out hitObstacle))
        {
            // Check if we hit ground
            if (hitObstacle != null && hitObstacle.isGround)
            {
                // Stop falling - plate lands intact on ground
                hasLanded = true;

                // Position plate exactly on top of ground
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
                // Hit non-ground obstacle - fracture
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

        // Hide intact plate, show fragments
        intactPlate.SetActive(false);
        SetFragmentsActive(true);

        // Calculate impulse strength based on alpha
        float finalImpulseStrength = impulseStrength * Mathf.Max(alpha, 0f);

        // Only apply angular velocity if rotation is enabled
        Vector3 plateAngularVelocity = enableFragmentRotation ? platePhysics.AngularVelocity : Vector3.zero;
        float effectiveTorqueMultiplier = enableFragmentRotation ? fragmentTorqueMultiplier : 0f;
        float effectiveAngularVelocity = enableFragmentRotation ? fragmentAngularVelocity : 0f;

        foreach (var piece in pieces)
        {
            // Transform local center to world space using current rotation
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

        // Draw plate bounds
        Gizmos.color = Color.yellow;
        Gizmos.matrix = Application.isPlaying ?
            Matrix4x4.TRS(platePhysics.Position, platePhysics.Rotation, Vector3.one) :
            Matrix4x4.TRS(startPosition, Quaternion.Euler(startRotation), Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(plateWidth, plateThickness, plateDepth));
        Gizmos.matrix = Matrix4x4.identity;

        // Draw collision sphere (what actually triggers fracture)
        if (Application.isPlaying)
        {
            float collisionRadius = Mathf.Min(plateWidth, plateDepth) * 0.35f;
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f); // Red to show actual collision zone
            Gizmos.DrawWireSphere(platePhysics.Position, collisionRadius);
        }

        // Draw detected obstacles
        if (Application.isPlaying && collisionDetector != null)
        {
            collisionDetector.DrawDebugGizmos();
        }
    }
}
