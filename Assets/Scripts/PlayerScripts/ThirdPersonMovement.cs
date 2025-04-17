using UnityEngine;
using UnityEngine.UI;
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

    // *** Boss Area Camera Control Variables ***
    [Header("Boss Area Camera Settings")]
    [SerializeField] private bool inBossArea = false;
    [SerializeField] private float bossAreaCameraDistance = 12.0f;
    [SerializeField] private float bossAreaCameraHeight = 5.0f;
    [SerializeField] private float cameraTransitionSpeed = 2.0f;
    private Vector3 originalCameraOffset;
    private bool hasStoredOriginalCameraPosition = false;

    // *** Variables for Mouse Look Rotation ***
    [Header("Mouse Look Rotation")]
    [Tooltip("Set this LayerMask to only include the 'Ground' layer you created.")]
    [SerializeField] private LayerMask groundLayerMask; 
    [Tooltip("How far the raycast for mouse aiming should check.")]
    [SerializeField] private float mouseRayMaxDistance = 100f; 

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

        bombCount = startingBombs;
        UpdateBombUI(); 
        if (bombPrefab == null) 
        {
            Debug.LogError("Bomb Prefab has not been assigned in the ThirdPersonMovement script Inspector! Bomb throwing will not work.");
        }
        
        if (groundLayerMask == 0)
        {
            Debug.LogWarning("Ground Layer Mask is not set in the ThirdPersonMovement Inspector. Mouse rotation will not work correctly. Please set it to your 'Ground' layer.", this);
        }
    }

    void Update()
    {
        HandleMovement();
        HandleBombThrowInput(); 
        HandleMouseRotation();
        // Add this inside the Update() method of a suitable script        
        // Only update camera position if in boss area
        if (inBossArea && mainCamera != null)
        {
            UpdateCameraPosition();
        }
    }

    void HandleMovement()
    {
        // Ground Check and Gravity
        groundedPlayer = controller.isGrounded;
        if (groundedPlayer && playerVelocity.y < 0) { playerVelocity.y = 0f; }
        if (!groundedPlayer) { playerVelocity.y += Physics.gravity.y * Time.deltaTime; }

        // Input and Base Movement Vector
        Vector3 moveInput = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        
        // Only apply camera roll movement if it's enabled and we're not in the boss area
        if (cameraRoll != null && cameraRoll.IsRolling && !inBossArea) 
        { 
            moveInput.z += cameraRoll.rollSpeed * Time.deltaTime; 
        }
        
        Vector3 desiredMoveDirection = moveInput;
        Vector3 desiredMoveDelta = desiredMoveDirection * playerSpeed * Time.deltaTime;

        // Predict and Clamp Position
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

        // Calculate Allowed Movement & Move
        Vector3 allowedMoveDelta = predictedPos - currentPos;
        controller.Move(allowedMoveDelta);
        controller.Move(playerVelocity * Time.deltaTime); // Apply gravity
    }
    
    // Method to handle camera positioning for boss area
    void UpdateCameraPosition()
    {
        // Store original camera offset when first entering boss area
        if (!hasStoredOriginalCameraPosition)
        {
            originalCameraOffset = mainCamera.transform.position - transform.position;
            hasStoredOriginalCameraPosition = true;
            Debug.Log("Stored original camera offset: " + originalCameraOffset);
        }
        
        // Calculate desired offset for boss area
        Vector3 targetOffset = new Vector3(
            0, // Centered horizontally
            bossAreaCameraHeight, // Higher up for boss fight
            -bossAreaCameraDistance // Further back for boss fight
        );
        
        // Current offset from player to camera
        Vector3 currentOffset = mainCamera.transform.position - transform.position;
        
        // Smoothly interpolate camera position
        Vector3 newOffset = Vector3.Lerp(currentOffset, targetOffset, Time.deltaTime * cameraTransitionSpeed);
        
        // Apply new camera position
        mainCamera.transform.position = transform.position + newOffset;
        
        // Look at the player (plus a small height adjustment)
        mainCamera.transform.LookAt(transform.position + Vector3.up * 1.5f);
    }
    
    // Method to handle rotation towards mouse
    void HandleMouseRotation()
    {
        if (mainCamera == null) return;

        // Create a ray from the camera through the mouse cursor
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // Raycast against the ground layer
        if (Physics.Raycast(ray, out RaycastHit hitInfo, mouseRayMaxDistance, groundLayerMask))
        {
            Vector3 targetPoint = hitInfo.point;
            Vector3 directionToLook = targetPoint - transform.position;
            directionToLook.y = 0f; // Keep on horizontal plane

            if (directionToLook.sqrMagnitude > 0.01f)
            {
                // Calculate target rotation
                Quaternion targetRotation = Quaternion.LookRotation(directionToLook);
                transform.rotation = targetRotation;
            }
        }
    }

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
    
    // --- Boss Area Methods ---
    
    // Called when the player enters/exits the boss area
    public void SetBossAreaMode(bool enabled)
    {
        inBossArea = enabled;
        Debug.Log("Boss area mode set to: " + enabled);
        
        // Enable/disable camera roll based on boss area
        if (cameraRoll != null)
        {
            cameraRoll.enabled = !enabled; // Disable roll in boss area, enable outside
            Debug.Log("Camera roll set to: " + !enabled);
        }
        
        // If exiting boss area, restore original camera position
        if (!enabled && hasStoredOriginalCameraPosition && mainCamera != null)
        {
            mainCamera.transform.position = transform.position + originalCameraOffset;
            hasStoredOriginalCameraPosition = false;
        }
    }
}