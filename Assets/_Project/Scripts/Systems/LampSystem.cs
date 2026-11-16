using System;
using Project.Player;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Project.Systems
{
    /// <summary>
    /// Karbür fener sistemi: yağ zamanla azalır, sağ tık basılıyken odak ışığı
    /// (daha güçlü ve dar ışın — "ışık doğrular") açılır ama yağı hızlı yakar.
    /// Yağ bitince fener söner; yağ kutuları (OilCan) feneri tam doldurur.
    /// Oyun başlarken kendini oyuncunun kamerasına kurar; sahneye elle eklenmez.
    /// </summary>
    public class LampSystem : MonoBehaviour
    {
        public static LampSystem Instance { get; private set; }

        /// <summary>Yağ oranı değiştiğinde tetiklenir (0 = boş, 1 = dolu). HUD dinler.</summary>
        public static event Action<float> OilChanged;

        /// <summary>Odak modu açılıp kapandığında tetiklenir.</summary>
        public static event Action<bool> FocusChanged;

        [Header("--- Yağ Ayarları ---")]
        [Tooltip("Maksimum yağ miktarı")]
        [SerializeField] private float _maxOil = 100f;

        [Tooltip("Başlangıç yağı")]
        [SerializeField] private float _startOil = 100f;

        [Tooltip("Normal kullanımda saniyede harcanan yağ (100 yağ ≈ 3-4 dakika)")]
        [SerializeField] private float _normalDrain = 0.55f;

        [Tooltip("Odak ışığında saniyede harcanan yağ (normalin ~4 katı)")]
        [SerializeField] private float _focusDrain = 2.2f;

        [Header("--- Işık Ayarları ---")]
        [Tooltip("Normal ışık şiddeti")]
        [SerializeField] private float _normalIntensity = 14f;

        [Tooltip("Odak ışık şiddeti")]
        [SerializeField] private float _focusIntensity = 24f;

        [Tooltip("Normal ışın açısı (derece)")]
        [SerializeField] private float _normalAngle = 55f;

        [Tooltip("Odak ışın açısı (derece) — dar ve uzun")]
        [SerializeField] private float _focusAngle = 22f;

        [Tooltip("Fener rengi — 1996 tungsten/karbür sıcaklığı")]
        [SerializeField] private Color _lampColor = new Color(1f, 0.82f, 0.6f);

        [Header("--- Uyarı Ayarları ---")]
        [Tooltip("Yağ bu oranın altındaysa (%0-1) fener titremeye başlar")]
        [SerializeField, Range(0f, 1f)] private float _warnThreshold = 0.2f;

        public float OilPercent => _oil / _maxOil;
        public bool HasOil => _oil > 0f;
        public bool IsFocusing => _focusing && HasOil;

        private Light _light;
        private float _oil;
        private bool _focusing;
        private float _flickerSeed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;

            var fpc = FindAnyObjectByType<FirstPersonController>();
            if (fpc == null) return; // sahnede oyuncu yoksa fener de yok

            var camera = fpc.GetComponentInChildren<Camera>();
            if (camera == null) camera = fpc.GetComponent<Camera>();
            if (camera == null) return;

            var go = new GameObject("[KarbürFener]");
            go.transform.SetParent(camera.transform, false);
            go.transform.localPosition = new Vector3(0.25f, -0.2f, 0.1f); // el hizasında sağda
            go.AddComponent<LampSystem>();
        }

        private void Awake()
        {
            Instance = this;
            _oil = Mathf.Clamp(_startOil, 0f, _maxOil);
            _flickerSeed = UnityEngine.Random.value * 100f;

            _light = gameObject.AddComponent<Light>();
            _light.type = LightType.Spot;
            _light.color = _lampColor;
            _light.intensity = _normalIntensity;
            _light.range = 18f;
            _light.spotAngle = _normalAngle;
            _light.shadows = LightShadows.Soft;
            _light.shadowStrength = 0.9f;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            ReadFocusInput();

            // Yağ tüketimi
            float drain = IsFocusing ? _focusDrain : _normalDrain;
            float before = OilPercent;
            _oil = Mathf.Max(0f, _oil - drain * Time.deltaTime);
            if (!Mathf.Approximately(before, OilPercent) || before <= _warnThreshold)
                OilChanged?.Invoke(OilPercent);

            UpdateLight();
        }

        private void ReadFocusInput()
        {
            bool wantFocus = false;

#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse != null) wantFocus = mouse.rightButton.isPressed;
#else
            wantFocus = Input.GetMouseButton(1);
#endif

            if (wantFocus != _focusing)
            {
                _focusing = wantFocus;
                FocusChanged?.Invoke(_focusing);
            }
        }

        private void UpdateLight()
        {
            bool focus = IsFocusing;

            float targetIntensity = focus ? _focusIntensity : _normalIntensity;
            float targetAngle = focus ? _focusAngle : _normalAngle;

            // Yağ azaldıkça ışık zayıflar (yüzde 20'nin altında titrer)
            float pct = OilPercent;
            float strength = pct <= 0f ? 0f : 1f;
            if (pct > 0f && pct < _warnThreshold)
            {
                // Karbür alevinin penişi: hızlı + düzensiz titreme
                float t = Time.time * 9f + _flickerSeed;
                float flicker = 0.55f + 0.45f * (Mathf.PerlinNoise(t, 0f) * 0.6f + Mathf.PerlinNoise(t * 3.1f, 7f) * 0.4f);
                strength = Mathf.Lerp(0.25f, 1f, pct / _warnThreshold) * flicker;
            }

            // Yumuşak geçiş
            float lerpSpeed = focus ? 6f : 3f;
            _light.intensity = Mathf.Lerp(_light.intensity, targetIntensity * strength, Time.deltaTime * lerpSpeed);
            _light.spotAngle = Mathf.Lerp(_light.spotAngle, targetAngle, Time.deltaTime * lerpSpeed);
            _light.range = Mathf.Lerp(_light.range, focus ? 26f : 18f, Time.deltaTime * lerpSpeed);
        }

        /// <summary>Feneri tamamen doldurur (yağ kutusu kullanıldığında).</summary>
        public void RefillFull()
        {
            _oil = _maxOil;
            OilChanged?.Invoke(OilPercent);
        }

        /// <summary>Belirli miktarda yağ ekler.</summary>
        public void AddOil(float amount)
        {
            _oil = Mathf.Clamp(_oil + amount, 0f, _maxOil);
            OilChanged?.Invoke(OilPercent);
        }
    }
}
