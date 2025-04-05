using UnityEngine;
using UnityEngine.AI; // Required for NavMeshAgent

[RequireComponent(typeof(NavMeshAgent))] // Ensures NavMeshAgent is present
public class ZombieAi : MonoBehaviour
{
    // --- References (Assign in Inspector or find in Awake) ---
    [Header("References")]
    [SerializeField] public NavMeshAgent agent;
    [SerializeField] private Transform player;
    // Optional: [SerializeField] private Animator animator;

    [Header("Detection Settings")]
    [SerializeField] private LayerMask whatIsPlayer; // Assign your Player layer in Inspector!
    [SerializeField] private float sightRange = 15f;
    [SerializeField] private float attackRange = 2f;

    [Header("Attack Settings")]
    [SerializeField] private float timeBetweenAttacks = 1.5f;
    [SerializeField] private int attackDamage = 10; // How much damage the zombie deals

    // --- State Variables ---
    private bool playerInSightRange;
    private bool playerInAttackRange;
    private bool alreadyAttacked; // Cooldown flag for attacks

    void Awake()
    {
        // Get components automatically
        agent = GetComponent<NavMeshAgent>();

        // Find player by tag - MORE RELIABLE!
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            Debug.LogError($"Zombie {name}: Cannot find GameObject tagged 'Player'. AI will not function.", this);
            enabled = false; // Disable AI if player isn't found
        }

        // Basic validation
        if (agent == null)
        {
            Debug.LogError($"Zombie {name}: NavMeshAgent component not found!", this);
            enabled = false;
        }
         // Ensure stopping distance is reasonable for attacking
        if (agent != null && agent.stoppingDistance >= attackRange) {
             Debug.LogWarning($"Zombie {name}: NavMeshAgent stopping distance ({agent.stoppingDistance}) should ideally be slightly less than attack range ({attackRange}) to allow attacks.", this);
             // Consider setting it automatically: agent.stoppingDistance = attackRange * 0.9f;
        }
    }

    void Update()
    {
        // Don't run update if player wasn't found
        if (player == null) return;

        // --- Check Player Proximity ---
        // Use CheckSphere - remember this detects through walls unless you add Raycasting
        playerInSightRange = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);
        playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, whatIsPlayer);

        // --- State Machine Logic ---
        if (playerInAttackRange)
        {
            AttackPlayer();
        }
        else if (playerInSightRange)
        {
            ChasePlayer(); 
        }
        else
        {
            Idle(); // Otherwise, IDLE!
        }
    }

    private void Idle()
    {
        
        if (agent.hasPath) // Only stop if currently moving
        {
             agent.ResetPath();
        }
    }

    private void ChasePlayer()
    {
        agent.SetDestination(player.position);
    }

    private void AttackPlayer()
    {
        agent.SetDestination(transform.position);

        transform.LookAt(player.position); // Look directly at player center
        
        // Check attack cooldown
        if (!alreadyAttacked)
        {
            // --- Perform Attack ---
            Debug.Log($"{name}: Attacking Player!"); // Log the attack attempt

            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
            else
            {
                Debug.LogWarning($"{name}: Could not find PlayerHealth component on Player to deal damage.");
            }

            // Optional: Trigger Attack animation
            // animator?.SetBool("IsChasing", false);
            // animator?.SetTrigger("Attack"); // Use a trigger for one-shot attack anims

            // --- End Attack ---

            alreadyAttacked = true; 
            Invoke(nameof(ResetAttack), timeBetweenAttacks);
        }
    }

    private void ResetAttack()
    {
        alreadyAttacked = false; // Allow attacking again
    }

    // Optional: Draw gizmos in the editor to visualize ranges
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}