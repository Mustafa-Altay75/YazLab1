using UnityEngine;

/// <summary>
/// Can. Hem muhafızlarda hem oyuncuda aynı bileşen kullanılıyor; fark
/// sadece ayarlarda (oyuncu ölmez, muhafız ölür).
///
/// Hasarın <b>kime</b> verileceğine burası karar vermez, sadece hasarı
/// kabul eder ya da reddeder. Vuran taraf (oyuncu, muhafız) ayrı bir
/// bileşen; böylece ileride başka bir şey de vurabilir ve burada
/// değişiklik gerekmez.
///
/// <see cref="Invulnerable"/> açıkken hiçbir hasar işlenmiyor. Muhafızlar
/// çarpışma başlayana kadar bu bayrakla korunuyor: oyuncu hücresinden
/// yeni çıkmış bir büyücü, iki muhafızı üstlerine tıklayarak çözebilmemeli.
/// Kararı veren yer GuardEncounter; bu sınıf sadece uyguluyor.
/// </summary>
public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 30;

    [Tooltip("Kapalıysa can sıfıra inse de ölmez. Oyuncuda kapalı: oyunun kaybetme durumu yok.")]
    [SerializeField] private bool canDie = true;

    [Header("Animasyon")]
    [Tooltip("Boş bırakılırsa alt objelerdeki Animator kullanılır.")]
    [SerializeField] private Animator animator;
    [SerializeField] private string hitTrigger = "Hit";
    [SerializeField] private string deathTrigger = "Death";

    [Header("Ölünce")]
    [Tooltip("Ceset yolu kapatmaya devam etmesin diye collider ve NavMesh engeli kapatılır.")]
    [SerializeField] private bool clearBlockersOnDeath = true;

    /// <summary>Başlangıç canı.</summary>
    public int Max { get { return maxHealth; } }

    /// <summary>Kalan can.</summary>
    public int Current { get; private set; }

    /// <summary>Öldü mü.</summary>
    public bool IsDead { get; private set; }

    /// <summary>Açıkken hiçbir hasar işlenmez.</summary>
    public bool Invulnerable { get; set; }

    /// <summary>Hasar alındığında tetiklenir (kaynak, miktar).</summary>
    public event System.Action<Health, int> Damaged;

    /// <summary>Ölüm anında bir kez tetiklenir.</summary>
    public event System.Action<Health> Died;

    private bool vurusVar, olumVar;

    private void Awake()
    {
        Current = maxHealth;
        if (animator == null) animator = GetComponentInChildren<Animator>();
        vurusVar = ParametreVar(hitTrigger);
        olumVar = ParametreVar(deathTrigger);
    }

    /// <summary>
    /// Hasar uygular. Gerçekten işlendiyse true döner; vuran taraf bunu
    /// kullanarak boşa giden vuruşu sayabilir.
    /// </summary>
    public bool TakeDamage(int amount)
    {
        if (IsDead || Invulnerable || amount <= 0) return false;

        Current = Mathf.Max(0, Current - amount);

        if (Damaged != null) Damaged(this, amount);

        if (Current > 0 || !canDie)
        {
            Tetikle(hitTrigger, vurusVar);
            return true;
        }

        Ol();
        return true;
    }

    private void Ol()
    {
        IsDead = true;
        Tetikle(deathTrigger, olumVar);

        if (clearBlockersOnDeath)
        {
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            foreach (var o in GetComponentsInChildren<UnityEngine.AI.NavMeshObstacle>()) o.enabled = false;
        }

        if (Died != null) Died(this);
    }

    private void Tetikle(string ad, bool varMi)
    {
        if (!varMi || animator == null || string.IsNullOrEmpty(ad)) return;
        animator.SetTrigger(ad);
    }

    private bool ParametreVar(string ad)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        if (string.IsNullOrEmpty(ad)) return false;

        foreach (var p in animator.parameters) if (p.name == ad) return true;
        return false;
    }
}
