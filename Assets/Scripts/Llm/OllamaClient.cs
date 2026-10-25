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
        "\"enum\":[\"staff\",\"spellbook\",\"cell_key\",\"gold\",\"none\"]}}," +
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

    private int eskiKareHizi;
    private int eskiVSync;

    private UnityWebRequest aktifIstek;
    private Coroutine aktifCoroutine;

    public bool IsBusy { get { return aktifCoroutine != null; } }

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

    private IEnumerator Gonder(string playerText, string history, string gameState,
                               System.Action<PaladinDecision, LlmCallStats> onResult,
                               System.Action<string> onError)
    {
        string prompt = PromptKur(playerText, history, gameState);
        string govde = IstekGovdesi(prompt);

        var istek = new UnityWebRequest(config.endpoint, UnityWebRequest.kHttpVerbPOST);
        istek.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(govde));
        istek.downloadHandler = new DownloadHandlerBuffer();
        istek.SetRequestHeader("Content-Type", "application/json");
        istek.timeout = Mathf.Max(1, Mathf.CeilToInt(config.timeoutSeconds));
        aktifIstek = istek;

        CpuyuModeleBirak(true);

        float basladi = Time.realtimeSinceStartup;
        yield return istek.SendWebRequest();
        float gecen = Time.realtimeSinceStartup - basladi;

        CpuyuModeleBirak(false);

        aktifCoroutine = null;
        aktifIstek = null;

        if (istek.result != UnityWebRequest.Result.Success)
        {
            // Unity zaman aşımını da ConnectionError diye raporluyor, o yüzden
            // ikisini istek.error metnine bakarak ayırıyoruz. Yanlış teşhis,
            // teşhis koymamaktan beterdir: ilk denemede "sunucu çalışıyor mu"
            // yazıyordu ve sunucu gayet çalışıyordu.
            bool zamanAsimi = !string.IsNullOrEmpty(istek.error) &&
                              istek.error.ToLower().Contains("timeout");

            string sebep = zamanAsimi
                ? "Paladin düşünmesini " + config.timeoutSeconds.ToString("F0") + " saniyede bitiremedi."
                : (istek.result == UnityWebRequest.Result.ConnectionError
                    ? "Ollama'ya ulaşılamadı (" + istek.error + "). Sunucu çalışıyor mu?"
                    : "Model yanıt vermedi: " + istek.error);

            if (logCalls) Debug.LogWarning("OllamaClient: " + sebep + " (" + gecen.ToString("F1") + " sn)", this);

            istek.Dispose();
            if (onError != null) onError(sebep);
            yield break;
        }

        string ham = istek.downloadHandler.text;
        istek.Dispose();

        HamKaydet(govde, ham);

        OllamaYanit dis = null;
        try { dis = JsonUtility.FromJson<OllamaYanit>(ham); }
        catch (System.Exception e) { Debug.LogWarning("OllamaClient: dış JSON okunamadı: " + e.Message, this); }

        if (dis == null || string.IsNullOrEmpty(dis.response))
        {
            if (onError != null) onError("Sunucudan boş yanıt geldi.");
            yield break;
        }

        PaladinDecision karar = null;
        try { karar = JsonUtility.FromJson<PaladinDecision>(dis.response); }
        catch (System.Exception e) { Debug.LogWarning("OllamaClient: şema JSON'u okunamadı: " + e.Message, this); }

        var olcum = new LlmCallStats
        {
            seconds = gecen,
            tokens = dis.eval_count,
            model = config.model
        };

        // Çeviri doğrulamadan ÖNCE: Gecerli() oyunun kendi değerlerini
        // bekliyor, modelden İngilizce etiket gelirse hepsi geçersiz sayılır.
        if (karar != null) karar.EtiketleriCevir();

        if (karar == null || !karar.Gecerli())
        {
            if (logCalls) Debug.LogWarning("OllamaClient: şemaya uymayan yanıt: " + dis.response, this);
            if (onError != null) onError("Model anlaşılmayan bir yanıt verdi.");
            yield break;
        }

        // Çıktıyı kendi girdisine karşı denetle: oyuncunun hiç anmadığı
        // eşyayı iddia olarak saymayalım. Burada yapılıyor ki ölçüm
        // koşucusu da dahil her çağrı aynı denetimden geçsin.
        int dusen = karar.IddialariDogrula(playerText);
        if (dusen > 0 && logCalls)
            Debug.Log("OllamaClient: oyuncunun cumlesinde gecmeyen " + dusen + " iddia dusuruldu.", this);

        if (logCalls) Debug.Log("OllamaClient: " + olcum + "  ->  " + karar, this);

        if (onResult != null) onResult(karar, olcum);
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
          .Append(",\"num_predict\":").Append(config.numPredict).Append("}");
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
