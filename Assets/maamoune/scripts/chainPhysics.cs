using UnityEngine;
using System.Collections.Generic;

public class ChainPhysicsSystem : MonoBehaviour
{
    [Header("Chain Configuration")]
    public int numberOfLinks = 10;
    public float linkMass = 0.5f;
    public float linkSize = 0.15f;
    public float linkSpacing = 0.25f;

    [Header("Physics Parameters")]
    public float gravity = -9.81f;
    public float airDamping = 0.995f;
    public int constraintSolverIterations = 50;

    [Header("Constraint Parameters (from Paper)")]
    [Tooltip("Constraint stiffness K (spring constant)")]
    public float constraintStiffness = 1000000.0f; // K - spring stiffness

    [Tooltip("Constraint damping B")]
    public float constraintDamping = 0.0f; // B - damping coefficient

    [Header("Detachment Parameters")]
    [Tooltip("Maximum stretch ratio before detachment (0.2 = 20%)")]
    public float maxStretchBeforeDetach = 0.2f; // epsilon threshold (stretch-based)

    [Header("Energy Transfer Parameters (from Paper)")]
    [Tooltip("Alpha: Energy transfer coefficient [0-1] physical, >1 for exaggeration")]
    [Range(0.0f, 2.0f)]
    public float alpha = 0.5f; // alpha - MAIN PARAMETER from paper!

    [Tooltip("Ratio of energy going to cube vs chain (0=all chain, 1=all cube)")]
    [Range(0.0f, 1.0f)]
    public float cubeEnergyRatio = 0.4f; // Energy split ratio

    [Header("Boundary Controls")]
    public float chainStartHeight = 5.0f;
    public GameObject topCube;
    public GameObject bottomCube;
    public float cubeSpeed = 3.0f;
    public bool bottomCubeAttached = true;

    [Header("Visualization")]
    public Color linkColor = Color.white;
    public Color constraintColor = Color.green;
    public Color strainedColor = Color.yellow;
    public Color detachedColor = Color.cyan;
    public bool showDebugInfo = true;

    private List<ChainLink> links = new List<ChainLink>();
    private List<ChainConstraint> constraints = new List<ChainConstraint>();
    private bool initialized = false;
    private Vec3 bottomCubeVelocity = Vec3.Zero;

    void Start()
    {
        InitializeChain();
    }

    void InitializeChain()
    {
        links.Clear();
        constraints.Clear();
        bottomCubeAttached = true;
        bottomCubeVelocity = Vec3.Zero;

        if (topCube == null)
        {
            topCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topCube.name = "TopCube_ANCHOR";
            topCube.transform.localScale = Vector3.one * 0.3f;
            topCube.GetComponent<Renderer>().material.color = Color.cyan;
            Destroy(topCube.GetComponent<Collider>());
            topCube.transform.position = new Vector3(0, chainStartHeight, 0);
        }

        if (bottomCube == null)
        {
            bottomCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bottomCube.name = "BottomCube_WEIGHT";
            bottomCube.transform.localScale = Vector3.one * 0.3f;
            bottomCube.GetComponent<Renderer>().material.color = Color.magenta;
            Destroy(bottomCube.GetComponent<Collider>());
        }

        for (int i = 0; i < numberOfLinks; i++)
        {
            float yPos = chainStartHeight - (i * linkSpacing);
            Vec3 pos = new Vec3(0, yPos, 0);
            ChainLink link = new ChainLink(pos, linkMass, linkSize);
            links.Add(link);
        }

        for (int i = 0; i < links.Count - 1; i++)
        {
            ChainConstraint constraint = new ChainConstraint(
                links[i],
                links[i + 1],
                constraintStiffness, 
                constraintDamping,   
                float.MaxValue       
            );
            constraint.energyTransfer = alpha;
            constraint.broken = false;
            constraints.Add(constraint);
        }

        if (bottomCube != null && links.Count > 0)
        {
            bottomCube.transform.position = links[links.Count - 1].position.ToUnityVec3();
        }

        initialized = true;
        Debug.Log($"Chain initialized! alpha={alpha}, K={constraintStiffness}, B={constraintDamping}, stretch_threshold={maxStretchBeforeDetach * 100}%");
    }

