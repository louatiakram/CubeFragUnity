using UnityEngine;
using System.Collections.Generic;

/// Full, fixed, simplified fracture demo (math-only).
/// - Exposed: alpha (look + blast), fallSpeed (only fall parameter).
/// - Pre-impact: single thin plate falls as one.
/// - On first contact with any collider tagged "Obstacle": plate breaks into a grid of cubes.
/// - Break impulse is projected onto obstacle tangent -> no "pop up", natural sliding.
/// - Supports SphereCollider and BoxCollider (OBB). Fallback ground plane y=0.
///
/// Setup:
///   • Create obstacles, add SphereCollider or BoxCollider, and tag them "Obstacle".
///   • Add this script to an empty GameObject; assign a material to its MeshRenderer (optional).
///   • Tweak `alpha` (0..1.5 like the video) and `fallSpeed`.
public class AlphaPlate_Simple : MonoBehaviour
{
    [Header("=== CONTROLS ===")]
    [Tooltip("α controls pre-separation look and scatter strength after impact.")]
    public float alpha = 0.8f;          // Try 0.0, 0.5, 1.0, 1.5

    [Tooltip("Downward speed (m/s). This is the ONLY fall parameter.")]
    public float fallSpeed = 8f;        // 5=slow, 8=medium, 12=fast

    [Header("=== PLATE & GRID ===")]
    [Range(2, 32)] public int grid = 10;
    public float plateWidth = 6f;
    public float plateDepth = 6f;
    public float plateThickness = 0.12f; // thin slab look

    [Header("=== START ===")]
    public Vector3 startPos = new Vector3(0, 8f, 0);

    [Header("=== DEBUG ===")]
    public bool showDebug = true;

    // ---------- Internal structures ----------
    class Piece
    {
        public Transform tf;
        public Vector3 localCenter;  // local center when plate is assembled
        public Vector3 pos, vel;     // math-only state (post-break)
        public float mass;
        public float radius;         // bounding sphere for contact
    }

    struct Obstacle
    {
        public enum Kind { Sphere, Box }
        public Kind kind;
        public Transform tr;
        public SphereCollider sc;
        public BoxCollider bc;
        public Vector3 half;       // world half extents (box)
        public Quaternion rot;     // world rotation (box)
        public Vector3 centerWS;   // world center (box)
        public float sphereR;      // world radius (sphere)
    }

    // ---------- State ----------
    private List<Piece> pieces = new List<Piece>();
    private List<Obstacle> obstacles = new List<Obstacle>();
    private Vector3 platePos, plateVel;
    private bool glued = true;
    private bool justBroke = false; // one-frame guard after break
    private float dtAcc = 0f;

    // Single visible thin plate (pre-break)
    private GameObject prePlateGO;
    private MeshFilter prePlateMF;
    private MeshRenderer prePlateMR;

    // ---------- Constants (keep internal for simplicity) ----------
    const float timeStep = 1f / 60f;
    const float restitutionImpact = 0.05f; // tiny bounce
    const float frictionK = 0.85f;         // strong sliding friction
    const float impulseBase = 12f;         // scaled by alpha
    const float visualDefragRadial = 0.25f; // visual offsets while glued
    const float visualDefragJitter = 0.05f;

    // ------------------------------- Unity -------------------------------
    void Awake()
    {
        platePos = startPos;
        plateVel = Vector3.down * Mathf.Max(fallSpeed, 0f);

        // Ensure host has a renderer (for material); create the pre-break plate
        var hostMR = GetComponent<MeshRenderer>();
        if (!hostMR) hostMR = gameObject.AddComponent<MeshRenderer>();
        if (!GetComponent<MeshFilter>()) gameObject.AddComponent<MeshFilter>();

        prePlateGO = new GameObject("ThinPlate_PreBreak");
        prePlateGO.transform.SetParent(transform, false);
        prePlateMF = prePlateGO.AddComponent<MeshFilter>();
        prePlateMR = prePlateGO.AddComponent<MeshRenderer>();
        prePlateMF.sharedMesh = BuildBox(plateWidth, plateThickness, plateDepth);
        prePlateMR.sharedMaterial = hostMR.sharedMaterial != null
            ? hostMR.sharedMaterial
            : new Material(Shader.Find("Standard"));

        // Build fragments hidden
        BuildGrid(prePlateMR.sharedMaterial);
        SetFragmentsActive(false);
    }

