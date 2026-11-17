using System.Collections;
using Project.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Project.UI
{
    /// <summary>
    /// GİRİŞ SİNEMATİĞİ — akışkan film modu (eski "SinematikGiris" tarzı):
    /// - Fotoğraflar TAM EKRAN (cover) gösterilir, geçişler yumuşak kararmayla yapılır
    /// - Altyazılar altta DAKTİLO efektiyle harf harf yazılır, sonra sönerek silinir
    /// - Her slaytın kendi anlatım sesi (SES: satırı) çalar; ses bitince sıradakine geçilir
    /// - BOŞLUK / ENTER / E ile her an TÜM sinematik atlanır → oyun yüklenir
    /// - Metinler, fotoğraflar ve sesler Assets/Resources/Intro/intro_metinler.txt'den okunur
    ///   (Not Defteri'yle düzenlenebilir)
    /// - Giris sahnesinde hazır Canvas varsa onu kullanır (Hierarchy'de görünür,
    ///   Inspector'dan düzenlenebilir); yoksa kendini kurar
    /// </summary>
    public class IntroScreen : MonoBehaviour
    {
        public static bool IsActive { get; private set; }

        // GİRİŞ SİNEMATİĞİ: "Giris" sahnesinde çalışır; bitince oyun sahnesi (Oyun) yüklenir.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            // Sadece Giris sahnesinde kendi kendine kurulur;
            // oyun sahnesinde gereksiz (orada oyuncu direk başlar).
            if (!SceneManager.GetActiveScene().name.Equals("Giris")) return;
            if (FindAnyObjectByType<IntroScreen>() != null) return;
            var go = new GameObject("[GirisSinematigi]");
            go.AddComponent<IntroScreen>();
        }

        private struct Slide
        {
            public Sprite photo;
            public string title;
            public string body;
            public string hint;
            public AudioClip voice; // slaytın kendi anlatım sesi (varsa)
        }

        private Image _background;
        private Image _photo;
        private CanvasGroup _photoGroup;
        private HudLabel _title;
        private HudLabel _body;
        private HudLabel _hint;
        private Slide[] _slides;
        private AudioSource _voiceSource;   // slayt anlatım sesi
        private FirstPersonController _player;
        private bool _gecisYapiliyor;

        [Header("Altyazı Ayarları")]
        [Tooltip("Altyazı yazı boyutu — Inspector'dan buradan AYARLA, kod değiştirmesin. Oyun çalışırken de sürükleyebilirsin.")]
        [Range(24f, 120f)]
        public float AltyaziPunto = 64f;
        private float _sonPunto = -1f;

        [Tooltip("Altyazı yazı rengi — Inspector'dan buradan seç (renk paleti açılır).")]
        public Color AltyaziRengi = new Color(0.97f, 0.97f, 0.95f, 1f);
        private Color _sonRenk = Color.clear;

        // Daktilo ve geçiş ayarları (eski projendeki değerler)
        private const float DaktiloHarfSuresi = 0.035f;
        private const float FotoSonSuresi = 0.4f;
        private const float FotoAcilisSuresi = 0.8f;
        private const float AltyaziSonSuresi = 0.5f;

        private void Awake()
        {
            IsActive = true;

            _slides = BuildSlides();

            // Sahnede hazır UI varsa onu kullan; yoksa kendini kurar
            if (!TryAdoptSceneUI())
            {
                var canvasGO = new GameObject("Canvas", typeof(Canvas));
                canvasGO.transform.SetParent(transform, false);
                var canvas = canvasGO.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 200; // her şeyin üstünde
                var scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0f; // yüksekliğe hizala — yazı boyutu her ekranda aynı

                // Zifiri siyah zemin
                var bgGO = new GameObject("Zemin", typeof(Image));
                bgGO.transform.SetParent(canvasGO.transform, false);
                Stretch(bgGO.GetComponent<RectTransform>());
                _background = bgGO.GetComponent<Image>();
                _background.color = Color.black;

                // Fotoğraf: tam ekran kaplama (cover)
                var photoGO = new GameObject("Fotograf", typeof(Image), typeof(CanvasGroup));
                photoGO.transform.SetParent(canvasGO.transform, false);
                Ortala(photoGO.GetComponent<RectTransform>());
                _photo = photoGO.GetComponent<Image>();
                _photo.preserveAspect = false;
                _photoGroup = photoGO.GetComponent<CanvasGroup>();
                _photoGroup.alpha = 0f;

                // Yazılar — sinematik yerleşim (hepsi ORTALI)
                _title = HudLabel.Create(canvasGO.transform, "Baslik", 60f, new Vector2(0.5f, 0.87f), new Vector2(1400f, 90f));
                _title.TextColor = new Color(0.97f, 0.96f, 0.94f);
                _title.Bold = true;
                _title.TopLeft = false;

                _body = HudLabel.Create(canvasGO.transform, "Metin", 52f, new Vector2(0.5f, 0.055f), new Vector2(1700f, 180f));
                _body.TextColor = new Color(0.97f, 0.97f, 0.95f);
                _body.Bold = true;
                _body.TopLeft = false;

                _hint = HudLabel.Create(canvasGO.transform, "Ipucu", 20f, new Vector2(0.5f, 0.15f), new Vector2(1000f, 44f));
                _hint.TextColor = new Color(0.65f, 0.65f, 0.62f);
                _hint.Bold = false;
                _hint.TopLeft = false;
            }

            // Oyuncu varsa (eski mod — oyun sahnesinde) sinematik boyunca sustur
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
                source.volume = 0.35f;
                source.Play();
            }

            // Slayt anlatım sesleri için ayrı bir kanal
            _voiceSource = gameObject.AddComponent<AudioSource>();
            _voiceSource.playOnAwake = false;
            _voiceSource.volume = 1f;
        }

        private void Start()
        {
            StartCoroutine(SinematikAkisi());
        }

        private void Update()
        {
            if (_gecisYapiliyor) return;

            // Canvas boyutu ilk karede oturur / pencere boyutu değişince fotoğrafı yeniden hizala
            BoyutKontrol();

            // Puntoyı SADECE KULLANICI ayarlar: Inspector'daki kaydırıcı değişirse altyazıya uygula
            if (!Mathf.Approximately(AltyaziPunto, _sonPunto))
            {
                _sonPunto = AltyaziPunto;
                if (_body != null) _body.Size = AltyaziPunto;
            }

            // Rengi SADECE KULLANICI ayarlar: Inspector'daki renk seçici değişirse altyazıya uygula
            if (AltyaziRengi != _sonRenk)
            {
                _sonRenk = AltyaziRengi;
                if (_body != null) _body.TextColor = AltyaziRengi;
            }

            // BOŞLUK / ENTER / E: sinematiğin tamamını atla, oyuna geç
            if (AtlaBasildi())
            {
                _gecisYapiliyor = true;
                StopAllCoroutines();
                if (_voiceSource != null) _voiceSource.Stop();
                StartCoroutine(OyunaGecis());
            }
        }

        /// <summary>Sinematik akışı: her slayt — fotoğraf geçişi + daktilo altyazı + ses + sessizlik.</summary>
        private IEnumerator SinematikAkisi()
        {
            yield return null; // canvas ilk karede yerleşsin (ilk fotoğraf tam oturmalı)
            _hint.Text = string.Empty;
            _title.Text = string.Empty; // başlık yok — sadece fotoğraf + altyazı
            Sprite oncekiFoto = null;

            foreach (var slide in _slides)
            {
                // Fotoğraf değiştiyse yumuşak geçiş yap
                if (slide.photo != oncekiFoto)
                {
                    yield return StartCoroutine(FotografGecisi(slide.photo));
                    oncekiFoto = slide.photo;
                }

                // Metin konumu: fotoğraf varsa altta altyazı, yoksa ortada (KONTROLLER gibi)
                var bodyRt = _body != null ? _body.Rect : null;
                if (bodyRt != null)
                {
                    if (slide.photo != null)
                    {
                        bodyRt.anchorMin = new Vector2(0.5f, 0.055f);
                        bodyRt.anchorMax = new Vector2(0.5f, 0.055f);
                        bodyRt.sizeDelta = new Vector2(1700f, 180f);
                    }
                    else
                    {
                        bodyRt.anchorMin = new Vector2(0.5f, 0.32f);
                        bodyRt.anchorMax = new Vector2(0.5f, 0.32f);
                        bodyRt.sizeDelta = new Vector2(1250f, 300f);
                    }
                }

                // Slaytın kendi anlatım sesi (varsa) çal
                if (_voiceSource != null)
                {
                    _voiceSource.Stop();
                    _voiceSource.clip = slide.voice;
                    if (slide.voice != null) _voiceSource.Play();
                }

                // Daktilo efektiyle altyazı
                yield return StartCoroutine(AltyaziYaz(slide.body));

                // Ses çalıyorsa bitmesini bekle; yoksa okuma süresi kadar bekle
                if (slide.voice != null && _voiceSource != null && _voiceSource.isPlaying)
                {
                    yield return new WaitWhile(() => _voiceSource.isPlaying);
                    yield return new WaitForSeconds(0.9f);
                }
                else
                {
                    yield return new WaitForSeconds(2.0f + slide.body.Length * 0.045f);
                }

                // Altyazı sönerek silinir
                yield return StartCoroutine(AltyaziSil(AltyaziSonSuresi));
                yield return new WaitForSeconds(0.35f);
            }

            // Sinematik bitti: son fotoğrafın üstünde altta "BOŞLUK — Madene İn" bekler
            var sonRt = _body != null ? _body.Rect : null;
            if (sonRt != null)
            {
                sonRt.anchorMin = new Vector2(0.5f, 0.055f);
                sonRt.anchorMax = new Vector2(0.5f, 0.055f);
                sonRt.sizeDelta = new Vector2(1700f, 180f);
            }
            if (_body != null) _body.Text = "[ BOŞLUK ]   Madene İn";
        }

        /// <summary>Fotoğrafı sönerek değiştirir, yenisini açarak gösterir (cover fit ile).</summary>
        private IEnumerator FotografGecisi(Sprite yeni)
        {
            // Eski fotoğraf solarak kaybolur
            float t = 0f;
            while (t < FotoSonSuresi)
            {
                t += Time.deltaTime;
                _photoGroup.alpha = Mathf.Lerp(_photoGroup.alpha, 0f, t / FotoSonSuresi);
                yield return null;
            }
            _photoGroup.alpha = 0f;

            _photo.sprite = yeni;
            FitPhotoCover();

            // Yeni fotoğraf karardan açılır
            t = 0f;
            while (t < FotoAcilisSuresi)
            {
                t += Time.deltaTime;
                _photoGroup.alpha = Mathf.Lerp(0f, 1f, t / FotoAcilisSuresi);
                yield return null;
            }
            _photoGroup.alpha = 1f;
        }

        /// <summary>Altyazıyı harf harf yazar (daktilo efekti).</summary>
        private IEnumerator AltyaziYaz(string metin)
        {
            _body.Text = string.Empty;
            foreach (var harf in metin)
            {
                _body.Text += harf;
                yield return new WaitForSeconds(DaktiloHarfSuresi);
            }
        }

        /// <summary>Altyazıyı sönerek siler.</summary>
        private IEnumerator AltyaziSil(float sure)
        {
            var orijinal = _body.TextColor;
            float t = 0f;
            while (t < sure)
            {
                t += Time.deltaTime;
                var renk = orijinal;
                renk.a = Mathf.Lerp(1f, 0f, t / sure);
                _body.TextColor = renk;
                yield return null;
            }
            _body.Text = string.Empty;
            orijinal.a = 1f;
            _body.TextColor = orijinal;
        }

        /// <summary>Sinematik bitince: kararmaya geç ve oyunu yükle (ya da oyuncuyu devreye al).</summary>
        private IEnumerator OyunaGecis()
        {
            // Fotoğraf kararanlığa gömülür
            float t = 0f;
            while (t < 1.2f)
            {
                t += Time.deltaTime;
                _photoGroup.alpha = Mathf.Lerp(_photoGroup.alpha, 0f, t / 1.2f);
                yield return null;
            }
            _photoGroup.alpha = 0f;

            IsActive = false;

            if (_player != null)
            {
                // Oyun sahnesinde açıldıysa (eski mod): oyuncuyu devreye al
                _player.enabled = true;
                _player.LockCursor(true);
                var interactor = _player.GetComponent<Interaction.Interactor>();
                if (interactor != null) interactor.enabled = true;
            }
            else
            {
                // Giris sahnesindeyiz: sinematik bitti, oyunu yükle
                SceneManager.LoadScene("Oyun");
            }

            Destroy(gameObject);
        }

        private Vector2 _sonCanvasBoy; // fotoğraf yeniden hizalama için canvas boyutu takibi

        /// <summary>Canvas boyutu değiştiyse (ilk kare, pencere boyutu) fotoğrafı yeniden oturtur.</summary>
        private void BoyutKontrol()
        {
            if (_photo == null || _photo.canvas == null) return;
            var canvasRt = _photo.canvas.transform as RectTransform;
            if (canvasRt == null) return;

            var boy = new Vector2(canvasRt.rect.width, canvasRt.rect.height);
            if (boy.x < 1f || boy.y < 1f) return; // boyut henüz oturmamış
            if (Mathf.Abs(boy.x - _sonCanvasBoy.x) < 0.5f && Mathf.Abs(boy.y - _sonCanvasBoy.y) < 0.5f) return;

            _sonCanvasBoy = boy;
            FitPhotoCover();
        }

        /// <summary>BOŞLUK / ENTER / E kontrolü (iki giriş sistemini de destekler).</summary>
        private static bool AtlaBasildi()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame)) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E);
