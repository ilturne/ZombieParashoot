using UnityEngine;
using System.Collections;

public class ZombieSpawner : MonoBehaviour
{
    public GameObject zombiePrefab;
    public Transform playerTransform;
    public float spawnInterval = 0.5f;
    public float minSpawnDistance = 15f;
    public float maxSpawnDistance = 25f;
    public int maxZombies = 60;

    [Tooltip("How many extra units ahead of the camera view edge to spawn zombies.")]
    public float extraOffsetAhead = 5.0f; // Adjust this value in the Inspector

    void Start()
    {
        if (playerTransform == null) {
             Debug.LogError("Player Transform not assigned to Zombie Spawner!");
             enabled = false; // Disable script if player is missing
             return;
        }
         if (zombiePrefab == null) {
             Debug.LogError("Zombie Prefab not assigned to Zombie Spawner!");
             enabled = false; // Disable script if prefab is missing
             return;
        }
        StartCoroutine(SpawnZombies());
    }

    IEnumerator SpawnZombies()
    {
        // Small initial delay before first spawn attempt
        yield return new WaitForSeconds(2f);

        while (true) // Game loop condition
        {
             // Check current zombie count using FindGameObjectsWithTag (can be slightly slow, consider maintaining a list if performance becomes an issue)
            if (GameObject.FindGameObjectsWithTag("Zombie").Length < maxZombies)
            {
                SpawnZombie(); // Attempt to spawn a zombie
            }
            // Wait for the specified interval before the next spawn check
            yield return new WaitForSeconds(spawnInterval);
        }
    }


    void SpawnZombie()
    {
        if (playerTransform == null) return;

        // --- CHANGE THIS LINE ---
        // Original: float randomAngle = Random.Range(-90f, 90f);
        // Narrow the angle slightly (e.g., -80 to +80 degrees)
        float randomAngle = Random.Range(-80f, 80f);
        // --- END CHANGE ---

        Vector3 spawnDirection = Quaternion.AngleAxis(randomAngle, Vector3.up) * playerTransform.forward;

        // ... (rest of the SpawnZombie method remains the same) ...
        // ... (distance calculation, position calculation, view check, offset, instantiation) ...

        // Make sure the rest of the SpawnZombie method from the previous correct version is here:
        float spawnDistance = Random.Range(minSpawnDistance, maxSpawnDistance);
        Vector3 spawnPosition = playerTransform.position + spawnDirection.normalized * spawnDistance;
        // Adjust Y position (consider raycasting down for uneven terrain later)
        spawnPosition.y = playerTransform.position.y;

        Camera cam = Camera.main;
        if (cam == null) { /* ... error log ... */ return; }

        Vector3 viewportPoint = cam.WorldToViewportPoint(spawnPosition);
        int pushAttempts = 0;
        int maxPushAttempts = 50;
        while (viewportPoint.z > cam.nearClipPlane &&
               viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
               viewportPoint.y >= 0 && viewportPoint.y <= 1 &&
               pushAttempts < maxPushAttempts )
        {
            spawnPosition += spawnDirection.normalized * 1f;
            viewportPoint = cam.WorldToViewportPoint(spawnPosition);
            pushAttempts++;
        }
        if(pushAttempts >= maxPushAttempts){ /* ... warning log ... */ }

        spawnPosition += spawnDirection.normalized * extraOffsetAhead;

        // Final Y adjustment (optional raycast here)
        spawnPosition.y = playerTransform.position.y; // Simple version for now


        GameObject zombieInstance = Instantiate(zombiePrefab, spawnPosition, Quaternion.identity);
        // zombieInstance.transform.LookAt(playerTransform.position); // Optional face player
    }
}