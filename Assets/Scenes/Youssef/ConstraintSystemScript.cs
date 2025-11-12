using UnityEngine;
using System.Collections.Generic;
using static QuaternionRotation;

/// <summary>
/// Constraint System - Gère les contraintes entre fragments
/// Modélise les connexions comme des ressorts rigides
/// Stocke l'énergie et la libère lors de la rupture
/// </summary>
public class ConstraintBond
{
    public int fragmentA;
    public int fragmentB;

    public float stiffness = 100f;           // k du ressort
    public float dampingCoeff = 0.5f;        // Amortissement
    public float breakThreshold = 0.5f;      // Seuil de rupture (déformation max)
    public float restLength;                 // Longueur au repos

    public float storedEnergy = 0f;          // Énergie stockée
    public bool isBroken = false;

    public Vector3 lastForce = Vector3.zero;

    public ConstraintBond(int a, int b, Vector3 posA, Vector3 posB, float k, float damp, float threshold)
    {
        fragmentA = a;
        fragmentB = b;
        stiffness = k;
        dampingCoeff = damp;
        breakThreshold = threshold;
        restLength = Vector3.Distance(posA, posB);
    }

    /// <summary>
    /// Évalue la contrainte et calcule les forces
    /// Retourne true si rupture
    /// </summary>
    public bool EvaluateConstraint(Vector3 posA, Vector3 posB, Vector3 velA, Vector3 velB)
    {
        if (isBroken) return false;

        Vector3 delta = posB - posA;
        float currentLength = delta.magnitude;
        float deformation = currentLength - restLength;
        float deformationRatio = Mathf.Abs(deformation) / restLength;

        // Ressort: F = -k * Δx
        float springForce = -stiffness * deformation;

        // Amortissement: F_damp = -c * v_rel
        Vector3 relativeVel = velB - velA;
        Vector3 forceDirection = delta.normalized;
        float dampingForce = -dampingCoeff * Vector3.Dot(relativeVel, forceDirection);

        // Force totale
        float totalForce = springForce + dampingForce;
        lastForce = forceDirection * totalForce;

        // Énergie potentielle stockée: E = 0.5 * k * x²
        storedEnergy = 0.5f * stiffness * deformation * deformation;

        Debug.Log($"Constraint {fragmentA}-{fragmentB}: Deformation={deformationRatio:F3}, Energy={storedEnergy:F3}");

        // Rupture si déformation > seuil
        if (deformationRatio > breakThreshold)
        {
            isBroken = true;
            Debug.Log($"💥 CONSTRAINT BROKEN: {fragmentA}-{fragmentB}! Energy released: {storedEnergy}");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Récupère l'impulsion directionnelle lors de la rupture
    /// </summary>
    public Vector3 GetBreakImpulsion(float energyTransferRate = 0.9f)
    {
        // Impulsion = sqrt(2 * Énergie * transferRate) * direction
        float impulseMagnitude = Mathf.Sqrt(2f * storedEnergy * energyTransferRate);
        return lastForce.normalized * impulseMagnitude;
    }
}

/// <summary>
/// Gestionnaire de contraintes pour un cube fragmenté
/// </summary>
public class ConstraintSystemScript : MonoBehaviour
{
    [Header("Constraint Settings")]
    public float stiffness = 100f;
    public float damping = 0.5f;
    public float breakThreshold = 0.3f;
    public float energyTransferRate = 0.9f;

    private List<ConstraintBond> constraints = new List<ConstraintBond>();
    private List<FragmentPhysicsScript> fragments = new List<FragmentPhysicsScript>();

    public void RegisterFragment(FragmentPhysicsScript frag)
    {
        fragments.Add(frag);
    }

    /// <summary>
    /// Crée les contraintes entre fragments adjacents (grille 3D)
    /// </summary>
    public void CreateConstraintGrid(int fragmentsPerAxis, float fragmentSize)
    {
        constraints.Clear();

        // Pour chaque fragment, créer contraintes avec ses voisins
        for (int x = 0; x < fragmentsPerAxis; x++)
        {
            for (int y = 0; y < fragmentsPerAxis; y++)
            {
                for (int z = 0; z < fragmentsPerAxis; z++)
                {
                    int idx = GetFragmentIndex(x, y, z, fragmentsPerAxis);

                    // Voisins (6 directions)
                    if (x + 1 < fragmentsPerAxis)
                        CreateConstraint(idx, GetFragmentIndex(x + 1, y, z, fragmentsPerAxis));

                    if (y + 1 < fragmentsPerAxis)
                        CreateConstraint(idx, GetFragmentIndex(x, y + 1, z, fragmentsPerAxis));

                    if (z + 1 < fragmentsPerAxis)
                        CreateConstraint(idx, GetFragmentIndex(x, y, z + 1, fragmentsPerAxis));
                }
            }
        }
    }

    private void CreateConstraint(int fragA, int fragB)
    {
        if (fragA >= fragments.Count || fragB >= fragments.Count) return;

        Vector3 posA = fragments[fragA].position;
        Vector3 posB = fragments[fragB].position;

        ConstraintBond bond = new ConstraintBond(fragA, fragB, posA, posB, stiffness, damping, breakThreshold);
        constraints.Add(bond);
    }

    private int GetFragmentIndex(int x, int y, int z, int axis)
    {
        return x + y * axis + z * axis * axis;
    }

    public void UpdateConstraints()
    {
        foreach (var constraint in constraints)
        {
            if (constraint.isBroken) continue;

            Vector3 posA = fragments[constraint.fragmentA].position;
            Vector3 posB = fragments[constraint.fragmentB].position;
            Vector3 velA = fragments[constraint.fragmentA].velocity;
            Vector3 velB = fragments[constraint.fragmentB].velocity;

            // Évaluer et détecter rupture
            if (constraint.EvaluateConstraint(posA, posB, velA, velB))
            {
                // Rupture !
                ApplyBreakImpulsion(constraint);
            }
            else
            {
                // Appliquer les forces de contrainte
                ApplyConstraintForces(constraint);
            }
        }
    }

    private void ApplyConstraintForces(ConstraintBond constraint)
    {
        Vector3 force = constraint.lastForce;

        // Action/réaction
        fragments[constraint.fragmentA].AddForce(-force);
        fragments[constraint.fragmentB].AddForce(force);
    }

    private void ApplyBreakImpulsion(ConstraintBond constraint)
    {
        Vector3 impulsion = constraint.GetBreakImpulsion(energyTransferRate);

        // Appliquer l'impulsion aux deux fragments
        fragments[constraint.fragmentA].AddImpulsion(-impulsion);
        fragments[constraint.fragmentB].AddImpulsion(impulsion);

        Debug.Log($"Impulsion appliquée: {impulsion.magnitude}");
    }
    public List<ConstraintBond> GetAllConstraints()
    {
        return constraints;
    }

    public bool IsFullyFragmented()
    {
        foreach (var constraint in constraints)
        {
            if (!constraint.isBroken) return false;
        }
        return true;
    }
}
