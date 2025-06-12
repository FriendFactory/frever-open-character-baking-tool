using System;
using System.Collections.Generic;
using System.Linq;
using Bridge.Models.ClientServer.Assets;
using Bridge.Models.ClientServer.StartPack.Metadata;
using JetBrains.Annotations;
using UMA.AssetBundles;

namespace ExportCharacterTool
{
    [UsedImplicitly]
    public sealed class UmaBundleHelper
    {
        private readonly Dictionary<string, AssetBundleIndex.AssetBundleIndexList> _indexItemCache = new();
        private readonly UnityAssetType[] _unityAssetTypes;

        public UmaBundleHelper(UnityAssetType[] unityAssetTypes)
        {
            _unityAssetTypes = unityAssetTypes;
        }

        public void InitializeIndexer()
        {
            AssetBundleManager.UseDynamicIndexer = true;
            var indexer = UnityEngine.ScriptableObject.CreateInstance<AssetBundleIndex>();
            indexer.bundlesWithVariant = Array.Empty<string>();
            indexer.ownBundleHash = "";
            AssetBundleManager.AssetBundleIndexObject = indexer;
            AssetBundleManager.Initialize();
        }

        public void AddBundlesToIndex(IEnumerable<UmaBundleFullInfo> umaBundles)
        {
            foreach (var item in umaBundles)
            {
                AddBundleToIndex(item);
            }
        }

        public void AddBundleToIndex(UmaBundleFullInfo bundle)
        {
            if (AssetBundleManager.AssetBundleIndexObject.bundlesIndex.Exists(x => x.assetBundleName == bundle.Name)) return;

            AssetBundleIndex.AssetBundleIndexList listItem = null;
            if (_indexItemCache.TryGetValue(bundle.Name, out var value))
            {
                listItem = value;
            }
            else
            {
                listItem = new AssetBundleIndex.AssetBundleIndexList(bundle.Name);
                foreach (var asset in bundle.UmaAssets)
                {
                    if (asset.UmaAssetFiles.Count == 0) continue;
                    var file = asset.UmaAssetFiles.FirstOrDefault();
                    var typeId = file.UnityAssetTypesIds.FirstOrDefault();
                    var type = _unityAssetTypes.First(x => x.Id == typeId);

                    var indexItem = new AssetBundleIndex.AssetBundleIndexItem
                    {
                        filename = file.Name,
                        assetName = asset.Name,
                        assetHash = (int)asset.Hash,
                        assetType = type.Name,
                        assetWardrobeSlot = asset.SlotId != null ? asset.SlotName : string.Empty
                    };

                    listItem.assetBundleAssets.Add(indexItem);
                }
                listItem.allDependencies = bundle.DependentUmaBundles.Select(x => x.Name).ToArray();
                _indexItemCache[bundle.Name] = listItem;
            }
            
            AssetBundleManager.AssetBundleIndexObject.bundlesIndex.Add(listItem);
        }
        
        public void RemoveBundleFromIndex(string bundleName)
        {
            var bundleIndexModel = AssetBundleManager.AssetBundleIndexObject.bundlesIndex.FirstOrDefault(
                    x => x.assetBundleName == bundleName);
            if (bundleIndexModel != null)
            {
                AssetBundleManager.AssetBundleIndexObject.bundlesIndex.Remove(bundleIndexModel);
            }
        }
    }
}