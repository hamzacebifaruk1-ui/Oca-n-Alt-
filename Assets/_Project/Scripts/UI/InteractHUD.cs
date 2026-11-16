using Project.Data;
using Project.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Etkileşim ipucu (ekran altı) ve delil bildirimi (ekran üstü) HUD'u.
    /// Oyun başlarken kendini kurar; sahneye elle eklemek gerekmez.
    /// </summary>
    public class InteractHUD : MonoBehaviour
    {
        private static InteractHUD _instance;

        private HudLabel _prompt;
        private HudLabel _toast;
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

            // Canvas kurulumu (ekran üstü katman)
            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            // Etkileşim ipucu — ekranın alt-ortası, maden amber tonunda
            _prompt = HudLabel.Create(canvasGO.transform, "EtkilesimIpucu", 30f, new Vector2(0.5f, 0.16f), new Vector2(1000f, 90f));
            _prompt.TextColor = new Color(1f, 0.88f, 0.7f);
            _prompt.Bold = true;
            _prompt.Text = string.Empty;

            // Delil bildirimi — ekranın üst-ortası
            _toast = HudLabel.Create(canvasGO.transform, "DelilBildirimi", 34f, new Vector2(0.5f, 0.84f), new Vector2(1200f, 90f));
            _toast.TextColor = new Color(1f, 0.9f, 0.75f);
            _toast.Bold = true;
            _toast.Text = string.Empty;
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
            if (_toast != null && _toast.Text.Length > 0 && Time.time > _toastUntil)
            {
                _toast.Text = string.Empty;
            }
        }

        private void SetPrompt(string text)
        {
            if (_prompt != null) _prompt.Text = string.IsNullOrEmpty(text) ? string.Empty : text;
        }

        private void OnEvidenceAdded(EvidenceItem item, int keyCount, int total)
        {
            if (_toast == null || item == null) return;

            string extra = item.isKeyEvidence ? $"  |  Ana kanıt {keyCount}/3" : $"  |  Toplam delil: {total}";
            _toast.Text = $"◆ Kanıt toplandı: {item.title}{extra}";
            _toastUntil = Time.time + 3.5f;
        }
    }
}
