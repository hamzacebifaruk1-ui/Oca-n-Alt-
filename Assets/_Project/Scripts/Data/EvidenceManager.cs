using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Data
{
    /// <summary>
    /// Toplanan delillerin merkezi. Finaller buna bakar:
    /// - Gün Işığı: 3 ana kanıt + dosya verilmedi
    /// - Sessizlik: Asıl Kayıt + en az 1 doğrulayıcı + dosya verildi
    /// - Vardiya: Karar Anı 1'de lamba bırakıldı (delilden bağımsız)
    /// Oyun başlarken kendini kurar, sahneye elle eklenmez.
    /// </summary>
    public class EvidenceManager : MonoBehaviour
    {
        public static EvidenceManager Instance { get; private set; }

        /// <summary>(delil, ana delil sayısı, toplam delil) — HUD dinler.</summary>
        public static event Action<EvidenceItem, int, int> EvidenceAdded;

        private readonly List<EvidenceItem> _collected = new List<EvidenceItem>();

        public IReadOnlyList<EvidenceItem> Collected => _collected;
        public int KeyEvidenceCount { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance == null)
            {
                var go = new GameObject("[EvidenceManager]");
                go.AddComponent<EvidenceManager>();
            }
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Delili envantere ekler. Aynı delil iki kez eklenemez.</summary>
        public bool AddEvidence(EvidenceItem item)
        {
            if (item == null || _collected.Contains(item)) return false;

            _collected.Add(item);
            if (item.isKeyEvidence) KeyEvidenceCount++;

            EvidenceAdded?.Invoke(item, KeyEvidenceCount, _collected.Count);
            Debug.Log($"<color=green>[Delil]</color> Toplandı: {item.title} (Ana kanıt {KeyEvidenceCount}, toplam {_collected.Count})");
            return true;
        }

        public bool HasEvidence(EvidenceItem item) => item != null && _collected.Contains(item);

        /// <summary>Yeni oyuna başlarken envanteri sıfırlar.</summary>
        public void ResetRun()
        {
            _collected.Clear();
            KeyEvidenceCount = 0;
        }
    }
}
