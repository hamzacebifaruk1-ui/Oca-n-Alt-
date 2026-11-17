using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Project.Interaction;
using Project.Player;

namespace MyEditorTools
{
    /// <summary>
    /// TÜNEL ÜRETİCİ (PROTOTİP): oyuncunun baktığı yönde oynanabilir bir kurtarma
    /// koridoru üretir — duvarlar, ahşap tahkimat, raylar, tavan fenerleri,
    /// variller ve yağ kutuları. Projedeki mevcut dokular kullanılır.
    /// Üretimden önce otomatik sahne yedeği alınır (SahneYedekAl).
    /// Menü: Tools/Oyun/Tünel Üreticisi (Prototip)
    /// </summary>
    public class TunelUretici : EditorWindow
    {
        [MenuItem("Tools/Oyun/Tünel Üreticisi (Prototip)")]
        public static void Open() => GetWindow<TunelUretici>("Tünel Üreticisi");

        // --- Ayarlar (pencereden değiştirilir) ---
        private int _segments = 12;
        private float _segLen = 4f;
        private float _width = 3.2f;
        private float _height = 3f;
        private bool _rails = true;
        private bool _lights = true;
        private bool _props = true;
        private bool _oilCans = true;
        private bool _hideOldGallery = true;
        private int _copies = 3;
        private bool _removeBoxCorridor = true;

        private const string RootName = "[ÜretilenKoridor]";
        private const string ExtRootName = "[GaleriUzatma]";
        private const string OldGalleryName = "--- MADEN GALERISI ---";

        private Dictionary<string, Material> _mats;

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Koridor oyuncunun BAKTIĞI yönde üretilir. Üretimden ÖNCE otomatik sahne yedeği alınır.\n" +
                "Beğenmezsen 'Üretileni Sil' ile temizlersin.", MessageType.Info);

            _segments = EditorGUILayout.IntSlider("Bölme sayısı", _segments, 4, 40);
            _segLen = EditorGUILayout.Slider("Bölme uzunluğu (m)", _segLen, 2f, 8f);
            _width = EditorGUILayout.Slider("Koridor genişliği (m)", _width, 2f, 6f);
            _height = EditorGUILayout.Slider("Tavan yüksekliği (m)", _height, 2.2f, 5f);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("İçerik", EditorStyles.boldLabel);
            _rails = EditorGUILayout.Toggle("Raylar + traversler", _rails);
            _lights = EditorGUILayout.Toggle("Tavan fenerleri", _lights);
            _props = EditorGUILayout.Toggle("Variller + moloz yığınları", _props);
            _oilCans = EditorGUILayout.Toggle("Yağ kutuları", _oilCans);
            _hideOldGallery = EditorGUILayout.Toggle("Eski küçük galeriyi gizle", _hideOldGallery);

