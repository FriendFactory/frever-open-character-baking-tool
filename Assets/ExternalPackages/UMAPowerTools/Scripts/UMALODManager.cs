using UnityEngine;
using System.Collections.Generic;
using UMA.Integrations.PowerTools;
#if UNITY_5_5_OR_NEWER
using UnityEngine.Profiling;
#endif

namespace UMA.PowerTools
{
	public class UMALODManager : MonoBehaviour
	{
		public UMAContext context;
		public UMALODConversionSet[] conversionSets;
		private OptimizedLODManager optimizedManager;
	
		public void Start()
		{
			if (context == null) context = UMAContext.FindInstance();
			Validate();
		}
	
		private void Validate()
		{
			if (optimizedManager == null)
			{
				optimizedManager = new OptimizedLODManager();
				FillInDataSet();
			}
		}
	
		protected void FillInDataSet()
		{
			foreach (var conversionSet in conversionSets)
			{
				optimizedManager.AddConversions(conversionSet.Conversions);
			}
		}
	
		public void ProcessUMARecipe(UMAData.UMARecipe originalRecipe, UMAData.UMARecipe resultingRecipe, int LOD)
		{
			Profiler.BeginSample("LOD Processing");
			var existingConversions = new Dictionary<List<OverlayData>, List<OverlayData>>();
			if (resultingRecipe.slotDataList == null || resultingRecipe.slotDataList.Length != originalRecipe.slotDataList.Length)
			{
				resultingRecipe.slotDataList = new SlotData[originalRecipe.slotDataList.Length];
			}
	
			resultingRecipe.raceData = originalRecipe.raceData;
			for (int i = 0; i < originalRecipe.slotDataList.Length; i++)
			{
				ProcessUMASlot(originalRecipe.slotDataList[i], ref resultingRecipe.slotDataList[i], LOD, existingConversions);
			}
			Profiler.EndSample();
		}
	
		private void ProcessUMASlot(SlotData slotData, ref SlotData processedSlotData, int LOD, Dictionary<List<OverlayData>, List<OverlayData>> existingConversions)
		{
			if (slotData == null)
			{
				processedSlotData = null;
				return;
			}
			var sourceSlot = slotData.asset.nameHash;
			var entry = optimizedManager.FindEntry(UMALODConversionEntry.ConversionGroup.SlotData, sourceSlot);
	
			if (entry != null)
			{
				if (entry.hasChange(LOD))
				{
					entry.InstantiateSlot(LOD, context, ref processedSlotData);
					if (processedSlotData == null) return;
				}
				else if (processedSlotData == null || processedSlotData.asset.nameHash != slotData.asset.nameHash)
				{
					processedSlotData = new SlotData(slotData.asset);
				}
			}
			else if (processedSlotData == null || processedSlotData.asset.nameHash != slotData.asset.nameHash)
			{
				processedSlotData = new SlotData(slotData.asset);
			}
			processedSlotData.SetOverlayList(ProcessUMAOverlayList(slotData.GetOverlayList(), processedSlotData.GetOverlayList(), LOD, existingConversions));
		}
	
		private List<OverlayData> ProcessUMAOverlayList(List<OverlayData> overlays, List<OverlayData> dest, int LOD, Dictionary<List<OverlayData>, List<OverlayData>> existingConversions)
		{
			List<OverlayData> newOverlays;
			if (!existingConversions.TryGetValue(overlays, out newOverlays))
			{
				newOverlays = dest;
				ResizeListAddNull(newOverlays, overlays.Count);
				for (int i = 0; i < newOverlays.Count; i++)
				{
					newOverlays[i] = ProcessUMAOverlay(overlays[i], newOverlays[i], LOD);
				}
				existingConversions.Add(overlays, newOverlays);
			}
			return newOverlays;
		}
	
		private void ResizeListAddNull(List<OverlayData> newOverlays, int size)
		{
			if (newOverlays.Capacity < size) newOverlays.Capacity = size;
			int dif = size - newOverlays.Count;
			while (dif > 0)
			{
				newOverlays.Add(null);
				dif--;
			}
			while (dif < 0)
			{
				newOverlays.RemoveAt(newOverlays.Count - 1);
				dif++;
			}
		}
	
