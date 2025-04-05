using UnityEngine;
using UnityEngine.UI; // Keep if you might add UI Text for bomb count later
using System.Collections;

public class ThirdPersonMovement : MonoBehaviour
{
    // --- Existing Movement & Boundary Variables ---
    private CharacterController controller;
    private Vector3 playerVelocity;
    private bool groundedPlayer;
    [SerializeField] private float playerSpeed = 7.0f; // Made SerializeField
    private CameraRoll cameraRoll;

    [Header("Movement Boundaries")]
    public float minX = 0f;
    public float maxX = 20f;
    // Note: Using Z clamping from your provided script. If vertical screen clamping is desired, refer to previous Y clamping logic.

    // --- NEW: Bomb Ability Variables ---
    [Header("Bomb Ability")]
    [Tooltip("Assign the 'BombThrowablePrefab' here.")]
    [SerializeField] private GameObject bombPrefab;
    [Tooltip("Optional: An empty GameObject parented to the player where bombs originate.")]
    [SerializeField] private Transform bombSpawnPoint;
    [Tooltip("The force/speed applied to the bomb when thrown.")]
    [SerializeField] private float launchForce = 15f;
    [Tooltip("How many bombs the player starts with.")]
    [SerializeField] private int startingBombs = 0;
    private int bombCount = 0; // Current number of bombs held

    // Optional: Link to a UI Text element to display bomb count
    // [SerializeField] private Text bombCountText;
    // --- End Bomb Variables ---

