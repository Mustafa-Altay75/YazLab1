using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

/// <summary>
/// İskeletin otonom davranışını süren bileşen.
///
/// Burada hareketin kendisi yazılmaz: iskelete rota tarif edilmez, sadece
/// hedef nokta verilir ve yolu NavMeshAgent bake edilmiş NavMesh üzerinde
/// kendisi hesaplar. Bu sınıfın işi hangi hedefin ne zaman verileceğine
/// karar veren durum makinesini çevirmek ve animasyonu ajanın gerçek
/// hızından sürmektir.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class SkeletonCompanion : MonoBehaviour
{
    /// <summary>Animatördeki boşta durma durumunun adı.</summary>
    public const string IdleStateName = "Skeletons_Idle";

    [Header("Referanslar")]
    [Tooltip("Boş bırakılırsa aynı objedeki SkeletonInteractable kullanılır.")]
    [SerializeField] private SkeletonInteractable interactable;

    [Tooltip("Boş bırakılırsa alt objelerdeki Animator kullanılır.")]
    [SerializeField] private Animator skeletonAnimator;

    [Tooltip("İskeletin alacağı anahtar. Sandık anahtarı DEĞİL, masanın üstündeki.")]
    [SerializeField] private GameObject keyObject;

    [Tooltip("İskeletin açacağı hücre kapısı.")]
    [SerializeField] private DoorInteractable door;

    [Tooltip("Boş bırakılırsa aynı objedeki SkeletonSensor kullanılır.")]
    [SerializeField] private SkeletonSensor sensor;

    [Tooltip("Görev bitince iskeletin çekileceği nokta. Boşsa kapıdan uzaklaşma yönü hesaplanır.")]
    [SerializeField] private Transform restPoint;

    [Header("İkinci Görev: Basınç Plakası")]
    [Tooltip("İskeletin üstünde duracağı plaka.")]
    [SerializeField] private Transform platePoint;

    [Tooltip("Takip edilecek oyuncu. Boş bırakılırsa sahnedeki PlayerController aranır.")]
    [SerializeField] private Transform player;

    [Tooltip("Takip ederken oyuncuya bu mesafeden yakınsa durur.")]
    [SerializeField] private float followDistance = 2.5f;

    [Tooltip("Takipte hedef bu aralıkla yenilenir (saniye). Her karede yenilemek yol hesabını boşuna tekrarlatır.")]
    [SerializeField] private float followRepathInterval = 0.25f;

    [Tooltip("Takip modunu açıp kapatan tuş.")]
    [SerializeField] private Key followKey = Key.F;

    [Header("Üçüncü Görev: Kılıç ve Muhafızlar")]
    [Tooltip("Masanın üstündeki kılıç. Alınınca gizlenir.")]
    [SerializeField] private GameObject swordObject;

    [Tooltip("İskeletin elindeki kılıç (handslot.r altında). Başta kapalı olmalı.")]
    [SerializeField] private GameObject swordInHand;

    [Tooltip("Muhafızlar. Boş bırakılırsa sahnedeki GuardEncounter aranır.")]
    [SerializeField] private GuardEncounter guardEncounter;

    [Tooltip("Muhafıza bu mesafeden yakınsa vurur. Muhafızlar NavMesh'i oyduğu için kabul yarıçapından büyük olmalı.")]
    [SerializeField] private float attackRange = 2.6f;

    [Tooltip("İskeletin bir vuruşunun hasarı.")]
    [SerializeField] private int attackDamage = 10;

    [Tooltip("İki vuruş arası süre (saniye).")]
    [SerializeField] private float attackCooldown = 0.9f;

    [Header("Davranış Ayarları")]
    [Tooltip("Hedefe bu mesafeden yakınsa vardı sayılır.")]
    [SerializeField] private float acceptRadius = 1.5f;

    [Tooltip("Anahtarı alırken beklenecek süre (saniye).")]
    [SerializeField] private float pickUpDuration = 1.0f;

    [Tooltip("Yol bulunamadığında yeniden denemeden önceki bekleme (saniye).")]
    [SerializeField] private float retryDelay = 2.0f;

    [Tooltip("Ajan bu kadar süre kıpırdamazsa takılmış sayılır (saniye).")]
    [SerializeField] private float stuckThreshold = 1.5f;

    [Tooltip("Kapıyı açtıktan sonra kapıdan bu kadar uzağa çekilir (birim).")]
    [SerializeField] private float stepBackDistance = 2.2f;

    [Tooltip("Geri çekilme bu süreyi aşarsa olduğu yerde kalır (saniye).")]
    [SerializeField] private float stepBackTimeout = 4.0f;

    [Header("Animasyon")]
    [Tooltip("Yürüme animasyonunu açan bool parametresinin adı.")]
    [SerializeField] private string walkParameter = "IsWalking";

    [Tooltip("Bu hızın üstünde yürüme animasyonu oynar.")]
    [SerializeField] private float walkSpeedThreshold = 0.1f;

    [Header("Hata Ayıklama")]
    [Tooltip("Durum geçişleri konsola yazılsın mı.")]
    [SerializeField] private bool logTransitions = true;

    [Tooltip("Sadece gösterim amaçlı; oyun sırasında güncel durumu gösterir.")]
    [SerializeField] private string currentStateName = "-";

    private NavMeshAgent agent;
    private SkeletonContext context;
    private ISkeletonState currentState;
    private int walkParameterHash;
    private bool hasWalkParameter;

    /// <summary>Dışarıdan okunabilsin diye; testlerde ve arayüzde kullanılır.</summary>
    public string CurrentStateName => currentStateName;

    /// <summary>Oyuncu "anahtarı getir" emrini verdi mi.</summary>
    public bool OrderGiven => context != null && context.OrderGiven;

    /// <summary>Anahtar görevi bitti mi. Takip modu ve plaka emri buna bağlı.</summary>
    public bool KeyErrandDone => context != null && context.KeyErrandDone;

    /// <summary>Şu an oyuncuyu takip ediyor mu.</summary>
    public bool IsFollowing => context != null && context.FollowRequested;

    /// <summary>Plakaya gitme emri verildi mi.</summary>
    public bool PlateOrderGiven => context != null && context.PlateOrderGiven;

    /// <summary>Plaka hedefi sahnede bağlı mı.</summary>
    public bool HasPlateTask => platePoint != null;

    /// <summary>Kılıç görevi sahnede bağlı mı.</summary>
    public bool HasSwordTask => swordObject != null && guardEncounter != null;

    /// <summary>Kılıç emri verildi mi.</summary>
    public bool SwordOrderGiven => context != null && context.SwordOrderGiven;

    /// <summary>İskelet kılıcı aldı mı.</summary>
    public bool IsArmed => context != null && context.Armed;

    /// <summary>
    /// "Kılıcı al" emrini iletir. Muhafızların önünden E ile çağrılıyor.
    ///
    /// İskelet emri nerede alırsa alsın masaya kendi yolunu bularak
    /// gidiyor; bu projenin en uzun yolculuğu ve ajanın gerçekten plan
    /// yaptığını gösteren en net örnek.
    /// </summary>
    public void OrderToSword()
    {
        if (context == null || !context.KeyErrandDone) return;
        if (!HasSwordTask || context.SwordOrderGiven || context.Armed) return;

        context.SwordOrderGiven = true;
        context.FollowRequested = false;

        if (logTransitions)
        {
            Debug.Log("İskelete emir verildi: kılıcı al.", this);
        }
    }

    /// <summary>
    /// Oyuncu kapıyı bir kez görüp kendi açamayacağını anladı mı.
    ///
    /// "Plakaya git" emri buna bağlı: oyuncu daha kapıyı görmeden iskeleti
    /// plakaya yollayabilseydi, hem emri yanlışlıkla verebilirdi hem de
    /// bulmacanın ne işe yaradığını hiç anlamadan geçmiş olurdu.
    /// </summary>
    public bool GateSeen { get; private set; }

    /// <summary>Kapının yanına varıldığında PlateGate çağırır.</summary>
    public void MarkGateSeen()
    {
        if (GateSeen) return;

        GateSeen = true;

        if (logTransitions)
        {
            Debug.Log("Oyuncu kapıyı gördü; artık iskelet her yerden plakaya gönderilebilir.", this);
        }
    }

    /// <summary>Kaç kez takıldığı. Testlerde ve raporda kullanılıyor.</summary>
    public int StuckCount => context != null ? context.StuckCount : 0;

    /// <summary>Kaç kez yeniden yol istendiği.</summary>
    public int RepathCount => context != null ? context.RepathCount : 0;

    /// <summary>
    /// Oyuncunun emrini iletir. SkeletonOrderInteractable buradan çağırıyor;
    /// iskeletin yola çıkması bu çağrıya bağlı.
    /// </summary>
    public void GiveOrder()
    {
        if (context == null || context.OrderGiven) return;

        context.OrderGiven = true;

        if (logTransitions)
        {
            Debug.Log("İskelete emir verildi: anahtarı getir.", this);
        }
    }

    /// <summary>
    /// Takip modunu açar/kapatır. Anahtar görevi bitmeden çalışmaz;
    /// o aşamada iskeletin zaten bir işi var.
    /// </summary>
    public void ToggleFollow()
    {
        if (context == null || !context.KeyErrandDone) return;

        // Kılıç görevi sürerken takip emri verilemez; yoldan çevirmek
        // iskeleti yarı yolda bırakırdı.
        if (context.SwordOrderGiven) return;

        context.FollowRequested = !context.FollowRequested;

        if (logTransitions)
        {
            Debug.Log(context.FollowRequested
                ? "İskelet takip moduna alındı."
                : "İskelete burada bekle denildi.", this);
        }
    }

    /// <summary>
    /// "Plakaya git" emrini iletir. Hem iskeletin yanından hem de kapının
    /// önünden çağrılabiliyor; ikisi de aynı duruma gidiyor.
    ///
    /// İskeletin takip ediyor olması şart değil: emir nereden verilirse
    /// verilsin, iskelet o an bulunduğu yerden yola çıkar. Zaten ajanın
    /// kendi yolunu bulduğunu gösteren şey de bu.
    /// </summary>
    public void OrderToPlate()
    {
        if (context == null || !context.KeyErrandDone || !GateSeen) return;
        if (platePoint == null || context.PlateOrderGiven) return;

        context.PlateOrderGiven = true;
        context.FollowRequested = false;

        if (logTransitions)
        {
            Debug.Log("İskelete emir verildi: plakaya git.", this);
        }
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (interactable == null) interactable = GetComponent<SkeletonInteractable>();
        if (skeletonAnimator == null) skeletonAnimator = GetComponentInChildren<Animator>();
        if (sensor == null) sensor = GetComponent<SkeletonSensor>();

        walkParameterHash = Animator.StringToHash(walkParameter);
        hasWalkParameter = HasParameter(skeletonAnimator, walkParameter);

        if (!hasWalkParameter && skeletonAnimator != null)
        {
            Debug.LogWarning(
                "Animatörde '" + walkParameter + "' parametresi yok. " +
                "İskelet yürüyecek ama yürüme animasyonu oynamayacak.", this);
        }

        context = new SkeletonContext(
            agent,
            skeletonAnimator,
            interactable,
            keyObject,
            door,
            sensor,
            restPoint,
            acceptRadius,
            pickUpDuration,
            retryDelay,
            stuckThreshold,
            stepBackDistance,
            stepBackTimeout,
            logTransitions);

        if (player == null)
        {
            PlayerController pc = FindAnyObjectByType<PlayerController>();
            if (pc != null) player = pc.transform;
        }

        context.AttachPlateTask(platePoint, player, followDistance, followRepathInterval);

        if (guardEncounter == null) guardEncounter = FindAnyObjectByType<GuardEncounter>();
        context.AttachSwordTask(swordObject, swordInHand, guardEncounter,
                                attackRange, attackDamage, attackCooldown);

        // Kılıç başta elinde olmamalı; sahnede açık unutulsa bile burada kapanır.
        if (swordInHand != null) swordInHand.SetActive(false);

        // Emri muhafızlar duyuruyor, görevi iskelet yürütüyor. GuardEncounter
        // kılıç diye bir şey bilmiyor; sadece oyuncunun istediğini söylüyor.
        if (guardEncounter != null) guardEncounter.SwordErrandRequested += MuhafizlarKilicIstedi;

        currentState = context.Inactive;
        currentStateName = currentState.Name;
        currentState.Enter(context);
    }

    private void OnDestroy()
    {
        if (guardEncounter != null) guardEncounter.SwordErrandRequested -= MuhafizlarKilicIstedi;
    }

    private void MuhafizlarKilicIstedi(GuardEncounter kaynak, GameObject isteyen)
    {
        OrderToSword();
    }

    private void Update()
    {
        if (currentState == null) return;

        // Sensör her karede, durum makinesinden ÖNCE taransın; durumlar
        // karar verirken güncel ölçümü görsün.
        ScanSensor();

        ReadFollowKey();

        ISkeletonState next = currentState.Tick(context);

        if (next != null && next != currentState)
        {
            currentState.Exit(context);

            if (logTransitions)
            {
                Debug.Log("İskelet durumu: " + currentState.Name + " -> " + next.Name, this);
            }

            currentState = next;
            currentStateName = currentState.Name;
            currentState.Enter(context);
        }

        UpdateWalkAnimation();
    }

    /// <summary>
    /// Takip tuşunu okur.
    ///
    /// Girdi normalde oyuncunun işi, ama bu tek tuş için Mustafa'nın
    /// PlayerController veya PlayerInteraction scriptine dokunmak
    /// gerekmesin diye burada okunuyor. Etkileşim tuşu (E) onun
    /// sisteminde kalmaya devam ediyor.
    /// </summary>
    private void ReadFollowKey()
    {
        if (Keyboard.current == null) return;
        if (context == null || !context.KeyErrandDone) return;

        if (Keyboard.current[followKey].wasPressedThisFrame)
        {
            ToggleFollow();
        }
    }

    /// <summary>
    /// Sensöre o anki hedefi verip çevreyi taratır. Hedef, ajanın gittiği
    /// noktadır; hiç yol yoksa görüş hattı kontrolü atlanır.
    /// </summary>
    private void ScanSensor()
    {
        if (sensor == null) return;

        bool hedefVar = agent != null && agent.isOnNavMesh && agent.hasPath;
        Vector3 hedef = hedefVar ? agent.destination : transform.position;
        sensor.Scan(hedef, hedefVar);
    }

    /// <summary>
    /// Yürüme animasyonunu ajanın gerçek hızından sürer. Animasyonu durum
    /// makinesinden tetiklemek yerine hızdan sürmek, iskelet yavaşlayıp
    /// döndüğünde de doğru görünmesini sağlar.
    /// </summary>
    private void UpdateWalkAnimation()
    {
        if (!hasWalkParameter || skeletonAnimator == null) return;

        bool walking = agent != null
                       && agent.isOnNavMesh
                       && !agent.isStopped
                       && agent.velocity.magnitude > walkSpeedThreshold;

        skeletonAnimator.SetBool(walkParameterHash, walking);
    }

    private static bool HasParameter(Animator animator, string parameterName)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName) return true;
        }

        return false;
    }
}
