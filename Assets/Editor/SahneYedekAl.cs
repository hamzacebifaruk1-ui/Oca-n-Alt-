using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyEditorTools
{
    /// <summary>
    /// Sahne yedeği: ana sahneyi tarih/saat damgalı kopyayla Assets/Backups klasörüne koyar.
    /// Korkuya karşı sigorta: sahneyi bozacak bir işe girişmeden ÖNCE bunu çalıştır.
    /// Menü: Tools/Oyun/Sahne Yedeği Al
    /// </summary>
    public static class SahneYedekAl
    {
        [MenuItem("Tools/Oyun/Sahne Yedeği Al")]
        public static void BackupScene()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[Yedek] Play modundayken çalışmaz. Önce oyunu durdur.");
                return;
            }

            var scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded || string.IsNullOrEmpty(scene.path))
            {
                Debug.LogWarning("[Yedek] Kayıtlı bir sahne açık değil.");
                return;
            }

            // Sahneden önce kaydet ki yedek güncel olsun
            EditorSceneManager.SaveScene(scene);

            var dir = "Assets/Backups";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var stamp = System.DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            var fileName = Path.GetFileNameWithoutExtension(scene.path);
            var dest = $"{dir}/{fileName}_yedek_{stamp}.unity";

            try
            {
                File.Copy(scene.path, dest, overwrite: true);
                AssetDatabase.ImportAsset(dest);
                AssetDatabase.Refresh();
                Debug.Log($"<color=green>[Yedek]</color> Sahne kopyalandı: <b>{dest}</b>");
            }
            catch (System.Exception e)
            {
                // Yedek başarısız olsa da iş akışı devam etsin (en kötü ihtimalle git yedeği var)
                Debug.LogWarning($"<color=yellow>[Yedek]</color> Kopyalanamadı, devam ediliyor: {e.Message}");
            }
        }
    }
}
