using UnityEngine;
using UnityEngine.InputSystem;

namespace EstanDentro.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movimiento")]
        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float sprintSpeed = 7f;
        [SerializeField] private float crouchSpeed = 2f;
        [SerializeField] private float gravity = -20f;

        [Header("Camara")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float mouseSensitivity = 0.15f;
        [SerializeField] private float minPitch = -85f;
        [SerializeField] private float maxPitch = 85f;
        [SerializeField] private bool invertY = false;

        [Header("Crouch")]
        [SerializeField] private float standingHeight = 1.8f;
        [SerializeField] private float crouchingHeight = 1.0f;
        [SerializeField] private float heightLerpSpeed = 10f;

        private CharacterController controller;
        private Vector2 moveInput;
        private Vector2 lookInput;
        private bool sprintHeld;
        private bool crouchHeld;
        private float verticalVelocity;
        private float pitch;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            controller.height = standingHeight;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            HandleLook();
            HandleMove();
            HandleCrouchHeight();
        }

        private void HandleLook()
        {
            float yaw = lookInput.x * mouseSensitivity;
            float pitchDelta = lookInput.y * mouseSensitivity * (invertY ? 1f : -1f);

            transform.Rotate(0f, yaw, 0f, Space.Self);

            pitch = Mathf.Clamp(pitch + pitchDelta, minPitch, maxPitch);
            if (cameraPivot != null)
                cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void HandleMove()
        {
            float speed = crouchHeld ? crouchSpeed : (sprintHeld ? sprintSpeed : walkSpeed);

            Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
            move *= speed;

            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            else
                verticalVelocity += gravity * Time.deltaTime;

            move.y = verticalVelocity;
            controller.Move(move * Time.deltaTime);
        }

        private void HandleCrouchHeight()
        {
            float target = crouchHeld ? crouchingHeight : standingHeight;
            controller.height = Mathf.Lerp(controller.height, target, Time.deltaTime * heightLerpSpeed);
            controller.center = new Vector3(0f, controller.height * 0.5f, 0f);

            if (cameraPivot != null)
            {
                Vector3 p = cameraPivot.localPosition;
                p.y = controller.height - 0.15f;
                cameraPivot.localPosition = p;
            }
        }

        public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();
        public void OnLook(InputValue value) => lookInput = value.Get<Vector2>();
        public void OnSprint(InputValue value) => sprintHeld = value.isPressed;
        public void OnCrouch(InputValue value) => crouchHeld = value.isPressed;
    }
}
