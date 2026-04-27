using UnityEngine;

public class Projectile : MonoBehaviour
{
    [HideInInspector] // Hidden in inspector, as speed is set by the tank gun upon firing
    public float speed = 150f; 
    
    public float damage = 25f;
    public float lifetime = 4f; // Projectile disappears after 4 seconds if it flies off the map

    void Start()
    {
        // Self-destruct timer so lost projectiles don't leak memory
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Projectile flies strictly forward at its speed
        transform.Translate(transform.forward * speed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        // If we hit something, look for a health component on that object
        Health targetHealth = other.GetComponent<Health>();

        if (targetHealth != null)
        {
            targetHealth.TakeDamage(damage);
        }

        // Destroy the projectile upon any collision with a physical object
        Destroy(gameObject);
    }
}