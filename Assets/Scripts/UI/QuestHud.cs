using TMPro;
using UnityEngine;

/// <summary>
/// Sağ üstteki görev paneli.
///
/// Rüşvet görevi oyunun en çok adımı olan parçası: iskeleti çağır, emri
/// ver, toplamasını bekle, altınları al, Paladin'e ver. Oyuncunun bunu
/// konuşmanın içinden çıkarması beklenemezdi; panel her an <b>tek bir</b>
/// sonraki adımı söylüyor.
///
/// Sohbetin kendi durum kutusu sağ altta duruyor ve ayrı bir iş yapıyor:
/// o "Paladin ne yapıyor" der, bu "sen ne yapacaksın" der.
///
/// Panel hiçbir şeye karar vermiyor, yalnızca okuyor. Durumun sahibi
/// PaladinNegotiation ve SkeletonCompanion.
/// </summary>
public class QuestHud : MonoBehaviour
{
    [Header("Arayüz")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI stepText;

    [Header("Bağlantılar")]
    [SerializeField] private PaladinNegotiation negotiation;
    [SerializeField] private SkeletonCompanion companion;
    [SerializeField] private PlayerPossessions possessions;
    [SerializeField] private Transform player;

    [Header("Ayarlar")]
    [Tooltip("İskelet bu mesafeden yakınsa emir verilebilir sayılır; " +
             "PlayerInteraction'ın menzilinden biraz geniş tutuldu ki yazı " +
             "oyuncu tam menzile girmeden görünsün.")]
    [SerializeField] private float orderRange = 3.5f;

    [SerializeField] private string questTitle = "GÖREV";

    private void Awake()
    {
        if (negotiation == null) negotiation = FindAnyObjectByType<PaladinNegotiation>();
        if (companion == null) companion = FindAnyObjectByType<SkeletonCompanion>();
        if (possessions == null) possessions = FindAnyObjectByType<PlayerPossessions>();
        if (player == null && possessions != null) player = possessions.transform;

        Goster(false);
    }

    private void Update()
    {
        if (negotiation == null || companion == null) { Goster(false); return; }

        // Görev yalnızca Paladin altın istedikten sonra var.
        if (!companion.CoinTaskUnlocked) { Goster(false); return; }

        string adim = SonrakiAdim();
        if (string.IsNullOrEmpty(adim)) { Goster(false); return; }

        Goster(true);
        if (titleText != null) titleText.text = questTitle;
        if (stepText != null) stepText.text = adim;
    }

    /// <summary>
    /// O an yapılacak tek iş. Sıra, görevin gerçek sırası: sondan başa
    /// bakılıyor ki tamamlanmış adımlar geride kalsın.
    /// </summary>
    private string SonrakiAdim()
    {
        int gereken = negotiation.BribeCoinCount;

        if (negotiation.Outcome == NegotiationOutcome.Price)
            return "Paladin altınları aldı.\nKapı açıldı — çıkışa git.";

        if (possessions != null && possessions.Coins >= gereken)
            return "Altınlar sende.\n[E] Paladin'e ver";

        if (companion.CoinsReady)
            return "İskelet altınları getirdi.\n[E] Altınları al";

        if (companion.CoinOrderGiven)
            return "İskelet altınları topluyor.\nAltın: " + companion.CoinsCollected + " / " + gereken;

        if (IskeletYakinda())
            return "Paladin " + gereken + " altın istiyor.\n[E] Altınları getirmesini söyle";

        return "Paladin " + gereken + " altın istiyor.\n[F] İskeleti çağır";
    }

    private bool IskeletYakinda()
    {
        if (player == null) return false;

        Vector3 a = player.position;
        Vector3 b = companion.transform.position;
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b) <= orderRange;
    }

    private void Goster(bool acik)
    {
        if (panelRoot != null && panelRoot.activeSelf != acik) panelRoot.SetActive(acik);
    }
}
