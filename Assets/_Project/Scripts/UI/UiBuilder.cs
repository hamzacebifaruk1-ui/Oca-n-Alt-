using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// UI kurulum yardımcısı: hem editör kurulum aracı (OyunKurulumUI) hem de
    /// sahnede HUD yoksa çalıştıran kod yedeği aynı yapıyı üretir.
    /// TMP Essentials gerekmez; Unity'nin dahili LegacyRuntime fontunu kullanır.
    /// </summary>
    public static class UiBuilder
    {
        public static Font DefaultFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        /// <summary>Ekran üstü canvas oluşturur (1920x1080 referans çözünürlük).</summary>
        public static GameObject NewCanvas(Transform parent, int sortingOrder)
        {
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(parent, false);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            return go;
        }

        /// <summary>Yazı oluşturur. anchor: (0-1) ekran oranı, sizeDelta: piksel.</summary>
        public static Text NewText(Transform parent, string name, string text, float size,
            Vector2 anchor, Vector2 sizeDelta, Color color,
            TextAnchor align = TextAnchor.MiddleCenter, bool bold = true)
        {
            var go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = sizeDelta;

            var t = go.GetComponent<Text>();
            t.font = DefaultFont;
            t.text = text;
            t.fontSize = Mathf.RoundToInt(size);
            t.color = color;
            t.alignment = align;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        /// <summary>Düz renkli kutu/panel oluşturur (konumu çağıran ayarlar).</summary>
        public static Image NewImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);

            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Üst elementi tamamen kaplayan dikdörtgen.</summary>
        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
