using UnityEngine;
using System.Collections.Generic;

namespace YassineBM
{
    /// <summary>
    /// Main scene setup that creates and manages multiple fragment spheres
    /// </summary>
    public class SceneSetup : MonoBehaviour
    {
        [Header("Sphere Configuration")]
        [SerializeField] private int fragmentsPerSphere = 50;
        [SerializeField] private float sphereRadius = 1f;
        [SerializeField] private float fragmentSize = 0.1f;
        
        [Header("Sphere Spawning")]
        [SerializeField] private int numberOfSpheres = 3;
        [SerializeField] private Vector3 spawnAreaSize = new Vector3(10f, 5f, 10f);
        [SerializeField] private Vector3 spawnCenter = new Vector3(0, 5f, 0);
        
        [Header("Physics")]
        [SerializeField] private float gravity = 9.81f;
        [SerializeField] private float groundLevel;
        [SerializeField] private float fragmentMass = 0.1f;
        [SerializeField] private float restitution = 0.5f; // Bounciness (0 = no bounce, 1 = perfect bounce)
        
        [Header("Collision Settings")]
        [SerializeField] private bool enableFragmentCollisions = true;
        [SerializeField] private bool createSpringConstraints = true;
        [SerializeField] private float initialConstraintDistance = 0.3f;
        
        [Header("Initial Velocity (Optional)")]
        [SerializeField] private bool applyRandomVelocity = false;
        [SerializeField] private float maxInitialVelocity = 2f;
        
        private List<FragmentSphere> fragmentSpheres = new List<FragmentSphere>();
        private GameObject platformObject;
        
        private void Start()
        {
            CreatePlatform();
            CreateFragmentSpheres();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            
            // Check sphere-to-sphere collisions first
            for (int i = 0; i < fragmentSpheres.Count; i++)
            {
                for (int j = i + 1; j < fragmentSpheres.Count; j++)
                {
                    CheckSphereToSphereCollision(fragmentSpheres[i], fragmentSpheres[j]);
                }
            }
            
            // Update all fragment spheres
            foreach (var sphere in fragmentSpheres)
            {
                sphere.UpdatePhysics(deltaTime, gravity, groundLevel, restitution, enableFragmentCollisions);
            }
        }
        
        /// <summary>
        /// Check and resolve collision between two fragment spheres
        /// </summary>
        private void CheckSphereToSphereCollision(FragmentSphere sphereA, FragmentSphere sphereB)
        {
            Vector3 centerA = sphereA.GetCenter();
            Vector3 centerB = sphereB.GetCenter();
            float radiusA = sphereA.GetRadius();
            float radiusB = sphereB.GetRadius();
            
            Vector3 delta = centerB - centerA;
            float distance = delta.magnitude;
            float minDistance = radiusA + radiusB;
            
            if (distance < minDistance && distance > 0.001f)
            {
                // Collision detected between spheres
                Vector3 normal = delta / distance;
                float overlap = minDistance - distance;
                
                // Get average mass of fragments in each sphere
                float massA = sphereA.GetTotalMass();
                float massB = sphereB.GetTotalMass();
                float totalMass = massA + massB;
                
                // Calculate separation vector
                Vector3 separationA = -normal * (overlap * (massB / totalMass));
                Vector3 separationB = normal * (overlap * (massA / totalMass));
                
                // Apply separation to all fragments in each sphere
                sphereA.ApplyPositionOffset(separationA);
                sphereB.ApplyPositionOffset(separationB);
                
                // Calculate relative velocity between sphere centers
                Vector3 velocityA = sphereA.GetAverageVelocity();
                Vector3 velocityB = sphereB.GetAverageVelocity();
                Vector3 relativeVelocity = velocityB - velocityA;
                float velocityAlongNormal = Vector3.Dot(relativeVelocity, normal);
                
                // Only apply impulse if spheres are moving towards each other
                if (velocityAlongNormal < 0)
                {
                    // Calculate impulse with coefficient of restitution
                    float impulseMagnitude = -(1 + restitution) * velocityAlongNormal;
                    impulseMagnitude /= (1 / massA + 1 / massB);
                    
                    Vector3 impulse = normal * impulseMagnitude;
                    
                    // Distribute impulse among all fragments (divide by fragment count)
                    sphereA.ApplyDistributedImpulse(-impulse);
                    sphereB.ApplyDistributedImpulse(impulse);
                }
            }
        }

