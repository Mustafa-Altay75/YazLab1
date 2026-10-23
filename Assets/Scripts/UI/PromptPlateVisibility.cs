using TMPro;
using UnityEngine;

/// <summary>
/// Etkileşim yazısının arkasındaki taş levhayı, yazı boşken gizler.
///
/// PlayerInteraction yakınında etkileşilecek bir şey yokken metni boş
/// bırakıyor. Levha kendi başına bir Image olduğu için metin boşalsa bile
/// ekranda kalırdı; bu bileşen metni izleyip levhayı birlikte söndürüyor.
/// Böylece PlayerInteraction'a hiç dokunmadan çalışıyor.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class PromptPlateVisibility : MonoBehaviour
{
    [Tooltip("İzlenecek yazı. Boş bırakılırsa alt objelerde aranır.")]
    [SerializeField] private TextMeshProUGUI promptText;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (promptText == null) promptText = GetComponentInChildren<TextMeshProUGUI>();

        // Oyun başlarken görünmesin; ilk kare dolana kadar bekleme olmasın.
        Uygula(false);
    }

    private void LateUpdate()
    {
        bool gorunsun = promptText != null && !string.IsNullOrEmpty(promptText.text);
        Uygula(gorunsun);
    }

    private void Uygula(bool gorunsun)
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = gorunsun ? 1f : 0f;
    }
}
