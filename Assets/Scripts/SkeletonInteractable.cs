using UnityEngine;

public class SkeletonInteractable : MonoBehaviour, IInteractable
{
    [Header("Animasyon Ayarlarý")]
    [SerializeField] private Animator skeletonAnimator;
    [SerializeField] private string resurrectTrigger = "Resurrect";
    [SerializeField] private string playerSummonTrigger = "Summon";

    // Arkadaþýnýn NavMesh scriptinden kontrol edebilmesi için public özellik
    public bool IsResurrected { get; private set; } = false;

    private PlayerInventory cachedInventory;

    public string GetInteractPrompt()
    {
        // Dirildiyse ekranda bir þey yazmasýn
        if (IsResurrected) return string.Empty;

        if (cachedInventory == null)
        {
            cachedInventory = FindFirstObjectByType<PlayerInventory>();
        }

        // Asa ve büyü kitabý alýnmadýysa uyarý ver
        if (cachedInventory != null && !cachedInventory.IsArmed)
        {
            return "Diriltmek için Asa ve Büyü Kitabý gerekli!";
        }

        return "[E] Ýskeleti Dirilt";
    }

    public void Interact(GameObject interactor)
    {
        if (IsResurrected) return;

        PlayerInventory inventory = interactor.GetComponent<PlayerInventory>();
        if (inventory == null || !inventory.IsArmed) return;

        IsResurrected = true;

        // 1. Büyücünün çaðýrma (Summon) animasyonunu oynat
        Animator playerAnimator = interactor.GetComponentInChildren<Animator>();
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(playerSummonTrigger);
        }

        // 2. Ýskeletin yerden kalkma (Resurrect) animasyonunu oynat
        if (skeletonAnimator != null)
        {
            skeletonAnimator.SetTrigger(resurrectTrigger);
        }

        // 3. Sarý parlama (Highlight) efektini kapat
        InteractableHighlight highlight = GetComponent<InteractableHighlight>();
        if (highlight != null)
        {
            highlight.SetHighlight(false);
            highlight.enabled = false;
        }
    }
}