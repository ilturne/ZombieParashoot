using UnityEngine;

public class CameraRoll : MonoBehaviour
{
    public float delayBeforeRoll = 5f;   // Time to wait before the camera starts moving
    public float rollSpeed = 2.0f;         // Speed at which the camera will move in the Z direction
    private bool startRolling = false;
    private bool debugToggle = false;    // Allows you to override the delay and toggle rolling

    public bool IsRolling { get { return startRolling; } } // Property to check if the camera is rolling
    void Start()
    {
        // Start the delayed rolling only if debug override is off.
        if (!debugToggle)
            Invoke("StartRolling", delayBeforeRoll);
    }

    void Update()
    {
        // Debug: Press X to toggle camera rolling on/off.
        if (Input.GetKeyDown(KeyCode.X))
        {
            startRolling = !startRolling;
            debugToggle = true; // Once we use the toggle, ignore the delay.
            Debug.Log("Camera rolling toggled: " + (startRolling ? "On" : "Off"));
        }

        // If rolling is enabled, move the camera forward in the Z direction.
        if (startRolling)
        {
            transform.position += new Vector3(0f, 0f, rollSpeed * Time.deltaTime);
        }
    }

    // Called after the delay to enable camera rolling.
    void StartRolling()
    {
        startRolling = true;
    }
}
