using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Üç sonun ekranı ve çıkış sineması.
///
/// Akış sona göre ayrışıyor:
///
/// <b>Geçiş ve Bedel</b> — kapı açılır, oyuncu kendi ayağıyla çıkar.
/// Eşikten geçtiğinde kontrol kesilir, ekran yavaşça beyazlar, birkaç
/// saniye sonra son ekranı gelir. Özgürlük anını oyuncunun yaşaması için:
/// ekranın kapı açılır açılmaz basması o anı elinden alıyordu.
///
/// <b>Hüküm</b> — çıkılacak bir kapı yok. Paladin saldırır, ekran kısa bir
/// gecikmeyle gelir ve tek seçenek yeniden başlamaktır.
///
/// Buradaki metinler modelin repliği değil, oyunun menü yazıları —
/// "Oyun Bitti" ile aynı türden. Paladin'in ağzından çıkan her cümle
/// modelden geliyor.
/// </summary>
public class EndingScreen : MonoBehaviour
{
    [Header("Arayüz")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private TextMeshProUGUI hintText;

    [Tooltip("Ekranı beyazlatan tam ekran görsel. Alfası koddan sürülüyor.")]
    [SerializeField] private UnityEngine.UI.Image whiteOverlay;

    [Tooltip("Yeniden oyna düğmesi.")]
    [SerializeField] private UnityEngine.UI.Button restartButton;

    [Header("Bağlantılar")]
    [SerializeField] private PaladinNegotiation negotiation;

    [Tooltip("Geçiş ve bedel sonlarında açılacak çıkış kapısı.")]
    [SerializeField] private ExitDoor exitDoor;

    [Tooltip("Bedel sonunda oyuncunun bırakacağı asa ve kitap.")]
    [SerializeField] private GameObject[] surrenderedGear;

    [Tooltip("Sinema başlayınca kapatılacak oyuncu bileşenleri.")]
    [SerializeField] private MonoBehaviour[] disableOnCinematic;

    [Header("Zamanlama")]
    [Tooltip("Ekranın beyazlaması kaç saniye sürsün.")]
    [SerializeField] private float fadeSeconds = 1.6f;

    [Tooltip("Beyaz dolduktan sonra son ekranı gelene kadar beklenen süre.")]
    [SerializeField] private float holdSeconds = 1.4f;

    [Tooltip("Hüküm sonunda ekranın gelmesi için beklenen süre; son replik okunsun diye.")]
    [SerializeField] private float judgementDelay = 2.0f;

    [SerializeField] private Key restartKey = Key.R;

    private NegotiationOutcome son = NegotiationOutcome.None;
    private bool gorunur;

    private void Awake()
    {
        if (negotiation == null) negotiation = FindAnyObjectByType<PaladinNegotiation>();
        if (negotiation != null) negotiation.OutcomeReached += SonaUlasildi;

        if (panelRoot != null) panelRoot.SetActive(false);
        if (whiteOverlay != null)
        {
            whiteOverlay.gameObject.SetActive(false);
            whiteOverlay.color = new Color(1f, 1f, 1f, 0f);
        }
        if (restartButton != null) restartButton.onClick.AddListener(YenidenBasla);
    }

    private void OnDestroy()
    {
        if (negotiation != null) negotiation.OutcomeReached -= SonaUlasildi;
        if (restartButton != null) restartButton.onClick.RemoveListener(YenidenBasla);
    }

    private void Update()
    {
        if (!gorunur || Keyboard.current == null) return;
        if (Keyboard.current[restartKey].wasPressedThisFrame) YenidenBasla();
    }

    /// <summary>
    /// Sona varıldı. Kapı açılan sonlarda ekran burada GELMİYOR; yalnızca
    /// dünya hazırlanıyor ve oyuncu çıkışa yönlendiriliyor.
    /// </summary>
    private void SonaUlasildi(NegotiationOutcome gelen)
    {
        son = gelen;

        switch (son)
        {
            case NegotiationOutcome.Passage:
                if (exitDoor != null) exitDoor.Open();
                break;

            case NegotiationOutcome.Price:
                if (exitDoor != null) exitDoor.Open();
                foreach (var g in surrenderedGear) if (g != null) g.SetActive(false);
                break;

            case NegotiationOutcome.Judgement:
                StartCoroutine(HukumuGoster());
                break;
        }
    }

    /// <summary>Oyuncu çıkış eşiğinden geçti. ExitTrigger çağırıyor.</summary>
    public void PlayExitSequence(NegotiationOutcome gelen)
    {
        son = gelen;
        StartCoroutine(CikisSinemasi());
    }

    private IEnumerator CikisSinemasi()
    {
        KontroluKes();

        // Ekranı beyazlat: kapıdan çıkışın kendisi.
        if (whiteOverlay != null)
        {
            whiteOverlay.gameObject.SetActive(true);

            float t = 0f;
            while (t < fadeSeconds)
            {
                t += Time.deltaTime;
                float a = Mathf.Clamp01(t / Mathf.Max(0.01f, fadeSeconds));
                whiteOverlay.color = new Color(1f, 1f, 1f, a);
                yield return null;
            }
            whiteOverlay.color = Color.white;
        }

        yield return new WaitForSeconds(holdSeconds);
        Yaz();
        Ac();
    }

    private IEnumerator HukumuGoster()
    {
        yield return new WaitForSeconds(judgementDelay);
        KontroluKes();
        Yaz();
        Ac();
    }

    /// <summary>
    /// Sinema başlayınca oyuncunun kontrolü kesiliyor: oyuncu beyazın
    /// içinde yürümeye devam ederse an dağılıyor.
    /// </summary>
    private void KontroluKes()
    {
        foreach (var m in disableOnCinematic) if (m != null) m.enabled = false;
    }

    private void Ac()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        gorunur = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Yaz()
    {
        switch (son)
        {
            case NegotiationOutcome.Passage:
                Metin("GEÇİŞ",
                      "Paladin kenara çekildi. Sözlerin, kemiklerinden daha ağır bastı.");
                break;

            case NegotiationOutcome.Price:
                Metin("BEDEL",
                      "Altınları aldı ve kapıyı açtı. Asan ve kitabın burada kalıyor; " +
                      "dışarı çıkan sen değil, senden geriye kalan.");
                break;

            case NegotiationOutcome.Judgement:
                Metin("HÜKÜM",
                      "Paladin kılıcını çekti. Bu kapı, tehdit edenlere açılmaz.");
                break;
        }
    }

    private void Metin(string baslik, string govde)
    {
        if (titleText != null) titleText.text = baslik;
        if (bodyText != null) bodyText.text = govde;
        if (hintText != null) hintText.text = "[" + restartKey + "] veya düğmeyle yeniden oyna";
    }

    private void YenidenBasla()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
