using UnityEngine;

public class PowerUpDespawner : MonoBehaviour
{
    [Tooltip("How far behind the player the power-up must be before despawning.")]
    [SerializeField] private float despawnDistanceBehind = 20f; 

    [Tooltip("How often (in seconds) to check the distance.")]
    [SerializeField] private float checkInterval = 1.0f; 

    private Transform playerTransform;
    private float checkTimer;
    private float despawnDistanceBehindSqr; // Store squared distance for efficiency

    void Start()
    {
        // Find the player GameObject using its tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogError($"PowerUpDespawner on {gameObject.name} could not find the Player object tagged 'Player'. Despawning checks will not work.", this);
            enabled = false; // Disable script if player isn't found
            return;
        }

        // Pre-calculate squared distance for performance (avoid square roots)
        despawnDistanceBehindSqr = despawnDistanceBehind * despawnDistanceBehind;

        // Stagger the first check slightly across different power-ups
        checkTimer = Random.Range(0f, checkInterval); 
    }

    void Update()
    {
        // Don't run if player wasn't found
        if (playerTransform == null) return;

        // Countdown the timer
        checkTimer -= Time.deltaTime;
        if (checkTimer <= 0f)
        {
            checkTimer = checkInterval; // Reset timer
            CheckIfShouldDespawn();
        }
    }

    void CheckIfShouldDespawn()
    {
        // Vector from player pointing towards this power-up
        Vector3 directionToPowerUp = transform.position - playerTransform.position;
        
        // --- Check 1: Is the power-up generally behind the player? ---
        // Vector3.Dot gives a negative value if the angle between player's forward 
        // and the direction to the power-up is greater than 90 degrees.
        float dotForward = Vector3.Dot(playerTransform.forward, directionToPowerUp.normalized);

        // Use a small negative threshold (e.g. -0.1f) to ensure it's truly behind, not just directly to the side.
        if (dotForward < -0.1f) 
        {
            // --- Check 2: Is it far enough away? ---
            // Compare squared distance for efficiency
            float distanceSqr = directionToPowerUp.sqrMagnitude; 

            if (distanceSqr > despawnDistanceBehindSqr)
            {
                // Power-up is behind the player AND further than the threshold distance.
                // Debug.Log($"Despawning {gameObject.name}: Behind player (dot={dotForward}) and distance sqr {distanceSqr} > {despawnDistanceBehindSqr}"); // Optional: Confirmation log
                Destroy(gameObject); // Destroy this power-up instance
            }
        }
    }
}