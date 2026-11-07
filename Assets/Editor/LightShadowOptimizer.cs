using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyEditorTools
{
    [InitializeOnLoad]
    public static class LightShadowOptimizer
    {
        private const string DoneKey = "Light_Shadow_Optimized_v1";

        static LightShadowOptimizer()
        {
            EditorApplication.delayCall += OptimizeShadows;
        }

        [MenuItem("Tools/Lighting/Optimize Mine Light Shadows")]
        public static void OptimizeShadows()
        {
            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            int shadowCount = 0;
            foreach (var l in lights)
            {
                if (l.type == LightType.Point)
                {
                    // Tüneldeki ana fenerler (en fazla 4 tanesi) gölge düşürsün, geri kalanı atlası doldurmasın
                    if (l.name == "led" || l.name == "led (1)" || l.name == "led (2)" || l.name == "Maden_Derinlik_MaviIsik")
                    {
                        l.shadows = LightShadows.Soft;
                        shadowCount++;
                    }
                    else
                    {
                        l.shadows = LightShadows.None; // Performans ve sıfır atlas uyarısı için
                    }
                }
            }

            var activeScene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log($"<color=green>[Shadow Optimizer]</color> Işık gölgeleri optimize edildi ({shadowCount} adet gölge düşüren ana ışık). Konsol uyarıları giderildi.");
        }
    }
}
