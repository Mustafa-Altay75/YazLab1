using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Paladin'le yapılan konuşmanın tam dökümünü oyun içinde gösterir.
///
/// Proje dokümanının 4.4 maddesi en az beş farklı diyalog senaryosunun
/// test edilmesini ve <i>modelin tepkilerinin raporda örneklerle
/// gösterilmesini</i> istiyor. Konsol kaydı bunu tam karşılamıyor: ekran
/// görüntüsünde oyunun içinden geldiği belli olmuyor ve modelin ne
/// dediğiyle kodun ne yaptığı yan yana görünmüyor.
///
/// Panelin asıl işi bu ikisini yan yana koymak. Her tur için modelin
/// söyledikleri (niyet, samimiyet, iddialar) ile kodun kararı (güvenin
/// hangi yöne ne kadar gittiği, yalan sayacı) aynı blokta duruyor — yani
/// <b>sınıflandıran model, karar veren kod</b> ayrımı tek bir ekran
/// görüntüsünde görülebiliyor.
///
/// Oyun mantığına dokunmuyor, yalnızca kaydediyor ve çiziyor. Kapalıyken
/// hiçbir etkisi yok: TestShortcuts ve ErrandTimer ile aynı desen, tek
/// anahtar.
/// </summary>
public class DialogueTranscript : MonoBehaviour
{
    [Tooltip("ANA ANAHTAR. Kapalıyken ne kayıt tutulur ne de panel açılır. " +
             "Ekran görüntüsü alınırken açılır, teslimde kapalı olmalı.")]
    [SerializeField] private bool active;

    [Tooltip("Paneli açıp kapatan tuş. Harf tuşu olamaz: sohbet kutusu " +
             "açıkken yazılan her harf panele de giderdi.")]
    [SerializeField] private Key toggleKey = Key.F3;

    [Tooltip("Ekran görüntüsünün üstünde görünecek senaryo adı. Her senaryodan " +
             "önce buradan değiştirilir; boşsa hiç yazılmaz.")]
    [SerializeField] private string scenarioLabel = "";

    /// <summary>Tek bir konuşma turu: oyuncunun cümlesi ve ona verilen karşılık.</summary>
    private class Tur
    {
        public int Sira;
        public string Oyuncu;
        public string Niyet;
        public int Samimiyet;
        public string Iddialar;
        public int GuvenOnce;
        public int GuvenSonra;
        public int YalanToplam;
        public int TehditToplam;
        public string Replik;
        public string Ceviri;
        public bool Cevrimdisi;
        public float Saniye;
        public string Model;
    }

    private static DialogueTranscript ornek;

    private readonly List<Tur> turlar = new List<Tur>();
    private PaladinNegotiation pazarlik;
    private PlayerPossessions tasinanlar;
    private bool gorunur;
    private Vector2 kaydirma;
    private int sonGuven;

    private GUIStyle baslikStili;
    private GUIStyle satirStili;
    private Texture2D zemin;

    private void Awake()
    {
        ornek = this;
        if (pazarlik == null) pazarlik = FindAnyObjectByType<PaladinNegotiation>();
        if (tasinanlar == null) tasinanlar = FindAnyObjectByType<PlayerPossessions>();
    }

    private void Start()
    {
        // Başlangıç güveni Awake'te okunamıyor: pazarlık da kendi Awake'inde
        // kuruyor ve sıra garanti değil. İlk turun "önce" değeri sıfır
        // görünüyordu, yani ilk satırın güven farkı olduğundan büyük çıkıyordu.
        if (pazarlik != null) sonGuven = pazarlik.Trust;
    }

    private void OnDestroy()
    {
        if (ornek == this) ornek = null;
    }

    /// <summary>
    /// Bir turu kaydeder. Diyalog paneli her yanıttan sonra, pazarlığın
    /// Apply çağrısından hemen <b>sonra</b> çağırıyor; sıra önemli, çünkü
    /// güvenin yeni değeri ancak o çağrıdan sonra belli oluyor.
    ///
    /// Bileşen sahnede yoksa ya da kapalıysa hiçbir şey yapmıyor, böylece
    /// çağıran tarafın kontrol etmesi gerekmiyor.
    /// </summary>
    public static void Kaydet(string oyuncuMetni, PaladinDecision karar, LlmCallStats olcum)
    {
        if (ornek == null || !ornek.active || karar == null) return;
        ornek.TuruEkle(oyuncuMetni, karar, olcum);
    }

    private void TuruEkle(string oyuncuMetni, PaladinDecision karar, LlmCallStats olcum)
    {
        int guvenSonra = pazarlik != null ? pazarlik.Trust : sonGuven;

        turlar.Add(new Tur
        {
            Sira = turlar.Count + 1,
            Oyuncu = oyuncuMetni,
            Niyet = karar.niyet,
            Samimiyet = karar.samimiyet,
            Iddialar = IddiaMetni(karar.claims),
            GuvenOnce = sonGuven,
            GuvenSonra = guvenSonra,
            YalanToplam = pazarlik != null ? pazarlik.LieCount : 0,
            TehditToplam = pazarlik != null ? pazarlik.ThreatCount : 0,
            Replik = karar.replik,
            Ceviri = karar.ceviri,
            Cevrimdisi = karar.cevrimdisi,
            Saniye = olcum.seconds,
            Model = olcum.model
        });

        sonGuven = guvenSonra;
    }

