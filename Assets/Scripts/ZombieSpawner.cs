using UnityEngine;
using System.Collections;

public class ZombieSpawner : MonoBehaviour
{
    // The zombie prefab to spawn.
    public GameObject zombiePrefab;
    
    // Reference to the player's transform.
    public Transform playerTransform;
    
    // Time between spawns (in seconds). Adjust to get about 12 zombies over 2–3 minutes.
    public float spawnInterval = 3f;
    
    // Minimum and maximum distance from the player at which zombies will spawn.
    public float minSpawnDistance = 15f;
    public float maxSpawnDistance = 25f;

    // Optional: maximum number of zombies allowed to exist at one time.
    public int maxZombies = 30;

    void Start()
    {
        StartCoroutine(SpawnZombies());
    }

    IEnumerator SpawnZombies()
    {
        yield return new WaitForSeconds(2f);
        
        while (true)
        {
            if (GameObject.FindGameObjectsWithTag("Zombie").Length < maxZombies)
            {
                SpawnZombie();
            }
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnZombie()
    {
        // Choose a random angle between -90° and +90° relative to the player's forward direction.
        float randomAngle = Random.Range(-90f, 90f);
        Vector3 spawnDirection = Quaternion.AngleAxis(randomAngle, Vector3.up) * playerTransform.forward;
        
        // Choose a random distance.
        float spawnDistance = Random.Range(minSpawnDistance, maxSpawnDistance);
        
        // Calculate initial spawn position.
        Vector3 spawnPosition = playerTransform.position + spawnDirection.normalized * spawnDistance;
        spawnPosition.y = playerTransform.position.y; // ensure it spawns on the ground level
        
        // Ensure the spawn position is outside of the camera's view.
        // We convert the spawn position to viewport coordinates (0-1 range).
        Camera cam = Camera.main;
        Vector3 viewportPoint = cam.WorldToViewportPoint(spawnPosition);
        
        // While the spawn position is within the camera's view (viewport x and y between 0 and 1),
        // push it further out along the chosen direction.
        while (viewportPoint.x >= 0 && viewportPoint.x <= 1 && 
               viewportPoint.y >= 0 && viewportPoint.y <= 1)
        {
            spawnPosition += spawnDirection.normalized * 1f; // shift by 1 unit
            viewportPoint = cam.WorldToViewportPoint(spawnPosition);
        }

        // Instantiate the zombie.
        GameObject zombieInstance = Instantiate(zombiePrefab, spawnPosition, Quaternion.identity);
        zombieInstance.tag = "Zombie";
    }
}
