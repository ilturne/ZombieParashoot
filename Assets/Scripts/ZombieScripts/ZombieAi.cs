using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class ZombieAi : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5.0f;        // Speed for zombies to run at
    public float turnSpeed = 120f;
    [Tooltip("How often to update the path to the player")]
    public float pathUpdateInterval = 0.2f; // Shorter interval for more responsive following
    [Tooltip("Distance to start slowing down when approaching player")]
    public float slowingDistance = 2.0f;  // Start slowing down when close
    
    [Header("Target Settings")]
    public Transform player;
    public float attackRange = 1.0f;     // How close zombies need to get to attack
    public float sightRange = 30f;       // Added back for ZombieSpawner
    
    [Header("Attack Settings")]
    public float attackDamage = 10f;     // Damage dealt per attack
    public float attackCooldown = 1.0f;  // Time between attacks
    
    [Header("Animation")]
    public string runAnimParam = "isRunning";  // Only using run animation
    [Tooltip("How quickly to blend animations")]
    public float animationBlendSpeed = 0.2f;   // Smooth animation transitions
    
    [Header("Damage Visual")]
    public float damageFlashDuration = 0.2f;   // How long the zombie flashes red when damaged
    public Color damageFlashColor = Color.red; // Color to flash when damaged
    
    // Reference to components
    [HideInInspector]
    public NavMeshAgent agent;           // Made public for ZombieHealth
    private Animator zombieAnimator;
    private float lastAttackTime = 0f;
    private bool hasRunParam = false;
    private PlayerHealth playerHealth;    // Reference to the player's health script
    private Renderer[] renderers;         // All renderers on this zombie
    private Material[] originalMaterials; // Original materials to restore after damage
    private Color[] originalColors;       // Original colors to restore after damage
    
    // Movement state tracking
    private Vector3 lastPosition;
    private float lastPathUpdateTime = 0f;
    private bool isStuck = false;
    private float stuckCheckTime = 0f;
    private const float STUCK_CHECK_INTERVAL = 1.0f;
    private const float MINIMUM_MOVEMENT_DISTANCE = 0.1f;
    
    void Start()
    {
        // Find player if not assigned
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                // Try to get the PlayerHealth component
                playerHealth = playerObj.GetComponent<PlayerHealth>();
                if (playerHealth == null)
                {
                    Debug.LogWarning("Player doesn't have a PlayerHealth component. Zombies won't be able to deal damage!");
                }
            }
            else
            {
                Debug.LogError("ZombieAi: Player not found!");
            }
        }
        else
        {
            // Player transform was assigned directly, try to get the health component
            playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth == null)
            {
                Debug.LogWarning("Player doesn't have a PlayerHealth component. Zombies won't be able to deal damage!");
            }
        }
        
        // Get NavMeshAgent
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("ZombieAi: Missing NavMeshAgent component!");
            enabled = false;
            return;
        }
        
        // Get Animator
        zombieAnimator = GetComponent<Animator>();
        if (zombieAnimator != null)
        {
            // Check if the animator has our parameters
            CheckAnimatorParameters();
        }
        
        // Get all renderers for damage flash effect
        renderers = GetComponentsInChildren<Renderer>();
        StoreMaterialColors();
        
        // Configure agent for smoother movement
        ConfigureAgent();
        
        // Start running immediately - set destination to player
        if (player != null)
        {
            agent.SetDestination(player.position);
        }
        
        // Initialize position tracking for stuck detection
        lastPosition = transform.position;
        lastPathUpdateTime = Time.time;
        stuckCheckTime = Time.time + STUCK_CHECK_INTERVAL;
    }
    
    void ConfigureAgent()
    {
        if (agent == null) return;
        
        // Better acceleration and turning for smoother movement
        agent.speed = moveSpeed;
        agent.angularSpeed = turnSpeed;
        agent.acceleration = 12f; // Increased from 8f for faster response
        agent.stoppingDistance = 0.5f; // Small stopping distance so they don't bunch up
        
        // Improved path finding settings
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.radius = 0.4f; // Slightly smaller radius to reduce bumping
        agent.height = 1.8f;
        agent.avoidancePriority = Random.Range(40, 60); // Varied priority helps with crowding
        
        // Enable auto-braking for smoother stops
        agent.autoBraking = true;
    }
    
    // Store original materials and colors for damage flash effect
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
    
    void CheckAnimatorParameters()
    {
        if (zombieAnimator == null) return;
        
        // Check for run parameter only
        foreach (AnimatorControllerParameter param in zombieAnimator.parameters)
        {
            if (param.name == runAnimParam && param.type == AnimatorControllerParameterType.Bool)
            {
                hasRunParam = true;
                break;
            }
        }
        
        if (!hasRunParam && zombieAnimator.parameters.Length > 0)
        {
            Debug.LogWarning($"Animator does not have a '{runAnimParam}' boolean parameter. Running animation won't work.");
        }
    }
    
    void Update()
    {
        if (player == null) return;
        
        // Check if we're stuck and need to unstick ourselves
        if (Time.time >= stuckCheckTime)
        {
            CheckIfStuck();
            stuckCheckTime = Time.time + STUCK_CHECK_INTERVAL;
        }
        
        // Update path to player frequently for smoother following
        if (Time.time >= lastPathUpdateTime + pathUpdateInterval)
        {
            UpdatePathToPlayer();
            lastPathUpdateTime = Time.time;
        }
        
        // Get distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // Check if we're close enough to attack
        if (distanceToPlayer <= attackRange)
        {
            // Only attack if cooldown has elapsed
            if (Time.time > lastAttackTime + attackCooldown)
            {
                // Briefly stop to attack
                agent.isStopped = true;
                
                // Look at player while attacking
                FaceTarget(player.position);
                
                // Deal damage
                AttackPlayer();
                
                // Set cooldown
                lastAttackTime = Time.time;
                
                // Resume movement after a short delay
                StartCoroutine(ResumeMovementAfterAttack(0.5f));
            }
        }
        else
        {
            // Make sure we're not stopped
            agent.isStopped = false;
            
            // Set running animation
            if (zombieAnimator != null && hasRunParam)
            {
                zombieAnimator.SetBool(runAnimParam, true);
            }
            
            // Adjust speed based on distance for smoother approach
            if (distanceToPlayer < slowingDistance)
            {
                // Slow down when close to the player for smoother stopping
                float speedFactor = Mathf.Clamp01(distanceToPlayer / slowingDistance);
                agent.speed = Mathf.Lerp(moveSpeed * 0.5f, moveSpeed, speedFactor);
            }
            else
            {
                // Full speed when farther away
                agent.speed = moveSpeed;
            }
        }
    }
    
    void UpdatePathToPlayer()
    {
        if (player == null || agent == null) return;
        
        // Set destination to current player position
        agent.SetDestination(player.position);
    }
    
    void CheckIfStuck()
    {
        if (agent == null || agent.isStopped) return;
        
        // Calculate movement since last check
        float movementDistance = Vector3.Distance(transform.position, lastPosition);
        
        // If we've barely moved and we should be moving, we might be stuck
        if (movementDistance < MINIMUM_MOVEMENT_DISTANCE && !agent.pathPending && agent.remainingDistance > agent.stoppingDistance)
        {
            if (!isStuck)
            {
                isStuck = true;
                StartCoroutine(UnstickZombie());
            }
        }
        else
        {
            isStuck = false;
        }
        
        // Update last position for next check
        lastPosition = transform.position;
    }
    
    IEnumerator UnstickZombie()
    {
        // Try to unstick by temporarily adjusting path finding
        agent.radius *= 0.8f;
        agent.height *= 0.9f;
        
        // Jitter the destination slightly
        if (player != null)
        {
            Vector3 jitteredPosition = player.position + Random.insideUnitSphere * 2f;
            agent.SetDestination(jitteredPosition);
        }
        
        // Short delay for the new settings to take effect
        yield return new WaitForSeconds(0.5f);
        
        // Restore original settings
        ConfigureAgent();
        isStuck = false;
    }
    
    IEnumerator ResumeMovementAfterAttack(float delay)
    {
        yield return new WaitForSeconds(delay);
        agent.isStopped = false;
    }
    
    void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0; // Keep on horizontal plane
        
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * turnSpeed);
        }
    }
    
    void AttackPlayer()
    {
        // Use our cached playerHealth reference if available
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
            
            // Make player flash red
            StartCoroutine(FlashPlayerRed());
            
            Debug.Log($"Zombie attacked player for {attackDamage} damage at distance: {Vector3.Distance(transform.position, player.position)}");
            return;
        }
        
        // Fallback: Try to find the PlayerHealth component if we don't have it
        if (player != null)
        {
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(attackDamage);
                playerHealth = health; // Cache it for future use
                
                // Make player flash red
                StartCoroutine(FlashPlayerRed());
                
                Debug.Log($"Zombie attacked player for {attackDamage} damage");
            }
            else
            {
                Debug.LogWarning("Zombie tried to attack player but couldn't find PlayerHealth component!");
            }
        }
    }
    
    // Make player flash red when taking damage
    IEnumerator FlashPlayerRed()
    {
        if (player == null) yield break;
        
        // Get all renderers on the player
        Renderer[] playerRenderers = player.GetComponentsInChildren<Renderer>();
        if (playerRenderers.Length == 0) yield break;
        
        // Store original colors
        Color[] originalPlayerColors = new Color[playerRenderers.Length];
        for (int i = 0; i < playerRenderers.Length; i++)
        {
            if (playerRenderers[i].material.HasProperty("_Color"))
            {
                originalPlayerColors[i] = playerRenderers[i].material.color;
                playerRenderers[i].material.color = Color.red;
            }
        }
        
        // Wait for flash duration
        yield return new WaitForSeconds(damageFlashDuration);
        
        // Restore original colors
        for (int i = 0; i < playerRenderers.Length; i++)
        {
            if (playerRenderers[i].material.HasProperty("_Color"))
            {
                playerRenderers[i].material.color = originalPlayerColors[i];
            }
        }
    }
    
    void OnCollisionStay(Collision collision)
    {
        // Additional attack method - if we're touching the player and cooldown has elapsed
        if (collision.gameObject.CompareTag("Player") && Time.time > lastAttackTime + attackCooldown)
        {
            // Deal damage - this ensures damage works even if the distance calculation isn't perfect
            AttackPlayer();
            
            // Set cooldown
            lastAttackTime = Time.time;
        }
    }
    
    // Public method for bullets to damage the zombie
    public void TakeDamage(float amount)
    {
        // Flash zombie red to indicate damage
        StartCoroutine(FlashDamage());
        
        // Get ZombieHealth component if available
        ZombieHealth health = GetComponent<ZombieHealth>();
        if (health != null)
        {
            health.TakeDamage(amount);
        }
        else
        {
            // Simple implementation if no ZombieHealth component
            Debug.Log($"Zombie took {amount} damage from bullet");
        }
    }
    
    // Flash zombie red when damaged
    IEnumerator FlashDamage()
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
                    // Blend with red for flash effect
                    currentMats[i].color = damageFlashColor;
                }
                index++;
            }
            renderer.materials = currentMats;
        }
        
        // Wait for flash duration
        yield return new WaitForSeconds(damageFlashDuration);
        
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
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw sight range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Draw slowing distance
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // Orange
        Gizmos.DrawWireSphere(transform.position, slowingDistance);
    }
}