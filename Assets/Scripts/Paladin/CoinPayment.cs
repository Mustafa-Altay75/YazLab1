using UnityEngine;

/// <summary>
/// Altınları Paladin'e teslim etme noktası.
///
/// Önce teslim, oyuncunun sohbete dönüp "altınlar bende" demesine bağlıydı.
/// İki sakıncası vardı: oyuncunun bunu kendiliğinden bulması gerekiyordu, ve
/// tamamlanma modelin o cümleden <c>altin</c> çıkarmasına bağlıydı — ölçümde
/// zaman zaman çıkaramıyordu. Artık eşyayı elden vermek yetiyor.
///
/// <b>Bu modeli atlamak değil.</b> Rüşvet <i>talebi</i> modelden geliyor:
/// sabır sayacı bitince Paladin kendi cümlesiyle fiyatını söylüyor. Oyuncu
/// da o talebi yerine getiriyor. Diğer iki son tamamen modelin çıktısından
/// tetikleniyor.
///
/// Paladin'in kendisinde değil, kendi collider'ı olan bir alt objede
/// durmalı: PaladinNpc de IInteractable ve PlayerInteraction bir collider
/// için GetComponentInParent ile TEK bir tane çözüyor.
/// </summary>
public class CoinPayment : MonoBehaviour, IInteractable
{
    [SerializeField] private PaladinNegotiation negotiation;
    [SerializeField] private PlayerPossessions possessions;

    [SerializeField] private string promptText = "[E] Altınları Paladin'e ver";

    private void Awake()
    {
        if (negotiation == null) negotiation = FindAnyObjectByType<PaladinNegotiation>();
        if (possessions == null) possessions = FindAnyObjectByType<PlayerPossessions>();
    }

    public string GetInteractPrompt()
    {
        return Verilebilir() ? promptText : string.Empty;
    }

    public void Interact(GameObject interactor)
    {
        if (!Verilebilir()) return;
        negotiation.PayBribe();
    }

    private bool Verilebilir()
    {
        if (negotiation == null || possessions == null) return false;
        if (negotiation.Phase != NegotiationPhase.BribeDemanded) return false;

        return possessions.Coins >= negotiation.BribeCoinCount;
    }
}
