using UnityEngine;
using UnityEditor;

namespace UMA.PowerTools
{
	[CanEditMultipleObjects()]
	[CustomEditor(typeof(OverlayDataAsset))]
	public class OverlayDataAssetEditor : Editor
	{
		bool allCutouts;
		void OnEnable()
		{
			allCutouts = true;
			foreach (var t in targets)
			{
				var overlay = t as OverlayDataAsset;
				if (overlay.overlayType != OverlayDataAsset.OverlayType.Cutout)
				{
					allCutouts = false;
					break;
				}
			}
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			if (allCutouts)
			{
				GUILayout.Space(20);
				Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
				GUI.Box(dropArea, "Drag Slots here to setup triangle occlusion");
				GUILayout.Space(20);
				DropAreaGUI(dropArea);
			}
		}

		private void DropAreaGUI(Rect dropArea)
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
					foreach (var t in targets)
					{
						var overlay = t as OverlayDataAsset;
						for (int i = 0; i < draggedObjects.Length; i++)
						{
							if (draggedObjects[i])
							{
								SlotDataAsset tempSlotDataAsset = draggedObjects[i] as SlotDataAsset;
								if (tempSlotDataAsset)
								{
									overlay.UpdateOcclusion(tempSlotDataAsset);
								}
							}
						}
					}
				}
			}
		}
	}
}