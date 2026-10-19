using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target & Framing")]
    [SerializeField] private Transform target;           // Takip edilecek karakter (Mage)
    [SerializeField] private float height = 1.4f;        // Omuz / baþ hizasý yüksekliði
    [SerializeField] private float distance = 3.2f;      // Karakterden geriye mesafe

    [Header("Mouse Sensitivity")]
    [SerializeField] private float sensitivityX = 0.15f; // Saða-sola dönüþ hýzý
    [SerializeField] private float sensitivityY = 0.15f; // Yukarý-aþaðý bakýþ hýzý
    [SerializeField] private float minPitch = -15f;      // Aþaðý bakýþ sýnýrý
    [SerializeField] private float maxPitch = 60f;       // Yukarý bakýþ sýnýrý

    [Header("Wall & Ceiling Collision")]
    [SerializeField] private float cameraRadius = 0.2f;  // Kameranýn duvarlara girmemesi için çarpýþma çapý
    [SerializeField] private float minDistance = 0.5f;   // Duvara sýkýþýnca karaktere en fazla ne kadar yaklaþsýn

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

        // Duvar veya tavan kontrolü: Kamera ile karakter arasýna duvar girerse kamerayý öne çek
        float finalDistance = distance;
        RaycastHit[] hits = Physics.SphereCastAll(pivotPosition, cameraRadius, direction, distance);

        foreach (RaycastHit hit in hits)
        {
            // Karakterin kendi collider'ýný ve trigger'larý yoksay
            if (hit.transform == target || hit.transform.IsChildOf(target) || hit.collider.isTrigger)
                continue;

            if (hit.distance < finalDistance)
            {
                finalDistance = Mathf.Clamp(hit.distance, minDistance, distance);
            }
        }

        // Yaylanma olmadan doðrudan yörüngeye oturt
        transform.rotation = targetRotation;
        transform.position = pivotPosition + direction * finalDistance;
    }
}