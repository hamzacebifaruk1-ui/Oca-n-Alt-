using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// HUD yazı etiketi: TMP Essentials varsa TextMeshPro, yoksa Unity'nin
    /// dahili Legacy Text sistemini kullanır. Böylece font eksikliğinde
    /// yazılar asla kaybolmaz.
    /// </summary>
    public class HudLabel : MonoBehaviour
    {
        private TMP_Text _tmp;
        private Text _legacy;
        private RectTransform _rect;

        /// <summary>Yeni etiket oluşturur ve konumunu ayarlar.</summary>
        public static HudLabel Create(Transform parent, string name, float fontSize, Vector2 anchor, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var label = go.AddComponent<HudLabel>();
            label._rect = go.GetComponent<RectTransform>();
            label._rect.anchorMin = anchor;
            label._rect.anchorMax = anchor;
            label._rect.pivot = new Vector2(0.5f, 0.5f);
            label._rect.sizeDelta = sizeDelta;
            label.Size = fontSize;
            return label;
        }

        private void Awake()
        {
            bool tmpAvailable = false;
            try { tmpAvailable = TMP_Settings.defaultFontAsset != null; } catch { tmpAvailable = false; }

            // Sahne içinden geliyorsa _rect kayıt edilmediği için null olabilir — kendimiz bulalım
            if (_rect == null) _rect = GetComponent<RectTransform>();

            if (tmpAvailable)
            {
                _tmp = gameObject.AddComponent<TextMeshProUGUI>();
                _tmp.raycastTarget = false;
                _tmp.textWrappingMode = TextWrappingModes.Normal; // (eski adı: enableWordWrapping)
            }
            else
            {
                _legacy = gameObject.AddComponent<Text>();
                _legacy.raycastTarget = false;
                _legacy.horizontalOverflow = HorizontalWrapMode.Wrap;
                _legacy.verticalOverflow = VerticalWrapMode.Overflow;
                _legacy.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_legacy.font == null) _legacy.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
        }

        public string Text
        {
            get => _tmp != null ? _tmp.text : _legacy.text;
            set { if (_tmp != null) _tmp.text = value; else if (_legacy != null) _legacy.text = value; }
        }

        public Color TextColor
        {
            get => _tmp != null ? _tmp.color : _legacy.color;
            set { if (_tmp != null) _tmp.color = value; else if (_legacy != null) _legacy.color = value; }
        }

        public float Size
        {
            set
            {
                if (_tmp != null) _tmp.fontSize = value;
                else if (_legacy != null) _legacy.fontSize = Mathf.RoundToInt(value);
            }
        }

        public bool Bold
        {
            set
            {
                if (_tmp != null) _tmp.fontStyle = value ? FontStyles.Bold : FontStyles.Normal;
                else if (_legacy != null) _legacy.fontStyle = value ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        public bool TopLeft
        {
            set
            {
                if (_tmp != null) _tmp.alignment = value ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.Center;
                else if (_legacy != null) _legacy.alignment = value ? TextAnchor.UpperLeft : TextAnchor.MiddleCenter;
            }
        }

        public RectTransform Rect => _rect != null ? _rect : (_rect = GetComponent<RectTransform>());
    }
}
