using UnityEngine;

public class FinalBossTriggerAndSpawner : MonoBehaviour
{
    [Header("Boss Setup")]
    public GameObject bossPrefab;

    [Header("Spawn Settings")]
    public float distanceInFrontOfPlayer = 6f;

    private GameObject bossInstance;
    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered || !other.CompareTag("Player")) return;
        triggered = true;

        SpawnBossInFrontOfPlayer(other.transform);

        Destroy(gameObject); // Trigger once only
    }

    private void SpawnBossInFrontOfPlayer(Transform player)
    {
        if (bossPrefab == null)
        {
            Debug.LogError("FinalBossTrigger: No bossPrefab assigned.");
            return;
        }

        if (bossInstance != null) return;

        Vector3 spawnPos = player.position + player.forward * distanceInFrontOfPlayer;

        // Raycast to snap to ground
        if (Physics.Raycast(spawnPos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f))
        {
            spawnPos = hit.point;
        }
        else
        {
            spawnPos.y = player.position.y; // Fallback if no ground detected
        }

        bossInstance = Instantiate(bossPrefab, spawnPos, Quaternion.identity);
        Debug.Log("Final Boss spawned at: " + spawnPos);
    }
}
