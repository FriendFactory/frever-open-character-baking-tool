using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace UMA.PowerTools
{
	public class UMASaveCharacters : Editor
	{
		public static bool FileExists(string path)
		{
			return System.IO.Directory.Exists(System.IO.Directory.GetCurrentDirectory() + "/" + path);
		}

		[MenuItem("UMA/Power Tools/Save Character Prefabs")]
		public static void SaveCharacterPrefabsMenuItem(GameObject[] targets)
		{
			var newCharacters = new List<GameObject>();
			HashSet<UMAAvatarBase> saved = new HashSet<UMAAvatarBase>();
			foreach (var go in targets)
			{
				var selectedTransform = go.transform;
				UMAAvatarBase avatar = selectedTransform.GetComponent<UMAAvatarBase>();
				while( avatar == null && selectedTransform.parent != null )
				{
					selectedTransform = selectedTransform.parent;
					avatar = selectedTransform.GetComponent<UMAAvatarBase>();
				}
				if (avatar != null && PrefabUtility.GetPrefabObject(avatar.umaData.umaRoot) == null)
				{
					if (saved.Add(avatar))
					{
                        var id = avatar.name.Replace("CharacterView", string.Empty).Replace("/", string.Empty).Trim();
                        var path = $"Assets/ExportedCharacters/{id}/{avatar.name}.prefab";
                        var folder = $"Assets/ExportedCharacters/{id}/";
                        if (!Directory.Exists(folder))
                        {
                            Directory.CreateDirectory(folder);
                        }
                        if (FileExists(path))
						{
							Debug.LogWarning("Overwrite of prefabs not supported!");
						}
						else if (path.Length != 0)
						{
							newCharacters.Add(SaveCharacterPrefab(System.IO.Path.GetDirectoryName(path), System.IO.Path.GetFileNameWithoutExtension(path), avatar.umaData));
						}
					}
				}
			}
			if (newCharacters.Count > 0)
			{
				Selection.objects = newCharacters.ToArray();
			}
		}
		public static GameObject SaveCharacterPrefab(string assetFolder, string name, UMAData umaData)
		{
			return PowerToolsRuntime.SaveCharacterPrefab(assetFolder, name, umaData, EditorPrefs.GetBool("UnLogickFactory_PowerTools_Prefab_TPose"));
		}

		public static void SaveCharacterPrefab(UMAData umaData, string prefabName)
		{
			PowerToolsRuntime.EnsureProjectFolder("Assets/UMA/UMA_Generated/Complete");
			var assetFolder = AssetDatabase.GenerateUniqueAssetPath("Assets/UMA/UMA_Generated/Complete/" + prefabName);
			PowerToolsRuntime.SaveCharacterPrefab(assetFolder, prefabName, umaData, EditorPrefs.GetBool("UnLogickFactory_PowerTools_Prefab_TPose"));
		}
	}
}
