using UnityEngine;

/// <summary>
/// Bir dil modelinin bütün ayarları. Model adı hiçbir yerde koda
/// gömülmüyor; modeli değiştirmek sahnedeki alana başka bir varlık
/// sürüklemek demek, yeniden derleme gerekmiyor.
///
/// Her model için ayrı bir varlık tutuluyor (LlmConfig_Llama,
/// LlmConfig_Qwen), çünkü sistem promptunun modele göre farklılaşması
/// gerekebilir. Karşılaştırma yaparken promptlar aynı tutulmalı, yoksa
/// ölçülen şey model değil prompt ayarı olur.
/// </summary>
[CreateAssetMenu(fileName = "LlmConfig", menuName = "YazLab/LLM Ayarı")]
public class LlmConfig : ScriptableObject
{
    [Header("Sunucu")]
    [Tooltip("Ollama adresi. Yerelde çalışır; dışarı hiçbir veri gitmez.")]
    public string endpoint = "http://localhost:11434/api/generate";

    [Tooltip("Model adı, 'ollama list' çıktısındaki gibi.")]
    public string model = "llama3.2:3b";

    [Header("Üretim")]
    [Tooltip("0 kararlı, 1 yaratıcı. Ölçümler 0.4 ile alındı.")]
    [Range(0f, 1f)] public float temperature = 0.4f;

    [Tooltip("Üretilecek en fazla token. Replik kısa olduğu için düşük tutulabilir.")]
    public int numPredict = 220;

    [Tooltip("Yanıt bu süre içinde gelmezse istek iptal edilir (saniye). Ölçümle belirlendi, tahminle değil.")]
    public float timeoutSeconds = 90f;

    [Tooltip("Modelin bellekte kalma süresi. Ollama varsayılanı 5 dakika; " +
             "süre dolunca sonraki çağrı modeli baştan yüklüyor ve ilk soru çok uzuyor.")]
    public string keepAlive = "30m";

    [Header("Sistem Promptu")]
    [Tooltip("Modele verilen rol ve kurallar. Raporda paylaşılacak.")]
    [TextArea(10, 40)]
    public string systemPrompt = "";

    [Header("Görünen Ad")]
    [Tooltip("Arayüzdeki model değiştiricide görünecek kısa ad.")]
    public string displayName = "Llama 3.2 3B";
}
