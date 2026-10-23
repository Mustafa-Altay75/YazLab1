using UnityEngine;

/// <summary>
/// İskeletin çevresini raycast ile gözlemleyen sensör.
///
/// Şartname, NavMesh kullanılsa bile ajanın konumunu ve engellere olan
/// mesafesini/açısını algılayan sensör tabanlı bir gözlem mekanizması
/// istiyor. NavMesh yolu zaten buluyor; burada ölçülen veri, ajan bir yere
/// takıldığında neden takıldığını anlamak ve yeniden yol istemek için
/// kullanılıyor.
///
/// Üç tür ışın atılıyor:
///   1. Önde yelpaze  — engel var mı, ne kadar uzakta, hangi açıda
///   2. Hedefe tek ışın — görüş hattı açık mı
///   3. Aşağı ışın     — ayağının altında zemin var mı
/// </summary>
public class SkeletonSensor : MonoBehaviour
{
    [Header("Yelpaze (engel algılama)")]
    [Tooltip("Öne atılan ışın sayısı. Tek sayı olmalı ki ortada bir ışın bulunsun.")]
    [SerializeField] private int rayCount = 5;

    [Tooltip("Yelpazenin toplam açıklığı (derece).")]
    [SerializeField] private float spreadAngle = 70f;

    [Tooltip("İleri ışınların menzili (birim).")]
    [SerializeField] private float forwardRange = 2.5f;

    [Header("Diğer ışınlar")]
    [Tooltip("Işınların çıktığı göz yüksekliği.")]
    [SerializeField] private float eyeHeight = 0.5f;

    [Tooltip("Zemin kontrolü için aşağı atılan ışının menzili.")]
    [SerializeField] private float groundRange = 1.2f;

    [Header("Katmanlar")]
    [Tooltip("Engel sayılacak katmanlar.")]
    [SerializeField] private LayerMask obstacleMask = ~0;

    [Header("Hata Ayıklama")]
    [SerializeField] private bool drawGizmos = true;

    // --- Son taramanın sonuçları ---

    /// <summary>Yelpazedeki ışınlardan en az biri bir engele çarptı mı.</summary>
    public bool ObstacleAhead { get; private set; }

    /// <summary>Çarpan ışınlar içinde en yakın engelin mesafesi. Yoksa menzil.</summary>
    public float NearestObstacleDistance { get; private set; }

    /// <summary>En yakın engelin ileri yöne göre açısı (derece, soldaki negatif).</summary>
    public float NearestObstacleAngle { get; private set; }

    /// <summary>Hedefe giden doğru hat engelsiz mi.</summary>
    public bool TargetInLineOfSight { get; private set; }

    /// <summary>Ayağının altında zemin var mı.</summary>
    public bool GroundBelow { get; private set; }

    /// <summary>Son taramada atılan ışın sayısı.</summary>
    public int LastRayCount { get; private set; }

    // Gizmos için son taramanın ham verisi
    private Vector3[] rayEnds;
    private bool[] rayHits;
    private Vector3 eye;
    private Vector3 lastTarget;
    private bool hasTarget;

    // Kendi gövdesini engel saymamak için; ayrıca her karede çöp üretmemek için
    // RaycastNonAlloc kullanılıyor.
    private readonly RaycastHit[] buffer = new RaycastHit[12];

    private void Awake()
    {
        if (rayCount < 1) rayCount = 1;
        rayEnds = new Vector3[rayCount];
        rayHits = new bool[rayCount];
    }

    /// <summary>
    /// Tek ışın atar ve KENDİ collider'larını atlayarak en yakın gerçek
    /// engeli döndürür.
    ///
    /// Bu atlama şart: iskeletin kendi BoxCollider'ı (diriltme etkileşimi
    /// için) gövdesinin 2 birim önünde duruyor, yani tam ileri ışınların
    /// yolunda. Atlanmazsa sensör her karede "önümde engel var" derdi.
    /// </summary>
    private bool Isin(Vector3 origin, Vector3 dir, float range, out RaycastHit nearest)
    {
        nearest = default(RaycastHit);

        int sayi = Physics.RaycastNonAlloc(origin, dir, buffer, range, obstacleMask,
                                           QueryTriggerInteraction.Ignore);
        float enIyi = float.MaxValue;
        bool bulundu = false;

        for (int i = 0; i < sayi; i++)
        {
            Collider c = buffer[i].collider;
            if (c == null) continue;
            if (c.transform.IsChildOf(transform)) continue;   // kendi gövdemiz

            if (buffer[i].distance < enIyi)
            {
                enIyi = buffer[i].distance;
                nearest = buffer[i];
                bulundu = true;
            }
        }

        return bulundu;
    }

