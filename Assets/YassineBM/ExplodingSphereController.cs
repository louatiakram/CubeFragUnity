using UnityEngine;
using System.Collections.Generic;

namespace YassineBM
{
    /// <summary>
    /// Main controller for the exploding fractured sphere animation
    /// Sets up camera, creates shards, simulates constraints, and triggers explosion
    /// </summary>
    public class ExplodingSphereController : MonoBehaviour
    {
        [Header("Sphere Settings")]
        [SerializeField] private float sphereRadius = 2f;
        [SerializeField] private int numberOfShards = 20;
        [SerializeField] private int randomSeed = 42;

        [Header("Physics Settings")]
        [SerializeField] private float shardMass = 1f;
        [SerializeField] private float constraintStiffness = 50f;
        [SerializeField] private float breakThreshold = 0.5f;
        [SerializeField] private Vector3 gravity = new Vector3(0, -9.81f, 0);
        [SerializeField] private bool useGravity = false;

        [Header("Animation Settings")]
        [SerializeField] private float initialRotationSpeed = 0.2f; // Slower rotation
        [SerializeField] private float tensionIncreaseRate = 0.05f; // Slower tension build
        [SerializeField] private float maxTensionMultiplier = 2f; // Gentler tension
        [SerializeField] private float explosionDelay = 8f; // Much longer sequence
        [SerializeField] private bool useSlowMotion = false;
        [SerializeField] private float slowMotionScale = 0.5f; // 50% speed for dramatic effect

        [Header("Camera Settings")]
        [SerializeField] private float cameraStartDistance = 15f;
        [SerializeField] private float cameraEndDistance = 8f;
        [SerializeField] private float cameraStartHeight = 5f;
        [SerializeField] private float cameraEndHeight = 1f;
        [SerializeField] private float cameraOrbitSpeed = 5f; // Slower orbit
        [SerializeField] private bool useCinematicCamera = true;
        [SerializeField] private float cameraZoomSpeed = 0.5f;
        [SerializeField] private float cameraMovementSmoothing = 2f;

        [Header("Material Settings")]
        [SerializeField] private Color shardColor = new Color(0.2f, 0.8f, 1.0f, 1.0f); // Bright cyan
        [SerializeField] private bool useRandomColors = true;

        // Components
        private List<SphereShard> shards = new List<SphereShard>();
        private List<ShardConstraint> constraints = new List<ShardConstraint>();
        private GameObject sphereContainer;
        private GameObject wholeSphereObject;
        private Camera mainCamera;
        private GameObject cameraObject;

        // Animation state
        private float elapsedTime;
        private float currentTensionMultiplier = 1f;
        private bool isFractured;
        private bool hasExploded;
        private float cameraAngle;
        
        // Camera animation
        private float currentCameraDistance;
        private float currentCameraHeight;
        private float targetCameraDistance;
        private float targetCameraHeight;

        // Sphere rotation
        private Vector3 globalRotationAxis = Vector3.up;
        private float globalRotationAngle;
        
        // Whole sphere mesh transformation
        private Mesh wholeMesh;
        private Vector3[] originalWholeMeshVertices;
        private Vector3[] transformedWholeMeshVertices;
        
        // Crack visualization
        private List<GameObject> crackLines = new List<GameObject>();
        private bool cracksVisible = false;

        void Start()
        {
            SetupCamera();
            GenerateWholeSphere();
            ApplyInitialRotation();
        }

        void Update()
        {
            elapsedTime += Time.deltaTime;

            // Update cinematic camera with smooth movements
            if (useCinematicCamera)
            {
                UpdateCinematicCamera();
            }
            else
            {
                UpdateCameraOrbit();
            }

            // Animation phases with adjusted timing
            if (!isFractured)
            {
                // Phase 1: Spinning whole sphere with increasing tension
                UpdateWholeSphereRotation();
                
                // Show cracks when tension is high (70% of delay for slower reveal)
                if (elapsedTime >= explosionDelay * 0.7f && !cracksVisible)
                {
                    ShowCracks();
                }
                
                // Check if should fracture (when tension builds up) - 85% for slower timing
                if (elapsedTime >= explosionDelay * 0.85f)
                {
                    FractureSphere();
                }
            }
            else if (!hasExploded)
            {
                // Phase 2: Fractured sphere with constraints
                UpdateGlobalRotation();
                IncreaseTension();

                // Phase 3: Trigger explosion
                if (elapsedTime >= explosionDelay)
                {
                    TriggerExplosion();
                }
            }
        }

