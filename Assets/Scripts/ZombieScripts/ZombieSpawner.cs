using UnityEngine;
using System.Collections;

public class ZombieSpawner : MonoBehaviour
{
    [Header("Zombie Setup")]
    public GameObject zombiePrefab;
    public Transform playerTransform;
    
    [Header("Spawn Settings")]
    public float spawnInterval = 2.0f;        // Slower spawn rate (increased from 0.5f)
    public int maxZombies = 20;               // Reduced from 50 to 20
    public int initialZombieCount = 10;       // Reduced from 20 to 10
    
    [Header("Boss Area Settings")]
    [Tooltip("When true, the spawner will reduce spawn rates when boss is active")]
    public bool detectBossArea = true; 
    [Tooltip("How far the spawner should check for a boss")]
    public float bossDetectionRadius = 50f;
    [Tooltip("Reduced max zombies when near boss")]
    public int bossAreaMaxZombies = 5;
    [Tooltip("Slower spawn interval near boss")]
    public float bossAreaSpawnInterval = 5.0f;
    
    [Header("Spawn Position")]
    public float minSpawnDistance = 15f;      // Increased min distance from player
    public float maxSpawnDistance = 25f;      // Increased max distance from player
    public Vector3 spawnAreaCenter;           // Center of spawning area (green zone)
    public Vector3 spawnAreaSize = new Vector3(16f, 0f, 100f); // Size of spawning area (width, height, depth)
    
    [Header("Boundary Settings")]
    public Transform leftBoundary;            // Reference to the left boundary object
    public Transform rightBoundary;           // Reference to the right boundary object
    public bool useSceneBoundaries = true;    // Whether to use actual boundary objects
    public bool restrictToBounds = true;      // Whether to clamp zombies to boundaries
    
    [Header("NavMesh Settings")]
    public float navMeshSampleDistance = 3f;  // Max distance to search for valid NavMesh position
    public int maxSpawnAttempts = 5;          // Maximum attempts to find a valid position for each zombie
    public bool ensureOnNavMesh = true;       // Whether to check zombies are on NavMesh
    
    [Header("Zombie Movement")]
    public float zombieMinSpeed = 3.5f;       // Lowered from 5f to 3.5f
    public float zombieMaxSpeed = 5.5f;       // Lowered from 8f to 5.5f
    
    [Header("Debug")]
    public bool debugMode = true;
    public Color debugAreaColor = new Color(0f, 1f, 0f, 0.25f); // Semi-transparent green
    public KeyCode spawnKey = KeyCode.Z;      // Key to manually spawn zombies for testing

    // Private references
    private CameraRoll cameraRoll;
    private Camera mainCamera;
    private bool initialSpawnComplete = false;
    private float minX, maxX;
    public bool isInBossArea = false;
    private FinalBossController nearbyBoss = null;

    void Start()
    {
        // Find references if not set
        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (playerTransform == null)
            {
                Debug.LogError("ZombieSpawner: Player not found! Disabling spawner.");
                enabled = false;
                return;
            }
        }

