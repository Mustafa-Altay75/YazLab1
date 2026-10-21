using UnityEngine;

public class KeyPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private string promptText = "[E] Anahtarı Al";

    public string GetInteractPrompt() => promptText;

    public void Interact(GameObject interactor)
    {
        if (interactor.TryGetComponent<PlayerInventory>(out PlayerInventory inventory))
        {
            inventory.CollectKey();
            // Anahtarı masadan yok et
            gameObject.SetActive(false);
        }
    }
}