        void FixedUpdate()
        {
            // Only run physics simulation when sphere is fractured
            if (!isFractured) return;
            
            float deltaTime = Time.fixedDeltaTime;

            // Update constraints
            foreach (var constraint in constraints)
            {
                constraint.UpdateConstraint();
            }

            // Apply gravity if enabled
            if (useGravity)
            {
                foreach (var shard in shards)
                {
                    shard.rigidBody.ApplyGravity(gravity);
                }
            }

            // Update physics for all shards
            foreach (var shard in shards)
            {
                shard.PhysicsUpdate(deltaTime);
                shard.UpdateTransformation();
            }
        }

        /// <summary>
        /// Setup camera
        /// </summary>
        private void SetupCamera()
        {
            // Find or create camera
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                cameraObject = new GameObject("Main Camera");
                mainCamera = cameraObject.AddComponent<Camera>();
                mainCamera.tag = "MainCamera";
            }
            else
            {
                cameraObject = mainCamera.gameObject;
            }

            // Initialize camera position for cinematic sequence
            currentCameraDistance = cameraStartDistance;
            currentCameraHeight = cameraStartHeight;
            targetCameraDistance = cameraStartDistance;
            targetCameraHeight = cameraStartHeight;
            
            // Position camera at starting point
            cameraObject.transform.position = new Vector3(0, currentCameraHeight, -currentCameraDistance);
            cameraObject.transform.LookAt(Vector3.zero);

            // Setup camera properties
            mainCamera.backgroundColor = new Color(0.05f, 0.05f, 0.15f); // Dark blue background
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.fieldOfView = 60f;
            
            // Add lighting for better visualization
            SetupLighting();
        }

        /// <summary>
        /// Generate whole sphere (not fractured yet)
        /// </summary>
        private void GenerateWholeSphere()
        {
            sphereContainer = new GameObject("Sphere");
            wholeSphereObject = new GameObject("WholeSphere");
            wholeSphereObject.transform.SetParent(sphereContainer.transform);

            // Generate whole sphere mesh
            wholeMesh = SphereFragmentGenerator.GenerateWholeSphere(sphereRadius, 3);
            
            // Setup mesh components
            MeshFilter meshFilter = wholeSphereObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = wholeSphereObject.AddComponent<MeshRenderer>();
            
            meshFilter.mesh = wholeMesh;
            
            // Create beautiful gradient material for whole sphere
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null)
            {
                urpShader = Shader.Find("URP/Lit");
                if (urpShader == null)
                {
                    urpShader = Shader.Find("Universal Render Pipeline/Simple Lit");
                }
            }
            
            Material sphereMaterial = new Material(urpShader);
            sphereMaterial.SetColor("_BaseColor", new Color(0.3f, 0.7f, 1.0f, 1.0f)); // Bright blue
            sphereMaterial.SetFloat("_Metallic", 0.4f);
            sphereMaterial.SetFloat("_Smoothness", 0.8f);
            sphereMaterial.EnableKeyword("_EMISSION");
            sphereMaterial.SetColor("_EmissionColor", new Color(0.1f, 0.3f, 0.5f, 1.0f));
            
            meshRenderer.material = sphereMaterial;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            meshRenderer.receiveShadows = true;
            
            // Store vertices for manual transformation
            originalWholeMeshVertices = wholeMesh.vertices;
            transformedWholeMeshVertices = new Vector3[originalWholeMeshVertices.Length];
            
