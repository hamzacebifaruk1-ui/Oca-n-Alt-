using UnityEngine;

namespace Project.Data
{
    /// <summary>
    /// Bir delilin kartı (verisi). Sahnedeki EvidencePickup bu kartı taşır,
    /// toplandığında EvidenceManager'a kaydedilir.
    /// Finaller bu kartlara göre hesaplanır (Gün Işığı: 3 ana kanıt vb.).
    /// </summary>
    [CreateAssetMenu(menuName = "Ocagin Alti/Delil (Evidence)", fileName = "Evidence_")]
    public class EvidenceItem : ScriptableObject
    {
        [Tooltip("Benzersiz kimlik — final hesabında ve kayıt sistemlerinde kullanılır")]
        public string id;

        [Tooltip("Ekranda görünecek ad: 'Kilit Etiketi' gibi")]
        public string title;

        [TextArea(3, 6)]
        [Tooltip("İnceleme ekranında gösterilecek açıklama")]
        public string description;

        [Tooltip("Hangi bölümde bulunur (1-5)")]
        public int chapter = 1;

        [Tooltip("Final koşuluna giren ANA kanıt mı? (Doküman: 3 ana kanıt)")]
        public bool isKeyEvidence;
    }
}
