using UnityEngine;
using UnityEngine.InputSystem;
using EstanDentro.UI;

namespace EstanDentro.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movimiento")]
        [SerializeField] private float walkSpeed = 2.8f;
        [SerializeField] private float sprintSpeed = 4.5f;
        [SerializeField] private float crouchSpeed = 1.4f;
        [SerializeField] private float gravity = -20f;

        [Header("Camara")]
        [SerializeField] private Transform cameraPivot;
        public Transform CameraPivot => cameraPivot;
        public bool InputEnabled { get; set; } = true;
        public void SetPitch(float p) { pitch = Mathf.Clamp(p, minPitch, maxPitch); }
        [SerializeField, Tooltip("Grados por pixel de delta del mouse. Subir = mas rapido. Tipico 0.1 - 0.4.")]
        private float mouseSensitivity = 0.25f;
        [SerializeField, Tooltip("Grados por segundo cuando el stick esta a tope. Subir = mas rapido. Tipico 150 - 320.")]
        private float gamepadSensitivity = 240f;
        [SerializeField, Tooltip("Zona muerta del stick para evitar drift. 0.1 - 0.2 es comun.")]
        private float gamepadDeadzone = 0.12f;
        [SerializeField] private float minPitch = -85f;
        [SerializeField] private float maxPitch = 85f;
        [SerializeField] private bool invertY = false;

        [Header("Crouch")]
        [SerializeField] private float standingHeight = 1.8f;
        [SerializeField] private float crouchingHeight = 1.0f;
        [SerializeField] private float heightLerpSpeed = 10f;

        private CharacterController controller;
        private Vector2 moveInput;
        private Vector2 mouseLookInput;
        private Vector2 gamepadLookInput;
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

        private void Start()
        {
            ApplySettings();
        }

        public void ApplySettings()
        {
            mouseSensitivity = Settings.MouseSensitivity;
            gamepadSensitivity = Settings.GamepadSensitivity;
            invertY = Settings.InvertY;
        }

        private void Update()
        {
            if (InputEnabled)
            {
                PollHoldInputs();
                HandleLook();
            }
            HandleMove(); // gravedad sigue funcionando aunque input este bloqueado
            HandleCrouchHeight();
        }

        private void PollHoldInputs()
        {
            var kb = Keyboard.current;
            var gp = Gamepad.current;
            sprintHeld = (kb != null && kb.leftShiftKey.isPressed) ||
                         (gp != null && gp.leftStickButton.isPressed);
            crouchHeld = (kb != null && kb.cKey.isPressed) ||
                         (gp != null && gp.rightTrigger.isPressed);
        }

        private void HandleLook()
        {
            float yaw = mouseLookInput.x * mouseSensitivity;
            float pitchDelta = mouseLookInput.y * mouseSensitivity;

            Vector2 stick = ApplyDeadzone(gamepadLookInput, gamepadDeadzone);
            yaw += stick.x * gamepadSensitivity * Time.deltaTime;
            pitchDelta += stick.y * gamepadSensitivity * Time.deltaTime;

            pitchDelta *= invertY ? 1f : -1f;

            transform.Rotate(0f, yaw, 0f, Space.Self);

            pitch = Mathf.Clamp(pitch + pitchDelta, minPitch, maxPitch);
            if (cameraPivot != null)
                cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private static Vector2 ApplyDeadzone(Vector2 v, float deadzone)
        {
            float mag = v.magnitude;
            if (mag < deadzone) return Vector2.zero;
            return v * ((mag - deadzone) / (1f - deadzone) / mag);
        }

        private void HandleMove()
        {
            float speed = crouchHeld ? crouchSpeed : (sprintHeld ? sprintSpeed : walkSpeed);

            Vector2 effectiveMove = InputEnabled ? moveInput : Vector2.zero;
            Vector3 move = transform.right * effectiveMove.x + transform.forward * effectiveMove.y;
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
        public void OnLook(InputValue value) => mouseLookInput = value.Get<Vector2>();
        public void OnLookGamepad(InputValue value) => gamepadLookInput = value.Get<Vector2>();
        public void OnSprint(InputValue value) { /* polled in Update — Send Messages no propaga release */ }
        public void OnCrouch(InputValue value) { /* polled in Update — Send Messages no propaga release */ }

        public void OnInteract(InputValue value)
        {
            if (value.isPressed) Debug.Log("[Input] Interact pressed");
        }

        public void OnFlashlight(InputValue value)
        {
            if (value.isPressed) Debug.Log("[Input] Flashlight pressed");
        }

        public void OnPause(InputValue value)
        {
            if (value.isPressed) Debug.Log("[Input] Pause pressed");
        }

        public void OnBreatheFallback(InputValue value)
        {
            Debug.Log($"[Input] BreatheFallback {(value.isPressed ? "down" : "up")}");
        }
    }
}
