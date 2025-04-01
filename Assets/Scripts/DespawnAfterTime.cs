using UnityEngine;

public class DespawnAfterTime : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public float lifetime = 5f; // Time in seconds before the object despawns
    void Start()
    {
        Destroy(gameObject, lifetime);
    }
}
