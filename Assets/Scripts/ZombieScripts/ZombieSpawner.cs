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
        }
    }

    IEnumerator InitialZombieSpawn()
    {
        yield return new WaitForSeconds(1f); // Brief delay to ensure NavMesh is loaded
        
        Debug.Log($"Starting initial spawn of {initialZombieCount} zombies...");
        
        // Spawn initial batch of zombies
        for (int i = 0; i < initialZombieCount; i++)
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
            // Count current zombies
            GameObject[] zombies = GameObject.FindGameObjectsWithTag("Zombie");
            
            // Spawn more if below max
            if (zombies.Length < maxZombies)
            {
                SpawnZombie();
                
                // If we're well below the limit, spawn a couple zombies to catch up
                // Reduced from 3 to 2 zombies when below 50% capacity
                if (zombies.Length < maxZombies * 0.5f)
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
            
            // Wait before next spawn - increased interval
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
        
        // Configure AI component - FIXED: Changed from EnhancedZombieAi to ZombieAi
        ZombieAi zombieAI = zombie.GetComponent<ZombieAi>();
        if (zombieAI != null)
        {
            zombieAI.player = playerTransform;
            zombieAI.sightRange = 50f;
            zombieAI.enabled = true;
        }
    }
}