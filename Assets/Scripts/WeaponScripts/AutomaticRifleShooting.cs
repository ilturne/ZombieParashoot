using UnityEngine;

public class AutomaticRifleShooting : MonoBehaviour
{
    // Assign these in the Inspector:
    public GameObject bulletPrefab;   // The bullet prefab (can be the same or different)
    public float bulletSpeed = 30f;   // How fast the bullet moves (adjust as needed)
    public float fireRate = 10f;      // Bullets fired per second

    private Transform sharedFirePoint; 
    private float nextFireTime = 0f; 

    void Awake()
    {
        // Find the WeaponManager script (assuming it's on a parent GameObject)
        WeaponManager manager = GetComponentInParent<WeaponManager>(); 
        if (manager != null && manager.playerFirePoint != null)
        {
            // Store the reference for later use
            sharedFirePoint = manager.playerFirePoint;
        }
        else
        {
            // Log an error if we can't find it - helps debugging
            Debug.LogError("AutomaticRifleShooting could not find PlayerFirePoint reference via WeaponManager on " + gameObject.name + ". Ensure WeaponManager is on a parent object and PlayerFirePoint is assigned.", this);
            this.enabled = false; // Disable script if fire point is missing
        }
    }

    void Update()
    {
        // Check if the sharedFirePoint was successfully assigned before trying to shoot
        if (sharedFirePoint == null) return;

        // Check if the left mouse button is HELD down
        if (Input.GetMouseButton(0)) 
        {
            // Check if enough time has passed since the last shot
            if (Time.time >= nextFireTime)
            {
                Shoot();
                // Calculate the time for the next allowed shot
                nextFireTime = Time.time + 1f / fireRate; 
            }
        }
    }

    void Shoot()
    {

        if (sharedFirePoint == null) return; 

        GameObject bulletInstance = Instantiate(bulletPrefab, sharedFirePoint.position, sharedFirePoint.rotation);
        Rigidbody rb = bulletInstance.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = sharedFirePoint.forward * bulletSpeed;
        }
        
    }
}