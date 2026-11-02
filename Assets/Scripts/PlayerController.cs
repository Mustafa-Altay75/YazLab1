using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 12f;

    [Header("Jump & Gravity")]
    [SerializeField] private float jumpForce = 6.5f;
    [SerializeField] private float gravity = -18f;
    [SerializeField] private float groundCheckDistance = 0.25f;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform cameraTransform;

    private CharacterController controller;
    private Vector3 verticalVelocity;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int JumpHash = Animator.StringToHash("Jump");

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        HandleMovement();
        HandleJumpAndGravity();
    }

    private void HandleMovement()
    {
        Vector2 input = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed) input.y -= 1f;
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;
        }

        Vector3 moveDirection = Vector3.zero;

        if (input.sqrMagnitude > 0.01f)
        {
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            moveDirection = forward * input.y + right * input.x;
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        controller.Move(moveDirection * (moveSpeed * Time.deltaTime));

        if (animator != null)
        {
            animator.SetFloat(SpeedHash, input.magnitude);
        }
    }

    private void HandleJumpAndGravity()
    {
        // Karakterin yer algısını hem CharacterController hem de Raycast ile garantiye alıyoruz
        bool isGrounded = controller.isGrounded ||
                          Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance);

        // Yerdeyken basamak atlamasını ve zemine tutunmayı sağla
        if (isGrounded && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = -2f;
        }

        // Space tuşuna basıldığı an bekleme yapmadan zıplamayı tetikle
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            verticalVelocity.y = jumpForce;

            if (animator != null)
            {
                animator.ResetTrigger(JumpHash);
                animator.SetTrigger(JumpHash);
            }
        }

        // Yerçekimini uygula
        verticalVelocity.y += gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }
}