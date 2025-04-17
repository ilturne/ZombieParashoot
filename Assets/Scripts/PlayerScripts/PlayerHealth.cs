using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float baseMaxHealth = 100f; 
    [SerializeField] private float maxHealth = 100f; // Default to baseMaxHealth
    [SerializeField] private float currentHealth; 

    [Header("UI")]
    [SerializeField] private HealthBar healthBar; 
    [SerializeField] private GameObject healthBarPrefab;

    // References to disable on death (Assign in Inspector or get in Start)
    [Header("Component References")]
    [SerializeField] private ThirdPersonMovement movementScript;
    [SerializeField] private WeaponManager weaponManagerScript; 
    [SerializeField] private CameraRoll cameraRollScript; // Keep if you want to explicitly disable

    // Event for death (GameManager listens to this)
    public System.Action OnPlayerDeath;
    
    private bool isDead = false; 

    void Start()
    {
        maxHealth = baseMaxHealth;
        currentHealth = baseMaxHealth;
        isDead = false;
        
        // --- Get component references if not assigned in Inspector ---
        if (movementScript == null) movementScript = GetComponent<ThirdPersonMovement>();
        // Assuming WeaponManager is on a child object like "WeaponHolder" 
        if (weaponManagerScript == null) weaponManagerScript = GetComponentInChildren<WeaponManager>(); 
        if (cameraRollScript == null) cameraRollScript = FindFirstObjectByType<CameraRoll>(); // Find camera if needed

        // --- Null checks ---
        if (movementScript == null) Debug.LogError("PlayerHealth: Movement Script reference missing!", this);
        if (weaponManagerScript == null) Debug.LogError("PlayerHealth: Weapon Manager Script reference missing!", this);
        if (cameraRollScript == null) Debug.LogWarning("PlayerHealth: Camera Roll Script reference missing (optional).", this);


        // --- Health Bar Setup ---
        if (healthBar == null && healthBarPrefab != null)
        {
            GameObject healthBarObj = Instantiate(healthBarPrefab, transform.position + Vector3.up * 2f, Quaternion.identity);
            healthBarObj.transform.SetParent(transform);
            healthBar = healthBarObj.GetComponent<HealthBar>();
        }
        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
            healthBar.SetHealth(currentHealth);
        }
        
        Debug.Log($"Player health initialized: {currentHealth}/{maxHealth} (Base Max: {baseMaxHealth})");
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return; 

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

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

    // Heal / Shield methods (simplified for brevity, ensure your versions are kept if different)
    public void HealUpToBaseMax(float healAmount) { /* Your existing logic */ 
        if (isDead) return;
        if (currentHealth >= baseMaxHealth) return;
        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, baseMaxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
         if (healthBar != null) healthBar.SetHealth(currentHealth);
        Debug.Log($"Player healed {healAmount}. Current health: {currentHealth}/{maxHealth}");
    }
    public void IncreaseMaxHealthAndHeal(float amountToAdd) { /* Your existing logic */
        if (isDead) return; 
        maxHealth += amountToAdd;
        currentHealth += amountToAdd; 
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth); 
        if (healthBar != null) {
            healthBar.SetMaxHealth(maxHealth);
            healthBar.SetHealth(currentHealth);
        }
        Debug.Log($"Player SHIELD increased max health. Current health: {currentHealth}/{maxHealth}");
    }

    // --- Getters ---
    public float GetCurrentHealth() { return currentHealth; }
    public float GetMaxHealth() { return maxHealth; }
    public float GetBaseMaxHealth() { return baseMaxHealth; }

    // --- Die method - disables components and invokes event ---
    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("PlayerHealth.Die() called. Disabling components & Invoking OnPlayerDeath event...");

        // --- Disable Player Control ---
        if (movementScript != null)
        {
            movementScript.enabled = false; 
        }
        if (weaponManagerScript != null)
        {
             weaponManagerScript.enabled = false; // Disable weapon switching/firing logic
        }
        if (cameraRollScript != null)
        {
            cameraRollScript.enabled = false; // Explicitly disable camera if needed
        }
        
        // --- Invoke the death event ---
        // The GameManager will listen for this event.
        OnPlayerDeath?.Invoke(); 
    }
}