using UnityEditor;
using UnityEngine;

public class SpawnGroupExporter
{
    [MenuItem("Tools/SpawnGroup/Export Selected Group to SO")]
    public static void ExportSpawnGroup()
    {
        var selected = Selection.activeGameObject;

        if (!selected)
        {
            Debug.LogError("Please select a parent GameObject with prefab children.");
            return;
        }

        var data = ScriptableObject.CreateInstance<SpawnGroupData_Desert>();

        foreach (Transform child in selected.transform)
        {
            var prefab = PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject);
            if (prefab == null)
            {
                Debug.LogWarning($"'{child.name}' is not a prefab instance.");
                continue;
            }

            data.spawnInfos.Add(new SpawnGroupData_Desert.SpawnInfo
            {
                prefab = prefab,
                position = child.position,
                rotation = child.rotation
            });
        }

        string path = EditorUtility.SaveFilePanelInProject("Save SpawnGroupData", selected.name + "_SpawnData", "asset", "Save spawn data");
        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.CreateAsset(data, path);
            AssetDatabase.SaveAssets();
            Debug.Log("Spawn data saved.");
        }
    }
}
