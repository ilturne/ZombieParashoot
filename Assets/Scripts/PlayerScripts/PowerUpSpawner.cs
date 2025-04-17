using UnityEngine;
using System.Collections.Generic; // Required for List

public class PowerUpSpawner : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private List<GameObject> powerUpPrefabs; // Assign your 4 prefabs here in the Inspector
    [SerializeField] private Transform playerTransform; // Assign the player object here

    [Header("Spawning Settings")]
    [SerializeField] private float spawnInterval = 10f; // Time between spawn attempts
    [SerializeField] private float spawnYLevel = 1.0f; // Fixed Y position for power-ups
    [SerializeField] private Vector2 worldBoundsX = new Vector2(2f, 18f); // Min/Max X

    [Header("Proximity & View Settings")]
    [SerializeField] private float maxSpawnRadiusFromPlayer = 50f; // How far out from the player to consider spawning
    [SerializeField] private float minSpawnRadiusFromPlayer = 20f; // How close to the player spawns are allowed (must be > camera far clip?)
    [SerializeField] private int maxSpawnAttemptsPerInterval = 10; // Prevents potential infinite loops

    private float timer;
    private Camera mainCamera;

    void Start()
    {
        if (playerTransform == null)
        {
            // Attempt to find player by tag if not assigned
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
            else
            {
                Debug.LogError("PowerUpSpawner: Player Transform not assigned and couldn't find GameObject with tag 'Player'. Disabling spawner.");
                enabled = false; // Disable this script
                return;
            }
        }

        if (powerUpPrefabs == null || powerUpPrefabs.Count == 0)
        {
            Debug.LogError("PowerUpSpawner: No power-up prefabs assigned. Disabling spawner.");
            enabled = false;
            return;
        }

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
             Debug.LogError("PowerUpSpawner: Main Camera not found. Disabling spawner.");
             enabled = false;
             return;
        }

        timer = spawnInterval; // Start timer ready for first spawn check
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = spawnInterval; // Reset timer
            AttemptSpawn();
        }
    }

     void AttemptSpawn()
    {
        if (playerTransform == null || mainCamera == null) return; // Added camera null check here too

        for (int i = 0; i < maxSpawnAttemptsPerInterval; i++)
        {
            // 1. Calculate a potential spawn point around the player (ring shape)
            Vector2 randomDirectionXY = Random.insideUnitCircle.normalized * Random.Range(minSpawnRadiusFromPlayer, maxSpawnRadiusFromPlayer);
            // Create the offset in 3D space (using X and Z)
            Vector3 spawnOffset = new Vector3(randomDirectionXY.x, 0f, randomDirectionXY.y);
            // Calculate potential position relative to player
            Vector3 potentialSpawnPos = playerTransform.position + spawnOffset;

            // 2. Clamp to world bounds and set Y level
            potentialSpawnPos.x = Mathf.Clamp(potentialSpawnPos.x, worldBoundsX.x, worldBoundsX.y);
            potentialSpawnPos.y = spawnYLevel;

            // 3. Check Camera View: Is the point outside the camera frustum?
            Vector3 viewportPos = mainCamera.WorldToViewportPoint(potentialSpawnPos);
            // Check if outside screen edges (0 to 1)
            bool isOutOfCameraView = viewportPos.x < 0f || viewportPos.x > 1f || viewportPos.y < 0f || viewportPos.y > 1f;
            // Check if truly in front of the camera's near plane (important!)
            bool isInFrontOfCamera = viewportPos.z > mainCamera.nearClipPlane; // Use nearClipPlane for accuracy

            // 4. **** NEW CHECK: Is the point generally in front of the PLAYER? ****
            Vector3 directionToSpawnPoint = potentialSpawnPos - playerTransform.position;
            bool isGenerallyInFrontOfPlayer = false;
             // Avoid calculating dot product if the offset is essentially zero
            if (directionToSpawnPoint.sqrMagnitude > 0.1f) // Use sqrMagnitude for efficiency check
            {
                // Vector3.Dot returns > 0 if angle is < 90 degrees
                isGenerallyInFrontOfPlayer = Vector3.Dot(playerTransform.forward, directionToSpawnPoint.normalized) > 0f;
            }


            // 5. Combine ALL checks: Outside camera view, in front of camera plane, AND in front of player?
            if (isOutOfCameraView && isInFrontOfCamera && isGenerallyInFrontOfPlayer)
            {
                // Spawn a random power-up
                int randomIndex = Random.Range(0, powerUpPrefabs.Count);
                GameObject selectedPrefab = powerUpPrefabs[randomIndex];

                Instantiate(selectedPrefab, potentialSpawnPos, Quaternion.identity);
                // Debug.Log($"Spawned {selectedPrefab.name} at {potentialSpawnPos}"); // Optional spawn confirmation
                return; // Exit loop after successful spawn
            }
            // If checks fail, the loop continues to try another random point
        }
    }
}