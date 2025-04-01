using UnityEngine;
using UnityEngine.AI; 
public class ZombieAi : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public NavMeshAgent agent; // Reference to the NavMeshAgent component
    public Transform player; // Reference to the player's transform
    public LayerMask whatIsGround, whatIsPlayer; // Layer masks for ground and player detection
    public float health; // Health of the zombie

    //attacking 
    public float timeBetweenAttacks;
    bool alreadyAttacked; // Flag to check if the zombie has already attacked

    //States
    public float sightRange, attackRange; // Ranges for sight and attack
    public bool playerInSightRange, playerInAttackRange; // Flags to check if the player is in range

    private void Awake()
    {
        // Get the NavMeshAgent component attached to this GameObject
        player = GameObject.Find("Player").transform;
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        playerInSightRange = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer); // Check if the player is in sight range
        playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, whatIsPlayer); // Check if the player is in attack range

        if (playerInSightRange && !playerInAttackRange) // If the player is in sight but not in attack range
        {
            ChasePlayer(); // Chase the player
        }

        if (playerInAttackRange && playerInSightRange) // If the player is in attack range and sight range
        {
            AttackPlayer(); // Attack the player
        }
    }

    private void ChasePlayer()
    {
        agent.SetDestination(player.position); // Set the agent's destination to the player's position
    }

    private void AttackPlayer()
    {
        // Stop the agent from moving
        agent.SetDestination(transform.position); // Set the agent's destination to its current position
        transform.LookAt(player); // Rotate the zombie to face the player
        if (!alreadyAttacked) // If the zombie hasn't already attacked
        {
            // Attack the player (e.g., shoot or deal damage)
            Debug.Log("Attacking Player!"); // Placeholder for attack logic

            alreadyAttacked = true; // Set the flag to true to prevent multiple attacks
            Invoke(nameof(ResetAttack), timeBetweenAttacks); // Reset the attack after a delay
        }
    }

    private void ResetAttack()
    {
        alreadyAttacked = false; // Reset the attack flag
    }

    public void TakeDamage(int damage)
    {
        health -= damage; // Reduce the zombie's health by the damage taken
        if (health <= 0) Invoke(nameof(DestroyEnemy), 0.5f); // Destroy the zombie if health is zero or less
    }

    private void DestroyEnemy()
    {
        Destroy(gameObject); // Destroy the zombie GameObject
    }
}
