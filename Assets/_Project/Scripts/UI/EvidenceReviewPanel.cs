using Project.Data;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Project.UI
{
    /// <summary>
    /// Delil inceleme ekranı: bir delil toplandığında tam ekran açılır,
    /// başlık + açıklama gösterilir, E/ESC/tık ile kapatılır.
    /// Oyun başlarken kendini kurar; sahneye elle eklenmez.
    /// </summary>
    public class EvidenceReviewPanel : MonoBehaviour
    {
        public static EvidenceReviewPanel Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance._open;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("[DelilInceleme]");
            go.AddComponent<EvidenceReviewPanel>();
        }

        private CanvasGroup _group;
        private HudLabel _title;
        private HudLabel _body;
        private HudLabel _hint;
        private EvidenceItem _current;
        private bool _open;
        private float _openedAt;

        private void Awake()
        {
            Instance = this;

            var canvasGO = new GameObject("Canvas", typeof(Canvas));
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150; // HUD'un (100) üstünde

            // Karartma katmanı
            var dimGO = new GameObject("Karartma", typeof(Image));
            dimGO.transform.SetParent(canvasGO.transform, false);
            Stretch(dimGO.GetComponent<RectTransform>());
            dimGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

            // Merkez kart
            var cardGO = new GameObject("Kart", typeof(Image));
            cardGO.transform.SetParent(canvasGO.transform, false);
            var cardRect = cardGO.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(1100f, 560f);
            cardGO.GetComponent<Image>().color = new Color(0.07f, 0.065f, 0.06f, 0.97f);

            _title = HudLabel.Create(cardGO.transform, "Baslik", 40f, new Vector2(0.5f, 0.9f), new Vector2(980f, 80f));
            _title.TextColor = new Color(1f, 0.82f, 0.55f);
            _title.Bold = true;

            _body = HudLabel.Create(cardGO.transform, "Aciklama", 27f, new Vector2(0.5f, 0.47f), new Vector2(950f, 380f));
            _body.TextColor = new Color(0.92f, 0.9f, 0.86f);
            _body.TopLeft = true;

            _hint = HudLabel.Create(cardGO.transform, "Ipucu", 22f, new Vector2(0.5f, 0.08f), new Vector2(980f, 50f));
            _hint.TextColor = new Color(0.75f, 0.72f, 0.68f);
            _hint.Bold = false;
            _hint.Text = "[ E ]  Kapat";

            _group = canvasGO.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
        }

        private void OnEnable()
        {
            EvidenceManager.EvidenceAdded += Show;
        }

        private void OnDisable()
        {
            EvidenceManager.EvidenceAdded -= Show;
        }

        private void Show(EvidenceItem item, int keyCount, int total)
        {
            if (item == null) return;

            _current = item;
            _title.Text = item.title;
            _body.Text = string.IsNullOrEmpty(item.description) ? "(Açıklama yok)" : item.description;
            _hint.Text = item.isKeyEvidence ? "[ E ]  Kapat    —    Ana Kanıt" : "[ E ]  Kapat";

            _open = true;
            _openedAt = Time.unscaledTime;
            _group.alpha = 1f;
        }

        private void Update()
        {
            if (!_open) return;

            // Kapatma tuşları (aynı tuşla toplama-çakışmasını önlemek için kısa gecikme var)
            if (Time.unscaledTime > _openedAt + 0.45f && ClosePressed())
            {
                Close();
            }
        }

        private bool ClosePressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.eKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)) return true;
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
#endif
        }

        private void Close()
        {
            _open = false;
            _current = null;
            _group.alpha = 0f;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
