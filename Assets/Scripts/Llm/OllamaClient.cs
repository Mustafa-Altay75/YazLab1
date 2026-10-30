using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Yerelde çalışan Ollama sunucusuna konuşan istemci.
///
/// Çağrı eşzamansız: istek bir coroutine içinde gidiyor, oyun bu sırada
/// donmuyor. Bu önemli, çünkü 3B bir model bu makinede saniyeler sürüyor
/// ve bloklayan bir çağrı oyunu kilitlerdi.
///
/// Yanıtın biçimi tahmine bırakılmıyor: isteğe bir <b>JSON şeması</b>
/// ekleniyor ve Ollama modeli o şemaya uymaya zorluyor. Serbest metinden
/// alan ayıklamaya çalışmak 3B'de güvenilmezdi.
///
/// Model adı burada yazmıyor; her şey <see cref="LlmConfig"/> varlığından
/// geliyor. Modeli değiştirmek başka bir varlık bağlamak demek.
/// </summary>
public class OllamaClient : MonoBehaviour, ILlmClient
{
    /// <summary>
    /// Modelin uymak zorunda olduğu şema.
    ///
    /// <b>Alan sırası rastgele değil.</b> Ollama şemayı dilbilgisine
    /// çeviriyor ve model alanları buradaki sırayla üretiyor. İlk sürümde
    /// <c>niyet</c> en başta geliyordu: model etiketi hiçbir şey
    /// düşünmeden yazıyor, <c>gerekce</c> ise sonradan o etiketi
    /// savunuyordu. Şimdi önce gerekçe ve iddialar yazılıyor, etiket
    /// onların üstüne geliyor; replik de en sonda, çünkü karakterin
    /// cümlesini kurmak için neyin ne olduğunun belli olması gerekiyor.
    ///
    /// Niyet üç değerle sınırlı:
    /// beş sınıf denendi, ikisi (yalan, soru) her iki modelde de sürekli
    /// yanlış çıktı ve ikisi de gereksizdi — yalanı claims yakalıyor,
    /// soru ise oyun açısından zaten "diger".
    /// </summary>
    private const string Sema =
        "{\"type\":\"object\"," +
        "\"properties\":{" +
        "\"gerekce\":{\"type\":\"string\"}," +
        "\"claims\":{\"type\":\"array\",\"items\":{\"type\":\"string\"," +
        "\"enum\":[\"staff\",\"spellbook\",\"cell_key\",\"thieves\",\"none\"]}}," +
        "\"niyet\":{\"type\":\"string\",\"enum\":[\"persuade\",\"threat\",\"other\"]}," +
        "\"samimiyet\":{\"type\":\"integer\",\"minimum\":0,\"maximum\":100}," +
        "\"replik\":{\"type\":\"string\"}}," +
        "\"required\":[\"gerekce\",\"claims\",\"niyet\",\"samimiyet\",\"replik\"]}";

    [Tooltip("Model ve prompt ayarları. Modeli değiştirmek için başka bir varlık bağlayın.")]
    [SerializeField] private LlmConfig config;

    [Tooltip("İstek ve yanıtlar konsola yazılsın mı. Ölçüm alırken açık kalmalı.")]
    [SerializeField] private bool logCalls = true;

    [Tooltip("Modele giden ham istek ve ondan gelen ham JSON bu dosyaya eklenir. " +
             "Raporda 'bu cümleyi model üretti' iddiasının kanıtı; boşsa yazılmaz.")]
    [SerializeField] private string rawLogPath = "";

    [Tooltip("Yanıt beklenirken oyunun kare hızı buraya çekilir. -1 kapatır.\n" +
             "Model bu makinede CPU'da çalışıyor ve oyunla aynı çekirdekler için yarışıyor; " +
             "ölçüldü: 15'e çekmek 2.0 token/sn'yi 3.8'e çıkardı.")]
    [SerializeField] private int frameRateWhileThinking = 15;

