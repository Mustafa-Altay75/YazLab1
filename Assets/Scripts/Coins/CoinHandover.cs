using UnityEngine;

/// <summary>
/// İskeletin getirdiği altınları oyuncuya devreden etkileşim noktası.
///
/// İskeletin kendisinde değil, kendi collider'ı olan bir alt objede
/// durmalı — SkeletonOrderInteractable ile aynı sebep: PlayerInteraction
/// bir collider için GetComponentInParent ile TEK bir IInteractable
/// çözüyor, aynı objede ikinci bir tanesi gölgede kalırdı.
///
/// Altınlar buradan <see cref="PlayerPossessions"/>'a geçiyor. Oyuncu
/// bundan sonra Paladin'e "altınlar bende" dediğinde model bunu
/// <c>claims</c>'te raporluyor ve kod envanterden doğruluyor — asa ve
/// büyü kitabıyla tamamen aynı yol.
/// </summary>
public class CoinHandover : MonoBehaviour, IInteractable
{
    [Tooltip("Boş bırakılırsa üst objelerde aranır.")]
    [SerializeField] private SkeletonCompanion companion;

    [Tooltip("Boş bırakılırsa sahnede aranır.")]
    [SerializeField] private PlayerPossessions possessions;

    [SerializeField] private string promptText = "[E] Altınları al";

    private void Awake()
    {
        if (companion == null) companion = GetComponentInParent<SkeletonCompanion>();
        if (possessions == null) possessions = FindAnyObjectByType<PlayerPossessions>();
    }

    public string GetInteractPrompt()
    {
        if (companion == null || !companion.CoinsReady) return string.Empty;
        return promptText;
    }

    public void Interact(GameObject interactor)
    {
        if (companion == null || !companion.CoinsReady) return;

        if (possessions != null) possessions.AddCoins(companion.CoinsCollected);
        companion.TakeCoinsFromSkeleton();
    }
}