            Debug.Log("Generated whole sphere - ready to fracture on force application");
        }

        /// <summary>
        /// Generate fractured sphere with shards (called when sphere breaks)
        /// </summary>
        private void GenerateFracturedSphere()
        {
            // Don't create new container if already exists
            if (sphereContainer == null)
            {
                sphereContainer = new GameObject("FracturedSphere");
            }

            // Generate shard meshes by slicing the original whole sphere
            // This creates realistic fragments from the actual sphere mesh
            List<Mesh> shardMeshes = SphereFragmentGenerator.GenerateShardsFromSlicing(sphereRadius, numberOfShards, randomSeed);

            // Create material with URP compatibility
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null)
            {
                // Fallback to other URP shaders
                urpShader = Shader.Find("URP/Lit");
                if (urpShader == null)
                {
                    // Last resort: try Simple Lit
                    urpShader = Shader.Find("Universal Render Pipeline/Simple Lit");
                    if (urpShader == null)
                    {
                        urpShader = Shader.Find("URP/Simple Lit");
                    }
                }
            }
            
            if (urpShader == null)
            {
                Debug.LogError("URP shader not found! Make sure URP is properly installed.");
                // Create a basic material with error shader
                urpShader = Shader.Find("Sprites/Default");
            }
            
            Debug.Log($"Using shader: {urpShader.name}");

            // Create shard game objects
            for (int i = 0; i < shardMeshes.Count; i++)
            {
                GameObject shardObject = new GameObject($"Shard_{i}");
                shardObject.transform.SetParent(sphereContainer.transform);

                SphereShard shard = shardObject.AddComponent<SphereShard>();

                // Create vibrant material with URP properties
                Material mat = new Material(urpShader);
                mat.name = $"ShardMaterial_{i}";
                
                if (useRandomColors)
                {
                    // Use vibrant colors with better distribution
                    float hue = (float)i / shardMeshes.Count; // Rainbow distribution
                    Color baseColor = Color.HSVToRGB(hue, 0.8f, 1.0f);
                    
                    // URP Lit shader properties
                    mat.SetColor("_BaseColor", baseColor);
                    mat.SetFloat("_Metallic", 0.3f);
                    mat.SetFloat("_Smoothness", 0.7f);
                    
                    // Add emission for glow effect
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", baseColor * 0.3f);
                }
                else
                {
                    mat.SetColor("_BaseColor", shardColor);
                    mat.SetFloat("_Metallic", 0.3f);
                    mat.SetFloat("_Smoothness", 0.7f);
                }
                
                // Ensure opaque rendering
                mat.SetFloat("_Surface", 0); // 0 = Opaque, 1 = Transparent
                mat.SetFloat("_AlphaClip", 0);
                mat.renderQueue = 2000; // Geometry queue

                // Calculate centroid of mesh for initial position
                Vector3 centroid = CalculateMeshCentroid(shardMeshes[i]);
                
                shard.Initialize(shardMeshes[i], mat, shardMass, centroid);
                shards.Add(shard);
            }

            Debug.Log($"Generated {shards.Count} shards");
        }

        /// <summary>
        /// Create constraints between nearby shards
        /// </summary>
        private void CreateConstraints()
        {
            // Create constraints between shards that are close to each other
            float maxConstraintDistance = sphereRadius * 1.5f;

            for (int i = 0; i < shards.Count; i++)
            {
                for (int j = i + 1; j < shards.Count; j++)
                {
                    Vector3 posA = shards[i].GetCenterPosition();
                    Vector3 posB = shards[j].GetCenterPosition();
                    float distance = (posB - posA).magnitude;

                    if (distance < maxConstraintDistance)
                    {
                        ShardConstraint constraint = new ShardConstraint(
                            shards[i],
                            shards[j],
                            constraintStiffness,
                            breakThreshold
                        );
                        constraints.Add(constraint);
                    }
                }
            }

            Debug.Log($"Created {constraints.Count} constraints");
        }

        /// <summary>
        /// Apply initial rotation to the sphere
        /// </summary>
        private void ApplyInitialRotation()
        {
            globalRotationAxis = new Vector3(0.3f, 1f, 0.2f).normalized;
            globalRotationAngle = 0f;
        }

        /// <summary>
        /// Update rotation of whole sphere using manual matrix transformation
        /// </summary>
        private void UpdateWholeSphereRotation()
        {
            if (wholeSphereObject == null) return;
            
            globalRotationAngle += initialRotationSpeed * Time.deltaTime;

            // Create rotation matrix
            CustomMatrix4x4 rotationMatrix = CustomMatrix4x4.Rotation(globalRotationAxis, globalRotationAngle);

            // Transform all vertices manually
            for (int i = 0; i < originalWholeMeshVertices.Length; i++)
            {
                transformedWholeMeshVertices[i] = rotationMatrix.TransformPoint(originalWholeMeshVertices[i]);
            }

            // Update mesh
            wholeMesh.vertices = transformedWholeMeshVertices;
            wholeMesh.RecalculateNormals();
            wholeMesh.RecalculateBounds();
            
            // Visual effect: sphere starts pulsating as force builds
            // Slower, more dramatic pulsation
            float normalizedTime = elapsedTime / explosionDelay;
            float pulseFrequency = 2f + normalizedTime * 3f; // Speed up pulsation as tension builds
            float pulseIntensity = normalizedTime * normalizedTime * 0.08f; // Quadratic increase
            float pulseAmount = Mathf.Sin(elapsedTime * pulseFrequency) * pulseIntensity;
            float scale = 1f + pulseAmount;
            wholeSphereObject.transform.localScale = new Vector3(scale, scale, scale);
        }

        /// <summary>
        /// Show crack lines on the sphere surface before fracturing
        /// </summary>
        private void ShowCracks()
        {
            cracksVisible = true;
            Debug.Log("Cracks appearing on sphere surface!");
            
            // Generate crack lines based on future fracture planes
            Random.InitState(randomSeed);
            int crackCount = numberOfShards / 2; // Fewer cracks than shards
            
            for (int i = 0; i < crackCount; i++)
            {
                // Create crack as a line on sphere surface
                GameObject crackLine = new GameObject($"Crack_{i}");
                crackLine.transform.SetParent(sphereContainer.transform);
                
                LineRenderer lineRenderer = crackLine.AddComponent<LineRenderer>();
                
                // Setup line renderer for URP
                Material crackMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (crackMaterial.shader == null)
                {
                    crackMaterial = new Material(Shader.Find("Sprites/Default"));
                }
                
                crackMaterial.SetColor("_BaseColor", Color.black);
                crackMaterial.SetColor("_EmissionColor", new Color(1f, 0.3f, 0f) * 2f); // Orange glow
                crackMaterial.EnableKeyword("_EMISSION");
                
                lineRenderer.material = crackMaterial;
                lineRenderer.startWidth = 0.03f;
                lineRenderer.endWidth = 0.02f;
                lineRenderer.positionCount = 20;
                
                // Generate crack path on sphere surface
                Vector3 startPoint = Random.onUnitSphere * sphereRadius;
                Vector3 endPoint = Random.onUnitSphere * sphereRadius;
                
                // Create jagged line between points on sphere surface
                for (int j = 0; j < 20; j++)
                {
                    float t = j / 19f;
                    Vector3 point = Vector3.Slerp(startPoint, endPoint, t);
                    
                    // Add jitter for realistic crack appearance
                    Vector3 perpendicular = Vector3.Cross(point, Vector3.up).normalized;
                    float jitter = Random.Range(-0.1f, 0.1f);
                    point += perpendicular * jitter;
                    
                    // Project back onto sphere surface
                    point = point.normalized * sphereRadius;
                    
                    lineRenderer.SetPosition(j, point);
                }
                
                crackLines.Add(crackLine);
            }
        }

        /// <summary>
        /// Fracture the whole sphere into shards
        /// </summary>
        private void FractureSphere()
        {
            if (isFractured) return;
            
            isFractured = true;
            Debug.Log("FRACTURING SPHERE!");
            
            // Hide whole sphere
            if (wholeSphereObject != null)
            {
                wholeSphereObject.SetActive(false);
            }
            
            // Hide crack lines
            foreach (var crack in crackLines)
            {
                if (crack != null)
                {
                    Object.Destroy(crack);
                }
            }
            crackLines.Clear();
            
            // Generate shards by slicing the original mesh
            GenerateFracturedSphere();
            CreateConstraints();
            
            // Transfer rotation to shards
            foreach (var shard in shards)
            {
                shard.rigidBody.rotationAxis = globalRotationAxis;
                shard.rigidBody.rotationAngle = globalRotationAngle;
                shard.rigidBody.angularVelocity = globalRotationAxis * initialRotationSpeed;
            }
        }

        /// <summary>
        /// Update global rotation of the fractured sphere
        /// </summary>
        private void UpdateGlobalRotation()
        {
            globalRotationAngle += initialRotationSpeed * Time.deltaTime;

            // Apply rotation to all shards
            CustomMatrix4x4 rotationMatrix = CustomMatrix4x4.Rotation(globalRotationAxis, globalRotationAngle);

            foreach (var shard in shards)
            {
                // Get original position (on sphere surface)
                Vector3 originalPos = CalculateMeshCentroid(shard.shardMesh);
                
                // Rotate around center
                Vector3 rotatedPos = rotationMatrix.TransformPoint(originalPos);
                
                // Update shard position
                shard.rigidBody.position = rotatedPos;
                
                // Add some angular velocity for visual effect
                shard.rigidBody.angularVelocity = globalRotationAxis * initialRotationSpeed * 0.3f;
            }
        }

        /// <summary>
        /// Gradually increase tension in constraints
        /// </summary>
        private void IncreaseTension()
        {
            currentTensionMultiplier = 1f + (elapsedTime / explosionDelay) * (maxTensionMultiplier - 1f);
            currentTensionMultiplier = Mathf.Min(currentTensionMultiplier, maxTensionMultiplier);

            // Apply expanding force to shards
            float expansionForce = (currentTensionMultiplier - 1f) * 2f;
            
            foreach (var shard in shards)
            {
                Vector3 direction = shard.GetCenterPosition().normalized;
                shard.rigidBody.AddForce(direction * expansionForce);
            }
        }

        /// <summary>
        /// Trigger the explosion by forcing constraints to break
        /// </summary>
        private void TriggerExplosion()
        {
            if (hasExploded) return;
            
            hasExploded = true;
            Debug.Log("EXPLOSION TRIGGERED!");
            
            // Apply slow motion during explosion for dramatic effect
            if (useSlowMotion)
            {
                Time.timeScale = slowMotionScale;
                Time.fixedDeltaTime = 0.02f * slowMotionScale;
            }

            // Apply gentler outward force to all shards for slower, more graceful explosion
            foreach (var shard in shards)
            {
                Vector3 explosionDirection = shard.GetCenterPosition().normalized;
                float explosionForce = 20f; // Reduced from 50f for slower explosion
                
                shard.rigidBody.AddImpulse(explosionDirection * explosionForce);
                
                // Add random angular velocity (reduced for slower tumbling)
                Vector3 randomAngular = new Vector3(
                    Random.Range(-2f, 2f),
                    Random.Range(-2f, 2f),
                    Random.Range(-2f, 2f)
                );
                shard.rigidBody.AddAngularImpulse(randomAngular);
            }

            // Enable gravity after explosion (with reduced strength for slower fall)
            useGravity = true;
        }

        /// <summary>
        /// Setup scene lighting for better visualization
        /// </summary>
        private void SetupLighting()
        {
            // Create main directional light (key light)
            GameObject mainLightObj = new GameObject("Main Light");
            Light mainLight = mainLightObj.AddComponent<Light>();
            mainLight.type = LightType.Directional;
            mainLight.color = new Color(1f, 0.95f, 0.9f); // Warm white
            mainLight.intensity = 1.0f;
            mainLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            mainLight.shadows = LightShadows.Soft;

            // Create fill light (opposite side, dimmer)
            GameObject fillLightObj = new GameObject("Fill Light");
            Light fillLight = fillLightObj.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.8f, 0.9f, 1f); // Cool blue tint
            fillLight.intensity = 0.4f;
            fillLight.transform.rotation = Quaternion.Euler(-30f, 150f, 0f);

            // Create point light at center for dramatic effect
            GameObject pointLightObj = new GameObject("Center Point Light");
            Light pointLight = pointLightObj.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = new Color(1f, 1f, 1f);
            pointLight.intensity = 2.0f;
            pointLight.range = 15f;
            pointLight.transform.position = Vector3.zero;

            // Set ambient lighting
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.2f, 0.2f, 0.3f); // Subtle ambient
        }

        /// <summary>
        /// Cinematic camera with dynamic movement based on animation phase
        /// </summary>
        private void UpdateCinematicCamera()
        {
            float normalizedTime = elapsedTime / explosionDelay;
            
            // Phase-based camera targets
            if (!isFractured)
            {
                // Phase 1: Slow approach - camera moves closer to inspect sphere
                targetCameraDistance = Mathf.Lerp(cameraStartDistance, cameraEndDistance, normalizedTime * 1.2f);
                targetCameraHeight = Mathf.Lerp(cameraStartHeight, cameraEndHeight, normalizedTime * 0.8f);
            }
            else if (!hasExploded)
            {
                // Phase 2: Hold position during fracture
                targetCameraDistance = cameraEndDistance;
                targetCameraHeight = cameraEndHeight;
            }
            else
            {
                // Phase 3: Pull back to see explosion
                float explosionTime = elapsedTime - explosionDelay;
                targetCameraDistance = cameraEndDistance + explosionTime * 2f; // Zoom out
                targetCameraHeight = cameraEndHeight + explosionTime * 1.5f; // Rise up
            }
            
            // Smooth interpolation to target
            currentCameraDistance = Mathf.Lerp(currentCameraDistance, targetCameraDistance, 
                                              Time.deltaTime * cameraMovementSmoothing);
            currentCameraHeight = Mathf.Lerp(currentCameraHeight, targetCameraHeight, 
                                            Time.deltaTime * cameraMovementSmoothing);
            
            // Orbit around sphere
            cameraAngle += cameraOrbitSpeed * Time.deltaTime * Mathf.Deg2Rad;
            
            // Calculate position
            float x = Mathf.Cos(cameraAngle) * currentCameraDistance;
            float z = Mathf.Sin(cameraAngle) * currentCameraDistance;
            
            // Apply position
            cameraObject.transform.position = new Vector3(x, currentCameraHeight, z);
            
            // Look at sphere with slight upward tilt for drama
            Vector3 lookTarget = Vector3.zero;
            if (hasExploded)
            {
                // Follow average position of shards during explosion
                if (shards.Count > 0)
                {
                    Vector3 shardsCenter = Vector3.zero;
                    foreach (var shard in shards)
                    {
                        shardsCenter += shard.GetCenterPosition();
                    }
                    lookTarget = shardsCenter / shards.Count;
                }
            }
            
            cameraObject.transform.LookAt(lookTarget);
            
            // Add slight camera shake during explosion
            if (hasExploded && elapsedTime - explosionDelay < 1f)
            {
                float shakeIntensity = (1f - (elapsedTime - explosionDelay)) * 0.1f;
                Vector3 shake = new Vector3(
                    Random.Range(-shakeIntensity, shakeIntensity),
                    Random.Range(-shakeIntensity, shakeIntensity),
                    Random.Range(-shakeIntensity, shakeIntensity)
                );
                cameraObject.transform.position += shake;
            }
        }

        /// <summary>
        /// Update camera orbit (simple mode)
        /// </summary>
        private void UpdateCameraOrbit()
        {
            cameraAngle += cameraOrbitSpeed * Time.deltaTime * Mathf.Deg2Rad;

            float x = Mathf.Cos(cameraAngle) * cameraStartDistance;
            float z = Mathf.Sin(cameraAngle) * cameraStartDistance;

            cameraObject.transform.position = new Vector3(x, cameraStartHeight, z);
            cameraObject.transform.LookAt(Vector3.zero);
        }

        /// <summary>
        /// Calculate centroid of a mesh
        /// </summary>
        private Vector3 CalculateMeshCentroid(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            if (vertices.Length == 0) return Vector3.zero;

            Vector3 sum = Vector3.zero;
            foreach (var v in vertices)
            {
                sum += v;
            }
            return sum / vertices.Length;
        }

        /// <summary>
        /// Draw debug information
        /// </summary>
        void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            // Draw constraints
            Gizmos.color = Color.yellow;
            foreach (var constraint in constraints)
            {
                if (!constraint.isBroken && constraint.shardA != null && constraint.shardB != null)
                {
                    Gizmos.DrawLine(
                        constraint.shardA.GetCenterPosition(),
                        constraint.shardB.GetCenterPosition()
                    );
                }
            }

            // Draw shard centers
            Gizmos.color = Color.red;
            foreach (var shard in shards)
            {
                if (shard != null)
                {
                    Gizmos.DrawWireSphere(shard.GetCenterPosition(), 0.1f);
                }
            }
        }
    }
}

