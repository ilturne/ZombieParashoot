using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class ZombieAi : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float turnSpeed = 120f;
    public float pathUpdateInterval = 0.2f;
    public float slowingDistance = 2f;

    [Header("Combat")]
    public Transform player;
    public float sightRange = 30f;
    public float attackRange = 1f;
    public float attackDamage = 10f;
    public float attackCooldown = 1f;

    [Header("Animation")]
    public string runParam = "isRunning";

    [Header("Damage Flash")]
    public float flashDuration = 0.2f;
    public Color flashColor = Color.red;
    public NavMeshAgent agent;
    private Animator animator;
    private PlayerHealth playerHealth;
    private Renderer[] renderers;
    private Color[] originalColors;

    private float nextPathTime;
    private float nextAttackTime;
    private float nextStuckCheck;
    private Vector3 lastPosition;
    private bool isStuck;

    void Start()
    {
        // find player + health
        if (player == null)
            player = GameObject.FindWithTag("Player")?.transform;
        if (player != null)
            playerHealth = player.GetComponent<PlayerHealth>();

        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // grab renderers for damage flash
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            originalColors[i] = renderers[i].material.color;

        // configure agent
        agent.speed = moveSpeed;
        agent.angularSpeed = turnSpeed;
        agent.acceleration = 12f;
        agent.stoppingDistance = 0.5f;
        agent.autoBraking = true;

        // init timers & positions
        lastPosition = transform.position;
        nextPathTime = Time.time;
        nextStuckCheck = Time.time + 1f;

        // start chasing immediate
        if (player != null)
            agent.SetDestination(player.position);
    }

    void Update()
    {
        if (player == null) return;

        float now = Time.time;
        float dist = Vector3.Distance(transform.position, player.position);

        // stuck recovery
        if (now >= nextStuckCheck)
        {
            nextStuckCheck = now + 1f;
            float moved = Vector3.Distance(transform.position, lastPosition);
            if (moved < 0.1f && agent.hasPath && agent.remainingDistance > agent.stoppingDistance && !isStuck)
                StartCoroutine(Unstick());
            lastPosition = transform.position;
        }

        // update path
        if (now >= nextPathTime)
        {
            agent.SetDestination(player.position);
            nextPathTime = now + pathUpdateInterval;
        }

        // attacking?
        if (dist <= attackRange && now >= nextAttackTime)
        {
            agent.isStopped = true;
            FaceTarget(player.position);
            AttackPlayer();
            nextAttackTime = now + attackCooldown;
            StartCoroutine(ResumeAfterAttack(0.5f));
            return;
        }

        // chasing
        agent.isStopped = false;
        if (animator) animator.SetBool(runParam, true);

        // slow when close
        if (dist < slowingDistance)
            agent.speed = Mathf.Lerp(moveSpeed * 0.5f, moveSpeed, dist / slowingDistance);
        else
            agent.speed = moveSpeed;
    }

    void AttackPlayer()
    {
        if (playerHealth == null)
            playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
            StartCoroutine(FlashPlayerRed());
        }
    }

    IEnumerator ResumeAfterAttack(float delay)
    {
        yield return new WaitForSeconds(delay);
        agent.isStopped = false;
    }

    IEnumerator Unstick()
    {
        isStuck = true;
        agent.radius *= 0.8f;
        agent.SetDestination(player.position + Random.insideUnitSphere * 2f);
        yield return new WaitForSeconds(0.5f);
        agent.radius /= 0.8f;
        isStuck = false;
    }

    void FaceTarget(Vector3 target)
    {
        Vector3 dir = (target - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(dir), Time.deltaTime * turnSpeed);
    }

    IEnumerator FlashPlayerRed()
    {
        var rends = player.GetComponentsInChildren<Renderer>();
        Color[] orig = new Color[rends.Length];
        for (int i = 0; i < rends.Length; i++)
        {
            orig[i] = rends[i].material.color;
            rends[i].material.color = flashColor;
        }
        yield return new WaitForSeconds(flashDuration);
        for (int i = 0; i < rends.Length; i++)
            rends[i].material.color = orig[i];
    }

    // called by bullets
    public void TakeDamage(float dmg)
    {
        StartCoroutine(FlashSelf());
        var zh = GetComponent<ZombieHealth>();
        if (zh != null) zh.TakeDamage(dmg);
        else Debug.Log($"Zombie hit for {dmg}");
    }

    IEnumerator FlashSelf()
    {
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].material.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].material.color = originalColors[i];
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = new Color(1, 0.5f, 0, 0.5f);
        Gizmos.DrawWireSphere(transform.position, slowingDistance);
    }
}