// BreakableConstraint.cs
using UnityEngine;

public class BreakableConstraint
{
    public Fragment A, B;
    public Vector3 attachA_local; // local point on A
    public Vector3 attachB_local; // local point on B
    public float restLength;
    public float k; // stiffness
    public float damping; // b
    public float ruptureThresholdLambda; // epsilon
    public float alpha = 1.0f; // énergie transférée ratio

    // state
    public bool broken = false;

    public void Step(float dt)
    {
        if (broken) return;

        // compute world attachment positions (compute transforms manually)
        Vector3 WA = A.WorldPointFromLocal(attachA_local);
        Vector3 WB = B.WorldPointFromLocal(attachB_local);
        Vector3 dir = WB - WA;
        float dist = dir.magnitude;
        Vector3 n = dist > 1e-6f ? dir / dist : Vector3.zero;
        float x = dist - restLength; // violation (extension)
        // spring force
        Vector3 f = -k * x * n;

        // compute relative velocity at attachment points
        Vector3 vA = A.rb.GetPointVelocity(WA);
        Vector3 vB = B.rb.GetPointVelocity(WB);
        Vector3 relV = vB - vA;

        // damping force (Rayleigh simple)
        Vector3 fdamp = -damping * Vector3.Dot(relV, n) * n;

        Vector3 totalForce = f + fdamp;

        // Approx impulse magnitude on constraint (approx): lambda ~ totalForce * dt
        float lambdaApprox = totalForce.magnitude * dt;

        // rupture condition
        if (lambdaApprox > ruptureThresholdLambda)
        {
            broken = true;
            OnBreak(WA, WB, n, x);
            return;
        }

        // otherwise, apply equal/opposite forces to bodies to keep them together (stabilization)
        A.rb.AddForceAtPosition(-totalForce, WA, ForceMode.Force);
        B.rb.AddForceAtPosition(totalForce, WB, ForceMode.Force);
    }

    void OnBreak(Vector3 WA, Vector3 WB, Vector3 n, float x)
    {
        // compute stored energy E = 1/2 k x^2
        float E = 0.5f * k * x * x;

        // direction of impulse:
        // simplest: from center-to-center (WB - WA) normalized ; or use torque x force idea from paper.
        Vector3 dirImpulse = (WB - WA).normalized;
        if (dirImpulse == Vector3.zero) dirImpulse = Random.onUnitSphere;

        // Option 1: simple Δv = sqrt(2E/m)
        float mA = A.rb.mass;
        float mB = B.rb.mass;
        // distribute energy to both sides proportionally to mass (example)
        float EA = E * (mB / (mA + mB)); // heuristic: heavier gets less deltaV
        float EB = E * (mA / (mA + mB));
        float dvA = Mathf.Sqrt(2f * EA / mA);
        float dvB = Mathf.Sqrt(2f * EB / mB);

        A.rb.linearVelocity += -dirImpulse * dvA; // recoil opposite
        B.rb.linearVelocity += dirImpulse * dvB;

        // Option 2 (more correct): compute impulse respecting rotation using paper formula
        // compute effective mass mG and μ = sqrt(2 alpha mG E)
        // omitted here for brevity but I can add if you want accurate rotational impulse.
    }
}
