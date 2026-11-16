using Project.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Fener yağı göstergesi: sağ altta amber çubuk + yüzde yazısı.
    /// Odak modunda "[SAĞ TIK] ODAK IŞIK" yazar, yağ azaldıkça çubuk kırmızıya döner.
    /// Tercihen sahnede hazır bulunur (OyunKurulumUI koyar); yoksa kendini kurar.
    /// </summary>
    public class LampHUD : MonoBehaviour
    {
        private static LampHUD _instance;

        [Header("--- Sahneden Atanır (OyunKurulumUI ile doldurulur) ---")]
        [Tooltip("Çubuğun kendisi — genişliği yağ oranıyla ölçeklenir")]
        [SerializeField] private Image _fill;

        [Tooltip("FENER YAĞI %XX yazısı")]
        [SerializeField] private Text _labelText;

        [Tooltip("Odak modunda beliren yazı")]
        [SerializeField] private Text _focusText;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null) return;
            var go = new GameObject("[FenerHUD]");
            go.AddComponent<LampHUD>();
        }

        private void Awake()
        {
            _instance = this;

            // Sahneden atanmamışsa kendimizi kurarız (yedek yol)
            if (_fill == null || _labelText == null || _focusText == null) BuildFallback();

            UpdateBar(1f);
            LampSystem.OilChanged += UpdateBar;
            LampSystem.FocusChanged += OnFocusChanged;
        }

        private void BuildFallback()
        {
            var canvas = UiBuilder.NewCanvas(transform, 90);

            // Sağ alt köşe: göstergenin çerçevesi
            var frame = UiBuilder.NewImage(canvas.transform, "Gosterge", new Color(0f, 0f, 0f, 0.55f));
            var frameRect = frame.rectTransform;
            frameRect.anchorMin = new Vector2(1f, 0f);
            frameRect.anchorMax = new Vector2(1f, 0f);
            frameRect.pivot = new Vector2(1f, 0f);
            frameRect.anchoredPosition = new Vector2(-48f, 48f);
            frameRect.sizeDelta = new Vector2(320f, 26f);

            // Doluluk çubuğu (soldan sağa dolar)
            _fill = UiBuilder.NewImage(frame.transform, "Doluluk", new Color(1f, 0.78f, 0.45f));
            var fillRect = _fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(3f, 3f);
            fillRect.offsetMax = new Vector2(-3f, -3f);
            fillRect.pivot = new Vector2(0f, 0.5f);

            _labelText = UiBuilder.NewText(frame.transform, "FenerYazi", string.Empty, 19f,
                new Vector2(0.5f, 0.5f), new Vector2(300f, 24f), new Color(0.95f, 0.9f, 0.8f));

            _focusText = UiBuilder.NewText(frame.transform, "OdakYazi", string.Empty, 20f,
                new Vector2(0.5f, 0.5f), new Vector2(400f, 30f), new Color(1f, 0.85f, 0.6f));
            _focusText.rectTransform.anchoredPosition = new Vector2(0f, 26f);
        }

        private void OnDestroy()
        {
            LampSystem.OilChanged -= UpdateBar;
            LampSystem.FocusChanged -= OnFocusChanged;
        }

        private void UpdateBar(float percent)
        {
            if (_fill == null) return;

            _fill.rectTransform.localScale = new Vector3(Mathf.Clamp01(percent), 1f, 1f);

            if (percent < 0.2f) _fill.color = new Color(0.9f, 0.35f, 0.25f);       // kırmızı: az!
            else if (percent < 0.5f) _fill.color = new Color(1f, 0.62f, 0.35f);    // turuncu: azalıyor
            else _fill.color = new Color(1f, 0.78f, 0.45f);                        // amber: iyi

            _labelText.text = $"FENER YAĞI  %{Mathf.RoundToInt(percent * 100f)}";
        }

        private void OnFocusChanged(bool focusing)
        {
            if (_focusText != null) _focusText.text = focusing ? "[ SAĞ TIK ]  ODAK IŞIK" : string.Empty;
        }
    }
}
