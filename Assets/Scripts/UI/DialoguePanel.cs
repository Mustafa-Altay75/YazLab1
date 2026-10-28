using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Paladin'le konuşma paneli.
///
/// PaladinNpc konuşmayı başlatmıyor, sadece istendiğini duyuruyor; bu
/// panel o olaya abone oluyor. Böylece NPC'de tek satır hazır replik
/// durmuyor — repliklerin tamamı modelden geliyor.
///
/// <b>Üç durum var, ikisi değil:</b> kapalı, açık-görünür, açık-gizli.
/// Gizli durum, konuşma sürerken oyuna geri dönebilmek için: oyuncu
/// etrafı görür, yürür, iskelete emir verir; konuşma geçmişi durur.
/// Rüşvet görevi bu yüzden var — Paladin altın isteyince panel
/// kendiliğinden gizleniyor ve oyuncunun yapacak bir işi oluyor.
///
/// Gizliyken köşedeki küçük kutu durumu söylüyor: cevap bekleniyor mu,
/// cevap geldi mi, Paladin ne istiyor.
/// </summary>
public class DialoguePanel : MonoBehaviour
{
    [Header("Arayüz")]
    [Tooltip("Açılıp kapanan kök obje.")]
    [SerializeField] private GameObject panelRoot;

    [Tooltip("Konuşma geçmişinin yazıldığı metin.")]
    [SerializeField] private TextMeshProUGUI historyText;

    [Tooltip("Geçmişi taşıyan kaydırma alanı. Yeni satırda en alta iniyor.")]
    [SerializeField] private UnityEngine.UI.ScrollRect historyScroll;

    [Tooltip("Oyuncunun yazdığı kutu.")]
    [SerializeField] private TMP_InputField inputField;

    [Tooltip("Yanıt beklenirken görünen yazı.")]
    [SerializeField] private GameObject thinkingIndicator;

    [Tooltip("Solda duran durum etiketi: modelin adı ya da ÇEVRİMDIŞI MOD.")]
    [SerializeField] private TextMeshProUGUI modelLabel;

    [Tooltip("Sağda duran tuş ipuçları. Durum etiketinden ayrı tutuluyor: " +
             "ikisi tek yazıyken ÇEVRİMDIŞI MOD uzayıp ortadaki başlığın " +
             "üstüne biniyordu.")]
    [SerializeField] private TextMeshProUGUI keyHintLabel;

    [Header("Gizliyken Görünen Kutu")]
    [SerializeField] private GameObject statusBox;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Bağlantılar")]
    [SerializeField] private PaladinNpc paladin;
    [SerializeField] private OllamaClient client;
    [SerializeField] private PaladinNegotiation negotiation;

    [Tooltip("Panel GÖRÜNÜRKEN kapatılacak oyuncu bileşenleri. Gizlenince geri açılıyor.")]
    [SerializeField] private MonoBehaviour[] disableWhileVisible;

    [Header("Bağlantı")]
    [Tooltip("Ollama bağlantısını kesip açan tuş. Sunumda dayanıklılığı canlı " +
             "göstermek için: basınca model devre dışı kalıyor, oyun çevrimdışı " +
             "modla sürüyor, tekrar basınca Paladin geri dönüyor.\n" +
             "Harf tuşu olamaz: bu tuş panel AÇIKKEN, yani oyuncu yazarken " +
             "okunuyor; cümlenin içindeki her harf bağlantıyı kesip açardı. " +
             "Tab da olmaz, yazı kutusunda odak değiştiriyor. Insert denendi " +
             "ve bırakıldı: bu makinede sayı tuşuyla paylaşımlı. Dizüstünde " +
             "F sırası medya tuşuysa Fn+F2 ile basılıyor.")]
    [SerializeField] private Key toggleConnectionKey = Key.F2;

    [Header("Davranış")]
    [Tooltip("Modele bağlam olarak verilecek en fazla kaç tur.")]
    [SerializeField] private int historyTurns = 2;

    [Tooltip("Sohbeti geri getiren tuş. YALNIZCA panel gizliyken okunur.\n" +
             "Harf tuşu olabilmesinin tek sebebi bu: panel görünürken klavye " +
             "yazı kutusunun, o sırada bu tuşa hiç bakılmıyor. Yoksa " +
             "cümlenin içindeki her 'q' paneli gizlerdi.")]
    [SerializeField] private Key showKey = Key.Q;

    [Tooltip("Paneli gizleyen tuş. Panel görünürken okunur; konuşma kapanmaz, " +
             "oyuncu oyuna döner ve geçmiş yerinde kalır.")]
    [SerializeField] private Key hideKey = Key.Escape;

