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
    /// Tercihen sahnede hazır bulunur (OyunKurulumUI koyar); yoksa kendini kurar.
    /// </summary>
    public class EvidenceReviewPanel : MonoBehaviour
    {
        public static EvidenceReviewPanel Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance._open;

        [Header("--- Sahneden Atanır (OyunKurulumUI ile doldurulur) ---")]
        [Tooltip("Panelin açılıp kapanmasını sağlayan grup (alfası 0 = kapalı)")]
        [SerializeField] private CanvasGroup _group;

        [Tooltip("Delilin adı")]
        [SerializeField] private Text _titleText;

        [Tooltip("Delilin açıklaması")]
        [SerializeField] private Text _bodyText;

        [Tooltip("Alt ipucu: [ E ] Kapat")]
        [SerializeField] private Text _hintText;

        private EvidenceItem _current;
        private bool _open;
        private float _openedAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("[DelilInceleme]");
            go.AddComponent<EvidenceReviewPanel>();
        }

        private void Awake()
        {
            Instance = this;

            // Sahneden atanmamışsa kendimizi kurarız (yedek yol)
            if (_group == null || _titleText == null || _bodyText == null || _hintText == null) BuildFallback();
        }

        private void BuildFallback()
        {
            var canvas = UiBuilder.NewCanvas(transform, 150); // HUD'un (100) üstünde

            // Karartma katmanı
            var dim = UiBuilder.NewImage(canvas.transform, "Karartma", new Color(0f, 0f, 0f, 0.72f));
            UiBuilder.Stretch(dim.rectTransform);

            // Merkez kart
            var card = UiBuilder.NewImage(canvas.transform, "Kart", new Color(0.07f, 0.065f, 0.06f, 0.97f));
            var cardRect = card.rectTransform;
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(1100f, 560f);

            _titleText = UiBuilder.NewText(card.transform, "Baslik", string.Empty, 40f,
                new Vector2(0.5f, 0.9f), new Vector2(980f, 80f), new Color(1f, 0.82f, 0.55f));

            _bodyText = UiBuilder.NewText(card.transform, "Aciklama", string.Empty, 27f,
                new Vector2(0.5f, 0.47f), new Vector2(950f, 380f), new Color(0.92f, 0.9f, 0.86f), TextAnchor.UpperLeft);

            _hintText = UiBuilder.NewText(card.transform, "Ipucu", "[ E ]  Kapat", 22f,
                new Vector2(0.5f, 0.08f), new Vector2(980f, 50f), new Color(0.75f, 0.72f, 0.68f), TextAnchor.MiddleCenter, false);

            _group = canvas.AddComponent<CanvasGroup>();
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
            _titleText.text = item.title;
            _bodyText.text = string.IsNullOrEmpty(item.description) ? "(Açıklama yok)" : item.description;
            _hintText.text = item.isKeyEvidence ? "[ E ]  Kapat    —    Ana Kanıt" : "[ E ]  Kapat";

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
    }
}
