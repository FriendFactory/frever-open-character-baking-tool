using System;
using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UMA.PowerTools
{
	public static class PowerToolsRuntime
	{
		public static Type GetPreferredRecipeFormat()
		{
			foreach (var format in UMARecipeBase.GetRecipeFormats())
			{
				if (format.FullName == "UMA.RecipeTools.BinaryRecipeFloat")
					return format;
			}
			return typeof(UMATextRecipe);
		}
		
		public static bool IsGeneratedTexture(UMAMaterial.ChannelType channelType)
		{
			return channelType == UMAMaterial.ChannelType.Texture || channelType == UMAMaterial.ChannelType.DiffuseTexture || channelType == UMAMaterial.ChannelType.NormalMap;
		}		

		public static GameObject SaveCharacterPrefab(string assetFolder, string name, UMAData umaData, bool exportTPose = false)
		{
#if UNITY_EDITOR
			Avatar avatar = umaData.animator.avatar;

			EnsureProjectFolder(assetFolder);
			var prefabPath = assetFolder + "/" + name + ".prefab";

			var asset = ScriptableObject.CreateInstance(GetPreferredRecipeFormat()) as UMARecipeBase;
			UMAContext context = UMAContext.Instance != null ? UMAContext.Instance : GameObject.Find("UMAContext").GetComponent<UMAContext>(); // temporary hack till FindInstance is made public
			asset.Save(umaData.umaRecipe, context);
			AssetDatabase.CreateAsset(asset, assetFolder+"/"+name+"_recipe.asset");
			AssetDatabase.SaveAssets();

			foreach(var generatedMaterial in umaData.generatedMaterials.materials)
			{
				for(int i = 0; i < generatedMaterial.resultingAtlasList.Length; i++)
				{
					var materialChannel = generatedMaterial.umaMaterial.channels[i];
					if( !IsGeneratedTexture(materialChannel.channelType)) continue;
					var atlas = generatedMaterial.resultingAtlasList[i];
					Texture2D tex2D = atlas as Texture2D;
					if (tex2D == null)
					{
						tex2D = new Texture2D(atlas.width, atlas.height, TextureFormat.ARGB32, false, PlayerSettings.colorSpace == ColorSpace.Linear || materialChannel.channelType == UMAMaterial.ChannelType.NormalMap);
						RenderTexture.active = atlas as RenderTexture;
						tex2D.ReadPixels(new Rect(0, 0, atlas.width, atlas.height), 0, 0, false);
						RenderTexture.active = null;
#if !UNITY_ANDROID
						if (materialChannel.channelType == UMAMaterial.ChannelType.NormalMap)
						{
							//TransformNormalMap(tex2D);
						}
#endif
					}
					tex2D.name = name + $"{generatedMaterial.GetHashCode()}_" + generatedMaterial.umaMaterial.name + materialChannel.materialPropertyName;
					WriteAllBytes(GetAssetPath(assetFolder + "/" + tex2D.name + ".png"), tex2D.EncodeToPNG());
				}
			}
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

			if (exportTPose)
			{
				var WriteDefaultPoseMethod = typeof(Animator).GetMethod("WriteDefaultPose", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null, Type.EmptyTypes, null);
				if (WriteDefaultPoseMethod != null)
				{
					umaData.transform.position = Vector3.zero;
					umaData.transform.rotation = Quaternion.identity;
					umaData.transform.localScale = Vector3.one;
					WriteDefaultPoseMethod.Invoke(umaData.animator, null);
				}
				else
				{
					Debug.LogError("Animator.WriteDefaultPose not found, cannot export prefab in tpose");
				}
			}

#if UNITY_2018_3_OR_NEWER
			var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(umaData.gameObject, prefabPath, InteractionMode.AutomatedAction);
#else
			var prefab = PrefabUtility.CreatePrefab(assetFolder + "/" + name + ".prefab", umaData.gameObject, ReplacePrefabOptions.ConnectToPrefab);
#endif
			avatar.name = name;
			AssetDatabase.AddObjectToAsset(avatar, prefab);
			for (int i = 0; i < umaData.rendererCount; i++)
				AssetDatabase.AddObjectToAsset(umaData.GetRenderer(i).sharedMesh, prefab);

			var materials = new Material[umaData.generatedMaterials.materials.Count];
			for (int j = 0; j < umaData.generatedMaterials.materials.Count; j++)
			{
				var generatedMaterial = umaData.generatedMaterials.materials[j];
				var mat = generatedMaterial.material;
				materials[j] = mat;
			
				for(int i = 0; i < generatedMaterial.resultingAtlasList.Length; i++)
				{
					var materialChannel = generatedMaterial.umaMaterial.channels[i];
					if( !IsGeneratedTexture(materialChannel.channelType)) continue;
					var texturePath = assetFolder + "/" +name + $"{generatedMaterial.GetHashCode()}_" + generatedMaterial.umaMaterial.name + generatedMaterial.umaMaterial.channels[i].materialPropertyName + ".png";
					if( materialChannel.channelType == UMA.UMAMaterial.ChannelType.NormalMap)
					{
						var ti = TextureImporter.GetAtPath(texturePath) as TextureImporter;
						ti.textureType = TextureImporterType.NormalMap;
						AssetDatabase.ImportAsset(texturePath);
					}
					else
					{
						if( PlayerSettings.colorSpace == ColorSpace.Linear )
						{
							var ti = TextureImporter.GetAtPath(texturePath) as TextureImporter;
							ti.linearTexture = true;
							AssetDatabase.ImportAsset(texturePath);
						}
					}
					mat.SetTexture(materialChannel.materialPropertyName, AssetDatabase.LoadMainAssetAtPath(texturePath) as Texture);
				}
				AssetDatabase.AddObjectToAsset(mat, prefab);
			}

			AssetDatabase.SaveAssets();
			AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(prefab));
			prefab.GetComponentsInChildren<Animator>(true)[0].avatar = avatar;

			foreach (var component in prefab.GetComponents<MonoBehaviour>())
			{
				FilterUmaComponents(component);
			}


			foreach (var component in prefab.GetComponentsInChildren<MonoBehaviour>(true))
			{
				FilterUmaComponents(component);
			}

			for (int i = 0; i < umaData.rendererCount; i++)
			{
				var originalRenderer = umaData.GetRenderer(i);
				var newRenderer = prefab.transform.Find(originalRenderer.name).GetComponent<SkinnedMeshRenderer>();
				var originalMaterials = originalRenderer.sharedMaterials;
				var newMaterials = new Material[originalMaterials.Length];
				for(int j = 0; j < originalMaterials.Length; j++)
				{
					newMaterials[j] = materials[Array.IndexOf(materials, originalMaterials[j])];
				}
				newRenderer.sharedMaterials = newMaterials;
				newRenderer.sharedMesh = originalRenderer.sharedMesh;
				EditorUtility.SetDirty(newRenderer);
			}

#if UNITY_2018_3_OR_NEWER
			PrefabUtility.SavePrefabAsset(prefab);
#endif
			AssetDatabase.ImportAsset(prefabPath);
			AssetDatabase.SaveAssets();
			return prefab;
#else
			throw new NotImplementedException("SaveCharacterPrefab Cannot save a prefab outside of the Unity environment. This method only works in the editor!");
#endif
		}

		private static void FilterUmaComponents(MonoBehaviour component)
		{
			if (IsUmaComponent(component))
			{
				UMAData umaDataComponent = component as UMAData;
				if (umaDataComponent != null)
				{
					umaDataComponent.umaRoot = null;
				}
				UnityEngine.Object.DestroyImmediate(component, true);
			}
		}

		private static bool IsUmaComponent(MonoBehaviour component)
		{
			var nameSpace = component.GetType().Namespace;
            if (string.IsNullOrEmpty(nameSpace)) return false;
			return string.Compare(nameSpace, "UMA", true) == 0 || nameSpace.StartsWith("UMA.", StringComparison.InvariantCultureIgnoreCase);
		}

		private static void TransformNormalMap(Texture2D tex2D)
		{
			var pixels = tex2D.GetPixels32();
			for (int i = 0; i < pixels.Length; i ++)
			{
				TransformNormalMapPixel(ref pixels[i]);
			}
			tex2D.SetPixels32(pixels);
		}

		private static void TransformNormalMapPixel(ref Color32 color)
		{
			byte R = color.a;
			byte G = color.g;
			int iR = R;
			int iG = G;
			int B = Mathf.FloorToInt(Mathf.Sqrt(65535f - (iR * iR + iG * iG)));
			color.a = 255;
			color.r = R;
			color.g = G;
			color.b = (byte)B;			
		}

		public static void SaveCharacterPrefab(UMAData umaData, string prefabName)
		{
#if UNITY_EDITOR
			EnsureProjectFolder("Assets/UMA/UMA_Generated/Complete");
			var assetFolder = AssetDatabase.GenerateUniqueAssetPath("Assets/UMA/UMA_Generated/Complete/" + prefabName);
			SaveCharacterPrefab(assetFolder, prefabName, umaData);
#else
			throw new NotImplementedException("SaveCharacterPrefab Cannot save a prefab outside of the Unity environment. This method only works in the editor!");
#endif
		}

		#region helper methods

		public static void EnsureProjectFolder(string folder)
		{
#if UNITY_EDITOR
			if (!System.IO.Directory.Exists(System.IO.Directory.GetCurrentDirectory() + "/" + folder))
			{
				EnsureProjectFolder(System.IO.Path.GetDirectoryName(folder));
				AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(folder), System.IO.Path.GetFileName(folder));
			}
#else
			throw new NotImplementedException("EnsureProjectFolder: The concept of ensuring a project folder outside the Unity environment is flawed. This method only works in the editor!");
#endif
		}

		public static string GetAssetPath(string path)
		{
			return System.IO.Directory.GetCurrentDirectory() + "/" + path;
		}

		public static void WriteAllBytes(string path, byte[] data)
		{
			using (var file = System.IO.File.Open(path, System.IO.FileMode.OpenOrCreate))
			{
				file.Write(data, 0, data.Length);
				file.SetLength(data.Length);
				file.Flush();
			}
		}
		#endregion
	}
}
