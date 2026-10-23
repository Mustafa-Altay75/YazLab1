using UnityEngine;

/// <summary>
/// Oyuncunun iskelete emir vermesini sağlayan etkileşim noktası.
///
/// Mustafa'nın IInteractable arayüzünü uyguluyor; böylece iskelet de not,
/// sandık ve kapı gibi sıradan bir etkileşim nesnesi oluyor ve
/// PlayerInteraction'a hiç dokunmadan onun sistemine bağlanıyor.
///
/// ÖNEMLİ: Bu bileşen Skeleton_Minion'un KENDİSİNDE değil, kendi collider'ı
/// olan bir alt objede durmalı. Çünkü PlayerInteraction bir collider için
/// GetComponentInParent ile TEK bir IInteractable çözüyor; aynı objede
/// SkeletonInteractable zaten var ve ikincisi gölgede kalırdı.
/// </summary>
public class SkeletonOrderInteractable : MonoBehaviour, IInteractable
{
    [Tooltip("Boş bırakılırsa üst objelerde aranır.")]
    [SerializeField] private SkeletonCompanion companion;

    [Tooltip("Boş bırakılırsa üst objelerde aranır.")]
    [SerializeField] private SkeletonInteractable interactable;

    [SerializeField] private string promptText = "[E] Anahtarı getir";

    private void Awake()
    {
        if (companion == null) companion = GetComponentInParent<SkeletonCompanion>();
        if (interactable == null) interactable = GetComponentInParent<SkeletonInteractable>();
    }

    public string GetInteractPrompt()
    {
        // Diriltilmeden emir verilemez; o aşamada söz SkeletonInteractable'da.
        if (interactable == null || !interactable.IsResurrected) return string.Empty;

        // Emir verildiyse artık gösterecek bir şey yok.
        if (companion == null || companion.OrderGiven) return string.Empty;

        return promptText;
    }

    public void Interact(GameObject interactor)
    {
        if (interactable == null || !interactable.IsResurrected) return;
        if (companion == null || companion.OrderGiven) return;

        companion.GiveOrder();
    }
}
