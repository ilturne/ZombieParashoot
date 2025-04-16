using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float baseMaxHealth = 100f; // The initial, default maximum health
    [SerializeField] private float maxHealth;           // The current maximum health (can be increased by shield)
    [SerializeField] private float currentHealth;       // Player's current HP

    // UI Components
    [Header("UI")]
    [SerializeField] private HealthBar healthBar;  // Reference to the health bar
    [SerializeField] private GameObject healthBarPrefab; // Prefab to instantiate if healthBar not assigned

    // Optional: UI References (Assign these in the Inspector if you have them)
    [Header("Other Components (Optional)")]
    [SerializeField] private CameraRoll cameraRoll; 
    [SerializeField] private ThirdPersonMovement playerMovement;
    [SerializeField] private GameObject GameOverCanvas; // Reference to the Game Over UI canvas
    private GameManager gameManager;
    public System.Action OnPlayerDeath;
    
    void Start()
    {
        // Initialize health based on the base maximum
        maxHealth = baseMaxHealth;
        currentHealth = baseMaxHealth; // Start full
        
        // Set up health bar if not already assigned
        if (healthBar == null && healthBarPrefab != null)
        {
            // Instantiate the health bar prefab above the player
            GameObject healthBarObj = Instantiate(healthBarPrefab, transform.position + Vector3.up * 2f, Quaternion.identity);
            // Make it a child of the player
            healthBarObj.transform.SetParent(transform);
            // Get the HealthBar component
            healthBar = healthBarObj.GetComponent<HealthBar>();
        }
        
        // Initialize the health bar
        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
            healthBar.SetHealth(currentHealth);
        }
        
        Debug.Log($"Player health initialized: {currentHealth}/{maxHealth} (Base Max: {baseMaxHealth})");
    }

    public void TakeDamage(float damageAmount)
    {
        if (currentHealth <= 0) return; // Already dead

        currentHealth -= damageAmount;
        // Clamp health between 0 and the CURRENT maximum health
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        // Update health bar
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
        }

        Debug.Log($"Player took {damageAmount} damage. Current health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Use this for the "Health" power-up - Heals up to the BASE max (e.g., 100)
    public void HealUpToBaseMax(float healAmount)
    {
        if (currentHealth <= 0) return; // Cannot heal if dead
        // Don't heal if already at or above the original base max health
        if (currentHealth >= baseMaxHealth) return;

        currentHealth += healAmount;
        // Clamp first to the base max, then ensure it doesn't exceed the current absolute max (safety check)
        currentHealth = Mathf.Clamp(currentHealth, 0f, baseMaxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        // Update health bar
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
        }

        Debug.Log($"Player healed {healAmount} (capped at base max {baseMaxHealth}). Current health: {currentHealth}/{maxHealth}");
    }

    // Use this for the "Shield" power-up - Increases MAX and CURRENT health
    public void IncreaseMaxHealthAndHeal(float amountToAdd)
    {
        if (currentHealth <= 0) return; // Cannot apply if dead
        
        maxHealth += amountToAdd; // Increase max health
        currentHealth += amountToAdd; // Add health points
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        // Update health bar max and current values
        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
            healthBar.SetHealth(currentHealth);
        }

        Debug.Log($"Player SHIELD increased max health by {amountToAdd} to {maxHealth}. Current health: {currentHealth}/{maxHealth}");
    }


    // --- Getters ---
    public float GetCurrentHealth() { return currentHealth; }
    public float GetMaxHealth() { return maxHealth; }
    public float GetBaseMaxHealth() { return baseMaxHealth; }


    // --- Helper methods ---
    private void Die()
    {
        Debug.Log("Player has died!");
        // TODO: Implement actual death logic
        OnPlayerDeath?.Invoke();
        // gameObject.SetActive(false); // Example
        if (cameraRoll != null)
        {
            cameraRoll.enabled = false; // Disable camera roll on death
        }
        if (playerMovement != null)
        {
            playerMovement.enabled = false; // Disable player movement on death
        }
        if (gameManager != null)
        {
            GameOverCanvas.SetActive(true); // Show Game Over UI
            gameManager.PlayerDied();
        }
    }
}