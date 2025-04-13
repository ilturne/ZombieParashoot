using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using System.Collections;

public class FinalBossController : MonoBehaviour
{
    [Header("Base Stats")]
    public float maxHealth = 1000f;
    private float health;
    public float attackRange = 4f;
    public float chaseRange = 30f;
    public float attackCooldown = 2f;
    public float walkSpeed = 2.0f;
    public float runSpeed = 4.0f;
    public float strafeSpeed = 3.0f;
    
    [Header("Phase 1")]
    public float phase1Damage = 25f;
    
    [Header("Phase 2")]
    public float phase2HealthThreshold = 500f;    // Health threshold to trigger phase 2
    public float phase2DamageMultiplier = 2.0f;   // Damage multiplier for phase 2
    public float phase2SpeedMultiplier = 1.5f;    // Speed multiplier for phase 2
    public Color phase2Color = Color.red;         // Color tint for phase 2 (optional)
    
    [Header("Teleportation")]
    public bool enableTeleportation = true;
    public float teleportCooldown = 10f;          // Time between teleports
    public float teleportMinDistance = 10f;       // Minimum distance to teleport
    public float teleportMaxDistance = 15f;       // Maximum distance to teleport
    public float teleportAttackChance = 0.7f;     // Chance to teleport behind player for attack
    public GameObject teleportEffectPrefab;       // Optional VFX for teleportation
    
    [Header("Advanced Behavior")]
    public float strafeDuration = 2.0f;
    public float strafeChance = 0.3f;
    public bool useDynamicMovement = true;

    // Private variables
    private float currentDamage;
    private float nextAttackTime = 0f;
    private float nextTeleportTime = 0f;
    private float nextMovementChangeTime = 0f;
    private bool isDead = false;
    private bool isPhaseTwo = false;
    private bool isFirstPhaseTransition = true;
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
    }

    void Update()
    {
        if (isDead || player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // Phase 2 transition check
        if (!isPhaseTwo && health <= phase2HealthThreshold)
        {
            TransitionToPhaseTwo();
        }

        // Teleportation check (phase 2 only)
        if (isPhaseTwo && enableTeleportation && Time.time >= nextTeleportTime)
        {
            TeleportBoss();
            return; // Skip rest of update to let teleport complete
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
        
        Debug.Log("Boss entering Phase 2! Damage increased to " + currentDamage);
        
        // Activate teleport system in phase 2
        if (enableTeleportation)
        {
            nextTeleportTime = Time.time + teleportCooldown * 0.5f; // First teleport happens sooner
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
        else if (roll < strafeChance + 0.1f && isPhaseTwo)
        {
            // Small chance to back up if in phase 2
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
                PlayAnimation(runState);
                targetPosition = player.position;
                break;
        }
        
        agent.SetDestination(targetPosition);
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

        nextAttackTime = Time.time + (isPhaseTwo ? attackCooldown * 0.7f : attackCooldown); // Faster attacks in phase 2
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

        health -= amount;
        
        if (health <= 0)
        {
            Die();
            return;
        }
        
        // In phase 2, boss has a chance to teleport when hit
        if (isPhaseTwo && enableTeleportation && Random.value < 0.3f && Time.time >= nextTeleportTime)
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

    void Die()
    {
        isDead = true;
        agent.isStopped = true;
        
        // Play a random death animation
        string deathAnim = GetRandomDeathAnimation();
        PlayAnimation(deathAnim);

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
        // Check if we're already playing this animation
        if (animator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
            return;
            
        // Use CrossFade for smoother transitions between animations
        animator.CrossFade(stateName, 0.2f, 0);
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

    // Color pulsing effect for phase 2
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
        float pulseMagnitude = 0.2f;
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