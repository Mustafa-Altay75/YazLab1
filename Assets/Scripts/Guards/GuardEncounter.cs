using UnityEngine;

/// <summary>
/// Kemerin önünde yolu kesen iki muhafız ve aralarındaki kısa çarpışma.
///
/// Buranın varlık sebebi bir süs değil, bir görev boşluğu: hücre kapısı ile
/// Paladin'in kapısı arasındaki yol boyunca oyuncunun yapacağı hiçbir şey
/// yoktu. Muhafızlar o aralığı dolduruyor.
///
/// Muhafızlar <b>özerk değildir</b> ve hiçbir yerde öyle anlatılmayacak.
/// Yürümezler, kovalamazlar, oyuncuyu "fark etmezler"; dururlar, yolu
/// kaparlar ve menzillerine gireni zamanlayıcıyla döverler. Projede kendi
/// yolunu kendi planlayan tek ajan iskelet.
///
/// Bu bileşen Mustafa'nın IInteractable arayüzünü uyguluyor, böylece
/// PlayerInteraction'a hiç dokunmadan onun sistemine bağlanıyor. Yaklaşınca
/// çıkan yazı bir soru değil, retorik bir laf: cevap seçeneği yok. Oyuncunun
/// yapabileceği tek şey iskeleti kılıcı almaya göndermek.
///
/// <b>Çarpışma, iskelet kılıcıyla gelene kadar başlamıyor.</b> O ana kadar
/// muhafızlar ne hasar alır ne verir. Bu bir denge ayarı değil, kurgunun
/// kendisi: hücresinden yeni çıkmış bir büyücü iki muhafızı üstlerine
/// tıklayarak çözmemeli, çözümü iskelet olmalı. Tek bayrakla yapılıyor
/// (<see cref="FightStarted"/>) ve raporda ne olduğu açıkça yazılacak —
/// senaryolanmış bir çarpışma tetiği, yani sıradan oyun tasarımı.
/// </summary>
public class GuardEncounter : MonoBehaviour, IInteractable
{
    [Header("Muhafızlar")]
    [Tooltip("Boş bırakılırsa alt objeler kullanılır.")]
    [SerializeField] private Transform[] guards;

    [Header("Yazılar")]
    [Tooltip("Retorik laf. Cevap seçeneği yok, bilerek.")]
    [SerializeField] private string tauntText = "\"Kapıya varmanın bu kadar kolay olduğunu mu sandın?\"";

    [Tooltip("Kılıç görevi bağlıyken lafın sonuna eklenir.")]
    [SerializeField] private string errandPrompt = "\n[E] İskeleti kuşanmaya gönder";

    [Tooltip("İskelet kılıcı almaya gittikten sonraki yazı.")]
    [SerializeField] private string waitingText = "\"Kemiklerini de mi çağırdın?\"";

    [Header("Hata Ayıklama")]
    [Tooltip("Sadece geliştirme için: açıkken muhafızlar yolu kesmez, oyuncu geçebilir. " +
             "Diyalog commit'ini test edebilmek için var. Teslimde kapalı olmalı.")]
    [SerializeField] private bool debugPassable;

    [SerializeField] private bool logChanges = true;

    /// <summary>Oyuncu muhafızları bir kez gördü mü. Kılıç seçeneği buna bağlanacak.</summary>
    public bool GuardsSeen { get; private set; }

    /// <summary>Muhafızlar alt edildi mi; alt edilince yol açılır.</summary>
    public bool Defeated { get; private set; }

    /// <summary>İskelet kılıcı almaya gönderildi mi.</summary>
    public bool ErrandGiven { get; private set; }

    /// <summary>Çarpışma başladı mı. Başlamadan hiçbir taraf hasar almaz/vermez.</summary>
    public bool FightStarted { get; private set; }

    /// <summary>Oyuncu iskeleti kuşanmaya göndermek istediğinde tetiklenir.</summary>
    public event System.Action<GuardEncounter, GameObject> SwordErrandRequested;

