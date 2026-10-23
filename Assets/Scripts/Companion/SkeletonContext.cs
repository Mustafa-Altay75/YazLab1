using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Durumların ortak kullandığı veri ve referansları taşır.
/// Durum sınıfları MonoBehaviour değildir; ihtiyaç duydukları her şeye
/// buradan erişirler. Böylece durumlar sahneden bağımsız kalır ve test edilebilir olur.
/// </summary>
public class SkeletonContext
{
    // --- Sahneden gelen referanslar ---
    public NavMeshAgent Agent { get; }
    public Animator Animator { get; }
    public SkeletonInteractable Interactable { get; }

    /// <summary>İskeletin alacağı anahtar (masanın üstündeki, sandık anahtarından ayrı).</summary>
    public GameObject KeyObject { get; }

    /// <summary>Açılacak hücre kapısı.</summary>
    public DoorInteractable Door { get; }

    /// <summary>Çevreyi raycast ile gözleyen sensör.</summary>
    public SkeletonSensor Sensor { get; }

    /// <summary>
    /// Görev bitince iskeletin çekileceği nokta. Boşsa kapıdan uzaklaşma
    /// yönü hesaplanır, ama sahnede bir nokta verilirse o kullanılır.
    /// </summary>
    public Transform RestPoint { get; }

    // --- Ayarlar ---
    /// <summary>Hedefe bu mesafeden yakınsa "vardı" sayılır.</summary>
    public float AcceptRadius { get; }

    /// <summary>Anahtarı alma animasyonu için beklenecek süre (saniye).</summary>
    public float PickUpDuration { get; }

    /// <summary>Yol bulunamadığında yeniden denemeden önce beklenecek süre (saniye).</summary>
    public float RetryDelay { get; }

    /// <summary>Ajan bu kadar süre kıpırdamazsa takılmış sayılır (saniye).</summary>
    public float StuckThreshold { get; }

    /// <summary>Kapıyı açtıktan sonra kapıdan bu kadar uzağa çekilir (birim).</summary>
    public float StepBackDistance { get; }

    /// <summary>Geri çekilme bu süreden uzun sürerse olduğu yerde kalır (saniye).</summary>
    public float StepBackTimeout { get; }

    /// <summary>Durum geçişleri konsola yazılsın mı.</summary>
    public bool LogTransitions { get; }

    // --- Çalışma anında değişen veri ---
    /// <summary>Anahtar alındı mı.</summary>
    public bool HasKey { get; set; }

    /// <summary>Oyuncu "anahtarı getir" emrini verdi mi.</summary>
    public bool OrderGiven { get; set; }

    /// <summary>Kaç kez takıldığı. Testlerde ve raporda kullanılıyor.</summary>
    public int StuckCount { get; private set; }

    /// <summary>Kaç kez yeniden yol istendiği.</summary>
    public int RepathCount { get; private set; }

    // Ajanın ne zamandır kıpırdamadığı. -1 ise hareket hâlinde.
    private float stoppedSince = -1f;

    // --- Durum örnekleri ---
    // Her karede yeni nesne üretmemek için durumlar bir kez oluşturulur.
    public ISkeletonState Inactive { get; }
    public ISkeletonState WaitingForOrder { get; }
    public ISkeletonState MoveToKey { get; }
    public ISkeletonState PickUpKey { get; }
    public ISkeletonState MoveToDoor { get; }
    public ISkeletonState UnlockDoor { get; }
    public ISkeletonState StepBack { get; }
    public ISkeletonState Completed { get; }
    public ISkeletonState Blocked { get; }

    public SkeletonContext(
        NavMeshAgent agent,
        Animator animator,
        SkeletonInteractable interactable,
        GameObject keyObject,
        DoorInteractable door,
        SkeletonSensor sensor,
        Transform restPoint,
        float acceptRadius,
        float pickUpDuration,
        float retryDelay,
        float stuckThreshold,
        float stepBackDistance,
        float stepBackTimeout,
        bool logTransitions)
    {
        Agent = agent;
        Animator = animator;
        Interactable = interactable;
        KeyObject = keyObject;
        Door = door;
        Sensor = sensor;
        RestPoint = restPoint;
        AcceptRadius = acceptRadius;
        PickUpDuration = pickUpDuration;
        RetryDelay = retryDelay;
        StuckThreshold = stuckThreshold;
        StepBackDistance = stepBackDistance;
        StepBackTimeout = stepBackTimeout;
        LogTransitions = logTransitions;

        Inactive = new InactiveState();
        WaitingForOrder = new WaitingForOrderState();
        MoveToKey = new MoveToKeyState();
        PickUpKey = new PickUpKeyState();
        MoveToDoor = new MoveToDoorState();
        UnlockDoor = new UnlockDoorState();
        StepBack = new StepBackState();
        Completed = new CompletedState();
        Blocked = new BlockedState();
    }

