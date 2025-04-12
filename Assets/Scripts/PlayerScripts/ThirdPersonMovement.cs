using UnityEngine;
using UnityEngine.UI; // Keep if you might add UI Text for bomb count later
using System.Collections;

public class ThirdPersonMovement : MonoBehaviour
{
    // --- Existing Movement & Boundary Variables ---
    private CharacterController controller;
    private Vector3 playerVelocity;
    private bool groundedPlayer;
    [SerializeField] private float playerSpeed = 7.0f; 
    private CameraRoll cameraRoll;

    [Header("Movement Boundaries")]
    public float minX = 0f;
    public float maxX = 20f;

    // --- Bomb Ability Variables ---
    [Header("Bomb Ability")]
    [Tooltip("Assign the 'BombThrowablePrefab' here.")]
    [SerializeField] private GameObject bombPrefab;
    [Tooltip("Optional: An empty GameObject parented to the player where bombs originate.")]
    [SerializeField] private Transform bombSpawnPoint;
    [Tooltip("The force/speed applied to the bomb when thrown.")]
    [SerializeField] private float launchForce = 15f;
    [Tooltip("How many bombs the player starts with.")]
    [SerializeField] private int startingBombs = 0;
    private int bombCount = 0; 

    // --- InstaKill Variables ---
    [Header("InstaKill PowerUp")]
    [SerializeField] private float instaKillDuration = 15.0f; 
    private bool isInstaKillActive = false;
    private Coroutine currentInstaKillCoroutine = null; 
    public bool IsInstaKillActive => isInstaKillActive;
    private Camera mainCamera;

    // *** NEW: Variables for Mouse Look Rotation ***
    [Header("Mouse Look Rotation")]
    [Tooltip("Set this LayerMask to only include the 'Ground' layer you created.")]
    [SerializeField] private LayerMask groundLayerMask; 
    [Tooltip("How far the raycast for mouse aiming should check.")]
    [SerializeField] private float mouseRayMaxDistance = 100f; 
    // Optional: Add turn speed for smoother rotation
    // [SerializeField] private float turnSpeed = 15f; 
    // *** END NEW ***

    void Start()
    {
        controller = GetComponent<CharacterController>();
        mainCamera = Camera.main; // Keep this, it's needed for raycasting
        if (mainCamera != null)
        {
            cameraRoll = mainCamera.GetComponent<CameraRoll>();
        }
        else
        {
            Debug.LogError("Main Camera not found!");
        }

        bombCount = startingBombs;
        UpdateBombUI(); 
        if (bombPrefab == null) 
        {
            Debug.LogError("Bomb Prefab has not been assigned in the ThirdPersonMovement script Inspector! Bomb throwing will not work.");
        }
        
        // *** NEW: Check if Ground Layer Mask is set ***
        if (groundLayerMask == 0) // LayerMask value is 0 if nothing is selected
        {
            Debug.LogWarning("Ground Layer Mask is not set in the ThirdPersonMovement Inspector. Mouse rotation will not work correctly. Please set it to your 'Ground' layer.", this);
        }
        // *** END NEW ***
    }

    void Update()
    {
        HandleMovement();
        HandleBombThrowInput(); 
        HandleMouseRotation(); // *** NEW: Call rotation handling separately or at the end of HandleMovement ***
    }

    void HandleMovement()
    {
        // Ground Check and Gravity (Keep this)
        groundedPlayer = controller.isGrounded;
        if (groundedPlayer && playerVelocity.y < 0) { playerVelocity.y = 0f; }
        if (!groundedPlayer) { playerVelocity.y += Physics.gravity.y * Time.deltaTime; }

        // Input and Base Movement Vector (Keep this)
        Vector3 moveInput = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        if (cameraRoll != null && cameraRoll.IsRolling) { moveInput.z += cameraRoll.rollSpeed * Time.deltaTime; }
        Vector3 desiredMoveDirection = moveInput;
        Vector3 desiredMoveDelta = desiredMoveDirection * playerSpeed * Time.deltaTime;

        // Predict and Clamp Position (Keep this)
        Vector3 currentPos = transform.position;
        Vector3 predictedPos = currentPos + desiredMoveDelta;
        predictedPos.x = Mathf.Clamp(predictedPos.x, minX, maxX);
        if (mainCamera != null)
        {
            float distance = Vector3.Dot(predictedPos - mainCamera.transform.position, mainCamera.transform.forward);
            distance = Mathf.Max(distance, mainCamera.nearClipPlane + 0.01f);
            Vector3 bottomViewportPoint = new Vector3(0.5f, 0f, distance);
            Vector3 topViewportPoint = new Vector3(0.5f, 0.7f, distance); 
            Vector3 bottomWorldPoint = mainCamera.ViewportToWorldPoint(bottomViewportPoint);
            Vector3 topWorldPoint = mainCamera.ViewportToWorldPoint(topViewportPoint);
            float bottomLimit = Mathf.Min(bottomWorldPoint.z, topWorldPoint.z); 
            float topLimit = Mathf.Max(bottomWorldPoint.z, topWorldPoint.z);
            predictedPos.z = Mathf.Clamp(predictedPos.z, bottomLimit, topLimit);
        }
        else { Debug.LogWarning("Main Camera not found, cannot clamp Z to view."); }

        // Calculate Allowed Movement & Move (Keep this)
        Vector3 allowedMoveDelta = predictedPos - currentPos;
        controller.Move(allowedMoveDelta);
        controller.Move(playerVelocity * Time.deltaTime); // Apply gravity

        // --- Rotation based on movement input is REMOVED ---
        // *** REMOVED START ***
        /* if (moveInput.sqrMagnitude > 0.01f) 
        {
            Vector3 lookDirection = new Vector3(moveInput.x, 0, moveInput.z).normalized;
            if(lookDirection != Vector3.zero)
                 transform.rotation = Quaternion.LookRotation(lookDirection);
        }
        */
        // *** REMOVED END ***
    }
    