    // -- InstaKill Variables --
    [Header("InstaKill PowerUp")]
    [SerializeField] private float instaKillDuration = 15.0f; // Duration in seconds
    private bool isInstaKillActive = false;
    private Coroutine currentInstaKillCoroutine = null; // To manage the timer
    // Public property to check status from other scripts (like Bullet)
    public bool IsInstaKillActive => isInstaKillActive;
    private Camera mainCamera;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraRoll = mainCamera.GetComponent<CameraRoll>();
        }
        else
        {
            Debug.LogError("Main Camera not found!");
        }

        // --- NEW: Initialize Bombs ---
        bombCount = startingBombs;
        UpdateBombUI(); // Display initial count (currently logs to console)
        if (bombPrefab == null) // Check if prefab is assigned
        {
            Debug.LogError("Bomb Prefab has not been assigned in the ThirdPersonMovement script Inspector! Bomb throwing will not work.");
        }
        // --- End Bomb Init ---
    }

    void Update()
    {
        // Keep logic separated for clarity
        HandleMovement();
        HandleBombThrowInput(); // Check for bomb throw input each frame
    }

    // --- Existing HandleMovement (with your Z clamping) ---
    void HandleMovement()
    {
        // Ground Check and Gravity
        groundedPlayer = controller.isGrounded;
        if (groundedPlayer && playerVelocity.y < 0) { playerVelocity.y = 0f; }
        if (!groundedPlayer) { playerVelocity.y += Physics.gravity.y * Time.deltaTime; }

        // Input and Base Movement Vector
        Vector3 moveInput = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        if (cameraRoll != null && cameraRoll.IsRolling) { moveInput.z += cameraRoll.rollSpeed * Time.deltaTime; }
        Vector3 desiredMoveDirection = moveInput;
        Vector3 desiredMoveDelta = desiredMoveDirection * playerSpeed * Time.deltaTime;

        // Predict and Clamp Position
        Vector3 currentPos = transform.position;
        Vector3 predictedPos = currentPos + desiredMoveDelta;

        // Clamp X
        predictedPos.x = Mathf.Clamp(predictedPos.x, minX, maxX);

        // Clamp Z position based on Camera Viewport (from your script)
        if (mainCamera != null)
        {
            float distance = Vector3.Dot(predictedPos - mainCamera.transform.position, mainCamera.transform.forward);
            distance = Mathf.Max(distance, mainCamera.nearClipPlane + 0.01f);
            Vector3 bottomViewportPoint = new Vector3(0.5f, 0f, distance);
            Vector3 topViewportPoint = new Vector3(0.5f, 0.7f, distance); // Using 0.7 from your script
            Vector3 bottomWorldPoint = mainCamera.ViewportToWorldPoint(bottomViewportPoint);
            Vector3 topWorldPoint = mainCamera.ViewportToWorldPoint(topViewportPoint);
            float bottomLimit = Mathf.Min(bottomWorldPoint.z, topWorldPoint.z); // Use Min/Max for safety
            float topLimit = Mathf.Max(bottomWorldPoint.z, topWorldPoint.z);
            predictedPos.z = Mathf.Clamp(predictedPos.z, bottomLimit, topLimit);
        }
        else { Debug.LogWarning("Main Camera not found, cannot clamp Z to view."); }

        // Calculate Allowed Movement & Move
        Vector3 allowedMoveDelta = predictedPos - currentPos;
        controller.Move(allowedMoveDelta);
        controller.Move(playerVelocity * Time.deltaTime); // Apply gravity

        // Rotation
        if (moveInput.sqrMagnitude > 0.01f)
        {
            Vector3 lookDirection = new Vector3(moveInput.x, 0, moveInput.z).normalized;
            if(lookDirection != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(lookDirection);
        }
    }

    void HandleBombThrowInput()
    {
        // Input.GetMouseButtonDown(1) is typically the Right Mouse Button
        if (Input.GetMouseButtonDown(1))
        {
            TryThrowBomb();
        }
    }

    // Public method for the PowerUpItem script to call when Bomb type is collected
    public void AddBombs(int amount)
    {
        bombCount += amount;
        UpdateBombUI(); // Update display
        Debug.Log($"Player collected {amount} bombs. Total bombs: {bombCount}");
    }

    // Checks if player has bombs before throwing
    private void TryThrowBomb()
    {
        if (bombCount > 0)
        {
            ThrowBomb(); // Execute the throw logic
            bombCount--; // Use up one bomb
            UpdateBombUI(); // Update display
            Debug.Log($"Bomb thrown. Bombs remaining: {bombCount}");
        }
        else
        {
            Debug.Log("Cannot throw bomb - No bombs available!");
            // Optional: Play an empty/error sound effect
        }
    }

    // Handles the instantiation and launch of the bomb
    private void ThrowBomb()
    {
        if (bombPrefab == null) {
             Debug.LogError("ThrowBomb failed: Bomb Prefab is not assigned in Inspector!");
             return; // Don't proceed if prefab is missing
        }

        // Determine spawn position: Use dedicated point or offset from player
        Vector3 spawnPos;
        Quaternion spawnRot;
        if (bombSpawnPoint != null)
        {
            spawnPos = bombSpawnPoint.position;
            spawnRot = bombSpawnPoint.rotation; // Use spawn point's rotation
        }
        else
        {
            // Default spawn: Adjust Y offset (e.g., 0.8f) to roughly match hand height
            spawnPos = transform.position + transform.forward * 1.0f + transform.up * 0.8f;
            spawnRot = transform.rotation; // Use player's rotation
        }

        // Create the bomb instance from the prefab
        GameObject bombInstance = Instantiate(bombPrefab, spawnPos, spawnRot);

        // Get the Rigidbody to apply force
        Rigidbody bombRb = bombInstance.GetComponent<Rigidbody>();
        if (bombRb != null)
        {
            // Calculate launch direction: Player's forward + some upward angle
            // Adjust the multiplier (e.g., 0.7f) to change the arc height
            Vector3 launchDirection = (transform.forward + transform.up * 0.7f).normalized;

            // Apply the launch force using VelocityChange for consistent initial speed
            bombRb.AddForce(launchDirection * launchForce, ForceMode.VelocityChange);
        }
        else
        {
            // This shouldn't happen if you set up BombThrowablePrefab correctly
            Debug.LogError("Bomb Prefab is missing its Rigidbody component!");
        }
    }
    
    public void ActivateInstaKill()
    {
        if (currentInstaKillCoroutine != null)
        {
            StopCoroutine(currentInstaKillCoroutine); // Reset the timer if already active
        }

        isInstaKillActive = true;

        currentInstaKillCoroutine = StartCoroutine(InstaKillTimer());
    }

    private IEnumerator InstaKillTimer()
    {
        yield return new WaitForSeconds(instaKillDuration);
        isInstaKillActive = false;
        currentInstaKillCoroutine = null; // Reset the coroutine reference
    }
    
    // Updates any UI element showing the bomb count (currently just logs)
    private void UpdateBombUI()
    {
        // Replace this with actual UI update if you add a Text element
        Debug.Log($"Bomb Count: {bombCount}");
        // Example:
        // if (bombCountText != null)
        // {
        //     bombCountText.text = $"Bombs: {bombCount}";
        // }
    }
    // --- End Bomb Handling Methods ---

} // End of Class