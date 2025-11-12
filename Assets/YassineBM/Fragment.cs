using UnityEngine;

namespace YassineBM
{
    /// <summary>
    /// Represents a single fragment with custom physics
    /// </summary>
    public class Fragment : MonoBehaviour
    {
        public RigidBodyCustom rigidBody;
        public Color fragmentColor;
        
        private Renderer fragmentRenderer;

        public void Initialize(Vector3 position, float mass, float radius, Color color)
        {
            rigidBody = new RigidBodyCustom(position, mass, radius);
            fragmentColor = color;
            
            // Set up visual
            fragmentRenderer = GetComponent<Renderer>();
            if (fragmentRenderer != null)
            {
                // Try multiple possible URP shader names
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("URP/Lit");
                if (shader == null) shader = Shader.Find("Shader Graphs/URPLit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader == null) shader = Shader.Find("Standard"); // Ultimate fallback
                
                // Create and assign new material
                Material mat = new Material(shader);
                mat.color = fragmentColor; // Works for most shaders
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", fragmentColor); // URP property
                }
                fragmentRenderer.sharedMaterial = mat;
            }
        }

        public void UpdateTransform()
        {
            // Apply transformation from custom matrix
            Matrix4x4Custom matrix = rigidBody.GetTransformMatrix();
            
            // Apply position
            Vector3 newPosition = matrix.GetPosition();
            transform.SetPositionAndRotation(newPosition, rigidBody.rotation);
        }

        public void ApplyExplosionForce(Vector3 center, float force)
        {
            Vector3 direction = (rigidBody.position - center).normalized;
            float distance = Vector3.Distance(rigidBody.position, center);
            
            // Calculate impulse based on distance (closer = stronger)
            float falloff = Mathf.Max(0.1f, 1.0f - (distance / 5.0f));
            Vector3 impulse = direction * force * falloff;
            
            rigidBody.ApplyImpulse(impulse);
            
            // Add some random angular velocity for visual effect
            rigidBody.angularVelocity = Random.insideUnitSphere * 5f;
        }

        public void ApplyGravity(float gravity)
        {
            rigidBody.ApplyForce(Vector3.down * gravity * rigidBody.mass);
        }

        public void HandleGroundCollision(float groundLevel, float restitution)
        {
            if (rigidBody.position.y - rigidBody.radius <= groundLevel)
            {
                // Position correction
                rigidBody.position.y = groundLevel + rigidBody.radius;
                
                // Velocity reflection with energy loss
                rigidBody.velocity.y = -rigidBody.velocity.y * restitution;
                
                // Apply friction only if not perfectly elastic
                if (restitution < 1.0f)
                {
                    float friction = 1.0f - (0.05f * (1.0f - restitution)); // Scale friction with restitution
                    rigidBody.velocity.x *= friction;
                    rigidBody.velocity.z *= friction;
                    
                    // Stop if moving too slowly (only for imperfect collisions)
                    if (Mathf.Abs(rigidBody.velocity.y) < 0.1f)
                    {
                        rigidBody.velocity.y = 0;
                    }
                }
            }
        }
    }
}

