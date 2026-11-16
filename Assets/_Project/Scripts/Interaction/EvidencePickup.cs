using Project.Data;
using UnityEngine;

namespace Project.Interaction
{
    /// <summary>
    /// Sahnedeki fiziksel delil nesnesi. Bir EvidenceItem kartı taşır;
    /// oyuncu bakıp E'ye basınca delil envantere eklenir, nesne toplanmış olur.
    /// </summary>
    public class EvidencePickup : MonoBehaviour, IInteractable
    {
        [Header("--- Delil ---")]
        [Tooltip("Bu nesnenin taşıdığı delil kartı")]
        [SerializeField] private EvidenceItem _evidence;

        [Tooltip("Toplanınca sahneden kaldırılsın mı?")]
        [SerializeField] private bool _destroyAfterCollect = true;

        public string InteractionPrompt => _evidence != null ? $"İncele: {_evidence.title}" : "İncele";

        public bool CanInteract => _evidence != null && EvidenceManager.Instance != null && !EvidenceManager.Instance.HasEvidence(_evidence);

        public void Interact(Interactor interactor)
        {
            if (!CanInteract) return;

            if (EvidenceManager.Instance.AddEvidence(_evidence) && _destroyAfterCollect)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
