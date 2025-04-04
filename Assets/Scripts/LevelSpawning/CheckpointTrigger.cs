using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    public SectionManager sectionToSpawn;
    public SectionManager sectionToDespawn;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        if (other.CompareTag("Player"))
        {
            if (sectionToSpawn != null)
                sectionToSpawn.Spawn();

            if (sectionToDespawn != null)
                sectionToDespawn.Despawn();

            triggered = true;
        }
    }
}
