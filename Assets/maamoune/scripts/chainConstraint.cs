using UnityEngine;

// Constraint between two chain links (spring model)
[System.Serializable]
public class ChainConstraint
{
    public ChainLink linkA;
    public ChainLink linkB;
    public float restLength;        // Rest length of constraint
    public float stiffness;         // Spring stiffness (k)
    public float damping;           // Damping coefficient
    public float breakThreshold;    // Breaking threshold (max force)
    public bool broken;             // Is constraint broken?

    // Energy transfer coefficient (alpha from paper)
    public float energyTransfer = 0.8f;

    public ChainConstraint(ChainLink a, ChainLink b, float k, float d, float breakForce)
    {
        linkA = a;
        linkB = b;
        restLength = (a.position - b.position).Magnitude();
        stiffness = k;
        damping = d;
        breakThreshold = breakForce;
        broken = false;
    }

    // Calculate current stretch/compression
    public float GetStretch()
    {
        float currentLength = (linkA.position - linkB.position).Magnitude();
        return currentLength - restLength;
    }

    // Calculate potential energy: E = 0.5 * k * x puissance 2
    public float GetPotentialEnergy()
    {
        float stretch = GetStretch();
        return 0.5f * stiffness * stretch * stretch;
    }

    // Get constraint direction (from B to A)
    public Vec3 GetDirection()
    {
        Vec3 dir = linkA.position - linkB.position;
        return dir.Normalized();
    }

    // Apply constraint force (spring-damper)
    public void ApplyConstraintForce()
    {
        if (broken) return;

        Vec3 direction = GetDirection();
        float stretch = GetStretch();

        // Relative velocity along constraint
        Vec3 relativeVel = linkA.velocity - linkB.velocity;
        float dampingForce = damping * Vec3.Dot(relativeVel, direction);

        // Total force magnitude
        float forceMag = stiffness * stretch + dampingForce;

        Vec3 force = direction * forceMag;

        // Apply equal and opposite forces
        linkA.AddForce(force * -1.0f);
        linkB.AddForce(force);

        // Check for breaking
        if (Mathf.Abs(forceMag) > breakThreshold)
        {
            Break();
        }
    }

    // Break the constraint and apply energized impulse
    public void Break()
    {
        if (broken) return;

        broken = true;

        // Calculate stored energy
        float energy = GetPotentialEnergy();

        // Calculate impulse magnitude: deltaV = sqrt(2 * alpha * E / m)
        // For two bodies, use effective mass
        float effectiveMass = (linkA.mass * linkB.mass) / (linkA.mass + linkB.mass);
        float impulseMag = Mathf.Sqrt(2.0f * energyTransfer * energy / effectiveMass);

        // Direction: orthogonal to constraint (simulate recoil/bounce)
        Vec3 constraintDir = GetDirection();

        // Create a perpendicular direction (simplified - random tangent)
        Vec3 tangent = Vec3.Cross(constraintDir, Vec3.Up);
        if (tangent.Magnitude() < 0.01f)
            tangent = Vec3.Cross(constraintDir, new Vec3(1, 0, 0));
        tangent = tangent.Normalized();

        Vec3 impulseA = tangent * impulseMag;
        Vec3 impulseB = tangent * (-impulseMag);

        // Apply impulses
        linkA.ApplyImpulse(impulseA);
        linkB.ApplyImpulse(impulseB);

        Debug.Log($"Constraint broken! Energy: {energy:F3}, Impulse: {impulseMag:F3}");
    }
}
