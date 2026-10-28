using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Sona varıldığını oyuncuya dünyanın içinde duyuran katman.
///
/// Kapının açılması tek başına yetmiyordu: Paladin ikna olduğunda
/// ekranda hiçbir şey değişmiyor, uzaktaki bir kapı kanadı sessizce
/// dönüyor ve oyuncu serbest kaldığını ancak oraya yürürse anlıyordu.
/// Burası o boşluğu dolduruyor — önce ortada kısa bir duyuru, sonra
/// çıkışa varana kadar üstte asılı kalan bir görev satırı.
///
/// <b>Hangi sonların var olduğunu bilmiyor.</b> Başlık, gövde ve görev
/// yazısı <see cref="NegotiationEnding"/>'in kendi alanlarından geliyor;
/// metni boş bırakılan son hiçbir şey göstermiyor. Yeni bir son eklemek
/// bu dosyayı değiştirmiyor.
/// </summary>
public class EndingCue : MonoBehaviour
{
    [Header("Duyuru")]
    [Tooltip("Ekranın ortasında beliren duyuru kutusu.")]
    [SerializeField] private GameObject cueRoot;
    [SerializeField] private TextMeshProUGUI cueTitleText;
    [SerializeField] private TextMeshProUGUI cueBodyText;

    [Header("Görev Şeridi")]
    [Tooltip("Duyuru geçtikten sonra üstte asılı kalan şerit.")]
    [SerializeField] private GameObject objectiveRoot;
    [SerializeField] private TextMeshProUGUI objectiveLabel;

    [Header("Bağlantılar")]
    [SerializeField] private PaladinNegotiation negotiation;
    [SerializeField] private EndingScreen endingScreen;

    [Header("Zamanlama")]
    [Tooltip("Sona varıldıktan sonra duyurunun beklediği süre. Sohbet paneli " +
             "son repliği göstermek için açık kalıyor; duyuru onun üstüne " +
             "binmesin diye aynı gecikmeyle geliyor.")]
    [SerializeField] private float startDelay = 2.2f;

    [SerializeField] private float fadeInSeconds = 0.6f;
    [SerializeField] private float holdSeconds = 3.2f;
    [SerializeField] private float fadeOutSeconds = 0.8f;

    private CanvasGroup cueGroup;

    private void Awake()
    {
        if (negotiation == null) negotiation = FindAnyObjectByType<PaladinNegotiation>();
        if (endingScreen == null) endingScreen = FindAnyObjectByType<EndingScreen>();

        if (negotiation != null) negotiation.EndingReached += SonaUlasildi;
        if (endingScreen != null) endingScreen.ExitSequenceStarted += CikisaVarildi;

        if (cueRoot != null)
        {
            cueGroup = cueRoot.GetComponent<CanvasGroup>();
            if (cueGroup == null) cueGroup = cueRoot.AddComponent<CanvasGroup>();
            cueGroup.alpha = 0f;
            cueRoot.SetActive(false);
        }

        if (objectiveRoot != null) objectiveRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (negotiation != null) negotiation.EndingReached -= SonaUlasildi;
        if (endingScreen != null) endingScreen.ExitSequenceStarted -= CikisaVarildi;
    }

    private void SonaUlasildi(NegotiationEnding son)
    {
        if (son == null || !isActiveAndEnabled) return;

        // Önce öncekinden kalanı temizle. Normal oynanışta tek son olduğu
        // için fark etmiyor, ama ölçüm koşucusu senaryoları tek oturumda
        // art arda koşuyor ve GEÇİŞ'ten kalan görev şeridi HÜKÜM ekranının
        // üstünde asılı kalıyordu.
        if (isi != null) StopCoroutine(isi);
        if (cueRoot != null) cueRoot.SetActive(false);
        if (objectiveRoot != null) objectiveRoot.SetActive(false);
        if (cueGroup != null) cueGroup.alpha = 0f;

        isi = StartCoroutine(Oynat(son));
    }

    private Coroutine isi;

    private IEnumerator Oynat(NegotiationEnding son)
    {
        yield return new WaitForSeconds(startDelay);

        if (!string.IsNullOrEmpty(son.CueTitle) || !string.IsNullOrEmpty(son.CueBody))
        {
            if (cueTitleText != null) cueTitleText.text = son.CueTitle;
            if (cueBodyText != null) cueBodyText.text = son.CueBody;

            yield return Duyuru();
        }

        // Görev şeridi yalnızca gidilecek bir yer varken anlamlı: kapıdan
        // çıkılmayan sonlarda ekran zaten kendiliğinden geliyor.
        if (!son.WaitForExit || string.IsNullOrEmpty(son.ObjectiveText)) yield break;

        if (objectiveLabel != null) objectiveLabel.text = son.ObjectiveText;
        if (objectiveRoot != null) objectiveRoot.SetActive(true);
    }

    private IEnumerator Duyuru()
    {
        if (cueRoot == null) yield break;

        cueRoot.SetActive(true);

        yield return Solma(0f, 1f, fadeInSeconds);
        yield return new WaitForSeconds(holdSeconds);
        yield return Solma(1f, 0f, fadeOutSeconds);

        cueRoot.SetActive(false);
    }

    private IEnumerator Solma(float bas, float hedef, float sure)
    {
        if (cueGroup == null) yield break;

        if (sure <= 0f) { cueGroup.alpha = hedef; yield break; }

        float t = 0f;
        while (t < sure)
        {
            t += Time.deltaTime;
            cueGroup.alpha = Mathf.Lerp(bas, hedef, t / sure);
            yield return null;
        }
        cueGroup.alpha = hedef;
    }

    /// <summary>Oyuncu çıkıştan geçti; şeridin işi bitti.</summary>
    private void CikisaVarildi()
    {
        if (objectiveRoot != null) objectiveRoot.SetActive(false);
        if (cueRoot != null) cueRoot.SetActive(false);
    }
}
