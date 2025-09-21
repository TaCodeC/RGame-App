using UnityEngine;

public class BlackHoleBehav : MonoBehaviour
{
    [Header("Player Reference")]
    public Rigidbody playerRb;  

    [Header("Black Hole Settings")]
    public float gravityConstant = 50f;           // pullStrength
    public float maxAcceleration = 200f;          // límite de seguridad 
    public float minSqrDistance = 0.01f;          // evita división por cero
    
    private bool nearby;                          // true si el player está dentro del trigger
    void Start()
    {
        nearby = false;
    }

    void FixedUpdate()
    {
        if (!nearby || !playerRb) return;
        // vector hacia el agujero 
        Vector3 offset = transform.position - playerRb.worldCenterOfMass;
        float sqrDist = offset.sqrMagnitude;
        if (sqrDist < minSqrDistance) return; // evita divisoon en 0

        // ley inverso cuadrado: a = G / r^2
        float accel = gravityConstant / sqrDist;
        if (maxAcceleration > 0f) accel = Mathf.Min(accel, maxAcceleration);

        // aplica aceleración
        playerRb.AddForce(offset.normalized * accel, ForceMode.Acceleration);
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            nearby = true;
            Debug.Log("nearby");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) 
        {
            nearby = false;
            Debug.Log("not nearby");
        }
    }
}
