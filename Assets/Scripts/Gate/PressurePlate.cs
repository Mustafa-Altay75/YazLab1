using UnityEngine;

/// <summary>
/// Üstünde ağırlık varken kapıyı açık tutan basınç plakası.
///
/// Kapıyı iskeletin durum makinesi değil, plaka açıyor. Ayrım kasıtlı:
/// plakanın üstünde bir şey olup olmadığını bilen şey plakadır. Plaka
/// iskeletin ne emri aldığını bilmiyor, sadece ağırlık arıyor; böylece
/// ileride oraya başka bir şey de basabilir ve iskelet tarafında hiçbir
/// değişiklik gerekmez.
///
/// İki ölçüt var ve ikisi de fiziksel:
///
/// 1. <b>Dar alan.</b> Plakanın görünen karosu 4x4 ama basma alanı çok
///    daha küçük. Plaka merkez odanın ana güzergâhında duruyor; geniş
///    bırakılsaydı takip eden iskelet odadan geçerken kazara basar ve
///    kapı kendiliğinden açılırdı.
/// 2. <b>Bekleme süresi.</b> Ağırlık kesintisiz bu kadar durmadan plaka
///    inmiş sayılmıyor. Üstünden yürüyerek geçen birini eler, durup
///    bekleyeni geçirir. Gerçek bir basınç plakası da böyle çalışır.
///
/// Konum için collider değil iskeletin transform'u okunuyor: iskeletin
/// collider'ı gövdesinin iki birim önünde duruyor (diriltme etkileşimi
/// için öyle konmuş) ve dar bir alanla birlikte yanlış sonuç verirdi.
/// </summary>
public class PressurePlate : MonoBehaviour
{
    [Tooltip("Plaka basılınca açılacak kapı.")]
    [SerializeField] private PlateGate gate;

    [Tooltip("Boş bırakılırsa sahnedeki SkeletonCompanion aranır.")]
    [SerializeField] private SkeletonCompanion companion;

    [Header("Basma Ölçütleri")]
    [Tooltip("Plakanın merkezine bu yatay mesafeden yakınsa üstünde sayılır.")]
    [SerializeField] private float pressRadius = 1.1f;

    [Tooltip("Ağırlığın kesintisiz bu kadar durması gerekir (saniye). Üstünden geçeni eler.")]
    [SerializeField] private float dwellSeconds = 0.8f;

    [Tooltip("Plakanın kaç saniyede bir yoklanacağı.")]
    [SerializeField] private float checkInterval = 0.1f;

    [Tooltip("Durum değişikliği konsola yazılsın mı.")]
    [SerializeField] private bool logChanges = true;

    private float sonrakiYoklama;
    private float ustundeBeriZaman = -1f;
    private bool basili;

    /// <summary>Plakanın üstünde ağırlık var ve bekleme süresi dolmuş mu.</summary>
    public bool IsPressed { get { return basili; } }

    private void Awake()
    {
        if (companion == null) companion = FindAnyObjectByType<SkeletonCompanion>();
    }

    private void Update()
    {
        if (Time.time < sonrakiYoklama) return;
        sonrakiYoklama = Time.time + checkInterval;

        bool ustunde = UstundeAgirlikVar();

        if (!ustunde)
        {
            ustundeBeriZaman = -1f;
        }
        else if (ustundeBeriZaman < 0f)
        {
            ustundeBeriZaman = Time.time;
        }

        // Bekleme süresi dolmadan basılmış sayılmaz.
        bool simdi = ustunde && (Time.time - ustundeBeriZaman) >= dwellSeconds;

        if (simdi == basili) return;

        basili = simdi;

        if (logChanges)
        {
            Debug.Log(basili ? "Plakaya basıldı, kapı açılıyor."
                             : "Plakadan inildi, kapı kapanıyor.", this);
        }

        if (gate == null) return;
        if (basili) gate.OpenGate(); else gate.CloseGate();
    }

    /// <summary>
    /// İskelet plakanın üstünde mi. Yatay mesafeye bakılıyor; yükseklik
    /// önemli değil, plaka zeminde ve üstünde durulabilen tek şey o.
    /// </summary>
    private bool UstundeAgirlikVar()
    {
        if (companion == null) return false;

        Vector3 a = companion.transform.position;
        Vector3 b = transform.position;
        a.y = 0f;
        b.y = 0f;

        return Vector3.Distance(a, b) <= pressRadius;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Application.isPlaying && basili ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pressRadius);
    }
}