    [Header("Çevrimdışı Mod")]
    [Tooltip("Açıkken model hiç denenmez, doğrudan yedek değerlendiriciye düşülür.\n" +
             "İki işi var: Ollama kurulu olmayan bir makinede oyunun tamamı " +
             "oynanabilsin, ve sunumda bağlantı canlı kesilip dayanıklılık " +
             "gösterilebilsin. Oyun içinde diyalog panelindeki tuşla da açılıp " +
             "kapanıyor; başlangıç değeri kapalı olmalı.")]
    [SerializeField] private bool forceOffline;

    [Header("Isıtma")]
    [Tooltip("Açıkken sahne başlarken modele sessiz bir çağrı yapılır. " +
             "Sebebi ölçüldü: model bellekte değilken ilk soru, yüklenme + 1937 tokenlik " +
             "sistem promptunun işlenmesi + şema kısıtlı üretim yüzünden 90 saniyelik zaman " +
             "aşımını aşıyor ve oyuncunun ilk cümlesi çevrimdışına düşüyordu. Ollama prompt " +
             "önekini önbelleğe aldığı için ısıtmadan sonraki turlar ~35 saniye.")]
    [SerializeField] private bool warmUpOnStart = true;

    [Tooltip("Isıtma isteğinin kendi zaman aşımı. Oyuncunun turundan uzun olmalı: " +
             "bu çağrının işi zaten yavaş olan ilk yüklenmeyi üstlenmek.")]
    [SerializeField] private float warmUpTimeoutSeconds = 300f;

    [Tooltip("Bozuk JSON gelince gönderilecek tek onarım isteğinin eki.")]
    [TextArea(2, 4)]
    [SerializeField] private string repairSuffix =
        "\n\nYour previous answer was not valid JSON. Answer again. " +
        "Return the JSON object only, with no text before or after it.";

    private int eskiKareHizi;
    private int eskiVSync;

    private UnityWebRequest aktifIstek;
    private Coroutine aktifCoroutine;

    public bool IsBusy { get { return aktifCoroutine != null; } }

    /// <summary>
    /// Son tur yedek değerlendiriciyle mi sonuçlandı. Arayüz buna bakıp
    /// <c>ÇEVRİMDIŞI MOD</c> etiketini gösteriyor.
    ///
    /// Her turda yeniden hesaplanıyor: Ollama sonradan açılırsa bir
    /// sonraki turda kendiliğinden siliniyor ve Paladin yeniden konuşuyor.
    /// </summary>
    public bool Offline { get; private set; }

    /// <summary>
    /// Bağlantı elle kesildi mi.
    ///
    /// <see cref="Offline"/> bir turun <i>sonucunu</i> söylüyor, bu ise
    /// bir <i>tercihi</i>: açıkken model hiç denenmiyor. Sunumda kapatıp
    /// açmak için diyalog paneli bunu değiştiriyor.
    /// </summary>
    public bool ForceOffline
    {
        get { return forceOffline; }
        set
        {
            forceOffline = value;

            // Elle geri acildiginda son turun sonucu da siliniyor ki etiket
            // hemen modele donsun. Bir sonraki tur gercegi yazacak: sunucu
            // hala kapaliysa yine cevrimdisina dusulur.
            if (!value) Offline = false;
        }
    }

    public string ModelName
    {
        get { return config != null ? (string.IsNullOrEmpty(config.displayName) ? config.model : config.displayName) : "-"; }
    }

    /// <summary>Çalışan ayar. Model değiştirici bunu değiştiriyor.</summary>
    public LlmConfig Config
    {
        get { return config; }
        set { if (!IsBusy) config = value; }
    }

    private void Awake()
    {
        // Editörde statik alanlar oyunlar arası yaşayabiliyor; anlatım
        // sırası yeni oyunda baştan başlasın.
        KeywordEvaluator.Sifirla();
    }

    /// <summary>
    /// Sahne açılışında modeli sessizce ısıtır.
    ///
    /// Oyuncuya hiçbir şey göstermiyor, oyunun durumuna dokunmuyor ve
    /// <see cref="IsBusy"/>'yi meşgul etmiyor: oyuncu ısıtma sürerken
    /// konuşmaya başlarsa isteği reddedilmesin diye kendi isteğini ayrı
    /// tutuyor. Başarısız olursa da bir şey yapmıyor; <see cref="Offline"/>
    /// işaretlenmiyor, çünkü bu bir tur değil.
    /// </summary>
    private IEnumerator Start()
    {
        if (!warmUpOnStart || forceOffline || config == null) yield break;
        yield return Isit();
    }

