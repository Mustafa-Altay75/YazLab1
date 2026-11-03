using UnityEngine;

/// <summary>
/// Asadan çıkan büyü mermisi: kendi yolunu tarar, çarptığı şeye hasar verir.
///
/// Hasarı <b>mermi</b> taşıyor, oyuncu değil. Önceden vuruşu ayrı bir yakın
/// dövüş bileşeni veriyordu; menzili 2.6 birimdi, oysa mermi 50 birim
/// uçuyordu. Uzaktaki hedefe atış yapılınca mermi gidiyor ama hiçbir şey
/// olmuyordu. Artık tek bir hasar yolu var: merminin çarptığı yer.
///
/// Bu, muhafızları oyuncunun tek başına geçmesini sağlamıyor:
/// <see cref="Health.Invulnerable"/> açık olduğu sürece hasar işlenmiyor ve
/// o bayrağı yalnızca iskelet kılıcıyla varınca
/// <see cref="GuardEncounter.BeginFight"/> temizliyor.
/// </summary>
public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 22f;
    [SerializeField] private float lifeTime = 3f;

    [Tooltip("Çarptığı cana işleyen hasar.")]
    [SerializeField] private int damage = 10;

    private float timer;

    private void Update()
    {
        // 1. Yaşam süresi kontrolü
        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            Destroy(gameObject);
            return;
        }

        // 2. Bu karede ne kadar yol alacağımızı hesapla
        float moveDistance = speed * Time.deltaTime;
        Vector3 forward = transform.forward;

        // 3. Karakterin kendisini vurmamak için önümüzdeki yolu Raycast ile tara
        if (Physics.Raycast(transform.position, forward, out RaycastHit hit, moveDistance))
        {
            // Karakterin kendine veya alt parçalarına çarparsa yok say, geç
            if (hit.collider.CompareTag("Player") || hit.transform.root.CompareTag("Player") || hit.collider.isTrigger)
            {
                transform.position += forward * moveDistance;
                return;
            }

            // Canlı bir hedefe çarptıysa hasarı işle. GetComponentInParent:
            // collider çoğu zaman alt parçada, Health ise kökte duruyor.
            Health can = hit.collider.GetComponentInParent<Health>();
            if (can != null) can.TakeDamage(damage);

            // Duvara veya herhangi bir engele çarptığı an tam çarptığı yerde yok et
            transform.position = hit.point;
            Destroy(gameObject);
            return;
        }

        // Önü boşsa normal hareketine devam et
        transform.position += forward * moveDistance;
    }
}