    void Update()
    {
        if (!initialized) return;

        float dt = Time.deltaTime;
        if (dt > 0.016f) dt = 0.016f;

        HandleInput();
        PhysicsStep(dt);
    }

    void PhysicsStep(float dt)
    {
        LockTopLink();

        if (bottomCubeAttached)
        {
            LockBottomLink();
            CheckForAutoDetach(); 
        }
        else
        {
            UpdateDetachedCube(dt);
        }

        ClearForces();
        ApplyGravity();
        IntegrateMotion(dt);

        for (int i = 0; i < constraintSolverIterations; i++)
        {
            EnforceRigidConstraints();
        }

        if (bottomCubeAttached && bottomCube != null && links.Count > 0)
        {
            bottomCube.transform.position = links[links.Count - 1].position.ToUnityVec3();
        }
    }

    void LockTopLink()
    {
        if (topCube != null && links.Count > 0)
        {
            links[0].position = Vec3.FromUnityVec3(topCube.transform.position);
            links[0].velocity = Vec3.Zero;
        }
    }

    void LockBottomLink()
    {
        if (bottomCube != null && links.Count > 0)
        {
            links[links.Count - 1].position = Vec3.FromUnityVec3(bottomCube.transform.position);
            links[links.Count - 1].velocity = Vec3.Zero;
        }
    }

    void CheckForAutoDetach()
    {
        float maxStretch = 0f;

        foreach (var constraint in constraints)
        {
            float currentLength = (constraint.linkB.position - constraint.linkA.position).Magnitude();
            float stretch = (currentLength - constraint.restLength) / constraint.restLength;
            maxStretch = Mathf.Max(maxStretch, stretch);
        }

        if (maxStretch > maxStretchBeforeDetach)
        {
            DetachBottomCubeWithEnergy();
        }
    }

    void DetachBottomCubeWithEnergy()
    {
        if (!bottomCubeAttached) return;

        Debug.Log("=== ENERGIZED DETACHMENT ===");

        float totalEnergy = CalculateStoredEnergy();

        Debug.Log($"Total stored energy: {totalEnergy:F2} J");

        float cubeEnergy = totalEnergy * cubeEnergyRatio * alpha;
        float chainEnergy = totalEnergy * (1.0f - cubeEnergyRatio) * alpha;

        Vec3 detachDirection = (Vec3.FromUnityVec3(bottomCube.transform.position) - links[links.Count - 1].position).Normalized();

        float cubeMass = linkMass * 2.0f;
        float cubeImpulseMag = Mathf.Sqrt(2.0f * cubeEnergy / cubeMass);

        bottomCubeVelocity = detachDirection * cubeImpulseMag;

        Debug.Log($"Cube launched with impulse: {cubeImpulseMag:F2} m/s");

        ApplySnapBackToChain(chainEnergy);

        if (bottomCube != null)
        {
            bottomCube.GetComponent<Renderer>().material.color = detachedColor;
        }

        bottomCubeAttached = false;
    }

    float CalculateStoredEnergy()
    {
        float totalEnergy = 0f;

        foreach (var constraint in constraints)
        {
            float stretch = constraint.GetStretch();
            float energy = 0.5f * constraintStiffness * stretch * stretch;
            totalEnergy += Mathf.Abs(energy);
        }

        for (int i = 1; i < links.Count; i++)
        {
            float restY = chainStartHeight - (i * linkSpacing);
            float displacement = links[i].position.y - restY;
            float potentialEnergy = links[i].mass * Mathf.Abs(gravity) * Mathf.Abs(displacement);
            totalEnergy += potentialEnergy;
        }

        return totalEnergy;
    }