    [Tooltip("Bir sona varıldıktan sonra panel bu kadar açık kalır (saniye). " +
             "Son ekranının gecikmesiyle aynı olmalı ki son replik okunabilsin.")]
    [SerializeField] private float closeDelayOnEnding = 2.0f;

    [Header("Görünüm")]
    [Tooltip("Panel bu sürede belirip kaybolur (saniye). 0 = anında.")]
    [SerializeField] private float fadeSeconds = 0.18f;

    [Tooltip("Bekleme yazısının noktaları bu aralıkla değişir (saniye).")]
    [SerializeField] private float thinkingTickSeconds = 0.4f;

    [Tooltip("Bekleme yazısının gövdesi; sonuna nokta ekleniyor.")]
    [SerializeField] private string thinkingLabel = "Paladin düşünüyor";

    private CanvasGroup panelGroup;
    private Coroutine solmaIsi;
    private Coroutine dusunmeIsi;

    private readonly List<string> gecmis = new List<string>();
    private readonly StringBuilder ekran = new StringBuilder();
    private bool acik;
    private bool gorunur;
    private bool bekleniyor;
    private bool okunmamisCevap;
    private string sonDurumYazisi = string.Empty;
    private bool sonBagKopuk;

    /// <summary>Son çağrının ölçümü. Rapor için okunabilsin diye dışarı açık.</summary>
    public LlmCallStats LastStats { get; private set; }

    /// <summary>Panel şu an ekranda mı. Güven çubuğu buna bakıyor.</summary>
    public bool IsVisible { get { return acik && gorunur; } }

    private void Awake()
    {
        if (paladin == null) paladin = FindAnyObjectByType<PaladinNpc>();
        if (client == null) client = FindAnyObjectByType<OllamaClient>();
        if (negotiation == null) negotiation = FindAnyObjectByType<PaladinNegotiation>();

        if (paladin != null) paladin.DialogueRequested += KonusmaIstendi;
        if (inputField != null) inputField.onSubmit.AddListener(Gonder);
        if (negotiation != null)
        {
            negotiation.EndingReached += SonaUlasildi;
        }

        acik = false;
        PaneliGoster(false);
        if (statusBox != null) statusBox.SetActive(false);
    }

    private void OnDestroy()
    {
        if (paladin != null) paladin.DialogueRequested -= KonusmaIstendi;
        if (inputField != null) inputField.onSubmit.RemoveListener(Gonder);
        if (negotiation != null)
        {
            negotiation.EndingReached -= SonaUlasildi;
        }
    }

    private void Update()
    {
        if (!acik || Keyboard.current == null) return;

        if (!gorunur)
        {
            // Gizliyken klavye oyunun: harf tuşu okunabilir.
            if (Keyboard.current[showKey].wasPressedThisFrame) Goster();
            return;
        }

        if (Keyboard.current[hideKey].wasPressedThisFrame)
        {
            Gizle();
            return;
        }

        if (Keyboard.current[toggleConnectionKey].wasPressedThisFrame) BaglantiyiDegistir();
    }

    private void KonusmaIstendi(PaladinNpc kaynak, GameObject isteyen)
    {
        // Pazarlık bittiyse konuşacak bir şey kalmadı. Yoksa oyuncu
        // Paladin'e tekrar [E] basıp bitmiş bir sohbeti açıyor ve köşedeki
        // kutu yeniden "seni bekliyor" demeye başlıyordu.
        if (negotiation != null && negotiation.IsOver)
        {
            if (kaynak != null) kaynak.CloseDialogue();
            return;
        }

        if (negotiation != null) negotiation.MarkStarted();

        acik = true;
        Goster();
    }

    // --- Görünürlük ---

    private void Goster()
    {
        gorunur = true;
        okunmamisCevap = false;

        PaneliGoster(true);
        if (statusBox != null) statusBox.SetActive(false);

        InputLock.Capture();
        foreach (var m in disableWhileVisible) if (m != null) m.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        EtiketiYenile();
        Dusunuyor(bekleniyor);
        OdagiVer();
    }

    /// <summary>
    /// Paneli gizler ama konuşmayı bitirmez: oyuncu oyuna döner, geçmiş
    /// durur, bekleyen istek iptal edilmez.
    /// </summary>
    private void Gizle()
    {
        gorunur = false;

        PaneliGoster(false);
        InputLock.Release();
        foreach (var m in disableWhileVisible) if (m != null) m.enabled = true;

        // NPC'yi serbest bırak: oyuncu sohbete hem Q ile hem de Paladin'in
        // yanına gidip [E] ile dönebilsin. İkisi de aynı paneli açıyor.
        if (paladin != null) paladin.CloseDialogue();

        DurumKutusunuYenile();
        ImleciKareSonundaKilitle();
    }

