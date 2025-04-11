using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    // Assign your weapon GameObjects (the ones in the Hierarchy) here
    public List<GameObject> weapons; 

    // *** NEW: Assign the single PlayerFirePoint GameObject here ***
    public Transform playerFirePoint; 

    private int currentWeaponIndex = 0;

    void Start()
    {
        if (playerFirePoint == null)
        {
             Debug.LogError("PlayerFirePoint is not assigned in the WeaponManager!", this);
             // Consider disabling the component or handling this error appropriately
        }
        SelectWeapon(currentWeaponIndex); 
    }

    void Update()
    {
        // Don't allow switching if the list is empty or not set up
        if (weapons == null || weapons.Count == 0) return;

        int previousWeaponIndex = currentWeaponIndex;

        // Detect scroll wheel input
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll > 0f) // Scrolled Up
        {
            currentWeaponIndex--;
            if (currentWeaponIndex < 0)
            {
                currentWeaponIndex = weapons.Count - 1; // Wrap around to the last weapon
            }
        }
        else if (scroll < 0f) // Scrolled Down
        {
            currentWeaponIndex++;
            if (currentWeaponIndex >= weapons.Count)
            {
                currentWeaponIndex = 0; // Wrap around to the first weapon
            }
        }

        // Only switch if the index actually changed
        if (previousWeaponIndex != currentWeaponIndex)
        {
            SelectWeapon(currentWeaponIndex);
        }
    }

    void SelectWeapon(int index)
    {
        // Basic validation
        if (weapons == null || weapons.Count == 0) return; 
        if (index < 0 || index >= weapons.Count)
        {
             Debug.LogError("Invalid weapon index selected: " + index, this);
             index = 0; // Default to first weapon
        }

        // Loop through all weapons and activate/deactivate them
        for (int i = 0; i < weapons.Count; i++)
        {
            // Check if the weapon reference is valid before trying to activate/deactivate
            if (weapons[i] != null) 
            {
                 weapons[i].SetActive(i == index); 
            }
            else
            {
                Debug.LogWarning($"Weapon at index {i} is null in the WeaponManager list.", this);
            }
        }

        currentWeaponIndex = index; 

        // Optional: Log which weapon is active (check if reference is valid first)
        if (weapons[currentWeaponIndex] != null)
        {
            // Debug.Log("Switched to weapon: " + weapons[currentWeaponIndex].name); 
        }
    }
}