    void ApplySnapBackToChain(float chainEnergy)
    {
        int numFreeLinks = links.Count - 1;
        if (numFreeLinks <= 0) return;

        float energyPerLink = chainEnergy / numFreeLinks;
        float impulsePerLink = Mathf.Sqrt(2.0f * energyPerLink / linkMass);

        Debug.Log($"Chain snap-back impulse: {impulsePerLink:F2} per link");

        for (int i = 1; i < links.Count; i++)
        {
            ChainLink link = links[i];
            float positionRatio = (float)i / (links.Count - 1);

            Vec3 upwardImpulse = Vec3.Up * impulsePerLink * (1.5f - positionRatio * 0.5f);

            float wavePhase = positionRatio * Mathf.PI;
            Vec3 waveImpulse = new Vec3(
                Mathf.Sin(wavePhase * 3.0f) * impulsePerLink * 0.4f,
                0,
                Mathf.Cos(wavePhase * 3.0f) * impulsePerLink * 0.2f
            );

            link.ApplyImpulse(upwardImpulse + waveImpulse);
        }
    }

    void UpdateDetachedCube(float dt)
    {
        if (bottomCube == null) return;

        // Apply gravity to cube
        Vec3 gravityVec = new Vec3(0, gravity, 0);
        bottomCubeVelocity = bottomCubeVelocity + gravityVec * dt;
        bottomCubeVelocity = bottomCubeVelocity * 0.99f; // Air resistance

        // Update position
        Vec3 cubePos = Vec3.FromUnityVec3(bottomCube.transform.position);
        cubePos = cubePos + bottomCubeVelocity * dt;
        bottomCube.transform.position = cubePos.ToUnityVec3();
    }

    void ClearForces()
    {
        foreach (var link in links)
        {
            link.ClearForces();
        }
    }

    void ApplyGravity()
    {
        Vec3 gravityForce = new Vec3(0, gravity, 0);

        for (int i = 1; i < links.Count; i++)
        {
            if (i == links.Count - 1 && bottomCubeAttached)
                continue;

            links[i].AddForce(gravityForce * links[i].mass);
        }
    }

    void IntegrateMotion(float dt)
    {
        for (int i = 1; i < links.Count; i++)
        {
            var link = links[i];

            if (i == links.Count - 1 && bottomCubeAttached)
                continue;

            Vec3 acceleration = link.force / link.mass;
            link.velocity = link.velocity + acceleration * dt;
            link.velocity = link.velocity * airDamping;
            link.position = link.position + link.velocity * dt;
        }
    }

    void EnforceRigidConstraints()
    {
        foreach (var constraint in constraints)
        {
            ChainLink a = constraint.linkA;
            ChainLink b = constraint.linkB;

            Vec3 delta = b.position - a.position;
            float currentLength = delta.Magnitude();

            if (currentLength < 0.0001f) continue;

            Vec3 direction = delta / currentLength;
            float error = currentLength - constraint.restLength;

            if (Mathf.Abs(error) < 0.00001f) continue;

            int indexA = links.IndexOf(a);
            int indexB = links.IndexOf(b);

            bool lockedA = (indexA == 0) || (indexA == links.Count - 1 && bottomCubeAttached);
            bool lockedB = (indexB == 0) || (indexB == links.Count - 1 && bottomCubeAttached);

            Vec3 correction = direction * error;

            if (!lockedA && !lockedB)
            {
                a.position = a.position + correction * 0.5f;
                b.position = b.position - correction * 0.5f;
            }
            else if (lockedA && !lockedB)
            {
                b.position = b.position - correction;
            }
            else if (!lockedA && lockedB)
            {
                a.position = a.position + correction;
            }
        }
    }

