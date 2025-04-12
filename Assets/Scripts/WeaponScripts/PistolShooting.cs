using UnityEngine;

public class PistolShooting : MonoBehaviour
{
    // Assign these in the Inspector:
    public GameObject bulletPrefab;   // The bullet prefab
    public float bulletSpeed = 20f;   // How fast the bullet moves
    private Transform sharedFirePoint; 

    void Awake()
    {
        WeaponManager manager = GetComponentInParent<WeaponManager>(); 
        if (manager != null && manager.playerFirePoint != null)
        {
            sharedFirePoint = manager.playerFirePoint;
        }
        else
        {
            Debug.LogError("PistolShooting could not find PlayerFirePoint reference via WeaponManager on " + gameObject.name + ". Ensure WeaponManager is on a parent object and PlayerFirePoint is assigned.", this);
            this.enabled = false; 
        }
    }

    void Update()
    {
        // Check if the sharedFirePoint was successfully assigned before trying to shoot
        if (sharedFirePoint == null) return; 

        // Check if the left mouse button is clicked
        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }

    void Shoot()
    {
        // Ensure we have the reference before proceeding (extra safety)
        if (sharedFirePoint == null) return; 

        // Instantiate the bullet at the SHARED firePoint's position and rotation
        GameObject bulletInstance = Instantiate(bulletPrefab, sharedFirePoint.position, sharedFirePoint.rotation);

        // Get the bullet's Rigidbody and set its velocity based on the SHARED firePoint's forward direction
        Rigidbody rb = bulletInstance.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = sharedFirePoint.forward * bulletSpeed;
        }
    }
}