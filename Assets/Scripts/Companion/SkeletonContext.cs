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

    // --- İkinci görev: basınç plakası ---
    /// <summary>İskeletin üstünde duracağı basınç plakası.</summary>
    public Transform PlatePoint { get; private set; }

    /// <summary>Takip modunda arkasında yürünecek oyuncu.</summary>
    public Transform Player { get; private set; }

    /// <summary>Takip ederken oyuncuya bu mesafeden yakınsa durur.</summary>
    public float FollowDistance { get; private set; }

    /// <summary>Takipte hedef bu aralıkla yenilenir; her karede değil.</summary>
    public float FollowRepathInterval { get; private set; }

    // --- Üçüncü görev: kılıç ve muhafızlar ---
    /// <summary>Masanın üstündeki kılıç. Alınınca gizleniyor.</summary>
    public GameObject SwordObject { get; private set; }

    /// <summary>İskeletin elindeki kılıç. Başta kapalı, alınınca açılıyor.</summary>
    public GameObject SwordInHand { get; private set; }

    /// <summary>Muhafızları ve çarpışmayı yöneten bileşen.</summary>
    public GuardEncounter Encounter { get; private set; }

    /// <summary>Muhafıza bu mesafeden yakınsa vurabilir.</summary>
    public float AttackRange { get; private set; }

    /// <summary>İskeletin bir vuruşunun hasarı.</summary>
    public int AttackDamage { get; private set; }

    /// <summary>İki vuruş arası süre (saniye).</summary>
    public float AttackCooldown { get; private set; }

    // --- Dördüncü görev: rüşvet altınları ---
    /// <summary>Toplanabilecek altınlar. Alınanlar kapatılıyor.</summary>
    public GameObject[] Coins { get; private set; }

    /// <summary>Altınların bırakılacağı nokta: Paladin'in kapısının önü.</summary>
    public Transform CoinDropPoint { get; private set; }

    /// <summary>Kaç altın toplanacak.</summary>
    public int CoinsNeeded { get; private set; }

    /// <summary>Şu ana kadar toplanan.</summary>
    public int CoinsCollected { get; private set; }

    /// <summary>Altınlar kapının önüne bırakıldı, oyuncu alabilir.</summary>
    public bool CoinsReady { get; set; }

    /// <summary>Oyuncu "altınları getir" emrini verdi mi.</summary>
    public bool CoinOrderGiven { get; set; }

    /// <summary>
    /// İşi bitince çekileceği nokta. Paladin'in dibinde beklerse
    /// oyuncunun etkileşim menzilini kapatıyor.
    /// </summary>
    public Transform RetreatPoint { get; private set; }

    // --- Çalışma anında değişen veri ---
    /// <summary>Anahtar alındı mı.</summary>
    public bool HasKey { get; set; }

    /// <summary>Oyuncu "anahtarı getir" emrini verdi mi.</summary>
    public bool OrderGiven { get; set; }

    /// <summary>Anahtar görevi tamamlandı mı. Takip modu buna bağlı.</summary>
    public bool KeyErrandDone { get; set; }

    /// <summary>Oyuncu takip modunu açtı mı.</summary>
    public bool FollowRequested { get; set; }

    /// <summary>Oyuncu "plakaya git" emrini verdi mi.</summary>
    public bool PlateOrderGiven { get; set; }

    /// <summary>Oyuncu "kılıcı al" emrini verdi mi.</summary>
    public bool SwordOrderGiven { get; set; }

    /// <summary>Kılıç alındı mı.</summary>
    public bool Armed { get; set; }

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
    public ISkeletonState Following { get; }
    public ISkeletonState MoveToPlate { get; }
    public ISkeletonState StandOnPlate { get; }
    public ISkeletonState MoveToSword { get; }
    public ISkeletonState PickUpSword { get; }
    public ISkeletonState MoveToThreat { get; }
    public ISkeletonState AttackThreat { get; }
    public ISkeletonState MoveToCoins { get; }
    public ISkeletonState CollectCoin { get; }
    public ISkeletonState ReturnToPaladin { get; }
    public ISkeletonState WaitingWithCoins { get; }
    public ISkeletonState Retreat { get; }

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
        Following = new FollowingState();
        MoveToPlate = new MoveToPlateState();
        StandOnPlate = new StandOnPlateState();
        MoveToSword = new MoveToSwordState();
        PickUpSword = new PickUpSwordState();
        MoveToThreat = new MoveToThreatState();
        AttackThreat = new AttackThreatState();
        MoveToCoins = new MoveToCoinsState();
        CollectCoin = new CollectCoinState();
        ReturnToPaladin = new ReturnToPaladinState();
        WaitingWithCoins = new WaitingWithCoinsState();
        Retreat = new RetreatState();
    }

    /// <summary>
    /// İkinci görevin referanslarını bağlar. Ayrı bir metot, çünkü bunlar
    /// hücre kapısı açılana kadar kullanılmıyor ve kurucuyu daha da
    /// uzatmanın okunurluğa faydası yok.
    /// </summary>
    public void AttachPlateTask(Transform plate, Transform player,
                                float followDistance, float followRepathInterval)
    {
        PlatePoint = plate;
        Player = player;
        FollowDistance = followDistance;
        FollowRepathInterval = followRepathInterval;
    }

    /// <summary>
    /// Kılıç görevinin referanslarını bağlar. AttachPlateTask ile aynı
    /// gerekçe: bu referanslar oyunun geç bir aşamasına ait ve kurucuyu
    /// daha da uzatmanın okunurluğa faydası yok.
    /// </summary>
    public void AttachSwordTask(GameObject sword, GameObject swordInHand, GuardEncounter encounter,
                                float attackRange, int attackDamage, float attackCooldown)
    {
        SwordObject = sword;
        SwordInHand = swordInHand;
        Encounter = encounter;
        AttackRange = attackRange;
        AttackDamage = attackDamage;
        AttackCooldown = attackCooldown;
    }

    /// <summary>
    /// Rüşvet görevinin referanslarını bağlar.
    /// </summary>
    public void AttachCoinTask(GameObject[] coins, Transform dropPoint, Transform retreatPoint, int needed)
    {
        Coins = coins;
        CoinDropPoint = dropPoint;
        RetreatPoint = retreatPoint;
        CoinsNeeded = needed;
    }

    /// <summary>
    /// Henüz alınmamış, iskelete en yakın altın. Hepsi alındıysa null.
    ///
    /// Sırayı mesafe belirliyor, sahnedeki dizilim değil: iskelet nereden
    /// gelirse gelsin makul bir yol izlesin.
    /// </summary>
    public GameObject NextCoin()
    {
        if (Coins == null || Agent == null) return null;

        GameObject enIyi = null;
        float enYakin = float.MaxValue;
        Vector3 benim = Agent.transform.position;

        foreach (var c in Coins)
        {
            if (c == null || !c.activeInHierarchy) continue;

            float d = Vector3.Distance(benim, c.transform.position);
            if (d >= enYakin) continue;

            enYakin = d;
            enIyi = c;
        }

        return enIyi;
    }

    /// <summary>En yakın altını alır ve sayacı artırır.</summary>
    public void TakeCoin()
    {
        var c = NextCoin();
        if (c == null) return;

        c.SetActive(false);
        CoinsCollected++;
    }

    /// <summary>
    /// O an ayakta olan en yakın muhafız. Kimin ayakta olduğunu bilen yer
    /// GuardEncounter; durumlar o listeyi kendileri tutmuyor.
    /// </summary>
    public Health NearestThreat()
    {
        if (Encounter == null || Agent == null) return null;
        return Encounter.NearestLivingGuard(Agent.transform.position);
    }

    /// <summary>Hedef vuruş menzilinde mi (yatay mesafe).</summary>
    public bool WithinAttackRange(Health hedef)
    {
        if (hedef == null || Agent == null) return false;

        Vector3 a = Agent.transform.position;
        Vector3 b = hedef.transform.position;
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b) <= AttackRange;
    }

    /// <summary>
    /// Çarpışma bitti: iskelet oyuncunun yanına döner. Takip bayrağı
    /// burada açılıyor, çünkü iskeleti muhafızların yanında bırakmak
    /// oyuncuyu geri yürüyüp onu çağırmaya zorlardı.
    /// </summary>
    public ISkeletonState FightOver()
    {
        SwordOrderGiven = false;
        FollowRequested = true;
        return Following;
    }

    /// <summary>Hedefe döner. Ajanın dönüş kontrolü varken de çalışsın diye doğrudan yazılıyor.</summary>
    public void FaceTarget(Vector3 hedef)
    {
        if (Agent == null) return;

        Vector3 yon = hedef - Agent.transform.position;
        yon.y = 0f;
        if (yon.sqrMagnitude < 0.0001f) return;

        Agent.transform.rotation = Quaternion.LookRotation(yon.normalized);
    }

    /// <summary>Vuruş animasyonunu tetikler. Parametre yoksa sessizce geçer.</summary>
    public void TriggerAttackAnimation()
    {
        if (Animator == null || Animator.runtimeAnimatorController == null) return;

        foreach (var p in Animator.parameters)
        {
            if (p.name != "Attack") continue;
            Animator.SetTrigger("Attack");
            return;
        }
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
