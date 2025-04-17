using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float amount);
}

[RequireComponent(typeof(Rigidbody), typeof(Collider))]

public class Bullet : MonoBehaviour
{
    public float speed = 30f;
    public float damage = 20f;
    public float lifeTime = 3f;
    public GameObject hitEffect;
    public LayerMask hitLayers;

    Rigidbody rb;
    Vector3 lastPos;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void OnEnable()
    {
        rb.linearVelocity = transform.forward * speed;
        lastPos = transform.position;
        Invoke(nameof(DestroySelf), lifeTime);
    }

    void Update()
    {
        Vector3 travel = transform.position - lastPos;
        float dist = travel.magnitude;
        if (dist > 0f && Physics.Raycast(lastPos, travel.normalized, out var hit, dist, hitLayers))
        {
            HandleHit(hit.collider, hit.point, hit.normal);
            return;
        }
        lastPos = transform.position;
    }

    void OnCollisionEnter(Collision col)
    {
        var contact = col.contacts[0];
        HandleHit(col.collider, contact.point, contact.normal);
    }

    void HandleHit(Collider col, Vector3 pos, Vector3 norm)
    {
        // Zombie case
        if (col.CompareTag("Zombie"))
        {
            var zh = col.GetComponent<ZombieHealth>();
            if (zh != null)
            {
                // InstaKill?
                var pm = GameObject.FindWithTag("Player")?.GetComponent<ThirdPersonMovement>();
                if (pm != null && pm.IsInstaKillActive)
                    zh.InstaKill();
                else
                    zh.TakeDamage(damage);
            }
        }
        // Boss case
        else if (col.CompareTag("Boss"))
        {
            var boss = col.GetComponent<FinalBossController>();
            if (boss != null)
            {
                var pm = GameObject.FindWithTag("Player")?.GetComponent<ThirdPersonMovement>();
                float dmg = (pm != null && pm.IsInstaKillActive) ? damage * 5f : damage;
                boss.TakeDamage(dmg);
            }
        }
        // Other IDamageable (falls back):
        else
        {
            var dmgable = col.GetComponentInParent<IDamageable>();
            if (dmgable != null)
                dmgable.TakeDamage(damage);
        }

        if (hitEffect != null)
            Instantiate(hitEffect, pos, Quaternion.LookRotation(norm));

        DestroySelf();
    }

    void DestroySelf()
    {
        CancelInvoke();
        Destroy(gameObject);
    }
}
