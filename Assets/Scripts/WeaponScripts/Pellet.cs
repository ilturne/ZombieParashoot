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
        // NEW: Check if we hit the final boss
        else if (hitObject.CompareTag("Boss") || hitObject.GetComponent<FinalBossController>() != null)
        {
            // Try to deal damage to the boss
            FinalBossController bossController = hitObject.GetComponent<FinalBossController>();
            if (bossController != null)
            {
                // Check for InstaKill
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    ThirdPersonMovement playerMovement = playerObject.GetComponent<ThirdPersonMovement>();
                    
                    // If player has InstaKill active, apply massive damage but don't instantly kill boss
                    if (playerMovement != null && playerMovement.IsInstaKillActive)
                    {
                        Debug.Log($"Applying InstaKill bonus damage to boss!");
                        bossController.TakeDamage(damage * 5f); // 5x damage instead of instant death
                    }
                    else
                    {
                        // Apply normal damage
                        Debug.Log($"Dealing normal {damage} damage to boss");
                        bossController.TakeDamage(damage);
                    }
                }
                else
                {
                    // Fallback if player not found
                    bossController.TakeDamage(damage);
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