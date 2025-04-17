using UnityEngine;
using UnityEngine.SceneManagement; // Required for scene management

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; } // Singleton instance
    [Header("UI References")]
    [Tooltip("Assign the parent Panel GameObject for the Game Over UI")]
    [SerializeField] private GameObject gameOverPanel; // Assign in Inspector\

    [Header("Lives")]
    [Tooltip("Number of lives the player has")]
    [SerializeField] private int maxLives = 3; 
    private int currentLives; // Track current lives
    private PlayerHealth playerHealth; // Store reference to unsubscribe later

    public int KillCount {get; private set; } // Track number of kills

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy duplicate GameManager
            return;
        }
        else 
        {
            Instance = this; // Set the singleton instance
            DontDestroyOnLoad(gameObject); // Persist across scenes
        }
    }

    void Start() // Using Start to increase chance PlayerHealth.Start runs first
    {
        currentLives = maxLives;
        KillCount = 0;
        // Ensure the panel is hidden at the start
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("Game Over Panel is not assigned in the GameManager Inspector!", this);
        }

        // --- Find Player and Subscribe to Death Event ---
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player"); // Make sure player is tagged "Player"
        if (playerObj != null)
        {
            playerHealth = playerObj.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // Subscribe the HandlePlayerDeath function to the player's death event
                playerHealth.OnPlayerDeath += HandlePlayerDeath; 
                Debug.Log("GameManager subscribed to Player OnPlayerDeath event.");
            }
            else
            {
                Debug.LogError("GameManager found Player object but couldn't find PlayerHealth component!", this);
            }
        }
        else
        {
            Debug.LogError("GameManager could not find object tagged 'Player' to get PlayerHealth!", this);
        }
        // --- End Subscription ---

        // Initial game state setup 
        Time.timeScale = 1f; 
    }

    public void RegisterKill() {
        KillCount++; // Increment kill count
    }

    // This method is called when the PlayerHealth.OnPlayerDeath event is invoked
    private void HandlePlayerDeath() 
    {
        Debug.Log("HandlePlayerDeath() EXECUTED in GameManager."); 

        // Pause the game
        Time.timeScale = 0f; 

        // Show the mouse cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Activate the Game Over UI Panel
        if (gameOverPanel != null)
        {
            Debug.Log($"Attempting to activate GameOverPanel: {gameOverPanel.name}"); 
            gameOverPanel.SetActive(true); // GameManager activates the UI
        }
        else
        {
             Debug.LogError("Cannot activate GameOverPanel because reference is null!");
        }
    }

    // Called by the UI Restart Button's OnClick event
    public void RestartGame()
    {
        Debug.Log("RestartGame called.");
        Time.timeScale = 1f;
        Scene currentScene = SceneManager.GetActiveScene();
        Debug.Log($"Reloading scene: {currentScene.name} (Build Index: {currentScene.buildIndex})");
        gameOverPanel.SetActive(false);
        SceneManager.LoadScene("CombinedWorld", LoadSceneMode.Single); // Reload the current scene

    }

    // Optional: Quit function
     public void QuitGame()
     {
         Debug.Log("Quitting Game...");
         Application.Quit();
     }

    // --- Unsubscribe when GameManager is destroyed ---
    void OnDestroy()
    {
        if (playerHealth != null) 
        {
            playerHealth.OnPlayerDeath -= HandlePlayerDeath; // Unsubscribe
            Debug.Log("GameManager unsubscribed from Player OnPlayerDeath event.");
        }
    }
    // --- End Unsubscribe ---
}