    void HandleInput()
    {
        float moveAmount = cubeSpeed * Time.deltaTime;

        if (topCube != null)
        {
            if (Input.GetKey(KeyCode.UpArrow))
                topCube.transform.position += Vector3.up * moveAmount * 0.3f;
            if (Input.GetKey(KeyCode.DownArrow))
                topCube.transform.position += Vector3.down * moveAmount * 0.3f;
            if (Input.GetKey(KeyCode.LeftArrow))
                topCube.transform.position += Vector3.left * moveAmount * 0.3f;
            if (Input.GetKey(KeyCode.RightArrow))
                topCube.transform.position += Vector3.right * moveAmount * 0.3f;
        }

        if (bottomCube != null && bottomCubeAttached)
        {
            if (Input.GetKey(KeyCode.W))
                bottomCube.transform.position += Vector3.up * moveAmount;
            if (Input.GetKey(KeyCode.S))
                bottomCube.transform.position += Vector3.down * moveAmount;
            if (Input.GetKey(KeyCode.A))
                bottomCube.transform.position += Vector3.left * moveAmount;
            if (Input.GetKey(KeyCode.D))
                bottomCube.transform.position += Vector3.right * moveAmount;
        }

        if (Input.GetKeyDown(KeyCode.Space) && bottomCubeAttached)
        {
            DetachBottomCubeWithEnergy();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            InitializeChain();
        }
    }

    void OnDrawGizmos()
    {
        if (!initialized || links == null || links.Count == 0) return;

        Gizmos.color = linkColor;
        foreach (var link in links)
        {
            Gizmos.DrawWireCube(link.position.ToUnityVec3(), Vector3.one * link.size);
        }
        foreach (var constraint in constraints)
        {
            float currentLength = (constraint.linkB.position - constraint.linkA.position).Magnitude();
            float stretch = (currentLength - constraint.restLength) / constraint.restLength;

            if (stretch < 0.05f)
                Gizmos.color = constraintColor;
            else if (stretch < maxStretchBeforeDetach * 0.7f)
                Gizmos.color = strainedColor;
            else
                Gizmos.color = Color.red;

            Gizmos.DrawLine(
                constraint.linkA.position.ToUnityVec3(),
                constraint.linkB.position.ToUnityVec3()
            );
        }

        Gizmos.color = Color.yellow;
        foreach (var link in links)
        {
            Gizmos.DrawSphere(link.position.ToUnityVec3(), 0.04f);
        }
    }

    void OnGUI()
    {
        if (!showDebugInfo) return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 24;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.white;

        float maxStretch = 0f;
        float maxForce = 0f;

        if (bottomCubeAttached && constraints.Count > 0)
        {
            foreach (var c in constraints)
            {
                float currentLength = (c.linkB.position - c.linkA.position).Magnitude();
                float stretch = (currentLength - c.restLength) / c.restLength;
                maxStretch = Mathf.Max(maxStretch, stretch);

                float stretchAbs = currentLength - c.restLength;
                float force = constraintStiffness * stretchAbs;
                maxForce = Mathf.Max(maxForce, Mathf.Abs(force));
            }
        }

        float currentEnergy = bottomCubeAttached ? CalculateStoredEnergy() : 0f;

        int y = 15;
        int lineHeight = 30; 

        GUI.Label(new Rect(15, y, 500, lineHeight), $"Alpha (energy transfer): {alpha:F2}", style);
        y += lineHeight;

        GUI.Label(new Rect(15, y, 500, lineHeight), $"K (stiffness): {constraintStiffness:F0} N/m", style);
        y += lineHeight;

        GUI.Label(new Rect(15, y, 500, lineHeight), $"Max constraint force: {maxForce:F1} N", style);
        y += lineHeight;

        GUI.Label(new Rect(15, y, 500, lineHeight), $"Stretch: {maxStretch * 100:F1}% / {maxStretchBeforeDetach * 100:F0}%", style);
        y += lineHeight;

        GUI.Label(new Rect(15, y, 500, lineHeight), $"Stored energy: {currentEnergy:F2} J", style);
        y += lineHeight;

        y += 10;
        GUI.Label(new Rect(15, y, 500, lineHeight), $"Status: {(bottomCubeAttached ? "Attached" : "Detached")}", style);
    }


}
