using UnityEngine;

namespace YassineBM
{
    /// <summary>
    /// Custom rigid body implementation - no Unity physics allowed
    /// Stores mass, inertia, velocity, angular velocity
    /// </summary>
    public class CustomRigidBody
    {
        // Physical properties
        public float mass;
        public float inertiaTensor; // Simplified scalar inertia

        // Linear motion
        public Vector3 velocity;
        public Vector3 acceleration;

        // Angular motion
        public Vector3 angularVelocity;
        public Vector3 angularAcceleration;

        // Position and rotation (stored separately, applied via matrices)
        public Vector3 position;
        public Vector3 rotationAxis;
        public float rotationAngle;

        // Damping
        public float linearDamping = 0.99f;
        public float angularDamping = 0.99f;

        // Forces and torques accumulator
        private Vector3 forceAccumulator;
        private Vector3 torqueAccumulator;

        public CustomRigidBody(float mass, Vector3 initialPosition)
        {
            this.mass = mass;
            this.position = initialPosition;
            this.velocity = Vector3.zero;
            this.acceleration = Vector3.zero;
            this.angularVelocity = Vector3.zero;
            this.angularAcceleration = Vector3.zero;
            this.rotationAxis = Vector3.up;
            this.rotationAngle = 0f;
            this.forceAccumulator = Vector3.zero;
            this.torqueAccumulator = Vector3.zero;
            
            // Calculate inertia tensor (simplified as scalar for sphere-like objects)
            // For a sphere: I = (2/5) * m * r^2, we'll use a default
            this.inertiaTensor = (2.0f / 5.0f) * mass * 1.0f; // Assume radius ~1
        }

        /// <summary>
        /// Add a force to be applied this frame
        /// </summary>
        public void AddForce(Vector3 force)
        {
            forceAccumulator += force;
        }

        /// <summary>
        /// Add an impulse (instant velocity change)
        /// </summary>
        public void AddImpulse(Vector3 impulse)
        {
            velocity += impulse / mass;
        }

        /// <summary>
        /// Add a torque to be applied this frame
        /// </summary>
        public void AddTorque(Vector3 torque)
        {
            torqueAccumulator += torque;
        }

        /// <summary>
        /// Add an angular impulse
        /// </summary>
        public void AddAngularImpulse(Vector3 angularImpulse)
        {
            angularVelocity += angularImpulse / inertiaTensor;
        }

        /// <summary>
        /// Update physics simulation
        /// </summary>
        public void Integrate(float deltaTime)
        {
            // Linear motion
            acceleration = forceAccumulator / mass;
            velocity += acceleration * deltaTime;
            velocity *= linearDamping;
            position += velocity * deltaTime;

            // Angular motion
            angularAcceleration = torqueAccumulator / inertiaTensor;
            angularVelocity += angularAcceleration * deltaTime;
            angularVelocity *= angularDamping;

            // Update rotation representation
            float angularSpeed = angularVelocity.magnitude;
            if (angularSpeed > 0.0001f)
            {
                rotationAxis = angularVelocity.normalized;
                rotationAngle += angularSpeed * deltaTime;
                
                // Keep angle in reasonable range
                while (rotationAngle > Mathf.PI * 2)
                    rotationAngle -= Mathf.PI * 2;
            }

            // Clear accumulators
            forceAccumulator = Vector3.zero;
            torqueAccumulator = Vector3.zero;
        }

        /// <summary>
        /// Get transformation matrix for this rigid body
        /// </summary>
        public CustomMatrix4x4 GetTransformationMatrix()
        {
            CustomMatrix4x4 rotation = CustomMatrix4x4.Rotation(rotationAxis, rotationAngle);
            CustomMatrix4x4 translation = CustomMatrix4x4.Translation(position);
            return translation * rotation;
        }

        /// <summary>
        /// Apply gravity
        /// </summary>
        public void ApplyGravity(Vector3 gravity)
        {
            AddForce(gravity * mass);
        }
    }
}

