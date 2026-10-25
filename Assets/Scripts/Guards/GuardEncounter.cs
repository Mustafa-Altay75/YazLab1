using UnityEngine;

/// <summary>
/// Kemerin önünde yolu kesen iki muhafız.
///
/// Buranın varlık sebebi bir süs değil, bir görev boşluğu: hücre kapısı ile
/// Paladin'in kapısı arasındaki yol boyunca oyuncunun yapacağı hiçbir şey
/// yoktu. Muhafızlar o aralığı dolduruyor.
///
/// Muhafızlar <b>özerk değildir</b> ve hiçbir yerde öyle anlatılmayacak.
/// Yürümezler, kovalamazlar, oyuncuyu "fark etmezler"; sadece dururlar ve
/// geçişi kapatırlar. Projede kendi yolunu kendi planlayan tek ajan iskelet.
///
/// Bu bileşen Mustafa'nın IInteractable arayüzünü uyguluyor, böylece
/// PlayerInteraction'a hiç dokunmadan onun sistemine bağlanıyor. Yaklaşınca
/// çıkan yazı bir soru değil, retorik bir laf: cevap seçeneği yok. Oyuncunun
/// yapabileceği tek şey iskeleti kılıcı almaya göndermek.
///
/// Konuşmayı ve dövüşü bu bileşen YÜRÜTMEZ, sadece istendiğini duyurur —
/// PaladinNpc ile aynı desen. Kılıç görevi ve çarpışma ayrı bir commit'te
/// gelecek; o zaman SwordErrandRequested olayına abone olunacak.
/// </summary>
public class GuardEncounter : MonoBehaviour, IInteractable
{
    [Header("Muhafızlar")]
    [Tooltip("Boş bırakılırsa alt objelerdeki collider'lar kullanılır.")]
    [SerializeField] private Transform[] guards;

    [Header("Yazılar")]
    [Tooltip("Retorik laf. Cevap seçeneği yok, bilerek.")]
    [SerializeField] private string tauntText = "\"Kapıya varmanın bu kadar kolay olduğunu mu sandın?\"";

    [Tooltip("Kılıç görevi bağlandığında lafın sonuna eklenir.")]
    [SerializeField] private string errandPrompt = "     [E] İskeleti kuşanmaya gönder";

    [Header("Hata Ayıklama")]
    [Tooltip("Sadece geliştirme için: açıkken muhafızlar yolu kesmez, oyuncu geçebilir. " +
             "Çarpışma eklenene kadar Paladin'in odasını test edebilmek için var. Teslimde kapalı olmalı.")]
    [SerializeField] private bool debugPassable;

    /// <summary>Oyuncu muhafızları bir kez gördü mü. Kılıç seçeneği buna bağlanacak.</summary>
    public bool GuardsSeen { get; private set; }

    /// <summary>Muhafızlar alt edildi mi; alt edilince yol açılır.</summary>
    public bool Defeated { get; private set; }

    /// <summary>Oyuncu iskeleti kuşanmaya göndermek istediğinde tetiklenir.</summary>
    public event System.Action<GuardEncounter, GameObject> SwordErrandRequested;

    private bool uyariVerildi;

    private void Awake()
    {
        if (guards == null || guards.Length == 0) AltObjelerdenTopla();

        // Geliştirme bayrağı: yolu baştan aç. Defeated işaretlenmiyor, çünkü
        // muhafızlar alt edilmiş değil; sadece önümüzden çekilmiş oluyorlar.
        if (debugPassable) GecisiAc();
    }

    public string GetInteractPrompt()
    {
        if (Defeated) return string.Empty;

        // Yazının ekranda görünmesi, oyuncunun muhafızları görmesi demek.
        GuardsSeen = true;

        // Kılıç görevi henüz bağlı değilse [E] gösterme; basılacak bir şey yok.
        return SwordErrandRequested != null ? tauntText + errandPrompt : tauntText;
    }

    public void Interact(GameObject interactor)
    {
        if (Defeated) return;

        if (SwordErrandRequested != null)
        {
            SwordErrandRequested(this, interactor);
            return;
        }

        // Sessizce yutmak yerine bir kere uyar: bu durum eksik kurulumun işareti.
        if (!uyariVerildi)
        {
            uyariVerildi = true;
            Debug.LogWarning("GuardEncounter: kılıç görevi bağlı değil, [E] bir işe yaramıyor.", this);
        }
    }

    /// <summary>
    /// Muhafızlar alt edilince çağrılır: yol açılır ve yazı susar.
    /// Çarpışma commit'i buraya bağlanacak.
    /// </summary>
    public void MarkDefeated()
    {
        if (Defeated) return;
        Defeated = true;
        GecisiAc();
    }

    /// <summary>
    /// Muhafızların gövdesini ve NavMesh engelini kapatır. İkisi birden
    /// gerekiyor: collider oyuncuyu, engel ise iskeleti durduruyor.
    /// </summary>
    private void GecisiAc()
    {
        foreach (var t in guards)
        {
            if (t == null) continue;

            foreach (var c in t.GetComponentsInChildren<Collider>()) c.enabled = false;
            foreach (var o in t.GetComponentsInChildren<UnityEngine.AI.NavMeshObstacle>()) o.enabled = false;
        }
    }

    private void AltObjelerdenTopla()
    {
        var liste = new System.Collections.Generic.List<Transform>();
        foreach (Transform t in transform) liste.Add(t);
        guards = liste.ToArray();
    }
}