    /// <summary>
    /// Ajana hedef verir. Hedef NavMesh üzerinde değilse en yakın geçerli
    /// noktaya oturtulur; hiç oturmuyorsa false döner ve çağıran durum
    /// Blocked'a geçer.
    /// </summary>
    public bool SetDestination(Vector3 target)
    {
        if (Agent == null || !Agent.isOnNavMesh) return false;

        if (!NavMesh.SamplePosition(target, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            return false;
        }

        Agent.isStopped = false;
        return Agent.SetDestination(hit.position);
    }

    /// <summary>
    /// Ajan hesaplanan yolun sonuna vardı mı.
    /// Yol kısmi (PathPartial) olsa bile ajan yolun sonuna gelir; gerçekten
    /// hedefin yanında mı olduğuna AcceptRadius ile ayrıca bakılır.
    /// </summary>
    public bool HasArrived()
    {
        if (Agent == null || Agent.pathPending) return false;
        return Agent.remainingDistance <= Agent.stoppingDistance;
    }

    /// <summary>Ajanın şu anki konumu gerçek hedefin kabul yarıçapı içinde mi.</summary>
    public bool IsNear(Vector3 target)
    {
        if (Agent == null) return false;
        Vector3 a = Agent.transform.position;
        Vector3 b = target;
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b) <= AcceptRadius;
    }

    /// <summary>Yol geçersizse (hiç yol yoksa) true döner.</summary>
    public bool PathFailed()
    {
        if (Agent == null || Agent.pathPending) return false;
        return Agent.pathStatus == NavMeshPathStatus.PathInvalid;
    }

    /// <summary>Ajanı durdurur, yürüme animasyonunu da kapatır.</summary>
    public void Stop()
    {
        if (Agent != null && Agent.isOnNavMesh)
        {
            Agent.isStopped = true;
            Agent.ResetPath();
        }
        ResetStuck();
    }

    /// <summary>
    /// Ajan hedefe gitmeye çalışırken kıpırdamıyorsa takılmış sayılır.
    /// Yürüyen durumlar her karede çağırır; varış kontrolünden SONRA
    /// çağrılmalı, yoksa hedefe varıp duran ajan takılmış görünür.
    /// </summary>
    public bool IsStuck()
    {
        if (Agent == null || !Agent.isOnNavMesh || Agent.pathPending)
        {
            stoppedSince = -1f;
            return false;
        }

        if (Agent.velocity.magnitude >= 0.05f)
        {
            stoppedSince = -1f;
            return false;
        }

        if (stoppedSince < 0f)
        {
            stoppedSince = Time.time;
            return false;
        }

        if (Time.time - stoppedSince < StuckThreshold) return false;

        stoppedSince = -1f;
        StuckCount++;
        return true;
    }

    /// <summary>
    /// İskeleti kapıya döndürür ve ajanın dönüş kontrolünü kapatır.
    ///
    /// Kapatmak şart: NavMeshAgent kendi dönüşünü sürüyor ve biz rotasyonu
    /// elle yazsak bile sonraki karede kalan hıza göre geri çeviriyor.
    /// Görev bittiği için dönüş kontrolünü bırakmasında sakınca yok.
    /// </summary>
    public void FaceDoor()
    {
        if (Agent == null || Door == null) return;

        Agent.updateRotation = false;

        Vector3 bak = Door.transform.position - Agent.transform.position;
        bak.y = 0f;
        if (bak.sqrMagnitude > 0.01f)
        {
            Agent.transform.rotation = Quaternion.LookRotation(bak.normalized);
        }
    }

    /// <summary>Takılma sayacını sıfırlar.</summary>
    public void ResetStuck()
    {
        stoppedSince = -1f;
    }

    /// <summary>
    /// Mevcut hedefe yeniden yol ister. Takılmadan sonra çağrılıyor;
    /// NavMesh aynı yolu verse bile ajan yeniden hızlanır.
    /// </summary>
    public bool Repath(Vector3 target)
    {
        RepathCount++;
        ResetStuck();
        if (Agent != null && Agent.isOnNavMesh) Agent.ResetPath();
        return SetDestination(target);
    }
}
