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

    // --- Ayarlar ---
    /// <summary>Hedefe bu mesafeden yakınsa "vardı" sayılır.</summary>
    public float AcceptRadius { get; }

    /// <summary>Anahtarı alma animasyonu için beklenecek süre (saniye).</summary>
    public float PickUpDuration { get; }

    /// <summary>Yol bulunamadığında yeniden denemeden önce beklenecek süre (saniye).</summary>
    public float RetryDelay { get; }

    /// <summary>Durum geçişleri konsola yazılsın mı.</summary>
    public bool LogTransitions { get; }

    // --- Çalışma anında değişen veri ---
    /// <summary>Anahtar alındı mı.</summary>
    public bool HasKey { get; set; }

    /// <summary>Oyuncu "anahtarı getir" emrini verdi mi.</summary>
    public bool OrderGiven { get; set; }

    // --- Durum örnekleri ---
    // Her karede yeni nesne üretmemek için durumlar bir kez oluşturulur.
    public ISkeletonState Inactive { get; }
    public ISkeletonState WaitingForOrder { get; }
    public ISkeletonState MoveToKey { get; }
    public ISkeletonState PickUpKey { get; }
    public ISkeletonState MoveToDoor { get; }
    public ISkeletonState UnlockDoor { get; }
    public ISkeletonState Completed { get; }
    public ISkeletonState Blocked { get; }

    public SkeletonContext(
        NavMeshAgent agent,
        Animator animator,
        SkeletonInteractable interactable,
        GameObject keyObject,
        DoorInteractable door,
        float acceptRadius,
        float pickUpDuration,
        float retryDelay,
        bool logTransitions)
    {
        Agent = agent;
        Animator = animator;
        Interactable = interactable;
        KeyObject = keyObject;
        Door = door;
        AcceptRadius = acceptRadius;
        PickUpDuration = pickUpDuration;
        RetryDelay = retryDelay;
        LogTransitions = logTransitions;

        Inactive = new InactiveState();
        WaitingForOrder = new WaitingForOrderState();
        MoveToKey = new MoveToKeyState();
        PickUpKey = new PickUpKeyState();
        MoveToDoor = new MoveToDoorState();
        UnlockDoor = new UnlockDoorState();
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
    }
}
