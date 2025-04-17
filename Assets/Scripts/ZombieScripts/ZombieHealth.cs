using UnityEngine;
using System.Collections;

public class ZombieHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    [Header("Damage Feedback")]
    [SerializeField] private float hitFlashDuration = 0.15f; // How long the red flash lasts
    [SerializeField] private Color hitFlashColor = Color.red; // The color to flash
    [SerializeField] private float hitFlashIntensity = 0.8f; // How intense the flash is (0-1)
    
    // --- Despawn Logic Variables ---
    [Header("Despawn Settings")]
    [Tooltip("If the zombie is further than this distance AND behind the player, it will despawn.")]
    [SerializeField] private float despawnDistanceBehind = 35f;
    [Tooltip("How often (in seconds) the zombie checks if it should despawn.")]
    [SerializeField] private float despawnCheckInterval = 1.0f; // Check once per second

    [Header("Death Settings")]
    [SerializeField] private Color deathColor = new Color(0.3f, 0.3f, 0.3f); // Grayish color for dead zombies
    [SerializeField] private float fallDeathDuration = 1.0f; // How long the death fall animation takes
    [SerializeField] private float destroyDelay = 2.0f; // Time before destroying the object

    private Transform playerTransform; // Reference to the player found at runtime
    private float despawnCheckTimer;
    private float despawnDistanceBehindSqr; // Store squared distance for efficiency
    
    // For damage flash effect
    private Renderer[] renderers;
    private Material[] originalMaterials;
    private Color[] originalColors;
    private Coroutine currentFlashRoutine;
    
    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        if (!CompareTag("Zombie")) { 
            Debug.LogWarning($"Object {name} has ZombieHealth component but doesn't have 'Zombie' tag", this);
        }

        // Find player for despawn check
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
        
        // Get all renderers for damage flash effect
        renderers = GetComponentsInChildren<Renderer>();
        
        // Store original material colors
        StoreMaterialColors();
    }
    
    // Store original colors of all materials
    void StoreMaterialColors()
    {
        // Count total materials
        int totalMaterials = 0;
        foreach (Renderer renderer in renderers)
        {
            totalMaterials += renderer.materials.Length;
        }
        
        // Initialize arrays
        originalMaterials = new Material[totalMaterials];
        originalColors = new Color[totalMaterials];
        
        // Store original materials and colors
        int index = 0;
        foreach (Renderer renderer in renderers)
        {
            Material[] mats = renderer.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                originalMaterials[index] = mats[i];
                if (mats[i].HasProperty("_Color"))
                {
                    originalColors[index] = mats[i].color;
                }
                else
                {
                    originalColors[index] = Color.white;
                }
                index++;
            }
        }
    }

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

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;
        
        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        
        // Play damage flash effect
        PlayDamageFlash();
        
        //Debug.Log($"{gameObject.name} took {damageAmount} damage. Health: {currentHealth}/{maxHealth}");
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    // Play the red flash effect when damaged
    void PlayDamageFlash()
    {
        // If already flashing, stop that coroutine
        if (currentFlashRoutine != null)
        {
            StopCoroutine(currentFlashRoutine);
        }
        
        // Start new flash
        currentFlashRoutine = StartCoroutine(FlashColorsRoutine());
    }
    
    // Coroutine to handle the red flash effect
    IEnumerator FlashColorsRoutine()
    {
        // Apply red flash to all materials
        int index = 0;
        foreach (Renderer renderer in renderers)
        {
            Material[] currentMats = renderer.materials;
            for (int i = 0; i < currentMats.Length; i++)
            {
                if (currentMats[i].HasProperty("_Color"))
                {
                    // Store current color before flashing
                    Color originalColor = currentMats[i].color;
                    
                    // Blend with red for flash effect
                    currentMats[i].color = Color.Lerp(originalColor, hitFlashColor, hitFlashIntensity);
                }
                index++;
            }
            renderer.materials = currentMats;
        }
        
        // Wait for flash duration
        yield return new WaitForSeconds(hitFlashDuration);
        
        // If zombie died during flash, don't restore original colors
        if (isDead) yield break;
        
        // Restore original colors
        index = 0;
        foreach (Renderer renderer in renderers)
        {
            Material[] currentMats = renderer.materials;
            for (int i = 0; i < currentMats.Length; i++)
            {
                if (currentMats[i].HasProperty("_Color"))
                {
                    currentMats[i].color = originalColors[index];
                }
                index++;
            }
            renderer.materials = currentMats;
        }
        
        currentFlashRoutine = null;
    }

    public void InstaKill()
    {
        if (isDead) return;
        
        currentHealth = 0;
        Debug.Log($"{gameObject.name} was insta-killed!");
        Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        // Stop any current flash effect
        if (currentFlashRoutine != null)
        {
            StopCoroutine(currentFlashRoutine);
            currentFlashRoutine = null;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterKill(); // Increment kill count
        }

        // Disable collider to prevent further interaction
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Stop the zombie from moving
        ZombieAi aiScript = GetComponent<ZombieAi>();
        if (aiScript != null)
        {
            if (aiScript.agent != null) aiScript.agent.isStopped = true;
            aiScript.enabled = false;
        }
        
        // Start death animation coroutine
        StartCoroutine(DeathAnimation());
    }
    
    // Animated death effect without requiring an animator
    IEnumerator DeathAnimation()
    {
        // Apply death color to materials
        foreach (Renderer renderer in renderers)
        {
            Material[] mats = renderer.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i].HasProperty("_Color"))
                {
                    mats[i].color = deathColor;
                }
            }
            renderer.materials = mats;
        }
        
        // Make the zombie fall over
        float elapsedTime = 0f;
        Quaternion startRotation = transform.rotation;
        
        // Random fall direction (forward, backward, or to either side)
        float randomFallDirection = Random.Range(0f, 360f);
        Quaternion fallRotation = Quaternion.Euler(90f, transform.eulerAngles.y + randomFallDirection, 0f);
        
        Vector3 startPosition = transform.position;
        Vector3 endPosition = new Vector3(transform.position.x, 0.2f, transform.position.z); // Lower to ground
        
        while (elapsedTime < fallDeathDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fallDeathDuration;
            
            // Ease-in curve for natural falling
            t = 1f - Mathf.Cos(t * Mathf.PI * 0.5f);
            
            transform.rotation = Quaternion.Slerp(startRotation, fallRotation, t);
            transform.position = Vector3.Lerp(startPosition, endPosition, t);
            
            yield return null;
        }
        
        // Destroy after delay
        Destroy(gameObject, destroyDelay);
    }
}