    private IEnumerator Isit()
    {
        // Tek token isteniyor: pahalı olan üretim değil, sistem promptunun
        // işlenmesi ve modelin belleğe alınması. İkisi de bu çağrıda oluyor.
        string govde = IstekGovdesi("merhaba", 1);

        using (var istek = new UnityWebRequest(config.endpoint, UnityWebRequest.kHttpVerbPOST))
        {
            istek.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(govde));
            istek.downloadHandler = new DownloadHandlerBuffer();
            istek.SetRequestHeader("Content-Type", "application/json");
            istek.timeout = Mathf.Max(1, Mathf.CeilToInt(warmUpTimeoutSeconds));

            float basladi = Time.realtimeSinceStartup;
            yield return istek.SendWebRequest();
            float gecen = Time.realtimeSinceStartup - basladi;

            if (!logCalls) yield break;

            if (istek.result == UnityWebRequest.Result.Success)
                Debug.Log("OllamaClient: model ısıtıldı (" + gecen.ToString("F1") + " sn).", this);
            else
                Debug.Log("OllamaClient: ısıtma başarısız, oyun etkilenmiyor: " + istek.error, this);
        }
    }

    public void Ask(string playerText, string history, string gameState,
                    System.Action<PaladinDecision, LlmCallStats> onResult,
                    System.Action<string> onError)
    {
        if (config == null)
        {
            if (onError != null) onError("Model ayarı bağlı değil.");
            return;
        }

        if (IsBusy)
        {
            if (onError != null) onError("Önceki soru hâlâ yanıtlanıyor.");
            return;
        }

        aktifCoroutine = StartCoroutine(Gonder(playerText, history, gameState, onResult, onError));
    }

    public void Cancel()
    {
        if (!IsBusy) return;

        if (aktifIstek != null) aktifIstek.Abort();
        if (aktifCoroutine != null) StopCoroutine(aktifCoroutine);
        aktifCoroutine = null;
        aktifIstek = null;

        // Coroutine yarıda kesildiği için kare hızını burada geri vermek
        // zorundayız; yoksa oyun 15 karede takılı kalırdı.
        CpuyuModeleBirak(false);
    }

    /// <summary>Tek bir HTTP çağrısının sonucu. Merdivenin basamakları bunu paylaşıyor.</summary>
    private class IstekSonucu
    {
        /// <summary>Sunucuya hiç ulaşılamadı, zaman aşıldı ya da HTTP hatası döndü.</summary>
        public bool agHatasi;

        /// <summary>Oyuncuya gösterilebilecek tek cümlelik sebep.</summary>
        public string sebep;

        /// <summary>Başarılıysa ham gövde.</summary>
        public string ham;

        public float saniye;
    }

    /// <summary>
    /// Modele giden tek çağrı. Hatayı yorumlamıyor, yalnızca raporluyor;
    /// ne yapılacağına <see cref="Gonder"/> karar veriyor.
    /// </summary>
    private IEnumerator Istek(string govde, System.Action<IstekSonucu> bitti)
    {
        var sonuc = new IstekSonucu();

        var istek = new UnityWebRequest(config.endpoint, UnityWebRequest.kHttpVerbPOST);
        istek.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(govde));
        istek.downloadHandler = new DownloadHandlerBuffer();
        istek.SetRequestHeader("Content-Type", "application/json");
        istek.timeout = Mathf.Max(1, Mathf.CeilToInt(config.timeoutSeconds));
        aktifIstek = istek;

        float basladi = Time.realtimeSinceStartup;
        yield return istek.SendWebRequest();
        sonuc.saniye = Time.realtimeSinceStartup - basladi;

        aktifIstek = null;

        if (istek.result != UnityWebRequest.Result.Success)
        {
            sonuc.agHatasi = true;
            sonuc.sebep = HataSebebi(istek);
            if (logCalls)
                Debug.LogWarning("OllamaClient: " + sonuc.sebep +
                                 " (" + sonuc.saniye.ToString("F1") + " sn)", this);
            istek.Dispose();
            bitti(sonuc);
            yield break;
        }

        sonuc.ham = istek.downloadHandler.text;
        istek.Dispose();
        bitti(sonuc);
    }

    /// <summary>
    /// Üç hata türü ayrı ayrı adlandırılıyor; şartname bunu istiyor ve
    /// yanlış teşhis, teşhis koymamaktan beterdir. Unity zaman aşımını da
    /// ConnectionError diye raporladığı için ayrımı metinden yapıyoruz:
    /// ilk sürüm, sunucu gayet çalışırken "sunucu çalışıyor mu" diyordu.
    /// </summary>
    private string HataSebebi(UnityWebRequest istek)
    {
        bool zamanAsimi = !string.IsNullOrEmpty(istek.error) &&
                          istek.error.ToLower().Contains("timeout");

        if (zamanAsimi)
            return "Paladin düşünmesini " + config.timeoutSeconds.ToString("F0") +
                   " saniyede bitiremedi.";

        if (istek.responseCode == 404)
            return "Model sunucuda yok: " + config.model + " çekilmemiş.";

        if (istek.result == UnityWebRequest.Result.ConnectionError)
            return "Ollama sunucusuna ulaşılamadı (" + istek.error + ").";

        return "Model yanıt vermedi: " + istek.error;
    }

    /// <summary>
    /// Ham gövdeden geçerli bir karar çıkarmayı dener. Çıkaramazsa null.
    /// </summary>
    private PaladinDecision KararCikar(string ham, string playerText)
    {
        OllamaYanit dis = null;
        try { dis = JsonUtility.FromJson<OllamaYanit>(ham); }
        catch (System.Exception e) { Debug.LogWarning("OllamaClient: dış JSON okunamadı: " + e.Message, this); }

        if (dis == null || string.IsNullOrEmpty(dis.response)) return null;

        PaladinDecision karar = null;
        try { karar = JsonUtility.FromJson<PaladinDecision>(dis.response); }
        catch (System.Exception e) { Debug.LogWarning("OllamaClient: şema JSON'u okunamadı: " + e.Message, this); }

        // Çeviri doğrulamadan ÖNCE: Gecerli() oyunun kendi değerlerini
        // bekliyor, modelden İngilizce etiket gelirse hepsi geçersiz sayılır.
        if (karar != null) karar.EtiketleriCevir();

        if (karar == null || !karar.Gecerli())
        {
            if (logCalls) Debug.LogWarning("OllamaClient: şemaya uymayan yanıt: " + dis.response, this);
            return null;
        }

        // Çıktıyı kendi girdisine karşı denetle: oyuncunun hiç anmadığı
        // eşyayı iddia olarak saymayalım.
        int dusen = karar.IddialariDogrula(playerText);
        if (dusen > 0 && logCalls)
            Debug.Log("OllamaClient: oyuncunun cumlesinde gecmeyen " + dusen + " iddia dusuruldu.", this);

        return karar;
    }

    /// <summary>
    /// Modelden geçerli bir yanıt alınamadı: kararı yedek değerlendirici
    /// veriyor. <b>Hata değil, bir sonuç</b> — bu yüzden onError değil
    /// onResult çağrılıyor ve oyun kaldığı yerden sürüyor.
    /// </summary>
    private void Yedege(string playerText, string sebep,
                        System.Action<PaladinDecision, LlmCallStats> onResult)
    {
        Offline = true;

        var karar = KeywordEvaluator.Degerlendir(playerText);
        var olcum = new LlmCallStats { seconds = 0f, tokens = 0, model = "çevrimdışı" };

        if (logCalls) Debug.LogWarning("OllamaClient: ÇEVRİMDIŞI MOD — " + sebep, this);
        if (onResult != null) onResult(karar, olcum);
    }

    /// <summary>
    /// Üç basamaklı merdiven: şema kısıtlı istek, sonra tek bir onarım
    /// isteği, sonra yedek değerlendirici. Her tur en baştan başlıyor,
    /// yani Ollama sonradan açılırsa Paladin kendiliğinden yeniden
    /// konuşuyor.
    /// </summary>
    private IEnumerator Gonder(string playerText, string history, string gameState,
                               System.Action<PaladinDecision, LlmCallStats> onResult,
                               System.Action<string> onError)
    {
        if (forceOffline)
        {
            aktifCoroutine = null;
            Yedege(playerText, "forceOffline açık (hata ayıklama).", onResult);
            yield break;
        }

        string prompt = PromptKur(playerText, history, gameState);
        string govde = IstekGovdesi(prompt);

        CpuyuModeleBirak(true);

        // 1. basamak: şema kısıtlı istek.
        IstekSonucu s1 = null;
        yield return Istek(govde, r => s1 = r);

        PaladinDecision karar = null;
        float gecen = s1.saniye;
        int token = 0;

        if (!s1.agHatasi)
        {
            HamKaydet(govde, s1.ham);
            karar = KararCikar(s1.ham, playerText);
            token = TokenSayisi(s1.ham);
        }

        // 2. basamak: gövde geldi ama şemaya uymuyor. Tek bir onarım isteği.
        if (karar == null && !s1.agHatasi)
        {
            if (logCalls) Debug.LogWarning("OllamaClient: yanıt şemaya uymadı, onarım isteniyor.", this);

            IstekSonucu s2 = null;
            yield return Istek(IstekGovdesi(prompt + repairSuffix), r => s2 = r);

            gecen += s2.saniye;
            if (!s2.agHatasi)
            {
                HamKaydet("--- ONARIM ISTEGI ---", s2.ham);
                karar = KararCikar(s2.ham, playerText);
                token = TokenSayisi(s2.ham);
            }
        }

        CpuyuModeleBirak(false);
        aktifCoroutine = null;

        // 3. basamak: hâlâ yoksa yedek değerlendirici.
        if (karar == null)
        {
            Yedege(playerText, s1.agHatasi ? s1.sebep : "Model şemaya uymayan yanıt verdi.", onResult);
            yield break;
        }

        Offline = false;

        var olcum = new LlmCallStats { seconds = gecen, tokens = token, model = config.model };
        if (logCalls) Debug.Log("OllamaClient: " + olcum + "  ->  " + karar, this);

        if (onResult != null) onResult(karar, olcum);
    }

    /// <summary>Ham gövdeden token sayısını okur; okunamazsa sıfır.</summary>
    private static int TokenSayisi(string ham)
    {
        try
        {
            var dis = JsonUtility.FromJson<OllamaYanit>(ham);
            return dis != null ? dis.eval_count : 0;
        }
        catch (System.Exception)
        {
            return 0;
        }
    }

    /// <summary>
    /// Beklerken oyunun kare hızını kısar, sonra geri verir.
    ///
    /// Model bu makinede CPU'da çalışıyor ve oyunla aynı çekirdekler için
    /// yarışıyor. Aynı cümle, aynı model: serbest kare hızında 2.0
    /// token/sn, 15'e çekilince 3.8. Oyuncu zaten "düşünüyor" yazısına
    /// bakıyor, o sırada 60 kare çizmenin bir faydası yok.
    /// </summary>
    private void CpuyuModeleBirak(bool birak)
    {
        if (frameRateWhileThinking <= 0) return;

        if (birak)
        {
            eskiKareHizi = Application.targetFrameRate;
            eskiVSync = QualitySettings.vSyncCount;
            // targetFrameRate yalnızca vSync kapalıyken işliyor.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = frameRateWhileThinking;
        }
        else
        {
            Application.targetFrameRate = eskiKareHizi;
            QualitySettings.vSyncCount = eskiVSync;
        }
    }

    /// <summary>
    /// Modele giden metin.
    ///
    /// Geçmişin nasıl etiketlendiği önemli çıktı: ilk sürüm sadece
    /// "Önceki konuşma" yazıyordu ve model bunu devam ettirilecek bir
    /// metin sanıp kendi önceki repliğini kelimesi kelimesine tekrar
    /// ediyordu. Artık geçmişin yalnızca bağlam olduğu ve cevabın sadece
    /// son söze verileceği açıkça yazılıyor.
    /// </summary>
    private static string PromptKur(string playerText, string history, string gameState)
    {
        var sb = new StringBuilder();

        // Oyun durumu en başta duruyor: model önce neyin doğru olduğunu
        // okusun, sonra konuşmayı. Sıralamanın etkisi ölçülmedi; tersini
        // denemek gerekirse buradan tek satırla değişiyor.
        if (!string.IsNullOrEmpty(gameState))
        {
            sb.AppendLine(gameState);
            sb.AppendLine();
        }

        if (!string.IsNullOrEmpty(history))
        {
            sb.AppendLine("Earlier in this conversation. This is context only, so that you remember it.");
            sb.AppendLine("Do not repeat these lines and do not answer them again:");
            sb.AppendLine(history);
            sb.AppendLine();
        }

        sb.AppendLine("The player's newest line. Answer only this one:");
        sb.AppendLine(playerText);
        return sb.ToString();
    }

    private string IstekGovdesi(string prompt)
    {
        return IstekGovdesi(prompt, config.numPredict);
    }

    private string IstekGovdesi(string prompt, int tokenSiniri)
    {
        var sb = new StringBuilder();
        sb.Append("{");
        sb.Append("\"model\":\"").Append(JsonKacis(config.model)).Append("\",");
        sb.Append("\"prompt\":\"").Append(JsonKacis(prompt)).Append("\",");
        sb.Append("\"system\":\"").Append(JsonKacis(config.systemPrompt)).Append("\",");
        sb.Append("\"stream\":false,");
        if (!string.IsNullOrEmpty(config.keepAlive))
            sb.Append("\"keep_alive\":\"").Append(JsonKacis(config.keepAlive)).Append("\",");
        sb.Append("\"format\":").Append(Sema).Append(",");
        sb.Append("\"options\":{\"temperature\":")
          .Append(config.temperature.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture))
          .Append(",\"num_predict\":").Append(tokenSiniri).Append("}");
        sb.Append("}");
        return sb.ToString();
    }

    /// <summary>
    /// JSON kaçışı. Elle yazmanın sebebi, gövdeye şemayı ham JSON olarak
    /// gömmek: hazır bir serileştirici bunu metin sanıp tırnaklardı.
    /// </summary>
    private static string JsonKacis(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;

        var sb = new StringBuilder(s.Length + 16);
        foreach (char c in s)
        {
            switch (c)
            {
                case '\"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 32) sb.Append("\\u").Append(((int)c).ToString("x4"));
                    else sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Giden isteği ve gelen ham yanıtı dosyaya ekler.
    ///
    /// Oyunun ürettiği repliklerin gerçekten modelden geldiğini göstermenin
    /// en doğrudan yolu bu: dosyadaki ham JSON'un replik alanı, ekranda
    /// görünen cümlenin aynısı oluyor.
    /// </summary>
    private void HamKaydet(string istekGovdesi, string hamYanit)
    {
        if (string.IsNullOrEmpty(rawLogPath)) return;

        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("==== " + System.DateTime.Now.ToString("HH:mm:ss") + "  model=" + config.model + " ====");
            sb.AppendLine("--- GIDEN ISTEK ---");
            sb.AppendLine(istekGovdesi);
            sb.AppendLine("--- GELEN HAM YANIT ---");
            sb.AppendLine(hamYanit);
            sb.AppendLine();
            System.IO.File.AppendAllText(rawLogPath, sb.ToString(), System.Text.Encoding.UTF8);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("OllamaClient: ham kayit yazilamadi: " + e.Message, this);
        }
    }

    /// <summary>Ollama'nın dış zarfı. İlgilenmediğimiz alanlar yok sayılır.</summary>
    [System.Serializable]
    private class OllamaYanit
    {
        public string response;
        public int eval_count;
        public bool done;
    }
}
