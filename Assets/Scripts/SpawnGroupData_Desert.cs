using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SpawnGroupData", menuName = "Spawner/SpawnGroupData")]
public class SpawnGroupData_Desert : ScriptableObject
{
    [System.Serializable]
    public class SpawnInfo
    {
        public GameObject prefab;
        public Vector3 position;
        public Quaternion rotation;
    }

    public List<SpawnInfo> spawnInfos = new List<SpawnInfo>();
}
