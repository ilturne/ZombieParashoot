using UnityEngine;
using UnityEngine.SceneManagement; // Required for scene management

public class GameManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Assign the parent Panel GameObject for the Game Over UI")]
    [SerializeField] private GameObject gameOverPanel; // Assign in Inspector

    void Awake()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("Game Over Panel is not assigned in the GameManager Inspector!", this);
        }
    }

    // Called by PlayerHealth when the player dies
    public void PlayerDied()
    {
        Debug.Log("GameManager received PlayerDied signal.");

        // Pause the game
        Time.timeScale = 0f; // Stops time-based operations

        // Show the mouse cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Activate the Game Over UI Panel
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    // This function will be called by the UI Restart Button's OnClick event
    public void RestartGame()
    {
        Debug.Log("RestartGame called.");

        // Unpause the game
        Time.timeScale = 1f;

        // Hide and lock cursor again (adjust if your game doesn't lock cursor)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Get the current active scene
        Scene currentScene = SceneManager.GetActiveScene();

        // Reload the current scene
        SceneManager.LoadScene(currentScene.buildIndex); 
        // Using buildIndex is generally safer than name if you rename scenes
    }

     // Optional: Add Quit function for a Quit button
     public void QuitGame()
     {
         Debug.Log("Quitting Game...");
         Application.Quit(); // Note: This only works in a built game, not the Editor
     }
}