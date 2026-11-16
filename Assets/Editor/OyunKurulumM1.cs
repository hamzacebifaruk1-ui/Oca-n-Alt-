using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Project.Data;
using Project.Interaction;

namespace MyEditorTools
{
    /// <summary>
    /// M1 Kurulumu: delil kartlarını oluşturur, Player'a Interactor ekler,
    /// test için bir örnek delil nesnesi sahneye yerleştirir.
    /// Menü: Tools/Oyun/M1 Kurulum
    /// </summary>
    [InitializeOnLoad]
    public static class OyunKurulumM1
    {
        private const string DoneKey = "Oyun_M1_Kurulum_v1";

        static OyunKurulumM1()
        {
            EditorApplication.delayCall += () =>
            {
                if (SessionState.GetBool(DoneKey, false)) return;
                SessionState.SetBool(DoneKey, true);
                RunInternal(auto: true);
            };
        }

        [MenuItem("Tools/Oyun/M1 Kurulum (Etkileşim + Delil)")]
        public static void RunFromMenu() => RunInternal(auto: false);

        private static void RunInternal(bool auto)
        {
            if (Application.isPlaying) return;

            var fpc = Object.FindFirstObjectByType<Project.Player.FirstPersonController>();
            if (fpc == null)
            {
                if (!auto) Debug.LogWarning("[M1] Sahnedede Player (FirstPersonController) bulunamadı.");
                return;
            }

            // 1) Player'a Interactor ekle
            var playerGO = fpc.gameObject;
            if (playerGO.GetComponent<Interactor>() == null)
            {
                Undo.AddComponent<Interactor>(playerGO);
                Debug.Log("<color=cyan>[M1]</color> Player'a <b>Interactor</b> eklendi (E ile etkileşim).");
            }

            // 2) Delil kartlarını oluştur (dokümandaki 3 ana/delil)
            var kilit = EnsureEvidence("Evidence_KilitEtiketi", "kilit_etiketi", "Kilit Etiketi",
                "Kurtarma koridorunun eşiğindeki taşların arasından çıkan pirinç etiket. Üzerindeki tarih, kazadan üç gün önceki güne damgalı: kilit, faciadan önce değiştirilmiş.",
                2, true);
            var kroki = EnsureEvidence("Evidence_VardiyaKrokisi", "vardiya_krokisi", "Vardiya Krokisi",
                "Havalandırma odasındaki yırtık 1989 krokisi. Tahliye kapısı kapalı çizilmiş, anahtar teslim hanesi boş. Kenar notu: \"Kilit değişti. Anahtar Veli'de.\"",
                2, false);
            var kayit = EnsureEvidence("Evidence_AsilKayit", "asil_kayit", "Asıl Kayıt Paketi",
                "Kör ölçüm odasının duvarına örülmüş bölmeden çıkan paket: Hasan'ın imzalı gaz çizelgeleri ve kilit değişim emri. En arkadaki kâğıtta şu yazıyor: \"Oğlumun bunları görmesini istemedim.\"",
                4, true);

            // 3) Test için sahneye bir örnek delil yerleştir (oyuncunun başlangıcına yakın)
            if (Object.FindFirstObjectByType<EvidencePickup>() == null && kilit != null)
            {
                var pickupGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pickupGO.name = "Delil_KilitEtiketi";
                pickupGO.transform.localScale = new Vector3(0.16f, 0.02f, 0.11f);
                pickupGO.transform.position = fpc.transform.position + fpc.transform.forward * 1.8f + Vector3.up * 1.1f;
                Undo.RegisterCreatedObjectUndo(pickupGO, "Create Sample Evidence");

                var pickup = pickupGO.AddComponent<EvidencePickup>();
                var so = new SerializedObject(pickup);
                so.FindProperty("_evidence").objectReferenceValue = kilit;
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("<color=cyan>[M1]</color> Test delili 'Delil_KilitEtiketi' sahneye yerleştirildi.");
            }

            // 4) Sahneyi kaydet
            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("<color=green>[M1]</color> Kurulum tamamlandı.");
        }

        private static EvidenceItem EnsureEvidence(string fileName, string id, string title, string desc, int chapter, bool key)
        {
            string dir = "Assets/_Project/ScriptableObjects/Notes";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string path = $"{dir}/{fileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<EvidenceItem>(path) != null)
            {
                return AssetDatabase.LoadAssetAtPath<EvidenceItem>(path);
            }

            var item = ScriptableObject.CreateInstance<EvidenceItem>();
            item.id = id;
            item.title = title;
            item.description = desc;
            item.chapter = chapter;
            item.isKeyEvidence = key;

            AssetDatabase.CreateAsset(item, path);
            Debug.Log($"<color=cyan>[M1]</color> Delil kartı oluşturuldu: {title}");
            return item;
        }
    }
}