    private void Kapat()
    {
        acik = false;
        gorunur = false;

        if (client != null) client.Cancel();
        bekleniyor = false;
        Dusunuyor(false);

        PaneliGoster(false);
        if (statusBox != null) statusBox.SetActive(false);
        if (paladin != null) paladin.CloseDialogue();

        InputLock.Release();
        foreach (var m in disableWhileVisible) if (m != null) m.enabled = true;

        ImleciKareSonundaKilitle();
    }

    /// <summary>
    /// Paneli açar/kapatır. Sert açılıp kapanmak yerine kısa bir
    /// solmayla: konuşma paneli oyun sırasında sık sık gizlenip geri
    /// geliyor ve anlık sıçrama her seferinde göze batıyordu.
    /// </summary>
    private void PaneliGoster(bool ac)
    {
        if (panelRoot == null) return;

        if (panelGroup == null)
        {
            panelGroup = panelRoot.GetComponent<CanvasGroup>();
            if (panelGroup == null) panelGroup = panelRoot.AddComponent<CanvasGroup>();
        }

        if (ac) panelRoot.SetActive(true);

        if (fadeSeconds <= 0f || !isActiveAndEnabled)
        {
            panelGroup.alpha = ac ? 1f : 0f;
            if (!ac) panelRoot.SetActive(false);
            return;
        }

        if (solmaIsi != null) StopCoroutine(solmaIsi);
        solmaIsi = StartCoroutine(Solma(ac));
    }

    private IEnumerator Solma(bool ac)
    {
        float bas = panelGroup.alpha;
        float hedef = ac ? 1f : 0f;
        float t = 0f;

        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            panelGroup.alpha = Mathf.Lerp(bas, hedef, t / fadeSeconds);
            yield return null;
        }

