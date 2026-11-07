using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace MyEditorTools
{
    [InitializeOnLoad]
    public static class MadenLightingSetup
    {
        private const string DoneKey = "Maden_Lighting_Setup_Done_v2";

        static MadenLightingSetup()
        {
            EditorApplication.delayCall += AutoExecute;
        }

        private static void AutoExecute()
        {
            if (SessionState.GetBool(DoneKey, false)) return;
            SessionState.SetBool(DoneKey, true);
            ApplyMadenAtmosphere();
        }

        [MenuItem("Tools/Lighting/Apply Maden Lighting & Atmosphere")]
        public static void ApplyMadenAtmosphere()
        {
            Debug.Log("<color=yellow>[Maden Lighting]</color> Maden atmosferi ve ışıklandırması uygulanıyor...");

            // 1. Directional Light'ı kapat (Yeraltı madeninde güneş ışığı sızması olmaz)
            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    Undo.RecordObject(l.gameObject, "Disable Directional Light");
                    l.gameObject.SetActive(false);
                }
            }

            // 2. RenderSettings (Ortam Işığı ve Sis)
            RenderSettings.ambientMode = AmbientMode.Flat;
            // Zifiri karanlığa yakın gerçekçi maden ortam rengi
            RenderSettings.ambientLight = new Color(0.015f, 0.015f, 0.022f);

            // Sis (Tünelin derinlik hissi ve tozlu maden buğusu)
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.035f, 0.05f, 0.07f); // Koyu kömür/mavi sis
            RenderSettings.fogStartDistance = 4.0f;
            RenderSettings.fogEndDistance = 35.0f;

            // 3. Tünel Tavan Lambaları (Sıcak Akkor / Tungsten Fener Işıkları)
            Color warmLanternColor = new Color(1.0f, 0.72f, 0.38f); // 2700K sıcak sarı/kehribar

            foreach (var l in lights)
            {
                if (l.name.ToLower().Contains("led"))
                {
                    Undo.RecordObject(l, "Configure Mine Lantern");
                    l.type = LightType.Point;
                    l.color = warmLanternColor;
                    l.intensity = 18.0f;
                    l.range = 10.0f;
                    l.shadows = LightShadows.Soft;
                    l.shadowStrength = 0.9f;
                    l.shadowBias = 0.05f;
                    l.shadowNormalBias = 0.4f;
                }
            }

            // 4. Arka Plan Gizemli Soğuk Mavi Işık (Fotoğraftaki tünel ucunda parıldayan derinlik maviliği)
            string bgLightName = "Maden_Derinlik_MaviIsik";
            var existingBgLight = GameObject.Find(bgLightName);
            if (existingBgLight == null)
            {
                existingBgLight = new GameObject(bgLightName);
                Undo.RegisterCreatedObjectUndo(existingBgLight, "Create Mine Background Light");
                var bgLight = existingBgLight.AddComponent<Light>();
                bgLight.type = LightType.Point;
                bgLight.color = new Color(0.18f, 0.58f, 0.85f); // Soğuk turkuaz / mavi parıltı
                bgLight.intensity = 15.0f;
                bgLight.range = 16.0f;
                bgLight.shadows = LightShadows.Soft;

                // Tünelin derin ucunda konumlandır (+X yönünde)
                existingBgLight.transform.position = new Vector3(18.0f, 1.8f, 7.2f);
            }
            else
            {
                var bgLight = existingBgLight.GetComponent<Light>();
                if (bgLight != null)
                {
                    bgLight.color = new Color(0.18f, 0.58f, 0.85f);
                    bgLight.intensity = 15.0f;
                    bgLight.range = 16.0f;
                    bgLight.shadows = LightShadows.Soft;
                }
            }

            // 5. Volume Profile Oluşturma (Sub-asset olarak doğru kayıt)
            string settingsFolder = "Assets/_Project/Settings";
            if (!Directory.Exists(settingsFolder))
            {
                Directory.CreateDirectory(settingsFolder);
                AssetDatabase.Refresh();
            }

            string profilePath = $"{settingsFolder}/MadenVolumeProfile.asset";
            AssetDatabase.DeleteAsset(profilePath);

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "MadenVolumeProfile";
            AssetDatabase.CreateAsset(profile, profilePath);

            ConfigureVolumeEffects(profile);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            // 6. Sahnede Global Volume Nesnesi Oluştur veya Güncelle
            string volumeGameObjectName = "[Global Volume - Maden]";
            var volumeGO = GameObject.Find(volumeGameObjectName);
            if (volumeGO == null)
            {
                volumeGO = new GameObject(volumeGameObjectName);
                Undo.RegisterCreatedObjectUndo(volumeGO, "Create Maden Global Volume");
            }

            var volume = volumeGO.GetComponent<Volume>();
            if (volume == null)
            {
                volume = volumeGO.AddComponent<Volume>();
            }

            volume.isGlobal = true;
            volume.priority = 1.0f;
            volume.weight = 1.0f;
            volume.sharedProfile = profile;

            // 7. Kamera Post-Processing Ayarını Aç
            var mainCam = Camera.main;
            if (mainCam != null)
            {
                Undo.RecordObject(mainCam, "Setup Camera For Maden");
                mainCam.nearClipPlane = 0.05f;

                if (mainCam.TryGetComponent<UniversalAdditionalCameraData>(out var camData))
                {
                    Undo.RecordObject(camData, "Enable Camera Post Processing");
                    camData.renderPostProcessing = true;
                    camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                    camData.volumeLayerMask = ~0; // Tüm katmanlardaki Volume'leri algılasın
                }
            }

            // Sahneyi kaydet
            var activeScene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("<color=green>[Maden Lighting]</color> <b>Maden atmosferi başarıyla uygulandı!</b>");
            Debug.Log("- ACES Tonemapping, Sıcak Fener Bloomi, Split Toning, Derinlik Sisi ve Tünel Karartma (Vignette) aktif.");
        }

        private static T AddComponentToProfile<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet<T>(out var existing))
            {
                return existing;
            }

            var comp = profile.Add<T>(true);
            comp.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
            AssetDatabase.AddObjectToAsset(comp, profile);
            return comp;
        }

        private static void ConfigureVolumeEffects(VolumeProfile profile)
        {
            // 1. Tonemapping (ACES - Sinematik Yüksek Dinamik Kontrast)
            var tonemapping = AddComponentToProfile<Tonemapping>(profile);
            tonemapping.mode.overrideState = true;
            tonemapping.mode.value = TonemappingMode.ACES;

            // 2. Bloom (Fenerlerin etrafında sıcak altın rengi ışık halesi)
            var bloom = AddComponentToProfile<Bloom>(profile);
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 0.85f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 1.1f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.7f;
            bloom.tint.overrideState = true;
            bloom.tint.value = new Color(1.0f, 0.82f, 0.60f);

            // 3. Color Adjustments (Kömür madeni kontrastı ve ton ayarı)
            var colorAdjustments = AddComponentToProfile<ColorAdjustments>(profile);
            colorAdjustments.postExposure.overrideState = true;
            colorAdjustments.postExposure.value = 0.15f;
            colorAdjustments.contrast.overrideState = true;
            colorAdjustments.contrast.value = 24.0f; // Güçlü gölgeler
            colorAdjustments.saturation.overrideState = true;
            colorAdjustments.saturation.value = 6.0f;

            // 4. White Balance (Hafif sıcaklık)
            var whiteBalance = AddComponentToProfile<WhiteBalance>(profile);
            whiteBalance.temperature.overrideState = true;
            whiteBalance.temperature.value = 14.0f;
            whiteBalance.tint.overrideState = true;
            whiteBalance.tint.value = -3.0f;

            // 5. Split Toning (Ön plandaki sıcak sarı vs arka plandaki soğuk mavi ayrımı)
            var splitToning = AddComponentToProfile<SplitToning>(profile);
            splitToning.shadows.overrideState = true;
            splitToning.shadows.value = new Color(0.12f, 0.20f, 0.32f); // Gölgelerde soğuk derinlik
            splitToning.highlights.overrideState = true;
            splitToning.highlights.value = new Color(1.0f, 0.80f, 0.48f); // Işıklarda sıcak kehribar
            splitToning.balance.overrideState = true;
            splitToning.balance.value = -8.0f;

            // 6. Vignette (Tünel klostrofobisi ve kenar karartması)
            var vignette = AddComponentToProfile<Vignette>(profile);
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.38f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.45f;
            vignette.rounded.overrideState = true;
            vignette.rounded.value = false;

            // 7. Film Grain (Doğal, hafif pürüzlü yeraltı filmi hissi)
            var filmGrain = AddComponentToProfile<FilmGrain>(profile);
            filmGrain.type.overrideState = true;
            filmGrain.type.value = FilmGrainLookup.Medium1;
            filmGrain.intensity.overrideState = true;
            filmGrain.intensity.value = 0.12f;
            filmGrain.response.overrideState = true;
            filmGrain.response.value = 0.8f;
        }
    }
}
