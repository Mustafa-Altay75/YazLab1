/// <summary>
/// Paladin'e bir cümle sorup yapılandırılmış yanıt alan istemci.
///
/// Arayüz olmasının sebebi testler ve yedek plan: oyun kodu Ollama'yı
/// değil bu sözleşmeyi tanıyor. İleride çevrimdışı bir değerlendirici ya
/// da testlerde sahte bir istemci aynı yere takılabilir, diyalog
/// arayüzünde tek satır değişmeden.
/// </summary>
public interface ILlmClient
{
    /// <summary>Şu an bekleyen bir istek var mı.</summary>
    bool IsBusy { get; }

    /// <summary>Kullanılan modelin görünen adı.</summary>
    string ModelName { get; }

    /// <summary>
    /// Oyuncunun cümlesini gönderir. Kareyi bloklamaz; sonuç hazır
    /// olduğunda geri çağrı tetiklenir.
    /// </summary>
    /// <param name="playerText">Oyuncunun yazdığı cümle.</param>
    /// <param name="history">Önceki konuşma, modele bağlam olarak verilir.</param>
    /// <param name="gameState">
    /// Oyunun bildiği doğrular: oyuncunun gerçekten ne taşıdığı, Paladin'in
    /// sabrı, rüşvet istenip istenmediği. Modelin bilemeyeceği şeyler
    /// bunlar; vermezsek iddia uydurup yalanı da fark edemiyor.
    /// </param>
    /// <param name="onResult">Başarılı yanıt. Şema geçerliyse çağrılır.</param>
    /// <param name="onError">Hata ya da zaman aşımı; tek cümlelik sebep.</param>
    void Ask(string playerText, string history, string gameState,
             System.Action<PaladinDecision, LlmCallStats> onResult,
             System.Action<string> onError);

    /// <summary>Bekleyen isteği iptal eder.</summary>
    void Cancel();
}

/// <summary>
/// Bir çağrının ölçümleri. Rapordaki model karşılaştırması ve zaman aşımı
/// değeri bu sayılardan çıkacak; tahminle değil.
/// </summary>
public struct LlmCallStats
{
    /// <summary>İstek gönderiminden yanıta kadar geçen süre (saniye).</summary>
    public float seconds;

    /// <summary>Modelin ürettiği token sayısı.</summary>
    public int tokens;

    /// <summary>Saniyede token.</summary>
    public float TokensPerSecond { get { return seconds > 0f ? tokens / seconds : 0f; } }

    /// <summary>Yanıtı veren modelin adı.</summary>
    public string model;

    public override string ToString()
    {
        return model + "  " + seconds.ToString("F1") + " sn, " + tokens + " token, " +
               TokensPerSecond.ToString("F1") + " token/sn";
    }
}