    /// <summary>
    /// Çevreyi tarar. Her karede çağrılır; hedef verilmezse görüş hattı
    /// kontrolü atlanır.
    /// </summary>
    public void Scan(Vector3 target, bool hasTargetPoint)
    {
        if (rayEnds == null || rayEnds.Length != rayCount)
        {
            rayEnds = new Vector3[rayCount];
            rayHits = new bool[rayCount];
        }

        eye = transform.position + Vector3.up * eyeHeight;
        lastTarget = target;
        hasTarget = hasTargetPoint;
        LastRayCount = rayCount;

        // --- 1) Önde yelpaze ---
        ObstacleAhead = false;
        NearestObstacleDistance = forwardRange;
        NearestObstacleAngle = 0f;

        float adim = (rayCount > 1) ? spreadAngle / (rayCount - 1) : 0f;
        float baslangic = -spreadAngle * 0.5f;

        for (int i = 0; i < rayCount; i++)
        {
            float aci = baslangic + adim * i;
            Vector3 yon = Quaternion.AngleAxis(aci, Vector3.up) * transform.forward;

            RaycastHit vurus;
            bool carpti = Isin(eye, yon, forwardRange, out vurus);

            rayHits[i] = carpti;
            rayEnds[i] = carpti ? vurus.point : eye + yon * forwardRange;

            if (carpti)
            {
                ObstacleAhead = true;
                if (vurus.distance < NearestObstacleDistance)
                {
                    NearestObstacleDistance = vurus.distance;
                    NearestObstacleAngle = aci;
                }
            }
        }

        // --- 2) Hedefe görüş hattı ---
        if (hasTargetPoint)
        {
            Vector3 hedefGoz = target + Vector3.up * eyeHeight;
            Vector3 fark = hedefGoz - eye;
            float uzaklik = fark.magnitude;
            RaycastHit engel;
            TargetInLineOfSight = uzaklik < 0.01f ||
                                  !Isin(eye, fark.normalized, uzaklik, out engel);
        }
        else
        {
            TargetInLineOfSight = false;
        }

        // --- 3) Zemin ---
        RaycastHit zemin;
        GroundBelow = Isin(eye, Vector3.down, groundRange + eyeHeight, out zemin);
    }

    /// <summary>Konsola yazılacak okunur özet; takılma anında kullanılıyor.</summary>
    public string Ozet()
    {
        return string.Format(
            "engel={0} mesafe={1:F2} açı={2:F0}° görüşHattı={3} zemin={4}",
            ObstacleAhead, NearestObstacleDistance, NearestObstacleAngle,
            TargetInLineOfSight, GroundBelow);
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos || rayEnds == null) return;

        // Yelpaze: engele çarpan kırmızı, boş olan yeşil
        for (int i = 0; i < rayEnds.Length; i++)
        {
            Gizmos.color = rayHits[i] ? Color.red : Color.green;
            Gizmos.DrawLine(eye, rayEnds[i]);
            if (rayHits[i]) Gizmos.DrawWireSphere(rayEnds[i], 0.07f);
        }

        // Hedefe görüş hattı: açıksa sarı, kapalıysa gri
        if (hasTarget)
        {
            Gizmos.color = TargetInLineOfSight ? Color.yellow : Color.gray;
            Gizmos.DrawLine(eye, lastTarget + Vector3.up * eyeHeight);
        }

        // Zemin ışını
        Gizmos.color = GroundBelow ? Color.cyan : Color.magenta;
        Gizmos.DrawLine(eye, eye + Vector3.down * (groundRange + eyeHeight));
    }
}
