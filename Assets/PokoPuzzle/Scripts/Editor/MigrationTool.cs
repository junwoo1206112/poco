#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PokoPuzzle.Editor
{
    public static class MigrationTool
    {
        [MenuItem("Tools/Poko Puzzle/Migrate Editor Scripts")]
        public static void MigrateEditorScripts()
        {
            var sourceDir = "Assets/PokoPuzzle/Editor";
            var targetDir = "Assets/PokoPuzzle/Scripts/Editor";

            MoveFile(sourceDir, targetDir, "ExcelDataGenerator.cs");
            MoveFile(sourceDir, targetDir, "ExcelDataImporter.cs");

            AssetDatabase.Refresh();
            Debug.Log("[MigrationTool] Editor scripts migrated successfully.");
        }

        private static void MoveFile(string fromDir, string toDir, string fileName)
        {
            var source = $"{fromDir}/{fileName}";
            var dest = $"{toDir}/{fileName}";

            if (!System.IO.File.Exists(System.IO.Path.GetFullPath(source)))
            {
                Debug.Log($"[MigrationTool] Skipping {fileName} — not found in {fromDir}");
                return;
            }

            var message = AssetDatabase.MoveAsset(source, dest);
            if (!string.IsNullOrEmpty(message))
            {
                Debug.LogError($"[MigrationTool] Failed to move {fileName}: {message}");
            }
            else
            {
                Debug.Log($"[MigrationTool] Moved {fileName} to {toDir}");
            }
        }
    }
}
#endif
