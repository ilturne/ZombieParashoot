using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ZombieSpawner : MonoBehaviour
{
    [Header("Zombie Setup")]
    public GameObject zombiePrefab;
    public Transform playerTransform;
    
    [Header("Spawn Settings")]
    public float spawnInterval = 2.0f;
    public int maxZombies = 20;
    public int initialZombieCount = 10;
    
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
    public float minSpawnDistance = 15f;
    public float maxSpawnDistance = 25f;
    [Tooltip("Whether zombies can spawn in front of the player")]
    public bool allowFrontSpawns = true;
    [Tooltip("Whether zombies can spawn behind the player")]
    public bool allowBackSpawns = true;
    [Tooltip("Whether zombies can spawn to the sides of the player")]
    public bool allowSideSpawns = true;
    [Tooltip("Angle in degrees considered as 'front' of player (total angle)")]
    public float frontSpawnAngle = 90f;
    [Tooltip("Angle in degrees considered as 'back' of player (total angle)")]
    public float backSpawnAngle = 90f;
    public Vector3 spawnAreaCenter;
    public Vector3 spawnAreaSize = new Vector3(16f, 0f, 100f);
    
    [Header("Group Spawning")]
    [Tooltip("Whether zombies spawn in groups")]
    public bool spawnInGroups = true;
    [Tooltip("Minimum zombies per group")]
    public int minGroupSize = 2;
    [Tooltip("Maximum zombies per group")]
    public int maxGroupSize = 4;
    [Tooltip("How close together group members spawn")]
    public float groupSpawnRadius = 5f;
    [Tooltip("Chance that a spawn will trigger a group (0-1)")]
    public float groupSpawnChance = 0.4f;
    
    [Header("Boundary Settings")]
    public Transform leftBoundary;
    public Transform rightBoundary;
    public bool useSceneBoundaries = true;
    public bool restrictToBounds = true;
    
    [Header("NavMesh Settings")]
    public float navMeshSampleDistance = 3f;
    public int maxSpawnAttempts = 5;
    public bool ensureOnNavMesh = true;
    
    [Header("Zombie Movement")]
    public float zombieMinSpeed = 3.5f;
    public float zombieMaxSpeed = 5.5f;
    
    [Header("Camera Awareness")]
    [Tooltip("Avoid spawning zombies that would block player's view")]
    public bool avoidBlockingCamera = true;
    [Tooltip("Minimum angle (degrees) from player's forward vector to avoid blocking view")]
    public float minCameraAvoidanceAngle = 30f;
    [Tooltip("Distance from player within which to check for camera blocking")]
    public float cameraBlockCheckDistance = 10f;
    
    [Header("Debug")]
    public bool debugMode = true;
    public Color debugAreaColor = new Color(0f, 1f, 0f, 0.25f);
    public KeyCode spawnKey = KeyCode.Z;

    // Private references
    private CameraRoll cameraRoll;
    private Camera mainCamera;
    private bool initialSpawnComplete = false;
    private float minX, maxX;
    public bool isInBossArea = false;
    private FinalBossController nearbyBoss = null;
    private List<Vector3> recentSpawnPositions = new List<Vector3>();
    private int maxRecentPositions = 10;

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

        // Initialize with valid values
        if (minGroupSize < 1) minGroupSize = 1;
        if (maxGroupSize < minGroupSize) maxGroupSize = minGroupSize;
        if (groupSpawnChance < 0f) groupSpawnChance = 0f;
        if (groupSpawnChance > 1f) groupSpawnChance = 1f;

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
    
    // Check if player is in a boss area
    IEnumerator CheckForBossAreaRoutine()
    {
        WaitForSeconds checkInterval = new WaitForSeconds(0.5f);
    
        while (true)
        {
            if (detectBossArea && playerTransform != null)
            {
                Collider[] colliders = Physics.OverlapSphere(playerTransform.position, bossDetectionRadius * 1.5f);
                nearbyBoss = null;
            
                foreach (Collider col in colliders)
                {
                    FinalBossController boss = col.GetComponentInParent<FinalBossController>();
                    if (boss != null)
                    {
                        nearbyBoss = boss;
                        break;
                    }
                
                    if (col.CompareTag("Boss"))
                    {
                        nearbyBoss = col.GetComponentInParent<FinalBossController>();
                        if (nearbyBoss == null)
                        {
                            isInBossArea = true;
                            Debug.Log("Found boss-tagged object without controller");
                        }
                        break;
                    }
                }
            
                bool wasInBossArea = isInBossArea;
                isInBossArea = (nearbyBoss != null);
            
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
            
            // Draw spawn direction indicators
            if (playerTransform != null)
            {
                // Front cone
                if (allowFrontSpawns)
                {
                    Gizmos.color = Color.blue;
                    DrawDirectionCone(playerTransform.position, playerTransform.forward, frontSpawnAngle, maxSpawnDistance);
                }
                
                // Back cone
                if (allowBackSpawns)
                {
                    Gizmos.color = Color.red;
                    DrawDirectionCone(playerTransform.position, -playerTransform.forward, backSpawnAngle, maxSpawnDistance);
                }
                
                // Side indicators
                if (allowSideSpawns)
                {
                    Gizmos.color = Color.green;
                    // Approximate side zones with two lines on each side
                    Vector3 leftDir = Quaternion.Euler(0, -frontSpawnAngle/2, 0) * playerTransform.forward;
                    Vector3 rightDir = Quaternion.Euler(0, frontSpawnAngle/2, 0) * playerTransform.forward;
                    Vector3 backLeftDir = Quaternion.Euler(0, -180 + backSpawnAngle/2, 0) * playerTransform.forward;
                    Vector3 backRightDir = Quaternion.Euler(0, 180 - backSpawnAngle/2, 0) * playerTransform.forward;
                    
                    // The side areas are between these vectors
                    Gizmos.DrawLine(playerTransform.position, playerTransform.position + leftDir * maxSpawnDistance);
                    Gizmos.DrawLine(playerTransform.position, playerTransform.position + rightDir * maxSpawnDistance);
                    Gizmos.DrawLine(playerTransform.position, playerTransform.position + backLeftDir * maxSpawnDistance);
                    Gizmos.DrawLine(playerTransform.position, playerTransform.position + backRightDir * maxSpawnDistance);
                }
            }
            
            // Draw recent spawn positions
            Gizmos.color = Color.magenta;
            foreach (Vector3 pos in recentSpawnPositions)
            {
                Gizmos.DrawSphere(pos, 0.5f);
            }
        }
    }
    
    // Helper to draw direction cones in the editor
    void DrawDirectionCone(Vector3 position, Vector3 direction, float angle, float length)
    {
        Vector3 leftDir = Quaternion.Euler(0, -angle/2, 0) * direction;
        Vector3 rightDir = Quaternion.Euler(0, angle/2, 0) * direction;
        
        Gizmos.DrawLine(position, position + direction * length);
        Gizmos.DrawLine(position, position + leftDir * length);
        Gizmos.DrawLine(position, position + rightDir * length);
        
        // Draw an arc between the edges
        int segments = 10;
        Vector3 prevPos = position + leftDir * length;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float currentAngle = Mathf.Lerp(-angle/2, angle/2, t);
            Vector3 currentDir = Quaternion.Euler(0, currentAngle, 0) * direction;
            Vector3 currentPos = position + currentDir * length;
            Gizmos.DrawLine(prevPos, currentPos);
            prevPos = currentPos;
        }
    }

    IEnumerator InitialZombieSpawn()
    {
        yield return new WaitForSeconds(1f);
        
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
        
        // Spawn initial zombies in groups for more interesting formations
        int zombiesSpawned = 0;
        while (zombiesSpawned < actualInitialCount)
        {
            // Determine if this is a group spawn
            bool doGroupSpawn = spawnInGroups && Random.value < groupSpawnChance;
            
            if (doGroupSpawn)
            {
                // Determine group size (capped by remaining count)
                int remainingToSpawn = actualInitialCount - zombiesSpawned;
                int groupSize = Mathf.Min(Random.Range(minGroupSize, maxGroupSize + 1), remainingToSpawn);
                
                // Spawn the group
                zombiesSpawned += SpawnZombieGroup(groupSize);
            }
            else
            {
                // Regular single spawn
                if (SpawnZombie())
                {
                    zombiesSpawned++;
                }
            }
            
            yield return new WaitForSeconds(0.5f);
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
            // If in boss area, don't spawn or gradually remove zombies
            if (isInBossArea)
            {
                if (detectBossArea && nearbyBoss != null)
                {
                    GameObject[] zombies = GameObject.FindGameObjectsWithTag("Zombie");
                
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
                    
                        if (furthestZombie != null)
                        {
                            Destroy(furthestZombie);
                            Debug.Log("Removed zombie in boss area");
                        }
                    }
                }
            
                yield return new WaitForSeconds(1.0f);
                continue;
            }
        
            // Count current zombies
            GameObject[] currentZombies = GameObject.FindGameObjectsWithTag("Zombie");
        
            // Only spawn more if below max
            if (currentZombies.Length < maxZombies)
            {
                // Randomly determine if this is a group spawn
                bool doGroupSpawn = spawnInGroups && Random.value < groupSpawnChance;
                
                if (doGroupSpawn)
                {
                    // Calculate how many zombies we can spawn in this group
                    int maxAllowed = maxZombies - currentZombies.Length;
                    int groupSize = Mathf.Min(Random.Range(minGroupSize, maxGroupSize + 1), maxAllowed);
                    
                    if (groupSize >= minGroupSize)
                    {
                        SpawnZombieGroup(groupSize);
                    }
                    else
                    {
                        SpawnZombie(); // Fall back to single spawn if not enough capacity
                    }
                }
                else
                {
                    SpawnZombie();
                }
            
                // If we're well below the limit, spawn a couple more zombies to catch up
                if (currentZombies.Length < maxZombies * 0.5f)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        if (currentZombies.Length + i + 1 < maxZombies && SpawnZombie())
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
    
    // Spawn a group of zombies around a central point
    int SpawnZombieGroup(int groupSize)
    {
        if (groupSize <= 0) return 0;
        
        // Try to find a valid spawn position for the group center
        Vector3 groupCenterPosition = Vector3.zero;
        bool positionFound = false;
        
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            groupCenterPosition = GetRandomSpawnPosition();
            
            // Validate the position
            if (IsValidSpawnPosition(groupCenterPosition))
            {
                positionFound = true;
                break;
            }
        }
        
        if (!positionFound)
        {
            if (debugMode)
            {
                Debug.LogWarning("Failed to find valid group center position after " + maxSpawnAttempts + " attempts");
            }
            return 0;
        }
        
        // Spawn zombies around the center point
        int zombiesSpawned = 0;
        
        // First zombie at the center
        if (SpawnZombieAtPosition(groupCenterPosition))
        {
            zombiesSpawned++;
        }
        
        // Remaining zombies in a circle around the center
        for (int i = 1; i < groupSize; i++)
        {
            // Random position in a circle around center
            Vector2 randomCircle = Random.insideUnitCircle * groupSpawnRadius;
            Vector3 memberPosition = new Vector3(
                groupCenterPosition.x + randomCircle.x,
                groupCenterPosition.y,
                groupCenterPosition.z + randomCircle.y
            );
            
            // Validate and adjust position if needed
            if (ensureOnNavMesh)
            {
                UnityEngine.AI.NavMeshHit hit;
                if (UnityEngine.AI.NavMesh.SamplePosition(memberPosition, out hit, navMeshSampleDistance, UnityEngine.AI.NavMesh.AllAreas))
                {
                    memberPosition = hit.position;
                }
                else
                {
                    // If not on NavMesh, skip this group member
                    continue;
                }
            }
            
            // Apply boundary constraints
            if (restrictToBounds && useSceneBoundaries)
            {
                memberPosition.x = Mathf.Clamp(memberPosition.x, minX, maxX);
            }
            
            // Spawn the group member
            if (SpawnZombieAtPosition(memberPosition))
            {
                zombiesSpawned++;
            }
        }
        
        if (debugMode)
        {
            Debug.Log($"Spawned a group of {zombiesSpawned} zombies around {groupCenterPosition}");
        }
        
        return zombiesSpawned;
    }
    
    // Get a random spawn position based on allowed zones
    Vector3 GetRandomSpawnPosition()
    {
        // Determine which spawn zones are available
        List<SpawnZone> availableZones = new List<SpawnZone>();
        
        if (allowFrontSpawns) availableZones.Add(SpawnZone.Front);
        if (allowBackSpawns) availableZones.Add(SpawnZone.Back);
        if (allowSideSpawns) 
        {
            availableZones.Add(SpawnZone.Left);
            availableZones.Add(SpawnZone.Right);
        }
        
        // If no zones are enabled, default to all zones
        if (availableZones.Count == 0)
        {
            availableZones.Add(SpawnZone.Front);
            availableZones.Add(SpawnZone.Back);
            availableZones.Add(SpawnZone.Left);
            availableZones.Add(SpawnZone.Right);
        }
        
        // Pick a random zone
        SpawnZone chosenZone = availableZones[Random.Range(0, availableZones.Count)];
        
        // Generate position based on zone
        Vector3 spawnPos = Vector3.zero;
        float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
        
        switch (chosenZone)
        {
            case SpawnZone.Front:
                // In front of player with some angle variation
                float frontAngle = Random.Range(-frontSpawnAngle/2, frontSpawnAngle/2);
                Vector3 frontDir = Quaternion.Euler(0, frontAngle, 0) * playerTransform.forward;
                spawnPos = playerTransform.position + frontDir * distance;
                break;
                
            case SpawnZone.Back:
                // Behind player with some angle variation
                float backAngle = Random.Range(-backSpawnAngle/2, backSpawnAngle/2);
                Vector3 backDir = Quaternion.Euler(0, backAngle, 0) * -playerTransform.forward;
                spawnPos = playerTransform.position + backDir * distance;
                break;
                
            case SpawnZone.Left:
                // To the left of player
                float leftSideAngle = Random.Range(-90 - (180-frontSpawnAngle-backSpawnAngle)/4, 
                                                 -90 + (180-frontSpawnAngle-backSpawnAngle)/4);
                Vector3 leftDir = Quaternion.Euler(0, leftSideAngle, 0) * playerTransform.forward;
                spawnPos = playerTransform.position + leftDir * distance;
                break;
                
            case SpawnZone.Right:
                // To the right of player
                float rightSideAngle = Random.Range(90 - (180-frontSpawnAngle-backSpawnAngle)/4, 
                                                  90 + (180-frontSpawnAngle-backSpawnAngle)/4);
                Vector3 rightDir = Quaternion.Euler(0, rightSideAngle, 0) * playerTransform.forward;
                spawnPos = playerTransform.position + rightDir * distance;
                break;
        }
        
        // Set proper Y level
        spawnPos.y = playerTransform.position.y;
        
        return spawnPos;
    }
    
    // Validate if a position is suitable for spawning
    bool IsValidSpawnPosition(Vector3 position)
    {
        // Check if in boss area - don't spawn too close to boss
        if (isInBossArea && nearbyBoss != null)
        {
            float distanceToBoss = Vector3.Distance(position, nearbyBoss.transform.position);
            if (distanceToBoss < 15f)
            {
                return false;
            }
        }
        
        // Check camera blocking if enabled
        if (avoidBlockingCamera && mainCamera != null)
        {
            // Only check for positions relatively close to the player
            float distanceToPlayer = Vector3.Distance(position, playerTransform.position);
            if (distanceToPlayer < cameraBlockCheckDistance)
            {
                // Get direction from player to spawn position
                Vector3 directionToSpawn = (position - playerTransform.position).normalized;
                
                // Check angle between player's forward and direction to spawn
                float angle = Vector3.Angle(playerTransform.forward, directionToSpawn);
                
                // If the angle is too small, this zombie might block the view
                if (angle < minCameraAvoidanceAngle)
                {
                    return false;
                }
            }
        }
        
        // Check if position is visible in camera
        if (mainCamera != null)
        {
            Vector3 viewportPoint = mainCamera.WorldToViewportPoint(position);
            bool isVisible = (viewportPoint.x >= 0 && viewportPoint.x <= 1 && 
                             viewportPoint.y >= 0 && viewportPoint.y <= 1 && 
                             viewportPoint.z > 0);
            
            if (isVisible)
            {
                return false; // Don't spawn in camera view
            }
        }
        
        return true;
    }
    
    bool SpawnZombie()
    {
        Vector3 spawnPosition = Vector3.zero;
        bool positionFound = false;
        
        // Try multiple spawn positions
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            spawnPosition = GetRandomSpawnPosition();
            
            // Apply boundary constraints
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
                    if (IsValidSpawnPosition(spawnPosition))
                    {
                        positionFound = true;
                        break;
                    }
                }
            }
            else if (IsValidSpawnPosition(spawnPosition))
            {
                positionFound = true;
                break;
            }
        }
        
        if (!positionFound)
        {
            if (debugMode)
            {
                Debug.LogWarning("Failed to find valid spawn position after " + maxSpawnAttempts + " attempts");
            }
            return false;
        }
        
        return SpawnZombieAtPosition(spawnPosition);
    }
    
    bool SpawnZombieAtPosition(Vector3 position)
    {
        // Instantiate the zombie
        GameObject zombie = Instantiate(zombiePrefab, position, Quaternion.identity);
        zombie.tag = "Zombie";
        
        // Configure zombie movement and AI
        ConfigureZombie(zombie);
        
        // Record this position for debugging
        recentSpawnPositions.Add(position);
        if (recentSpawnPositions.Count > maxRecentPositions)
        {
            recentSpawnPositions.RemoveAt(0);
        }
        
        if (debugMode)
        {
            Debug.DrawLine(playerTransform.position, position, Color.red, 1f);
            Debug.Log($"Spawned zombie at {position}");
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
                agent.speed += cameraRoll.rollSpeed * 0.7f;
            }
            
            // Ensure the agent is enabled and has a path
            agent.enabled = true;
            agent.isStopped = false;
            agent.acceleration = 8f;
            
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
    
    // Enum to represent spawn zones
    private enum SpawnZone
    {
        Front,
        Back,
        Left,
        Right
    }
}