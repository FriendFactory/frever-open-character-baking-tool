using UnityEngine;
using UnityEditor;

namespace UMA.PowerTools
{
	[CustomEditor(typeof(UMARecipeBase), true)]
	public class PowerToolsRecipeEditor : Editors.RecipeEditor
	{
		//public override void OnSceneDrag(SceneView view)
		//{
		//    Debug.Log("here");
		//    base.OnSceneDrag(view);
		//}
		static GameObject RecipeDragPrefab;

		public override GameObject CreateAvatar(UMARecipeBase recipe)
		{
			var context = PowerToolsContext.FindInstance();
			if (context == null) return base.CreateAvatar(recipe);

			if( RecipeDragPrefab == null )
			{
				var recipeToolsFolderGuids = AssetDatabase.FindAssets("UMAPowerTools", new string[] { "Assets" });
				if (recipeToolsFolderGuids.Length > 0)
				{
					var path = AssetDatabase.GUIDToAssetPath(recipeToolsFolderGuids[0]);
					path = path + "/RecipeDragPrefab.prefab";
					RecipeDragPrefab = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
				}
			}

			if (RecipeDragPrefab == null || Event.current.shift )
			{
				var GO = new GameObject(recipe.name);
				var avatar = GO.AddComponent<UMALODAvatar>();
				avatar.umaRecipe = recipe;
				avatar.umaGenerator = context.generator;
				avatar.context = context.umaContext;
				avatar.controller = context.controller;
				avatar.lodManager = context.lodManager;
				avatar.animationController = context.animationController;
				return GO;
			}
			else
			{
				var GO = Instantiate(RecipeDragPrefab) as GameObject;
				GO.name = recipe.name;
				var avatar = GO.GetComponent<UMALODAvatar>();
				avatar.umaRecipe = recipe;
				avatar.umaGenerator = context.generator;
				avatar.context = context.umaContext;
				avatar.controller = context.controller;
				avatar.lodManager = context.lodManager;
				avatar.animationController = context.animationController;
				return GO;
			}
		}
	}
}
