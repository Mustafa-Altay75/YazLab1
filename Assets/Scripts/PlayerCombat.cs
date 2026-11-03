using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float attackCooldown = 0.25f;
    [SerializeField] private float maxAimDistance = 50f;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerInventory inventory;

    private float lastAttackTime;
    private Camera mainCamera;
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (inventory == null) inventory = GetComponent<PlayerInventory>();
        mainCamera = Camera.main;
    }

    private void Update()
    {
        // Envanter kontrolü: Asa alınmadıysa büyü atılmaz
        if (inventory != null && !inventory.IsArmed) return;

        // Sol tık kontrolü
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                Shoot();
                lastAttackTime = Time.time;
            }
        }
    }

    private void Shoot()
    {
        if (projectilePrefab == null) return;

        // Animasyonu tetikle
        if (animator != null)
        {
            animator.ResetTrigger(AttackHash);
            animator.SetTrigger(AttackHash);
        }

        if (mainCamera == null) mainCamera = Camera.main;

        // 1. Ekranın tam ortasından (Crosshair) düz bir ışın at
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint;

        // Karakterin kendi gövdesini vurmamak için RaycastAll ile tara
        RaycastHit[] hits = Physics.RaycastAll(ray, maxAimDistance);
        RaycastHit closestValidHit = default;
        float closestDist = float.MaxValue;
        bool hitFound = false;

        foreach (RaycastHit hit in hits)
        {
            // Karakterin kendisini veya Trigger olan objeleri atla
            if (hit.transform == transform || hit.transform.IsChildOf(transform) || hit.collider.isTrigger)
                continue;

            if (hit.distance < closestDist)
            {
                closestDist = hit.distance;
                closestValidHit = hit;
                hitFound = true;
            }
        }

        if (hitFound)
        {
            targetPoint = closestValidHit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(maxAimDistance);
        }

        // 2. Namlu (FirePoint) pozisyonunu belirle
        Vector3 spawnPosition = (firePoint != null) ? firePoint.position : transform.position + Vector3.up * 1.2f + transform.forward * 0.5f;

        // 3. Namludan hedef noktaya yön hesapla
        Vector3 shootDirection = (targetPoint - spawnPosition).normalized;
        Quaternion projectileRotation = Quaternion.LookRotation(shootDirection);

        // 4. Mermiyi oluştur
        Instantiate(projectilePrefab, spawnPosition, projectileRotation);
    }
}