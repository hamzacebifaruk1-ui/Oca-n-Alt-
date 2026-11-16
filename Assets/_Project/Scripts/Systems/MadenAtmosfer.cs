using System.Collections.Generic;
using UnityEngine;

namespace Project.Systems
{
    /// <summary>
    /// Maden atmosferi: kamera etrafında uçuşan toz zerreleri ve
    /// eski tip maden lambalarının organik titremesi (flicker).
    /// Kameraya eklenir; sahnede ayrıca kurulum gerektirmez.
    /// </summary>
    public class MadenAtmosfer : MonoBehaviour
    {
        [Header("--- Toz Zerreleri (Dust) ---")]
        [Tooltip("Fener ışığında parıldayan toz zerreleri")]
        [SerializeField] private bool _enableDust = true;

        [Tooltip("Toz yoğunluğu (0 = yok gibi, 1 = yoğun)")]
        [SerializeField, Range(0f, 1f)] private float _dustDensity = 0.5f;

        [Tooltip("Toz zerrelerinin rengi ve saydamlığı")]
        [SerializeField] private Color _dustColor = new Color(1f, 0.9f, 0.75f, 0.28f);

        [Tooltip("Tozun kameraya göre yayılma yarıçapı (metre)")]
        [SerializeField] private float _dustRadius = 9f;

        [Header("--- Lamba Titremesi (Flicker) ---")]
        [Tooltip("Eski gaz lambası tarzı organik ışık titremesi")]
        [SerializeField] private bool _enableFlicker = true;

        [Tooltip("Titreme şiddeti")]
        [SerializeField, Range(0f, 0.3f)] private float _flickerStrength = 0.08f;

        [Tooltip("Titreme hızı")]
        [SerializeField, Range(0.5f, 6f)] private float _flickerScale = 2.2f;

        [Tooltip("İsmi bu kelimeyi içeren ışıklar titremeden muaf tutulur")]
        [SerializeField] private string _flickerExcludeKeyword = "MaviIsik";

        [Tooltip("Lambaların arada bir kısa süre kasılma olasılığı")]
        [SerializeField, Range(0f, 0.01f)] private float _dipChance = 0.002f;

        private ParticleSystem _dust;
        private readonly List<FlickerLight> _flickerLights = new List<FlickerLight>();
        private float _nextScanTime;

        private class FlickerLight
        {
            public Light light;
            public float baseIntensity;
            public float seed;
            public float dipTimer;
        }

        private void Awake()
        {
            if (_enableDust) CreateDust();
            ScanLights();
        }

        private void Update()
        {
            if (_enableFlicker) UpdateFlicker();
        }

        private void OnDisable()
        {
            // Bileşen kapatılırsa ışıkları orijinal parlaklıklarına döndür
            foreach (var f in _flickerLights)
            {
                if (f.light != null) f.light.intensity = f.baseIntensity;
            }
            _flickerLights.Clear();
            _nextScanTime = 0f;
        }

        private void OnEnable()
        {
            ScanLights();
        }

        #region Toz Zerreleri

        private void CreateDust()
        {
            var dustGO = new GameObject("Maden_Toz_Zerreleri");
            dustGO.transform.SetParent(transform, false);
            _dust = dustGO.AddComponent<ParticleSystem>();

            // Sistem otomatik başlamasın; önce durdurup ayarları yapacağız
            _dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = _dust.main;
            main.duration = 1f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.006f, 0.022f);
            main.startColor = _dustColor;
            main.maxParticles = 400;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = new ParticleSystem.MinMaxCurve(0f, 0.01f);

            var emission = _dust.emission;
            emission.rateOverTime = Mathf.Lerp(5f, 45f, _dustDensity);

            var shape = _dust.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = _dustRadius;

            // Yavaş, savrulan süzülme hareketi
            var noise = _dust.noise;
            noise.enabled = true;
            noise.strength = 0.08f;
            noise.frequency = 0.12f;
            noise.quality = ParticleSystemNoiseQuality.Low;

            var renderer = dustGO.GetComponent<ParticleSystemRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            // Yumuşak yuvarlak nokta dokusunu kodla üret (harici asset gerekmez)
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader != null)
            {
                var material = new Material(shader);
                var dot = MakeDotTexture();
                if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", dot);
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", dot);
                material.name = "Maden_Toz_Malzemesi";
                renderer.material = material;
            }

            // Tüm ayarlar tamamlandı, toz yağmaya başlasın
            _dust.Play();
        }

        /// <summary>Merkezi parlak, kenarları yumuşak kaybolan küçük nokta dokusu üretir.</summary>
        private static Texture2D MakeDotTexture()
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false) { name = "MadenTozNoktasi" };
            float center = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                    float alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(1f - dist));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply();
            return tex;
        }

        #endregion

        #region Lamba Titremesi

        private void ScanLights()
        {
            _flickerLights.Clear();

            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type != LightType.Point) continue;
                if (!string.IsNullOrEmpty(_flickerExcludeKeyword) && light.name.Contains(_flickerExcludeKeyword)) continue;

                _flickerLights.Add(new FlickerLight
                {
                    light = light,
                    baseIntensity = light.intensity,
                    seed = Random.Range(0f, 100f),
                    dipTimer = 0f
                });
            }

            _nextScanTime = Time.time + 5f;
        }

        private void UpdateFlicker()
        {
            // Sahneye yeni lamba eklendiyse listeyi ara sıra tazele
            if (Time.time >= _nextScanTime) ScanLights();

            float time = Time.time;

            foreach (var flicker in _flickerLights)
            {
                if (flicker.light == null) continue;

                // Organik titreme: farklı hızlarda iki PerlinNoise katmanı üst üste
                float slowNoise = Mathf.PerlinNoise(time * _flickerScale + flicker.seed, flicker.seed);
                float fastNoise = Mathf.PerlinNoise(time * _flickerScale * 3.7f, flicker.seed * 2f);
                float factor = 1f + (slowNoise - 0.5f) * 2f * _flickerStrength + (fastNoise - 0.5f) * _flickerStrength;

                // Ara sıra lambanın kısa süre kasılıp solması
                if (flicker.dipTimer > 0f)
                {
                    flicker.dipTimer -= Time.deltaTime;
                    factor *= Mathf.Lerp(1f, 0.55f, Mathf.Clamp01(flicker.dipTimer / 0.12f));
                }
                else if (Random.value < _dipChance)
                {
                    flicker.dipTimer = 0.12f;
                }

                flicker.light.intensity = flicker.baseIntensity * factor;
            }
        }

        #endregion
    }
}
