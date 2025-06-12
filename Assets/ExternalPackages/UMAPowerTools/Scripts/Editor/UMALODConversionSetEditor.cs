using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UMA.Integrations.PowerTools;

namespace UMA.PowerTools
{
	[CustomEditor(typeof(UMALODConversionSet))]
	public class UMALODConversionSetEditor : Editor 
	{
		UMALODConversionSet conversionSet;
		bool canUpdate;
		UMALODManager.OptimizedLODManager optimizedManager;
		List<UMALODManager.OptimizedLODManager.OptimizedConversionEntry> sources;
		IComparer<UMALODManager.OptimizedLODManager.OptimizedConversionEntry> sortInstance;
		class sortingAlgo : IComparer<UMALODManager.OptimizedLODManager.OptimizedConversionEntry>
		{
			public int Compare(UMALODManager.OptimizedLODManager.OptimizedConversionEntry x, UMALODManager.OptimizedLODManager.OptimizedConversionEntry y)
			{
				int res = string.Compare(x.GetSourceName(), y.GetSourceName());
				if (res == 0)
				{
					res = x.GetGroup() == UMALODConversionEntry.ConversionGroup.SlotData ? -1 : 1;
				}
				return res;
			}
		}
	
		public void OnEnable()
		{
			conversionSet = target as UMALODConversionSet;
			optimizedManager = new UMALODManager.OptimizedLODManager();
			optimizedManager.AddConversions(conversionSet.Conversions);
			sources = new List<UMALODManager.OptimizedLODManager.OptimizedConversionEntry>(optimizedManager.GetAllEntries());
			sortInstance = new sortingAlgo();
			sources.Sort(sortInstance);
			canUpdate = false;
		}
	
		public override void OnInspectorGUI()
		{
			GUILayout.Label("LOD Conversion Set", EditorStyles.boldLabel);
	
			GUILayout.Space(20);
			for (int i = 0; i < sources.Count; i++)
			{
				var element = sources[i];
				var sourceName = element.GetSourceName();
				var count = element.elementCount;
				var group = element.GetGroup();
				
				GUILayout.BeginHorizontal();
				GUILayout.Label(sourceName, EditorStyles.boldLabel, GUILayout.ExpandWidth(false));
				GUILayout.Label(" - ", EditorStyles.boldLabel, GUILayout.ExpandWidth(false));
				GUILayout.Label(group.ToString(), EditorStyles.boldLabel);
				if (GUILayout.Button("Limit", GUILayout.Width(60.0f)))
				{
					var entry = new UMALODConversionEntry() { groupInt = conversionSet.Conversions[0].groupInt, DestinationPieceName = "", LODLevel = conversionSet.Conversions[conversionSet.Conversions.Length-1].LODLevel+1, SourcePieceName = sourceName };
					ArrayUtility.Add(ref conversionSet.Conversions, entry);
					optimizedManager.AddConversion(entry);
					canUpdate = false;
				}
				if (GUILayout.Button("-", GUILayout.Width(20.0f)))
				{
					for (int j = 0; j < count; j++)
					{
						var raw = element.GetRawElement(j);
						ArrayUtility.RemoveAt(ref conversionSet.Conversions, ArrayUtility.IndexOf(conversionSet.Conversions, raw));
					}
					sources.RemoveAt(i);
					optimizedManager.Remove(element);
					canUpdate = false;
				}
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
				GUILayout.Space(10);
				GUILayout.Label("LOD", GUILayout.Width(40.0f));
				GUILayout.Label("Override");
				GUILayout.Label(group == UMALODConversionEntry.ConversionGroup.SlotData ? "Drop Slot" : "Drop Overlay", GUILayout.Width(140.0f));
				GUILayout.Space(20);
				GUILayout.EndHorizontal();
				for (int j = 0; j < count; j++)
				{
					GUILayout.BeginHorizontal();
					GUILayout.Space(10);
					var newLOD = EditorGUILayout.IntField(element.GetElementLOD(j), GUILayout.Width(40.0f));
					if (GUI.changed && canUpdate)
					{
						element.SetElementLOD(j, newLOD);
						canUpdate = false;
					}
	
					var newText = EditorGUILayout.TextField(element.GetElementDestinationName(j));
					if (GUI.changed && canUpdate)
					{
						element.SetElementDestinationName(j, newText);
						canUpdate = false;
					} 
	
					if( group == UMALODConversionEntry.ConversionGroup.SlotData )
					{
						var newSlotData = EditorGUILayout.ObjectField(null, typeof(SlotDataAsset), false, GUILayout.Width(140.0f)) as SlotDataAsset;
						if( newSlotData != null )
						{
							element.SetElementDestinationName(j, newSlotData.slotName);
							canUpdate = false;
						}
					}
					else
					{
						var newOverlayData = EditorGUILayout.ObjectField(null, typeof(OverlayDataAsset), false, GUILayout.Width(140.0f)) as OverlayDataAsset;
						if (newOverlayData != null)
						{
							element.SetElementDestinationName(j, newOverlayData.overlayName);
							canUpdate = false;
						}
					}
	
					if (GUILayout.Button("-", GUILayout.Width(20.0f)))
					{
						var raw = element.GetRawElement(j);
						element.RemoveElement(j);
						ArrayUtility.RemoveAt(ref conversionSet.Conversions, ArrayUtility.IndexOf(conversionSet.Conversions, raw));
						canUpdate = false;
					}
					GUILayout.EndHorizontal();
				}
				Rect newDropArea = GUILayoutUtility.GetRect(0.0f, 20.0f, GUILayout.ExpandWidth(true));
				if (group == UMALODConversionEntry.ConversionGroup.SlotData)
				{
					GUI.Box(newDropArea, "Drag new LOD Slots here");
				}
				else
				{
					GUI.Box(newDropArea, "Drag new LOD Overlays here");
				}
				NewDropAreaGUI(newDropArea, element);
			}
	
			GUILayout.Space(20);
			Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
			GUI.Box(dropArea, "Drag Slots here");
			NewDropAreaGUI(dropArea, null);
	
			if (GUILayout.Button("Clear List"))
			{
				conversionSet.Conversions = new UMALODConversionEntry[0];
				sources.Clear();
				optimizedManager = new UMALODManager.OptimizedLODManager();
			
				canUpdate = false;
			}
	
			if (!canUpdate)
			{
				EditorUtility.SetDirty(conversionSet);
				AssetDatabase.SaveAssets();
			}
	
	
			canUpdate = true;
		}
		private void NewDropAreaGUI(Rect dropArea, UMALODManager.OptimizedLODManager.OptimizedConversionEntry element)
		{
			var evt = Event.current;
	
			if (evt.type == EventType.DragUpdated)
			{
				if (dropArea.Contains(evt.mousePosition))
				{
					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
				}
			}
	
			if (evt.type == EventType.DragPerform)
			{
				if (dropArea.Contains(evt.mousePosition))
				{
					DragAndDrop.AcceptDrag();
	
					var draggedObjects = DragAndDrop.objectReferences;
					for (int i = 0; i < draggedObjects.Length; i++)
					{
						if (draggedObjects[i] != null)
						{
							AddObject(draggedObjects[i], element);
						}
					}
				}
			}
		}
	
