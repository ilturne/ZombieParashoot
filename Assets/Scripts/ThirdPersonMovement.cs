using UnityEngine;

public class ThirdPersonMovement : MonoBehaviour
{
    private CharacterController controller;
    private Vector3 playerVelocity;
    private bool groundedPlayer;
    private float playerSpeed = 7.0f;

    private CameraRoll cameraRoll;
    private void Start()
    {
        controller = gameObject.AddComponent<CharacterController>();
        cameraRoll = Camera.main.GetComponent<CameraRoll>();
    }

    void Update()
    {
        groundedPlayer = controller.isGrounded;
        if (groundedPlayer && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
        }

        Vector3 move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        
        if (cameraRoll != null && cameraRoll.IsRolling)
        {
            move.z += cameraRoll.rollSpeed * Time.deltaTime;
        }

        controller.Move(move * Time.deltaTime * playerSpeed);

        if (move != Vector3.zero)
        {
            gameObject.transform.forward = move;
        }

        controller.Move(playerVelocity * Time.deltaTime);

        // Clamp the player position by X and by the bottom of the camera view.
        Vector3 currentPos = transform.position;
        currentPos.x = Mathf.Clamp(currentPos.x, 0f, 20f);

        // Clamp the Z position based on the camera view
        Camera cam = Camera.main;
        float distance = Mathf.Abs(cam.transform.position.y - transform.position.y);
        Vector3 bottomWorldPoint = cam.ViewportToWorldPoint(new Vector3(0.5f, 0f, distance));
        Vector3 topWorldPoint = cam.ViewportToWorldPoint(new Vector3(0.5f, 3f, distance));
        float bottomLimit = bottomWorldPoint.z;
        float topLimit = topWorldPoint.z;
        currentPos.z = Mathf.Clamp(currentPos.z, bottomLimit, topLimit);

        transform.position = currentPos;
    }
}