    void Start()
    {
        RefreshObstacles();
    }

    void Update()
    {
        if (glued)
            plateVel = Vector3.down * Mathf.Max(fallSpeed, 0f);

        // refresh obstacle cache occasionally
        if (Time.frameCount % 30 == 0)
            RefreshObstacles();

        dtAcc += Mathf.Min(Time.deltaTime, 0.05f);
        while (dtAcc >= timeStep)
        {
            dtAcc -= timeStep;
            Step(timeStep);
        }

        // Render
        if (glued)
        {
            // Move the single thin plate
            prePlateGO.transform.position = platePos;
            prePlateGO.transform.rotation = Quaternion.identity;

            // Apply only a VISUAL defrag (they remain glued physically)
            float w = plateWidth, d = plateDepth;
            float radial = visualDefragRadial * alpha;
            float jitter = visualDefragJitter * alpha;
            foreach (var p in pieces)
            {
                Vector3 outward = new Vector3(
                    p.localCenter.x / (w * 0.5f + 1e-6f), 0f,
                    p.localCenter.z / (d * 0.5f + 1e-6f)
                );
                Vector3 offset = outward * radial + Random.insideUnitSphere * jitter;
                // Place child cubes for visuals too (kept inactive - but if you prefer seeing them, toggle SetFragmentsActive(true) to preview)
                p.tf.position = platePos + p.localCenter + offset;
                p.tf.rotation = Quaternion.Euler(0f, (Random.value - 0.5f) * 20f * alpha, 0f);
            }
        }
        else
        {
            foreach (var p in pieces)
                p.tf.position = p.pos;
        }
    }

    // ------------------------------- Simulation -------------------------------
    void Step(float dt)
    {
        if (glued)
        {
            platePos += plateVel * dt;

            // Collide plate proxy (sphere) vs world; first hit -> break
            float plateR = 0.5f * Mathf.Sqrt(plateWidth * plateWidth + plateDepth * plateDepth);
            Vector3 n, pHit;
            if (CollideWorldSphere(ref platePos, ref plateVel, plateR, dt, out n, out pHit))
            {
                BreakGlue(pHit, n);
                justBroke = true;
            }
        }
        else
        {
            foreach (var p in pieces)
            {
                p.vel += Vector3.down * fallSpeed * dt;
                p.pos += p.vel * dt;

                if (justBroke) continue;

                Vector3 n, ph;
                if (CollideWorldSphere(ref p.pos, ref p.vel, p.radius, dt, out n, out ph))
                {
                    // Kill normal bounce, keep slide
                    float vn = Vector3.Dot(p.vel, n);
                    if (vn < 0f) p.vel -= (1f + restitutionImpact) * vn * n;

                    // Kinetic friction
                    Vector3 vt = p.vel - Vector3.Dot(p.vel, n) * n;
                    p.vel -= vt * frictionK;

                    // Project "gravity" along tangent to follow surface
                    Vector3 g = Vector3.down * fallSpeed;
                    g -= n * Vector3.Dot(g, n);
                    p.vel += g * dt;
                }
            }
            // Consume one-frame guard
            justBroke = false;
        }
    }

    void BreakGlue(Vector3 impactPoint, Vector3 normal)
    {
        glued = false;

        // Hide plate, show fragments
        if (prePlateGO) prePlateGO.SetActive(false);
        SetFragmentsActive(true);

        float J = impulseBase * Mathf.Max(alpha, 0f);

        foreach (var p in pieces)
        {
            p.pos = platePos + p.localCenter;
            p.vel = plateVel;

            // Tangent-only impulse for natural spread
            Vector3 n = (normal.sqrMagnitude > 1e-6f) ? normal.normalized : Vector3.up;
            Vector3 radial = (p.pos - impactPoint);
            Vector3 tang = radial - Vector3.Dot(radial, n) * n;
            if (tang.sqrMagnitude < 1e-8f)
            {
                tang = Vector3.Cross(n, Vector3.right);
                if (tang.sqrMagnitude < 1e-6f) tang = Vector3.Cross(n, Vector3.forward);
            }
            tang = (tang.normalized + 0.12f * Random.insideUnitSphere).normalized;

            float Jm = J * p.mass;
            p.vel += tang * (Jm / Mathf.Max(p.mass, 1e-6f));

            // Tiny normal nudge to prevent z-fighting
            p.vel += n * (0.12f * alpha);
        }
    }

