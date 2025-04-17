using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    // Assign your weapon GameObjects (the ones in the Hierarchy) here
    public List<GameObject> weapons; 

    // *** NEW: Assign the single PlayerFirePoint GameObject here ***
    public int[] killThresholds = new int[] { 0, 12, 25, 35 }; 
    private int currentWeaponIndex = 0;
    public Transform playerFirePoint; 


    void Start()
    {
        if (playerFirePoint == null)
        {
             Debug.LogError("PlayerFirePoint is not assigned in the WeaponManager!", this);
             // Consider disabling the component or handling this error appropriately
        }
        SelectWeapon(currentWeaponIndex); 
        SelectWeapon(0);
    }

    void Update()
    {
        if (weapons == null || weapons.Count == 0) return;

        // 1) determine max unlocked index
        int kills = GameManager.Instance?.KillCount ?? 0;
        int maxUnlocked = 0;
        for (int i = 1; i < killThresholds.Length && i < weapons.Count; i++)
            if (kills >= killThresholds[i])
                maxUnlocked = i;

        int prev = currentWeaponIndex;
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f) currentWeaponIndex--;
        else if (scroll < 0f) currentWeaponIndex++;

        // 2) wrap & clamp within [0..maxUnlocked]
        if (currentWeaponIndex < 0) currentWeaponIndex = maxUnlocked;
        if (currentWeaponIndex > maxUnlocked) currentWeaponIndex = 0;

        if (prev != currentWeaponIndex)
            SelectWeapon(currentWeaponIndex);
    }

    void SelectWeapon(int index)
    {
        for (int i = 0; i < weapons.Count; i++)
            if (weapons[i] != null)
                weapons[i].SetActive(i == index);

        currentWeaponIndex = index;
    }
}