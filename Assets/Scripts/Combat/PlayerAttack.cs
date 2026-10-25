using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Oyuncunun vuruşunun hasara dönüşmesi.
///
/// Saldırı <b>girdisi</b> ve animasyonu zaten PlayerController'da var ama
/// hiçbir şeye hasar vermiyordu. Bu bileşen sadece eksik olan parçayı
/// ekliyor. PlayerController'a dokunulmadı: Mustafa'nın dosyası, ve aynı
/// tuşu burada da okumak onun scriptini değiştirmekten daha az riskli.
/// İskeletin takip tuşunda da aynı yol izlenmişti.
///
/// Vuruş bir koni: önde, <see cref="attackRange"/> yarıçapında ve
/// <see cref="attackAngle"/> açısı içinde kalan en yakın cana işliyor.
/// Nişan almak gerekmesin diye koni geniş; bu bir dövüş oyunu değil,
/// boş bir koridoru dolduran kısa bir çarpışma.
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    [Header("Vuruş")]
    [Tooltip("Bir vuruşun verdiği hasar.")]
    [SerializeField] private int damage = 10;

    [Tooltip("Vuruşun erişimi (birim).")]
    [SerializeField] private float attackRange = 2.6f;

    [Tooltip("Önden sapma toleransı (derece). Tam nişan gerekmesin diye geniş.")]
    [SerializeField] private float attackAngle = 80f;

    [Tooltip("İki vuruş arasındaki en kısa süre (saniye). Animasyonla uyumlu olmalı.")]
    [SerializeField] private float cooldown = 0.6f;

    [Header("Koşullar")]
    [Tooltip("Boş bırakılırsa aynı objedeki PlayerInventory kullanılır.")]
    [SerializeField] private PlayerInventory inventory;

    [Tooltip("Açıkken asa kuşanılmadan vurulamaz. Sandıktan asayı almak böylece gerçekten bir şeye yarıyor.")]
    [SerializeField] private bool requireArmed = true;

    [Header("Hata Ayıklama")]
    [SerializeField] private bool logHits = true;

    private float sonrakiVurus;

    private void Awake()
    {
        if (inventory == null) inventory = GetComponent<PlayerInventory>();
    }

    private void Update()
    {
        // Diyalog kutusuna yazarken boşluk tuşu saldırı başlatmasın.
        if (InputLock.Captured) return;

        if (!GirdiVar()) return;
        if (Time.time < sonrakiVurus) return;
        if (requireArmed && inventory != null && !inventory.IsArmed) return;

        sonrakiVurus = Time.time + cooldown;
        Vur();
    }

    /// <summary>PlayerController ile aynı girdi: sol tık veya boşluk.</summary>
    private bool GirdiVar()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
    }

    private void Vur()
    {
        Health hedef = EnYakinHedef();
        if (hedef == null) return;

        if (!hedef.TakeDamage(damage)) return;

        if (logHits)
        {
            Debug.Log("Oyuncu vurdu: " + hedef.name + " (" + hedef.Current + "/" + hedef.Max + ")", this);
        }
    }

    /// <summary>
    /// Koninin içindeki en yakın can. Kendi canını (varsa) eler, ölmüş
    /// olanı atlar; yoksa cesede vurmaya devam ederdi.
    /// </summary>
    private Health EnYakinHedef()
    {
        Health enIyi = null;
        float enYakin = float.MaxValue;

        foreach (var c in Physics.OverlapSphere(transform.position, attackRange))
        {
            var h = c.GetComponentInParent<Health>();
            if (h == null || h.IsDead || h.gameObject == gameObject) continue;

            Vector3 yon = h.transform.position - transform.position;
            yon.y = 0f;
            if (yon.sqrMagnitude < 0.0001f) continue;
            if (Vector3.Angle(transform.forward, yon) > attackAngle * 0.5f) continue;

            float d = yon.magnitude;
            if (d >= enYakin) continue;

            enYakin = d;
            enIyi = h;
        }

        return enIyi;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
