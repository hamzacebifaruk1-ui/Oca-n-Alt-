using System;
using UnityEngine;
using UnityEditor;

namespace FolderColor.Editor
{
    [InitializeOnLoad]
    public static class FolderColorAutoAssigner
    {
        private const string DoneKey = "FolderColor_Done_Once";

        private struct FolderConfig
        {
            public string Guid;
            public string IconName;
            public Color Color;

            public FolderConfig(string guid, string iconName, Color color)
            {
                Guid = guid;
                IconName = iconName;
                Color = color;
            }
        }

        static FolderColorAutoAssigner()
        {
            EditorApplication.delayCall += () =>
            {
                if (SessionState.GetBool(DoneKey, false)) return;
                SessionState.SetBool(DoneKey, true);
                ExecuteAssignment();
            };
        }

        [MenuItem("Tools/Folder Color/Apply All _Project Folder Colors")]
        public static void ExecuteAssignment()
        {
            var configs = new FolderConfig[]
            {
                new FolderConfig("7c8ac22cff4cbf944a4d5fed2c050865", "MeshFolder.png", new Color(0.5450981f, 0.2705882f, 0.07450981f, 1f)),
                new FolderConfig("244202c6a6878bb408a57b2173e30969", "AbaseFolder.png", new Color(0f, 1f, 1f, 1f)),
                new FolderConfig("c98f95e2bc85569488f157b2eb9255b8", "CodeFolder.png", new Color(0.6470588f, 0.1647059f, 0.1647059f, 1f)),
                new FolderConfig("b5fac564b8dae9c4ab3c4fee90aac01f", "FavoryFolder.png", new Color(0.98f, 0.75f, 0.14f, 1f)),
                new FolderConfig("6519412026f258f43aa98bf9dab80e00", "GroupedPackageFolder.png", new Color(0.63f, 0.38f, 0.95f, 1f)),
                new FolderConfig("d907d1f82ec5f0041b04598873ff126a", "TextureFolder.png", new Color(0.06f, 0.73f, 0.85f, 1f)),
                new FolderConfig("729281495762bc548a01d0794d083faf", "AnimFolder.png", new Color(0.13f, 0.77f, 0.45f, 1f)),
                new FolderConfig("4e5c1565567c4fd488bc915af915efe8", "DataGridFolder.png", new Color(0.23f, 0.51f, 0.96f, 1f)),
                new FolderConfig("f9ec557856e4d9c48a3b3049c8f6ebc6", "PrefabFolder.png", new Color(0.18f, 0.55f, 1.0f, 1f)),
                new FolderConfig("5afe2a663915bd543b82ae7903dd6ef2", "ShaderFolder.png", new Color(0.93f, 0.28f, 0.6f, 1f)),
                new FolderConfig("07d92f043448550489ece63d6205a76c", "SoundFolder.png", new Color(0.98f, 0.45f, 0.09f, 1f)),
                new FolderConfig("a67a359643e82bf4eb0ded3eed32091c", "MusicFolder.png", new Color(1.0f, 0.62f, 0.2f, 1f)),
                new FolderConfig("bbdb2d443432e5241a1b86676c68e508", "SoundFolder.png", new Color(0.95f, 0.45f, 0.15f, 1f)),
                new FolderConfig("d12e83e462b7dfa4ca5c82ce4c1bdaa1", "SoundFolder.png", new Color(0.85f, 0.25f, 0.15f, 1f)),
                new FolderConfig("5ccef0f9adfc31a4695384293c36b5c0", "FileFolder.png", new Color(0.08f, 0.72f, 0.52f, 1f)),
                new FolderConfig("4cb65e7ec63968743bc321e8892141fe", "FileFolder.png", new Color(0.92f, 0.82f, 0.42f, 1f)),
                new FolderConfig("d67b3b869d5471e4f9673afeddb6bbc5", "ParameterFolder.png", new Color(0.55f, 0.6f, 0.68f, 1f))
            };

            foreach (var cfg in configs)
            {
                FolderColorDataControl.FolderColorData.Set(cfg.Guid, cfg.IconName, cfg.Color, false, false, FolderColorDataControl.FolderColorMode.Personal);
                FolderColorDataControl.FolderColorData.Set(cfg.Guid, cfg.IconName, cfg.Color, false, false, FolderColorDataControl.FolderColorMode.Global);
            }

            AssetDatabase.SaveAssets();
            EditorApplication.RepaintProjectWindow();
            Debug.Log("<color=green>[FolderColor]</color> All _Project folders successfully styled with appropriate icons and colors!");
        }
    }
}
