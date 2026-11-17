using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyEditorTools
{
    /// <summary>
    /// SAHNE OTOMATİK YENİLEYİCİ: dışarıdan (git restore / benim düzeltmelerim)
    /// değiştirilen sahneyi Unity'ye elle kapatmadan yeniden yükletir.
    /// Çalışma mantığı: ben "Library/sahne_yenile_istegi.flag" dosyasını bırakırım;
    /// Unity derleme yaptığında bu script bayrağı görür ve sahneyi diskten yeniden açar.
    /// Bellekteki (silinecek) hâli önce Temp klasörüne yedekler — hiçbir şey kaybolmaz.
    /// </summary>
    [InitializeOnLoad]
    public static class SahneOtomatikYenile
    {
        private const string FlagPath = "Library/sahne_yenile_istegi.flag";
        private const string ScenePath = "Assets/_Project/Scenes/Oyun.unity";

        static SahneOtomatikYenile()
        {
            EditorApplication.delayCall += RunIfFlagged;
        }

        private static void RunIfFlagged()
        {
            if (!System.IO.File.Exists(FlagPath)) return;
            try { System.IO.File.Delete(FlagPath); } catch { }

            var scene = SceneManager.GetActiveScene();

            // 1) Bellekteki şu anki hâli yedekle (eski/eksik bile olsa silmeyelim)
            if (scene.isLoaded && !string.IsNullOrEmpty(scene.path))
            {
                try
                {
                    var yedekAdi = $"Temp/hafiza_yedegi_{System.DateTime.Now:HHmmss}.unity";
                    EditorSceneManager.SaveScene(scene, yedekAdi);
                    Debug.Log($"<color=yellow>[Yenile]</color> Bellekteki eski hâl yedeklendi: {yedekAdi}");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[Yenile] Hafıza yedeği alınamadı: {e.Message}");
                }
            }

            // 2) Sahneyi diskten yeniden yükle
            EditorSceneManager.OpenScene(ScenePath);
            Debug.Log($"<color=green>[Yenile]</color> Sahne diskten yeniden yüklendi: {ScenePath}");
        }
    }
}
