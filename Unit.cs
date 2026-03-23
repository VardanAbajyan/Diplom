using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody), typeof(LineRenderer))]
public class Unit : MonoBehaviour
{
    public bool isSelected = false;
    public GameObject selectionVisual;

    [Header("Sensors")]
    public float sensorRadius = 2.5f;
    private float yieldTimer = 0f;
    private Collider[] scanResults = new Collider[10];

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
    private WaitForFixedUpdate fixedUpdateWait;
    
    private LineRenderer pathLine;

    void Awake()
    {
        pathfinder = FindFirstObjectByType<Pathfinding>();
        rb = GetComponent<Rigidbody>();
        fixedUpdateWait = new WaitForFixedUpdate();
        
        pathLine = GetComponent<LineRenderer>();
        if (pathLine != null) pathLine.positionCount = 0;
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
            UpdatePathVisual(path, targetIndex);

            ScanEnvironment();

            if (yieldTimer > 0)
            {
                yieldTimer -= Time.fixedDeltaTime;
                rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, new Vector3(0, rb.linearVelocity.y, 0), 10f * Time.fixedDeltaTime);
                yield return fixedUpdateWait;
                continue;
            }

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

            yield return fixedUpdateWait;
        }

        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        rb.angularVelocity = Vector3.zero;
        
        if (pathLine != null) pathLine.positionCount = 0;
    }

    void UpdatePathVisual(List<Vector3> path, int currentIndex)
    {
        if (pathLine == null) return;

        pathLine.enabled = true; 
        
        int remainingPoints = path.Count - currentIndex;
        pathLine.positionCount = remainingPoints + 1;
        
        float heightOffset = 0.5f;

        pathLine.SetPosition(0, transform.position + Vector3.up * heightOffset);

        for (int i = 0; i < remainingPoints; i++)
        {
            Vector3 pointPosition = path[currentIndex + i] + Vector3.up * heightOffset;
            pathLine.SetPosition(i + 1, pointPosition);
        }
    }

    void ScanEnvironment()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, sensorRadius, scanResults);

        for (int i = 0; i < hitCount; i++)
        {
            Collider obj = scanResults[i];
            if (obj.CompareTag("Tank") && obj.gameObject != gameObject)
            {
                if (gameObject.GetInstanceID() < obj.gameObject.GetInstanceID())
                {
                    yieldTimer = 0.5f;
                    return;
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sensorRadius);
    }
}
