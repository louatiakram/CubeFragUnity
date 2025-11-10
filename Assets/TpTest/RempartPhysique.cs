using UnityEngine;
using System.Collections.Generic;
using static QuaternionRotation;

/// <summary>
/// Physique de corps rigide - 100% personnalisée
/// Utilise uniquement MyVector3 et MyQuaternion custom
/// </summary>
public class RempartPhysique : MonoBehaviour
{
    [Header("Propriétés Physiques")]
    public float masse = 2f;
    public Vector3 sizeUnity = new Vector3(1f, 1f, 1f); // Inspector only
    private MyVector3 size; // Internal use

    [Header("Coefficients")]
    public float coefficientRestitution = 0.6f;
    public float friction = 0.98f;
    public float frictionAngulaire = 0.95f;

    [Header("Résistance de l'Air")]
    public bool utiliserResistanceAir = true;
    public float coefficientTrainee = 1.05f;
    public float densiteAir = 1.225f;

    [Header("Correction de Pénétration")]
    public float pourcentageCorrection = 0.4f;
    public float seuilPenetration = 0.01f;

    // État du rigid body
    private MyVector3 position;
    private MyQuaternion rotation;
    private MyVector3 quantiteMouvement;
    private MyVector3 momentAngulaire;

    // Variables dérivées
    private MyVector3 vitesse;
    private MyVector3 vitesseAngulaire;

    // Tenseur d'inertie
    private Matrix3x3 tensorInertieLocal;
    private Matrix3x3 tensorInertieInverseMonide;

    // Contacts
    private class ContactPoint
    {
        public MyVector3 position;
        public MyVector3 normal;
        public float penetration;
        public float accumulatedImpulse;
        public int frameCount;
    }
    private List<ContactPoint> persistentContacts = new List<ContactPoint>();

    void Start()
    {
        // Conversion size
        size = MyVector3.FromUnity(sizeUnity);

        // Initialisation de l'état
        position = MyVector3.FromUnity(transform.position);
        rotation = MyQuaternion.FromUnity(transform.rotation);
        quantiteMouvement = MyVector3.Zero;
        momentAngulaire = MyVector3.Zero;

        // Calcul tenseur d'inertie
        CalculerTensorInertieLocal();
        UpdateTensorInertieMondeReel();
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // Intégration RK4
        IntegrerEtatRigidBody(dt);

        // Mise à jour Transform Unity
        transform.position = position.ToUnity();
        transform.rotation = rotation.ToUnity();

        // Nettoyage contacts
        persistentContacts.RemoveAll(c => c.frameCount > 5);
        foreach (var contact in persistentContacts)
        {
            contact.frameCount++;
        }
    }

    private void CalculerTensorInertieLocal()
    {
        float lx = size.x;
        float ly = size.y;
        float lz = size.z;

        float Ixx = (masse / 12f) * (ly * ly + lz * lz);
        float Iyy = (masse / 12f) * (lx * lx + lz * lz);
        float Izz = (masse / 12f) * (lx * lx + ly * ly);

        tensorInertieLocal = new Matrix3x3(
            new Vector3(Ixx, 0, 0),
            new Vector3(0, Iyy, 0),
            new Vector3(0, 0, Izz)
        );
    }

    private void UpdateTensorInertieMondeReel()
    {
        // I_world = R * I_local * R^T
        Matrix3x3 R = rotation.ToMatrix3x3();
        Matrix3x3 RT = TransposeMatrix3x3(R);

        Matrix3x3 temp = MultiplyMatrix3x3(R, tensorInertieLocal);
        Matrix3x3 IWorld = MultiplyMatrix3x3(temp, RT);

        tensorInertieInverseMonide = InverseMatrix3x3(IWorld);

        // ω = I^(-1) * L
        Vector3 LUnity = momentAngulaire.ToUnity();
        Vector3 omegaUnity = tensorInertieInverseMonide.MultiplyVector(LUnity);
        vitesseAngulaire = MyVector3.FromUnity(omegaUnity);

        // v = P / m
        vitesse = MyVector3.Divide(quantiteMouvement, masse);
    }

