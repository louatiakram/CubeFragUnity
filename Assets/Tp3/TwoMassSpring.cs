using System.Numerics;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

public class TwoMassSpring : MonoBehaviour
{
    public Transform mass1;
    public Transform mass2;

    public float k1 = 10.0f; // Constante de ressort du premier ressort
    public float k2 = 10.0f; // Constante de ressort du deuxi��me ressort
    public float k3 = 10.0f;    

    public float mass1value = 1.0f; // Masse du premier objet
    public float mass2value = 1.0f; // Masse du deuxi��me objet

    public float damping1 = 0.5f; // Coefficient d'amortissement du premier ressort

    private UnityEngine.Vector2 displacement;
    private UnityEngine.Vector2 velocity;
    private UnityEngine.Vector2 acceleration;

    //private Matrix2x2 M;
    //private Matrix2x2 K;
    //private Matrix2x2 C;
    


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        displacement = new UnityEngine.Vector2(mass1.localPosition.x, mass2.localPosition.x);
        velocity = UnityEngine.Vector2.zero;
        //M = new Matrix2x2(mass1value, 0, 0, mass2value);
        //K = new Matrix2x2(k1 + k2, -k2, -k2, k2 + k3);
        //C = new Matrix2x2(damping1, 0, 0, 0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
