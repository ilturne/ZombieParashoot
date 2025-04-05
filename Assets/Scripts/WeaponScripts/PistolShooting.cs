using UnityEngine;

public class PistolShooting : MonoBehaviour
{
    // Assign these in the Inspector:
    public GameObject bulletPrefab;  // The bullet prefab
    public GameObject firePoint;      // The fire point on the pistol
    public float bulletSpeed = 20f;  // How fast the bullet moves

    void Update()
    {
        // Check if the left mouse button is clicked
        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }

    void Shoot()
    {
        // Instantiate the bullet at the firePoint's position and rotation
        GameObject bulletInstance = Instantiate(bulletPrefab, firePoint.transform.position, firePoint.transform.rotation);

        // Get the bullet's Rigidbody and set its velocity
        Rigidbody rb = bulletInstance.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = firePoint.transform.forward * bulletSpeed;
        }
    }
}
