using UnityEngine;

public class ShotgunShooting : MonoBehaviour
{
    // Assignables
    public GameObject pelletPrefab;     // Prefab for each pellet
    
    // Shotgun Stats
    public float pelletSpeed = 15f;     // Speed of each pellet
    public int minPellets = 6;          // Minimum pellets per shot
    public int maxPellets = 10;         // Maximum pellets per shot
    public float spreadFactor = 0.1f;   // How much the pellets spread out (0 = no spread, higher = wider)
    public float cooldownTime = 1.0f;   // Seconds between allowed shots

    // Internal cooldown tracking
    private float nextFireTime = 0f;

    // Reference to the player's single fire point
    private Transform sharedFirePoint; 

    void Awake()
    {
        // Get the shared fire point reference from the WeaponManager
        WeaponManager manager = GetComponentInParent<WeaponManager>(); 
        if (manager != null && manager.playerFirePoint != null)
        {
            sharedFirePoint = manager.playerFirePoint;
        }
        else
        {
            Debug.LogError("ShotgunShooting could not find PlayerFirePoint reference via WeaponManager on " + gameObject.name + ". Ensure WeaponManager is on a parent object and PlayerFirePoint is assigned.", this);
            this.enabled = false; // Disable script if fire point is missing
        }

        // Basic validation for pellet counts
        if (minPellets > maxPellets)
        {
            Debug.LogWarning("Shotgun minPellets is greater than maxPellets. Clamping minPellets to maxPellets.", this);
            minPellets = maxPellets;
        }
        if (minPellets < 1)
        {
             Debug.LogWarning("Shotgun minPellets must be at least 1. Setting to 1.", this);
             minPellets = 1;
        }
    }

    void Update()
    {
        // Don't proceed if the fire point isn't set
        if (sharedFirePoint == null) return; 

        // Check if enough time has passed since the last shot
        if (Time.time >= nextFireTime)
        {
            // Check for the fire button click (only on the frame it's pressed down)
            if (Input.GetMouseButtonDown(0))
            {
                Shoot();
                // Set the time when the next shot is allowed
                nextFireTime = Time.time + cooldownTime; 
            }
        }
    }

    void Shoot()
    {
         // Determine how many pellets to fire this time
        int pelletCount = Random.Range(minPellets, maxPellets + 1); // +1 because Random.Range (int) is exclusive of the max value

        // Debug.Log($"Firing {pelletCount} pellets."); // Optional: for testing

        for (int i = 0; i < pelletCount; i++)
        {
            // 1. Calculate Direction with Spread
            Vector3 baseDirection = sharedFirePoint.forward;
            
            // Get a random offset vector within a unit sphere, scaled by our spread factor
            Vector3 spread = Random.insideUnitSphere * spreadFactor; 
            
            // Add the spread to the base direction 
            // (For more precise control, you might normalize this result, but this is often sufficient)
            Vector3 fireDirection = baseDirection + spread; 

            // 2. Instantiate the Pellet
            // We can instantiate at the fire point, facing the direction it will travel
            GameObject pelletInstance = Instantiate(pelletPrefab, sharedFirePoint.position, Quaternion.LookRotation(fireDirection));
            // Alternatively, if pellet rotation doesn't matter:
            // GameObject pelletInstance = Instantiate(pelletPrefab, sharedFirePoint.position, sharedFirePoint.rotation);

            // 3. Set Pellet Velocity
            Rigidbody rb = pelletInstance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = fireDirection.normalized * pelletSpeed; // Use normalized direction for consistent speed
            }
        }

    }
}