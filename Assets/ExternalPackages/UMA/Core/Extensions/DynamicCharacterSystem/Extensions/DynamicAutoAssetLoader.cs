using System.Collections;
using UnityEngine;
using UMA.AssetBundles;
using System.Collections.Generic;

namespace UMA.CharacterSystem
{
    public class DynamicAutoAssetLoader : DynamicAssetLoader
    {
        public void LoadBundlesList()
        {
            StartCoroutine(LoadAllBundlesAsync());
        }

        protected IEnumerator LoadAllBundlesAsync()
        {
            if (!isInitialized)
            {
                if (!isInitializing)
                {
                    yield return Initialize();
                }
                else
                {
                    while (isInitialized == false)
                    {
                        yield return null;
                    }
                }
            }

            var bundlesInManifest = AssetBundleManager.AssetBundleIndexObject.GetAllAssetBundles();
            foreach (string bundle in bundlesInManifest)
            {
                yield return LoadAssetBundleAsync(bundle);
            }
        }
    }
}