		private void AddObject(object obj, UMALODManager.OptimizedLODManager.OptimizedConversionEntry element)
		{
			var tempSlotData = obj as SlotDataAsset;
			if (tempSlotData)
			{
				AddSlotData(tempSlotData, element);
				return;
			}
	
			var tempOverlayData = obj as OverlayDataAsset;
			if (tempOverlayData != null)
			{
				AddOverlayData(tempOverlayData, element);
				return;
			}
	
			//var path = AssetDatabase.GetAssetPath(obj);
			//if (System.IO.Directory.Exists(path))
			//{
			//	var assetFiles = System.IO.Directory.GetFiles(path, "*.asset");
			//	foreach (var assetFile in assetFiles)
			//	{
			//		AddObject(AssetDatabase.LoadMainAssetAtPath(assetFile), element);
			//	}
			//}
		}
	
		private void AddOverlayData(OverlayDataAsset tempOverlayData, UMALODManager.OptimizedLODManager.OptimizedConversionEntry element)
		{
			if (element != null)
			{
				if (element.GetGroup() != UMALODConversionEntry.ConversionGroup.OverlayData) return;
	
				var entry = new UMALODConversionEntry() { SourcePieceName = element.GetSourceName(), group = UMALODConversionEntry.ConversionGroup.OverlayData, DestinationPieceName = tempOverlayData.overlayName, LODLevel = element.GetHighestLOD() + 1 };
				ArrayUtility.Add(ref conversionSet.Conversions, entry);
				element.AddEntry(entry);
				canUpdate = false;
				EditorUtility.SetDirty(conversionSet);
			}
			else
			{
				var entry = new UMALODConversionEntry() { SourcePieceName = tempOverlayData.overlayName, group = UMALODConversionEntry.ConversionGroup.OverlayData, DestinationPieceName = tempOverlayData.overlayName, LODLevel = 0 };
				ArrayUtility.Add(ref conversionSet.Conversions, entry);
				element = optimizedManager.AddConversion(entry);
				sources.Add(element);
				sources.Sort(sortInstance);
				canUpdate = false;
				EditorUtility.SetDirty(conversionSet);
			}
		}
	
		private void AddSlotData(SlotDataAsset tempSlotData, UMALODManager.OptimizedLODManager.OptimizedConversionEntry element)
		{
			if (element != null)
			{
				if (element.GetGroup() != UMALODConversionEntry.ConversionGroup.SlotData) return;
	
				var entry = new UMALODConversionEntry() { SourcePieceName = element.GetSourceName(), group = UMALODConversionEntry.ConversionGroup.SlotData, DestinationPieceName = tempSlotData.slotName, LODLevel = element.GetHighestLOD() + 1 };
				ArrayUtility.Add(ref conversionSet.Conversions, entry);
				element.AddEntry(entry);
				canUpdate = false;
				EditorUtility.SetDirty(conversionSet);
			}
			else
			{
				var entry = new UMALODConversionEntry() { SourcePieceName = tempSlotData.slotName, group = UMALODConversionEntry.ConversionGroup.SlotData, DestinationPieceName = tempSlotData.slotName, LODLevel = 0 };
				ArrayUtility.Add(ref conversionSet.Conversions, entry);
				element = optimizedManager.AddConversion(entry);
				sources.Add(element);
				sources.Sort(sortInstance);
				canUpdate = false;
				EditorUtility.SetDirty(conversionSet);
			}
		}
		
	}
}