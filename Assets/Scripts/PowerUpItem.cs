using UnityEngine;

public class PowerUpItem : MonoBehaviour
{
    [Header("Visual Effects")]
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.25f;
    [SerializeField] private float spinSpeed = 50f;

    private float startY;
    private Vector3 startPosition; // Store initial position for bobbing calculation

    void Start()
    {
        // Store the initial local position relative to where it was spawned
        startPosition = transform.position;
        startY = startPosition.y; // Should be 1f based on spawner logic
    }

    void Update()
    {
        // Bobbing effect
        float newY = startY + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // Spinning effect (around its own up axis)
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime);
    }

    // --- Collision Detection ---
    // This function is called when another Collider enters this trigger
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object that entered the trigger has the "Player" tag
        if (other.CompareTag("Player"))
        {
            // Placeholder for activating the specific power-up effect
            Debug.Log($"Player collided with power-up: {gameObject.name}");

            // --- TODO: Implement specific power-up logic here ---
            // Example: other.GetComponent<PlayerHealth>()?.AddHealth(25);
            // Example: other.GetComponent<PlayerMovement>()?.IncreaseSpeed(5f, 10f); // Speed boost for 10 seconds

            // Destroy the power-up object after collection
            Destroy(gameObject);
        }
    }
}