using UnityEngine;

public class PowerUpItem : MonoBehaviour
{
    // Define the different types of power-ups
    public enum PowerUpType
    {
        Shield,     // Increases Max HP and Heals by effectAmount
        Health,     // Heals by effectAmount, capped at base max HP
        Bomb,       // (Future Implementation)
        InstaKill   // (Future Implementation)
    }

    [Header("Power-up Settings")]
    [Tooltip("Select the type of this power-up.")]
    [SerializeField] private PowerUpType type = PowerUpType.Shield;
    [Tooltip("The amount used for the power-up's effect (e.g., health amount for Shield/Health).")]
    [SerializeField] private float effectAmount = 50f;

    // --- Your Existing Visual Effects Variables ---
    [Header("Visual Effects")]
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.25f;
    [SerializeField] private float spinSpeed = 50f;

    private float startY;
    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
        startY = startPosition.y;
    }

    // --- Your Existing Update Method ---
    void Update()
    {
        float newY = startY + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();

            // Proceed only if the player has the PlayerHealth script
            if (playerHealth != null)
            {
                bool collected = false; // Flag to ensure we destroy the item only if collected

                // Determine action based on the power-up type set in the Inspector
                switch (type)
                {
                    case PowerUpType.Shield:
                        // Call the specific function in PlayerHealth for shield effect
                        playerHealth.IncreaseMaxHealthAndHeal(effectAmount);
                        Debug.Log($"Player collected SHIELD."); // Health script logs details
                        collected = true;
                        break;

                    case PowerUpType.Health:
                        // Call the specific function for health pack effect
                        playerHealth.HealUpToBaseMax(effectAmount);
                        Debug.Log($"Player collected HEALTH PACK."); // Health script logs details
                        collected = true;
                        break;

                    case PowerUpType.Bomb:
                        ThirdPersonMovement playerMovement = other.GetComponent<ThirdPersonMovement>();
                        if (playerMovement != null)
                        {
                             int bombsToAdd = Mathf.Max(1, Mathf.RoundToInt(effectAmount)); // Ensure at least 1
                             playerMovement.AddBombs(bombsToAdd);
                             Debug.Log($"Player collected BOMB(s). Added {bombsToAdd}.");
                             collected = true; // Mark as collected to destroy object
                        } else {
                             Debug.LogWarning($"Player object {other.name} does not have a ThirdPersonMovement component to add bombs to.");
                        }
                        break;

                    case PowerUpType.InstaKill:
                        ThirdPersonMovement playerMovementInstaKill = other.GetComponent<ThirdPersonMovement>();
                        if (playerMovementInstaKill != null)
                        {
                            playerMovementInstaKill.ActivateInstaKill(); // Call the activation method
                            collected = true;
                        } else {
                            Debug.LogWarning($"Player object {other.name} does not have a ThirdPersonMovement component to activate InstaKill.");
                        }
                        break;

                    default:
                        Debug.LogWarning($"Collected power-up of unhandled type: {type}");
                        break; // Don't destroy if type is unknown/unhandled
                }

                // Destroy the power-up object IF it was successfully processed
                if (collected)
                {
                    Destroy(gameObject);
                }
            }
            else
            {
                // Log a warning if the player doesn't have the health script
                Debug.LogWarning($"Collided object tagged 'Player' ({other.name}) is missing PlayerHealth component.");
            }
        }
    }
}