#endif
        }

        private Slide[] BuildSlides()
        {
            // Önce Assets/Resources/Intro/intro_metinler.txt dosyasını dene
            var parsed = ParseSlidesFile();
            if (parsed != null && parsed.Length > 0) return parsed;

            // Dosya yoksa / boşsa: hazır metinler (yedek)
            return new[]
            {
                new Slide { photo = LoadPhoto("intro_gorsel1"), title = "OCAĞIN ALTI", voice = LoadVoice("intro_ses01_maden"), body = "Zonguldak  —  Kara Elmas Ocağı  —  Kasım 1996.", hint = "" },
                new Slide { photo = LoadPhoto("intro_gorsel2"), title = "", voice = LoadVoice("intro_ses02_vardiya"), body = "Her vardiya aynı sözle başlardı: \"Herkes döner.\"   1989'da o söz bozuldu.", hint = "" },
                new Slide { photo = LoadPhoto("intro_gorsel7"), title = "", voice = null, body = "İniş başlıyor.", hint = "[ BOŞLUK ]   Ocağa İn" },
            };
        }

        /// <summary>
        /// intro_metinler.txt dosyasını okur ve slaytlara çevirir.
        /// Format:
        ///   === SLAYT 1 ===
        ///   BASLIK: ...        (isteğe bağlı)
        ///   FOTO: intro_gorsel1  (isteğe bağlı — Resources/Intro içindeki dosya adı)
        ///   SES: intro_ses01_maden (isteğe bağlı — slaytın anlatım sesi)
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
                else if (line.StartsWith("SES:", System.StringComparison.OrdinalIgnoreCase))
                {
                    var fileName = line.Substring("SES:".Length).Trim();
                    if (!string.IsNullOrEmpty(fileName)) current.voice = LoadVoice(fileName);
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

        /// <summary>
        /// Giris sahnesinde önceden kurulmuş UI varsa devralır (Inspector'dan
        /// düzenleyebilmen için). Canvas altında Zemin / Fotograf / Baslik / Metin /
        /// Ipucu nesneleri aranır; hepsi varsa true döner.
        /// </summary>
        private bool TryAdoptSceneUI()
        {
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return false;

            var zeminTr = canvas.transform.Find("Zemin");
            var fotoTr = canvas.transform.Find("Fotograf");
            var baslikTr = canvas.transform.Find("Baslik");
            var metinTr = canvas.transform.Find("Metin");
            var ipucuTr = canvas.transform.Find("Ipucu");
            if (zeminTr == null || fotoTr == null || baslikTr == null || metinTr == null || ipucuTr == null) return false;

            _background = zeminTr.GetComponent<Image>();
            _photo = fotoTr.GetComponent<Image>();
            _photoGroup = fotoTr.GetComponent<CanvasGroup>();
            _title = baslikTr.GetComponent<HudLabel>();
            _body = metinTr.GetComponent<HudLabel>();
            _hint = ipucuTr.GetComponent<HudLabel>();
            if (_background == null || _photo == null || _photoGroup == null ||
                _title == null || _body == null || _hint == null) return false;

            // Fotoğrafı tam ekran kaplama (cover) moduna al
            var fotoRt = _photo.rectTransform;
            fotoRt.anchorMin = new Vector2(0.5f, 0.5f);
            fotoRt.anchorMax = new Vector2(0.5f, 0.5f);
            fotoRt.pivot = new Vector2(0.5f, 0.5f);
            _photo.preserveAspect = false;

            // Yazılar her zaman ortalı (sahnedeki eski hizayı ez)
            _title.TopLeft = false;
            _body.TopLeft = false;
            _hint.TopLeft = false;

            return true;
        }

        /// <summary>RectTransform'i canvas merkezine sabitler (cover fit için).</summary>
        private static void Ortala(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        /// <summary>
        /// Fotoğrafı ekranı TAM kaplayacak şekilde boyutlar (gerçek canvas boyutuna göre;
        /// oran bozulmaz, kenarlardan taşar — film jeneriği görüntüsü).
        /// </summary>
        private void FitPhotoCover()
        {
            if (_photo == null) return;
            var rt = _photo.rectTransform;

            // Canvas'ın GERÇEK boyutunu kullan (referans 1920x1080 değil) —
            // böylece ekran oranı ne olursa olsun kenarlarda boşluk kalmaz.
            var boy = new Vector2(1920f, 1080f);
            if (_photo.canvas != null)
            {
                var canvasRt = _photo.canvas.transform as RectTransform;
                if (canvasRt != null && canvasRt.rect.width > 1f && canvasRt.rect.height > 1f)
                    boy = canvasRt.rect.size;
            }

            if (_photo.sprite == null)
            {
                rt.sizeDelta = boy;
                return;
            }

            var gorselOran = _photo.sprite.texture.width / (float)_photo.sprite.texture.height;
            var ekranOran = boy.x / boy.y;

            if (gorselOran > ekranOran)
                rt.sizeDelta = new Vector2(boy.y * gorselOran, boy.y); // geniş görsel: yanlardan taşar
            else
                rt.sizeDelta = new Vector2(boy.x, boy.x / gorselOran); // dik görsel: üst-alttan taşar
        }

        /// <summary>Fotoğrafı Texture olarak yükleyip Sprite'a çevirir. Uzantı yazılsa da temizlenir.</summary>
        private static Sprite LoadPhoto(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;

            // "intro_gorsel1.png" gibi yazılırsa uzantıyı at
            var dot = fileName.LastIndexOf('.');
            if (dot > 0) fileName = fileName.Substring(0, dot);

            var tex = Resources.Load<Texture2D>("Intro/" + fileName);
            if (tex == null) return null;
            return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }

        /// <summary>Slaytın anlatım sesini Resources/Intro'dan yükler. Uzantı yazılsa da temizlenir.</summary>
        private static AudioClip LoadVoice(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;

            // "intro_ses01_maden.mp3" gibi yazılırsa uzantıyı at
            var dot = fileName.LastIndexOf('.');
            if (dot > 0) fileName = fileName.Substring(0, dot);

            return Resources.Load<AudioClip>("Intro/" + fileName);
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
