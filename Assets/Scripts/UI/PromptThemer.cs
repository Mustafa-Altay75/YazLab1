using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Etkileşim levhasını yapılan işin türüne göre renklendirir ve üstüne
/// kategori etiketi yazar.
///
/// Dokuz etkileşimin hepsi aynı levhayı kullanıyordu; iskelete emir
/// vermekle Paladin'le konuşmak aynı görünüyordu. Oysa projenin iki
/// yapay zekâ bileşeni tam olarak bu ikisi — ayrımın ekranda görünmesi
/// hem oynanışta hem sunumda işe yarıyor.
///
/// Temayı <see cref="PromptThemeRegistry"/> söylüyor. Bizim
/// etkileşimlerimiz kendi temasını bildiriyor; Mustafa'nın dosyalarına
/// dokunmadığımız için onlar bildirmiyor, o yüzden aşağıdaki metin
/// kuralları var. Bunlar yalnızca renk seçiyor, hiçbir oyun kararına
/// karışmıyor.
/// </summary>
public class PromptThemer : MonoBehaviour
{
    [System.Serializable]
    public class TextRule
    {
        [Tooltip("Ekrandaki yazı bunu içeriyorsa tema uygulanır.")]
        public string contains = "";
        public PromptThemeKind kind = PromptThemeKind.World;
    }

    [Header("Levha")]
    [Tooltip("Kenarlık çizgileri. PromptPlate'te üst ve alt olmak üzere iki tane.")]
    [SerializeField] private Image[] borderImages;

    [Tooltip("Levhanın gövdesi; PromptPlate'te kökün kendi Image'i.")]
    [SerializeField] private Image bodyImage;
    [SerializeField] private TextMeshProUGUI promptText;

    [Tooltip("Yazının üstündeki kategori etiketi.")]
    [SerializeField] private TextMeshProUGUI labelText;

    [Tooltip("Tuş kutusunun arka planı ve yazısı; tema rengini alır.")]
    [SerializeField] private Image keyCapImage;
    [SerializeField] private TextMeshProUGUI keyCapText;

    [Header("Temalar")]
    [SerializeField] private PromptTheme[] themes;

    [Tooltip("Bildirim yapmayan etkileşimler için metin kuralları. " +
             "Sırayla bakılır, ilk eşleşen kazanır.")]
    [SerializeField] private TextRule[] textRules;

    [Header("Etiket Gölgesi")]
    [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.85f);
    [SerializeField] private Vector2 shadowOffset = new Vector2(0.6f, -0.6f);
    [Range(-1f, 1f)][SerializeField] private float shadowDilate = 0.2f;
    [Range(0f, 1f)][SerializeField] private float shadowSoftness = 0.25f;

    private PromptThemeKind sonTema = (PromptThemeKind)(-1);
    private bool golgeKuruldu;

    private void Awake()
    {
        // Sahne yeniden yüklendiğinde eski oyunun yazıları kalmasın.
        PromptThemeRegistry.Clear();
    }

    private void LateUpdate()
    {
        if (promptText == null) return;

        string yazi = promptText.text;
        if (string.IsNullOrEmpty(yazi))
        {
            if (labelText != null && labelText.gameObject.activeSelf) labelText.gameObject.SetActive(false);
            return;
        }

        var tema = TemayiBul(yazi);
        if (tema == sonTema) return;

        sonTema = tema;
        Uygula(Tema(tema));
    }

    private PromptThemeKind TemayiBul(string yazi)
    {
        // Önce kendi etkileşimlerimizin bildirdiği kesin eşleşme.
        var kayitli = PromptThemeRegistry.Resolve(yazi);
        if (kayitli != PromptThemeKind.World) return kayitli;

        // Sonra bildirim yapmayanlar için metin kuralları.
        if (textRules != null)
        {
            foreach (var kural in textRules)
            {
                if (kural == null || string.IsNullOrEmpty(kural.contains)) continue;
                if (yazi.Contains(kural.contains)) return kural.kind;
            }
        }

        return PromptThemeKind.World;
    }

    private PromptTheme Tema(PromptThemeKind kind)
    {
        if (themes != null)
            foreach (var t in themes)
                if (t != null && t.kind == kind) return t;

        return new PromptTheme();
    }

    private void Uygula(PromptTheme tema)
    {
        if (borderImages != null)
            foreach (var b in borderImages) if (b != null) b.color = tema.border;
        if (bodyImage != null) bodyImage.color = tema.body;
        if (keyCapImage != null) keyCapImage.color = tema.accent;
        if (keyCapText != null) keyCapText.color = tema.body;

        if (labelText == null) return;

        bool varMi = !string.IsNullOrEmpty(tema.label);
        labelText.gameObject.SetActive(varMi);
        if (!varMi) return;

        labelText.text = tema.label;
        labelText.color = tema.accent;
        GolgeyiKur(labelText);
    }

    /// <summary>
    /// Etiketin arkasına gölge koyar.
    ///
    /// Etiket levhanın dışında, doğrudan oyun dünyasının üstünde duruyor;
    /// koyu zeminde okunuyordu ama ganimet odasının açık taşlarında
    /// kayboluyordu. TMP'nin kendi alt katmanı (underlay) kullanılıyor,
    /// ikinci bir metin kopyası değil.
    ///
    /// <c>fontMaterial</c> ilk okunduğunda TMP o metne özel bir malzeme
    /// örneği üretiyor, yani paylaşılan font malzemesi bozulmuyor.
    /// </summary>
    private void GolgeyiKur(TextMeshProUGUI yazi)
    {
        if (golgeKuruldu) return;

        var mat = yazi.fontMaterial;
        mat.EnableKeyword("UNDERLAY_ON");
        mat.SetColor("_UnderlayColor", shadowColor);
        mat.SetFloat("_UnderlayOffsetX", shadowOffset.x);
        mat.SetFloat("_UnderlayOffsetY", shadowOffset.y);
        mat.SetFloat("_UnderlayDilate", shadowDilate);
        mat.SetFloat("_UnderlaySoftness", shadowSoftness);

        golgeKuruldu = true;
    }
}
