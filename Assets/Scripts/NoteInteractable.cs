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
        "\"Benden sonra bu hücreye düşecek kader ortağıma...\n\n" +
        "Uzun süredir bu soğuk zindanda tutsağım. Hücreyi incelerken duvardaki bayrağın arkasında gizli bir bölme olduğunu keşfettim. " +
        "İçeride kilitli bir sandık ve sandığın içinde bir asa ile büyü kitabı var. " +
        "Ne yazık ki ben bir büyücü değilim, bu emanetlerin gücünü uyandıramadım.\n\n" +
        "Eğer asayı ve kitabı kullanabilecek kudrete sahipsen, sandığın anahtarını içerideki kırık masanın üzerine bıraktım. Onları al ve buradan kurtul\n\n" +
        "[Kapatmak için tekrar E tuşuna bas veya uzaklaş]\"";

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

        // Oyuncu raftan uzaklaşırsa notu otomatik kapat
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