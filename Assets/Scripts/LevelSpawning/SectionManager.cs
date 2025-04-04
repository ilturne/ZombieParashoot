using UnityEngine;
using System.Collections.Generic;

public class SectionManager : MonoBehaviour
{
    [Header("Spawn Data")]
    public SpawnGroupData spawnData;

    public Transform spawnParent;

    private List<GameObject> spawnedObjects = new List<GameObject>();
    private bool hasSpawned = false;

    public void Spawn()
    {
        if (hasSpawned) return;

        foreach (var info in spawnData.spawnInfos)
        {
            GameObject obj = Instantiate(info.prefab, info.position, info.rotation);

            if (spawnParent != null)
            {
                obj.transform.SetParent(spawnParent, true);
            }
            spawnedObjects.Add(obj);
            
        }

        hasSpawned = true;
        Debug.Log($"{gameObject.name}: Section spawned.");
    }

    public void Despawn()
    {
        foreach (var obj in spawnedObjects)
        {
            if (obj != null) Destroy(obj);
        }

        spawnedObjects.Clear();
        hasSpawned = false;

        Debug.Log($"{gameObject.name}: Section despawned.");
    }
}