    // ------------------------------- Obstacles via Tag -------------------------------
    void RefreshObstacles()
    {
        obstacles.Clear();
        var obs = GameObject.FindGameObjectsWithTag("Obstacle");
        for (int i = 0; i < obs.Length; i++)
        {
            var go = obs[i];
            var sc = go.GetComponent<SphereCollider>();
            var bc = go.GetComponent<BoxCollider>();

            if (sc != null)
            {
                float scale = Mathf.Max(go.transform.lossyScale.x, go.transform.lossyScale.y, go.transform.lossyScale.z);
                obstacles.Add(new Obstacle
                {
                    kind = Obstacle.Kind.Sphere,
                    tr = go.transform,
                    sc = sc,
                    sphereR = Mathf.Abs(sc.radius * scale)
                });
            }
            else if (bc != null)
            {
                Vector3 lossy = go.transform.lossyScale;
                Vector3 half = Vector3.Scale(bc.size * 0.5f, lossy);
                obstacles.Add(new Obstacle
                {
                    kind = Obstacle.Kind.Box,
                    tr = go.transform,
                    bc = bc,
                    half = new Vector3(Mathf.Abs(half.x), Mathf.Abs(half.y), Mathf.Abs(half.z)),
                    rot = go.transform.rotation,
                    centerWS = go.transform.TransformPoint(bc.center)
                });
            }
        }
    }

    /// Sphere (our proxy) vs world (sphere/box) with ground fallback y=0.
    /// Pushes pos out of penetration and returns contact normal/point.
    bool CollideWorldSphere(ref Vector3 pos, ref Vector3 vel, float radius, float dt, out Vector3 outN, out Vector3 outP)
    {
        bool hit = false; Vector3 N = Vector3.zero; Vector3 P = Vector3.zero;

        // Spheres
        for (int i = 0; i < obstacles.Count; i++)
        {
            var ob = obstacles[i];
            if (ob.kind != Obstacle.Kind.Sphere) continue;

            Vector3 c = ob.tr.position;
            float R = ob.sphereR + radius;
            Vector3 to = pos - c;
            float d2 = to.sqrMagnitude;

            if (d2 < R * R)
            {
                float d = Mathf.Max(Mathf.Sqrt(d2), 1e-6f);
                Vector3 n = to / d;
                float penetration = R - d;
                pos += n * penetration;

                N = n; P = pos - n * radius; hit = true;
            }
        }

        // Boxes (OBB)
        for (int i = 0; i < obstacles.Count; i++)
        {
            var ob = obstacles[i];
            if (ob.kind != Obstacle.Kind.Box) continue;

            Quaternion inv = Quaternion.Inverse(ob.rot);
            Vector3 localP = inv * (pos - ob.centerWS);

            Vector3 q = new Vector3(
                Mathf.Clamp(localP.x, -ob.half.x, ob.half.x),
                Mathf.Clamp(localP.y, -ob.half.y, ob.half.y),
                Mathf.Clamp(localP.z, -ob.half.z, ob.half.z)
            );
            Vector3 diff = localP - q;
            float d2 = diff.sqrMagnitude;
            if (d2 < radius * radius)
            {
                Vector3 nLocal;
                float d = Mathf.Sqrt(Mathf.Max(d2, 1e-10f));
                if (d > 1e-6f) nLocal = diff / d;
                else
                {
                    // choose face normal based on smallest slack
                    Vector3 slack = new Vector3(
                        ob.half.x - Mathf.Abs(localP.x),
                        ob.half.y - Mathf.Abs(localP.y),
                        ob.half.z - Mathf.Abs(localP.z)
                    );
                    if (slack.x <= slack.y && slack.x <= slack.z) nLocal = new Vector3(Mathf.Sign(localP.x), 0, 0);
                    else if (slack.y <= slack.x && slack.y <= slack.z) nLocal = new Vector3(0, Mathf.Sign(localP.y), 0);
                    else nLocal = new Vector3(0, 0, Mathf.Sign(localP.z));
                }

                Vector3 nWorld = ob.rot * nLocal;
                float penetration = radius - Mathf.Sqrt(Mathf.Max(d2, 0f));
                pos += nWorld * penetration;

                N = nWorld; P = pos - nWorld * radius; hit = true;
            }
        }

        // Ground fallback (y=0)
        if (!hit)
        {
            float dist = pos.y;                 // plane n=up, d=0
            float penetration = radius - dist;
            if (penetration > 0f)
            {
                Vector3 n = Vector3.up;
                pos += n * penetration;
                N = n; P = new Vector3(pos.x, 0f, pos.z);
                hit = true;
            }
        }

        outN = N; outP = P;
        return hit;
    }

