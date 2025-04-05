using UnityEngine;
using System.Collections;

public class ZombieHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    // --- NEW: Despawn Logic Variables ---
    [Header("Despawn Settings")]
    [Tooltip("If the zombie is further than this distance AND behind the player, it will despawn.")]
    [SerializeField] private float despawnDistanceBehind = 35f;
    [Tooltip("How often (in seconds) the zombie checks if it should despawn.")]
    [SerializeField] private float despawnCheckInterval = 1.0f; // Check once per second

    private Transform playerTransform; // Reference to the player found at runtime
    private float despawnCheckTimer;
    private float despawnDistanceBehindSqr; // Store squared distance for efficiency
    // --- End Despawn Logic Variables ---

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        if (!CompareTag("Zombie")) { /* ... optional warning/tagging ... */ }

        // --- NEW: Find Player for Despawn Check ---
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogError($"Zombie {name} could not find the Player object tagged 'Player'. Despawn check will be disabled.", this);
            // Disable despawn check if player isn't found
            this.enabled = false; // Or just skip the check in Update
        }

        // Pre-calculate squared distance for efficiency
        despawnDistanceBehindSqr = despawnDistanceBehind * despawnDistanceBehind;
        // Stagger the first check slightly across different zombies
        despawnCheckTimer = Random.Range(0f, despawnCheckInterval);
        // --- End Find Player ---
    }

    // --- NEW: Update Method for Despawn Check ---
    void Update()
    {
        // Don't check if dead, or if player wasn't found
        if (isDead || playerTransform == null) return;

        // Countdown timer
        despawnCheckTimer -= Time.deltaTime;
        if (despawnCheckTimer <= 0f)
        {
            despawnCheckTimer = despawnCheckInterval; // Reset timer for next check
            CheckIfShouldDespawn();
        }
    }

    // --- NEW: Despawn Check Logic ---
    void CheckIfShouldDespawn()
    {
        // Calculate vector from player pointing TO the zombie
        Vector3 directionToZombie = transform.position - playerTransform.position;
        float distanceToPlayerSqr = directionToZombie.sqrMagnitude;

        // Optimization: Only proceed if the zombie is beyond the minimum distance threshold
        if (distanceToPlayerSqr > despawnDistanceBehindSqr)
        {
            // Calculate the dot product between player's forward and direction to zombie
            // A negative dot product means the zombie is behind the player (> 90 degrees away from forward)
            float dotForward = Vector3.Dot(playerTransform.forward, directionToZombie.normalized);

            // Use a threshold slightly less than 0 to avoid despawning things exactly to the side
            float behindThreshold = -0.1f;
            if (dotForward < behindThreshold)
            {
                Destroy(gameObject); // Destroy the zombie immediately
            }
        }
    }
    // --- End Despawn Check Logic ---


    // --- Existing TakeDamage Method (Keep As Is) ---
    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;
        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        Debug.Log($"{gameObject.name} took {damageAmount} damage. Health: {currentHealth}/{maxHealth}");
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void InstaKill()
    {
        if (isDead) return;
        Die();
    }
    // --- Existing Die Method (Keep As Is) ---
    void Die()
    {
        isDead = true;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // stop the zombie from moving
        var agent = GetComponent<ZombieAi>().agent;
        if (agent != null) agent.isStopped = true;

        var aiScript = GetComponent<ZombieAi>();
        if (aiScript != null) aiScript.enabled = false;

        Destroy(gameObject, 2.0f);
    }
}