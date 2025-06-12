using System;
using System.IO;
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ExportCharacterTool
{
    public static class BuildAssetBundleManager
    {
        private const string ASSET_BUNDLES_ROOT_FOLDER = "AssetBundles";
        
        public static void BuildAssetBundles(IEnumerable<BundleData> bundleDatas, string platformDirPrefix, params BuildTarget[] buildTargets)
        {
            var buildMap = new List<AssetBundleBuild>();
            foreach (var data in bundleDatas)
            {
                buildMap.Add(new AssetBundleBuild()
                {
                    assetBundleName = data.CharacterId.ToString(),
                    assetNames = new[]
                    {
                        data.Path
                    }
                });
            }
            
            foreach (var buildTarget in buildTargets)
            {
                var outputDir = $"{ASSET_BUNDLES_ROOT_FOLDER}/{buildTarget}/{platformDirPrefix}";
                if (!Directory.Exists(outputDir))
                    Directory.CreateDirectory(outputDir);
                BuildPipeline.BuildAssetBundles(outputDir, buildMap.ToArray(), BuildAssetBundleOptions.None, buildTarget);
            }

            AssetDatabase.Refresh();
        }
        
        public static void ClearAssetBundleFolder()
        {
            if (Directory.Exists(ASSET_BUNDLES_ROOT_FOLDER))
            {
                Directory.Delete(ASSET_BUNDLES_ROOT_FOLDER, true);
            }
        }
    }
}
#endif

[Serializable]
public struct BundleData
{
    public long CharacterId;
    public string CharacterVersion;
    public string Path;
}