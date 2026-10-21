using UnityEngine;

public class DoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Door Hinge Settings")]
    [SerializeField] private Transform doorHinge;        // Döndürülecek kapý kanadý veya menteþe objesi
    [SerializeField] private Vector3 openRotationOffset = new Vector3(0f, 90f, 0f);
    [SerializeField] private float openSpeed = 3f;
    [SerializeField] private bool requiresSkeletonOrUnlock = false; // Ýskelet açacaksa true yapýlabilir

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
        return isOpen ? "" : "[E] Kapýyý Aç";
    }

    public void Interact(GameObject interactor)
    {
        if (requiresSkeletonOrUnlock)
        {
            Debug.Log("Bu kapý dýþarýdan kilitli! Minik iskeleti göndererek açtýrmalýsýn.");
            return;
        }

        OpenDoor();
    }

    public void OpenDoor()
    {
        isOpen = true;
    }
}