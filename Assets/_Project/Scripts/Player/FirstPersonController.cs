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

        [Header("--- Hareket Yumuşatma ---")]
        [Tooltip("Ivmelenme/yavaşlama sertliği: yüksek = keskin, düşük = akıcı")]
        [SerializeField] private float _acceleration = 14f;

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

        [Header("--- Koşma FOV Efekti ---")]
        [Tooltip("Koşarken görüş alanının (FOV) ne kadar genişleyeceği")]
        [SerializeField] private float _sprintFovBoost = 8f;

        [Tooltip("FOV geçiş hızı")]
        [SerializeField] private float _fovLerpSpeed = 8f;

        [Header("--- İniş Etkisi (Landing Impact) ---")]
        [Tooltip("İniş anında kameranın aşağı dalma şiddeti")]
        [SerializeField] private float _landingImpactStrength = 0.08f;

        [Tooltip("İniş etkisinin toparlanma hızı")]
        [SerializeField] private float _landingRecoverySpeed = 6f;

        [Tooltip("Bu düşüş hızının (m/s) altındaki inişlerde sarsıntı olmaz")]
        [SerializeField] private float _landingImpactMinSpeed = 3f;

        [Header("--- Ayak Sesleri ---")]
        [Tooltip("Ayak sesi için AudioSource (boş bırakılırsa çocuklardan otomatik bulunur)")]
        [SerializeField] private AudioSource _footstepSource;

        [Tooltip("Rastgele çalınacak ayak sesleri (boşsa ses çalınmaz)")]
        [SerializeField] private AudioClip[] _footstepClips;

        [Tooltip("Ayak sesi seviyesi")]
        [SerializeField, Range(0f, 1f)] private float _footstepVolume = 0.6f;

        [Tooltip("Tek adım sesleri için adımlar arası mesafe (metre) — uzun kayıtlarda etkisizdir")]
        [SerializeField] private float _footstepDistance = 2.0f;

        [Header("--- Eğilme (Crouch) Ayarları ---")]
        [SerializeField] private bool _enableCrouch = true;
        [SerializeField] private float _standingHeight = 2.0f;
        [SerializeField] private float _crouchingHeight = 1.25f;
        [SerializeField] private float _crouchTransitionSpeed = 6f;

        [Tooltip("Eğilirken kameranın ne kadar aşağı ineceği (metre)")]
        [SerializeField] private float _crouchCameraLower = 0.55f;

        // Bileşenler ve dahili değişkenler
        private CharacterController _characterController;
        private Vector3 _defaultCameraLocalPos;
        private float _cameraPitch = 0f;
        private float _verticalVelocity = 0f;
        private float _bobTimer = 0f;
        private bool _isCrouching = false;
        private bool _toggleCrouch = false;
        private float _targetHeight;
        private float _defaultFov = 60f;
        private bool _isSprinting = false;
        private bool _wasGrounded = true;
        private float _previousVerticalVelocity = 0f;
        private float _landingOffset = 0f;
        private float _eyeBaseY = 0f;
        private float _stepDistanceAccumulator = 0f;
        private Vector3 _currentHorizontalVelocity = Vector3.zero;

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
                _defaultFov = _playerCamera.fieldOfView;
                _eyeBaseY = _defaultCameraLocalPos.y;
            }

            // Ayak ses kaynağını otomatik bul; hiçbiri yoksa oyuncuya yeni bir AudioSource ekle
            if (_footstepSource == null)
            {
                _footstepSource = GetComponentInChildren<AudioSource>();
            }
            if (_footstepSource == null)
            {
                _footstepSource = gameObject.AddComponent<AudioSource>();
                _footstepSource.playOnAwake = false;
                _footstepSource.loop = false;
                // Kısmen 3D: adım sesleri dünyada konumlu ama tınlamasın
                _footstepSource.spatialBlend = 0.5f;
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
            HandleSprintFov();
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

            // Koşma FOV efekti yalnızca gerçekten koşarken tetiklenir
            _isSprinting = currentSpeed > _walkSpeed + 0.01f && isGrounded;

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

            // Hedef hıza doğru yumuşak ivmelenme/yavaşlama (frame-rate bağımsız)
            Vector3 targetVelocity = moveDirection * (currentSpeed * moveInput.magnitude);
            float smooth = 1f - Mathf.Exp(-_acceleration * Time.deltaTime);
            _currentHorizontalVelocity = Vector3.Lerp(_currentHorizontalVelocity, targetVelocity, smooth);
            Vector3 horizontalVelocity = _currentHorizontalVelocity;
            Vector3 finalVelocity = horizontalVelocity + (Vector3.up * _verticalVelocity);

            _characterController.Move(finalVelocity * Time.deltaTime);

            // İniş etkisi: düşüş hızı eşiği aştıysa kamerayı aşağı daldır
            if (isGrounded && !_wasGrounded && _previousVerticalVelocity < -_landingImpactMinSpeed)
            {
                _landingOffset = Mathf.Clamp(_previousVerticalVelocity * _landingImpactStrength * 0.5f, -0.35f, 0f);
            }
            _wasGrounded = isGrounded;
            if (isGrounded)
            {
                // Yerdeyken sonraki iniş için "önceki hız" değerini tazele
                _previousVerticalVelocity = _verticalVelocity;
            }

            // Ayak sesleri: durunca/havadayken kesin kes, hareket halinde moda göre çal
            if (isGrounded && horizontalVelocity.sqrMagnitude > 0.04f)
            {
                UpdateFootsteps(horizontalVelocity.magnitude);
            }
            else if (_footstepSource != null && _footstepSource.isPlaying)
            {
                // Durunca ya da zıplayınca sesi anında kes (arkada çalmaya devam etmesin)
                _footstepSource.Stop();
                _stepDistanceAccumulator = _footstepDistance * 0.5f; // Hareket başladığında ilk adım çok geç gelmesin
            }
        }

        private void UpdateFootsteps(float speed)
        {
            if (_footstepSource == null || _footstepClips == null || _footstepClips.Length == 0) return;

            AudioClip clip = _footstepClips[Random.Range(0, _footstepClips.Length)];
            if (clip == null) return;

            // 1.5 saniyeden uzun kayıtlar tek adım değil, arka arkaya adımlar içeren "yürüyüş kaydı"dır
            bool isLoopRecording = clip.length > 1.5f;

            if (isLoopRecording)
            {
                // Uzun kayıt modu: hareket ederken döngüde sürekli çalar, tekrar başlatmaz
                if (!_footstepSource.isPlaying)
                {
                    _footstepSource.loop = true;
                    _footstepSource.clip = clip;
                    _footstepSource.volume = _footstepVolume;
                    _footstepSource.Play();
                }
                // Hıza göre ton: yürürken normal, koşarken hafif hızlı/tiz
                _footstepSource.pitch = Mathf.Lerp(1f, 1.15f, Mathf.Clamp01(speed / Mathf.Max(_sprintSpeed, 0.01f)));
            }
            else
            {
                // Tek adım modu: mesafe biriktiriciyle adım adım, üst üste binmesin
                _footstepSource.loop = false;
                _stepDistanceAccumulator += speed * Time.deltaTime;
                if (_stepDistanceAccumulator >= _footstepDistance && !_footstepSource.isPlaying)
                {
                    // Her adımda hafif ton değişimi, tekrarlı ses hissini kırar
                    _footstepSource.pitch = Random.Range(0.92f, 1.08f);
                    _footstepSource.PlayOneShot(clip, _footstepVolume);
                    _stepDistanceAccumulator = 0f;
                }
            }
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
            // C: aç/kapa (bas → çök ve kal, tekrar bas → kalk)
            if (kb != null && kb.cKey.wasPressedThisFrame)
            {
                _toggleCrouch = !_toggleCrouch;
            }
            // Ctrl: basılı tutuldukça çök, bırakınca kalk
            bool ctrlHeld = kb != null && (kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed);
#else
            if (Input.GetKeyDown(KeyCode.C))
            {
                _toggleCrouch = !_toggleCrouch;
            }
            bool ctrlHeld = Input.GetKey(KeyCode.LeftControl);
#endif

            _isCrouching = _toggleCrouch || ctrlHeld;

            _targetHeight = _isCrouching ? _crouchingHeight : _standingHeight;

            // Ayağa kalkarken başın üstünde tavan varsa kalkma; çökmüş bekle
            if (_targetHeight > _characterController.height && !CanStandUp())
            {
                return;
            }

            if (!Mathf.Approximately(_characterController.height, _targetHeight))
            {
                _characterController.height = Mathf.MoveTowards(_characterController.height, _targetHeight, Time.deltaTime * _crouchTransitionSpeed);

                // Ayak tabanını daima aynı dünya seviyesinde tut (Aşağı çöküp batmayı engeller)
                float centerOffset = (_characterController.height - _standingHeight) * 0.5f;
                _characterController.center = new Vector3(0f, centerOffset, 0f);

                if (_playerCamera != null)
                {
                    float targetCameraY = _isCrouching ? (_defaultCameraLocalPos.y - _crouchCameraLower) : _defaultCameraLocalPos.y;
                    Vector3 camPos = _playerCamera.transform.localPosition;
                    camPos.y = Mathf.MoveTowards(camPos.y, targetCameraY, Time.deltaTime * _crouchTransitionSpeed);
                    _playerCamera.transform.localPosition = camPos;

                    // Göz hizası temelini de eğilme hedefiyle birlikte kaydır
                    // (Head bob bu temele göre salınır, yoksa eğilirken kamera yukarı doğru savaşır)
                    _eyeBaseY = Mathf.MoveTowards(_eyeBaseY, targetCameraY, Time.deltaTime * _crouchTransitionSpeed);
                }
            }
        }

        /// <summary>Kalkarken üstte tavan/dişelif var mı diye bakar; varsa çökmüş kalmak daha güvenlidir.</summary>
        private bool CanStandUp()
        {
            float raise = _standingHeight - _characterController.height;
            if (raise <= 0.01f) return true;

            Vector3 origin = transform.position + _characterController.center;
            float radius = _characterController.radius * 0.9f;
            float distance = (_characterController.height * 0.5f) + raise + 0.05f;

            return !Physics.SphereCast(origin, radius, Vector3.up, out _, distance, ~LayerMask.GetMask("Ignore Raycast"), QueryTriggerInteraction.Ignore);
        }

        #endregion

        #region Kafa Sallantısı (Head Bob)

        private void HandleHeadBob()
        {
            if (_playerCamera == null) return;

            // İniş etkisini yumuşakça toparla
            if (Mathf.Abs(_landingOffset) > 0.0005f)
            {
                _landingOffset = Mathf.Lerp(_landingOffset, 0f, Time.deltaTime * _landingRecoverySpeed);
            }
            else
            {
                _landingOffset = 0f;
            }

            if (!_enableHeadBob) return;

            Vector3 horizontalVelocity = new Vector3(_characterController.velocity.x, 0f, _characterController.velocity.z);
            bool isMoving = horizontalVelocity.magnitude > 0.15f;

            // Göz hizası temeli: eğilme sırasında da kamerayla uyumlu kalır
            Vector3 eyeBase = new Vector3(_defaultCameraLocalPos.x, _eyeBaseY, _defaultCameraLocalPos.z);

            if (CheckIsGrounded() && isMoving)
            {
                _bobTimer += Time.deltaTime * _bobFrequency * (horizontalVelocity.magnitude / _walkSpeed);
                float bobY = Mathf.Sin(_bobTimer) * _bobAmplitude;
                float bobX = Mathf.Cos(_bobTimer * 0.5f) * (_bobAmplitude * 0.5f);

                Vector3 targetPos = eyeBase;
                targetPos.y += bobY + _landingOffset;
                targetPos.x += bobX;

                _playerCamera.transform.localPosition = Vector3.Lerp(_playerCamera.transform.localPosition, targetPos, Time.deltaTime * 10f);
            }
            else
            {
                _bobTimer = 0f;
                // Durulduğunda kamerayı yumuşakça varsayılan göz hizasına döndür (iniş etkisi dahil)
                Vector3 targetPos = eyeBase;
                targetPos.y += _landingOffset;

                _playerCamera.transform.localPosition = Vector3.Lerp(_playerCamera.transform.localPosition, targetPos, Time.deltaTime * 8f);
            }
        }

        #endregion

        #region Koşma FOV Efekti

        private void HandleSprintFov()
        {
            if (_playerCamera == null) return;

            float targetFov = _isSprinting ? _defaultFov + _sprintFovBoost : _defaultFov;

            if (!Mathf.Approximately(_playerCamera.fieldOfView, targetFov))
            {
                _playerCamera.fieldOfView = Mathf.Lerp(_playerCamera.fieldOfView, targetFov, Time.deltaTime * _fovLerpSpeed);
            }
        }

        #endregion

        #region İmleç Yönetimi

        private void HandleCursorToggle()
        {
            // Delil inceleme ekranı açıkken ESC'in imleci serbest bırakmasın (o ESC ekranı kapatır)
            if (Project.UI.EvidenceReviewPanel.IsOpen) return;

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
