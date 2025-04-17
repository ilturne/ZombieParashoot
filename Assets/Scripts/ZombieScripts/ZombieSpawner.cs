using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class ZombieSpawner : MonoBehaviour
{
    [Header("References")]
    public GameObject zombiePrefab;
    public Transform player;

    [Header("Spawn Timing")]
    public float startInterval = 5f;     // initial slow spawn
    public float minInterval = 0.5f;     // fastest spawn after ramp
    public float rampDuration = 300f;    // seconds to go from startInterval → minInterval

    [Header("Spawn Volume")]
    public float minRadius = 10f;
    public float maxRadius = 25f;

    [Header("Limits")]
    public int maxZombies = 20;
    public float navMeshSampleDistance = 3f;

    [Header("Debug")]
    public bool debug = false;

    private float startTime;

    void Start()
    {
        if (player == null)
            player = GameObject.FindWithTag("Player")?.transform;

        if (zombiePrefab == null || player == null)
        {
            Debug.LogError("ZombieSpawner: missing references!");
            enabled = false;
            return;
        }

        startTime = Time.time;
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            if (player == null)
            {
                var go = GameObject.FindWithTag("Player");
                if (go != null)
                    player = go.transform;
                else
                {
                    yield return new WaitForSeconds(1f); // exit if player is not found
                    continue;
                }
            }
            
            // only spawn if under cap
            if (GameObject.FindGameObjectsWithTag("Zombie").Length < maxZombies)
            {
                Vector3 spawnPos = GetRandomNavMeshPoint();
                if (spawnPos != Vector3.zero)
                {
                    Instantiate(zombiePrefab, spawnPos, Quaternion.identity).tag = "Zombie";
                    if (debug)
                        Debug.DrawLine(player.position, spawnPos, Color.red, 2f);
                }
            }

            // compute how long until next spawn
            float elapsed = Time.time - startTime;
            float t = Mathf.Clamp01(elapsed / rampDuration);
            float interval = Mathf.Lerp(startInterval, minInterval, t);

            yield return new WaitForSeconds(interval);
        }
    }

    Vector3 GetRandomNavMeshPoint()
    {
        if (player == null)
            return Vector3.zero;
        for (int i = 0; i < 5; i++)
        {
            // random direction & distance
            Vector3 dir = Random.insideUnitSphere;
            dir.y = 0;
            dir.Normalize();
            float dist = Random.Range(minRadius, maxRadius);
            Vector3 candidate = player.position + dir * dist;

            // snap to NavMesh
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas))
                return hit.position;
        }
        return Vector3.zero; // fallback if no valid spot found
    }
}
