using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target & Framing")]
    [SerializeField] private Transform target;           // Takip edilecek karakter (Mage)
    [SerializeField] private float height = 1.4f;        // Omuz / baş hizası yüksekliği
    [SerializeField] private float distance = 3.2f;      // Karakterden geriye mesafe

    [Header("Mouse Sensitivity")]
    [SerializeField] private float sensitivityX = 0.15f; // Sağa-sola dönüş hızı
    [SerializeField] private float sensitivityY = 0.15f; // Yukarı-aşağı bakış hızı
    [SerializeField] private float minPitch = -15f;      // Aşağı bakış sınırı
    [SerializeField] private float maxPitch = 60f;       // Yukarı bakış sınırı

    [Header("Wall & Ceiling Collision")]
    [SerializeField] private float cameraRadius = 0.2f;  // Kameranın duvarlara girmemesi için çarpışma çapı
    [SerializeField] private float minDistance = 0.5f;   // Duvara sıkışınca karaktere en fazla ne kadar yaklaşsın

    private float yaw;
    private float pitch;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Vector3 currentAngles = transform.eulerAngles;
        yaw = currentAngles.y;
        pitch = currentAngles.x;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        HandleInput();
        UpdateCameraTransform();
    }

    private void HandleInput()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            bool isLocked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = isLocked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isLocked;
        }

        if (Cursor.lockState != CursorLockMode.Locked || Mouse.current == null) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        yaw += mouseDelta.x * sensitivityX;
        pitch -= mouseDelta.y * sensitivityY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void UpdateCameraTransform()
    {
        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivotPosition = target.position + Vector3.up * height;
        Vector3 direction = -(targetRotation * Vector3.forward);

        // Duvar veya tavan kontrolü: Kamera ile karakter arasına duvar girerse kamerayı öne çek
        float finalDistance = distance;
        RaycastHit[] hits = Physics.SphereCastAll(pivotPosition, cameraRadius, direction, distance);

        foreach (RaycastHit hit in hits)
        {
            // Karakterin kendi collider'ını ve trigger'ları yoksay
            if (hit.transform == target || hit.transform.IsChildOf(target) || hit.collider.isTrigger)
                continue;

            if (hit.distance < finalDistance)
            {
                finalDistance = Mathf.Clamp(hit.distance, minDistance, distance);
            }
        }

        // Yaylanma olmadan doğrudan yörüngeye oturt
        transform.rotation = targetRotation;
        transform.position = pivotPosition + direction * finalDistance;
    }
}