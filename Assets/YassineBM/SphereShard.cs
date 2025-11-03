using UnityEngine;

namespace YassineBM
{
    /// <summary>
    /// Represents a single fragment/shard of the fractured sphere
    /// </summary>
    public class SphereShard : MonoBehaviour
    {
        // Custom physics
        public CustomRigidBody rigidBody;

        // Visual
        public Mesh shardMesh;
        public Material shardMaterial;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;

        // Original vertices (for matrix transformation)
        private Vector3[] originalVertices;
        private Vector3[] transformedVertices;

        /// <summary>
        /// Initialize the shard with a mesh and physical properties
        /// </summary>
        public void Initialize(Mesh mesh, Material material, float mass, Vector3 initialPosition)
        {
            // Create a copy of the mesh to avoid shared reference issues
            shardMesh = Object.Instantiate(mesh);
            shardMesh.name = mesh.name + "_Instance";
            shardMaterial = material;

            // Setup mesh components
            meshFilter = gameObject.AddComponent<MeshFilter>();
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            
            // Assign mesh and material
            meshFilter.mesh = shardMesh;
            meshRenderer.material = shardMaterial;
            
            // Ensure proper rendering settings
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            meshRenderer.receiveShadows = true;

            // Initialize custom rigid body
            rigidBody = new CustomRigidBody(mass, initialPosition);

            // Store original vertices
            originalVertices = shardMesh.vertices;
            transformedVertices = new Vector3[originalVertices.Length];
            
            // Initial transformation
            UpdateTransformation();
        }

        /// <summary>
        /// Update the shard's transformation using custom matrices
        /// </summary>
        public void UpdateTransformation()
        {
            // Get transformation matrix from rigid body
            CustomMatrix4x4 transformMatrix = rigidBody.GetTransformationMatrix();

            // Transform all vertices manually
            for (int i = 0; i < originalVertices.Length; i++)
            {
                transformedVertices[i] = transformMatrix.TransformPoint(originalVertices[i]);
            }

            // Update mesh
            Mesh mesh = meshFilter.mesh;
            mesh.vertices = transformedVertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        /// <summary>
        /// Get the center position of this shard
        /// </summary>
        public Vector3 GetCenterPosition()
        {
            return rigidBody.position;
        }

        /// <summary>
        /// Apply physics update
        /// </summary>
        public void PhysicsUpdate(float deltaTime)
        {
            rigidBody.Integrate(deltaTime);
        }
    }
}