    private void IntegrerEtatRigidBody(float dt)
    {
        // Sauvegarde état
        MyVector3 x0 = position;
        MyQuaternion R0 = rotation;
        MyVector3 P0 = quantiteMouvement;
        MyVector3 L0 = momentAngulaire;

        // k1
        UpdateTensorInertieMondeReel();
        MyVector3 v1 = vitesse;
        MyQuaternion Rdot1 = CalculerDeriveeRotation(rotation, vitesseAngulaire);
        MyVector3 F1 = CalculerForcesExternes();
        MyVector3 Tau1 = CalculerTorquesExternes();

        // k2
        position = MyVector3.Add(x0, MyVector3.Scale(v1, dt / 2f));
        rotation = MyQuaternion.Add(R0, MyQuaternion.Scale(Rdot1, dt / 2f));
        rotation.Normalize();
        quantiteMouvement = MyVector3.Add(P0, MyVector3.Scale(F1, dt / 2f));
        momentAngulaire = MyVector3.Add(L0, MyVector3.Scale(Tau1, dt / 2f));

        UpdateTensorInertieMondeReel();
        MyVector3 v2 = vitesse;
        MyQuaternion Rdot2 = CalculerDeriveeRotation(rotation, vitesseAngulaire);
        MyVector3 F2 = CalculerForcesExternes();
        MyVector3 Tau2 = CalculerTorquesExternes();

        // k3
        position = MyVector3.Add(x0, MyVector3.Scale(v2, dt / 2f));
        rotation = MyQuaternion.Add(R0, MyQuaternion.Scale(Rdot2, dt / 2f));
        rotation.Normalize();
        quantiteMouvement = MyVector3.Add(P0, MyVector3.Scale(F2, dt / 2f));
        momentAngulaire = MyVector3.Add(L0, MyVector3.Scale(Tau2, dt / 2f));

        UpdateTensorInertieMondeReel();
        MyVector3 v3 = vitesse;
        MyQuaternion Rdot3 = CalculerDeriveeRotation(rotation, vitesseAngulaire);
        MyVector3 F3 = CalculerForcesExternes();
        MyVector3 Tau3 = CalculerTorquesExternes();

        // k4
        position = MyVector3.Add(x0, MyVector3.Scale(v3, dt));
        rotation = MyQuaternion.Add(R0, MyQuaternion.Scale(Rdot3, dt));
        rotation.Normalize();
        quantiteMouvement = MyVector3.Add(P0, MyVector3.Scale(F3, dt));
        momentAngulaire = MyVector3.Add(L0, MyVector3.Scale(Tau3, dt));

        UpdateTensorInertieMondeReel();
        MyVector3 v4 = vitesse;
        MyQuaternion Rdot4 = CalculerDeriveeRotation(rotation, vitesseAngulaire);
        MyVector3 F4 = CalculerForcesExternes();
        MyVector3 Tau4 = CalculerTorquesExternes();

        // Mise à jour finale
        MyVector3 vSum = MyVector3.Add(v1, MyVector3.Scale(v2, 2f));
        vSum = MyVector3.Add(vSum, MyVector3.Scale(v3, 2f));
        vSum = MyVector3.Add(vSum, v4);
        position = MyVector3.Add(x0, MyVector3.Scale(vSum, dt / 6f));

        MyQuaternion RdotSum = MyQuaternion.Add(Rdot1, MyQuaternion.Scale(Rdot2, 2f));
        RdotSum = MyQuaternion.Add(RdotSum, MyQuaternion.Scale(Rdot3, 2f));
        RdotSum = MyQuaternion.Add(RdotSum, Rdot4);
        rotation = MyQuaternion.Add(R0, MyQuaternion.Scale(RdotSum, dt / 6f));
        rotation.Normalize();

        MyVector3 FSum = MyVector3.Add(F1, MyVector3.Scale(F2, 2f));
        FSum = MyVector3.Add(FSum, MyVector3.Scale(F3, 2f));
        FSum = MyVector3.Add(FSum, F4);
        quantiteMouvement = MyVector3.Add(P0, MyVector3.Scale(FSum, dt / 6f));

        MyVector3 TauSum = MyVector3.Add(Tau1, MyVector3.Scale(Tau2, 2f));
        TauSum = MyVector3.Add(TauSum, MyVector3.Scale(Tau3, 2f));
        TauSum = MyVector3.Add(TauSum, Tau4);
        momentAngulaire = MyVector3.Add(L0, MyVector3.Scale(TauSum, dt / 6f));

        UpdateTensorInertieMondeReel();

        // Friction
        quantiteMouvement = MyVector3.Scale(quantiteMouvement, friction);
        momentAngulaire = MyVector3.Scale(momentAngulaire, frictionAngulaire);
    }