    // *** NEW: Method to handle rotation towards mouse ***
    void HandleMouseRotation()
    {
        if (mainCamera == null) return; // Need the camera

        // Create a ray from the camera going through the mouse cursor
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // Perform the raycast against the ground layer
        if (Physics.Raycast(ray, out RaycastHit hitInfo, mouseRayMaxDistance, groundLayerMask))
        {
            // We hit the ground layer
            Vector3 targetPoint = hitInfo.point;

            // Calculate the direction from the player to the hit point
            Vector3 directionToLook = targetPoint - transform.position;

            // Keep the rotation only on the Y axis (ignore height differences)
            directionToLook.y = 0f; 

            // Check if the direction is valid (not zero)
            if (directionToLook.sqrMagnitude > 0.01f) // Use sqrMagnitude for efficiency
            {
                // Calculate the target rotation
                Quaternion targetRotation = Quaternion.LookRotation(directionToLook);

                // Apply the rotation directly (instant snap)
                transform.rotation = targetRotation;

                // --- Optional: Smooth Rotation ---
                // Uncomment the line below and comment out the line above for smooth turning
                // Make sure you have the 'turnSpeed' variable declared and assigned above.
                // transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
                // --- End Optional ---
            }
        }
        // Optional: Add an 'else' here if you want the player to do something 
        // specific if the mouse isn't pointing at the ground (e.g., keep last rotation).
    }
    // *** END NEW ***

    // --- Bomb Handling Methods --- 
    void HandleBombThrowInput()
    {
        if (Input.GetMouseButtonDown(1))
        {
            TryThrowBomb();
        }
    }

    public void AddBombs(int amount)
    {
        bombCount += amount;
        UpdateBombUI(); 
        Debug.Log($"Player collected {amount} bombs. Total bombs: {bombCount}");
    }

    private void TryThrowBomb()
    {
        if (bombCount > 0)
        {
            ThrowBomb(); 
            bombCount--; 
            UpdateBombUI(); 
            Debug.Log($"Bomb thrown. Bombs remaining: {bombCount}");
        }
        else
        {
            Debug.Log("Cannot throw bomb - No bombs available!");
        }
    }
    
    private void ThrowBomb()
    {
        if (bombPrefab == null) {
            Debug.LogError("ThrowBomb failed: Bomb Prefab is not assigned in Inspector!");
            return; 
        }
        Vector3 spawnPos;
        Quaternion spawnRot;
        if (bombSpawnPoint != null)
        {
            spawnPos = bombSpawnPoint.position;
            spawnRot = bombSpawnPoint.rotation; 
        }
        else
        {
            spawnPos = transform.position + transform.forward * 1.0f + transform.up * 0.8f;
            spawnRot = transform.rotation; 
        }
        GameObject bombInstance = Instantiate(bombPrefab, spawnPos, spawnRot);
        Rigidbody bombRb = bombInstance.GetComponent<Rigidbody>();
        if (bombRb != null)
        {
            Vector3 launchDirection = (transform.forward + transform.up * 0.7f).normalized;
            bombRb.AddForce(launchDirection * launchForce, ForceMode.VelocityChange);
        }
        else
        {
            Debug.LogError("Bomb Prefab is missing its Rigidbody component!");
        }
    }
    
    private void UpdateBombUI()
    {
        Debug.Log($"Bomb Count: {bombCount}");
        // if (bombCountText != null) { bombCountText.text = $"Bombs: {bombCount}"; }
    }
    // --- End Bomb Handling Methods ---

    // --- InstaKill Methods ---
    public void ActivateInstaKill()
    {
        if (currentInstaKillCoroutine != null)
        {
            StopCoroutine(currentInstaKillCoroutine); 
        }
        isInstaKillActive = true;
        currentInstaKillCoroutine = StartCoroutine(InstaKillTimer());
    }

    private IEnumerator InstaKillTimer()
    {
        yield return new WaitForSeconds(instaKillDuration);
        isInstaKillActive = false;
        currentInstaKillCoroutine = null; 
    }
    // --- End InstaKill Methods ---

} // End of Class