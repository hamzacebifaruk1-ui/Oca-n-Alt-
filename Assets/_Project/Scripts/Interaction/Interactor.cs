using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Project.Interaction
{
    /// <summary>
    /// Oyuncunun etkileşim sensörü: kameradan ışın (raycast) atar,
    /// bakılan IInteractable nesnesini bulur ve E tuşuyla etkileşimi tetikler.
    /// </summary>
    public class Interactor : MonoBehaviour
    {
        [Header("--- Etkileşim Ayarları ---")]
        [Tooltip("Etkileşim menzili (metre) — dokümandaki etkileşimMesafesi değeri")]
        [SerializeField] private float _maxDistance = 3f;

        [Tooltip("Hangi katmanlardaki nesnelerle etkileşilebilir")]
        [SerializeField] private LayerMask _interactableLayers = ~0;

        [Tooltip("Işının çıkış noktası (boşsa çocuk kamera kullanılır)")]
        [SerializeField] private Transform _rayOrigin;

        /// <summary>Bakılan nesne değiştiğinde / prompt güncellendiğinde tetiklenir (HUD dinler).</summary>
        public static event Action<string> PromptChanged;

        private Camera _camera;
        private IInteractable _currentTarget;

        private void Awake()
        {
            if (_rayOrigin == null)
            {
                _camera = GetComponentInChildren<Camera>();
                _rayOrigin = _camera != null ? _camera.transform : transform;
            }
        }

        private void Update()
        {
            // Giriş sinematiği veya delil inceleme ekranı açıkken etkileşim pasif
            if (Project.UI.IntroScreen.IsActive || Project.UI.EvidenceReviewPanel.IsOpen) return;

            ScanTarget();
            HandleInput();
        }

        private void ScanTarget()
        {
            IInteractable found = null;

            if (Physics.Raycast(_rayOrigin.position, _rayOrigin.forward, out RaycastHit hit, _maxDistance, _interactableLayers, QueryTriggerInteraction.Ignore))
            {
                found = hit.collider.GetComponentInParent<IInteractable>();
                if (found != null && !found.CanInteract) found = null;
            }

            if (!ReferenceEquals(found, _currentTarget))
            {
                _currentTarget = found;
                PromptChanged?.Invoke(found != null ? found.InteractionPrompt : string.Empty);
            }
        }

        private void HandleInput()
        {
            if (_currentTarget == null) return;

            bool interactPressed = false;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame) interactPressed = true;
            var pad = Gamepad.current;
            if (pad != null && pad.buttonWest.wasPressedThisFrame) interactPressed = true;
#else
            if (Input.GetKeyDown(KeyCode.E)) interactPressed = true;
#endif

            if (interactPressed && _currentTarget.CanInteract)
            {
                _currentTarget.Interact(this);
                // Etkileşim sonrası prompt güncelliğini yitirebilir; hemen yeniden tara
                ScanTarget();
            }
        }
    }
}
