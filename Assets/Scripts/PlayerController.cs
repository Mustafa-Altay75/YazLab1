using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 14f;
    [SerializeField] private float gravity = -15f;

    [Header("References")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform cameraTransform;

    private Vector2 inputVector;
    private Vector3 verticalVelocity;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    private void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // Sahnedeki ana kamerayý referans al
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        HandleInput();
        HandleMovement();
        HandleGravity();
    }

    private void HandleInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        inputVector = Vector2.zero;
        if (keyboard.wKey.isPressed) inputVector.y += 1f;
        if (keyboard.sKey.isPressed) inputVector.y -= 1f;
        if (keyboard.aKey.isPressed) inputVector.x -= 1f;
        if (keyboard.dKey.isPressed) inputVector.x += 1f;

        inputVector = inputVector.normalized;

        // Saldýrý tetikleme
        if ((Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) || keyboard.spaceKey.wasPressedThisFrame)
        {
            if (animator != null) animator.SetTrigger(AttackHash);
        }
    }

    private void HandleMovement()
    {
        Vector3 moveDirection = Vector3.zero;

        if (inputVector.magnitude >= 0.1f)
        {
            // Kamera yönüne göre hareket vektörünü hesapla (Y ekseni sýfýrlanýr)
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            moveDirection = (camForward * inputVector.y + camRight * inputVector.x).normalized;

            // Karakteri hareket yönüne döndür
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // Yatay hareket
            controller.Move(moveDirection * (moveSpeed * Time.deltaTime));
        }

        // Animasyon geçiþi için hýz parametresi
        if (animator != null)
        {
            animator.SetFloat(SpeedHash, inputVector.magnitude, 0.05f, Time.deltaTime);
        }
    }

    private void HandleGravity()
    {
        if (controller.isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f;
        }
        else
        {
            verticalVelocity.y += gravity * Time.deltaTime;
        }

        // Düþey yerçekimi hareketi
        controller.Move(verticalVelocity * Time.deltaTime);
    }
}