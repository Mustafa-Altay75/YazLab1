using UnityEngine;
using UnityEngine.AI;

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
            acceptRadius,
            pickUpDuration,
            retryDelay,
            stuckThreshold,
            stepBackDistance,
            stepBackTimeout,
            logTransitions);

        currentState = context.Inactive;
        currentStateName = currentState.Name;
        currentState.Enter(context);
    }

    private void Update()
    {
        if (currentState == null) return;

        // Sensör her karede, durum makinesinden ÖNCE taransın; durumlar
        // karar verirken güncel ölçümü görsün.
        ScanSensor();

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
