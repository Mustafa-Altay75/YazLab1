using UnityEngine;

/// <summary>
/// Çıkış kapısının eşiği. Oyuncu buradan geçtiğinde oyun biter.
///
/// Son ekranı artık Paladin ikna olur olmaz değil, <b>oyuncu gerçekten
/// dışarı çıkınca</b> geliyor. Aradaki fark oynanışta büyük: kapı açılıyor,
/// oyuncu kendi ayağıyla yürüyüp çıkıyor, özgürlük anını o yaşıyor.
/// Ekranın hemen basması o anı elinden alıyordu.
///
/// Tetikleyici yalnızca kapıyı açan bir sona varıldıysa çalışıyor; kapı
/// zaten kapalıyken buradan geçilemez ama hata ayıklama sırasında geçilirse
/// oyun yanlışlıkla bitmesin.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ExitTrigger : MonoBehaviour
{
    [SerializeField] private PaladinNegotiation negotiation;
    [SerializeField] private EndingScreen endingScreen;

    private bool tetiklendi;

    private void Awake()
    {
        if (negotiation == null) negotiation = FindAnyObjectByType<PaladinNegotiation>();
        if (endingScreen == null) endingScreen = FindAnyObjectByType<EndingScreen>();

        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (tetiklendi || negotiation == null || endingScreen == null) return;
        if (other.GetComponentInParent<PlayerController>() == null) return;

        var son = negotiation.Outcome;
        if (son != NegotiationOutcome.Passage && son != NegotiationOutcome.Price) return;

        tetiklendi = true;
        endingScreen.PlayExitSequence(son);
    }
}
