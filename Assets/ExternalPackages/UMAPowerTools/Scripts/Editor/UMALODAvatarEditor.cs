using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace UMA.PowerTools
{
	[CustomEditor(typeof(UMALODAvatar))]
	public class UMALODAvatarEditor : Editor 
	{
	
		public override void OnInspectorGUI()
		{
			var avatar = target as UMALODAvatar;
			base.OnInspectorGUI();
			if( avatar.lodLevel != null )
			{
				EditorGUILayout.LabelField("LOD", avatar.lodLevel.index.ToString());
			}
		}
		[MenuItem("UMA/Power Tools/Create Autoloader Prefabs")]
		public static void CreateAutoLoaderPrefabs()
		{
			foreach (var selectedObject in Selection.objects)
			{
				RecursiveFindAndExecute(selectedObject, new System.Action<UMARecipeBase>(CreateAutoLoaderPrefab));
			}
			AssetDatabase.SaveAssets();
		}
	
		private static void RecursiveFindAndExecute<T>(Object obj, System.Action<T> action) where T : class
		{
			var cast = obj as T;
			if (cast != null)
			{
				action(cast);
				return;
			}
			var genericType = typeof(T);
			var path = AssetDatabase.GetAssetPath(obj);
			if (System.IO.Directory.Exists(path))
			{
				var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name, new string[] { path });
				foreach (var guid in guids)
				{
					cast = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), genericType) as T;
					if (cast != null)
					{
						action(cast);
					}
				}
			}
		}
	
		public static void CreateAutoLoaderPrefab(UMARecipeBase recipe)
		{
			var recipePath = AssetDatabase.GetAssetPath(recipe);
			var prefabPath = recipePath.Substring(0, recipePath.Length - 6) + ".prefab";
			var existingPrefab = AssetDatabase.LoadMainAssetAtPath(prefabPath);
			if (existingPrefab != null)
			{
				var prefabGO = existingPrefab as GameObject;
				var avatarBase = prefabGO.GetComponent<UMAAvatarBase>();
				DestroyImmediate(avatarBase, true);
				var avatar = prefabGO.AddComponent<UMA.PowerTools.UMALODAvatar>();
				avatar.umaRecipe = recipe;
				EditorUtility.SetDirty(prefabGO);
			}
			else
			{
				var prefabGO = new GameObject(recipe.name);
				var avatar = prefabGO.AddComponent<UMA.PowerTools.UMALODAvatar>();
				avatar.umaRecipe = recipe;
				PrefabUtility.CreatePrefab(prefabPath, prefabGO, ReplacePrefabOptions.ReplaceNameBased);
				DestroyImmediate(prefabGO);
			}
		}
	}
}