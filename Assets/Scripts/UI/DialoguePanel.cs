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

    [Tooltip("Hangi modelin konuştuğunu gösteren etiket.")]
    [SerializeField] private TextMeshProUGUI modelLabel;

    [Header("Gizliyken Görünen Kutu")]
    [SerializeField] private GameObject statusBox;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Bağlantılar")]
    [SerializeField] private PaladinNpc paladin;
    [SerializeField] private OllamaClient client;
    [SerializeField] private PaladinNegotiation negotiation;

    [Tooltip("Panel GÖRÜNÜRKEN kapatılacak oyuncu bileşenleri. Gizlenince geri açılıyor.")]
    [SerializeField] private MonoBehaviour[] disableWhileVisible;

    [Header("Model Karşılaştırma")]
    [Tooltip("Oyun içinde geçilebilecek model ayarları. İlki varsayılan.")]
    [SerializeField] private LlmConfig[] configs;

    [Tooltip("Modeli sırayla değiştiren tuş. Tab kullanılamaz: yazı kutusunda odak değiştiriyor.")]
    [SerializeField] private Key switchModelKey = Key.F2;

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

    private readonly List<string> gecmis = new List<string>();
    private readonly StringBuilder ekran = new StringBuilder();
    private int configIndex;
    private bool acik;
    private bool gorunur;
    private bool bekleniyor;
    private bool okunmamisCevap;
    private string sonDurumYazisi = string.Empty;

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
            negotiation.BribeDemanded += RusvetIstendi;
            negotiation.OutcomeReached += SonaUlasildi;
        }

        if (configs != null && configs.Length > 0 && client != null) client.Config = configs[0];

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
            negotiation.BribeDemanded -= RusvetIstendi;
            negotiation.OutcomeReached -= SonaUlasildi;
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

        if (Keyboard.current[switchModelKey].wasPressedThisFrame) ModelDegistir();
    }

    private void KonusmaIstendi(PaladinNpc kaynak, GameObject isteyen)
    {
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

    private void PaneliGoster(bool ac)
    {
        if (panelRoot != null) panelRoot.SetActive(ac);
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

        string yazi;
        if (bekleniyor) yazi = "Paladin düşünüyor...";
        else if (okunmamisCevap) yazi = "Paladin cevap verdi";
        else if (!string.IsNullOrEmpty(sonDurumYazisi)) yazi = sonDurumYazisi;
        else yazi = "Paladin bekliyor";

        statusText.text = yazi + "\n[" + showKey + "] sohbete dön";
        statusBox.SetActive(true);
    }

    private void RusvetIstendi(int adet)
    {
        sonDurumYazisi = "Paladin " + adet + " altın istiyor.\nİskelete emir ver.";
        SatirEkle("<i>Paladin " + adet + " altın istiyor. İskeleti altınları toplamaya gönder.</i>");

        // Oyuncunun yapacak bir işi var; paneli önünden çekiyoruz.
        if (gorunur) Gizle(); else DurumKutusunuYenile();
    }

    /// <summary>
    /// Bir sona varıldı. Panel hemen kapanmıyor: Paladin'in son repliği
    /// ekranda yazılı ve oyuncunun onu okuması gerekiyor. Son ekranı
    /// zaten kendi gecikmesiyle geliyor, panel de onunla birlikte çekiliyor.
    /// </summary>
    private void SonaUlasildi(NegotiationOutcome son)
    {
        sonDurumYazisi = string.Empty;
        if (inputField != null) inputField.interactable = false;

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
            SatirEkle("<i>İstemci bağlı değil.</i>");
            return;
        }

        if (client.IsBusy) return;

        inputField.text = string.Empty;
        SatirEkle("<b>Sen:</b> " + metin);

        bekleniyor = true;
        Dusunuyor(true);

        string durum = negotiation != null ? negotiation.BuildGameState() : string.Empty;

        client.Ask(metin, GecmisMetni(), durum,
            (karar, olcum) =>
            {
                LastStats = olcum;
                bekleniyor = false;
                Dusunuyor(false);

                SatirEkle("<b>Paladin:</b> " + karar.replik);
                gecmis.Add("Oyuncu: " + metin);
                gecmis.Add("Paladin: " + karar.replik);

                if (!gorunur) okunmamisCevap = true;
                DurumKutusunuYenile();

                // Sayaçlar en sonda: sona varıldıysa replik ekranda kalsın.
                if (negotiation != null) negotiation.Apply(karar);

                OdagiVer();
            },
            (hata) =>
            {
                bekleniyor = false;
                Dusunuyor(false);
                SatirEkle("<i>" + hata + "</i>");
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
        if (thinkingIndicator != null) thinkingIndicator.SetActive(acikMi && gorunur);
        if (inputField != null) inputField.interactable = !acikMi;
    }

    private void OdagiVer()
    {
        if (inputField == null || !gorunur) return;
        inputField.ActivateInputField();
        inputField.Select();
    }

    private void ModelDegistir()
    {
        if (configs == null || configs.Length < 2 || client == null) return;
        if (client.IsBusy) return;

        configIndex = (configIndex + 1) % configs.Length;
        client.Config = configs[configIndex];
        EtiketiYenile();
        SatirEkle("<i>Model değişti: " + client.ModelName + "</i>");
        OdagiVer();
    }

    private void EtiketiYenile()
    {
        if (modelLabel == null || client == null) return;
        modelLabel.text = client.ModelName + "   [" + switchModelKey + "] model   [" + hideKey + "] gizle";
    }
}
