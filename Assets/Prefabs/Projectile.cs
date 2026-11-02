using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 22f;
    [SerializeField] private float lifeTime = 3f;

    private float timer;

    private void Update()
    {
        // 1. Yaþam süresi kontrolü
        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            Destroy(gameObject);
            return;
        }

        // 2. Bu karede ne kadar yol alacaðýmýzý hesapla
        float moveDistance = speed * Time.deltaTime;
        Vector3 forward = transform.forward;

        // 3. Karakterin kendisini vurmamak için önümüzdeki yolu Raycast ile tara
        if (Physics.Raycast(transform.position, forward, out RaycastHit hit, moveDistance))
        {
            // Karakterin kendine veya alt parçalarýna çarparsa yok say, geç
            if (hit.collider.CompareTag("Player") || hit.transform.root.CompareTag("Player") || hit.collider.isTrigger)
            {
                transform.position += forward * moveDistance;
                return;
            }

            // Duvara veya herhangi bir engele çarptýðý an tam çarptýðý yerde yok et
            transform.position = hit.point;
            Destroy(gameObject);
            return;
        }

        // Önü boþsa normal hareketine devam et
        transform.position += forward * moveDistance;
    }
}