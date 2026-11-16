using System.Collections;
using Project.Player;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Project.UI
{
    /// <summary>
    /// Giriş sinematiği: fotoğraf + hikâye metni + arkada ambiyans sesi + kontrol kartı.
    /// Metinler Assets/Resources/Intro/intro_metinler.txt dosyasından okunur
    /// (Not Defteri'yle düzenlenebilir); dosya yoksa hazır yedek metinler kullanılır.
    /// Fotoğraflar Assets/Resources/Intro içinde intro_foto1/2/3 olarak beklenir;
    /// yoksa düz karanlık zeminle çalışır. Ses için intro_ambience dosyası otomatik yakalanır.
    /// Oyun başlarken kendini kurar; sıra bitince oyuncuyu devreye alır.
    /// </summary>
    public class IntroScreen : MonoBehaviour
    {
        public static bool IsActive { get; private set; }

        /*
         * GİRİŞ SİNEMATİĞİ ŞU ANDA KAPALI — Play'e basınca doğrudan oyunda başlar.
         * İleride tekrar açmak için: aşağıdaki satırların başındaki ve sonundaki
         * "/* ... *"/ işaretlerini kaldır ve kaydet. Metinler intro_metinler.txt'den okunur.
         *
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindAnyObjectByType<IntroScreen>() != null) return;
            var go = new GameObject("[GirisSinematigi]");
            go.AddComponent<IntroScreen>();
        }
        */

        private struct Slide
        {
            public Sprite photo;
            public string title;
            public string body;
            public string hint;
        }

        private Image _background;
        private Image _photo;
        private CanvasGroup _photoGroup;
        private HudLabel _title;
        private HudLabel _body;
        private HudLabel _hint;
        private Slide[] _slides;
        private int _index = -1;
        private float _slideShownAt;
        private bool _finishing;

        private FirstPersonController _player;

        private void Awake()
        {
            IsActive = true;

            _slides = BuildSlides();

            var canvasGO = new GameObject("Canvas", typeof(Canvas));
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200; // her şeyin üstünde

            // Zifiri siyah zemin
            var bgGO = new GameObject("Zemin", typeof(Image));
            bgGO.transform.SetParent(canvasGO.transform, false);
            Stretch(bgGO.GetComponent<RectTransform>());
            _background = bgGO.GetComponent<Image>();
            _background.color = Color.black;

            // Fotoğraf (varsa)
            var photoGO = new GameObject("Fotograf", typeof(Image), typeof(CanvasGroup));
            photoGO.transform.SetParent(canvasGO.transform, false);
            Stretch(photoGO.GetComponent<RectTransform>());
            _photo = photoGO.GetComponent<Image>();
            _photo.preserveAspect = true;
            _photoGroup = photoGO.GetComponent<CanvasGroup>();
            _photoGroup.alpha = 0f;

            // Metinler
            _title = HudLabel.Create(canvasGO.transform, "Baslik", 64f, new Vector2(0.5f, 0.72f), new Vector2(1400f, 120f));
            _title.TextColor = new Color(1f, 0.85f, 0.6f);
            _title.Bold = true;

            _body = HudLabel.Create(canvasGO.transform, "Metin", 30f, new Vector2(0.5f, 0.3f), new Vector2(1250f, 300f));
            _body.TextColor = new Color(0.92f, 0.9f, 0.86f);
            _body.Bold = true;

            _hint = HudLabel.Create(canvasGO.transform, "Ipucu", 22f, new Vector2(0.5f, 0.07f), new Vector2(1000f, 50f));
            _hint.TextColor = new Color(0.7f, 0.68f, 0.65f);
            _hint.Bold = false;

            // Oyuncuyu sinematik boyunca sustur
            _player = FindAnyObjectByType<FirstPersonController>();
            if (_player != null) _player.enabled = false;
            var interactor = _player != null ? _player.GetComponent<Interaction.Interactor>() : null;
            if (interactor != null) interactor.enabled = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Ambiyans sesi (varsa): Assets/Resources/Intro/intro_ambience
            var clip = Resources.Load<AudioClip>("Intro/intro_ambience");
            if (clip != null)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.clip = clip;
                source.loop = true;
                source.volume = 0.7f;
                source.Play();
            }

            NextSlide();
        }

        private Slide[] BuildSlides()
        {
            // Önce Assets/Resources/Intro/intro_metinler.txt dosyasını dene
            // (Not Defteri'yle düzenlenebilir; boş satır ayracında "|" = yeni satır)
            var parsed = ParseSlidesFile();
            if (parsed != null && parsed.Length > 0) return parsed;

            // Dosya yoksa / boşsa: hazır metinler (yedek)
            return new[]
            {
                new Slide { photo = LoadPhoto("intro_foto1"), title = "OCAĞIN ALTI", body = "Zonguldak  •  Kara Elmas Ocağı  •  Kasım 1996\n\n\"Karanlıkta duyduğun her ses sana yol göstermez.\nIşık ise neyin gerçekten orada olduğunu söyler.\"", hint = "[ E ]  Devam" },
                new Slide { photo = LoadPhoto("intro_foto2"), title = string.Empty, body = "Kadir Demirci, 1989'da kaybolan babasının izini sürmek için\nyeniden açılması planlanan Kara Elmas Ocağı'na denetime iner.\n\nBeş bölüm boyunca gerçek kayıtları bulacak,\nsesleri lambasıyla sınayacak ve sonunda kararı verecek.", hint = "[ E ]  Devam" },
                new Slide { photo = LoadPhoto("intro_foto3"), title = string.Empty, body = "\"Yeraltında kulaklarına güvenme.\nGözünü lambaya ver.\"\n\n— Osman Demirci", hint = "[ E ]  Devam" },
                new Slide { photo = null, title = "KONTROLLER", body = "WASD / Oklar   Yürü        Shift   Koş\nC   Eğil (aç/kapa)        Ctrl   Eğil (basılı tut)\nBoşluk   Zıpla             E   İncele\nESC   Fare imlecini serbest bırak", hint = "[ E ]  Ocağa İn" },
            };
        }

        /// <summary>
        /// intro_metinler.txt dosyasını okur ve slaytlara çevirir.
        /// Format:
        ///   === SLAYT 1 ===
        ///   BASLIK: ...        (isteğe bağlı)
        ///   FOTO: intro_foto1  (isteğe bağlı — Resources/Intro içindeki dosya adı, uzantısız)
        ///   METIN: satır|satır ( "|" işareti yeni satır demek )
        ///   IPUCU: [ E ] Devam (isteğe bağlı)
        /// </summary>
        private static Slide[] ParseSlidesFile()
        {
            TextAsset asset;
            try { asset = Resources.Load<TextAsset>("Intro/intro_metinler"); }
            catch { asset = null; }
            if (asset == null || string.IsNullOrEmpty(asset.text)) return null;

            var slides = new System.Collections.Generic.List<Slide>();
            Slide current = default;
            bool inSlide = false;

            var lines = asset.text.Replace("\r\n", "\n").Split('\n');
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                if (line.StartsWith("=== SLAYT", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (inSlide) slides.Add(current);
                    current = default;
                    inSlide = true;
                    continue;
                }

                if (!inSlide) continue;

                if (line.StartsWith("BASLIK:", System.StringComparison.OrdinalIgnoreCase))
                {
                    current.title = line.Substring("BASLIK:".Length).Trim();
                }
                else if (line.StartsWith("FOTO:", System.StringComparison.OrdinalIgnoreCase))
                {
                    var fileName = line.Substring("FOTO:".Length).Trim();
                    if (!string.IsNullOrEmpty(fileName)) current.photo = LoadPhoto(fileName);
                }
                else if (line.StartsWith("METIN:", System.StringComparison.OrdinalIgnoreCase))
                {
                    var text = line.Substring("METIN:".Length).Trim();
                    current.body = text.Replace("|", "\n");
                }
                else if (line.StartsWith("IPUCU:", System.StringComparison.OrdinalIgnoreCase))
                {
                    current.hint = line.Substring("IPUCU:".Length).Trim();
                }
            }
            if (inSlide) slides.Add(current);

            // En az başlık ya da metni olan slaytları say
            int valid = 0;
            foreach (var s in slides) if (!string.IsNullOrEmpty(s.title) || !string.IsNullOrEmpty(s.body)) valid++;
            if (valid == 0) return null;

            return slides.ToArray();
        }

        /// <summary>Fotoğrafı Texture olarak yükleyip Sprite'a çevirir (import ayarından bağımsız çalışır). Uzantı yazılsa da yazarlıktan temizlenir.</summary>
        private static Sprite LoadPhoto(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;

            // "intro_foto1.jpg" gibi yazılırsa uzantıyı at
            var dot = fileName.LastIndexOf('.');
            if (dot > 0) fileName = fileName.Substring(0, dot);

            var tex = Resources.Load<Texture2D>("Intro/" + fileName);
            if (tex == null) return null;
            return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }

        private void Update()
        {
            if (_finishing) return;

            // E / Boşluk / tık ile ilerle; 9 saniyede otomatik geç
            bool advance = AdvancePressed() || Time.unscaledTime - _slideShownAt > 9f;
            if (advance) NextSlide();
        }

        private bool AdvancePressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) return true;
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0);
#endif
        }

        private void NextSlide()
        {
            _index++;

            if (_index >= _slides.Length)
            {
                StartCoroutine(FinishSequence());
                return;
            }

            _slideShownAt = Time.unscaledTime;

            var slide = _slides[_index];
            _photo.sprite = slide.photo;
            _photo.gameObject.SetActive(slide.photo != null);
            _photoGroup.alpha = slide.photo != null ? 0.9f : 0f;
            _title.Text = slide.title;
            _body.Text = slide.body;
            _hint.Text = slide.hint;
        }

        private IEnumerator FinishSequence()
        {
            if (_finishing) yield break;
            _finishing = true;

            // Oyuncuya geçiş: fotoğraf sönerek karanlığa gömülür
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / 0.8f;
                if (_photoGroup != null) _photoGroup.alpha = Mathf.Lerp(_photoGroup.alpha, 0f, t);
                yield return null;
            }

            IsActive = false;

            if (_player != null)
            {
                _player.enabled = true;
                _player.LockCursor(true);
            }
            var interactor = _player != null ? _player.GetComponent<Interaction.Interactor>() : null;
            if (interactor != null) interactor.enabled = true;

            Destroy(gameObject);
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
