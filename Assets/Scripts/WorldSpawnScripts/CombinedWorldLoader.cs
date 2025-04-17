// CombinedWorldLoader.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class CombinedWorldLoader : MonoBehaviour
{
    [Tooltip("Name of the scene to load additively whenever CombinedWorld opens")]
    public string initialRegionScene = "PlainsRegion";

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // if we just loaded CombinedWorld (single), pull in the PlainsRegion
        if (scene.name == "CombinedWorld" && mode == LoadSceneMode.Single)
        {
            SceneManager.LoadScene(initialRegionScene, LoadSceneMode.Additive);
        }
    }
}