    // ------------------------------- Build & Helpers -------------------------------
    void BuildGrid(Material mat)
    {
        pieces.Clear();

        float cellX = plateWidth / grid;
        float cellZ = plateDepth / grid;
        float y = 0f;

        Vector3 origin = new Vector3(-plateWidth * 0.5f + cellX * 0.5f, y, -plateDepth * 0.5f + cellZ * 0.5f);
        float totalVol = plateWidth * plateDepth * plateThickness;
        float cellVol = cellX * cellZ * plateThickness;

        for (int iz = 0; iz < grid; iz++)
        {
            for (int ix = 0; ix < grid; ix++)
            {
                Vector3 localC = origin + new Vector3(ix * cellX, 0f, iz * cellZ);

                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"Cell_{ix}_{iz}";
                go.transform.SetParent(transform, false);
                go.transform.localScale = new Vector3(cellX * 0.98f, plateThickness, cellZ * 0.98f);

                var col = go.GetComponent<Collider>();
                if (col) DestroyImmediate(col); // no Unity colliders on fragments

                var mr = go.GetComponent<MeshRenderer>();
                mr.sharedMaterial = mat;

                var p = new Piece();
                p.tf = go.transform;
                p.localCenter = localC;
                p.mass = Mathf.Max(cellVol / Mathf.Max(totalVol, 1e-6f), 0.01f); // relative mass
                p.radius = 0.5f * Mathf.Sqrt(cellX * cellX + plateThickness * plateThickness + cellZ * cellZ);
                p.pos = startPos + localC; // will be set on break anyway
                p.vel = Vector3.zero;

                pieces.Add(p);
            }
        }
    }

    void SetFragmentsActive(bool on)
    {
        foreach (var p in pieces)
            if (p.tf) p.tf.gameObject.SetActive(on);
    }

    Mesh BuildBox(float sx, float sy, float sz)
    {
        Vector3 h = new Vector3(sx * 0.5f, sy * 0.5f, sz * 0.5f);

        Vector3[] v = {
            new Vector3(-h.x,-h.y,-h.z), new Vector3(h.x,-h.y,-h.z),
            new Vector3(h.x,-h.y, h.z), new Vector3(-h.x,-h.y, h.z),
            new Vector3(-h.x, h.y,-h.z), new Vector3(h.x, h.y,-h.z),
            new Vector3(h.x, h.y, h.z),  new Vector3(-h.x, h.y, h.z)
        };
        int[] t = {
            0,2,1, 0,3,2,
            4,5,6, 4,6,7,
            3,6,2, 3,7,6,
            0,1,5, 0,5,4,
            0,4,7, 0,7,3,
            1,2,6, 1,6,5
        };

        Mesh m = new Mesh { name = "ThinPlate" };
        m.vertices = v; m.triangles = t;
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    void OnDrawGizmos()
    {
        if (!showDebug) return;

        Vector3 c = Application.isPlaying ? platePos : startPos;

        // Plate footprint
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(c + Vector3.up * 0.0005f, new Vector3(plateWidth, 0.002f, plateDepth));

        // Obstacles preview in play mode
        if (Application.isPlaying)
        {
            foreach (var ob in obstacles)
            {
                if (ob.kind == Obstacle.Kind.Sphere)
                {
                    Gizmos.color = new Color(0.9f, 0.2f, 0.9f, 1f);
                    Gizmos.DrawWireSphere(ob.tr.position, ob.sphereR);
                }
                else
                {
                    Gizmos.color = new Color(1.0f, 0.6f, 0.15f, 1f);
                    Gizmos.matrix = Matrix4x4.TRS(ob.centerWS, ob.rot, Vector3.one);
                    Gizmos.DrawWireCube(Vector3.zero, ob.half * 2f);
                    Gizmos.matrix = Matrix4x4.identity;
                }
            }
        }
    }
}