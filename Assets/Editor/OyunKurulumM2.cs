using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Project.Interaction;
using Project.Player;

namespace MyEditorTools
{
    /// <summary>
    /// M2 Kurulumu: oyuncunun başlangıç noktasına yakın 3 yağ kutusu yerleştirir.
    /// Fener ve yağ göstergesi oyun başında kendiliğinden kurulur; burada sadece
    /// sahneye yağ kutularını koymak gerekir.
    /// Menü: Tools/Oyun/M2 Kurulum
    /// </summary>
    [InitializeOnLoad]
    public static class OyunKurulumM2
    {
        private const string DoneKey = "Oyun_M2_Kurulum_v1";

        static OyunKurulumM2()
        {
            EditorApplication.delayCall += () =>
            {
                if (SessionState.GetBool(DoneKey, false)) return;
                SessionState.SetBool(DoneKey, true);
                RunInternal(auto: true);
            };
        }

        [MenuItem("Tools/Oyun/M2 Kurulum (Lamba Sistemi)")]
        public static void RunFromMenu() => RunInternal(auto: false);

        private static void RunInternal(bool auto)
        {
            if (Application.isPlaying) return;

            var fpc = Object.FindFirstObjectByType<FirstPersonController>();
            if (fpc == null)
            {
                if (!auto) Debug.LogWarning("[M2] Sahnede Player (FirstPersonController) bulunamadı.");
                return;
            }

            if (Object.FindFirstObjectByType<OilCan>() != null)
            {
                if (!auto) Debug.Log("[M2] Sahnede zaten yağ kutusu var, kurulum atlandı.");
                return;
            }

            // Yağ kutuları: biri oyuncunun hemen önünde (test için), ikisi çevrede
            var origin = fpc.transform.position;
            var forward = fpc.transform.forward;

            CreateOilCan("YagKutusu_1", origin + forward * 2.5f, "oyuncunun önünde");
            CreateOilCan("YagKutusu_2", origin + forward * 7f + fpc.transform.right * 5f, "sağ koridorda");
            CreateOilCan("YagKutusu_3", origin + forward * 4f - fpc.transform.right * 8f, "sol koridorda");

            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("<color=green>[M2]</color> Kurulum tamamlandı: 3 yağ kutusu yerleştirildi.");
        }

        private static void CreateOilCan(string name, Vector3 position, string note)
        {
            var can = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            can.name = name;
            can.transform.localScale = new Vector3(0.22f, 0.35f, 0.22f);

            // Zemine otur
            if (Physics.Raycast(position + Vector3.up * 3f, Vector3.down, out RaycastHit hit, 10f))
                position = hit.point + Vector3.up * 0.36f;
            can.transform.position = position;

            // Koni gövdesi (Lambert vurgusu) — parıltı OilCan.Awake içinde eklenir
            var renderer = can.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", new Color(0.85f, 0.55f, 0.25f)); // yağ kutusu turuncusu
            mat.name = "YagKutusuMat";
            renderer.sharedMaterial = mat;

            can.AddComponent<OilCan>();
            Undo.RegisterCreatedObjectUndo(can, "Create Oil Can");
            Debug.Log($"<color=cyan>[M2]</color> Yağ kutusu '{name}' yerleştirildi ({note}).");
        }
    }
}
