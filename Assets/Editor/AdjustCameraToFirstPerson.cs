using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Project.Player;

namespace MyEditorTools
{
    [InitializeOnLoad]
    public static class AdjustCameraToFirstPerson
    {
        private const string DoneKey = "FPS_Camera_And_Controller_Setup_v2";

        static AdjustCameraToFirstPerson()
        {
            EditorApplication.delayCall += AutoExecute;
        }

        private static void AutoExecute()
        {
            if (SessionState.GetBool(DoneKey, false)) return;
            SessionState.SetBool(DoneKey, true);
            SetupFirstPersonCameraAndController();
        }

        [MenuItem("Tools/FPS/Setup Player CharacterController & Movement")]
        public static void SetupFirstPersonCameraAndController()
        {
            // 1. Find Capsule object (Player)
            GameObject player = null;
            var capsuleColliders = Object.FindObjectsByType<CapsuleCollider>(FindObjectsSortMode.None);
            if (capsuleColliders.Length > 0)
            {
                player = capsuleColliders[0].gameObject;
            }
            else
            {
                var allGOs = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                foreach (var go in allGOs)
                {
                    string lower = go.name.ToLower();
                    if (lower.Contains("player") || lower.Contains("capsule") || lower.Contains("oyuncu"))
                    {
                        player = go;
                        break;
                    }
                }
            }

            if (player == null)
            {
                Debug.LogError("[FPS Setup] Sahnede Player / Kapsül bulunamadı!");
                return;
            }

            // 2. Find Main Camera
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                mainCam = player.GetComponentInChildren<Camera>();
            }
            if (mainCam == null)
            {
                var camGO = GameObject.Find("Main Camera");
                if (camGO != null) mainCam = camGO.GetComponent<Camera>();
            }

            if (mainCam == null)
            {
                Debug.LogError("[FPS Setup] Main Camera bulunamadı!");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(player, "Setup FPS Player");

            // 3. Parent camera to player if not already
            if (mainCam.transform.parent != player.transform)
            {
                Undo.SetTransformParent(mainCam.transform, player.transform, "Parent Camera to Player");
            }

            // Eye height
            float eyeY = 0.70f;
            mainCam.transform.localPosition = new Vector3(0f, eyeY, 0.1f);
            mainCam.transform.localRotation = Quaternion.identity;
            mainCam.transform.localScale = Vector3.one;

            // 4. CharacterController setup
            var cc = player.GetComponent<CharacterController>();
            if (cc == null)
            {
                cc = Undo.AddComponent<CharacterController>(player);
            }

            cc.height = 2.0f;
            cc.radius = 0.5f;
            cc.center = Vector3.zero;
            cc.stepOffset = 0.3f;
            cc.slopeLimit = 45f;
            cc.minMoveDistance = 0f;

            // 5. Disable conflicting CapsuleCollider if present
            var capCol = player.GetComponent<CapsuleCollider>();
            if (capCol != null && capCol.enabled)
            {
                capCol.enabled = false;
            }

            // 6. Add FirstPersonController
            var fpsController = player.GetComponent<FirstPersonController>();
            if (fpsController == null)
            {
                fpsController = Undo.AddComponent<FirstPersonController>(player);
            }

            // 7. Save scene
            var activeScene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log($"<color=green>[FPS Setup]</color> <b>'{player.name}'</b> nesnesine CharacterController ve FirstPersonController başarıyla eklendi! Kamera göz hizasında bağlandı.");
        }
    }
}
