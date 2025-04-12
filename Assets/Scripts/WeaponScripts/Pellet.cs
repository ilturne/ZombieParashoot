using UnityEngine;

public class Pellet : MonoBehaviour
{
    [Header("Pellet Settings")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float lifetime = 3.0f;
    // Optional: [SerializeField] private GameObject impactEffectPrefab;

    private Rigidbody rb;

    void Awake() { rb = GetComponent<Rigidbody>(); }
    void Start() { Destroy(gameObject, lifetime); }
    void OnCollisionEnter(Collision collision)
    {
        HandleImpact(collision.gameObject, collision.contacts[0].point, collision.contacts[0].normal);
    }

    void HandleImpact(GameObject hitObject, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (hitObject.CompareTag("Zombie"))
        {
            ZombieHealth zombieHealth = hitObject.GetComponent<ZombieHealth>();
            if (zombieHealth != null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    ThirdPersonMovement playerMovement = playerObject.GetComponent<ThirdPersonMovement>();
                    // Check if player exists AND InstaKill is active
                    if (playerMovement != null && playerMovement.IsInstaKillActive)
                    {
                        // Player has InstaKill, call the specific method
                        Debug.Log($"Insta-Killing {hitObject.name}!");
                        zombieHealth.InstaKill();
                    }
                    else
                    {
                        // Player doesn't have InstaKill OR couldn't find script, deal normal damage
                        Debug.Log($"Dealing normal {damage} damage to {hitObject.name}");
                        zombieHealth.TakeDamage(damage);
                    }
                }
                else
                {
                    // Could not find player to check status, deal normal damage as fallback
                     Debug.LogWarning("Bullet couldn't find Player to check InstaKill status, dealing normal damage.");
                     zombieHealth.TakeDamage(damage);
                }
                // --- End InstaKill Check ---
            }
        }

        Destroy(gameObject);
    }
}
