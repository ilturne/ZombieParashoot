using UnityEngine;
using System.Collections;

public class ThrowableBomb : MonoBehaviour
{
    [Header("Explosion Settings")]
    [SerializeField] private float explosionDelay = 2.0f; // Time before exploding
    [SerializeField] private float explosionRadius = 5.0f; // Area of effect
    [SerializeField] private float explosionDamage = 100f; // Damage dealt to zombies
    [SerializeField] private GameObject explosionEffectPrefab; // Assign explosion visual effect prefab

    void Start()
    {
        // Start the countdown coroutine
        StartCoroutine(ExplodeAfterDelayCoroutine());
    }

    IEnumerator ExplodeAfterDelayCoroutine()
    {
        yield return new WaitForSeconds(explosionDelay);
        Explode();
    }

    void Explode()
    {
        Debug.Log("Bomb Exploding!");
        
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        // 2. Find nearby colliders
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);

        // 3. Apply damage and force
        foreach (Collider hitCollider in colliders)
        {
            ZombieHealth zombieHealth = hitCollider.GetComponent<ZombieHealth>();
            if (zombieHealth != null)
            {
                Debug.Log($"Bomb damaging zombie: {hitCollider.name}");
                zombieHealth.TakeDamage(explosionDamage);
            }

        }

        // 4. Destroy the bomb object itself
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}