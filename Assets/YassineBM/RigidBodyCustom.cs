using UnityEngine;

namespace YassineBM
{
    /// <summary>
    /// Custom rigid body physics properties
    /// </summary>
    public class RigidBodyCustom
    {
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 acceleration;
        public Quaternion rotation;
        public Vector3 angularVelocity;
        
        public float mass;
        public float radius; // For sphere collision
        public Vector3 centerOfMass;
        
        // Damping
        public float linearDamping = 0.99f;
        public float angularDamping = 0.98f;

        public RigidBodyCustom(Vector3 initialPosition, float mass, float radius)
        {
            this.position = initialPosition;
            this.mass = mass;
            this.radius = radius;
            this.velocity = Vector3.zero;
            this.acceleration = Vector3.zero;
            this.rotation = Quaternion.identity;
            this.angularVelocity = Vector3.zero;
            this.centerOfMass = Vector3.zero;
        }

        /// <summary>
        /// Apply an impulse force to the rigid body
        /// </summary>
        public void ApplyImpulse(Vector3 impulse)
        {
            velocity += impulse / mass;
        }

        /// <summary>
        /// Apply a force (will be integrated over time)
        /// </summary>
        public void ApplyForce(Vector3 force)
        {
            acceleration += force / mass;
        }

        /// <summary>
        /// Update physics simulation
        /// </summary>
        public void Integrate(float deltaTime, float dampingMultiplier = 1.0f)
        {
            // Update velocity from acceleration
            velocity += acceleration * deltaTime;
            
            // Apply damping (scaled by multiplier for high restitution scenarios)
            float effectiveLinearDamping = 1.0f - ((1.0f - linearDamping) * dampingMultiplier);
            float effectiveAngularDamping = 1.0f - ((1.0f - angularDamping) * dampingMultiplier);
            
            velocity *= effectiveLinearDamping;
            angularVelocity *= effectiveAngularDamping;
            
            // Update position from velocity
            position += velocity * deltaTime;
            
            // Update rotation from angular velocity
            if (angularVelocity.sqrMagnitude > 0.0001f)
            {
                Quaternion deltaRotation = Quaternion.Euler(angularVelocity * Mathf.Rad2Deg * deltaTime);
                rotation = deltaRotation * rotation;
                rotation.Normalize();
            }
            
            // Reset acceleration for next frame
            acceleration = Vector3.zero;
        }

        /// <summary>
        /// Get transformation matrix from rigid body state
        /// </summary>
        public Matrix4x4Custom GetTransformMatrix()
        {
            // Create rotation matrix from quaternion
            Matrix4x4Custom rotationMatrix = QuaternionToMatrix(rotation);
            
            // Create translation matrix
            Matrix4x4Custom translationMatrix = Matrix4x4Custom.Translation(position);
            
            // Compose: Translation * Rotation
            return translationMatrix * rotationMatrix;
        }

        /// <summary>
        /// Convert quaternion to rotation matrix
        /// </summary>
        private Matrix4x4Custom QuaternionToMatrix(Quaternion q)
        {
            float xx = q.x * q.x;
            float yy = q.y * q.y;
            float zz = q.z * q.z;
            float xy = q.x * q.y;
            float xz = q.x * q.z;
            float yz = q.y * q.z;
            float wx = q.w * q.x;
            float wy = q.w * q.y;
            float wz = q.w * q.z;

            return new Matrix4x4Custom(
                1 - 2 * (yy + zz), 2 * (xy - wz), 2 * (xz + wy), 0,
                2 * (xy + wz), 1 - 2 * (xx + zz), 2 * (yz - wx), 0,
                2 * (xz - wy), 2 * (yz + wx), 1 - 2 * (xx + yy), 0,
                0, 0, 0, 1
            );
        }
    }
}

