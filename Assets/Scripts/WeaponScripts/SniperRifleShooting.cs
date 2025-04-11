using UnityEngine;

public class SniperRifleShooting : MonoBehaviour
{
    // Assignables
    public GameObject bulletPrefab;     // Prefab for the sniper bullet
    
    // Sniper Stats
    public float bulletSpeed = 100f;    // Sniper bullets are typically very fast
    public float cooldownTime = 1.5f;   // Seconds between allowed shots (adjust as needed)

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
            Debug.LogError("SniperRifleShooting could not find PlayerFirePoint reference via WeaponManager on " + gameObject.name + ". Ensure WeaponManager is on a parent object and PlayerFirePoint is assigned.", this);
            this.enabled = false; // Disable script if fire point is missing
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
        // Ensure we have the reference (extra safety)
        if (sharedFirePoint == null) return;

        // 1. Instantiate ONE bullet at the shared fire point
        //    Rotation is set to the fire point's rotation, bullet travels based on velocity direction.
        GameObject bulletInstance = Instantiate(bulletPrefab, sharedFirePoint.position, sharedFirePoint.rotation);

        // 2. Set Bullet Velocity (straight forward, high speed)
        Rigidbody rb = bulletInstance.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = sharedFirePoint.forward * bulletSpeed;
        }

    }
}