    private MyQuaternion CalculerDeriveeRotation(MyQuaternion R, MyVector3 omega)
    {
        // dq/dt = 0.5 * ω_quat * q
        MyQuaternion omegaQuat = new MyQuaternion(0, omega.x, omega.y, omega.z);
        MyQuaternion dqdt = MyQuaternion.Multiply(omegaQuat, R);
        return MyQuaternion.Scale(dqdt, 0.5f);
    }

    private MyVector3 CalculerForcesExternes()
    {
        MyVector3 forceTotale = MyVector3.Zero;

        // Gravité
        Vector3 gravityUnity = Physics.gravity;
        MyVector3 gravity = MyVector3.FromUnity(gravityUnity);
        forceTotale = MyVector3.Add(forceTotale, MyVector3.Scale(gravity, masse));

        // Résistance de l'air
        if (utiliserResistanceAir)
        {
            float vitesseCarree = vitesse.SqrMagnitude();
            if (vitesseCarree > 0.001f)
            {
                float aireReference = Mathf.Max(size.x * size.y,
                                               Mathf.Max(size.y * size.z, size.x * size.z));

                float magnitudeTrainee = 0.5f * densiteAir * vitesseCarree *
                                        coefficientTrainee * aireReference;

                MyVector3 forceTrainee = MyVector3.Scale(vitesse.Normalized(), -magnitudeTrainee);
                forceTotale = MyVector3.Add(forceTotale, forceTrainee);
            }
        }

        return forceTotale;
    }

    private MyVector3 CalculerTorquesExternes()
    {
        MyVector3 torqueTotale = MyVector3.Zero;

        // Résistance angulaire
        if (utiliserResistanceAir)
        {
            float omegaCarree = vitesseAngulaire.SqrMagnitude();
            if (omegaCarree > 0.001f)
            {
                float facteurResistanceAngulaire = 0.1f;
                MyVector3 torqueResistance = MyVector3.Scale(vitesseAngulaire,
                    -facteurResistanceAngulaire * omegaCarree);
                torqueTotale = MyVector3.Add(torqueTotale, torqueResistance);
            }
        }

        return torqueTotale;
    }

