using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using System.Collections;

public class FinalBossController : MonoBehaviour
{
    [Header("Base Stats")]
    public float maxHealth = 3000f;             
    private float health;
    public float attackRange = 4f;
    public float chaseRange = 30f;
    public float attackCooldown = 2f;
    public float walkSpeed = 2.0f;
    public float runSpeed = 4.0f;
    public float strafeSpeed = 3.0f;
    
    [Header("Phase 1")]
    public float phase1Damage = 15f;
    public float bulletDodgeChance = 0.3f;      // Chance to dodge bullets in phase 1
    
    [Header("Phase 2")]
    public float phase2HealthThreshold = 1800f;   // Adjusted for new max health
    public float phase2DamageMultiplier = 1.3f;
    public float phase2SpeedMultiplier = 1.7f;    // Increased from 1.5f
    public float phase2DodgeChance = 0.6f;        // Higher chance to dodge in phase 2
    public Color phase2Color = Color.red;
    public bool phase2BulletReflection = true;    // New feature: can reflect bullets back
    public float reflectionChance = 0.2f;         // Chance to reflect bullets
    
    [Header("Phase 3")]
    public float phase3HealthThreshold = 800f;    // New final phase
    public float phase3DamageMultiplier = 2.0f;   // Even more damage
    public float phase3SpeedMultiplier = 2.0f;    // Even faster
    public float phase3DodgeChance = 0.7f;        // High dodge chance
    public Color phase3Color = new Color(1f, 0.4f, 0f, 1f); // Orange/fire color
    public float healingPulseInterval = 15f;      // Time between healing pulses
    public float healingAmount = 150f;            // Amount healed per pulse
    
    [Header("Teleportation")]
    public bool enableTeleportation = true;
    public float teleportCooldown = 8f;           // Reduced from 10f
    public float teleportMinDistance = 10f;
    public float teleportMaxDistance = 15f;
    public float teleportAttackChance = 0.7f;
    public GameObject teleportEffectPrefab;
    
    [Header("Bullet Dodge")]
    public float dodgeDistance = 5f;              // How far the boss moves when dodging
    public float dodgeCooldown = 1.0f;            // Cooldown between dodges
    public float bulletDetectionRadius = 8f;      // How far to check for incoming bullets
    public LayerMask bulletLayer;                 // Layer for bullets
    public GameObject dodgeEffectPrefab;          // Optional effect for dodging
    
    [Header("Advanced Behavior")]
    public float strafeDuration = 2.0f;
    public float strafeChance = 0.4f;             // Increased from 0.3f
    public bool useDynamicMovement = true;
    public GameObject shockwavePrefab;            // Prefab for shockwave attack
    public float shockwaveCooldown = 15f;         // Time between shockwaves
    public float shockwaveDamage = 10f;           // Damage dealt by shockwave

