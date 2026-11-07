using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Project.Player
{
    /// <summary>
    /// Birinci şahıs (First-Person) karakter ve kamera kontrolcüsü.
    /// Düzensiz zeminlerde, raylarda ve taşlarda çökme/batma yapmayacak şekilde optimize edilmiştir.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("--- Hareket Ayarları ---")]
        [Tooltip("Normal yürüme hızı (m/s)")]
        [SerializeField] private float _walkSpeed = 4.2f;

        [Tooltip("Koşma hızı (m/s)")]
        [SerializeField] private float _sprintSpeed = 7.0f;

        [Tooltip("Eğilme (Crouch) hızı (m/s)")]
        [SerializeField] private float _crouchSpeed = 2.2f;

        [Tooltip("Zıplama yüksekliği (metre)")]
        [SerializeField] private float _jumpHeight = 1.1f;

        [Tooltip("Yerçekimi ivmesi")]
        [SerializeField] private float _gravity = -19.62f;

        [Header("--- Kamera / Bakış Ayarları ---")]
        [Tooltip("Birinci şahıs kamerası")]
        [SerializeField] private Camera _playerCamera;

        [Tooltip("Fare hassasiyeti")]
        [SerializeField] private float _mouseSensitivity = 0.15f;

        [Tooltip("Kameranın yukarı bakabileceği maksimum açı")]
        [SerializeField] private float _maxLookUpAngle = 85f;

        [Tooltip("Kameranın aşağı bakabileceği maksimum açı")]
        [SerializeField] private float _maxLookDownAngle = -85f;

        [Header("--- Kafa Sallantısı (Head Bobbing) ---")]
        [Tooltip("Yürürken hafif kafa sallantısı")]
        [SerializeField] private bool _enableHeadBob = true;
        [SerializeField] private float _bobFrequency = 1.6f;
        [SerializeField] private float _bobAmplitude = 0.025f; // Yumuşak ve durunca çökme hissettirmeyen genlik

        [Header("--- Eğilme (Crouch) Ayarları ---")]
        [SerializeField] private bool _enableCrouch = true;
        [SerializeField] private float _standingHeight = 2.0f;
        [SerializeField] private float _crouchingHeight = 1.25f;
        [SerializeField] private float _crouchTransitionSpeed = 6f;

        // Bileşenler ve dahili değişkenler
        private CharacterController _characterController;
        private Vector3 _defaultCameraLocalPos;
        private float _cameraPitch = 0f;
        private float _verticalVelocity = 0f;
        private float _bobTimer = 0f;
        private bool _isCrouching = false;
        private float _targetHeight;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();

            // Karakter kontrolcüsünün basamak ve eğim toleranslarını ray ve kütüklere göre sabitle
            _characterController.stepOffset = 0.2f;
            _characterController.slopeLimit = 45f;
            _characterController.skinWidth = 0.05f;
            _characterController.minMoveDistance = 0f;
            _characterController.height = _standingHeight;
            _characterController.center = Vector3.zero;

            // Kamerayı otomatik bul
            if (_playerCamera == null)
            {
                _playerCamera = GetComponentInChildren<Camera>();
            }

            if (_playerCamera != null)
            {
                _defaultCameraLocalPos = _playerCamera.transform.localPosition;
            }

            _targetHeight = _standingHeight;
        }

        private void Start()
        {
            LockCursor(true);
        }

        private void Update()
        {
            HandleCursorToggle();
            HandleLook();
            HandleCrouch();
            HandleMovement();
            HandleHeadBob();
        }

        #region Kamera / Fare Bakışı

        private void HandleLook()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            Vector2 lookInput = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                lookInput = Mouse.current.delta.ReadValue() * _mouseSensitivity;
            }
            if (Gamepad.current != null)
            {
                lookInput += Gamepad.current.rightStick.ReadValue() * (_mouseSensitivity * 10f * Time.deltaTime);
            }
#else
            lookInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * _mouseSensitivity;
#endif

            // Yukarı / Aşağı bakış
            _cameraPitch -= lookInput.y;
            _cameraPitch = Mathf.Clamp(_cameraPitch, _maxLookDownAngle, _maxLookUpAngle);

            if (_playerCamera != null)
            {
                _playerCamera.transform.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
            }

            // Sağa / Sola bakış
            transform.Rotate(Vector3.up * lookInput.x);
        }

        #endregion

        #region Karakter Hareketi ve Zemin Kontrolü

        private void HandleMovement()
        {
            Vector2 moveInput = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) moveInput.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) moveInput.y -= 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) moveInput.x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) moveInput.x += 1f;
            }

            var pad = Gamepad.current;
            if (pad != null)
            {
                moveInput += pad.leftStick.ReadValue();
            }
#else
            moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif

            moveInput = Vector2.ClampMagnitude(moveInput, 1f);

            // Koşma
            bool isSprinting = false;
#if ENABLE_INPUT_SYSTEM
            if (kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed) && !_isCrouching)
            {
                isSprinting = true;
            }
            if (pad != null && pad.leftStickButton.isPressed && !_isCrouching)
            {
                isSprinting = true;
            }
#else
            if (Input.GetKey(KeyCode.LeftShift) && !_isCrouching) isSprinting = true;
