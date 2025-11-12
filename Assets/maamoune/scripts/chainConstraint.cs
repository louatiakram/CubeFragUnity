using UnityEngine;

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

    public float GetStretch()
    {
        float currentLength = (linkA.position - linkB.position).Magnitude();
        return currentLength - restLength;
    }

    public float GetPotentialEnergy()
    {
        float stretch = GetStretch();
        return 0.5f * stiffness * stretch * stretch;
    }

    public Vec3 GetDirection()
    {
        Vec3 dir = linkA.position - linkB.position;
        return dir.Normalized();
    }

    public void ApplyConstraintForce()
    {
        if (broken) return;

        Vec3 direction = GetDirection();
        float stretch = GetStretch();

        Vec3 relativeVel = linkA.velocity - linkB.velocity;
        float dampingForce = damping * Vec3.Dot(relativeVel, direction);

        float forceMag = stiffness * stretch + dampingForce;

        Vec3 force = direction * forceMag;

        linkA.AddForce(force * -1.0f);
        linkB.AddForce(force);

        if (Mathf.Abs(forceMag) > breakThreshold)
        {
            Break();
        }
    }

    public void Break()
    {
        if (broken) return;

        broken = true;

        float energy = GetPotentialEnergy();

        float effectiveMass = (linkA.mass * linkB.mass) / (linkA.mass + linkB.mass);
        float impulseMag = Mathf.Sqrt(2.0f * energyTransfer * energy / effectiveMass);

        Vec3 constraintDir = GetDirection();

        Vec3 tangent = Vec3.Cross(constraintDir, Vec3.Up);
        if (tangent.Magnitude() < 0.01f)
            tangent = Vec3.Cross(constraintDir, new Vec3(1, 0, 0));
        tangent = tangent.Normalized();

        Vec3 impulseA = tangent * impulseMag;
        Vec3 impulseB = tangent * (-impulseMag);

        linkA.ApplyImpulse(impulseA);
        linkB.ApplyImpulse(impulseB);

        Debug.Log($"Constraint broken! Energy: {energy:F3}, Impulse: {impulseMag:F3}");
    }
}
