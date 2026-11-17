using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Project.UI;

namespace MyEditorTools
{
    /// <summary>
    /// GİRİŞ SAHNESİ KURULUM ARACI: Tek tıkla "Giris" sahnesini oluşturur.
    /// İçinde: siyah zeminli kamera + [GirisSinematigi] (IntroScreen bileşeni)
    /// + TAM DÜZENLENEBİLİR CANVAS (Zemin, Fotograf, Baslik, Metin, Ipucu).
    /// Canvas sahneye gerçek nesne olarak kurulur — Hierarchy'de görünür,
    /// Inspector'dan renk/konum/boyut senin tarafından değiştirilebilir.
    /// Sinematik bitince IntroScreen otomatik olarak "Oyun" sahnesini yükler.
    /// Build Settings'e de her iki sahneyi sırasıyla yazar (Giris = 1. sahne).
    /// Kullanım: Tools → Oyun → Giriş Sahnesini Oluştur (v2 — Canvas sahneye kurulur)
    /// </summary>
    public static class GirisSahnesiKur
    {
        [MenuItem("Tools/Oyun/Giriş Sahnesini Oluştur")]
        public static void Olustur()
        {
            // 1) Sahneyi kur
            var yeniSahne = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Siyah zeminli kamera (ses dinleyici de burada)
            var kameraGO = new GameObject("Kamera");
            var cam = kameraGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            kameraGO.transform.position = new Vector3(0f, 0f, -10f);
            kameraGO.AddComponent<AudioListener>();
            // URP ek verisi (konsol uyarısını susturur)
            kameraGO.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();

            // Sinematik denetleyici
            var introGO = new GameObject("[GirisSinematigi]");
            introGO.AddComponent<IntroScreen>();

            // 2) DÜZENLENEBİLİR CANVAS — sahneye gerçek nesne olarak
            var canvasGO = new GameObject("GirisCanvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200; // her şeyin üstünde
            var scaler = canvasGO.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f); // senin istediğin taban çözünürlük
            scaler.matchWidthOrHeight = 0f; // yüksekliğe hizala — yazı boyutu her ekranda aynı

            // Zemin: zifiri siyah
            var zeminGO = new GameObject("Zemin", typeof(Image));
            zeminGO.transform.SetParent(canvasGO.transform, false);
            Doldur(zeminGO.GetComponent<RectTransform>());
            var zeminImg = zeminGO.GetComponent<Image>();
            zeminImg.color = Color.black;

            // Fotoğraf: canvas merkezinde, tam ekran kaplama (boyut IntroScreen'de hesaplanır)
            var fotoGO = new GameObject("Fotograf", typeof(Image), typeof(CanvasGroup));
            fotoGO.transform.SetParent(canvasGO.transform, false);
            var fotoRt = fotoGO.GetComponent<RectTransform>();
            fotoRt.anchorMin = new Vector2(0.5f, 0.5f);
            fotoRt.anchorMax = new Vector2(0.5f, 0.5f);
            fotoRt.pivot = new Vector2(0.5f, 0.5f);
            fotoRt.sizeDelta = new Vector2(1920f, 1080f);
            var fotoImg = fotoGO.GetComponent<Image>();
            fotoImg.preserveAspect = false; // kenarlardan taşırarak tam kaplama (cover)
            var fotoGrup = fotoGO.GetComponent<CanvasGroup>();
            fotoGrup.alpha = 0f;

            // Yazılar — sinematik yerleşim, hepsi ORTALI (HudLabel: TMP varsa TMP, yoksa Legacy)
            var baslik = HudLabel.Create(canvasGO.transform, "Baslik", 60f, new Vector2(0.5f, 0.87f), new Vector2(1400f, 90f));
            baslik.TextColor = new Color(0.97f, 0.96f, 0.94f);
            baslik.Bold = true;
            baslik.TopLeft = false;

            var metin = HudLabel.Create(canvasGO.transform, "Metin", 52f, new Vector2(0.5f, 0.055f), new Vector2(1700f, 180f));
            metin.TextColor = new Color(0.97f, 0.97f, 0.95f);
            metin.Bold = true;
            metin.TopLeft = false;

            var ipucu = HudLabel.Create(canvasGO.transform, "Ipucu", 20f, new Vector2(0.5f, 0.15f), new Vector2(1000f, 44f));
            ipucu.TextColor = new Color(0.65f, 0.65f, 0.62f);
            ipucu.Bold = false;
            ipucu.TopLeft = false;

            // 3) Kaydet
            EditorSceneManager.SaveScene(yeniSahne, "Assets/_Project/Scenes/Giris.unity");

            // 4) Build Settings: Giris ilk (1. sahne), Oyun ikinci
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/_Project/Scenes/Giris.unity", true),
                new EditorBuildSettingsScene("Assets/_Project/Scenes/Oyun.unity", true),
            };

            Debug.Log("<color=green>[Giriş Sahnesi]</color> Giris.unity kuruldu: Kamera + [GirisSinematigi] + GirisCanvas " +
                      "(Zemin/Fotograf/Baslik/Metin/Ipucu — hepsi Hierarchy'de, Inspector'dan düzenlenebilir). " +
                      "Build Settings: 1) Giris  2) Oyun. Ctrl+P ile dene!");
        }

        /// <summary>RectTransform'i tüm ekranı kaplar.</summary>
        private static void Doldur(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
