using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Project.Systems;
using Project.UI;

namespace MyEditorTools
{
    /// <summary>
    /// UI Sahne Kurulumu: HUD elemanlarını (etkileşim ipucu, delil bildirimi,
    /// fener göstergesi, delil inceleme paneli) sahneye GERÇEK nesne olarak koyar.
    /// Böylece Hierarchy'de görünür, Inspector'dan elle düzenlenebilir.
    /// Menü: Tools/Oyun/UI Sahne Kurulumu (Görünür Düzenleme)
    /// </summary>
    [InitializeOnLoad]
    public static class OyunKurulumUI
    {
        private const string DoneKey = "Oyun_UI_Kurulum_v1";

        static OyunKurulumUI()
        {
            EditorApplication.delayCall += () =>
            {
                if (SessionState.GetBool(DoneKey, false)) return;
                SessionState.SetBool(DoneKey, true);
                RunInternal(auto: true);
            };
        }

        [MenuItem("Tools/Oyun/UI Sahne Kurulumu (Görünür Düzenleme)")]
        public static void RunFromMenu() => RunInternal(auto: false);

        private static void RunInternal(bool auto)
        {
            if (Application.isPlaying) return;

            SetupGameHUD();
            SetupReviewPanel();

            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("<color=green>[UI]</color> Kurulum tamamlandı: HUD nesneleri artık Hierarchy'de görünür ve Inspector'dan düzenlenebilir.");
        }

        private static void SetupGameHUD()
        {
            var hudGO = GameObject.Find("[OyunHUD]");
            if (hudGO != null && hudGO.GetComponent<InteractHUD>() != null && hudGO.GetComponent<LampHUD>() != null) return;

            hudGO = new GameObject("[OyunHUD]");
            Undo.RegisterCreatedObjectUndo(hudGO, "Create HUD");
            hudGO.AddComponent<InteractHUD>();
            hudGO.AddComponent<LampHUD>();

            var canvas = UiBuilder.NewCanvas(hudGO.transform, 100);

            // Etkileşim ipucu — ekranın alt-ortası
            var prompt = UiBuilder.NewText(canvas.transform, "EtkilesimIpucu", string.Empty, 30f,
                new Vector2(0.5f, 0.16f), new Vector2(1000f, 90f), new Color(1f, 0.88f, 0.7f));

            // Delil bildirimi — ekranın üst-ortası
            var toast = UiBuilder.NewText(canvas.transform, "DelilBildirimi", string.Empty, 34f,
                new Vector2(0.5f, 0.84f), new Vector2(1200f, 90f), new Color(1f, 0.9f, 0.75f));

            // Fener göstergesi — sağ alt köşe
            var frame = UiBuilder.NewImage(canvas.transform, "FenerGosterge", new Color(0f, 0f, 0f, 0.55f));
            var frameRect = frame.rectTransform;
            frameRect.anchorMin = new Vector2(1f, 0f);
            frameRect.anchorMax = new Vector2(1f, 0f);
            frameRect.pivot = new Vector2(1f, 0f);
            frameRect.anchoredPosition = new Vector2(-48f, 48f);
            frameRect.sizeDelta = new Vector2(320f, 26f);

            var fill = UiBuilder.NewImage(frame.transform, "Doluluk", new Color(1f, 0.78f, 0.45f));
            var fillRect = fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(3f, 3f);
            fillRect.offsetMax = new Vector2(-3f, -3f);
            fillRect.pivot = new Vector2(0f, 0.5f);

            var label = UiBuilder.NewText(frame.transform, "FenerYazi", "FENER YAĞI  %100", 19f,
                new Vector2(0.5f, 0.5f), new Vector2(300f, 24f), new Color(0.95f, 0.9f, 0.8f));

            var focus = UiBuilder.NewText(frame.transform, "OdakYazi", string.Empty, 20f,
                new Vector2(0.5f, 0.5f), new Vector2(400f, 30f), new Color(1f, 0.85f, 0.6f));
            focus.rectTransform.anchoredPosition = new Vector2(0f, 26f);

            // Script referanslarını bağla
            var soIH = new SerializedObject(hudGO.GetComponent<InteractHUD>());
            soIH.FindProperty("_promptText").objectReferenceValue = prompt;
            soIH.FindProperty("_toastText").objectReferenceValue = toast;
            soIH.ApplyModifiedPropertiesWithoutUndo();

            var soLH = new SerializedObject(hudGO.GetComponent<LampHUD>());
            soLH.FindProperty("_fill").objectReferenceValue = fill;
            soLH.FindProperty("_labelText").objectReferenceValue = label;
            soLH.FindProperty("_focusText").objectReferenceValue = focus;
            soLH.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("<color=cyan>[UI]</color> [OyunHUD] sahneye eklendi (ipucu + bildirim + fener göstergesi).");
        }

        private static void SetupReviewPanel()
        {
            var panelGO = GameObject.Find("[DelilInceleme]");
            if (panelGO != null && panelGO.GetComponent<EvidenceReviewPanel>() != null) return;

            panelGO = new GameObject("[DelilInceleme]");
            Undo.RegisterCreatedObjectUndo(panelGO, "Create Evidence Review Panel");
            var panel = panelGO.AddComponent<EvidenceReviewPanel>();

            var canvas = UiBuilder.NewCanvas(panelGO.transform, 150); // HUD'un (100) üstünde

            var group = canvas.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;

            // Karartma katmanı
            var dim = UiBuilder.NewImage(canvas.transform, "Karartma", new Color(0f, 0f, 0f, 0.72f));
            UiBuilder.Stretch(dim.rectTransform);

            // Merkez kart
            var card = UiBuilder.NewImage(canvas.transform, "Kart", new Color(0.07f, 0.065f, 0.06f, 0.97f));
            var cardRect = card.rectTransform;
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(1100f, 560f);

            var title = UiBuilder.NewText(card.transform, "Baslik", string.Empty, 40f,
                new Vector2(0.5f, 0.9f), new Vector2(980f, 80f), new Color(1f, 0.82f, 0.55f));

            var body = UiBuilder.NewText(card.transform, "Aciklama", string.Empty, 27f,
                new Vector2(0.5f, 0.47f), new Vector2(950f, 380f), new Color(0.92f, 0.9f, 0.86f), TextAnchor.UpperLeft);

            var hint = UiBuilder.NewText(card.transform, "Ipucu", "[ E ]  Kapat", 22f,
                new Vector2(0.5f, 0.08f), new Vector2(980f, 50f), new Color(0.75f, 0.72f, 0.68f), TextAnchor.MiddleCenter, false);

            var so = new SerializedObject(panel);
            so.FindProperty("_group").objectReferenceValue = group;
            so.FindProperty("_titleText").objectReferenceValue = title;
            so.FindProperty("_bodyText").objectReferenceValue = body;
            so.FindProperty("_hintText").objectReferenceValue = hint;
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("<color=cyan>[UI]</color> [DelilInceleme] sahneye eklendi (inceleme kartı).");
        }
    }
}
