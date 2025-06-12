using UnityEngine;
using UnityEditor;

namespace UMA.PowerTools
{
	public class MenuItems 
	{
		private const string _SceneBoneBaking = "UMA/Power Tools/Scene Bone Baking";
		[MenuItem(_SceneBoneBaking)]
		static void ToggleBoneBaking()
		{
			var generator = Object.FindObjectOfType<UMAGeneratorBuiltin>();
			if (generator == null) return;
			Undo.RecordObject(generator, "Toggle Scene Bone Baking");
			if (generator.meshCombiner is UMABoneBakingMeshCombiner)
			{
				var defaultMeshCombiner = Object.FindObjectOfType<UMADefaultMeshCombiner>();
				if (defaultMeshCombiner == null)
					defaultMeshCombiner = Spawn<UMADefaultMeshCombiner>(generator.transform.parent);
				generator.meshCombiner = defaultMeshCombiner;
			}
			else
			{
				var boneBakingMeshCombiner = Object.FindObjectOfType<UMABoneBakingMeshCombiner>();
				if (boneBakingMeshCombiner == null)
					boneBakingMeshCombiner = Spawn<UMABoneBakingMeshCombiner>(generator.transform.parent);
				generator.meshCombiner = boneBakingMeshCombiner;
			}
		}

		[MenuItem(_SceneBoneBaking, true)]
		static bool ToggleBoneBakingActive()
		{
			var generator = Object.FindObjectOfType<UMAGeneratorBuiltin>();
			if (generator == null)
			{
				Menu.SetChecked(_SceneBoneBaking, false);
				return false;
			}
			Menu.SetChecked(_SceneBoneBaking, generator.meshCombiner is UMABoneBakingMeshCombiner);
			return true;
		}

		private const string _ExportPrefabTPose = "UMA/Power Tools/Export Prefab in TPose";
		[MenuItem(_ExportPrefabTPose)]
		static void ToggleExportPrefabTPose()
		{
			EditorPrefs.SetBool("UnLogickFactory_PowerTools_Prefab_TPose", !EditorPrefs.GetBool("UnLogickFactory_PowerTools_Prefab_TPose"));
		}

		[MenuItem(_ExportPrefabTPose, true)]
		static bool ToggleExportPrefabTPoseActive()
		{
			Menu.SetChecked(_ExportPrefabTPose, EditorPrefs.GetBool("UnLogickFactory_PowerTools_Prefab_TPose"));
			return true;
		}

		private static T Spawn<T>(Transform parent)
			where T : MonoBehaviour
		{
			var go = new GameObject(typeof(T).Name);
			go.transform.parent = parent;
			return go.AddComponent<T>();
		}
	}
}