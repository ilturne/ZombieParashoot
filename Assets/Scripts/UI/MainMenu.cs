using UnityEngine;
using UnityEngine.SceneManagement;
public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        // Load the game scene (assuming it's named "GameScene")
        SceneManager.LoadScene(1);
    }

    public void QuitGame()
    {
        // Quit the application (works in built version, not in editor)
        Application.Quit();
        Debug.Log("Game is quitting..."); // Log for debugging purposes
    }
}
