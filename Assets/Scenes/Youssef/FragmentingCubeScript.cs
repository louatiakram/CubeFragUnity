using System.Collections.Generic;
using UnityEngine;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gestionnaire du cube fragmenté - Chute jusqu'à (0,0,0) puis fragmentation
/// Le cube entier tombe uniformément via les contraintes
/// À (0,0,0), les contraintes se rompent et libèrent l'énergie stockée
/// </summary>
public class FragmentingCubeScript : MonoBehaviour
{
    [Header("Cube Settings")]
    public int fragmentsPerAxis = 4;  // 4x4x4 = 64 fragments
    public float cubeSize = 4f;
    public float fragmentMass = 0.1f;

    [Header("Physics")]
    public Vector3 initialVelocity = Vector3.zero;
    public float gravity = -9.81f;
    public Vector3 fragmentationPosition = Vector3.zero;  // Position où déclencher la fragmentation
    public float fragmentationRadius = 0.5f;  // Rayon autour du point de fragmentation

    [Header("Constraint Settings")]
    public float constraintStiffness = 100f;
    public float constraintDamping = 0.5f;
    public float breakThreshold = 0.3f;
    public float energyTransferRate = 0.9f;

    private List<FragmentPhysicsScript> fragments = new List<FragmentPhysicsScript>();
    private ConstraintSystemScript constraintSystem;
    private bool isFragmented = false;
    private Vector3 cubeCenter = Vector3.zero;

    void Start()
    {
        CreateFragmentedCube();
        CreateConstraintSystem();

        // Initialiser vitesses
        foreach (var frag in fragments)
        {
            frag.velocity = initialVelocity;
        }

        Debug.Log("🟦 Cube fragmenté créé, prêt à tomber");
    }

    void FixedUpdate()
    {
        // Appliquer gravité à tous les fragments
        foreach (var frag in fragments)
        {
            if (frag != null)
                frag.AddForce(new Vector3(0, gravity * frag.masse, 0));
        }

        // Mettre à jour contraintes (maintient le cube uni)
        if (constraintSystem != null)
        {
            constraintSystem.UpdateConstraints();
        }

        // Calculer le centre du cube
        CalculateCubeCenter();

        // Vérifier si le cube a atteint la position de fragmentation
        if (!isFragmented && IsAtFragmentationPosition())
        {
            TriggerFragmentation();
        }
    }

    private void CalculateCubeCenter()
    {
        if (fragments == null || fragments.Count == 0)
        {
            cubeCenter = transform.position;
            return;
        }

        Vector3 center = Vector3.zero;
        int validCount = 0;
        foreach (var frag in fragments)
        {
            if (frag != null)
            {
                center += frag.position;
                validCount++;
            }
        }

        cubeCenter = validCount > 0 ? center / validCount : transform.position;
    }

    private bool IsAtFragmentationPosition()
    {
        float distance = Vector3.Distance(cubeCenter, fragmentationPosition);
        return distance <= fragmentationRadius;
    }

    private void TriggerFragmentation()
    {
        isFragmented = true;
        Debug.Log($"💥 FRAGMENTATION DÉCLENCHÉE! Cube center: {cubeCenter}");

        if (constraintSystem == null)
        {
            Debug.LogError("TriggerFragmentation : constraintSystem est null. Abandon de la fragmentation.");
            return;
        }

        var allConstraints = constraintSystem.GetAllConstraints();
        if (allConstraints == null || allConstraints.Count == 0)
        {
            Debug.LogWarning("TriggerFragmentation : aucune contrainte trouvée.");
        }

        // Tous les contraintes se rompent et libèrent l'énergie
        foreach (var constraint in allConstraints)
        {
            if (constraint == null) continue;

            if (!constraint.isBroken)
            {
                constraint.isBroken = true;
                ApplyBreakImpulsion(constraint);
            }
        }

        Debug.Log("🔥 Tous les fragments se séparent!");
    }

    private void ApplyBreakImpulsion(ConstraintBond constraint)
    {
        if (constraint == null) return;

        Vector3 impulsion = constraint.GetBreakImpulsion(energyTransferRate);

        int a = constraint.fragmentA;
        int b = constraint.fragmentB;

        if (a >= 0 && a < fragments.Count && fragments[a] != null)
        {
            fragments[a].AddImpulsion(-impulsion);
        }
        else
        {
            Debug.LogWarning($"ApplyBreakImpulsion : fragment A invalide ({a})");
        }

        if (b >= 0 && b < fragments.Count && fragments[b] != null)
        {
            fragments[b].AddImpulsion(impulsion);
        }
        else
        {
            Debug.LogWarning($"ApplyBreakImpulsion : fragment B invalide ({b})");
        }

        Debug.Log($"Impulsion appliquée au fragment {a} et {b}: {impulsion.magnitude:F3}");
    }

    private void CreateFragmentedCube()
    {
        float fragmentSize = cubeSize / fragmentsPerAxis;
        float offset = cubeSize * 0.5f;

        for (int x = 0; x < fragmentsPerAxis; x++)
        {
            for (int y = 0; y < fragmentsPerAxis; y++)
            {
                for (int z = 0; z < fragmentsPerAxis; z++)
                {
                    // Position centrée à l'origine
                    Vector3 fragPos = new Vector3(
                        -offset + x * fragmentSize + fragmentSize * 0.5f,
                        -offset + y * fragmentSize + fragmentSize * 0.5f + cubeSize * 3f, // Hauteur initiale
                        -offset + z * fragmentSize + fragmentSize * 0.5f
                    );

                    // Créer GameObject fragment
                    GameObject fragGO = new GameObject($"Fragment_{x}_{y}_{z}");
                    fragGO.transform.parent = transform;
                    fragGO.transform.position = fragPos;

                    // Ajouter composant physique
                    FragmentPhysicsScript fragPhys = fragGO.AddComponent<FragmentPhysicsScript>();
                    fragPhys.masse = fragmentMass;
                    fragPhys.sizeLocal = Vector3.one * fragmentSize;
                    fragPhys.position = fragPos;
                    fragPhys.centreInertieLocal = Vector3.zero;

                    fragments.Add(fragPhys);
                }
            }
        }

        Debug.Log($"✅ Créé {fragments.Count} fragments");
    }

    private void CreateConstraintSystem()
    {
        GameObject constraintGO = new GameObject("ConstraintSystem");
        constraintGO.transform.parent = transform;

        constraintSystem = constraintGO.AddComponent<ConstraintSystemScript>();
        constraintSystem.stiffness = constraintStiffness;
        constraintSystem.damping = constraintDamping;
        constraintSystem.breakThreshold = breakThreshold;
        constraintSystem.energyTransferRate = energyTransferRate;

        // Enregistrer tous les fragments
        foreach (var frag in fragments)
        {
            constraintSystem.RegisterFragment(frag);
        }

        // Créer les contraintes entre fragments adjacents
        constraintSystem.CreateConstraintGrid(fragmentsPerAxis, cubeSize / fragmentsPerAxis);

        Debug.Log("✅ ConstraintSystem configuré");
    }
}