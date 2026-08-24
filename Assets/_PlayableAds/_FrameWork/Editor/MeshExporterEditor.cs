using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(MeshFilter))]
public class MeshExporterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MeshFilter meshFilter = (MeshFilter)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Mesh Exporter", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Export Mesh to Assets/ExportMesh"))
        {
            ExportMesh(meshFilter);
        }
    }

    private void ExportMesh(MeshFilter meshFilter)
    {
        if (meshFilter == null)
        {
            Debug.LogError("MeshFilter is null!");
            return;
        }

        Mesh mesh = meshFilter.sharedMesh;
        if (mesh == null)
        {
            Debug.LogError("No mesh found on MeshFilter!");
            return;
        }

        // Tạo thư mục nếu chưa tồn tại
        string folderPath = "Assets/ExportMesh";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "ExportMesh");
        }

        // Tạo tên file unique
        string fileName = meshFilter.gameObject.name + "_Mesh.asset";
        string assetPath = Path.Combine(folderPath, fileName);
        
        // Nếu file đã tồn tại, thêm số vào tên
        int counter = 1;
        while (File.Exists(assetPath))
        {
            fileName = meshFilter.gameObject.name + "_Mesh_" + counter + ".asset";
            assetPath = Path.Combine(folderPath, fileName);
            counter++;
        }

        // Tạo mesh mới để lưu (duplicate)
        Mesh meshToSave = Object.Instantiate(mesh);
        meshToSave.name = meshFilter.gameObject.name + "_Mesh";

        // Lưu mesh vào Assets
        AssetDatabase.CreateAsset(meshToSave, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Ping file vừa tạo trong Project window
        EditorGUIUtility.PingObject(meshToSave);

        Debug.Log($"Mesh exported successfully to: {assetPath}");
    }
}
