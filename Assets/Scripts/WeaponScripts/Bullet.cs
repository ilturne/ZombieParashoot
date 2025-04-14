using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float damage = 20f;          // Damage dealt to zombies
    public float speed = 30f;           // Speed of the bullet
    public float lifetime = 3f;         // How long the bullet lives before being destroyed
    
    [Header("Visual Effects")]
    public GameObject hitEffect;        // Optional hit effect prefab
    
    private Rigidbody rb;
    
    void Start()
    {
        // Get rigidbody
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Configure rigidbody
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.useGravity = false;
        
        // Set initial velocity
        rb.linearVelocity = transform.forward * speed;
        
        // Destroy after lifetime
        Destroy(gameObject, lifetime);
    }
    
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Hit a zombie: " + collision.gameObject.name);
        // Check if we hit a zombie
        if (collision.gameObject.CompareTag("Zombie"))
        {
            // Try to deal damage to the zombie
            ZombieAi zombieAi = collision.gameObject.GetComponent<ZombieAi>();
            if (zombieAi != null)
            {
                zombieAi.TakeDamage(damage);
            }
            
            // Try ZombieHealth component as well
            ZombieHealth zombieHealth = collision.gameObject.GetComponent<ZombieHealth>();
            if (zombieHealth != null)
            {
                zombieHealth.TakeDamage(damage);
            }
        }
        
        // Spawn hit effect if assigned
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }
        
        // Destroy bullet on impact
        Destroy(gameObject);
    }
}