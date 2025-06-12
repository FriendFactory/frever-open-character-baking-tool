using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

namespace UMA
{
	public class OverlayLibrary : OverlayLibraryBase
    {
        [SerializeField] protected OverlayDataAsset[] overlayElementList = Array.Empty<OverlayDataAsset>();
		[NonSerialized]
		private Dictionary<int, OverlayDataAsset> overlayDictionary;

		public int scaleAdjust = 1;
		public bool readWrite = false;
		public bool compress = false;

		void Awake()
		{
			ValidateDictionary();
		}

	#pragma warning disable 618
		override public void UpdateDictionary()
		{
			ValidateDictionary();
			overlayDictionary.Clear();
			for (int i = 0; i < overlayElementList.Length; i++)
			{
				if (overlayElementList[i])
				{
					var hash = OverlayNameToHash(overlayElementList[i].overlayName);
					if (!overlayDictionary.ContainsKey(hash))
					{
						overlayDictionary.Add(hash, overlayElementList[i]);
					}
				}
			}
		}

        public override bool HasOverlay(string Name)
		{
			ValidateDictionary();
			var hash = UMAUtils.StringToHash(Name);
			return overlayDictionary.ContainsKey(hash);
		}

		public override bool HasOverlay(int NameHash)
		{
			ValidateDictionary();
			return overlayDictionary.ContainsKey(NameHash);
		}

		public override void AddOverlayAsset(OverlayDataAsset overlay)
		{
			ValidateDictionary();
			var hash = UMAUtils.StringToHash(overlay.overlayName);
			if (overlayDictionary.ContainsKey(hash))
			{
				for (int i = 0; i < overlayElementList.Length; i++)
				{
					if (overlayElementList[i].overlayName == overlay.overlayName)
					{
						overlayElementList[i] = overlay;
						break;
					}
				}
			}
			else
			{
				var list = new OverlayDataAsset[overlayElementList.Length + 1];
				for (int i = 0; i < overlayElementList.Length; i++)
				{
					list[i] = overlayElementList[i];
				}
				list[list.Length - 1] = overlay;
				overlayElementList = list;
			}
			overlayDictionary[hash] = overlay;
		}
	#pragma warning restore 618

		public override void ValidateDictionary()
		{
			if (overlayDictionary == null)
			{
				overlayDictionary = new Dictionary<int, OverlayDataAsset>();
				UpdateDictionary();
			}
		}

		public override OverlayData InstantiateOverlay(string name)
		{
			var res = Internal_InstantiateOverlay(UMAUtils.StringToHash(name));
			if (res == null)
			{
				throw new UMAResourceNotFoundException("OverlayLibrary: Unable to find: " + name);
			}
			return res;
		}

		public override OverlayData InstantiateOverlay(int nameHash)
		{
			var res = Internal_InstantiateOverlay(nameHash);
			if (res == null)
			{
				throw new UMAResourceNotFoundException("OverlayLibrary: Unable to find hash: " + nameHash);
			}
			return res;
		}

		public override OverlayData InstantiateOverlay(string name, Color color)
		{
			var res = Internal_InstantiateOverlay(UMAUtils.StringToHash(name));
			if (res == null)
			{
				throw new UMAResourceNotFoundException("OverlayLibrary: Unable to find: " + name);
			}
			res.colorData.color = color;
			return res;
		}

		public override OverlayData InstantiateOverlay(int nameHash, Color color)
		{
			var res = Internal_InstantiateOverlay(nameHash);
			if (res == null)
			{
				throw new UMAResourceNotFoundException("OverlayLibrary: Unable to find hash: " + nameHash);
			}
			res.colorData.color = color;
			return res;
		}

		private OverlayData Internal_InstantiateOverlay(int nameHash)
		{
			ValidateDictionary();
            if (!overlayDictionary.TryGetValue(nameHash, out var source))
			{
				return null;
			}

            return new OverlayData(source);
        }

		public override OverlayDataAsset[] GetAllOverlayAssets()
		{
	#pragma warning disable 618
			return overlayElementList;
	#pragma warning restore 618
		}

		public override void ReleaseLibrary()
        {
            foreach (var overlay in overlayElementList)
            {
                overlay.ReleaseResources();
                Resources.UnloadAsset(overlay);
            }

            overlayDictionary.Clear();
            overlayElementList = Array.Empty<OverlayDataAsset>();
            Resources.UnloadUnusedAssets();
        }

        public override void ReleaseLibraryPartially(PartialLibraryUnloadingArgs args)
        {
            var assetsToKeep = args.AssetsToKeep;
            var overlaysToRemove = overlayElementList.Where(x => !assetsToKeep.ContainsIgnoreCamelCase(x.overlayName)).ToArray();
            overlayElementList = overlayElementList.Where(x => !overlaysToRemove.Contains(x)).ToArray();

            foreach (var overlay in overlaysToRemove)
            {
                var hashName = OverlayNameToHash(overlay.name);
                overlayDictionary.Remove(hashName);
                overlay.ReleaseResources(args.KeepGlobalUmaAssets? args.GlobalUmaAssets : null);
                Resources.UnloadAsset(overlay);
            }
            
            UpdateDictionary();
        }

        private int OverlayNameToHash(string overlayName)
        {
            return UMAUtils.StringToHash(overlayName);
        }
    }
}
