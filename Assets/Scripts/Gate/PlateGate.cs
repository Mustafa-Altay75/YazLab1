using UnityEngine;

/// <summary>
/// Paladin'in odasına açılan çift kanatlı kapı. İki kanat zıt yönlere,
/// araba kapısı gibi dışa doğru açılır.
///
/// Kapıyı oyuncu açmaz; plakada bekleyen iskelet açar. PressurePlate
/// üstünde ağırlık olduğu sürece OpenGate, inilince CloseGate çağırıyor.
///
/// Kapının yanındaki yazı hem mekanizmayı anlatıyor hem de emri veriyor.
/// Buraya bir kez gelmek bulmacayı "açıyor": o ana kadar iskeletin
/// yanında plaka seçeneği hiç görünmüyor, oyuncu kapıyı görüp kendi
/// açamayacağını anladıktan sonra emri her yerden verebiliyor. Emir
/// nereden verilirse verilsin iskelet o anki konumundan yola çıkıyor,
/// sabit bir karodan değil.
///
/// Kapanma gecikmeli: plakadan inen iskelet kapının dışında kalmasın diye
/// kapı, iskelet içeri geçene kadar açık kalıyor.
/// </summary>
public class PlateGate : MonoBehaviour, IInteractable
{
    [Header("Kanatlar")]
    [SerializeField] private Transform leafWest;
    [SerializeField] private Transform leafEast;

    [Header("Açılma")]
    [Tooltip("Kanatların kapalı konumdan sapma açısı. Batı kanadı eksi, doğu kanadı artı yönde döner.")]
    [SerializeField] private float openAngle = 105f;
    [SerializeField] private float openSpeed = 2.5f;

    [Header("Etkileşim")]
    [Tooltip("Boş bırakılırsa sahnedeki SkeletonCompanion aranır.")]
    [SerializeField] private SkeletonCompanion companion;

    [Tooltip("Kapının yanındaki yazı: mekanizmayı anlatır ve emri verir.")]
    [SerializeField] private string sendPrompt = "Kapı plakayla açılıyor — [E] iskeleti plakaya gönder";

    [Header("Kapanma")]
    [Tooltip("Plakadan inildikten sonra kapı, iskelet içeri geçene kadar açık kalır. Bu süre üst sınır.")]
    [SerializeField] private float closeGraceSeconds = 30f;

    [Tooltip("İskelet kapının iç tarafında bu kadar ilerlediyse geçmiş sayılır ve kapı kapanır.")]
    [SerializeField] private float insideClearance = 1.5f;


    [Header("Hata Ayıklama")]
    [Tooltip("Sadece geliştirme için: açıkken oyuncu E ile kapıyı doğrudan açabilir. Teslimde kapalı olmalı.")]
    [SerializeField] private bool allowPlayerInteraction = false;
    [SerializeField] private string debugOpenPrompt = "[E] Kapıyı aç (hata ayıklama)";

    private Quaternion kapaliBati, kapaliDogu;
    private Quaternion acikBati, acikDogu;
    private bool acik;

    // Plakadan inildi ama kapı henüz kapanmadı: iskeletin geçmesi bekleniyor.
    private bool kapanmayiBekliyor;
    private float kapanmaSonTarih;

    /// <summary>Kapı tamamen veya kısmen açık mı.</summary>
    public bool IsOpen { get { return acik; } }

    private void Awake()
    {
        if (companion == null) companion = FindAnyObjectByType<SkeletonCompanion>();

        if (leafWest != null)
        {
            kapaliBati = leafWest.localRotation;
            acikBati = kapaliBati * Quaternion.Euler(0f, -openAngle, 0f);
        }
        if (leafEast != null)
        {
            kapaliDogu = leafEast.localRotation;
            acikDogu = kapaliDogu * Quaternion.Euler(0f, openAngle, 0f);
        }
    }

    private void Update()
    {
        KapanmayiYurut();

        float hiz = Time.deltaTime * openSpeed;
        if (leafWest != null)
            leafWest.localRotation = Quaternion.Slerp(leafWest.localRotation, acik ? acikBati : kapaliBati, hiz);
        if (leafEast != null)
            leafEast.localRotation = Quaternion.Slerp(leafEast.localRotation, acik ? acikDogu : kapaliDogu, hiz);
    }

    public string GetInteractPrompt()
    {
        if (acik) return string.Empty;

        if (allowPlayerInteraction) return debugOpenPrompt;

        // İskelet henüz anahtar görevindeyse oyuncu buraya gelmiş olamaz,
        // ama gelirse de gösterecek bir şey yok.
        if (companion == null || !companion.KeyErrandDone) return string.Empty;
        if (!companion.HasPlateTask || companion.PlateOrderGiven) return string.Empty;

        // Yazının ekranda görünmesi, oyuncunun kapıyı görmesi demek.
        // Bulmacanın açıldığı an burası.
        companion.MarkGateSeen();

        return sendPrompt;
    }

    public void Interact(GameObject interactor)
    {
        if (acik) return;

        if (allowPlayerInteraction)
        {
            OpenGate();
            return;
        }

        if (companion == null) return;

        companion.OrderToPlate();
    }

    /// <summary>Plaka basılınca çağrılır.</summary>
    public void OpenGate()
    {
        kapanmayiBekliyor = false;
        acik = true;
        KanatCarpismasi(false);
    }

    /// <summary>
    /// Plakadan inilince çağrılır. Kapıyı hemen kapatmaz: plakadan inen
    /// iskelet kapıya doğru yürüyor ve anında kapatmak onu dışarıda
    /// bırakırdı. İskelet içeri geçince ya da süre dolunca kapanır.
    /// </summary>
    public void CloseGate()
    {
        if (!acik || kapanmayiBekliyor) return;

        kapanmayiBekliyor = true;
        kapanmaSonTarih = Time.time + closeGraceSeconds;
    }

    /// <summary>
    /// Bekleyen kapanmayı yürütür. İskelet kapının iç tarafına geçtiyse
    /// beklemeye gerek yok; geçmiyorsa süre dolunca yine de kapanır.
    /// </summary>
    private void KapanmayiYurut()
    {
        if (!kapanmayiBekliyor) return;

        bool gecti = IskeletIcerde();
        bool sureDoldu = Time.time >= kapanmaSonTarih;

        if (!gecti && !sureDoldu) return;

        kapanmayiBekliyor = false;
        acik = false;
        KanatCarpismasi(true);
    }

    /// <summary>
    /// İskelet kapının iç tarafına (odaya) geçti mi. Kapının kendi ileri
    /// ekseni kullanılıyor, böylece kapı döndürülürse de doğru çalışır.
    /// </summary>
    private bool IskeletIcerde()
    {
        if (companion == null) return false;

        Vector3 yerel = transform.InverseTransformPoint(companion.transform.position);
        return yerel.z > insideClearance;
    }

    /// <summary>
    /// Kanat collider'ları yalnızca kapalıyken yolu kesmeli. Açılırken
    /// kapatılıyor, yoksa dönen kanat oyuncuyu kenara itiyor.
    /// </summary>
    private void KanatCarpismasi(bool aktif)
    {
        if (leafWest != null)
        {
            var c = leafWest.GetComponent<Collider>();
            if (c != null) c.enabled = aktif;
        }
        if (leafEast != null)
        {
            var c = leafEast.GetComponent<Collider>();
            if (c != null) c.enabled = aktif;
        }
    }
}
