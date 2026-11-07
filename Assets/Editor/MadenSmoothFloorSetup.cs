using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyEditorTools
{
    [InitializeOnLoad]
    public static class MadenSmoothFloorSetup
    {
        private const string DoneKey = "Maden_Smooth_Floor_Added_v1";

        static MadenSmoothFloorSetup()
        {
            EditorApplication.delayCall += AutoExecute;
        }

        private static void AutoExecute()
        {
            if (SessionState.GetBool(DoneKey, false)) return;
            SessionState.SetBool(DoneKey, true);
            AddSmoothFloor();
        }

        [MenuItem("Tools/Mine/Add Smooth Walking Surface")]
        public static void AddSmoothFloor()
        {
            string floorName = "Maden_Akici_Yurume_Zemini";
            var existing = GameObject.Find(floorName);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }

            var floorGO = new GameObject(floorName);
            Undo.RegisterCreatedObjectUndo(floorGO, "Create Smooth Mine Floor");

            // Tünelin ana hiyerarşisine bağla
            var root = GameObject.Find("--- MADEN GALERISI ---");
            if (root != null)
            {
                floorGO.transform.SetParent(root.transform, false);
            }

            // Rayların ve traverslerin tam üst hizasında, tüm tüneli kapsayan pürüzsüz görünmez zemin
            floorGO.transform.position = new Vector3(12.5f, -1.66f, 7.38f);

            var boxCol = floorGO.AddComponent<BoxCollider>();
            boxCol.size = new Vector3(88.0f, 0.12f, 3.8f);

            // Oyuncunun CharacterController ayarlarını da güncelle
            var player = GameObject.Find("Player") ?? GameObject.Find("Oyuncu");
            if (player != null)
            {
                var cc = player.GetComponent<CharacterController>();
                if (cc != null)
                {
                    Undo.RecordObject(cc, "Update CharacterController Parameters");
                    cc.stepOffset = 0.2f;
                    cc.skinWidth = 0.05f;
                    cc.minMoveDistance = 0f;
                    cc.center = Vector3.zero;
                    cc.height = 2.0f;
                }
            }

            var activeScene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("<color=green>[Maden Smooth Floor]</color> Ray traversleri arasındaki boşluklara düşmeyi ve durunca çökmeyi önleyen pürüzsüz yürüme zemini başarıyla eklendi!");
        }
    }
}
