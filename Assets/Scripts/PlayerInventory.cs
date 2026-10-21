using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Character Equipment (In Hands)")]
    [SerializeField] private GameObject staffInRightHand;
    [SerializeField] private GameObject spellbookInLeftHand;

    public bool HasChestKey { get; private set; } = false;
    public bool IsArmed { get; private set; } = false;

    private void Start()
    {
        // Oyun başında karakter silahsız başlar
        if (staffInRightHand != null) staffInRightHand.SetActive(false);
        if (spellbookInLeftHand != null) spellbookInLeftHand.SetActive(false);
    }

    public void CollectKey()
    {
        HasChestKey = true;
        Debug.Log("Masadaki anahtar alındı!");
    }

    public void EquipMageGear()
    {
        IsArmed = true;
        if (staffInRightHand != null) staffInRightHand.SetActive(true);
        if (spellbookInLeftHand != null) spellbookInLeftHand.SetActive(true);
        Debug.Log("Asa ve Büyü Kitabı kuşanıldı!");
    }
}