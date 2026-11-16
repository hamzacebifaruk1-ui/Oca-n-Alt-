using Project.Systems;
using UnityEngine;

namespace Project.Interaction
{
    /// <summary>
    /// Fener yağı kutusu: E ile kullanılınca feneri tamamen doldurur ve tükenir.
    /// Maden raflarına, arabalara, kulübelere yerleştirilir (OyunKurulumM2 otomatik koyar).
    /// </summary>
    public class OilCan : MonoBehaviour, IInteractable
    {
        [Tooltip("Bakılırken gösterilecek yazı")]
        [SerializeField] private string _prompt = "Fener Yağı Doldur";

        [Tooltip("Kutu tükendiğinde sahneden yok edilsin mi?")]
        [SerializeField] private bool _destroyOnUse = true;

        private bool _used;

        public string InteractionPrompt => _used ? string.Empty : _prompt;
        public bool CanInteract => !_used;

        private void Awake()
        {
            // Kutunun dikkat çekmesi için hafif kendine ışık (uzaktan minik parıltı)
            var glow = gameObject.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.75f, 0.4f);
            glow.intensity = 0.35f;
            glow.range = 1.6f;
            glow.shadows = LightShadows.None;
        }

        public void Interact(Interactor interactor)
        {
            if (_used) return;
            _used = true;

            var lamp = LampSystem.Instance;
            if (lamp != null)
            {
                lamp.RefillFull();
                Debug.Log("<color=green>[Fener]</color> Yağ dolduruldu.");
            }

            if (_destroyOnUse) Destroy(gameObject);
            else gameObject.SetActive(false);
        }
    }
}