        private void CreatePlatform()
        {
            platformObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platformObject.name = "Platform";
            platformObject.transform.position = new Vector3(0, groundLevel - 0.5f, 0);
            platformObject.transform.localScale = new Vector3(40f, 1f, 40f); // Bigger platform (was 20x20)
            
            // Remove the box collider (we use custom ground collision)
            Collider collider = platformObject.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }
            
            // Set platform material
            Renderer rend = platformObject.GetComponent<Renderer>();
            
            // Try multiple possible URP shader names
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("URP/Lit");
            if (shader == null) shader = Shader.Find("Shader Graphs/URPLit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Standard");
            
            Material mat = new Material(shader);
            Color platformColor = new Color(0.2f, 0.25f, 0.3f); // Dark blue-gray
            mat.color = platformColor;
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", platformColor); // URP property
            }
            rend.sharedMaterial = mat;
            
            Debug.Log("Platform created");
        }

        private void CreateFragmentSpheres()
        {
            for (int s = 0; s < numberOfSpheres; s++)
            {
                // Random position in spawn area
                Vector3 spawnPosition = spawnCenter + new Vector3(
                    Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
                    Random.Range(0, spawnAreaSize.y),
                    Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
                );
                
                FragmentSphere sphere = new FragmentSphere();
                sphere.Initialize(
                    spawnPosition,
                    sphereRadius,
                    fragmentsPerSphere,
                    fragmentSize,
                    fragmentMass,
                    transform,
                    createSpringConstraints,
                    initialConstraintDistance
                );
                
                // Apply random initial velocity if enabled
                if (applyRandomVelocity)
                {
                    Vector3 randomVelocity = new Vector3(
                        Random.Range(-maxInitialVelocity, maxInitialVelocity),
                        Random.Range(-maxInitialVelocity, maxInitialVelocity),
                        Random.Range(-maxInitialVelocity, maxInitialVelocity)
                    );
                    sphere.SetInitialVelocity(randomVelocity);
                }
                
                fragmentSpheres.Add(sphere);
                
                Debug.Log($"Created fragment sphere {s + 1} at {spawnPosition} with {fragmentsPerSphere} fragments");
            }
        }

        private void OnGUI()
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 16;
            style.normal.textColor = Color.white;
            
            GUI.Label(new Rect(10, 10, 400, 30), $"Fragment Spheres: {fragmentSpheres.Count}", style);
            
            int totalFragments = 0;
            foreach (var sphere in fragmentSpheres)
            {
                totalFragments += sphere.GetFragmentCount();
            }
            GUI.Label(new Rect(10, 30, 400, 30), $"Total Fragments: {totalFragments}", style);
            GUI.Label(new Rect(10, 50, 400, 30), $"Gravity: {gravity} m/s²", style);
            GUI.Label(new Rect(10, 70, 400, 30), $"Collisions: {(enableFragmentCollisions ? "Enabled" : "Disabled")}", style);
            GUI.Label(new Rect(10, 90, 400, 30), $"Restitution: {restitution:F2} ({GetRestitutionDescription()})", style);
            GUI.Label(new Rect(10, 110, 400, 30), $"Fragment Mass: {fragmentMass:F2} kg", style);
        }
        
        private string GetRestitutionDescription()
        {
            if (restitution >= 0.95f) return "Perfect Bounce";
            if (restitution >= 0.7f) return "High Bounce";
            if (restitution >= 0.4f) return "Medium Bounce";
            if (restitution >= 0.15f) return "Low Bounce";
            return "No Bounce";
        }

        /// <summary>
        /// Represents a sphere made of fragments
        /// </summary>
        private class FragmentSphere
        {
            private List<Fragment> fragments = new List<Fragment>();
            private CollisionSystem collisionSystem;
            private Vector3 center;
            private float sphereRadius;
            private float fragmentRadius;
            private Color sphereBaseColor;

            public void Initialize(
                Vector3 sphereCenter,
                float radius,
                int fragmentCount,
                float fragmentSize,
                float fragmentMass,
                Transform parent,
                bool createConstraints,
                float constraintDistance)
            {
                this.center = sphereCenter;
                this.sphereRadius = radius;
                this.fragmentRadius = fragmentSize / 2f;
                
                // Generate a base color for this sphere
                sphereBaseColor = GenerateDistinctColor();
                Debug.Log($"Sphere at {sphereCenter} has base color: {sphereBaseColor}");
                
                // Create fragments in a spherical distribution
                for (int i = 0; i < fragmentCount; i++)
                {
                    Vector3 position = GenerateSpherePoint(i, fragmentCount, radius);
                    position += sphereCenter;
                    
                    // Create fragment game object
                    GameObject fragmentObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    fragmentObj.name = $"Fragment_{i}";
                    fragmentObj.transform.parent = parent;
                    fragmentObj.transform.localScale = Vector3.one * fragmentSize;
                    
                    // Remove the sphere collider component (we use custom collision)
                    Collider collider = fragmentObj.GetComponent<Collider>();
                    if (collider != null)
                    {
                        Object.Destroy(collider);
                    }
                    
                    // Add fragment component
                    Fragment fragment = fragmentObj.AddComponent<Fragment>();
                    
                    // Create color variation from sphere base color
                    Color color = new Color(
                        Mathf.Clamp01(sphereBaseColor.r + Random.Range(-0.1f, 0.1f)),
                        Mathf.Clamp01(sphereBaseColor.g + Random.Range(-0.1f, 0.1f)),
                        Mathf.Clamp01(sphereBaseColor.b + Random.Range(-0.1f, 0.1f)),
                        1f
                    );
                    
                    // Initialize fragment
                    fragment.Initialize(position, fragmentMass, fragmentSize / 2f, color);
                    
                    fragments.Add(fragment);
                }
                
                // Create collision system
                collisionSystem = new CollisionSystem(fragments);
                
                if (createConstraints)
                {
                    collisionSystem.CreateInitialConstraints(constraintDistance);
                }
            }

            /// <summary>
            /// Generate a distinct color for the sphere
            /// </summary>
            private Color GenerateDistinctColor()
            {
                // Predefined vibrant colors that work well in URP
                Color[] distinctColors = new Color[]
                {
                    new Color(1.0f, 0.2f, 0.2f),  // Bright Red
                    new Color(0.2f, 0.6f, 1.0f),  // Sky Blue
                    new Color(0.3f, 1.0f, 0.3f),  // Bright Green
                    new Color(1.0f, 0.8f, 0.0f),  // Golden Yellow
                    new Color(1.0f, 0.4f, 0.8f),  // Pink
                    new Color(0.6f, 0.2f, 1.0f),  // Purple
                    new Color(1.0f, 0.5f, 0.0f),  // Orange
                    new Color(0.0f, 0.9f, 0.9f),  // Cyan
                    new Color(0.9f, 0.9f, 0.2f),  // Lime
                    new Color(1.0f, 0.2f, 0.5f),  // Hot Pink
                };
                
                // Pick a random color from the palette
                return distinctColors[Random.Range(0, distinctColors.Length)];
            }

            /// <summary>
            /// Generate evenly distributed points on a sphere using Fibonacci sphere algorithm
            /// </summary>
            private Vector3 GenerateSpherePoint(int index, int total, float radius)
            {
                float phi = Mathf.PI * (3f - Mathf.Sqrt(5f)); // Golden angle
                float y = 1f - (index / (float)(total - 1)) * 2f;
                float radiusAtY = Mathf.Sqrt(1f - y * y);
                float theta = phi * index;
                
                float x = Mathf.Cos(theta) * radiusAtY;
                float z = Mathf.Sin(theta) * radiusAtY;
                
                return new Vector3(x, y, z) * radius;
            }

            /// <summary>
            /// Set initial velocity for all fragments in the sphere
            /// </summary>
            public void SetInitialVelocity(Vector3 velocity)
            {
                foreach (var fragment in fragments)
                {
                    fragment.rigidBody.velocity = velocity;
                }
            }

            public void UpdatePhysics(float deltaTime, float gravity, float groundLevel, float restitution, bool enableCollisions)
            {
                // Apply gravity to all fragments
                foreach (var fragment in fragments)
                {
                    fragment.ApplyGravity(gravity);
                }
                
                // Update collision system
                if (enableCollisions)
                {
                    collisionSystem.UpdateCollisions(deltaTime);
                }
                
                // Calculate damping multiplier based on restitution
                // restitution = 1.0 -> dampingMultiplier = 0.0 (no damping)
                // restitution = 0.0 -> dampingMultiplier = 1.0 (full damping)
                float dampingMultiplier = 1.0f - restitution;
                
                // Integrate physics and update transforms
                foreach (var fragment in fragments)
                {
                    fragment.rigidBody.Integrate(deltaTime, dampingMultiplier);
                    fragment.HandleGroundCollision(groundLevel, restitution);
                    fragment.UpdateTransform();
                }
            }

            public int GetFragmentCount()
            {
                return fragments.Count;
            }
            
            /// <summary>
            /// Get the current center of the sphere (average position of all fragments)
            /// </summary>
            public Vector3 GetCenter()
            {
                if (fragments.Count == 0) return center;
                
                Vector3 sum = Vector3.zero;
                foreach (var fragment in fragments)
                {
                    sum += fragment.rigidBody.position;
                }
                return sum / fragments.Count;
            }
            
            /// <summary>
            /// Get the approximate radius of the sphere
            /// </summary>
            public float GetRadius()
            {
                // Return initial radius plus fragment radius for collision bounds
                return sphereRadius + fragmentRadius;
            }
            
            /// <summary>
            /// Get total mass of all fragments
            /// </summary>
            public float GetTotalMass()
            {
                float totalMass = 0f;
                foreach (var fragment in fragments)
                {
                    totalMass += fragment.rigidBody.mass;
                }
                return totalMass;
            }
            
            /// <summary>
            /// Get average velocity of all fragments
            /// </summary>
            public Vector3 GetAverageVelocity()
            {
                if (fragments.Count == 0) return Vector3.zero;
                
                Vector3 sum = Vector3.zero;
                foreach (var fragment in fragments)
                {
                    sum += fragment.rigidBody.velocity;
                }
                return sum / fragments.Count;
            }
            
            /// <summary>
            /// Apply position offset to all fragments
            /// </summary>
            public void ApplyPositionOffset(Vector3 offset)
            {
                foreach (var fragment in fragments)
                {
                    fragment.rigidBody.position += offset;
                }
            }
            
            /// <summary>
            /// Apply impulse to all fragments
            /// </summary>
            public void ApplyImpulse(Vector3 impulsePerFragment)
            {
                foreach (var fragment in fragments)
                {
                    fragment.rigidBody.ApplyImpulse(impulsePerFragment);
                }
            }
            
            /// <summary>
            /// Apply total impulse distributed evenly among all fragments
            /// </summary>
            public void ApplyDistributedImpulse(Vector3 totalImpulse)
            {
                if (fragments.Count == 0) return;
                
                // Divide impulse by number of fragments
                Vector3 impulsePerFragment = totalImpulse / fragments.Count;
                
                foreach (var fragment in fragments)
                {
                    fragment.rigidBody.ApplyImpulse(impulsePerFragment);
                }
            }
        }
    }
}

