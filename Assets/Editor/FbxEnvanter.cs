using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// FBX ENVANTERİ — "New Folder" içindeki tüm FBX modelleri tarar,
/// her birinin içindeki mesh adlarını, üçgen sayılarını ve malzeme adlarını
/// bir rapor dosyasına yazar. Koridor genişletme için hangi modelin ne olduğunu
/// böyle anlarız.
/// Rapor: Assets/fbx_envanter.txt (Not Defteri ile açılabilir)
/// </summary>
public static class FbxEnvanter
{
    [MenuItem("Tools/Oyun/FBX Envanterini Çıkar")]
    public static void EnvanterCikar()
    {
        var klasor = "Assets/_Project/Prefabs/New Folder";
        if (!Directory.Exists(klasor))
        {
            EditorUtility.DisplayDialog("FBX Envanteri", "Klasör bulunamadı:\n" + klasor, "Tamam");
            return;
        }

        var fbxYollari = Directory.GetFiles(klasor, "*.fbx", SearchOption.AllDirectories);
        var rapor = new StringBuilder();
        rapor.AppendLine("FBX ENVANTERİ — " + System.DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
        rapor.AppendLine("Klasör: " + klasor);
        rapor.AppendLine("Toplam FBX: " + fbxYollari.Length);
        rapor.AppendLine(new string('=', 80));

        int sayi = 0;
        foreach (var yol in fbxYollari)
        {
            sayi++;
            EditorUtility.DisplayProgressBar("FBX Envanteri", Path.GetFileName(yol), (float)sayi / fbxYollari.Length);

            var importer = AssetImporter.GetAtPath(yol) as ModelImporter;
            rapor.AppendLine();
            rapor.AppendLine("[" + sayi + "] " + Path.GetFileName(yol));

            if (importer == null)
            {
                rapor.AppendLine("    (okunamadı)");
                continue;
            }

            // FBX'i geçici olarak sahneye açmadan meshleri oku
            var tumNesneler = AssetDatabase.LoadAllAssetsAtPath(yol);
            var meshler = new List<Mesh>();
            var malzemeler = new List<Material>();
            foreach (var nesne in tumNesneler)
            {
                if (nesne is Mesh m && !meshler.Contains(m)) meshler.Add(m);
                if (nesne is Material mat && !malzemeler.Contains(mat)) malzemeler.Add(mat);
            }

            // Nesne adı (FBX kök adı)
            var kokAd = importer.name;
            rapor.AppendLine("    Kök ad: " + kokAd);

            if (malzemeler.Count > 0)
            {
                rapor.AppendLine("    Malzemeler: " + string.Join(", ", malzemeler.ConvertAll(x => x.name).ToArray()));
            }

            foreach (var mesh in meshler)
            {
                var ucgen = mesh.triangles.Length / 3;
                var boyut = mesh.bounds.size;
                rapor.AppendLine(string.Format(
                    "    Mesh: {0}  |  üçgen: {1}  |  boyut: {2:N1} x {3:N1} x {4:N1} m",
                    mesh.name, ucgen, boyut.x, boyut.y, boyut.z));
            }

            if (meshler.Count == 0) rapor.AppendLine("    (mesh yok — muhtemelen boş/animasyon dosyası)");
        }

        EditorUtility.ClearProgressBar();

        var raporYolu = "Assets/fbx_envanter.txt";
        File.WriteAllText(raporYolu, rapor.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "FBX Envanteri",
            "Rapor hazır!\n\n" + raporYolu + "\n\nÇift tıklayıp Not Defteri'yle açabilirsin.",
            "Tamam");

        // Raporu Project penceresinde seç
        var raporDosyasi = AssetDatabase.LoadAssetAtPath<TextAsset>(raporYolu);
        if (raporDosyasi != null) Selection.activeObject = raporDosyasi;
    }
}