            EditorGUILayout.Space(10);
            if (GUILayout.Button("KORİDORU ÜRET", GUILayout.Height(36))) Generate();
            if (GUILayout.Button("Üretileni Sil", GUILayout.Height(24))) DeleteGenerated();

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Hazır Modellerle Uzatma", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Sahnedeki MEVCUT galeri parçalarını (kaya duvarları, tahkimat, ray) kopyalayıp zincirleme dizer.\n" +
                "Kutu koridoru otomatik silinir; sadece orijinal modeller kalır.", MessageType.None);
            _copies = EditorGUILayout.IntSlider("Kopya sayısı", _copies, 1, 6);
            _removeBoxCorridor = EditorGUILayout.Toggle("Kutu koridorunu sil", _removeBoxCorridor);
            if (GUILayout.Button("MEVCUT GALERİYİ UZAT", GUILayout.Height(32))) ExtendWithGallery();
        }

        private void Generate()
        {
            var fpc = Object.FindFirstObjectByType<FirstPersonController>();
            if (fpc == null)
            {
                EditorUtility.DisplayDialog("Tünel Üreticisi", "Sahnede Player (FirstPersonController) yok.", "Tamam");
                return;
            }

            // SİGORTA: üretmeden önce tarihli yedek
            SahneYedekAl.BackupScene();

            // Eski üretileni temizle (yeniden üretim için)
            var oldRoot = GameObject.Find(RootName);
            if (oldRoot != null) Undo.DestroyObjectImmediate(oldRoot);

            // Eski küçük galeriyi gizle (istersen geri açılır)
            var gallery = GameObject.Find(OldGalleryName);
            if (gallery != null) gallery.SetActive(!_hideOldGallery);

            // Yön: oyuncunun baktığı yönü eksene yuvarla (duvarlar düzgün otursun)
            var fwd = fpc.transform.forward; fwd.y = 0f;
            Vector3 dir;
            if (Mathf.Abs(fwd.x) > Mathf.Abs(fwd.z)) dir = new Vector3(Mathf.Sign(fwd.x), 0f, 0f);
            else dir = new Vector3(0f, 0f, Mathf.Sign(fwd.z == 0f ? 1f : fwd.z));
            var perp = Vector3.Cross(Vector3.up, dir); // sağ duvar yönü
            var look = Quaternion.LookRotation(dir);   // tüm kutular koridor yönüne hizalanacak

            // Başlangıç: oyuncunun 2m önü, zemine otur
            var startPos = fpc.transform.position + dir * 2f;
            if (Physics.Raycast(startPos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 12f))
                startPos.y = hit.point.y;
            else startPos.y = fpc.transform.position.y - 0.7f;

            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Üretilen Koridor");
            LoadMaterials();

            float halfW = _width * 0.5f;

            for (int i = 0; i < _segments; i++)
            {
                var center = startPos + dir * (i * _segLen + _segLen * 0.5f);

                // Zemin
                Box(root.transform, $"Zemin_{i + 1}", center + Vector3.down * 0.2f, new Vector3(_width, 0.4f, _segLen), _mats["Zemin"], look);

                // Yan duvarlar
                Box(root.transform, $"Duvar_Sag_{i + 1}", center + perp * (halfW + 0.175f), new Vector3(0.35f, _height, _segLen), _mats["Duvar"], look);
                Box(root.transform, $"Duvar_Sol_{i + 1}", center - perp * (halfW + 0.175f), new Vector3(0.35f, _height, _segLen), _mats["Duvar"], look);

                // Tavan
                Box(root.transform, $"Tavan_{i + 1}", center + Vector3.up * (_height + 0.175f), new Vector3(_width, 0.35f, _segLen), _mats["Tavan"], look);

                // Ahşap tahkimat: her bölmenin başına iki dikme + üst kiriş
                var edge = startPos + dir * (i * _segLen);
                var postScale = new Vector3(0.22f, _height, 0.22f);
                Box(root.transform, $"Dikme_Sag_{i + 1}", edge + perp * (halfW - 0.12f) + Vector3.up * (_height * 0.5f), postScale, _mats["Ahşap"], look);
                Box(root.transform, $"Dikme_Sol_{i + 1}", edge - perp * (halfW - 0.12f) + Vector3.up * (_height * 0.5f), postScale, _mats["Ahşap"], look);
                Box(root.transform, $"Kiris_{i + 1}", edge + Vector3.up * (_height - 0.12f), new Vector3(0.24f, 0.22f, _width), _mats["Ahşap"], look);

                // Raylar + traversler
                if (_rails)
                {
                    Box(root.transform, $"Ray_Sag_{i + 1}", center + perp * 0.35f + Vector3.up * 0.03f, new Vector3(0.08f, 0.05f, _segLen), _mats["Metal"], look);
                    Box(root.transform, $"Ray_Sol_{i + 1}", center - perp * 0.35f + Vector3.up * 0.03f, new Vector3(0.08f, 0.05f, _segLen), _mats["Metal"], look);
                    for (int s = 0; s < 5; s++)
                    {
                        var sleep = center + dir * (-_segLen * 0.5f + 0.4f + s * (_segLen / 5f));
                        Box(root.transform, $"Travers_{i + 1}_{s + 1}", sleep + Vector3.up * 0.015f, new Vector3(1.0f, 0.04f, 0.14f), _mats["Ahşap"], look);
                    }
                }

                // Tavan feneri (her iki bölmeye bir)
                if (_lights && i % 2 == 0)
                {
                    var lampPos = center + Vector3.up * (_height - 0.2f);
                    var bulb = Box(root.transform, $"Fener_{i + 1}", lampPos, new Vector3(0.14f, 0.14f, 0.14f), _mats["Ampul"]);
                    var light = bulb.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.color = new Color(1f, 0.82f, 0.6f);
                    light.intensity = 4f;
                    light.range = 9f;
                    light.shadows = LightShadows.Soft;
                    light.shadowStrength = 0.85f;
                }

                // Variller ve moloz (her üçüncü bölme)
                if (_props && i % 3 == 2)
                {
                    var barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    barrel.name = $"Varil_{i + 1}";
                    barrel.transform.SetParent(root.transform, false);
                    barrel.transform.position = center + perp * (halfW - 0.55f) + Vector3.up * 0.45f;
                    barrel.transform.localScale = new Vector3(0.5f, 0.45f, 0.5f);
                    barrel.GetComponent<MeshRenderer>().sharedMaterial = _mats["Varil"];

                    for (int d = 0; d < 3; d++)
                    {
                        var chunk = Box(root.transform, $"Moloz_{i + 1}_{d + 1}",
                            center + dir * (d * 0.8f - 0.8f) - perp * (halfW - 0.5f) + Vector3.up * 0.1f,
                            new Vector3(0.4f, 0.2f, 0.35f), _mats["Moloz"]);
                        chunk.transform.Rotate(0f, Random.Range(0f, 90f), 0f);
                    }
                }

                // Yağ kutusu (her beşinci bölme)
                if (_oilCans && i % 5 == 4)
                {
                    var can = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    can.name = $"YagKutusu_K{i + 1}";
                    can.transform.SetParent(root.transform, false);
                    can.transform.position = center + perp * (halfW - 0.45f) + Vector3.up * 0.36f;
                    can.transform.localScale = new Vector3(0.22f, 0.35f, 0.22f);
                    can.GetComponent<MeshRenderer>().sharedMaterial = _mats["Varil"];
                    can.AddComponent<OilCan>();
                }
            }

            // Koridorun sonu: kaya duvarı
            var end = startPos + dir * (_segments * _segLen);
            Box(root.transform, "Son_Duvar", end + Vector3.up * (_height * 0.5f), new Vector3(_width, _height, 0.35f), _mats["Duvar"], look);

            // Kaydet + kamerayı koridora yönlendir
            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Selection.activeGameObject = root;

            // Kamerayı koridorun ortasına getir — kullanıcı sonucu hemen görsün
            var mid = startPos + dir * (_segments * _segLen * 0.5f);
            FocusSceneOn(mid, _segments * _segLen);

            Debug.Log($"<color=green>[Tünel]</color> {_segments} bölmeli koridor üretildi ve sahne kaydedildi. Başlangıç: {startPos}, yön: {dir}. Oyunu başlatıp yürüyebilirsin!");
        }

        /// <summary>Scene penceresini hedef noktaya odaklar (pencere yoksa açar).</summary>
        private static void FocusSceneOn(Vector3 target, float viewSize)
        {
            var view = SceneView.lastActiveSceneView;
            if (view == null)
            {
                EditorApplication.ExecuteMenuItem("Window/Panels/Scene");
                view = SceneView.lastActiveSceneView;
            }
            if (view == null)
            {
                Debug.LogWarning("[Tünel] Scene penceresi bulunamadı — elle aç (Window > General > Scene).");
                return;
            }

            view.pivot = target + Vector3.up * 1.5f;
            view.size = Mathf.Max(viewSize, 10f);
            view.LookAt(view.pivot, Quaternion.Euler(22f, -30f, 0f));
            view.Repaint();
        }

        /// <summary>
        /// "--- MADEN GALERISI ---" kökünü N kez kopyalayıp kendi uzun ekseninin
        /// yönünde dizer. Böylece sahnedeki hazır kaya/tahkimat modelleri
        /// uzun bir koridora dönüşür. Orijinal galeri korunur.
        /// </summary>
        private void ExtendWithGallery()
        {
            var gallery = GameObject.Find(OldGalleryName);
            if (gallery == null)
            {
                EditorUtility.DisplayDialog("Tünel Üreticisi", "Sahnede '--- MADEN GALERISI ---' bulunamadı.", "Tamam");
                return;
            }

            // SİGORTA
            SahneYedekAl.BackupScene();

            // Eski üretilen koridorları temizle
            var boxRoot = GameObject.Find(RootName);
            if (boxRoot != null && _removeBoxCorridor) Undo.DestroyObjectImmediate(boxRoot);
            var oldExt = GameObject.Find(ExtRootName);
            if (oldExt != null) Undo.DestroyObjectImmediate(oldExt);

            // Galerinin uzun ekseni ve adımı (parçaların kapladığı boyut)
            var min = Vector3.positiveInfinity;
            var max = Vector3.negativeInfinity;
            foreach (Transform child in gallery.transform)
            {
                min = Vector3.Min(min, child.position);
                max = Vector3.Max(max, child.position);
            }
            var size = max - min;

            Vector3 dir;
            float step;
            if (size.x >= size.z) { dir = Vector3.right; step = size.x; }
            else { dir = Vector3.forward; step = size.z; }

            if (step < 5f)
            {
                EditorUtility.DisplayDialog("Tünel Üreticisi", "Galeri parçaları bir eksen boyunca dizilmemiş görünüyor; uzatma yapılamadı.", "Tamam");
                return;
            }

            var root = new GameObject(ExtRootName);
            Undo.RegisterCreatedObjectUndo(root, "Galeri Uzatma");

            for (int k = 1; k <= _copies; k++)
            {
                var clone = Object.Instantiate(gallery, root.transform);
                clone.name = $"Galeri_Uzatma_{k}";
                clone.transform.position = gallery.transform.position + dir * (step * k);
            }

            // Kaydet + kamerayı uzatmanın ortasına getir
            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            var mid = gallery.transform.position + dir * (step * (_copies * 0.5f + 0.5f));
            FocusSceneOn(mid, step * (_copies + 1));

            Debug.Log($"<color=green>[Tünel]</color> Galerinin {_copies} kopyası {dir} yönünde dizildi (adım: {step:N1} m). Koridor artık ~{step * (_copies + 1):N0} m!");
        }

        private void DeleteGenerated()
        {
            var root = GameObject.Find(RootName);
            if (root == null)
            {
                Debug.Log("[Tünel] Üretilen koridor zaten yok.");
                return;
            }

            Undo.DestroyObjectImmediate(root);

            // Eski galeriyi geri aç
            var gallery = GameObject.Find(OldGalleryName);
            if (gallery != null) gallery.SetActive(true);

            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("<color=green>[Tünel]</color> Üretilen koridor silindi, eski galeri geri açıldı.");
        }

        // --- Yardımcılar ---

        private static GameObject Box(Transform parent, string name, Vector3 position, Vector3 scale, Material mat, Quaternion? rot = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            if (rot.HasValue) go.transform.rotation = rot.Value;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = mat;
            return go;
        }

        private void LoadMaterials()
        {
            _mats = new Dictionary<string, Material>
            {
                ["Zemin"] = EnsureMat("Tunel_Zemin_Mat", "Assets/_Project/Prefabs/Textures/soil_mud.jpg", new Vector2(3f, 4f)),
                ["Duvar"] = EnsureMat("Tunel_Duvar_Mat", "Assets/_Project/Prefabs/Textures/concrete_dirty.jpg", new Vector2(4f, 2f)),
                ["Tavan"] = EnsureMat("Tunel_Tavan_Mat", "Assets/_Project/Prefabs/Textures/debris_01.jpg", new Vector2(3f, 4f)),
                ["Moloz"] = EnsureMat("Tunel_Moloz_Mat", "Assets/_Project/Prefabs/Textures/debris_02.jpg", new Vector2(1f, 1f)),
                ["Metal"] = EnsureMat("Tunel_Metal_Mat", "Assets/_Project/Prefabs/Textures/Metal.jpg", new Vector2(2f, 1f), new Color(0.75f, 0.75f, 0.78f)),
                ["Varil"] = EnsureMat("Tunel_Varil_Mat", "Assets/_Project/Prefabs/Textures/Metal.jpg", new Vector2(2f, 1f), new Color(0.62f, 0.38f, 0.22f)),
                ["Ahşap"] = EnsureMat("Tunel_Ahsap_Mat", null, Vector2.one, new Color(0.42f, 0.28f, 0.15f)),
                ["Ampul"] = EnsureMat("Tunel_Ampul_Mat", null, Vector2.one, new Color(1f, 0.9f, 0.65f)),
            };
        }

        private static readonly string MatDir = "Assets/_Project/Materials/Tunel";

        /// <summary>Materyalleri kalıcı asset olarak üretir (yoksa oluşturur, varsa yükler).</summary>
        private static Material EnsureMat(string name, string texPath, Vector2 tile, Color? tint = null)
        {
            var path = $"{MatDir}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            if (!AssetDatabase.IsValidFolder(MatDir))
                AssetDatabase.CreateFolder("Assets/_Project/Materials", "Tunel");

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex != null)
            {
                mat.mainTexture = tex;
                mat.mainTextureScale = tile;
            }
            if (tint.HasValue) mat.color = tint.Value;
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
    }
}
