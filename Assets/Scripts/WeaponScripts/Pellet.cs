using UnityEngine;

public class Pellet : MonoBehaviour
{
    [Header("Pellet Settings")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float lifetime = 3.0f;
    [SerializeField] private GameObject impactEffectPrefab; // Optional impact effect

    private Rigidbody rb;

    void Awake() { rb = GetComponent<Rigidbody>(); }
    void Start() { Destroy(gameObject, lifetime); }
    
    void OnCollisionEnter(Collision collision)
    {
        HandleImpact(collision.gameObject, collision.contacts[0].point, collision.contacts[0].normal);
    }

    void HandleImpact(GameObject hitObject, Vector3 hitPoint, Vector3 hitNormal)
    {
        // Check for zombie tag
        if (hitObject.CompareTag("Zombie"))
        {
            // Get the ZombieHealth component
            ZombieHealth zombieHealth = hitObject.GetComponent<ZombieHealth>();
            if (zombieHealth != null)
            {
                // Check for InstaKill
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    ThirdPersonMovement playerMovement = playerObject.GetComponent<ThirdPersonMovement>();
                    
                    // If player has InstaKill active, use it
                    if (playerMovement != null && playerMovement.IsInstaKillActive)
                    {
                        Debug.Log($"Insta-Killing {hitObject.name}!");
                        zombieHealth.InstaKill();
                    }
                    else
                    {
                        // Apply normal damage - this will trigger the red flash effect
                        Debug.Log($"Dealing normal {damage} damage to {hitObject.name}");
                        zombieHealth.TakeDamage(damage);
                    }
                }
                else
                {
                    // Fallback if player not found
                    zombieHealth.TakeDamage(damage);
                }
            }
        }

        // Spawn impact effect if provided
        if (impactEffectPrefab != null)
        {
            Instantiate(impactEffectPrefab, hitPoint, Quaternion.LookRotation(hitNormal));
        }

        // Destroy the pellet
        Destroy(gameObject);
    }
}