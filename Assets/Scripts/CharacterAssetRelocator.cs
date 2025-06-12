#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ExportCharacterTool
{
    /// <summary>
    /// Move prefabs outside Assets Folder to optimize asset database refreshing
    /// </summary>
    public static class CharacterAssetRelocator
    {
        public static void MoveCharactersOutsideTheProject(string environmentName)
        {
            AssetDatabase.Refresh();
            
            const string sourcePath = "Assets/ExportedCharacters";
            string destinationPath = $"ExportedCharacters/{environmentName}";

            if (!Directory.Exists(sourcePath))
            {
                Debug.LogError("Source path does not exist: " + sourcePath);
                return;
            }

            if (!Directory.Exists(destinationPath))
            {
                Directory.CreateDirectory(destinationPath);
            }

            var files = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories);

            foreach (var filePath in files)
            {
                if (filePath.EndsWith(".meta")) continue;

                var relativePath = filePath.Substring(sourcePath.Length + 1);
                var destFilePath = Path.Combine(destinationPath, relativePath);

                var destDirectory = Path.GetDirectoryName(destFilePath);
                if (!Directory.Exists(destDirectory))
                {
                    Directory.CreateDirectory(destDirectory);
                }

                File.Move(filePath, destFilePath);
                File.Move(filePath + ".meta", destFilePath + ".meta");
            }
        }
    }
}
#endif