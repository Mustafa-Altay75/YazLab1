using UnityEngine;

/// <summary>
/// Çıkış kapısının eşiği. Oyuncu buradan geçtiğinde oyun biter.
///
/// Son ekranı Paladin ikna olur olmaz değil, <b>oyuncu gerçekten dışarı
/// çıkınca</b> geliyor. Aradaki fark oynanışta büyük: kapı açılıyor,
/// oyuncu kendi ayağıyla yürüyüp çıkıyor, özgürlük anını o yaşıyor.
///
/// Yalnızca "kapıdan çıkılarak varılan" bir sona ulaşıldıysa çalışıyor;
/// hangi sonların öyle olduğunu <see cref="NegotiationEnding.WaitForExit"/>
/// söylüyor, burada sona özel kontrol yok.
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

        var son = negotiation.Reached;
        if (son == null || !son.WaitForExit) return;

        tetiklendi = true;
        endingScreen.PlayExitSequence();
    }
}