        panelGroup.alpha = hedef;
        if (!ac) panelRoot.SetActive(false);
        solmaIsi = null;
    }

    private void ImleciKareSonundaKilitle()
    {
        // İmleci hemen kilitlemiyoruz: ThirdPersonCamera da Escape'i görüyor
        // ve LateUpdate'inde imleci ters çeviriyor. Kare sonunda yazınca
        // son söz bizde oluyor.
        if (isActiveAndEnabled) StartCoroutine(ImleciKilitle());
    }

    private IEnumerator ImleciKilitle()
    {
        yield return new WaitForEndOfFrame();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // --- Durum kutusu ---

    private void DurumKutusunuYenile()
    {
        if (statusBox == null || statusText == null) return;

        if (!acik || gorunur) { statusBox.SetActive(false); return; }

        // Bitmiş pazarlıkta kutu susar; oyuncuya ne söyleyecek bir şeyi
        // var ne de dönecek bir sohbet.
        if (negotiation != null && negotiation.IsOver) { statusBox.SetActive(false); return; }

        string yazi;
        if (client != null && client.Offline && !bekleniyor) yazi = "Bağ koptu — ÇEVRİMDIŞI MOD";
        else if (bekleniyor) yazi = "Paladin düşünüyor...";
        else if (okunmamisCevap) yazi = "Paladin cevap verdi";
        else if (!string.IsNullOrEmpty(sonDurumYazisi)) yazi = sonDurumYazisi;
        else yazi = "Paladin bekliyor";

        statusText.text = yazi + "\n[" + showKey + "] sohbete dön";
        statusBox.SetActive(true);
    }


    /// <summary>
    /// Bir sona varıldı. Panel hemen kapanmıyor: Paladin'in son repliği
    /// ekranda yazılı ve oyuncunun onu okuması gerekiyor. Son ekranı
    /// zaten kendi gecikmesiyle geliyor, panel de onunla birlikte çekiliyor.
    /// </summary>
    private void SonaUlasildi(NegotiationEnding son)
    {
        sonDurumYazisi = string.Empty;
        if (inputField != null) inputField.interactable = false;

        // Konuşmanın nasıl bittiğini sohbetin kendisi söylesin. Panel
        // kapanmadan önce ekranda duran tek yer burası; çubuğun ucuna
        // dayanmasından başka ibare yoktu.
        if (son != null && !string.IsNullOrEmpty(son.ChatLine)) SistemSatiri(son.ChatLine);

        if (statusBox != null) statusBox.SetActive(false);

        if (acik && isActiveAndEnabled) StartCoroutine(SondanSonraKapat());
    }

    private IEnumerator SondanSonraKapat()
    {
        yield return new WaitForSeconds(closeDelayOnEnding);
        if (acik) Kapat();
    }

    // --- Konuşma ---

    /// <summary>TMP_InputField'in onSubmit'i buraya bağlı (Enter).</summary>
    public void Gonder(string metin)
    {
        if (!acik) return;

        metin = metin != null ? metin.Trim() : string.Empty;
        if (metin.Length == 0) { OdagiVer(); return; }

        if (client == null)
        {
            SistemSatiri("İstemci bağlı değil.");
            return;
        }

        if (client.IsBusy) return;

        inputField.text = string.Empty;
        OyuncuSatiri(metin);

        bekleniyor = true;
        Dusunuyor(true);

        string durum = negotiation != null ? negotiation.BuildGameState() : string.Empty;

        client.Ask(metin, GecmisMetni(), durum,
            (karar, olcum) =>
            {
                LastStats = olcum;
                bekleniyor = false;
                Dusunuyor(false);

                if (karar.cevrimdisi) AnlatimSatiri(karar.replik);
                else PaladinSatiri(karar.replik);

                BagDurumunuBildir();

                gecmis.Add("Oyuncu: " + metin);
                gecmis.Add("Paladin: " + karar.replik);

                if (!gorunur) okunmamisCevap = true;
                DurumKutusunuYenile();

                // Sayaçlar en sonda: sona varıldıysa replik ekranda kalsın.
                if (negotiation != null) negotiation.Apply(karar);

                // Döküm, güven güncellendikten sonra kaydediliyor; önce
                // çağrılsaydı turun güven değişimi bir tur geriden gelirdi.
                DialogueTranscript.Kaydet(metin, karar, olcum);

                OdagiVer();
            },
            (hata) =>
            {
                bekleniyor = false;
                Dusunuyor(false);
                SistemSatiri(hata);
                if (!gorunur) okunmamisCevap = true;
                DurumKutusunuYenile();
                OdagiVer();
            });

        DurumKutusunuYenile();
    }

    /// <summary>Modele verilecek bağlam: son birkaç tur, hepsi değil.</summary>
    private string GecmisMetni()
    {
        int satir = historyTurns * 2;
        int bas = Mathf.Max(0, gecmis.Count - satir);

        var sb = new StringBuilder();
        for (int i = bas; i < gecmis.Count; i++) sb.AppendLine(gecmis[i]);
        return sb.ToString();
    }

    /// <summary>
    /// Oyuncunun sözü: sağa yaslı ve sönük.
    ///
    /// İki ses de aynı hizada ve aynı parlaklıktaydı; göz konuşmayı
    /// takip edemiyordu. Hizayı ayırmak, rengi ayırmaktan çok daha
    /// hızlı okunuyor.
    /// </summary>
    private void OyuncuSatiri(string metin)
    {
        SatirEkle("<align=right><color=#8E8778><size=85%>SEN</size></color>\n<color=#B9B2A1>" +
                  metin + "</color></align>");
    }

    /// <summary>Paladin'in sözü: sola yaslı, parlak, adı tema renginde.</summary>
    private void PaladinSatiri(string metin)
    {
        SatirEkle("<color=#D9CBA0><size=85%><b>PALADİN</b></size></color>\n<color=#F0E8D6>" +
                  metin + "</color>");
    }

    /// <summary>
    /// Çevrimdışı turun satırı: Paladin'in ağzından değil, oyunun ağzından.
    ///
    /// Model yokken Paladin'e hazır replik söyletmek, LLM çıktısının
    /// taklidi diye okunabilirdi (NOT-5). Üçüncü şahıs, tırnaksız bir
    /// sahne tarifi ise hazır olduğunu kendisi söylüyor; üstelik ekranda
    /// ÇEVRİMDIŞI MOD yazıyor.
    /// </summary>
    private void AnlatimSatiri(string metin)
    {
        SatirEkle("<align=center><color=#8A8578><size=90%><i>" + metin + "</i></size></color></align>");
    }

    /// <summary>
    /// Bağlantı durumu değiştiyse sohbete bir kez not düşer. Her turda
    /// tekrarlamak sohbeti boğardı; yalnızca geçişler yazılıyor.
    /// </summary>
    private void BagDurumunuBildir()
    {
        if (client == null) return;
        if (client.Offline == sonBagKopuk) return;

        sonBagKopuk = client.Offline;
        SistemSatiri(sonBagKopuk
            ? "Paladin'le bağın zayıfladı. Sözlerini duyuyor ama karşılık veremiyor."
            : "Bağ yeniden kuruldu. Paladin yeniden konuşuyor.");

        EtiketiYenile();
    }

    /// <summary>Oyunun kendi notu: ortada, soluk, italik.</summary>
    private void SistemSatiri(string metin)
    {
        SatirEkle("<align=center><color=#7E786C><size=85%><i>" + metin + "</i></size></color></align>");
    }

    private void SatirEkle(string satir)
    {
        if (ekran.Length > 0) ekran.AppendLine();
        ekran.AppendLine(satir);
        if (historyText != null) historyText.text = ekran.ToString();

        if (isActiveAndEnabled) StartCoroutine(EnAltaKaydir());
    }

    /// <summary>
    /// Yeni satır eklenince en alta iner. Bir kare beklemek şart:
    /// ContentSizeFitter metnin yeni yüksekliğini bu karede hesaplamıyor,
    /// hemen kaydırırsak eski yüksekliğe göre kayıp yarıda kalıyor.
    /// </summary>
    private IEnumerator EnAltaKaydir()
    {
        if (historyScroll == null) yield break;

        yield return null;
        Canvas.ForceUpdateCanvases();
        historyScroll.verticalNormalizedPosition = 0f;
    }

    private void Dusunuyor(bool acikMi)
    {
        bool goster = acikMi && gorunur;

        if (thinkingIndicator != null) thinkingIndicator.SetActive(goster);
        if (inputField != null) inputField.interactable = !acikMi;

        if (dusunmeIsi != null) { StopCoroutine(dusunmeIsi); dusunmeIsi = null; }
        if (goster && isActiveAndEnabled) dusunmeIsi = StartCoroutine(NoktalariOynat());
    }

    /// <summary>
    /// Bekleme yazısının noktalarını döndürür.
    ///
    /// Yanıt yirmi saniye sürüyor ve sabit bir yazı o süre boyunca
    /// "oyun dondu mu" hissi veriyordu. Kıpırdayan üç nokta, beklemenin
    /// sürdüğünü söylemenin en ucuz yolu.
    /// </summary>
    private IEnumerator NoktalariOynat()
    {
        var yazi = thinkingIndicator != null ? thinkingIndicator.GetComponent<TextMeshProUGUI>() : null;
        int n = 0;

        while (true)
        {
            if (yazi != null) yazi.text = thinkingLabel + new string('.', 1 + (n % 3));
            n++;
            yield return new WaitForSecondsRealtime(thinkingTickSeconds);
        }
    }

    private void OdagiVer()
    {
        if (inputField == null || !gorunur) return;
        inputField.ActivateInputField();
        inputField.Select();
    }

    /// <summary>
    /// Ollama bağlantısını keser ya da geri açar.
    ///
    /// Eskiden bu tuş modeli değiştiriyordu. Model karşılaştırması
    /// ölçülüp rapora yazıldıktan sonra oyun içinde geçiş yapmanın bir
    /// değeri kalmadı; bağlantıyı canlı kesebilmek ise sunumda hata
    /// yönetimini anlatmanın en kısa yolu.
    ///
    /// Bir sonraki tura etki ediyor, o anki isteğe değil: beklemede olan
    /// bir çağrı varken tuş çalışmıyor.
    /// </summary>
    private void BaglantiyiDegistir()
    {
        if (client == null || client.IsBusy) return;

        client.ForceOffline = !client.ForceOffline;

        // Otomatik bildirimle çakışmasın: bir sonraki tur aynı durumu
        // ikinci kez duyurmasın diye işaret burada da güncelleniyor.
        sonBagKopuk = client.ForceOffline;

        SistemSatiri(client.ForceOffline
            ? "Ollama bağlantısı kesildi. Paladin sözlerini duyuyor ama karşılık veremiyor."
            : "Ollama bağlantısı yeniden açıldı. Paladin bir sonraki sözünde konuşacak.");

        EtiketiYenile();
        OdagiVer();
    }

    private void EtiketiYenile()
    {
        if (modelLabel == null || client == null) return;
        // ForceOffline bir tercih, Offline bir sonuç: ikisinden biri
        // yeterli. Tuşa basıldığı anda etiketin değişmesi için tercihe de
        // bakılıyor, yoksa oyuncu bir tur boyunca eski etiketi görürdü.
        bool kopuk = client.Offline || client.ForceOffline;
        modelLabel.text = kopuk ? "<color=#C98D74>ÇEVRİMDIŞI MOD</color>" : client.ModelName;

        // Tuşlar alanlardan okunuyor, sahneye elle yazılmıyor: tuş
        // değişirse ipucu da kendiliğinden değişsin.
        if (keyHintLabel != null)
            keyHintLabel.text = "[" + toggleConnectionKey + "] bağlantı   [" + hideKey + "] gizle";
    }
}