        // Get camera and camera roll
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraRoll = mainCamera.GetComponent<CameraRoll>();
            if (cameraRoll != null)
            {
                Debug.Log("Found CameraRoll script with speed: " + cameraRoll.rollSpeed);
            }
        }
        
        // Find boundaries
        if (useSceneBoundaries)
        {
            if (leftBoundary == null)
            {
                GameObject leftObj = GameObject.Find("BoundaryCheck Left");
                if (leftObj != null) leftBoundary = leftObj.transform;
            }
            
            if (rightBoundary == null)
            {
                GameObject rightObj = GameObject.Find("BoundaryCheck Right");
                if (rightObj != null) rightBoundary = rightObj.transform;
            }
            
            // Set boundary values
            if (leftBoundary != null && rightBoundary != null)
            {
                minX = leftBoundary.position.x + 2f; // 2 unit buffer from boundary
                maxX = rightBoundary.position.x - 2f; // 2 unit buffer from boundary
                Debug.Log($"Using scene boundaries: X range {minX} to {maxX}");
                
                // Update spawn area based on boundaries
                spawnAreaCenter = new Vector3((minX + maxX) / 2f, 0, playerTransform.position.z - 20f);
                spawnAreaSize.x = maxX - minX - 4f; // Width with buffers
            }
            else
            {
                Debug.LogWarning("Boundary objects not found, using default spawn area");
            }
        }
        
        // Verify setup
        if (zombiePrefab == null)
        {
            Debug.LogError("ZombieSpawner: Zombie prefab not assigned! Disabling spawner.");
            enabled = false;
            return;
        }

        // Start spawning
        StartCoroutine(InitialZombieSpawn());
        StartCoroutine(SpawnZombiesRoutine());
        StartCoroutine(CheckForBossAreaRoutine());
    }

    void Update()
    {
        // Manual spawn for testing
        if (debugMode && Input.GetKeyDown(spawnKey))
        {
            SpawnZombie();
        }
        
        // Update spawn area center to follow player
        spawnAreaCenter.z = playerTransform.position.z - 20f;
    }
    
    // NEW: Periodically check if player is in a boss area
    IEnumerator CheckForBossAreaRoutine()
    {
        WaitForSeconds checkInterval = new WaitForSeconds(0.5f); // Check more frequently
    
        while (true)
        {
            if (detectBossArea && playerTransform != null)
            {
                // Try to find a boss in the area - use a larger radius to detect earlier
                Collider[] colliders = Physics.OverlapSphere(playerTransform.position, bossDetectionRadius * 1.5f);
                nearbyBoss = null;
            
                foreach (Collider col in colliders)
                {
                    // Check for boss component
                    FinalBossController boss = col.GetComponentInParent<FinalBossController>();
                    if (boss != null)
                    {
                        nearbyBoss = boss;
                        break;
                    }
                
                    // Also check for boss tag
                    if (col.CompareTag("Boss"))
                    {
                        nearbyBoss = col.GetComponentInParent<FinalBossController>();
                        if (nearbyBoss == null)
                        {
                            // If the object has the Boss tag but no controller,
                            // we'll still count it as a boss area
                            isInBossArea = true;
                            Debug.Log("Found boss-tagged object without controller");
                        }
                        break;
                    }
                }
            
                // Update boss area status
                bool wasInBossArea = isInBossArea;
                isInBossArea = (nearbyBoss != null);
            
                // Log status change
                if (isInBossArea != wasInBossArea && debugMode)
                {
                    if (isInBossArea)
                    {
                        Debug.Log("Entered boss area - stopping zombie spawning");
                    }
                    else
                    {
                        Debug.Log("Left boss area - resuming zombie spawning");
                    }
                }
            }
        
            yield return checkInterval;
        }
    }

    void OnDrawGizmos()
    {
        if (debugMode)
        {
            // Draw spawn area
            Gizmos.color = debugAreaColor;
            Gizmos.DrawCube(spawnAreaCenter, spawnAreaSize);
            
            // Draw boundaries
            if (leftBoundary != null && rightBoundary != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(leftBoundary.position, leftBoundary.position + Vector3.forward * 100f);
                Gizmos.DrawLine(rightBoundary.position, rightBoundary.position + Vector3.forward * 100f);
            }
            
            // Draw boss detection radius
            if (playerTransform != null && detectBossArea)
            {
                Gizmos.color = isInBossArea ? Color.red : Color.yellow;
                Gizmos.DrawWireSphere(playerTransform.position, bossDetectionRadius);
            }
        }
    }

    IEnumerator InitialZombieSpawn()
    {
        yield return new WaitForSeconds(1f); // Brief delay to ensure NavMesh is loaded
        
        Debug.Log($"Starting initial spawn of {initialZombieCount} zombies...");
        
        // Check if we're starting in a boss area
        if (detectBossArea)
        {
            Collider[] colliders = Physics.OverlapSphere(playerTransform.position, bossDetectionRadius);
            foreach (Collider col in colliders)
            {
                if (col.CompareTag("Boss") || col.GetComponent<FinalBossController>() != null)
                {
                    isInBossArea = true;
                    break;
                }
            }
        }
        
        // If in boss area, reduce initial spawn
        int actualInitialCount = isInBossArea ? Mathf.Min(initialZombieCount, bossAreaMaxZombies) : initialZombieCount;
        
        // Spawn initial batch of zombies
        for (int i = 0; i < actualInitialCount; i++)
        {
            if (SpawnZombie())
            {
                yield return new WaitForSeconds(0.5f); // Increased delay between initial spawns (0.1f to 0.5f)
            }
        }
        
        initialSpawnComplete = true;
        Debug.Log("Initial zombie spawn complete!");
    }

    IEnumerator SpawnZombiesRoutine()
    {
        // Wait for initial spawn to complete
        yield return new WaitUntil(() => initialSpawnComplete);
    
        while (true)
        {
            // IMPORTANT: If in boss area, don't spawn any zombies at all
            if (isInBossArea)
            {
                // If we're in the boss area, remove any existing zombies
                if (detectBossArea && nearbyBoss != null)
                {
                    // Find all zombies
                    GameObject[] zombies = GameObject.FindGameObjectsWithTag("Zombie");
                
                    // If there are more than 0 zombies, remove one of them
                    // This gradually clears zombies rather than removing them all at once
                    if (zombies.Length > 0)
                    {
                        // Find the furthest zombie from the player
                        GameObject furthestZombie = zombies[0];
                        float maxDistance = 0f;
                    
                        foreach (GameObject zombie in zombies)
                        {
                            float distance = Vector3.Distance(zombie.transform.position, playerTransform.position);
                            if (distance > maxDistance)
                            {
                                maxDistance = distance;
                                furthestZombie = zombie;
                            }
                        }
                    
                        // Destroy the furthest zombie
                        if (furthestZombie != null)
                        {
                            Destroy(furthestZombie);
                            Debug.Log("Removed zombie in boss area");
                        }
                    }
                }
            
                // Wait before checking again
                yield return new WaitForSeconds(1.0f);
                continue; // Skip the rest of the loop
            }
        
            // Count current zombies
            GameObject[] currentZombies = GameObject.FindGameObjectsWithTag("Zombie");
        
            // Only spawn more if below max
            if (currentZombies.Length < maxZombies)
            {
                SpawnZombie();
            
                // If we're well below the limit, spawn a couple zombies to catch up
                if (currentZombies.Length < maxZombies * 0.5f)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        if (SpawnZombie())
                        {
                            yield return new WaitForSeconds(0.3f);
                        }
                    }
                }
            }
        
            // Wait before next spawn
            yield return new WaitForSeconds(spawnInterval);
        }
    }
    bool SpawnZombie()
    {
        Vector3 spawnPosition = Vector3.zero;
        bool positionFound = false;
        
        // Try multiple spawn positions
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            // Generate a random position within spawn area
            spawnPosition = new Vector3(
                Random.Range(spawnAreaCenter.x - spawnAreaSize.x/2, spawnAreaCenter.x + spawnAreaSize.x/2),
                playerTransform.position.y,
                Random.Range(spawnAreaCenter.z - spawnAreaSize.z/2, spawnAreaCenter.z + spawnAreaSize.z/2)
            );
            
            // Apply boundary constraints if enabled
            if (restrictToBounds && useSceneBoundaries)
            {
                spawnPosition.x = Mathf.Clamp(spawnPosition.x, minX, maxX);
            }
            
            // If in boss area, keep zombies further away
            if (isInBossArea && nearbyBoss != null)
            {
                float distanceToBoss = Vector3.Distance(spawnPosition, nearbyBoss.transform.position);
                if (distanceToBoss < 15f) // Minimum distance to boss
                {
                    continue; // Try another position
                }
            }
            
            // Ensure the spawn position is on the NavMesh
            if (ensureOnNavMesh)
            {
                UnityEngine.AI.NavMeshHit hit;
                if (UnityEngine.AI.NavMesh.SamplePosition(spawnPosition, out hit, navMeshSampleDistance, UnityEngine.AI.NavMesh.AllAreas))
                {
                    spawnPosition = hit.position;
                    positionFound = true;
                    break;
                }
            }
            else
            {
                positionFound = true;
                break;
            }
        }
        
        if (!positionFound)
        {
            if (debugMode)
            {
                Debug.LogWarning("Failed to find valid NavMesh position after " + maxSpawnAttempts + " attempts");
            }
            return false;
        }
        
        // Check if position is visible in camera
        if (mainCamera != null)
        {
            Vector3 viewportPoint = mainCamera.WorldToViewportPoint(spawnPosition);
            bool isVisible = (viewportPoint.x >= 0 && viewportPoint.x <= 1 && 
                             viewportPoint.y >= 0 && viewportPoint.y <= 1 && 
                             viewportPoint.z > 0);
            
            // If visible, try to find another position
            if (isVisible)
            {
                // Just push it further behind player
                spawnPosition.z -= 15f; // Increased from 10f to 15f
                
                // Recheck NavMesh
                if (ensureOnNavMesh)
                {
                    UnityEngine.AI.NavMeshHit hit;
                    if (!UnityEngine.AI.NavMesh.SamplePosition(spawnPosition, out hit, navMeshSampleDistance, UnityEngine.AI.NavMesh.AllAreas))
                    {
                        if (debugMode)
                        {
                            Debug.LogWarning("Adjusted position not on NavMesh, skipping spawn");
                        }
                        return false;
                    }
                    spawnPosition = hit.position;
                }
            }
        }
        
        // Instantiate the zombie
        GameObject zombie = Instantiate(zombiePrefab, spawnPosition, Quaternion.identity);
        zombie.tag = "Zombie";
        
        // Configure zombie movement and AI
        ConfigureZombie(zombie);
        
        if (debugMode)
        {
            Debug.DrawLine(playerTransform.position, spawnPosition, Color.red, 1f);
            Debug.Log($"Spawned zombie at {spawnPosition}");
        }
        
        return true;
    }

    void ConfigureZombie(GameObject zombie)
    {
        // Configure NavMeshAgent
        UnityEngine.AI.NavMeshAgent agent = zombie.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            // Set speed to match camera movement
            agent.speed = Random.Range(zombieMinSpeed, zombieMaxSpeed);
            
            if (cameraRoll != null && cameraRoll.IsRolling)
            {
                agent.speed += cameraRoll.rollSpeed * 0.7f; // Reduced from 0.8f to 0.7f
            }
            
            // Ensure the agent is enabled and has a path
            agent.enabled = true;
            agent.isStopped = false;
            agent.acceleration = 8f; // Reduced from 12f to 8f
            
            // Explicitly set the destination to the player
            if (playerTransform != null)
            {
                agent.SetDestination(playerTransform.position);
            }
        }
        else
        {
            Debug.LogWarning("Zombie prefab is missing NavMeshAgent component!");
        }
        
        // Configure AI component
        ZombieAi zombieAI = zombie.GetComponent<ZombieAi>();
        if (zombieAI != null)
        {
            zombieAI.player = playerTransform;
            zombieAI.sightRange = 50f;
            zombieAI.enabled = true;
        }
    }
}