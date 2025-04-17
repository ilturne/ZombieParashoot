using UnityEngine;
using UnityEngine.SceneManagement;
public class MainMenu : MonoBehaviour
{

    public void PlayGame()
    {
        SceneManager.LoadScene(1, LoadSceneMode.Single); // Load the game scene (index 1 in build settings)
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game is quitting..."); // Log for debugging purposes
    }
}