		private OverlayData ProcessUMAOverlay(OverlayData overlayData, OverlayData reference, int LOD)
		{
			if (overlayData == null) return null;
			var sourceOverlay = UMASkeleton.StringToHash(overlayData.asset.overlayName);
			var entry = optimizedManager.FindEntry(UMALODConversionEntry.ConversionGroup.OverlayData, sourceOverlay);
			if (entry != null)
			{
				if (entry.hasChange(LOD))
				{
					reference = entry.InstantiateOverlay(LOD, context, reference);
				}
			}
			if (reference != null && reference.asset.overlayName != overlayData.asset.overlayName)
			{
				reference = null;
			}
			if (reference == null)
			{
				reference = overlayData.Duplicate();
			}
			else
			{
				reference.colorData = overlayData.colorData.Duplicate();
			}
			return reference;
		}
	
	
		public class OptimizedLODManager
		{
			public class OptimizedConversionEntry
			{
				public struct ConversionElement
				{
					public int LOD;
					public int destHash;
					public bool delete;
					public UMALODConversionEntry entry;
				}
				public int SourceHash;
				private ConversionElement[] conversionElements;
				private int elementsInUse;
				public void AddEntry(UMALODConversionEntry entry)
				{
					if (conversionElements == null)
					{
						conversionElements = new ConversionElement[1];
					}
					if (elementsInUse == conversionElements.Length)
					{
						var newConversionArray = new ConversionElement[conversionElements.Length + 1];
						System.Array.Copy(conversionElements, newConversionArray, conversionElements.Length);
						conversionElements = newConversionArray;
					}
					conversionElements[elementsInUse++] = new ConversionElement()
					{
						LOD = entry.LODLevel,
						delete = string.IsNullOrEmpty(entry.DestinationPieceName),
						destHash = string.IsNullOrEmpty(entry.DestinationPieceName) ? 0 : UMA.UMASkeleton.StringToHash(entry.DestinationPieceName),
						entry = entry
					};
				}
				public OverlayData InstantiateOverlay(int LOD, UMAContext context, OverlayData reference)
				{
					int knownLOD = int.MinValue;
					int knownDestination = 0;
					for (int i = 0; i < elementsInUse; i++)
					{
						if (conversionElements[i].LOD > knownLOD && conversionElements[i].LOD <= LOD)
						{
							knownLOD = conversionElements[i].LOD;
							knownDestination = conversionElements[i].destHash;
						}
					}
					if (reference != null)
					{
						if (UMASkeleton.StringToHash(reference.asset.overlayName) == knownDestination)
						{
							return reference;
						}
					}
					if (knownDestination == 0) return null;
					return context.InstantiateOverlay(knownDestination);
				}
				internal void InstantiateSlot(int LOD, UMAContext context, ref SlotData processedSlotData)
				{
					int knownLOD = int.MinValue;
					int knownDestination = 0;
					bool knownDelete = false;
					for (int i = 0; i < elementsInUse; i++)
					{
						if (conversionElements[i].LOD > knownLOD && conversionElements[i].LOD <= LOD)
						{
							knownLOD = conversionElements[i].LOD;
							knownDestination = conversionElements[i].destHash;
							knownDelete = conversionElements[i].delete;
						}
					}
					if (knownDelete)
					{
						processedSlotData = null;
						return;
					}
					if (processedSlotData != null)
					{
						if (processedSlotData.asset.nameHash == knownDestination) return;
					}
					if (knownDestination == 0)
					{
						processedSlotData = null;
						return;
					}
					processedSlotData = context.InstantiateSlot(knownDestination);
				}
				public int elementCount { get { return elementsInUse; } }
				public int GetElementLOD(int idx)
				{
					return conversionElements[idx].LOD;
				}
				public void SetElementLOD(int idx, int value)
				{
					conversionElements[idx].LOD = value;
					conversionElements[idx].entry.LODLevel = value;
				}
	
