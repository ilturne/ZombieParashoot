using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    public Vector3 lastCheckpointPosition;
    public string lastCheckpointScene;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetCheckpoint(Vector3 position, string sceneName)
    {
        lastCheckpointPosition = position;
        lastCheckpointScene = sceneName;
        Debug.Log($"Checkpoint saved at {position} in scene {sceneName}");
    }

    public void RespawnPlayer(GameObject player)
    {
        StartCoroutine(RespawnCoroutine(player));
    }

    private IEnumerator RespawnCoroutine(GameObject player)
    {
        if (!IsSceneLoaded(lastCheckpointScene))
        {
            Debug.Log($"Loading scene {lastCheckpointScene} before respawn...");
            yield return SceneManager.LoadSceneAsync(lastCheckpointScene, LoadSceneMode.Additive);
        }

        yield return null;

        player.transform.position = lastCheckpointPosition + Vector3.up * 1.5f;
    }

    private bool IsSceneLoaded(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).name == sceneName)
                return true;
        }
        return false;
    }

    private void Start()
    {
        if (lastCheckpointPosition == Vector3.zero)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                SetCheckpoint(player.transform.position, SceneManager.GetActiveScene().name);
            }
        }
    }
}
