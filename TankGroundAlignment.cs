using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TankGroundAlignment : MonoBehaviour
{
    [Header("Raycast Settings")]
    public LayerMask groundLayer;
    public float raycastStartOffset = 5f;
    public float raycastLength = 500f;
    public float heightOffset = 0f;

    [Header("Smoothing Settings")]
    public float rotationSpeed = 10f;
    public float heightSmoothSpeed = 15f; 
    
    private Rigidbody rb;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (Physics.Raycast(transform.position + Vector3.up * raycastStartOffset, Vector3.down, out RaycastHit hit, raycastLength, groundLayer))
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, (hit.point.y + heightOffset - transform.position.y) * heightSmoothSpeed, rb.linearVelocity.z);

            Vector3 projectedForward = Vector3.ProjectOnPlane(transform.forward, hit.normal).normalized;
            
            if (projectedForward != Vector3.zero)
            {
                rb.MoveRotation(Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(projectedForward, hit.normal), Time.fixedDeltaTime * rotationSpeed));
            }
        }
    }
}