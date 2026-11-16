using Project.Data;
using Project.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Etkileşim ipucu (ekran altı) ve delil bildirimi (ekran üstü) HUD'u.
    /// Tercihen sahnede hazır bulunur (OyunKurulumUI koyar; Inspector'dan düzenlenebilir);
    /// sahnede yoksa oyun başlarken kendini kurar.
    /// </summary>
    public class InteractHUD : MonoBehaviour
    {
        private static InteractHUD _instance;

        [Header("--- Sahneden Atanır (OyunKurulumUI ile doldurulur) ---")]
        [Tooltip("Ekranda bakılan nesnenin ipucunu gösteren yazı")]
        [SerializeField] private Text _promptText;

        [Tooltip("Kanıt toplandığında üstte beliren bildirim yazısı")]
        [SerializeField] private Text _toastText;

        private float _toastUntil;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null) return;
            var go = new GameObject("[OyunHUD]");
            go.AddComponent<InteractHUD>();
        }

        private void Awake()
        {
            _instance = this;

            // Sahneden atanmamışsa kendimizi kurarız (yedek yol)
            if (_promptText == null || _toastText == null) BuildFallback();

            _promptText.text = string.Empty;
            _toastText.text = string.Empty;
        }

        private void BuildFallback()
        {
            var canvas = UiBuilder.NewCanvas(transform, 100);

            _promptText = UiBuilder.NewText(canvas.transform, "EtkilesimIpucu", string.Empty, 30f,
                new Vector2(0.5f, 0.16f), new Vector2(1000f, 90f), new Color(1f, 0.88f, 0.7f));

            _toastText = UiBuilder.NewText(canvas.transform, "DelilBildirimi", string.Empty, 34f,
                new Vector2(0.5f, 0.84f), new Vector2(1200f, 90f), new Color(1f, 0.9f, 0.75f));
        }

        private void OnEnable()
        {
            Interactor.PromptChanged += SetPrompt;
            EvidenceManager.EvidenceAdded += OnEvidenceAdded;
        }

        private void OnDisable()
        {
            Interactor.PromptChanged -= SetPrompt;
            EvidenceManager.EvidenceAdded -= OnEvidenceAdded;
        }

        private void Update()
        {
            // Bildirim süresi dolunca kaybolur
            if (_toastText != null && _toastText.text.Length > 0 && Time.time > _toastUntil)
            {
                _toastText.text = string.Empty;
            }
        }

        private void SetPrompt(string text)
        {
            if (_promptText != null) _promptText.text = string.IsNullOrEmpty(text) ? string.Empty : text;
        }

        private void OnEvidenceAdded(EvidenceItem item, int keyCount, int total)
        {
            if (_toastText == null || item == null) return;

            string extra = item.isKeyEvidence ? $"  |  Ana kanıt {keyCount}/3" : $"  |  Toplam delil: {total}";
            _toastText.text = $"◆ Kanıt toplandı: {item.title}{extra}";
            _toastUntil = Time.time + 3.5f;
        }
    }
}
