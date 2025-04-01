using UnityEngine;

public class ThirdPersonMovement : MonoBehaviour
{
    private CharacterController controller;
    private Vector3 playerVelocity;
    private bool groundedPlayer;
    private float playerSpeed = 7.0f;
    private CameraRoll cameraRoll; // Assuming CameraRoll script exists and works

    // Define boundaries directly in the script or make them public fields
    public float minX = 0f;
    public float maxX = 20f;

    private Camera mainCamera;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        mainCamera = Camera.main; // Cache the main camera
        if (mainCamera != null)
        {
            cameraRoll = mainCamera.GetComponent<CameraRoll>();
        }
        else
        {
            Debug.LogError("Main Camera not found!");
        }
    }

    void Update()
    {
        // --- Ground Check and Gravity ---
        groundedPlayer = controller.isGrounded;
        if (groundedPlayer && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
        }

        // Apply gravity if not grounded
        if (!groundedPlayer)
        {
             // Use Unity's gravity or your own value
            playerVelocity.y += Physics.gravity.y * Time.deltaTime;
        }

        // --- Input and Base Movement Vector ---
        Vector3 moveInput = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

        // Adjust Z movement based on camera roll if applicable
        if (cameraRoll != null && cameraRoll.IsRolling)
        {
            moveInput.z += cameraRoll.rollSpeed * Time.deltaTime; // Careful: This adds speed directly, might need scaling
        }

        // Normalize input if diagonal movement shouldn't be faster
        // Vector3 desiredMoveDirection = moveInput.normalized; // Uncomment if needed
        Vector3 desiredMoveDirection = moveInput; // Use this if diagonal speed increase is okay

        // Calculate the raw desired movement delta for this frame
        Vector3 desiredMoveDelta = desiredMoveDirection * playerSpeed * Time.deltaTime;

        // --- Predict and Clamp Position ---
        Vector3 currentPos = transform.position;
        Vector3 predictedPos = currentPos + desiredMoveDelta;

        // Clamp X position
        predictedPos.x = Mathf.Clamp(predictedPos.x, minX, maxX);

        // Clamp Z position based on Camera Viewport
        if (mainCamera != null)
        {
            // Calculate distance from camera plane. Using controller height might be more stable
            // if the ground isn't perfectly flat relative to the camera.
            // float distance = Mathf.Abs(mainCamera.transform.position.y - predictedPos.y); // Original approach
            // Alternative: Project onto camera forward vector for potentially better stability
            float distance = Vector3.Dot(predictedPos - mainCamera.transform.position, mainCamera.transform.forward);

            // Ensure distance is positive and reasonable (e.g., not behind the camera)
            distance = Mathf.Max(distance, mainCamera.nearClipPlane + 0.01f);

            // Viewport points (Y=0 is bottom, Y=1 is top)
            Vector3 bottomViewportPoint = new Vector3(0.5f, 0f, distance); // Using 0.5f for horizontal center
            Vector3 topViewportPoint = new Vector3(0.5f, 0.5f, distance);

            // Convert viewport points to world points
            Vector3 bottomWorldPoint = mainCamera.ViewportToWorldPoint(bottomViewportPoint);
            Vector3 topWorldPoint = mainCamera.ViewportToWorldPoint(topViewportPoint);

            // Determine the correct limits. Assumes world Z corresponds to screen vertical.
            // Check your camera setup if this assumption is wrong.
            float bottomLimit = bottomWorldPoint.z;
            float topLimit = topWorldPoint.z;

            // Swap if necessary (e.g., camera rotated)
            if (bottomLimit > topLimit)
            {
                float temp = bottomLimit;
                bottomLimit = topLimit;
                topLimit = temp;
            }

             // Apply Z clamping to the predicted position
            predictedPos.z = Mathf.Clamp(predictedPos.z, bottomLimit, topLimit);
        }
        else {
             Debug.LogWarning("Main Camera not found, cannot clamp Z to view.");
        }


        // --- Calculate Allowed Movement ---
        // Find the actual delta needed to reach the (potentially clamped) predicted position
        Vector3 allowedMoveDelta = predictedPos - currentPos;

        // --- Perform Movement ---
        // Move the controller by the allowed horizontal/forward delta
        controller.Move(allowedMoveDelta);

        // Apply vertical velocity (gravity/jumping) separately
        controller.Move(playerVelocity * Time.deltaTime);

        // --- Rotation ---
        // Rotate player to face the *input* direction (looks better than facing clamped direction)
        if (moveInput.sqrMagnitude > 0.01f) // Check sqrMagnitude for efficiency
        {
            // Use only horizontal/vertical input for rotation direction
            Vector3 lookDirection = new Vector3(moveInput.x, 0, moveInput.z);
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }
    }
}