				public string GetElementDestinationName(int idx)
				{
					return conversionElements[idx].entry.DestinationPieceName;
				}
				public void SetElementDestinationName(int idx, string value)
				{
					conversionElements[idx].entry.DestinationPieceName = value;
				}
	
				public string GetSourceName()
				{
					return conversionElements[0].entry.SourcePieceName;
				}
	
				public UMALODConversionEntry.ConversionGroup GetGroup()
				{
					return conversionElements[0].entry.group;
				}
	
				public UMALODConversionEntry GetRawElement(int idx)
				{
					return conversionElements[idx].entry;
				}
	
				public void RemoveElement(int idx)
				{
					if (idx < 0 || idx >= elementsInUse) return;
					for (int i = 0; i < elementsInUse-1; i++)
					{
						conversionElements[i] = conversionElements[i + (i >= idx ? 1 : 0)];
					}
					elementsInUse--;
				}
	
				public int GetHighestLOD()
				{
					int res = int.MinValue;
					for (int i = 0; i < elementsInUse; i++)
					{
						if (conversionElements[i].LOD > res) res = conversionElements[i].LOD;
					}
					return res;
				}
	
				internal bool hasChange(int LOD)
				{
					int knownLOD = int.MinValue;
					int knownDestination = 0;
					for (int i = 0; i < elementsInUse; i++)
					{
						if (conversionElements[i].LOD > knownLOD && conversionElements[i].LOD <= LOD)
						{
							knownLOD = conversionElements[i].LOD;
							knownDestination = conversionElements[i].destHash;
						}
					}
					return SourceHash != knownDestination;
				}
			}
	
			private Dictionary<int, OptimizedConversionEntry>[] optimizedDataSet;
	
			public void Validate()
			{
				if (optimizedDataSet == null)
				{
					optimizedDataSet = new Dictionary<int, OptimizedConversionEntry>[3];
					for (int i = 0; i < optimizedDataSet.Length; i++)
					{
						optimizedDataSet[i] = new Dictionary<int, OptimizedConversionEntry>();
					}
				}
			}
	
			public OptimizedConversionEntry AddConversion(UMALODConversionEntry conversion)
			{
				int sourceHash = UMASkeleton.StringToHash(conversion.SourcePieceName);
				int group = (int)conversion.group;
				var dataSet = optimizedDataSet[group];
				OptimizedConversionEntry entry;
	
				if( !dataSet.TryGetValue(sourceHash, out entry) )
				{
					entry = new OptimizedConversionEntry() { SourceHash = sourceHash };
					dataSet.Add(sourceHash, entry);
				}
				entry.AddEntry(conversion);
				return entry;
			}
	
			public void AddConversions(UMALODConversionEntry[] Conversions)
			{
				Validate();
				if (Conversions == null) return;
				foreach (var conversion in Conversions)
				{
					AddConversion(conversion);
				}
			}
	
			public OptimizedConversionEntry[] GetAllEntries()
			{
				var res = new OptimizedConversionEntry[GetTotalCount()];
				int idx = 0;
				for (int i = 0; i < optimizedDataSet.Length; i++)
				{
					foreach (var entry in optimizedDataSet[i].Values)
					{
						res[idx++] = entry;
					}
				}
				return res;
			}
	
			private int GetTotalCount()
			{
				int res = 0;
				for (int i = 0; i < optimizedDataSet.Length; i++)
				{
					res += optimizedDataSet[i].Count;
				}
				return res;
			}
	
			public void Remove(OptimizedConversionEntry element)
			{
				optimizedDataSet[(int)element.GetGroup()].Remove(element.SourceHash);
			}
	
			internal OptimizedConversionEntry FindEntry(UMALODConversionEntry.ConversionGroup group, int sourceHash)
			{
				OptimizedConversionEntry entry;
				if (optimizedDataSet[(int)group].TryGetValue(sourceHash, out entry))
					return entry;
				return null;
			}
		}
	
		public static UMALODManager FindInstance()
		{
			var managerGO = GameObject.Find("LODManager");
			if (managerGO == null) managerGO = GameObject.Find("AutoLoaderController");
			if (managerGO == null) return null;
			return managerGO.GetComponent<UMALODManager>();
		}
	}
}