    public void ResoudreCollision(RempartPhysique autre)
    {
        // Détection OBB
        OBBCollisionInfo collisionInfo = OBBCollision.CheckOBBCollision(
            transform, size, autre.transform, autre.size
        );

        if (!collisionInfo.isColliding) return;

        MyVector3 normale = collisionInfo.normal;

        // CORRECTION: Déclarer masseInvA et masseInvB ici (en dehors du foreach)
        float masseInvA = 1f / masse;
        float masseInvB = 1f / autre.masse;

        foreach (MyVector3 pointContact in collisionInfo.contactPoints)
        {
            ContactPoint contact = TrouverOuCreerContact(
                pointContact, normale, collisionInfo.penetrationDepth);

            MyVector3 r1 = MyVector3.Subtract(pointContact, position);
            MyVector3 r2 = MyVector3.Subtract(pointContact, autre.position);

            // v = v_cm + ω × r
            MyVector3 v1 = MyVector3.Add(vitesse, MyVector3.Cross(vitesseAngulaire, r1));
            MyVector3 v2 = MyVector3.Add(autre.vitesse, MyVector3.Cross(autre.vitesseAngulaire, r2));
            MyVector3 vitesseRelative = MyVector3.Subtract(v1, v2);

            float vitesseNormale = MyVector3.Dot(vitesseRelative, normale);

            if (vitesseNormale > 0) continue;

            // Calcul impulsion
            float e = coefficientRestitution * autre.coefficientRestitution;
            float numerateur = -(1f + e) * vitesseNormale;

            MyVector3 r1CrossN = MyVector3.Cross(r1, normale);
            MyVector3 r2CrossN = MyVector3.Cross(r2, normale);

            Vector3 invIA_r1CrossN = tensorInertieInverseMonide.MultiplyVector(r1CrossN.ToUnity());
            Vector3 invIB_r2CrossN = autre.tensorInertieInverseMonide.MultiplyVector(r2CrossN.ToUnity());

            MyVector3 term1 = MyVector3.Cross(MyVector3.FromUnity(invIA_r1CrossN), r1);
            MyVector3 term2 = MyVector3.Cross(MyVector3.FromUnity(invIB_r2CrossN), r2);

            float denominateur = masseInvA + masseInvB +
                MyVector3.Dot(term1, normale) +
                MyVector3.Dot(term2, normale);

            if (Mathf.Abs(denominateur) < 0.0001f) continue;

            float j = numerateur / denominateur;
            j += contact.accumulatedImpulse * 0.5f;
            contact.accumulatedImpulse = j;

            MyVector3 impulsion = MyVector3.Scale(normale, j);

            // Application impulsion linéaire
            quantiteMouvement = MyVector3.Add(quantiteMouvement, impulsion);
            autre.quantiteMouvement = MyVector3.Subtract(autre.quantiteMouvement, impulsion);

            // Application impulsion angulaire
            MyVector3 torque1 = MyVector3.Cross(r1, impulsion);
            MyVector3 torque2 = MyVector3.Cross(r2, impulsion);
            momentAngulaire = MyVector3.Add(momentAngulaire, torque1);
            autre.momentAngulaire = MyVector3.Subtract(autre.momentAngulaire, torque2);

            UpdateTensorInertieMondeReel();
            autre.UpdateTensorInertieMondeReel();
        }

        // Correction pénétration (masseInvA et masseInvB sont maintenant en portée)
        if (collisionInfo.penetrationDepth > seuilPenetration)
        {
            float correction = Mathf.Max(collisionInfo.penetrationDepth - seuilPenetration, 0f) *
                              pourcentageCorrection;

            float totalMasseInverse = masseInvA + masseInvB;
            MyVector3 deplacement = MyVector3.Scale(normale, correction / totalMasseInverse);

            position = MyVector3.Add(position, MyVector3.Scale(deplacement, masseInvA));
            autre.position = MyVector3.Subtract(autre.position,
                MyVector3.Scale(deplacement, masseInvB));
        }
    }

    private ContactPoint TrouverOuCreerContact(MyVector3 pos, MyVector3 normal, float penetration)
    {
        float seuil = 0.05f;
        foreach (var contact in persistentContacts)
        {
            if (MyVector3.Distance(contact.position, pos) < seuil)
            {
                contact.position = pos;
                contact.normal = normal;
                contact.penetration = penetration;
                contact.frameCount = 0;
                return contact;
            }
        }

        ContactPoint newContact = new ContactPoint
        {
            position = pos,
            normal = normal,
            penetration = penetration,
            accumulatedImpulse = 0f,
            frameCount = 0
        };

        persistentContacts.Add(newContact);
        return newContact;
    }

    public void AppliquerImpulsion(MyVector3 direction, float intensite)
    {
        MyVector3 impulsion = MyVector3.Scale(direction.Normalized(), intensite);
        quantiteMouvement = MyVector3.Add(quantiteMouvement, impulsion);
    }

