using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float baseMaxHealth = 100f; // The initial, default maximum health
    [SerializeField] private float maxHealth;           // The current maximum health (can be increased by shield)
    [SerializeField] private float currentHealth;       // Player's current HP

    // Optional: UI References (Assign these in the Inspector if you have them)
    [Header("UI (Optional)")]
    //[SerializeField] private Slider healthSlider;

    // Basic event for death (other scripts can subscribe to this)
    public System.Action OnPlayerDeath;

    void Start()
    {
        // Initialize health based on the base maximum
        currentHealth = baseMaxHealth; // Start full
        //UpdateHealthUI();
        Debug.Log($"Player health initialized: {currentHealth}/{maxHealth} (Base Max: {baseMaxHealth})");
    }

    public void TakeDamage(float damageAmount)
    {
        if (currentHealth <= 0) return; // Already dead

        currentHealth -= damageAmount;
        // Clamp health between 0 and the CURRENT maximum health
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        Debug.Log($"Player took {damageAmount} damage. Current health: {currentHealth}/{maxHealth}");
        //UpdateHealthUI();

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

        Debug.Log($"Player healed {healAmount} (capped at base max {baseMaxHealth}). Current health: {currentHealth}/{maxHealth}");
        //UpdateHealthUI();
    }

    // Use this for the "Shield" power-up - Increases MAX and CURRENT health
    public void IncreaseMaxHealthAndHeal(float amountToAdd)
    {
        if (currentHealth <= 0 && currentHealth != maxHealth) return; // Cannot apply if dead
        currentHealth += amountToAdd; // Add health points
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        Debug.Log($"Player SHIELD increased max health by {amountToAdd} to {maxHealth}. Current health: {currentHealth}/{maxHealth}");
        //UpdateHealthUI(); // Update UI to reflect new max and current health
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
    }

    // private void UpdateHealthUI()
    // {
    //     if (healthSlider != null)
    //     {
    //         // Update the slider's max value in case maxHealth changed
    //         healthSlider.maxValue = maxHealth;
    //         healthSlider.value = currentHealth;
    //     }
    // }
}