#endif

            float currentSpeed = _walkSpeed;
            if (_isCrouching) currentSpeed = _crouchSpeed;
            else if (isSprinting && moveInput.y > 0) currentSpeed = _sprintSpeed;

            Vector3 moveDirection = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;

            // Gelişmiş Zemin Algılama (Raylar ve pürüzlü kayalarda isGrounded titremesini önler)
            bool isGrounded = CheckIsGrounded();

            if (isGrounded)
            {
                // Zemindeyken yerçekimini küçük sabit bir kuvvete sabitle (aşırı aşağı çekip batırmaz)
                if (_verticalVelocity < 0f)
                {
                    _verticalVelocity = -1.5f;
                }

                bool jumpPressed = false;
#if ENABLE_INPUT_SYSTEM
                if (kb != null && kb.spaceKey.wasPressedThisFrame) jumpPressed = true;
                if (pad != null && pad.buttonSouth.wasPressedThisFrame) jumpPressed = true;
#else
                if (Input.GetButtonDown("Jump")) jumpPressed = true;
#endif

                if (jumpPressed && !_isCrouching)
                {
                    _verticalVelocity = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
                }
            }
            else
            {
                _verticalVelocity += _gravity * Time.deltaTime;
            }

            Vector3 horizontalVelocity = moveDirection * (currentSpeed * moveInput.magnitude);
            Vector3 finalVelocity = horizontalVelocity + (Vector3.up * _verticalVelocity);

            _characterController.Move(finalVelocity * Time.deltaTime);
        }

        private bool CheckIsGrounded()
        {
            if (_characterController.isGrounded) return true;

            // Ray aralıklarında boşluğa düşme sanrısını engelleyen alt SphereCast desteği
            Vector3 origin = transform.position + _characterController.center;
            float radius = _characterController.radius * 0.85f;
            float distance = (_characterController.height * 0.5f) + 0.12f;

            return Physics.SphereCast(origin, radius, Vector3.down, out _, distance, ~LayerMask.GetMask("Ignore Raycast"), QueryTriggerInteraction.Ignore);
        }

        #endregion

        #region Eğilme (Crouch)

        private void HandleCrouch()
        {
            if (!_enableCrouch) return;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.cKey.wasPressedThisFrame || kb.leftCtrlKey.wasPressedThisFrame))
            {
                _isCrouching = !_isCrouching;
            }
#else
            if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftControl))
            {
                _isCrouching = !_isCrouching;
            }
#endif

            _targetHeight = _isCrouching ? _crouchingHeight : _standingHeight;

            if (!Mathf.Approximately(_characterController.height, _targetHeight))
            {
                _characterController.height = Mathf.MoveTowards(_characterController.height, _targetHeight, Time.deltaTime * _crouchTransitionSpeed);

                // Ayak tabanını daima aynı dünya seviyesinde tut (Aşağı çöküp batmayı engeller)
                float centerOffset = (_characterController.height - _standingHeight) * 0.5f;
                _characterController.center = new Vector3(0f, centerOffset, 0f);

                if (_playerCamera != null)
                {
                    float targetCameraY = _isCrouching ? (_defaultCameraLocalPos.y - 0.55f) : _defaultCameraLocalPos.y;
                    Vector3 camPos = _playerCamera.transform.localPosition;
                    camPos.y = Mathf.MoveTowards(camPos.y, targetCameraY, Time.deltaTime * _crouchTransitionSpeed);
                    _playerCamera.transform.localPosition = camPos;
                }
            }
        }

        #endregion

        #region Kafa Sallantısı (Head Bob)

        private void HandleHeadBob()
        {
            if (!_enableHeadBob || _playerCamera == null) return;

            Vector3 horizontalVelocity = new Vector3(_characterController.velocity.x, 0f, _characterController.velocity.z);
            bool isMoving = horizontalVelocity.magnitude > 0.15f;

            if (CheckIsGrounded() && isMoving)
            {
                _bobTimer += Time.deltaTime * _bobFrequency * (horizontalVelocity.magnitude / _walkSpeed);
                float bobY = Mathf.Sin(_bobTimer) * _bobAmplitude;
                float bobX = Mathf.Cos(_bobTimer * 0.5f) * (_bobAmplitude * 0.5f);

                Vector3 targetPos = _defaultCameraLocalPos;
                targetPos.y += bobY;
                targetPos.x += bobX;

                _playerCamera.transform.localPosition = Vector3.Lerp(_playerCamera.transform.localPosition, targetPos, Time.deltaTime * 10f);
            }
            else
            {
                _bobTimer = 0f;
                // Durulduğunda kamerayı yumuşakça varsayılan göz hizasına döndür
                _playerCamera.transform.localPosition = Vector3.Lerp(_playerCamera.transform.localPosition, _defaultCameraLocalPos, Time.deltaTime * 8f);
            }
        }

        #endregion

        #region İmleç Yönetimi

        private void HandleCursorToggle()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                LockCursor(Cursor.lockState != CursorLockMode.Locked);
            }
            else if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                LockCursor(true);
            }
#else
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                LockCursor(Cursor.lockState != CursorLockMode.Locked);
            }
            else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
            {
                LockCursor(true);
            }
#endif
        }

        public void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        #endregion
    }
}
