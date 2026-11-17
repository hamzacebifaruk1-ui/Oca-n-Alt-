using UnityEditor;
using UnityEngine;
using Project.Player;

namespace MyEditorTools
{
    /// <summary>
    /// Sahne görünümü (Scene) kamerası bazen tuhaf açılara kayar; kullanıcı tüneli
    /// bulamaz. Bu araç kamerayı doğrudan oyuncunun yanına ışınlar.
    /// Menü: Tools/Oyun/Kamerayı Oyuncuya Getir
    /// Menü: Tools/Oyun/Kamerayı Oyuncuya Getir (Ctrl+P'yi Play'e bıraktık,
    /// kısayol vermiyoruz — isteyen Unity Shortcut Manager'dan ekleyebilir.)
    /// </summary>
    public static class SahneGorunumDuzelt
    {
        [MenuItem("Tools/Oyun/Kamerayı Oyuncuya Getir")]
        public static void BringCameraToPlayer()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[Kamera] Play modundayken çalışmaz. Önce oyunu durdur.");
                return;
            }

            var fpc = Object.FindFirstObjectByType<FirstPersonController>();
            if (fpc == null)
            {
                Debug.LogWarning("[Kamera] Sahnede Player bulunamadı.");
                return;
            }

            var view = SceneView.lastActiveSceneView;
            if (view == null)
            {
                // Hiç Scene penceresi aktif değilse önce sahneye odaklan
                EditorApplication.ExecuteMenuItem("Window/Panels/Scene");
                view = SceneView.lastActiveSceneView;
            }
            if (view == null)
            {
                Debug.LogWarning("[Kamera] Scene penceresi bulunamadı.");
                return;
            }

            var pos = fpc.transform.position;
            // Oyuncunun 10m arkasından ve 6m yukarıdan bak: tünel girişi kadrajda
            view.LookAt(pos + Vector3.up * 1.5f, Quaternion.Euler(25f, 180f, 0f), 12f);
            view.pivot = pos + Vector3.up * 1.5f;
            view.size = 12f;
            view.Repaint();

            Selection.activeGameObject = fpc.gameObject;
            Debug.Log("<color=green>[Kamera]</color> Scene görünümü oyuncuya getirildi. (Menü: Tools/Oyun/Kamerayı Oyuncuya Getir)");
        }
    }
}
