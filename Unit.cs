using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class Unit : MonoBehaviour
{
    public bool isSelected = false;
    public GameObject selectionVisual; 
    
    [Header("Movement Settings")]
    public float moveSpeed = 15f;
    public float turnSpeed = 120f;
    public float stoppingDistance = 1.0f;
    
    [Header("Layers")]
    public LayerMask bridgeLayer; 
    public LayerMask groundLayer; 

    private Coroutine moveCoroutine;
    private Pathfinding pathfinder;
    private Rigidbody rb;

    void Awake() 
    {
        pathfinder = FindFirstObjectByType<Pathfinding>();
        rb = GetComponent<Rigidbody>();
    }

    void Start() 
    {
        SetSelected(false);
    }

    public void SetSelected(bool selected) 
    {
        isSelected = selected;
        if (selectionVisual != null) selectionVisual.SetActive(selected);
    }

    public void MoveTo(Vector3 target) 
    {
        if (pathfinder == null) return;

        List<Vector3> path = pathfinder.FindPath(transform.position, target);

        if (path != null && path.Count > 0)
        {
            if (moveCoroutine != null) StopCoroutine(moveCoroutine);
            moveCoroutine = StartCoroutine(FollowPath(path));
        }
    }

    private IEnumerator FollowPath(List<Vector3> path)
    {
        int targetIndex = path.Count > 1 ? 1 : 0;

        while (targetIndex < path.Count)
        {
            Vector3 flatTarget = new Vector3(path[targetIndex].x, transform.position.y, path[targetIndex].z);
            bool isOnBridge = Physics.Raycast(transform.position + Vector3.up, Vector3.down, 5f, bridgeLayer);
            float currentReach = targetIndex == path.Count - 1 ? stoppingDistance : (isOnBridge ? 3.0f : 1.5f);

            if (Vector3.Distance(transform.position, flatTarget) <= currentReach)
            {
                targetIndex++;
                continue; 
            }

            Vector3 directionToTarget = (flatTarget - transform.position).normalized;
            
            if (directionToTarget != Vector3.zero)
            {
                Vector3 projectedDir = Vector3.ProjectOnPlane(directionToTarget, transform.up).normalized;
                
                if (projectedDir != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(projectedDir, transform.up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, (isOnBridge ? turnSpeed * 0.5f : turnSpeed) * Time.fixedDeltaTime);
                }

                if (Vector3.Angle(transform.forward, projectedDir) > 15f)
                {
                    rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
                }
                else
                {
                    Vector3 flatForward = transform.forward;
                    flatForward.y = 0;
                    rb.linearVelocity = new Vector3(flatForward.normalized.x * moveSpeed, rb.linearVelocity.y, flatForward.normalized.z * moveSpeed);
                }
            }

            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        rb.angularVelocity = Vector3.zero;
    }
}