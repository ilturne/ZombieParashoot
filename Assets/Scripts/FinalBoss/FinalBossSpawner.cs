using UnityEngine;

public class FinalBossTriggerAndSpawner : MonoBehaviour
{
    [Header("Boss Setup")]
    public GameObject bossPrefab;

    [Header("Spawn Settings")]
    public float distanceInFrontOfPlayer = 6f;

    [Header("Camera Settings")]
    [Tooltip("Whether to adjust the camera when entering boss area")]
    public bool adjustCamera = true;

    private GameObject bossInstance;
    private bool triggered = false;
    private Camera mainCamera;
    private CameraRoll cameraRoll;

    private void Start()
    {
        // Find camera references
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraRoll = mainCamera.GetComponent<CameraRoll>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered || !other.CompareTag("Player")) return;
        triggered = true;

        // Spawn the boss
        SpawnBossInFrontOfPlayer(other.transform);

        // Adjust camera for boss fight
        if (adjustCamera)
        {
            AdjustCameraForBossFight(other.gameObject);
        }

        // Stop zombie spawning explicitly
        StopZombieSpawning();

        // Don't destroy the trigger object as we might need it for boss arena boundaries
        // Instead, just disable the trigger component
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.enabled = false;
        }
    }

    private void SpawnBossInFrontOfPlayer(Transform player)
    {
        if (bossPrefab == null)
        {
            Debug.LogError("FinalBossTrigger: No bossPrefab assigned.");
            return;
        }

        if (bossInstance != null) return;

        Vector3 spawnPos = player.position + player.forward * distanceInFrontOfPlayer;

        // Raycast to snap to ground
        if (Physics.Raycast(spawnPos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f))
        {
            spawnPos = hit.point;
        }
        else
        {
            spawnPos.y = player.position.y; // Fallback if no ground detected
        }

        bossInstance = Instantiate(bossPrefab, spawnPos, Quaternion.identity);
        Debug.Log("Final Boss spawned at: " + spawnPos);
        
        // Make sure the boss has the Boss tag
        bossInstance.tag = "Boss";
    }
    
    private void AdjustCameraForBossFight(GameObject player)
    {
        if (mainCamera == null) return;
        
        // Since ThirdPersonMovement is on the player, we need to find it
        ThirdPersonMovement playerMovement = player.GetComponent<ThirdPersonMovement>();
        
        if (playerMovement != null)
        {
            // Call the method to enter boss mode
            playerMovement.SetBossAreaMode(true);
            Debug.Log("Set player to boss area mode");
        }
        else
        {
            Debug.LogWarning("Could not find ThirdPersonMovement on player.");
            
            // Fallback: Still disable camera roll even if we can't find player movement
            if (cameraRoll != null)
            {
                cameraRoll.enabled = false;
                Debug.Log("Camera roll disabled for boss fight");
            }
        }
    }
    
   private void StopZombieSpawning()
    {
        // Find zombie spawner and disable it
        ZombieSpawner[] spawners = FindObjectsByType<ZombieSpawner>(FindObjectsSortMode.None);
        foreach (ZombieSpawner spawner in spawners)
        {
            // This relies on the isInBossArea field we added to ZombieSpawner
            spawner.isInBossArea = true;
            Debug.Log("Set ZombieSpawner.isInBossArea to true");
        }
    }
}