    private Health[] canlar;
    private bool uyariVerildi;

    private void Awake()
    {
        if (guards == null || guards.Length == 0) AltObjelerdenTopla();

        var liste = new System.Collections.Generic.List<Health>();
        foreach (var t in guards)
        {
            if (t == null) continue;
            var h = t.GetComponent<Health>();
            if (h == null) continue;

            // Çarpışma başlayana kadar dokunulmaz.
            h.Invulnerable = true;
            h.Died += MuhafizOldu;
            liste.Add(h);
        }
        canlar = liste.ToArray();

        if (debugPassable) GecisiAc();
    }

    private void OnDestroy()
    {
        if (canlar == null) return;
        foreach (var h in canlar) if (h != null) h.Died -= MuhafizOldu;
    }

    public string GetInteractPrompt()
    {
        if (Defeated) return string.Empty;

        // Yazının ekranda görünmesi, oyuncunun muhafızları görmesi demek.
        GuardsSeen = true;

        if (ErrandGiven) return waitingText;

        // Kılıç görevi bağlı değilse [E] gösterme; basılacak bir şey yok.
        return SwordErrandRequested != null ? tauntText + errandPrompt : tauntText;
    }

    public void Interact(GameObject interactor)
    {
        if (Defeated || ErrandGiven) return;

        if (SwordErrandRequested != null)
        {
            ErrandGiven = true;
            SwordErrandRequested(this, interactor);
            return;
        }

        if (!uyariVerildi)
        {
            uyariVerildi = true;
            Debug.LogWarning("GuardEncounter: kılıç görevi bağlı değil, [E] bir işe yaramıyor.", this);
        }
    }

    /// <summary>
    /// Çarpışmayı başlatır. İskelet kılıcıyla muhafızların yanına varınca
    /// çağrılıyor; o ana kadar iki taraf da birbirine dokunamaz.
    /// </summary>
    public void BeginFight()
    {
        if (FightStarted || Defeated) return;

        FightStarted = true;
        foreach (var h in canlar) if (h != null) h.Invulnerable = false;

        if (logChanges) Debug.Log("İskelet kılıcıyla geldi, çarpışma başladı.", this);
    }

    /// <summary>Verilen noktaya en yakın ayakta muhafız. Hepsi öldüyse null.</summary>
    public Health NearestLivingGuard(Vector3 from)
    {
        Health enIyi = null;
        float enYakin = float.MaxValue;

        foreach (var h in canlar)
        {
            if (h == null || h.IsDead) continue;

            float d = Vector3.Distance(from, h.transform.position);
            if (d >= enYakin) continue;

            enYakin = d;
            enIyi = h;
        }

        return enIyi;
    }

    /// <summary>Hepsi öldü mü.</summary>
    public bool AllDown
    {
        get
        {
            if (canlar == null || canlar.Length == 0) return false;
            foreach (var h in canlar) if (h != null && !h.IsDead) return false;
            return true;
        }
    }

    private void MuhafizOldu(Health olen)
    {
        if (logChanges) Debug.Log("Muhafız düştü: " + olen.name, this);
        if (AllDown) MarkDefeated();
    }

    /// <summary>Muhafızlar alt edildi: yol açılır ve yazı susar.</summary>
    public void MarkDefeated()
    {
        if (Defeated) return;

        Defeated = true;
        GecisiAc();

        if (logChanges) Debug.Log("Muhafızlar alt edildi, yol açıldı.", this);
    }

    /// <summary>
    /// Muhafızların gövdesini ve NavMesh engelini kapatır. İkisi birden
    /// gerekiyor: collider oyuncuyu, engel ise iskeleti durduruyor.
    /// Health da ölürken aynısını yapıyor; burası alt edilmeden açılan
    /// durumlar (hata ayıklama bayrağı) için.
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
