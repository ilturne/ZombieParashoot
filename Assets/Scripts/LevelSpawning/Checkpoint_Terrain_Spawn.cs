using UnityEngine;

public class Checkpoint_Terrain_Spawn : MonoBehaviour
{
    public SpawnGroupData spawnData;

    private bool hasSpawned = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasSpawned) return;

        if (other.CompareTag("Player"))
        {
            foreach (var info in spawnData.spawnInfos)
            {
                Instantiate(info.prefab, info.position, info.rotation);
            }

            hasSpawned = true;
            Debug.Log("Spawned group at checkpoint!");
        }
    }
}
