using UnityEngine;
using System.Collections.Generic;

namespace YassineBM
{
    /// <summary>
    /// Handles collision detection and response between fragments
    /// </summary>
    public class CollisionSystem
    {
        private List<Fragment> fragments;
        
        // Spring constraint properties
        public float springConstant = 100f;
        public float dampingConstant = 5f;
        public float breakThreshold = 2.0f; // Distance at which spring breaks
        
        private Dictionary<(Fragment, Fragment), SpringConstraint> constraints;

        public CollisionSystem(List<Fragment> fragments)
        {
            this.fragments = fragments;
            this.constraints = new Dictionary<(Fragment, Fragment), SpringConstraint>();
        }

        /// <summary>
        /// Create spring constraints between nearby fragments
        /// </summary>
        public void CreateInitialConstraints(float maxDistance)
        {
            constraints.Clear();
            
            for (int i = 0; i < fragments.Count; i++)
            {
                for (int j = i + 1; j < fragments.Count; j++)
                {
                    float distance = Vector3.Distance(
                        fragments[i].rigidBody.position,
                        fragments[j].rigidBody.position
                    );
                    
                    if (distance <= maxDistance)
                    {
                        SpringConstraint spring = new SpringConstraint
                        {
                            restLength = distance,
                            isActive = true
                        };
                        constraints[(fragments[i], fragments[j])] = spring;
                    }
                }
            }
        }

        /// <summary>
        /// Update constraints and handle collisions
        /// </summary>
        public void UpdateCollisions(float deltaTime)
        {
            // Check and resolve fragment-to-fragment collisions
            for (int i = 0; i < fragments.Count; i++)
            {
                for (int j = i + 1; j < fragments.Count; j++)
                {
                    CheckFragmentCollision(fragments[i], fragments[j]);
                }
            }
            
            // Update spring constraints
            UpdateConstraints(deltaTime);
        }

        private void CheckFragmentCollision(Fragment a, Fragment b)
        {
            Vector3 delta = b.rigidBody.position - a.rigidBody.position;
            float distance = delta.magnitude;
            float minDistance = a.rigidBody.radius + b.rigidBody.radius;

            if (distance < minDistance && distance > 0.001f)
            {
                // Collision detected
                Vector3 normal = delta / distance;
                float overlap = minDistance - distance;

                // Separate fragments
                float totalMass = a.rigidBody.mass + b.rigidBody.mass;
                a.rigidBody.position -= normal * (overlap * (b.rigidBody.mass / totalMass));
                b.rigidBody.position += normal * (overlap * (a.rigidBody.mass / totalMass));

                // Calculate relative velocity
                Vector3 relativeVelocity = b.rigidBody.velocity - a.rigidBody.velocity;
                float velocityAlongNormal = Vector3.Dot(relativeVelocity, normal);

                // Only resolve if objects are moving towards each other
                if (velocityAlongNormal < 0)
                {
                    // Calculate impulse using coefficient of restitution
                    float restitution = 0.6f;
                    float impulseMagnitude = -(1 + restitution) * velocityAlongNormal;
                    impulseMagnitude /= (1 / a.rigidBody.mass + 1 / b.rigidBody.mass);

                    Vector3 impulse = normal * impulseMagnitude;

                    // Apply impulse
                    a.rigidBody.velocity -= impulse / a.rigidBody.mass;
                    b.rigidBody.velocity += impulse / b.rigidBody.mass;
                }
            }
        }

        private void UpdateConstraints(float deltaTime)
        {
            List<(Fragment, Fragment)> toRemove = new List<(Fragment, Fragment)>();

            foreach (var kvp in constraints)
            {
                var (fragA, fragB) = kvp.Key;
                var spring = kvp.Value;

                if (!spring.isActive) continue;

                Vector3 delta = fragB.rigidBody.position - fragA.rigidBody.position;
                float distance = delta.magnitude;

                // Check if spring should break
                if (distance > spring.restLength * breakThreshold)
                {
                    toRemove.Add(kvp.Key);
                    continue;
                }

                // Calculate spring force: F = -k * (x - x0)
                float displacement = distance - spring.restLength;
                Vector3 direction = delta / distance;
                
                // Spring force
                Vector3 springForce = direction * (springConstant * displacement);
                
                // Damping force: F = -c * v
                Vector3 relativeVelocity = fragB.rigidBody.velocity - fragA.rigidBody.velocity;
                Vector3 dampingForce = relativeVelocity * dampingConstant;
                
                Vector3 totalForce = springForce - dampingForce;

                // Apply forces to both fragments
                fragA.rigidBody.ApplyForce(totalForce);
                fragB.rigidBody.ApplyForce(-totalForce);
            }

            // Remove broken constraints
            foreach (var key in toRemove)
            {
                constraints.Remove(key);
            }
        }

        private class SpringConstraint
        {
            public float restLength;
            public bool isActive;
        }
    }
}

