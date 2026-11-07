using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyEditorTools
{
    [InitializeOnLoad]
    public static class MadenExpander
    {
        private const string DoneKey = "Maden_Expanded_Gallery_Built_v1";

        static MadenExpander()
        {
            EditorApplication.delayCall += AutoExecute;
        }

        private static void AutoExecute()
        {
            if (SessionState.GetBool(DoneKey, false)) return;
            SessionState.SetBool(DoneKey, true);
            BuildCompleteMineGallery();
        }

        [MenuItem("Tools/Mine/Build Full Realistic Mine Gallery")]
        public static void BuildCompleteMineGallery()
        {
            Debug.Log("<color=yellow>[Maden Builder]</color> Gerçekçi maden galerisi inşa ediliyor...");

            // 1. Şablon nesnelerini sahneden bul
            GameObject templateTunnel = FindObjectByNameContains("ufekaeedw");
            GameObject templateRail = FindObjectByNameContains("wdpncgrdw");
            GameObject templateArch = FindObjectByNameContains("vmhdagb");
            GameObject templateLight = GameObject.Find("led");
            GameObject templateCart = FindObjectByNameContains("ricaS");
            GameObject templateRock = FindObjectByNameContains("ujzhahdfa") ?? FindObjectByNameContains("ufekadxdw");

            if (templateTunnel == null || templateRail == null || templateArch == null)
            {
                Debug.LogError("[Maden Builder] Gerekli maden şablon parçaları (ufekaeedw, wdpncgrdw veya vmhdagb) sahnede bulunamadı!");
                return;
            }

            // 2. Hiyerarşiyi düzenli tutmak için ana kapsayıcı nesne oluştur
            string rootName = "--- MADEN GALERISI ---";
            GameObject mineRoot = GameObject.Find(rootName);
            if (mineRoot != null)
            {
                Undo.DestroyObjectImmediate(mineRoot);
            }
            mineRoot = new GameObject(rootName);
            Undo.RegisterCreatedObjectUndo(mineRoot, "Create Mine Gallery Root");

            // Boyut ve Adım Değerleri
            float railStepX = 8.18f;
            float tunnelStepX = 4.09f;
            float archStepX = 10.65f;

            // --- İLERİ DOĞRU ANA GALERİ UZATMASI (X = 15'ten X = 50'ye kadar) ---
            float startRailX = 19.53f;
            int newRailCount = 4; // 19.53, 27.71, 35.89, 44.07
            for (int i = 0; i < newRailCount; i++)
            {
                float posX = startRailX + (i * railStepX);
                var rail = Object.Instantiate(templateRail, new Vector3(posX, -1.71f, 7.38f), Quaternion.identity, mineRoot.transform);
                rail.name = $"Ray_Uzatma_{i + 1}";
                Undo.RegisterCreatedObjectUndo(rail, "Add Rail");
            }

            float startTunnelX = 15.77f;
            int newTunnelCount = 9; // 15.77'den ~48.5'e kadar
            for (int i = 0; i < newTunnelCount; i++)
            {
                float posX = startTunnelX + (i * tunnelStepX);
                var tunnel = Object.Instantiate(templateTunnel, new Vector3(posX, -1.64f, 7.39f), Quaternion.identity, mineRoot.transform);
                tunnel.name = $"Tunel_Kaya_Uzatma_{i + 1}";
                Undo.RegisterCreatedObjectUndo(tunnel, "Add Tunnel Segment");
            }

            float startArchX = 18.75f;
            int newArchCount = 3; // 18.75, 29.40, 40.05
            for (int i = 0; i < newArchCount; i++)
            {
                float posX = startArchX + (i * archStepX);
                var arch = Object.Instantiate(templateArch, new Vector3(posX, -1.88f, 7.25f), Quaternion.identity, mineRoot.transform);
                arch.name = $"Ahsap_Tahkimat_{i + 1}";
                Undo.RegisterCreatedObjectUndo(arch, "Add Timber Arch");
            }

            // Düzenli aralıklarla Sıcak Tavan Fenerleri
            if (templateLight != null)
            {
                float[] lightXPositions = new float[] { 17.2f, 25.5f, 33.8f, 42.1f };
                for (int i = 0; i < lightXPositions.Length; i++)
                {
                    var lightGO = Object.Instantiate(templateLight, new Vector3(lightXPositions[i], 2.52f, 7.19f), Quaternion.identity, mineRoot.transform);
                    lightGO.name = $"Maden_Feneri_Uzatma_{i + 1}";
                    var l = lightGO.GetComponent<Light>();
                    if (l != null)
                    {
                        l.color = new Color(1.0f, 0.72f, 0.38f);
                        l.intensity = 18.0f;
                        l.range = 10.0f;
                        l.shadows = LightShadows.Soft;
                    }
                    Undo.RegisterCreatedObjectUndo(lightGO, "Add Lantern");
                }
            }

            // --- ARKA KISIM GİRİŞ KAPATMASI (Oyuncunun arkasındaki boşluğa düşmeyi önleme) ---
            for (int i = 1; i <= 3; i++)
            {
                float posX = -13.70f - (i * tunnelStepX);
                var rearTunnel = Object.Instantiate(templateTunnel, new Vector3(posX, -1.64f, 7.39f), Quaternion.identity, mineRoot.transform);
                rearTunnel.name = $"Tunel_Arka_Giris_{i}";
                Undo.RegisterCreatedObjectUndo(rearTunnel, "Add Rear Tunnel");
            }

            // Tünelin arkasını kapatan ahşap takoz / kaya duvarı
            var backArch = Object.Instantiate(templateArch, new Vector3(-24.5f, -1.88f, 7.25f), Quaternion.identity, mineRoot.transform);
            backArch.name = "Arka_Giris_Tahkimat_Son";
            Undo.RegisterCreatedObjectUndo(backArch, "Add Back Wall Arch");

            // --- TÜNEL SONU / KAZI ALANI (Ayna / Dead-end excavation face) ---
            float endTunnelX = 48.2f;

            // Tünel ucuna maden vagonu yerleştir
            if (templateCart != null)
            {
                var endCart = Object.Instantiate(templateCart, new Vector3(44.0f, -0.85f, 7.38f), Quaternion.Euler(0, 90, 0), mineRoot.transform);
                endCart.name = "Maden_Vagonu_KazıAlani";
                Undo.RegisterCreatedObjectUndo(endCart, "Add End Cart");
            }

            // Tünel ucunu kapatan kaya kütleleri
            if (templateRock != null)
            {
                var endRock = Object.Instantiate(templateRock, new Vector3(endTunnelX + 2.0f, -1.0f, 7.39f), Quaternion.Euler(0, 180, 0), mineRoot.transform);
                endRock.name = "Maden_Kaya_Duvari_Son";
                Undo.RegisterCreatedObjectUndo(endRock, "Add End Rock Wall");
            }

            // Tünelin sonundaki gizemli mavi sis ışığını tünel ucuna taşı
            var blueLightGO = GameObject.Find("Maden_Derinlik_MaviIsik");
            if (blueLightGO != null)
            {
                blueLightGO.transform.position = new Vector3(endTunnelX - 2.0f, 1.8f, 7.2f);
                var bl = blueLightGO.GetComponent<Light>();
                if (bl != null)
                {
                    bl.range = 22.0f;
                    bl.intensity = 16.0f;
                }
            }

            // --- TÜM SAHNEDEKİ NESNELERE MESH COLLIDER EKLEME ---
            int collidersAdded = 0;
            var allRenderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            foreach (var mr in allRenderers)
            {
                var go = mr.gameObject;

                // Oyuncu kapsülünü ve ışıkları atla
                if (go.name == "Player" || go.name == "Oyuncu" || go.GetComponent<CharacterController>() != null)
                    continue;

                var mf = go.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;

                var col = go.GetComponent<Collider>();
                if (col == null)
                {
                    var mc = Undo.AddComponent<MeshCollider>(go);
                    mc.sharedMesh = mf.sharedMesh;
                    mc.convex = false; // Statik tünel duvarları ve zemin için içinden yürünebilir konkav çarpıştırıcı
                    collidersAdded++;
                }
                else if (col is MeshCollider existingMc && existingMc.sharedMesh == null)
                {
                    existingMc.sharedMesh = mf.sharedMesh;
                    existingMc.convex = false;
                }
            }

            // Sahneyi kaydet
            var activeScene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log($"<color=green>[Maden Builder]</color> <b>Maden galerisi başarıyla tamamlandı!</b> Toplam {collidersAdded} adet nesneye MeshCollider eklendi.");
        }

        private static GameObject FindObjectByNameContains(string partialName)
        {
            var allGOs = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var go in allGOs)
            {
                if (go.name.ToLower().Contains(partialName.ToLower()))
                {
                    return go;
                }
            }
            return null;
        }
    }
}
