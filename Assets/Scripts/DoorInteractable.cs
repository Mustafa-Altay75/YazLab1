using UnityEngine;

public class DoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Door Hinge Settings")]
    [SerializeField] private Transform doorHinge; // Döndürülecek kapý kanadý veya menteþe objesi
    [SerializeField] private Vector3 openRotationOffset = new Vector3(0f, 90f, 0f);
    [SerializeField] private float openSpeed = 3f;

    [Header("Lock Settings")]
    [Tooltip("True ise oyuncu E ile açamaz, sadece dýþarýdan iskelet açabilir.")]
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

        // Kapý kilitliyse oyuncuya ekranda neden açamadýðýný göster
        if (requiresSkeletonOrUnlock)
        {
            return "Kapý dýþarýdan kilitli! (Ýskelet açmalý)";
        }

        return "[E] Kapýyý Aç";
    }

    public void Interact(GameObject interactor)
    {
        if (isOpen) return;

        // Eðer etkileþime giren obje Oyuncu (PlayerController) ise ve kapý kilitliyse açýlmasýna izin verme
        if (requiresSkeletonOrUnlock && interactor != null && interactor.GetComponent<PlayerController>() != null)
        {
            Debug.Log("Bu kapý dýþarýdan kilitli! Minik iskeleti göndererek açtýrmalýsýn.");
            return;
        }

        // Etkileþime giren Minik Ýskelet ise (veya kilit kalktýysa) kapýyý aç
        OpenDoor();
    }

    public void OpenDoor()
    {
        requiresSkeletonOrUnlock = false;
        isOpen = true;

        // Kapý açýldýktan sonra sarý parlama (Highlight) efektini kapat
        InteractableHighlight highlight = GetComponent<InteractableHighlight>();
        if (highlight != null)
        {
            highlight.SetHighlight(false);
            highlight.enabled = false;
        }
    }
}