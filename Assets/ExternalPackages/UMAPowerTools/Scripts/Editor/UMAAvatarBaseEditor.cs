using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace UMA.PowerTools
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(UMAAvatarBase), true)]
	public class UMAAvatarBaseEditor : Editor
	{
		public override void OnInspectorGUI() 
		{
			bool allowShow = false;
			bool allowHide = false;
			bool allowSaveRecipe = false;
			bool allowSavePrefab = false;

			if (EditorApplication.isPlaying)
			{
				var oldRecipes = new UMARecipeBase[targets.Length];
				var dynamicAvatars = new UMAAvatarBase[targets.Length];
				for (int i = 0; i < targets.Length; i++)
				{
					dynamicAvatars[i] = targets[i] as UMAAvatarBase;
					oldRecipes[i] = dynamicAvatars[i] != null ? dynamicAvatars[i].umaRecipe : null;
					if ((dynamicAvatars[i].umaData != null && (oldRecipes[i] != null || (dynamicAvatars[i].umaData.umaRecipe != null && dynamicAvatars[i].umaData.umaRecipe.raceData != null))) && dynamicAvatars[i].umaData.umaRoot == null)
					{
						// if there is a recipe, but the avatar isn't shown.
						allowShow = true;
					}
					if (dynamicAvatars[i] != null && dynamicAvatars[i].umaData != null && dynamicAvatars[i].umaData.umaRoot != null)
					{
						allowHide = true;
					}
					if (!allowSaveRecipe && dynamicAvatars[i].umaData != null && dynamicAvatars[i].umaData.umaRoot != null && oldRecipes[i] == null)
					{
						allowSaveRecipe = true;
					}
					if (!allowSavePrefab && dynamicAvatars[i] != null && dynamicAvatars[i].umaData != null && dynamicAvatars[i].umaData.umaRoot != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(dynamicAvatars[i].umaData)))
					{
						allowSavePrefab = true;
					}
				}
				base.OnInspectorGUI();
				for (int i = 0; i < targets.Length; i++)
				{
					if (dynamicAvatars[i].umaData != null && dynamicAvatars[i].umaData.umaRoot != null)
					{
						if (oldRecipes[i] != (dynamicAvatars[i] != null ? dynamicAvatars[i].umaRecipe : null))
						{
							dynamicAvatars[i].Load(dynamicAvatars[i].umaRecipe);
						}
					}
				}
				GUILayout.BeginHorizontal();
				if( allowShow )
				{
					if( GUILayout.Button("Show") )
					{
						for (int i = 0; i < targets.Length; i++)
						{
							if ((oldRecipes[i] != null || (dynamicAvatars[i].umaData != null && dynamicAvatars[i].umaData.umaRecipe != null && dynamicAvatars[i].umaData.umaRecipe.raceData != null)) && dynamicAvatars[i].umaData.umaRoot == null)
							{
								dynamicAvatars[i].Show();
							}
						}
					}
				}
				if (allowHide )
				{
					if( GUILayout.Button("Hide") )
					{
						for (int i = 0; i < targets.Length; i++)
						{
							if (dynamicAvatars[i] != null && dynamicAvatars[i].umaData != null && dynamicAvatars[i].umaData.umaRoot != null)
							{
								dynamicAvatars[i].Hide();
							}
						}
					}
				}
				if( allowSaveRecipe )
				{
					if( GUILayout.Button("Save Recipe") )
					{
						var recipeFormat = GetPreferredRecipeFormat();
						for (int i = 0; i < targets.Length; i++)
						{
							if (dynamicAvatars[i].umaData != null && dynamicAvatars[i].umaData.umaRoot != null && oldRecipes[i] == null)
							{
								var path = EditorUtility.SaveFilePanelInProject("Save serialized Avatar: " + dynamicAvatars[i].name, dynamicAvatars[i].name + ".asset", "asset", "Binary Recipe 8 bit");
								if (path.Length != 0)
								{
									var asset = ScriptableObject.CreateInstance(recipeFormat) as UMARecipeBase;
									asset.Save(dynamicAvatars[i].umaData.umaRecipe, dynamicAvatars[i].context);
									AssetDatabase.CreateAsset(asset, path);
									AssetDatabase.SaveAssets();
									Debug.Log(asset.GetInfo());
								}
							}
						}
					}
				}

				if (allowSavePrefab)
				{
					if (GUILayout.Button("Create Prefab"))
					{
						for (int i = 0; i < targets.Length; i++)
						{
							if (dynamicAvatars[i] != null && dynamicAvatars[i].umaData != null && dynamicAvatars[i].umaData.umaRoot != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(dynamicAvatars[i].umaData)))
							{
								UMASaveCharacters.SaveCharacterPrefab(dynamicAvatars[i].umaData, dynamicAvatars[i].name);
							}
						}
					}
				}

				GUILayout.EndHorizontal();

				return;
			}
			var recipes = new UMARecipeBase[targets.Length];
			for(int i=0; i < targets.Length; i++)
			{
				var avatar = targets[i] as UMAAvatarBase;
				recipes[i] = avatar != null ? avatar.umaRecipe : null;
			}
			base.OnInspectorGUI();
			var persistance = PowerPackPersistance.GetInstance();
			persistance.Awake();
		 
			for (int i = 0; i < targets.Length; i++)
			{
				var avatar = targets[i] as UMAAvatarBase;
				if (persistance.HasAvatar(avatar))
				{
					if (recipes[i] != (avatar != null ? avatar.umaRecipe : null))
					{
						persistance.HideAvatar(avatar);
						persistance.ShowAvatar(avatar);
					}
					allowHide = true;
					if (allowShow) break;
				}
				else
				{
					allowShow = true;
					if (allowHide) break;
				}
			}

			GUILayout.BeginHorizontal();
			if (allowShow)
			{
				if (GUILayout.Button("Show"))
				{
					for (int i = 0; i < targets.Length; i++)
					{
						var avatar = targets[i] as UMAAvatarBase;
						if (!persistance.HasAvatar(avatar))
						{
							persistance.ShowAvatar(avatar);
						}
					}
				}
			}
			if (allowHide)
			{
				if (GUILayout.Button("Hide"))
				{
					for (int i = 0; i < targets.Length; i++)
					{
						var avatar = targets[i] as UMAAvatarBase;
						if (persistance.HasAvatar(avatar))
						{
							persistance.HideAvatar(avatar);
						}
					}
				}
			}
			if (GUILayout.Button("Create Prefab"))
			{
				for (int i = 0; i < targets.Length; i++)
				{
					var avatar = targets[i] as UMAAvatarBase;
					bool existed = persistance.HasAvatar(avatar);
					if (existed)
					{
						persistance.HideAvatar(avatar);
					}
					prefabName = avatar.name;
					persistance.ShowAvatar(avatar, umaData_OnUpdated);
					persistance.HideAvatar(avatar);
					if (existed)
					{
						persistance.ShowAvatar(avatar);
					}
				}
			}
			if (GUILayout.Button("Hide All"))
			{
				persistance.HideAll();
			}

			GUILayout.EndHorizontal();
			persistance.Release();
		}

		public static Type GetPreferredRecipeFormat()
		{
			return PowerToolsRuntime.GetPreferredRecipeFormat();
		}

		private static string prefabName;
		void umaData_OnUpdated(UMAData umaData)
		{
			UMASaveCharacters.SaveCharacterPrefab(umaData, prefabName);
		}
	}
}
