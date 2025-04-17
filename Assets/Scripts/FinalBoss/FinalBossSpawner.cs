using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class FinalBossTriggerAndSpawner : MonoBehaviour
{
    [Header("Boss Setup")]
    public GameObject bossPrefab;
    [Tooltip("Distance ahead of the player to spawn the boss")]
    public float distanceInFrontOfPlayer = 6f;

    [Header("Camera Settings")]
    public bool adjustCamera = true;

    private bool triggered = false;
    private Camera mainCamera;
    private CameraRoll cameraRoll;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera != null)
            cameraRoll = mainCamera.GetComponent<CameraRoll>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered || !other.CompareTag("Player")) return;
        triggered = true;

        SpawnBoss(other.transform);
        if (adjustCamera) AdjustCameraForBossFight(other.gameObject);
        StopAllZombieSpawners();

        // disable this trigger so it never fires again
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    private void SpawnBoss(Transform player)
    {
        if (bossPrefab == null)
        {
            Debug.LogError("FinalBossTrigger: bossPrefab not assigned!");
            return;
        }

        // base position in front of player
        Vector3 spawnPos = player.position + player.forward * distanceInFrontOfPlayer;

        // snap to NavMesh so boss stands on walkable ground
        if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            spawnPos = hit.position;

        var boss = Instantiate(bossPrefab, spawnPos, Quaternion.identity);
        boss.tag = "Boss";
        Debug.Log($"Final Boss spawned at {spawnPos}");
    }

    private void AdjustCameraForBossFight(GameObject player)
    {
        var mover = player.GetComponent<ThirdPersonMovement>();
        if (mover != null)
        {
            mover.SetBossAreaMode(true);
            Debug.Log("Player movement set to boss‐area mode");
        }
        else if (cameraRoll != null)
        {
            cameraRoll.enabled = false;
            Debug.Log("CameraRoll disabled for boss fight");
        }
        else
        {
            Debug.LogWarning("No ThirdPersonMovement or CameraRoll found; camera unchanged.");
        }
    }

    private void StopAllZombieSpawners()
    {
        // use the fast, no‑sort overload
        var spawners = FindObjectsByType<ZombieSpawner>(FindObjectsSortMode.None);
        foreach (var sp in spawners)
        {
            sp.enabled = false;
            sp.StopAllCoroutines();
            Debug.Log($"ZombieSpawner '{sp.name}' paused.");
        }
    }

}
