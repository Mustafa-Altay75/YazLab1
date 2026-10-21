using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float interactRange = 2.0f;
    [SerializeField] private Vector3 detectionOffset = new Vector3(0f, 0.8f, 0.5f);

    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI promptTextUI;

    private IInteractable currentInteractable;
    private InteractableHighlight currentHighlight;

    private void Update()
    {
        DetectInteractable();

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (currentInteractable != null)
            {
                currentInteractable.Interact(gameObject);
                // Etkileþim sonrasý metin ve parlamayý anýnda güncelle
                DetectInteractable();
            }
        }
    }

    private void DetectInteractable()
    {
        IInteractable foundInteractable = null;
        InteractableHighlight foundHighlight = null;

        Vector3 center = transform.TransformPoint(detectionOffset);
        Collider[] hits = Physics.OverlapSphere(center, interactRange);

        float closestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            if (!hit.gameObject.activeInHierarchy) continue;

            // Objenin kendisinde veya üst objesinde IInteractable var mý bak
            IInteractable interactable = hit.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                // Eðer kapý/sandýk zaten açýlmýþsa ve mesajý boþsa etkileþime alma
                if (string.IsNullOrEmpty(interactable.GetInteractPrompt())) continue;

                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    foundInteractable = interactable;
                    foundHighlight = hit.GetComponentInParent<InteractableHighlight>();
                }
            }
        }

        // Önceki objeden uzaklaþýldýysa parlamasýný kapat
        if (currentHighlight != null && currentHighlight != foundHighlight)
        {
            currentHighlight.SetHighlight(false);
        }

        // Yeni objeye yaklaþýldýysa parlamasýný aç
        if (foundHighlight != null)
        {
            foundHighlight.SetHighlight(true);
        }

        currentInteractable = foundInteractable;
        currentHighlight = foundHighlight;

        // Ekran yazýsýný güncelle
        if (promptTextUI != null)
        {
            promptTextUI.text = currentInteractable != null ? currentInteractable.GetInteractPrompt() : "";
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = transform.TransformPoint(detectionOffset);
        Gizmos.DrawWireSphere(center, interactRange);
    }
}