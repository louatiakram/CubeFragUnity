using UnityEngine;

namespace YassineBM
{
    /// <summary>
    /// Represents a spring-like constraint between two shards
    /// Stores deformation and can break when threshold is exceeded
    /// </summary>
    public class ShardConstraint
    {
        public SphereShard shardA;
        public SphereShard shardB;

        public float restLength; // Original distance between shards
        public float stiffness; // Spring stiffness constant (k)
        public float breakThreshold; // Maximum deformation before breaking

        public bool isBroken;

        private float currentDeformation;
        private float storedEnergy;

        public ShardConstraint(SphereShard a, SphereShard b, float stiffness, float breakThreshold)
        {
            this.shardA = a;
            this.shardB = b;
            this.stiffness = stiffness;
            this.breakThreshold = breakThreshold;
            this.isBroken = false;
            this.currentDeformation = 0f;
            this.storedEnergy = 0f;

            // Calculate rest length (initial distance)
            Vector3 delta = b.GetCenterPosition() - a.GetCenterPosition();
            this.restLength = delta.magnitude;
        }

        /// <summary>
        /// Update the constraint - apply spring forces and check for breaking
        /// </summary>
        public void UpdateConstraint()
        {
            if (isBroken) return;

            Vector3 posA = shardA.GetCenterPosition();
            Vector3 posB = shardB.GetCenterPosition();

            Vector3 delta = posB - posA;
            float currentLength = delta.magnitude;

            // Calculate deformation
            currentDeformation = Mathf.Abs(currentLength - restLength);

            // Calculate stored energy: E = 0.5 * k * x^2
            storedEnergy = 0.5f * stiffness * currentDeformation * currentDeformation;

            // Check if should break
            if (currentDeformation > breakThreshold)
            {
                Break();
                return;
            }

            // Apply spring force: F = -k * x
            if (currentLength > 0.0001f)
            {
                Vector3 direction = delta / currentLength;
                float displacement = currentLength - restLength;
                Vector3 springForce = direction * (stiffness * displacement);

                // Apply forces to both shards
                shardA.rigidBody.AddForce(springForce);
                shardB.rigidBody.AddForce(-springForce);
            }
        }

        /// <summary>
        /// Break the constraint and apply explosive impulses
        /// </summary>
        private void Break()
        {
            isBroken = true;

            // Calculate impulse velocity: Δv = sqrt(2E/m)
            // We'll split the energy between the two shards

            Vector3 posA = shardA.GetCenterPosition();
            Vector3 posB = shardB.GetCenterPosition();
            Vector3 direction = (posB - posA).normalized;

            // Calculate impulse magnitude for each shard
            float massA = shardA.rigidBody.mass;
            float massB = shardB.rigidBody.mass;

            // Energy distribution based on mass ratio
            float energyA = storedEnergy * (massB / (massA + massB));
            float energyB = storedEnergy * (massA / (massA + massB));

            // Calculate impulse velocities
            float impulseVelA = Mathf.Sqrt(2f * energyA / massA);
            float impulseVelB = Mathf.Sqrt(2f * energyB / massB);

            // Apply impulses in opposite directions
            Vector3 impulseA = -direction * impulseVelA * massA;
            Vector3 impulseB = direction * impulseVelB * massB;

            shardA.rigidBody.AddImpulse(impulseA);
            shardB.rigidBody.AddImpulse(impulseB);

            // Add some rotational impulse for visual effect
            Vector3 angularImpulseA = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f)
            ).normalized * impulseVelA * 0.5f;

            Vector3 angularImpulseB = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f)
            ).normalized * impulseVelB * 0.5f;

            shardA.rigidBody.AddAngularImpulse(angularImpulseA * massA);
            shardB.rigidBody.AddAngularImpulse(angularImpulseB * massB);

            Debug.Log($"Constraint broken! Energy: {storedEnergy}, Deformation: {currentDeformation}");
        }

        /// <summary>
        /// Get current deformation amount
        /// </summary>
        public float GetDeformation()
        {
            return currentDeformation;
        }

        /// <summary>
        /// Get stored energy in the constraint
        /// </summary>
        public float GetStoredEnergy()
        {
            return storedEnergy;
        }
    }
}