    [Header("UI")]
    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private HealthBar healthBar;
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0, 3, 0);
    [SerializeField] private float healthBarScale = 0.015f;

    // Private variables
    private float currentDamage;
    private float nextAttackTime = 0f;
    private float nextTeleportTime = 0f;
    private float nextDodgeTime = 0f;
    private float nextShockwaveTime = 0f;
    private float nextHealingPulseTime = 0f;
    private float nextMovementChangeTime = 0f;
    private bool isDead = false;
    private bool isPhaseTwo = false;
    private bool isPhaseThree = false;
    private bool isFirstPhaseTransition = true;
    private bool isSecondPhaseTransition = true;
    private int currentMovementPattern = 0;
    private Material bossMaterial;
    private Color originalColor;

    private Animator animator;
    private NavMeshAgent agent;
    private Transform player;
    private Renderer bossRenderer;

    // Animation state names
    private readonly string[] attackStates = { "attack1", "attack2", "attack3", "attack4" };
    private readonly string[] getHitStates = { "getHit1", "getHit2", "getHit3" };
    private readonly string[] deathStates = { "Death1", "Death2", "Death3" };
    private readonly string rageState = "Rage";
    private readonly string runState = "Run";
    private readonly string idleState = "Idle1";
    private readonly string walkBackState = "walkBack";
    private readonly string strafeLeftState = "StrafeLeft";
    private readonly string strafeRightState = "StrafeRight";

    void Start()
    {
         animator = GetComponent<Animator>();
        if (animator == null)
        {
        Debug.LogError("FinalBossController: Missing Animator component!");
        }
        else
        {
            // Verify animation parameters exist
            foreach (string anim in attackStates)
            {
                VerifyAnimation(anim);
            }
            VerifyAnimation(runState);
            VerifyAnimation(idleState);
            VerifyAnimation(walkBackState);
            VerifyAnimation(strafeLeftState);
            VerifyAnimation(strafeRightState);
        }
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        bossRenderer = GetComponentInChildren<Renderer>();
        
        if (bossRenderer != null)
        {
            bossMaterial = bossRenderer.material;
            originalColor = bossMaterial.color;
        }
        
        // Initialize stats
        health = maxHealth;
        currentDamage = phase1Damage;
        
        // Set initial speed
        if (agent != null)
        {
            agent.speed = walkSpeed;
        }
        
        // Start in idle
        PlayAnimation(idleState);

        // Set up health bar
        SetupHealthBar();
        
        // Initialize timers
        nextShockwaveTime = Time.time + shockwaveCooldown;
        nextHealingPulseTime = Time.time + healingPulseInterval;
        
        // Start checking for bullets to dodge
        StartCoroutine(CheckForBullets());
    }
    
    void VerifyAnimation(string animName)
    {
        if (animator == null) return;
    
        bool found = false;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == animName)
            {
                found = true;
                break;
            }
        }

        if (!found)
        {
            Debug.LogWarning($"Animation '{animName}' not found in the Animator controller!");
        }
    }
    
    void ConfigureAgent()
    {
        if (agent == null) return;
    
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.speed = walkSpeed;
        agent.acceleration = 8f;
        agent.angularSpeed = 120f;
        agent.stoppingDistance = 0f; // Get right up to the player
    
        // Make sure the agent isn't stopped
        agent.isStopped = false;
        agent.ResetPath();
    }
    
    void SetupHealthBar()
    {
        // Create health bar if prefab is assigned
        if (healthBar == null && healthBarPrefab != null)
        {
            // Instantiate the health bar above the boss
            GameObject healthBarObj = Instantiate(healthBarPrefab, transform.position + healthBarOffset, Quaternion.identity);
            
            // Make it a child of the boss
            healthBarObj.transform.SetParent(transform);
            
            // Set scale
            healthBarObj.transform.localScale = new Vector3(healthBarScale, healthBarScale, healthBarScale);
            
            // Get the health bar component
            healthBar = healthBarObj.GetComponentInChildren<HealthBar>();
            
            // Set the initial values
            if (healthBar != null)
            {
                healthBar.SetMaxHealth(maxHealth);
                healthBar.SetHealth(health);
            }
        }
    }

    void Update()
    {
        if (isDead || player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // Phase transitions check
        if (!isPhaseTwo && health <= phase2HealthThreshold)
        {
            TransitionToPhaseTwo();
        }
        else if (!isPhaseThree && health <= phase3HealthThreshold)
        {
            TransitionToPhaseThree();
        }

        // Teleportation check (phase 2 and 3 only)
        if ((isPhaseTwo || isPhaseThree) && enableTeleportation && Time.time >= nextTeleportTime)
        {
            TeleportBoss();
            return; // Skip rest of update to let teleport complete
        }
        
        // Shockwave attack check (phase 2 and 3 only)
        if ((isPhaseTwo || isPhaseThree) && Time.time >= nextShockwaveTime && distance <= chaseRange)
        {
            PerformShockwaveAttack();
            return;
        }
        
        // Healing pulse check (phase 3 only)
        if (isPhaseThree && Time.time >= nextHealingPulseTime)
        {
            PerformHealingPulse();
        }

        // Main behavior logic
        if (distance <= attackRange)
        {
            Attack();
        }
        else if (distance <= chaseRange)
        {
            // Check if it's time to change movement pattern
            if (useDynamicMovement && Time.time >= nextMovementChangeTime)
            {
                ChangeMovementPattern();
            }
            
            // Chase with the current movement pattern
            ChaseWithPattern();
        }
        else
        {
            // If we're too far away, return to idle
            PlayAnimation(idleState);
            currentMovementPattern = 0; // Reset to direct approach when idle
        }
    }

    void TransitionToPhaseTwo()
    {
        if (!isFirstPhaseTransition) return;
        
        isPhaseTwo = true;
        isFirstPhaseTransition = false;
        
        // Play rage animation for phase transition
        PlayAnimation(rageState);
        
        // Increase damage and speed for phase 2
        currentDamage = phase1Damage * phase2DamageMultiplier;
        agent.speed *= phase2SpeedMultiplier;
        runSpeed *= phase2SpeedMultiplier;
        strafeSpeed *= phase2SpeedMultiplier;
        walkSpeed *= phase2SpeedMultiplier;
        
        // Apply color change effect if renderer exists
        if (bossMaterial != null)
        {
            StartCoroutine(PulseColor(originalColor, phase2Color, 2.0f));
        }
        
        // Change health bar color to indicate phase 2
        if (healthBar != null && healthBar.fill != null)
        {
            // This will use the gradient colors based on current health percentage
            healthBar.SetHealth(health);
        }
        
        Debug.Log("Boss entering Phase 2! Damage increased to " + currentDamage);
        
        // Trigger shockwave to mark phase transition
        PerformShockwaveAttack();
        
        // Activate teleport system in phase 2
        if (enableTeleportation)
        {
            nextTeleportTime = Time.time + teleportCooldown * 0.5f; // First teleport happens sooner
        }
    }
    
    void TransitionToPhaseThree()
    {
        if (!isSecondPhaseTransition) return;
        
        isPhaseThree = true;
        isSecondPhaseTransition = false;
        
        // Play rage animation for phase transition
        PlayAnimation(rageState);
        
        // Increase damage and speed for phase 3
        currentDamage = phase1Damage * phase3DamageMultiplier;
        agent.speed *= phase3SpeedMultiplier / phase2SpeedMultiplier; // Adjust from phase 2
        runSpeed *= phase3SpeedMultiplier / phase2SpeedMultiplier;
        strafeSpeed *= phase3SpeedMultiplier / phase2SpeedMultiplier;
        walkSpeed *= phase3SpeedMultiplier / phase2SpeedMultiplier;
        
        // Apply color change effect if renderer exists
        if (bossMaterial != null)
        {
            StartCoroutine(PulseColor(phase2Color, phase3Color, 2.0f));
        }
        
        // Change health bar color to indicate phase 3
        if (healthBar != null && healthBar.fill != null)
        {
            // This will use the gradient colors based on current health percentage
            healthBar.SetHealth(health);
        }
        
        Debug.Log("Boss entering Phase 3! Damage increased to " + currentDamage);
        
        // Perform an immediate shockwave and healing pulse to mark phase transition
        PerformShockwaveAttack();
        PerformHealingPulse();
        
        // Make teleportation more frequent
        if (enableTeleportation)
        {
            teleportCooldown *= 0.7f;
            nextTeleportTime = Time.time + teleportCooldown * 0.3f; // First teleport happens very soon
        }
    }

    void TeleportBoss()
    {
        nextTeleportTime = Time.time + teleportCooldown;
        
        // Cache original position for effect
        Vector3 originalPosition = transform.position;
        
        // Determine teleport destination
        Vector3 teleportPosition;
        bool attackTeleport = Random.value < teleportAttackChance;
        
        if (attackTeleport)
        {
            // Teleport behind player for surprise attack
            teleportPosition = player.position - player.forward * 3.0f;
        }
        else
        {
            // Teleport to random position within range
            Vector2 randomCircle = Random.insideUnitCircle.normalized * 
                                   Random.Range(teleportMinDistance, teleportMaxDistance);
            teleportPosition = new Vector3(
                player.position.x + randomCircle.x,
                transform.position.y,
                player.position.z + randomCircle.y
            );
        }
        
        // Ensure we can navigate to the teleport position
        NavMeshHit hit;
        if (NavMesh.SamplePosition(teleportPosition, out hit, 5.0f, NavMesh.AllAreas))
        {
            teleportPosition = hit.position;
        }
        else
        {
            // Fallback if no valid NavMesh position
            teleportPosition = transform.position;
            return; // Skip teleport if no valid position
        }
        
        // Spawn teleport effect at original position
        if (teleportEffectPrefab != null)
        {
            Instantiate(teleportEffectPrefab, originalPosition, Quaternion.identity);
        }
        
        // Teleport
        transform.position = teleportPosition;
        
        // Look at player
        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));
        
        // Spawn teleport effect at new position
        if (teleportEffectPrefab != null)
        {
            Instantiate(teleportEffectPrefab, teleportPosition, Quaternion.identity);
        }
        
        // If this was an attack teleport, immediately attack
        if (attackTeleport)
        {
            Attack();
        }
    }
    
    void PerformShockwaveAttack()
    {
        // Reset the cooldown for next shockwave
        nextShockwaveTime = Time.time + shockwaveCooldown;
        
        // Briefly stop to perform the shockwave
        agent.isStopped = true;
        
        // Play an attack animation (could be modified to be a special animation)
        PlayAnimation(attackStates[0]);
        
        // Spawn shockwave effect/prefab if assigned
        if (shockwavePrefab != null)
        {
            GameObject shockwave = Instantiate(shockwavePrefab, transform.position, Quaternion.identity);
            
            // Scale up the shockwave to cover a wider area
            float shockwaveRadius = 15f;
            shockwave.transform.localScale = new Vector3(shockwaveRadius, 1f, shockwaveRadius);
            
            // Destroy the shockwave after a short time
            Destroy(shockwave, 3f);
        }
        
        // Apply damage to player if within shockwave range
        float shockwaveRange = 12f;
        if (Vector3.Distance(transform.position, player.position) <= shockwaveRange)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(shockwaveDamage);
                Debug.Log($"Shockwave hit player for {shockwaveDamage} damage!");
            }
        }
        
        // Resume movement after a short delay
        StartCoroutine(ResumeMovementAfterDelay(1.0f));
        
        Debug.Log("Boss performed shockwave attack!");
    }
    
    void PerformHealingPulse()
    {
        // Reset the cooldown for next healing pulse
        nextHealingPulseTime = Time.time + healingPulseInterval;
        
        // Don't heal if already at max health
        if (health >= maxHealth) return;
        
        // Add health
        health += healingAmount;
        health = Mathf.Min(health, maxHealth);
        
        // Update health bar
        if (healthBar != null)
        {
            healthBar.SetHealth(health);
        }
        
        // Visual effect for healing
        StartCoroutine(HealingPulseEffect());
        
        Debug.Log($"Boss healed for {healingAmount}. Current health: {health}");
    }
    
    IEnumerator HealingPulseEffect()
    {
        // Store original color
        Color targetColor = isPhaseThree ? phase3Color : (isPhaseTwo ? phase2Color : originalColor);
        
        // Flash green for healing effect
        if (bossMaterial != null)
        {
            Color healColor = Color.green;
            float duration = 1.0f;
            float elapsed = 0f;
            
            // Flash to green
            while (elapsed < duration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration * 0.5f);
                bossMaterial.color = Color.Lerp(targetColor, healColor, t);
                yield return null;
            }
            
            // Flash back to original color
            elapsed = 0f;
            while (elapsed < duration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration * 0.5f);
                bossMaterial.color = Color.Lerp(healColor, targetColor, t);
                yield return null;
            }
        }
    }
    
    IEnumerator ResumeMovementAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (agent != null) agent.isStopped = false;
    }
    
    // Check for incoming bullets to dodge
    IEnumerator CheckForBullets()
    {
        while (!isDead)
        {
            // Only check for bullets if dodge is off cooldown
            if (Time.time >= nextDodgeTime)
            {
                float dodgeChance = isPhaseThree ? phase3DodgeChance : (isPhaseTwo ? phase2DodgeChance : bulletDodgeChance);
                
                // Check if we roll for a dodge attempt
                if (Random.value < dodgeChance)
                {
                    // Look for bullets in detection radius
                    Collider[] bullets = Physics.OverlapSphere(transform.position, bulletDetectionRadius, bulletLayer);
                    if (bullets.Length > 0)
                    {
                        // Found bullets - dodge them!
                        DodgeBullet(bullets[0].transform.position);
                    }
                }
            }
            
            // Wait a short time before checking again
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    void DodgeBullet(Vector3 bulletPosition)
    {
        // Set dodge cooldown
        nextDodgeTime = Time.time + dodgeCooldown;
        
        // Calculate dodge direction - perpendicular to bullet direction
        Vector3 bulletDirection = (bulletPosition - transform.position).normalized;
        Vector3 dodgeDirection;
        
        // 50/50 chance to dodge left or right
        if (Random.value < 0.5f)
        {
            dodgeDirection = Vector3.Cross(bulletDirection, Vector3.up);
        }
        else
        {
            dodgeDirection = Vector3.Cross(Vector3.up, bulletDirection);
        }
        
        // Calculate dodge position
        Vector3 dodgePosition = transform.position + dodgeDirection * dodgeDistance;
        
        // Ensure the dodge position is on the NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(dodgePosition, out hit, dodgeDistance, NavMesh.AllAreas))
        {
            dodgePosition = hit.position;
        }
        else
        {
            // If not on NavMesh, try the opposite direction
            dodgePosition = transform.position - dodgeDirection * dodgeDistance;
            if (NavMesh.SamplePosition(dodgePosition, out hit, dodgeDistance, NavMesh.AllAreas))
            {
                dodgePosition = hit.position;
            }
            else
            {
                // If still not on NavMesh, abort dodge
                return;
            }
        }
        
        // Spawn dodge effect if assigned
        if (dodgeEffectPrefab != null)
        {
            Instantiate(dodgeEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // Teleport to dodge position
        transform.position = dodgePosition;
        
        // Look at player after dodging
        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));
        
        Debug.Log("Boss dodged a bullet!");
    }

    void ChangeMovementPattern()
    {
        // Set time for next pattern change
        nextMovementChangeTime = Time.time + strafeDuration;
        
        // Determine next movement pattern
        float roll = Random.value;
        
        if (roll < strafeChance)
        {
            // 50% chance for left or right strafe if we choose to strafe
            currentMovementPattern = (Random.value < 0.5f) ? 1 : 2;
        }
        else if (roll < strafeChance + 0.15f && (isPhaseTwo || isPhaseThree))
        {
            // Increased chance to back up in later phases
            currentMovementPattern = 3;
        }
        else
        {
            // Default to direct approach
            currentMovementPattern = 0;
        }
    }

    void ChaseWithPattern()
    {
        // Don't interrupt attack or hit animations
        if (IsPlayingImportantAnimation()) return;

        Vector3 targetPosition;
    
        switch (currentMovementPattern)
        {
            case 1: // Strafe Left
                agent.speed = strafeSpeed;
                PlayAnimation(strafeLeftState);
                targetPosition = GetStrafePosition(true);
                break;
            
            case 2: // Strafe Right
                agent.speed = strafeSpeed;
                PlayAnimation(strafeRightState);
                targetPosition = GetStrafePosition(false);
                break;
            
            case 3: // Walk Backward
                agent.speed = walkSpeed;
                PlayAnimation(walkBackState);
                targetPosition = transform.position + (transform.position - player.position).normalized * 5f;
                break;
            
            default: // Direct Approach
                agent.speed = runSpeed;
                // Force the run animation to play
                PlayAnimation(runState);
                targetPosition = player.position;
                break;
        }
    
        // Ensure the path is set
        agent.SetDestination(targetPosition);
    
        // Debug to check if NavMeshAgent is actually moving
        if (agent.velocity.magnitude < 0.1f && !agent.pathPending)
        {
            // If not moving, try to adjust the destination slightly
            Vector3 adjustedPosition = targetPosition + Random.insideUnitSphere * 2f;
            agent.SetDestination(adjustedPosition);
            Debug.Log("Boss was stuck, adjusting destination");
        }
    }

    Vector3 GetStrafePosition(bool isLeft)
    {
        // Calculate a position to the left or right of the boss relative to the player
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        Vector3 strafeDirection = isLeft ? 
            Vector3.Cross(directionToPlayer, Vector3.up) : 
            Vector3.Cross(Vector3.up, directionToPlayer);
        
        return transform.position + strafeDirection * 5f;
    }

    void Attack()
    {
        if (Time.time < nextAttackTime || IsPlayingImportantAnimation()) return;

        // Faster attacks in later phases
        float phaseCooldownMultiplier = isPhaseThree ? 0.5f : (isPhaseTwo ? 0.7f : 1.0f);
        nextAttackTime = Time.time + (attackCooldown * phaseCooldownMultiplier);
        
        agent.ResetPath();
        transform.LookAt(player);

        // Choose a random attack animation
        string attackAnim = GetRandomAttackAnimation();
        PlayAnimation(attackAnim);

        // Apply damage
        Invoke(nameof(ApplyDamage), 0.5f);
    }

    void ApplyDamage()
    {
        if (isDead || player == null) return;
        
        // Only apply damage if still within range (player might have moved)
        if (Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(currentDamage);
                Debug.Log($"Boss dealt {currentDamage} damage to player");
            }
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        
        // Check for bullet dodge
        float dodgeChance = isPhaseThree ? phase3DodgeChance : (isPhaseTwo ? phase2DodgeChance : bulletDodgeChance);
        if (Random.value < dodgeChance && Time.time >= nextDodgeTime)
        {
            // Successfully dodged!
            nextDodgeTime = Time.time + dodgeCooldown;
            
            // Show dodge effect
            if (dodgeEffectPrefab != null)
            {
                Instantiate(dodgeEffectPrefab, transform.position, Quaternion.identity);
            }
            
            Debug.Log("Boss dodged an attack!");
            return;
        }
        
        // Check for bullet reflection in phase 2 and 3
        if (phase2BulletReflection && isPhaseTwo && Random.value < reflectionChance)
        {
            // Reflect bullet back at player
            ReflectBullet();
            Debug.Log("Boss reflected bullet back at player!");
            return;
        }

        health -= amount;
        
        // Update health bar
        if (healthBar != null)
        {
            healthBar.SetHealth(health);
        }
        
        if (health <= 0)
        {
            Die();
            return;
        }
        
        // In phase 2 or 3, boss has a chance to teleport when hit
        if ((isPhaseTwo || isPhaseThree) && enableTeleportation && Random.value < 0.4f && Time.time >= nextTeleportTime)
        {
            // Reset teleport cooldown to allow emergency teleport
            nextTeleportTime = 0f;
            TeleportBoss();
        }
        else
        {
            // Play a random hit animation
            string hitAnim = GetRandomHitAnimation();
            PlayAnimation(hitAnim);
        }
    }
    
    void ReflectBullet()
    {
        // This would ideally spawn a projectile going toward the player
        // For now, we'll just apply some damage to the player
        if (player != null)
        {
            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                float reflectDamage = 15f;
                ph.TakeDamage(reflectDamage);
                Debug.Log($"Boss reflected {reflectDamage} damage to player");
            }
        }
    }

    void Die()
    {
        isDead = true;
        agent.isStopped = true;
        
        // Play a random death animation
        string deathAnim = GetRandomDeathAnimation();
        PlayAnimation(deathAnim);

        // Hide health bar
        if (healthBar != null)
        {
            healthBar.gameObject.transform.parent.gameObject.SetActive(false);
        }

        Invoke(nameof(ReturnToMenu), 5f);
    }

    void ReturnToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    // Helper methods for animation selection
    string GetRandomAttackAnimation()
    {
        return attackStates[Random.Range(0, attackStates.Length)];
    }

    string GetRandomHitAnimation()
    {
        return getHitStates[Random.Range(0, getHitStates.Length)];
    }

    string GetRandomDeathAnimation()
    {
        return deathStates[Random.Range(0, deathStates.Length)];
    }

    void PlayAnimation(string stateName)
    {
        if (animator == null) return;
    
        // Skip if we're already playing this animation
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName(stateName) && stateInfo.normalizedTime < 0.9f)
            return;
        
        // Force the animation to play by resetting the normalized time
        animator.CrossFade(stateName, 0.2f, 0);
    
        // Debug animation state
        Debug.Log($"Boss playing animation: {stateName}");
    }

    // Check if we're playing an animation that shouldn't be interrupted
    bool IsPlayingImportantAnimation()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        
        // Check if currently in attack, hit, or rage animations
        foreach (string attackState in attackStates)
        {
            if (stateInfo.IsName(attackState)) return true;
        }
        
        foreach (string hitState in getHitStates)
        {
            if (stateInfo.IsName(hitState)) return true;
        }
        
        foreach (string deathState in deathStates)
        {
            if (stateInfo.IsName(deathState)) return true;
        }
        
        if (stateInfo.IsName(rageState)) return true;
        
        return false;
    }

    // Color pulsing effect for phase 2 and 3
    IEnumerator PulseColor(Color startColor, Color targetColor, float duration)
    {
        float elapsed = 0f;
        
        // Initial pulse to target color
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            bossMaterial.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }
        
        // Ongoing subtle pulsing
        float pulseMagnitude = 0.3f; // Increased from 0.2f
        while (true)
        {
            // Pulse between target color and slightly darker
            float pulse = (Mathf.Sin(Time.time * 3f) * pulseMagnitude) + (1f - pulseMagnitude);
            bossMaterial.color = new Color(
                targetColor.r * pulse,
                targetColor.g * pulse,
                targetColor.b * pulse,
                targetColor.a
            );
            yield return null;
        }
    }
}