using UnityEngine;

public class ChestInteractable : MonoBehaviour, IInteractable
{
    [Header("Hinge Settings")]
    [SerializeField] private Transform chestLid;         // Sandýðýn sadece üst kapaðý (chest_lid)
    [SerializeField] private Vector3 openRotationOffset = new Vector3(-105f, 0f, 0f); // Kapaðýn geriye açýlma açýsý
    [SerializeField] private float openSpeed = 4f;

    private bool isOpen = false;
    private Quaternion targetRotation;

    private void Start()
    {
        if (chestLid != null)
        {
            targetRotation = Quaternion.Euler(chestLid.localEulerAngles + openRotationOffset);
        }
    }

    private void Update()
    {
        if (isOpen && chestLid != null)
        {
            chestLid.localRotation = Quaternion.Slerp(chestLid.localRotation, targetRotation, Time.deltaTime * openSpeed);
        }
    }

    public string GetInteractPrompt()
    {
        return isOpen ? "Sandýk Açýldý" : "[E] Sandýðý Aç";
    }

    public void Interact(GameObject interactor)
    {
        if (isOpen) return;

        if (interactor.TryGetComponent<PlayerInventory>(out PlayerInventory inventory))
        {
            if (!inventory.HasChestKey)
            {
                Debug.Log("Sandýk kilitli! Önce kýrýk masanýn üzerindeki anahtarý almalýsýn.");
                return;
            }

            isOpen = true;
            inventory.EquipMageGear();
        }
    }
}