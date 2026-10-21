using UnityEngine;

public class DoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Door Hinge Settings")]
    [SerializeField] private Transform doorHinge; // Döndürülecek kapı kanadı veya menteşe objesi
    [SerializeField] private Vector3 openRotationOffset = new Vector3(0f, 90f, 0f);
    [SerializeField] private float openSpeed = 3f;

    [Header("Lock Settings")]
    [Tooltip("True ise oyuncu E ile açamaz, sadece dışarıdan iskelet açabilir.")]
    [SerializeField] private bool requiresSkeletonOrUnlock = true;

    private bool isOpen = false;
    private Quaternion targetRotation;

    private void Start()
    {
        if (doorHinge == null) doorHinge = transform;
        targetRotation = Quaternion.Euler(doorHinge.localEulerAngles + openRotationOffset);
    }

    private void Update()
    {
        if (isOpen && doorHinge != null)
        {
            doorHinge.localRotation = Quaternion.Slerp(doorHinge.localRotation, targetRotation, Time.deltaTime * openSpeed);
        }
    }

    public string GetInteractPrompt()
    {
        if (isOpen) return string.Empty;

        // Kapı kilitliyse oyuncuya ekranda neden açamadığını göster
        if (requiresSkeletonOrUnlock)
        {
            return "Kapı dışarıdan kilitli! (İskelet açmalı)";
        }

        return "[E] Kapıyı Aç";
    }

    public void Interact(GameObject interactor)
    {
        if (isOpen) return;

        // Eğer etkileşime giren obje Oyuncu (PlayerController) ise ve kapı kilitliyse açılmasına izin verme
        if (requiresSkeletonOrUnlock && interactor != null && interactor.GetComponent<PlayerController>() != null)
        {
            Debug.Log("Bu kapı dışarıdan kilitli! Minik iskeleti göndererek açtırmalısın.");
            return;
        }

        // Etkileşime giren Minik İskelet ise (veya kilit kalktıysa) kapıyı aç
        OpenDoor();
    }

    public void OpenDoor()
    {
        requiresSkeletonOrUnlock = false;
        isOpen = true;

        // Kapı açıldıktan sonra sarı parlama (Highlight) efektini kapat
        InteractableHighlight highlight = GetComponent<InteractableHighlight>();
        if (highlight != null)
        {
            highlight.SetHighlight(false);
            highlight.enabled = false;
        }
    }
}