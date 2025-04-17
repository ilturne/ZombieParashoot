using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Pellet : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 30f;
    public float damage = 10f;
    public float lifetime = 3f;

    [Header("Hit Effect")]
    public GameObject impactEffectPrefab;

    [Header("Collision Layers")]
    public LayerMask hitLayers; // assign “Zombie” and “Boss” layers here

    private Rigidbody rb;
    private Vector3 lastPosition;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void OnEnable()
    {
        // fire
        rb.linearVelocity = transform.forward * speed;
        lastPosition = transform.position;
        Invoke(nameof(DestroySelf), lifetime);
    }

    void Update()
    {
        // continuous collision via raycast
        Vector3 travel = transform.position - lastPosition;
        float dist = travel.magnitude;
        if (dist > 0f && Physics.Raycast(lastPosition, travel.normalized, out RaycastHit hit, dist, hitLayers))
        {
            HandleHit(hit.collider, hit.point, hit.normal);
            return;
        }
        lastPosition = transform.position;
    }

    void OnCollisionEnter(Collision col)
    {
        // fallback in case CCD misses
        var contact = col.contacts[0];
        HandleHit(col.collider, contact.point, contact.normal);
    }

    void HandleHit(Collider col, Vector3 point, Vector3 normal)
    {
        // 1) Try IDamageable
        var dmgable = col.GetComponentInParent<IDamageable>();
        if (dmgable != null)
        {
            dmgable.TakeDamage(damage);
        }
        else if (col.CompareTag("Zombie"))
        {
            // 2) ZombieHealth or InstaKill
            var zh = col.GetComponent<ZombieHealth>();
            if (zh != null)
            {
                var pm = GameObject.FindWithTag("Player")?.GetComponent<ThirdPersonMovement>();
                if (pm != null && pm.IsInstaKillActive)
                    zh.InstaKill();
                else
                    zh.TakeDamage(damage);
            }
        }
        else if (col.CompareTag("Boss"))
        {
            // 3) Boss
            var boss = col.GetComponent<FinalBossController>();
            if (boss != null)
            {
                var pm = GameObject.FindWithTag("Player")?.GetComponent<ThirdPersonMovement>();
                float dmg = (pm != null && pm.IsInstaKillActive) ? damage * 5f : damage;
                boss.TakeDamage(dmg);
            }
        }

        // spawn impact effect
        if (impactEffectPrefab != null)
            Instantiate(impactEffectPrefab, point, Quaternion.LookRotation(normal));

        DestroySelf();
    }

    void DestroySelf()
    {
        CancelInvoke();
        Destroy(gameObject);
    }
}
