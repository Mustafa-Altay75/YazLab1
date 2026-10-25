using UnityEngine;

/// <summary>
/// Muhafızın vuruşu. <b>Kasten ilkel.</b>
///
/// Yürümüyor, kovalamıyor, hedef seçmiyor, geri çekilmiyor. Durduğu
/// yerden, menzile giren oyuncuya belli aralıklarla bir kez vuruyor. Bunun
/// adı düşman yapay zekâsı değil; zamanlayıcılı bir dekordur ve raporda da
/// böyle anlatılacak. Projede kendi yolunu kendi planlayan tek ajan
/// iskelet; o iddiayı bulandıracak hiçbir şey buraya girmemeli.
///
/// Vuruş, çarpışma başlamadan hiç çalışmıyor: <see cref="GuardEncounter"/>
/// iskelet kılıcıyla gelene kadar sayaç dönmüyor. Böylece oyuncu
/// muhafızlara yaklaşır, laf işitir, ama dayak yemez.
/// </summary>
public class GuardCombat : MonoBehaviour
{
    [Header("Bağlantılar")]
    [Tooltip("Boş bırakılırsa üst objelerde aranır.")]
    [SerializeField] private GuardEncounter encounter;

    [Tooltip("Boş bırakılırsa aynı objedeki Health kullanılır.")]
    [SerializeField] private Health health;

    [Tooltip("Vurulacak hedefin canı. Boş bırakılırsa sahnedeki PlayerInventory'nin canı aranır.")]
    [SerializeField] private Health target;

    [Tooltip("Boş bırakılırsa aynı objedeki Animator kullanılır.")]
    [SerializeField] private Animator animator;

    [Header("Vuruş")]
    [Tooltip("Bir vuruşun verdiği hasar. Oyuncuyu yormak için değil, çarpışmayı hissettirmek için.")]
    [SerializeField] private int damage = 1;

    [Tooltip("Hedef bu mesafeden yakınsa vurur (birim).")]
    [SerializeField] private float reach = 2.4f;

    [Tooltip("İki vuruş arasındaki süre (saniye).")]
    [SerializeField] private float cooldown = 1.6f;

    [SerializeField] private string attackTrigger = "Attack";

    private float sonrakiVurus;
    private bool tetikVar;

    private void Awake()
    {
        if (encounter == null) encounter = GetComponentInParent<GuardEncounter>();
        if (health == null) health = GetComponent<Health>();
        if (animator == null) animator = GetComponent<Animator>();

        if (target == null)
        {
            var inv = FindAnyObjectByType<PlayerInventory>();
            if (inv != null) target = inv.GetComponent<Health>();
        }

        tetikVar = ParametreVar(attackTrigger);
    }

    private void Update()
    {
        if (encounter == null || !encounter.FightStarted) return;
        if (health != null && health.IsDead) return;
        if (target == null || target.IsDead) return;
        if (Time.time < sonrakiVurus) return;

        Vector3 fark = target.transform.position - transform.position;
        fark.y = 0f;
        if (fark.magnitude > reach) return;

        sonrakiVurus = Time.time + cooldown;

        // Yerinden kıpırdamıyor; sadece hedefe dönüyor ki vuruş saçma durmasın.
        if (fark.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(fark.normalized);

        if (tetikVar && animator != null) animator.SetTrigger(attackTrigger);
        target.TakeDamage(damage);
    }

    private bool ParametreVar(string ad)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        foreach (var p in animator.parameters) if (p.name == ad) return true;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, reach);
    }
}
