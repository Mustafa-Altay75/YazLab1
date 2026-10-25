using UnityEngine;

/// <summary>
/// Geliştirme kısayolları: Paladin'e kadar olan bütün görevleri atlar.
///
/// <b>Sadece test içindir.</b> Diyalog üzerinde çalışırken her denemede
/// iskeleti diriltip anahtarı getirtmek, hücre kapısını açtırmak, iskeleti
/// plakaya göndermek ve muhafızları geçmek gerekiyordu; dakikalar süren bu
/// tekrar, asıl üzerinde çalışılan şeyi denemeyi zorlaştırıyordu.
///
/// Hiçbir oyun mantığı burada taklit edilmiyor. Bu bileşen yalnızca
/// nesnelerin kendi açma metotlarını çağırıyor — kapıyı normalde iskelet
/// nasıl açıyorsa bu da aynı metodu çağırıyor. Kapalıyken sahnede hiçbir
/// etkisi yok.
///
/// <b>Teslimde <see cref="active"/> kapalı olacak.</b> Tek bir anahtar
/// olmasının sebebi de bu: unutulacak ikinci bir bayrak kalmasın.
/// </summary>
public class TestShortcuts : MonoBehaviour
{
    [Tooltip("ANA ANAHTAR. Kapalıyken bu bileşen hiçbir şey yapmaz. Teslimde kapalı olmalı.")]
    [SerializeField] private bool active;

    [Header("Neler atlansın")]
    [Tooltip("Hücre kapısını baştan açar; iskeleti diriltip anahtar görevini yürütmek gerekmez.")]
    [SerializeField] private bool openCellDoor = true;

    [Tooltip("Muhafızları alt edilmiş sayar; kılıç görevi ve çarpışma gerekmez.")]
    [SerializeField] private bool clearGuards = true;

    [Tooltip("Paladin'in kapısını baştan açar; iskeleti plakaya göndermek gerekmez.")]
    [SerializeField] private bool openPlateGate = true;

    [Tooltip("Asayı ve büyü kitabını verir; sandığı açmak gerekmez.")]
    [SerializeField] private bool equipMageGear = true;

    [Tooltip("İskeleti diriltir ve emir bekler hâle getirir; diriltme ve anahtar görevi gerekmez.")]
    [SerializeField] private bool raiseSkeleton = true;

    [Header("Bağlantılar")]
    [Tooltip("Boş bırakılanlar sahneden aranır.")]
    [SerializeField] private DoorInteractable cellDoor;
    [SerializeField] private GuardEncounter guards;
    [SerializeField] private PlateGate gate;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private SkeletonInteractable skeleton;
    [SerializeField] private SkeletonCompanion companion;

    private System.Collections.IEnumerator Start()
    {
        if (!active) yield break;

        // Bir kare bekle. Start sırası garanti değil ve PlayerInventory de
        // kendi Start'ında asayı gizliyor: aynı karede kuşandırırsak onun
        // Start'ı sonra çalışıp eşyayı geri kapatabiliyor. Ölçüldü, oldu.
        yield return null;

        if (openCellDoor)
        {
            if (cellDoor == null) cellDoor = FindAnyObjectByType<DoorInteractable>();
            if (cellDoor != null) cellDoor.OpenDoor();
        }

        if (clearGuards)
        {
            if (guards == null) guards = FindAnyObjectByType<GuardEncounter>();
            if (guards != null) guards.MarkDefeated();
        }

        if (openPlateGate)
        {
            if (gate == null) gate = FindAnyObjectByType<PlateGate>();
            if (gate != null) gate.OpenGate();
        }

        if (equipMageGear)
        {
            if (inventory == null) inventory = FindAnyObjectByType<PlayerInventory>();
            if (inventory != null) { inventory.CollectKey(); inventory.EquipMageGear(); }
        }

        // İskelet en sonda: diriltme asa ve kitap gerektiriyor, yani
        // equipMageGear'dan sonra çalışmalı. Diriltme oyunun kendi
        // yolundan yapılıyor — SkeletonInteractable.Interact — ki animasyon
        // ve IsResurrected normal oynanıştakiyle birebir aynı olsun.
        if (raiseSkeleton)
        {
            if (skeleton == null) skeleton = FindAnyObjectByType<SkeletonInteractable>();
            if (companion == null && skeleton != null) companion = skeleton.GetComponent<SkeletonCompanion>();

            if (skeleton != null && !skeleton.IsResurrected && inventory != null)
            {
                skeleton.Interact(inventory.gameObject);
            }

            // Diriltmek tek başına yetmiyor: iskelet "emir bekler" duruma
            // normalde anahtar görevini bitirince geliyor ve takip, plaka,
            // altın emirlerinin hepsi o durumdan dallanıyor.
            if (companion != null) companion.SkipKeyErrand();
        }

        Debug.LogWarning("TestShortcuts AÇIK: görevler atlandı. Teslimde kapatılmalı.", this);
    }
}
