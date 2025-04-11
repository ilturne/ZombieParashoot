using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionTrigger : MonoBehaviour
{
    [Header("Scene Loading")]
    public string sceneToLoad = "PlainsRegion";
    public string sceneToUnload;

    private bool triggered = false;

    //  This runs when the game starts
    private void Start()
    {
        // Load the starting region additively
        if (!string.IsNullOrEmpty(sceneToLoad) && !IsSceneLoaded(sceneToLoad))
        {
            SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);
        }
    }

    //  This runs when the player enters the trigger
    private void OnTriggerEnter(Collider other)
    {
        if (triggered || !other.CompareTag("Player")) return;
        triggered = true;

        if (!string.IsNullOrEmpty(sceneToLoad) && !IsSceneLoaded(sceneToLoad))
        {
            SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);
        }

        if (!string.IsNullOrEmpty(sceneToUnload) && IsSceneLoaded(sceneToUnload))
        {
            SceneManager.UnloadSceneAsync(sceneToUnload);
        }
    }

    //  Utility to check if a scene is already loaded
    private bool IsSceneLoaded(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene loadedScene = SceneManager.GetSceneAt(i);
            if (loadedScene.name == sceneName)
                return true;
        }
        return false;
    }
}
