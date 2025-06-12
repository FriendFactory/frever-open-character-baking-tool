using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace UMA
{
	/// <summary>
	/// Utility class for generating texture atlases
	/// </summary>
	[Serializable]
	public class UMAGeneratorCoroutine : WorkerCoroutine
	{
		private class GeneratedMaterialLookupKey : IEquatable<GeneratedMaterialLookupKey>
		{
			public List<OverlayData> overlayList;
			public UMARendererAsset rendererAsset;

			public bool Equals(GeneratedMaterialLookupKey other)
			{
				return (overlayList == other.overlayList && rendererAsset == other.rendererAsset);
			}
		}

		TextureProcessBaseCoroutine textureProcessCoroutine;

		MaxRectsBinPack packTexture;

		UMAGeneratorBase umaGenerator;
		UMAData umaData;
		Texture[] backUpTexture;
		bool updateMaterialList;
		int scaleFactor;
		MaterialDefinitionComparer comparer = new MaterialDefinitionComparer();
		List<UMAData.GeneratedMaterial> generatedMaterials;
		List<UMARendererAsset> uniqueRenderers = new List<UMARendererAsset>();
		List<UMAData.GeneratedMaterial> atlassedMaterials = new List<UMAData.GeneratedMaterial>(20);
		Dictionary<GeneratedMaterialLookupKey, UMAData.GeneratedMaterial> generatedMaterialLookup;


		public void Prepare(UMAGeneratorBase _umaGenerator, UMAData _umaData, TextureProcessBaseCoroutine textureProcessCoroutine, bool updateMaterialList, int InitialScaleFactor)
		{
			umaGenerator = _umaGenerator;
			umaData = _umaData;
			this.textureProcessCoroutine = textureProcessCoroutine;
			this.updateMaterialList = updateMaterialList;
			scaleFactor = InitialScaleFactor;
		}

		private UMAData.GeneratedMaterial FindOrCreateGeneratedMaterial(UMAMaterial umaMaterial, UMARendererAsset renderer = null)
		{
			if (umaMaterial.materialType == UMAMaterial.MaterialType.Atlas)
			{
				foreach (var atlassedMaterial in atlassedMaterials)
				{
					if (atlassedMaterial.umaMaterial == umaMaterial && atlassedMaterial.rendererAsset == renderer)
					{
						return atlassedMaterial;
					}
					else
					{
						if (atlassedMaterial.umaMaterial.Equals(umaMaterial) && atlassedMaterial.rendererAsset == renderer)
						{
							return atlassedMaterial;
						}
					}
				}
			}

			var res = new UMAData.GeneratedMaterial();
			res.rendererAsset = renderer;
			res.umaMaterial = umaMaterial;
			res.material = UnityEngine.Object.Instantiate(umaMaterial.material) as Material;
			res.material.name = umaMaterial.material.name;
			atlassedMaterials.Add(res);
			generatedMaterials.Add(res);
			return res;
		}

		protected override void Start()
		{
			if (generatedMaterialLookup == null)
			{
				generatedMaterialLookup = new Dictionary<GeneratedMaterialLookupKey, UMAData.GeneratedMaterial>(20);
			}
			else
			{
				generatedMaterialLookup.Clear();
			}
			backUpTexture = umaData.backUpTextures();
			umaData.CleanTextures();
			generatedMaterials = new List<UMAData.GeneratedMaterial>(20);
			atlassedMaterials.Clear();
			uniqueRenderers.Clear();

			SlotData[] slots = umaData.umaRecipe.slotDataList;

			for (int i = 0; i < slots.Length; i++)
			{
				SlotData slot = slots[i];
				if (slot == null)
					continue;

				//Keep a running list of unique RendererHashes from our slots
				//Null rendererAsset gets added, which is good, it is the default renderer.
				if(!uniqueRenderers.Contains(slot.rendererAsset))
					uniqueRenderers.Add(slot.rendererAsset);

				// Let's only add the default overlay if the slot has meshData and NO overlays
				// This should be able to be removed if default overlay/textures are ever added to uma materials...
				if ((slot.asset.meshData != null) && (slot.OverlayCount == 0))
				{
                    if (umaGenerator.defaultOverlaydata != null)
                        slot.AddOverlay(umaGenerator.defaultOverlaydata);
				}

                OverlayData overlay0 = slot.GetOverlay(0);
				if ((slot.asset.material != null) && (overlay0 != null))
				{
					GeneratedMaterialLookupKey lookupKey = new GeneratedMaterialLookupKey
					{
						overlayList = slot.GetOverlayList(),
						rendererAsset = slot.rendererAsset
					};

					UMAData.GeneratedMaterial generatedMaterial;
					if (!generatedMaterialLookup.TryGetValue(lookupKey, out generatedMaterial))
					{
						generatedMaterial = FindOrCreateGeneratedMaterial(slot.asset.material, slot.rendererAsset);
						generatedMaterialLookup.Add(lookupKey, generatedMaterial);
					}

					int validOverlayCount = 0;
					for (int j = 0; j < slot.OverlayCount; j++)
					{
						var overlay = slot.GetOverlay(j);
						if (overlay != null)
						{
							validOverlayCount++;
							#if (UNITY_STANDALONE || UNITY_IOS || UNITY_ANDROID || UNITY_PS4 || UNITY_XBOXONE) && !UNITY_2017_3_OR_NEWER //supported platforms for procedural materials
							if (overlay.isProcedural)
								overlay.GenerateProceduralTextures();
                            #endif
						}
					}

					UMAData.MaterialFragment tempMaterialDefinition = new UMAData.MaterialFragment();
					tempMaterialDefinition.baseOverlay = new UMAData.textureData();
					tempMaterialDefinition.baseOverlay.textureList = overlay0.textureArray;
					tempMaterialDefinition.baseOverlay.alphaTexture = overlay0.alphaMask;
					tempMaterialDefinition.baseOverlay.overlayType = overlay0.overlayType;

					tempMaterialDefinition.umaMaterial = slot.asset.material;
					tempMaterialDefinition.baseColor = overlay0.colorData.color;
					tempMaterialDefinition.size = overlay0.pixelCount;

					tempMaterialDefinition.overlays = new UMAData.textureData[validOverlayCount - 1];
					tempMaterialDefinition.overlayColors = new Color32[validOverlayCount - 1];
					tempMaterialDefinition.rects = new Rect[validOverlayCount - 1];
					tempMaterialDefinition.overlayData = new OverlayData[validOverlayCount];
					tempMaterialDefinition.channelMask = new Color[validOverlayCount][];
					tempMaterialDefinition.channelAdditiveMask = new Color[validOverlayCount][];
					tempMaterialDefinition.overlayData[0] = slot.GetOverlay(0);
					tempMaterialDefinition.channelMask[0] = slot.GetOverlay(0).colorData.channelMask;
					tempMaterialDefinition.channelAdditiveMask[0] = slot.GetOverlay(0).colorData.channelAdditiveMask;
					tempMaterialDefinition.slotData = slot;

					int overlayID = 0;
					for (int j = 1; j < slot.OverlayCount; j++)
					{
						OverlayData overlay = slot.GetOverlay(j);
						if (overlay == null)
							continue;

						tempMaterialDefinition.rects[overlayID] = overlay.rect;
						tempMaterialDefinition.overlays[overlayID] = new UMAData.textureData();
						tempMaterialDefinition.overlays[overlayID].textureList = overlay.textureArray;
						tempMaterialDefinition.overlays[overlayID].alphaTexture = overlay.alphaMask;
						tempMaterialDefinition.overlays[overlayID].overlayType = overlay.overlayType;
						tempMaterialDefinition.overlayColors[overlayID] = overlay.colorData.color;

						overlayID++;
						tempMaterialDefinition.overlayData[overlayID] = overlay;
						tempMaterialDefinition.channelMask[overlayID] = overlay.colorData.channelMask;
						tempMaterialDefinition.channelAdditiveMask[overlayID] = overlay.colorData.channelAdditiveMask;
					}

					tempMaterialDefinition.overlayList = lookupKey.overlayList;
					tempMaterialDefinition.isRectShared = false;
					for (int j = 0; j < generatedMaterial.materialFragments.Count; j++)
					{
						if (tempMaterialDefinition.overlayList == generatedMaterial.materialFragments[j].overlayList)
						{
							tempMaterialDefinition.isRectShared = true;
							tempMaterialDefinition.rectFragment = generatedMaterial.materialFragments[j];
							break;
						}
					}
					generatedMaterial.materialFragments.Add(tempMaterialDefinition);
				}
			}

			packTexture = new MaxRectsBinPack(umaGenerator.atlasResolution, umaGenerator.atlasResolution, false);
		}

		public class MaterialDefinitionComparer : IComparer<UMAData.MaterialFragment>
		{
			public int Compare(UMAData.MaterialFragment x, UMAData.MaterialFragment y)
			{
				return y.size - x.size;
			}
		}

		protected override IEnumerator workerMethod()
		{
			umaData.generatedMaterials.rendererAssets = uniqueRenderers;
			umaData.generatedMaterials.materials = generatedMaterials;

			GenerateAtlasData();
			OptimizeAtlas();

			textureProcessCoroutine.Prepare(umaData, umaGenerator);
			yield return textureProcessCoroutine;

			CleanBackUpTextures();
			UpdateUV();

			// HACK - is this the right place?
			SlotData[] slots = umaData.umaRecipe.slotDataList;
			for (int i = 0; i < slots.Length; i++)
			{
				var slot = slots[i];
				if (slot == null)
					continue;

#if (UNITY_STANDALONE || UNITY_IOS || UNITY_ANDROID || UNITY_PS4 || UNITY_XBOXONE) && !UNITY_2017_3_OR_NEWER //supported platforms for procedural materials
				for (int j = 1; j < slot.OverlayCount; j++)
				{
					OverlayData overlay = slot.GetOverlay(j);
					if ((overlay != null) && (overlay.isProcedural))
						overlay.ReleaseProceduralTextures();
				}
#endif
			}

			if (updateMaterialList)
			{
				for (int j = 0; j < umaData.rendererCount; j++)
				{
					var renderer = umaData.GetRenderer(j);
					var mats = renderer.sharedMaterials;
					var newMats = new Material[mats.Length];
					var atlasses = umaData.generatedMaterials.materials;
					int materialIndex = 0;
					for (int i = 0; i < atlasses.Count; i++)
					{
						if (atlasses[i].rendererAsset == umaData.GetRendererAsset(j))
						{
							UMAUtils.DestroySceneObject(mats[materialIndex]);
							newMats[materialIndex] = atlasses[i].material;
							materialIndex++;
						}
					}
					renderer.sharedMaterials = newMats;
				}
			}
		}

		protected override void Stop()
		{

		}

		private void CleanBackUpTextures()
		{
			for (int textureIndex = 0; textureIndex < backUpTexture.Length; textureIndex++)
			{
				if (backUpTexture[textureIndex] != null)
				{
					Texture tempTexture = backUpTexture[textureIndex];
					if (tempTexture is RenderTexture)
					{
						RenderTexture tempRenderTexture = tempTexture as RenderTexture;
						tempRenderTexture.Release();
						UMAUtils.DestroySceneObject(tempRenderTexture);
						tempRenderTexture = null;
					}
					else
					{
						UMAUtils.DestroySceneObject(tempTexture);
					}
					backUpTexture[textureIndex] = null;
				}
			}
		}

		private void ReduceOnXAxis(UMAData.GeneratedMaterial generatedMaterial, UMAData.GeneratedMaterial newMaterial, float min)
		{
			int lastElementIndex = generatedMaterial.materialFragments.Count - 1;
			for (int atlasElementIndex = lastElementIndex; atlasElementIndex >= 0 ; --atlasElementIndex)
			{
				UMAData.MaterialFragment fragment = generatedMaterial.materialFragments[atlasElementIndex]; 
				if (fragment.atlasRegion.xMax > min || fragment.atlasRegion.yMax > min)
				{
					// This fragment needs to be moved to a new material
					newMaterial.materialFragments.Add(fragment);	// This materials are going to get sorted on the final iteration 
					generatedMaterial.materialFragments.RemoveAt(atlasElementIndex);
				}
			}
		}
		
		private void SplitAtlas(UMAData.GeneratedMaterial generatedMaterial)
		{
			// This block will handle the splitting of the atlases if it is not a squared one
			Vector2 cropResoution = CalculateOptimizeAtlasCropResolution(generatedMaterial);

			if (!Mathf.Approximately( cropResoution.x, cropResoution.y))
			{
				// We first create a new atlassed material
				var newMaterial = new UMAData.GeneratedMaterial();
				newMaterial.rendererAsset = generatedMaterial.rendererAsset;
				newMaterial.umaMaterial = generatedMaterial.umaMaterial;
				newMaterial.material = UnityEngine.Object.Instantiate(generatedMaterial.umaMaterial.material) as Material;
				newMaterial.material.name = generatedMaterial.umaMaterial.material.name + "_Split_";
				newMaterial.resolutionScale = generatedMaterial.resolutionScale;
						
				// We add the new material to the atlases and the generated materials. This will increase the iterations of the loop intentionally, so the new material will be processed
				atlassedMaterials.Add(newMaterial);	
				generatedMaterials.Add(newMaterial);
						
				// This is not a squared texture and we will need to split it
				float min = Mathf.Min(cropResoution.x, cropResoution.y);

				newMaterial.cropResolution = new Vector2(min, min);

				// Reduce on the X	
				ReduceOnXAxis(generatedMaterial, newMaterial, min);

				// Recalculate rects for the current material
				CalculateRects(generatedMaterial);
			}
		}
		
		private void GenerateAtlasData()
		{
			int initialMaterialCount = atlassedMaterials.Count;
			
			float StartScale = 1.0f / (float)scaleFactor;
			for (int i = 0; i < atlassedMaterials.Count; i++)
			{
				bool splitMaterial = (i >= initialMaterialCount);
				
				var generatedMaterial = atlassedMaterials[i];
				generatedMaterial.materialFragments.Sort(comparer);

				if (!splitMaterial)
				{
					generatedMaterial.resolutionScale = StartScale;
					generatedMaterial.cropResolution = new Vector2(umaGenerator.atlasResolution, umaGenerator.atlasResolution);
				}

				// This loop only makes sense for the UMAGeneratorBase.fitAtlas option, splitting the atlas will be done differently
				while (!CalculateRects(generatedMaterial))
				{
					generatedMaterial.resolutionScale = generatedMaterial.resolutionScale * 0.5f;
				}

				// Splitting the textures into squared textures
				SplitAtlas(generatedMaterial);
				
				UpdateSharedRect(generatedMaterial);
			}
		}

		private void UpdateSharedRect(UMAData.GeneratedMaterial generatedMaterial)
		{
			for (int i = 0; i < generatedMaterial.materialFragments.Count; i++)
			{
				var fragment = generatedMaterial.materialFragments[i];
				if (fragment.isRectShared)
				{
					fragment.atlasRegion = fragment.rectFragment.atlasRegion;
				}
			}
		}

		private bool CalculateRects(UMAData.GeneratedMaterial material)
		{
			Rect nullRect = new Rect(0, 0, 0, 0);
			packTexture.Init(Mathf.RoundToInt(material.cropResolution.x), Mathf.RoundToInt(material.cropResolution.y), false);

			for (int atlasElementIndex = 0; atlasElementIndex < material.materialFragments.Count; atlasElementIndex++)
			{
				var tempMaterialDef = material.materialFragments[atlasElementIndex];
				if (tempMaterialDef.isRectShared)
					continue;
				
				int width = Mathf.FloorToInt(tempMaterialDef.baseOverlay.textureList[0].width * material.resolutionScale * tempMaterialDef.slotData.overlayScale);
				int height = Mathf.FloorToInt(tempMaterialDef.baseOverlay.textureList[0].height * material.resolutionScale * tempMaterialDef.slotData.overlayScale);
				
				// If either width or height are 0 we will end up with nullRect and potentially loop forever
				if (width == 0 || height == 0) 
				{
					tempMaterialDef.atlasRegion = nullRect;
					continue;
				}
				
				tempMaterialDef.atlasRegion = packTexture.Insert(width, height, MaxRectsBinPack.FreeRectChoiceHeuristic.RectBestLongSideFit);

				if (tempMaterialDef.atlasRegion == nullRect)
				{
					if (umaGenerator.fitAtlas)
					{
						if (Debug.isDebugBuild)
							Debug.LogWarning("Atlas resolution is too small, Textures will be reduced.", umaData.gameObject);
						return false;
					}
					else
					{
						if (Debug.isDebugBuild)
							Debug.LogError("Atlas resolution is too small, not all textures will fit.", umaData.gameObject);
					}
				}
			}
			return true;
		}

		private Vector2 CalculateUsedResolution(UMAData.GeneratedMaterial material)
		{
			Vector2 usedArea = new Vector2(0, 0);
			for (int atlasElementIndex = 0; atlasElementIndex < material.materialFragments.Count; atlasElementIndex++)
			{
				if (material.materialFragments[atlasElementIndex].atlasRegion.xMax > usedArea.x)
				{
					usedArea.x = material.materialFragments[atlasElementIndex].atlasRegion.xMax;
				}

				if (material.materialFragments[atlasElementIndex].atlasRegion.yMax > usedArea.y)
				{
					usedArea.y = material.materialFragments[atlasElementIndex].atlasRegion.yMax;
				}
			}

			return usedArea;
		}
		
		private Vector2 CalculateOptimizeAtlasCropResolution(UMAData.GeneratedMaterial material)
		{
			Vector2 usedArea = CalculateUsedResolution(material);
			
			//Headless mode ends up with zero usedArea
			if(Mathf.Approximately( usedArea.x, 0f ) || Mathf.Approximately( usedArea.y, 0f ))
			{
				return Vector2.zero;
			}

			Vector2 tempResolution = new Vector2(umaGenerator.atlasResolution, umaGenerator.atlasResolution);

			bool done = false;
			while (!done && Mathf.Abs(usedArea.x) > 0.0001)
			{
				if (tempResolution.x * 0.5f >= usedArea.x)
				{
					tempResolution = new Vector2(tempResolution.x * 0.5f, tempResolution.y);
				}
				else
				{
					done = true;
				}
			}

			done = false;
			while (!done && Mathf.Abs(usedArea.y) > 0.0001)
			{

				if (tempResolution.y * 0.5f >= usedArea.y)
				{
					tempResolution = new Vector2(tempResolution.x, tempResolution.y * 0.5f);
				}
				else
				{
					done = true;
				}
			}

			return tempResolution;
		}

		private void OptimizeAtlas()
		{
			for (int atlasIndex = 0; atlasIndex < atlassedMaterials.Count; atlasIndex++)
			{
				var material = atlassedMaterials[atlasIndex];

				material.cropResolution = CalculateOptimizeAtlasCropResolution(material);
			}
		}

		private void UpdateUV()
		{
			UMAData.GeneratedMaterials umaAtlasList = umaData.generatedMaterials;

			for (int atlasIndex = 0; atlasIndex < umaAtlasList.materials.Count; atlasIndex++)
			{
				var material = umaAtlasList.materials[atlasIndex];
				if (material.umaMaterial.materialType == UMAMaterial.MaterialType.NoAtlas)
					continue;

				Vector2 finalAtlasAspect = new Vector2(umaGenerator.atlasResolution / material.cropResolution.x, umaGenerator.atlasResolution / material.cropResolution.y);

				for (int atlasElementIndex = 0; atlasElementIndex < material.materialFragments.Count; atlasElementIndex++)
				{
					Rect tempRect = material.materialFragments[atlasElementIndex].atlasRegion;
					tempRect.xMin = tempRect.xMin * finalAtlasAspect.x;
					tempRect.xMax = tempRect.xMax * finalAtlasAspect.x;
					tempRect.yMin = tempRect.yMin * finalAtlasAspect.y;
					tempRect.yMax = tempRect.yMax * finalAtlasAspect.y;
					material.materialFragments[atlasElementIndex].atlasRegion = tempRect;
				}
			}
		}
	}
}
