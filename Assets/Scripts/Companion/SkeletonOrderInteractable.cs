using UnityEngine;

/// <summary>
/// Oyuncunun iskelete emir vermesini sağlayan etkileşim noktası.
///
/// Mustafa'nın IInteractable arayüzünü uyguluyor; böylece iskelet de not,
/// sandık ve kapı gibi sıradan bir etkileşim nesnesi oluyor ve
/// PlayerInteraction'a hiç dokunmadan onun sistemine bağlanıyor.
///
/// Yazı artık sabit değil, iskeletin o anki görevine göre değişiyor:
/// hücre kapısından önce anahtar, sonra plaka. İkinci görevde takip modu
/// da devreye girdiği için F tuşu da burada duyuruluyor.
///
/// "Plakaya git" seçeneği oyuncu kapıyı bir kez görene kadar hiç
/// gösterilmiyor. Yoksa oyuncu daha kapıdan haberi yokken iskeleti
/// yanlışlıkla yollayabilir ve bulmacanın ne işe yaradığını anlamadan
/// geçer. Kapı görüldükten sonra emir her yerden verilebiliyor.
///
/// ÖNEMLİ: Bu bileşen Skeleton_Minion'un KENDİSİNDE değil, kendi collider'ı
/// olan bir alt objede durmalı. Çünkü PlayerInteraction bir collider için
/// GetComponentInParent ile TEK bir IInteractable çözüyor; aynı objede
/// SkeletonInteractable zaten var ve ikincisi gölgede kalırdı.
/// </summary>
public class SkeletonOrderInteractable : MonoBehaviour, IInteractable
{
    [Tooltip("Boş bırakılırsa üst objelerde aranır.")]
    [SerializeField] private SkeletonCompanion companion;

    [Tooltip("Boş bırakılırsa üst objelerde aranır.")]
    [SerializeField] private SkeletonInteractable interactable;

    [Header("Yazılar")]
    [SerializeField] private string keyErrandPrompt = "[E] Anahtarı getir";
    [SerializeField] private string followPrompt = "[F] Beni takip et";
    [SerializeField] private string waitPrompt = "[F] Burada bekle";
    [SerializeField] private string plateFollowingPrompt = "[E] Plakaya git\n[F] Burada bekle";
    [SerializeField] private string plateIdlePrompt = "[E] Plakaya git\n[F] Beni takip et";

    private void Awake()
    {
        if (companion == null) companion = GetComponentInParent<SkeletonCompanion>();
        if (interactable == null) interactable = GetComponentInParent<SkeletonInteractable>();
    }

    public string GetInteractPrompt()
    {
        // Diriltilmeden emir verilemez; o aşamada söz SkeletonInteractable'da.
        if (interactable == null || !interactable.IsResurrected) return string.Empty;
        if (companion == null) return string.Empty;

        // Birinci görev: anahtar.
        if (!companion.KeyErrandDone)
        {
            return companion.OrderGiven ? string.Empty : keyErrandPrompt;
        }

        // Kılıç görevi sürerken iskelete başka emir verilmez; yolda
        // çevirmek onu yarı yolda bırakırdı.
        if (companion.SwordOrderGiven) return string.Empty;

        // İkinci görev: plaka. Plaka sahnede bağlı değilse gösterilecek bir şey yok.
        if (!companion.HasPlateTask) return string.Empty;
        if (companion.PlateOrderGiven) return string.Empty;

        // Kapı henüz görülmediyse plaka diye bir şey bilmiyoruz; sadece
        // iskeleti yanımızda tutmayı teklif et.
        if (!companion.GateSeen)
        {
            return companion.IsFollowing ? waitPrompt : followPrompt;
        }

        return companion.IsFollowing ? plateFollowingPrompt : plateIdlePrompt;
    }

    public void Interact(GameObject interactor)
    {
        if (interactable == null || !interactable.IsResurrected) return;
        if (companion == null) return;

        if (!companion.KeyErrandDone)
        {
            if (!companion.OrderGiven) companion.GiveOrder();
            return;
        }

        if (companion.SwordOrderGiven) return;

        // Anahtar görevi bittiyse E artık plakaya gönderiyor. Takip
        // modunda olması şart değil; oyuncu iskeletin yanındaysa oradan da
        // gönderebilsin. Kapı görülmediyse OrderToPlate zaten hiçbir şey
        // yapmıyor, yazıda da E görünmüyor.
        companion.OrderToPlate();
    }
}