    /// <summary>
    /// İddiaları, her birinin doğru mu yalan mı olduğuyla birlikte yazar.
    ///
    /// Doğruluğu modele sormuyoruz; oyuncunun gerçekten ne taşıdığını bilen
    /// PlayerPossessions'a soruyoruz. Yalanı yakalayan mekanizma bu ve ekran
    /// görüntüsünde görünmesi gereken şey de tam olarak bu.
    /// </summary>
    private string IddiaMetni(string[] iddialar)
    {
        if (iddialar == null || iddialar.Length == 0) return "-";

        var sb = new StringBuilder();

        foreach (string iddia in iddialar)
        {
            if (string.IsNullOrEmpty(iddia)) continue;
            if (sb.Length > 0) sb.Append(", ");

            sb.Append(iddia);

            if (iddia == PlayerPossessions.Yok || tasinanlar == null) continue;
            sb.Append(tasinanlar.Has(iddia) ? " (dogru)" : " (YALAN)");
        }

        return sb.Length > 0 ? sb.ToString() : "-";
    }

    private void Update()
    {
        if (!active || Keyboard.current == null) return;
        if (Keyboard.current[toggleKey].wasPressedThisFrame) gorunur = !gorunur;
    }

    private void OnGUI()
    {
        if (!active || !gorunur) return;

        StilleriKur();

        float genislik = Mathf.Min(Screen.width - 40f, 980f);
        float yukseklik = Screen.height - 40f;
        Rect cerceve = new Rect((Screen.width - genislik) / 2f, 20f, genislik, yukseklik);

        GUI.DrawTexture(cerceve, zemin);
        GUILayout.BeginArea(new Rect(cerceve.x + 16f, cerceve.y + 12f,
                                     cerceve.width - 32f, cerceve.height - 24f));

        GUILayout.Label(BaslikMetni(), baslikStili);
        GUILayout.Space(6f);

        kaydirma = GUILayout.BeginScrollView(kaydirma);

        if (turlar.Count == 0) GUILayout.Label("Henuz konusma yok.", satirStili);

        foreach (Tur tur in turlar)
        {
            GUILayout.Label(TurMetni(tur), satirStili);
            GUILayout.Space(8f);
        }

        GUILayout.EndScrollView();

        GUILayout.Space(6f);
        GUILayout.Label(OzetMetni(), baslikStili);

        GUILayout.EndArea();
    }

    private string BaslikMetni()
    {
        var sb = new StringBuilder("DIYALOG OZETI");
        if (!string.IsNullOrEmpty(scenarioLabel)) sb.Append("  -  ").Append(scenarioLabel);

        if (turlar.Count > 0)
        {
            string model = turlar[turlar.Count - 1].Model;
            if (!string.IsNullOrEmpty(model)) sb.Append("  -  model: ").Append(model);
        }

        return sb.ToString();
    }

    private string TurMetni(Tur tur)
    {
        int fark = tur.GuvenSonra - tur.GuvenOnce;
        string yon = fark > 0 ? "+" : "";

        var sb = new StringBuilder();

        sb.Append(tur.Sira).Append(". oyuncu: ").Append('"').Append(tur.Oyuncu).Append('"').Append('\n');

        sb.Append("    model  : niyet ").Append(tur.Niyet)
          .Append(" | samimiyet ").Append(tur.Samimiyet)
          .Append(" | iddia: ").Append(tur.Iddialar);

        if (tur.Saniye > 0f)
        {
            sb.Append(" | ").Append(tur.Saniye.ToString("0.0", CultureInfo.InvariantCulture))
              .Append(" sn");
        }

        if (tur.Cevrimdisi) sb.Append("  [CEVRIMDISI]");

        sb.Append('\n').Append("    kod    : guven ").Append(tur.GuvenOnce)
          .Append(" -> ").Append(tur.GuvenSonra)
          .Append(" (").Append(yon).Append(fark).Append(")")
          .Append(" | yalan ").Append(tur.YalanToplam)
          .Append(" | tehdit ").Append(tur.TehditToplam);

        sb.Append('\n').Append("    paladin: ").Append(tur.Replik);

        if (!string.IsNullOrEmpty(tur.Ceviri))
        {
            sb.Append('\n').Append("             (").Append(tur.Ceviri).Append(")");
        }

        return sb.ToString();
    }

    private string OzetMetni()
    {
        var sb = new StringBuilder();
        sb.Append("Tur: ").Append(turlar.Count);

        if (pazarlik != null)
        {
            sb.Append("   guven: ").Append(pazarlik.Trust)
              .Append(" (gecis ").Append(pazarlik.TrustToPass)
              .Append(" / hukum ").Append(pazarlik.TrustToFail).Append(")")
              .Append("   yalan: ").Append(pazarlik.LieCount)
              .Append("   tehdit: ").Append(pazarlik.ThreatCount)
              .Append("   son: ")
              .Append(pazarlik.Reached != null ? pazarlik.Reached.Title : "-");
        }

        return sb.ToString();
    }

    private void StilleriKur()
    {
        if (zemin == null)
        {
            // Yarı saydam koyu zemin: ekran görüntüsünde panelin arkasında
            // oyunun kendisi görünsün isteniyor, yoksa "oyun içinden" olduğu
            // belli olmuyor. Tek pikseli esnetmek bir doku dosyası tutmaktan
            // ucuz.
            zemin = new Texture2D(1, 1);
            zemin.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.82f));
            zemin.Apply();
        }

        if (baslikStili == null)
        {
            baslikStili = new GUIStyle(GUI.skin.label);
            baslikStili.fontSize = 16;
            baslikStili.fontStyle = FontStyle.Bold;
            baslikStili.normal.textColor = new Color(1f, 0.85f, 0.4f);
            baslikStili.wordWrap = true;
        }

        if (satirStili == null)
        {
            satirStili = new GUIStyle(GUI.skin.label);
            satirStili.fontSize = 14;
            satirStili.richText = false;
            satirStili.wordWrap = true;
            satirStili.normal.textColor = Color.white;
        }
    }
}
