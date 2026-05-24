using UnityEngine;

[RequireComponent(typeof(UnitTeam))]
public class TankCombat : MonoBehaviour
{
    [Header("Turret Settings")]
    public Transform turret;
    public Transform firePoint;

    [Header("Shooting Settings")]
    public GameObject projectilePrefab;
    public float attackRange = 25f; 
    public float turretTurnSpeed = 5f;
    public float fireRate = 0.5f; 
    
    [Tooltip("Projectile muzzle velocity")]
    public float projectileSpeed = 150f; 

    private Transform currentTarget;
    private Rigidbody targetRb; 
    private UnitTeam myTeam;
    private float fireCountdown = 0f;
    
    private Transform manualTarget; 

    void Start()
    {
        myTeam = GetComponent<UnitTeam>();
    }

    
    public void SetManualTarget(Transform target)
    {
        UnitTeam targetTeam = target.GetComponent<UnitTeam>();
        
        
        if (targetTeam != null && myTeam != null && targetTeam.teamID == myTeam.teamID)
        {
            return; 
        }
        
        manualTarget = target;
    }

    public void ClearManualTarget()
    {
        manualTarget = null;
    }

    void Update()
    {
        FindNearestTarget();

        if (currentTarget != null)
        {
            AimAtTarget();

            if (fireCountdown <= 0f && IsAimingAtTarget())
            {
                Shoot();
                fireCountdown = 1f / fireRate;
            }
        }
        else
        {
            turret.localRotation = Quaternion.Slerp(turret.localRotation, Quaternion.identity, Time.deltaTime * turretTurnSpeed);
        }

        fireCountdown -= Time.deltaTime;
    }

    void FindNearestTarget()
    {
        if (manualTarget != null)
        {
            currentTarget = manualTarget;
            targetRb = manualTarget.GetComponent<Rigidbody>();
            return; 
        }

        Collider[] colliders = Physics.OverlapSphere(transform.position, attackRange);
        float shortestDistance = Mathf.Infinity;
        Transform nearestEnemy = null;
        Rigidbody nearestEnemyRb = null;

        foreach (Collider col in colliders)
        {
            UnitTeam targetTeam = col.GetComponent<UnitTeam>();
            
            if (targetTeam != null && targetTeam.teamID != myTeam.teamID)
            {
                Health targetHealth = col.GetComponent<Health>();
                if (targetHealth == null) continue; 

                float distanceToEnemy = Vector3.Distance(transform.position, col.transform.position);
                if (distanceToEnemy < shortestDistance)
                {
                    shortestDistance = distanceToEnemy;
                    nearestEnemy = col.transform;
                    nearestEnemyRb = col.GetComponent<Rigidbody>();
                }
            }
        }

        if (nearestEnemy != null && shortestDistance <= attackRange)
        {
            currentTarget = nearestEnemy;
            targetRb = nearestEnemyRb;
        }
        else
        {
            currentTarget = null;
            targetRb = null;
        }
    }

    void AimAtTarget()
    {
        Vector3 aimPoint = currentTarget.position;

        if (targetRb != null)
        {
            float distance = Vector3.Distance(firePoint.position, currentTarget.position);
            float timeToHit = distance / projectileSpeed;
            aimPoint += targetRb.linearVelocity * timeToHit;
        }

        Vector3 directionToTarget = aimPoint - turret.position;
        directionToTarget.y = 0; 

        if (directionToTarget.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            turret.rotation = Quaternion.Slerp(turret.rotation, targetRotation, Time.deltaTime * turretTurnSpeed);
        }
    }

    bool IsAimingAtTarget()
    {
        Vector3 aimPoint = currentTarget.position;
        if (targetRb != null)
        {
            float distance = Vector3.Distance(firePoint.position, currentTarget.position);
            aimPoint += targetRb.linearVelocity * (distance / projectileSpeed);
        }

        Vector3 dirToTarget = (aimPoint - turret.position).normalized;
        float angle = Vector3.Angle(turret.forward, dirToTarget);
        return angle < 5f;
    }

    void Shoot()
    {
        GameObject shell = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        
        Projectile projScript = shell.GetComponent<Projectile>();
        if (projScript != null)
        {
            projScript.speed = projectileSpeed;
        }
    }
}