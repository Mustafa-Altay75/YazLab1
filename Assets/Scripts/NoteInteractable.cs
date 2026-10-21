using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class NoteInteractable : MonoBehaviour, IInteractable
{
    [Header("UI References")]
    [SerializeField] private GameObject notePanel;
    [SerializeField] private TextMeshProUGUI noteContentText;

    [Header("Note Content")]
    [TextArea(5, 10)]
    [SerializeField]
    private string noteText =
        "\"Benden sonra bu hücreye düþecek kader ortaðýma...\n\n" +
        "Uzun süredir bu soðuk zindanda tutsaðým. Hücreyi incelerken duvardaki bayraðýn arkasýnda gizli bir bölme olduðunu keþfettim. " +
        "Ýçeride kilitli bir sandýk ve sandýðýn içinde bir asa ile büyü kitabý var. " +
        "Ne yazýk ki ben bir büyücü deðilim, bu emanetlerin gücünü uyandýramadým.\n\n" +
        "Eðer asayý ve kitabý kullanabilecek kudrete sahipsen, sandýðýn anahtarýný içerideki kýrýk masanýn üzerine býraktým. Onlarý al ve buradan kurtul\n\n" +
        "[Kapatmak için tekrar E tuþuna bas veya uzaklaþ]\"";

    [Header("Settings")]
    [SerializeField] private float autoCloseDistance = 3.0f;

    private bool isReading = false;
    private Transform playerTransform;

    public string GetInteractPrompt()
    {
        return isReading ? "[E] Notu Kapat" : "[E] Notu Oku";
    }

    public void Interact(GameObject interactor)
    {
        playerTransform = interactor.transform;
        ToggleNote(!isReading);
    }

    private void Update()
    {
        if (!isReading) return;

        // Oyuncu raftan uzaklaþýrsa notu otomatik kapat
        if (playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            if (distance > autoCloseDistance)
            {
                ToggleNote(false);
            }
        }
    }

    private void ToggleNote(bool state)
    {
        isReading = state;

        if (notePanel != null)
        {
            notePanel.SetActive(isReading);
        }

        if (isReading && noteContentText != null)
        {
            noteContentText.text = noteText;
        }
    }
}