    public void AppliquerImpulsionAngulaire(MyVector3 pointApplication, MyVector3 force)
    {
        MyVector3 r = MyVector3.Subtract(pointApplication, position);
        MyVector3 torque = MyVector3.Cross(r, force);
        momentAngulaire = MyVector3.Add(momentAngulaire, torque);
    }

    public static bool CheckAABBCollision(Vector3 posA, Vector3 sizeA, Vector3 posB, Vector3 sizeB)
    {
        Vector3 minA = posA - sizeA * 0.5f;
        Vector3 maxA = posA + sizeA * 0.5f;
        Vector3 minB = posB - sizeB * 0.5f;
        Vector3 maxB = posB + sizeB * 0.5f;

        return (minA.x <= maxB.x && maxA.x >= minB.x) &&
               (minA.y <= maxB.y && maxA.y >= minB.y) &&
               (minA.z <= maxB.z && maxA.z >= minB.z);
    }

    // Helpers Matrix3x3
    private Matrix3x3 TransposeMatrix3x3(Matrix3x3 mat)
    {
        return new Matrix3x3(
            new Vector3(mat.m[0, 0], mat.m[1, 0], mat.m[2, 0]),
            new Vector3(mat.m[0, 1], mat.m[1, 1], mat.m[2, 1]),
            new Vector3(mat.m[0, 2], mat.m[1, 2], mat.m[2, 2])
        );
    }

    private Matrix3x3 MultiplyMatrix3x3(Matrix3x3 a, Matrix3x3 b)
    {
        Matrix3x3 result = new Matrix3x3(Vector3.zero, Vector3.zero, Vector3.zero);
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                result.m[i, j] = 0;
                for (int k = 0; k < 3; k++)
                {
                    result.m[i, j] += a.m[i, k] * b.m[k, j];
                }
            }
        }
        return result;
    }

    private Matrix3x3 InverseMatrix3x3(Matrix3x3 mat)
    {
        float det = mat.m[0, 0] * (mat.m[1, 1] * mat.m[2, 2] - mat.m[1, 2] * mat.m[2, 1]) -
                    mat.m[0, 1] * (mat.m[1, 0] * mat.m[2, 2] - mat.m[1, 2] * mat.m[2, 0]) +
                    mat.m[0, 2] * (mat.m[1, 0] * mat.m[2, 1] - mat.m[1, 1] * mat.m[2, 0]);

        if (Mathf.Abs(det) < 0.0001f) return mat;

        float invDet = 1f / det;
        Matrix3x3 inv = new Matrix3x3(Vector3.zero, Vector3.zero, Vector3.zero);

        inv.m[0, 0] = (mat.m[1, 1] * mat.m[2, 2] - mat.m[1, 2] * mat.m[2, 1]) * invDet;
        inv.m[0, 1] = (mat.m[0, 2] * mat.m[2, 1] - mat.m[0, 1] * mat.m[2, 2]) * invDet;
        inv.m[0, 2] = (mat.m[0, 1] * mat.m[1, 2] - mat.m[0, 2] * mat.m[1, 1]) * invDet;

        inv.m[1, 0] = (mat.m[1, 2] * mat.m[2, 0] - mat.m[1, 0] * mat.m[2, 2]) * invDet;
        inv.m[1, 1] = (mat.m[0, 0] * mat.m[2, 2] - mat.m[0, 2] * mat.m[2, 0]) * invDet;
        inv.m[1, 2] = (mat.m[0, 2] * mat.m[1, 0] - mat.m[0, 0] * mat.m[1, 2]) * invDet;

        inv.m[2, 0] = (mat.m[1, 0] * mat.m[2, 1] - mat.m[1, 1] * mat.m[2, 0]) * invDet;
        inv.m[2, 1] = (mat.m[0, 1] * mat.m[2, 0] - mat.m[0, 0] * mat.m[2, 1]) * invDet;
        inv.m[2, 2] = (mat.m[0, 0] * mat.m[1, 1] - mat.m[0, 1] * mat.m[1, 0]) * invDet;

        return inv;
    }
}