
using UnityEngine;
using System.Collections.Generic;

/// Main manager for the fracturing plate system
/// Handles plate lifecycle: intact → fracture on collision → physics simulation
/// Compatible with ObstacleBounds-based collision detection
public class PlateManager : MonoBehaviour
{
    [Header("=== Visual Parameters ===")]
    [Tooltip("α controls pre-separation look and scatter strength after impact")]
    [Range(0f, 1.5f)] public float alpha = 0.8f;

    [Header("=== Physics Parameters ===")]
    [Tooltip("Downward velocity (m/s)")]
    public float fallSpeed = 8f;

    [Header("=== Plate Geometry ===")]
    [Range(2, 32)] public int gridResolution = 10;
    public float plateWidth = 6f;
    public float plateDepth = 6f;
    public float plateThickness = 0.12f;

    [Header("=== Initial State ===")]
    public Vector3 startPosition = new Vector3(0, 8f, 0);

    [Header("=== Fracture Physics ===")]
    [Tooltip("Radius around impact point that affects fragments (in world units)")]
    [Range(1f, 10f)] public float impactRadius = 3f;
    [Tooltip("Base impulse strength on fracture")]
    [Range(5f, 25f)] public float impulseStrength = 12f;

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
    private bool hasLanded = false; // New state to track if plate has landed on ground

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
        // Refresh obstacle list periodically (you can remove this if you manage obstacles yourself)
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
        // If hasLanded but not fractured, plate stops falling (no updates)
    }

    void InitializeComponents()
    {
        plateGeometry = gameObject.AddComponent<PlateGeometry>();
        platePhysics = gameObject.AddComponent<PlatePhysics>();
        collisionDetector = gameObject.AddComponent<CollisionDetector>();

        plateGeometry.Initialize(plateWidth, plateDepth, plateThickness, gridResolution);
        platePhysics.Initialize(startPosition, fallSpeed);
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
        // Physics update
        platePhysics.UpdateIntact(Time.deltaTime);

        // Visual update
        intactPlate.transform.position = platePhysics.Position;
        intactPlate.transform.rotation = Quaternion.identity;

        // Collision detection using bounding sphere
        float plateRadius = 0.5f * Mathf.Sqrt(plateWidth * plateWidth + plateDepth * plateDepth);
        Vector3 normal, hitPoint, closestPoint;
        ObstacleBounds hitObstacle;
        if (collisionDetector.CheckSphereCollision(
            platePhysics.Position,
            plateRadius,
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
                platePhysics.Initialize(landedPosition, 0f); // Stop movement
            }
            else
            {
                // Hit non-ground obstacle - fracture normally
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

        foreach (var piece in pieces)
        {
            piece.Initialize(
                platePhysics.Position + piece.LocalCenter,
                platePhysics.Velocity,
                impactPoint,
                normal,
                finalImpulseStrength,
                impactRadius
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
        Gizmos.DrawWireCube(center, new Vector3(plateWidth, plateThickness, plateDepth));

        // Draw collision sphere (what actually triggers fracture)
        if (Application.isPlaying)
        {
            float plateRadius = 0.5f * Mathf.Sqrt(plateWidth * plateWidth + plateDepth * plateDepth);
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(platePhysics.Position, plateRadius);
        }

        // Draw detected obstacles
        if (Application.isPlaying && collisionDetector != null)
        {
            collisionDetector.DrawDebugGizmos();
        }
    }
}
