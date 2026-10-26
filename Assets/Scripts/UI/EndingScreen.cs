using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Son ekranı ve çıkış sineması.
///
/// <b>Hangi sonların var olduğunu bilmiyor.</b> Başlık, metin ve davranış
/// <see cref="NegotiationEnding"/>'in kendisinden geliyor; burada sona
/// özel tek satır yok. Yeni bir son eklendiğinde bu dosya değişmiyor.
///
/// Kapıdan çıkılarak varılan sonlarda ekran hemen gelmiyor: kapı açılır,
/// oyuncu kendi ayağıyla çıkar, eşikte kontrol kesilir, ekran beyazlar ve
/// birkaç saniye sonra son görünür. Özgürlük anını oyuncunun yaşaması
/// için; ekranın kapı açılır açılmaz basması o anı elinden alıyordu.
///
/// Buradaki metinler modelin repliği değil, oyunun menü yazıları.
/// Paladin'in ağzından çıkan her cümle modelden geliyor.
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

    [SerializeField] private UnityEngine.UI.Button restartButton;

    [Header("Bağlantılar")]
    [SerializeField] private PaladinNegotiation negotiation;

    [Tooltip("Sinema başlayınca kapatılacak oyuncu bileşenleri.")]
    [SerializeField] private MonoBehaviour[] disableOnCinematic;

    [Header("Zamanlama")]
    [SerializeField] private float fadeSeconds = 1.6f;
    [SerializeField] private float holdSeconds = 1.4f;

    [Tooltip("Kapıdan çıkılmayan sonlarda ekranın gelmesi için beklenen süre; son replik okunsun diye.")]
    [SerializeField] private float immediateDelay = 2.0f;

    [SerializeField] private Key restartKey = Key.R;

    private NegotiationEnding son;
    private bool gorunur;

    private void Awake()
    {
        if (negotiation == null) negotiation = FindAnyObjectByType<PaladinNegotiation>();
        if (negotiation != null) negotiation.EndingReached += SonaUlasildi;

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
        if (negotiation != null) negotiation.EndingReached -= SonaUlasildi;
        if (restartButton != null) restartButton.onClick.RemoveListener(YenidenBasla);
    }

    private void Update()
    {
        if (!gorunur || Keyboard.current == null) return;
        if (Keyboard.current[restartKey].wasPressedThisFrame) YenidenBasla();
    }

    /// <summary>
    /// Sona varıldı. Kapıdan çıkılarak varılan sonlarda ekran burada
    /// GELMİYOR; dünya hazırlanıyor ve oyuncu çıkışa yöneliyor.
    /// </summary>
    private void SonaUlasildi(NegotiationEnding gelen)
    {
        son = gelen;
        if (!son.WaitForExit) StartCoroutine(GecikmeliGoster());
    }

    /// <summary>Çıkış sineması başladığında bir kez tetiklenir.</summary>
    public event System.Action ExitSequenceStarted;

    /// <summary>Oyuncu çıkış eşiğinden geçti. ExitTrigger çağırıyor.</summary>
    public void PlayExitSequence()
    {
        if (son == null) return;

        if (ExitSequenceStarted != null) ExitSequenceStarted();
        StartCoroutine(CikisSinemasi());
    }

    private IEnumerator CikisSinemasi()
    {
        KontroluKes();

        if (whiteOverlay != null)
        {
            whiteOverlay.gameObject.SetActive(true);

            float t = 0f;
            while (t < fadeSeconds)
            {
                t += Time.deltaTime;
                whiteOverlay.color = new Color(1f, 1f, 1f, Mathf.Clamp01(t / Mathf.Max(0.01f, fadeSeconds)));
                yield return null;
            }
            whiteOverlay.color = Color.white;
        }

        yield return new WaitForSeconds(holdSeconds);
        Ac();
    }

    private IEnumerator GecikmeliGoster()
    {
        yield return new WaitForSeconds(immediateDelay);
        KontroluKes();
        Ac();
    }

    /// <summary>
    /// Sinema başlayınca kontrol kesiliyor: oyuncu beyazın içinde
    /// yürümeye devam ederse an dağılıyor.
    /// </summary>
    private void KontroluKes()
    {
        foreach (var m in disableOnCinematic) if (m != null) m.enabled = false;
    }

    private void Ac()
    {
        if (son != null)
        {
            if (titleText != null) titleText.text = son.Title;
            if (bodyText != null) bodyText.text = son.Body;
            if (hintText != null)
                hintText.text = son.RestartOnly
                    ? "[" + restartKey + "] veya düğmeyle yeniden oyna"
                    : "[" + restartKey + "] veya düğmeyle baştan oyna";
        }

        if (panelRoot != null) panelRoot.SetActive(true);
        gorunur = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void YenidenBasla()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
