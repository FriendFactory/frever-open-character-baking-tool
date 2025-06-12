using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace UMA.PowerTools
{
	/// <summary>
	/// The Bone Baking mesh combiner from the UMA Power Tools third party package.
	/// </summary>
    public class UMABoneBakingMeshCombiner : UMAMeshCombiner
    {
        protected List<Material> combinedMaterialList;
		UMAImprovedSkeleton umaSkeleton;
		Matrix4x4[] inverseResolvedBoneMatrixes;
		MeshBuilder umaMesh;

		public bool dontCacheBoneWeights;

		public int CachedBoneWeights { get { return umaMesh != null ? umaMesh.CachedBoneWeights : 0; } }
		public int CachedBoneWeightEntries { get { return umaMesh != null ? umaMesh.CachedBoneWeightEntries : 0; } }

		UMAData umaData;
        int atlasResolution;
		int animatedBonesCount;
		Dictionary<int, int> mergeBoneDictionary;
		private int mergeBoneDictionaryCapacity;
		private List<Matrix4x4> _inverseResolvedBoneMatrixes;
		SkinnedMeshRenderer myRenderer;

		protected void EnsureUMADataSetup(bool updatedAtlas)
		{
			if (umaData.umaRoot == null)
			{
				GameObject newRoot = new GameObject("Root");
				newRoot.transform.parent = umaData.transform;
				newRoot.transform.localPosition = Vector3.zero;
				newRoot.transform.localRotation = Quaternion.Euler(270f, 0, 0f);
				umaData.umaRoot = newRoot;

				GameObject newGlobal = new GameObject("Global");
				newGlobal.transform.parent = newRoot.transform;
				newGlobal.transform.localPosition = Vector3.zero;
				newGlobal.transform.localRotation = Quaternion.Euler(90f, 90f, 0f);

				umaSkeleton = new UMAImprovedSkeleton(newGlobal.transform);
				umaData.skeleton = umaSkeleton;

				GameObject newSMRGO = new GameObject("UMARenderer");
				//make UMARenderer GO respect the layer setting of the UMAAvatar so cameras can just target this layer
				newSMRGO.layer = umaData.gameObject.layer;
				newSMRGO.transform.parent = umaData.transform;
				newSMRGO.transform.localPosition = Vector3.zero;
				newSMRGO.transform.localRotation = Quaternion.Euler(0, 0, 0f);
				newSMRGO.transform.localScale = Vector3.one;


				myRenderer = newSMRGO.AddComponent<SkinnedMeshRenderer>();
				myRenderer.rootBone = newGlobal.transform;
				myRenderer.sharedMesh = new Mesh();
				umaData.SetRenderers(new SkinnedMeshRenderer[1] { myRenderer });
			}
			else
			{
				myRenderer = umaData.GetRenderer(0);
				if (updatedAtlas)
				{
					umaData.CleanMesh(false);
				}
				umaSkeleton = umaData.skeleton as UMAImprovedSkeleton;
				if (umaSkeleton == null)
				{
					// happens after compile and continue
					umaSkeleton = new UMAImprovedSkeleton(umaData.umaRoot.transform.Find("Global"));
					umaData.skeleton = umaSkeleton;
				}
			}
		}


		public override void Preprocess(UMAData umaData)
		{
			umaData.isMeshDirty |= umaData.isShapeDirty;
		}

		/// <summary>
		/// Updates the UMA mesh and skeleton to match current slots.
		/// </summary>
		/// <param name="updatedAtlas">If set to <c>true</c> atlas has changed.</param>
		/// <param name="umaData">UMA data.</param>
		/// <param name="atlasResolution">Atlas resolution.</param>
        public override void UpdateUMAMesh(bool updatedAtlas, UMAData umaData, int atlasResolution)
        {
			this.umaData = umaData;
            this.atlasResolution = atlasResolution;

            combinedMaterialList = new List<Material>();

			umaData.ResetAnimatedBones();
            var combinedMeshArray = BuildCombineInstances();

			EnsureUMADataSetup(updatedAtlas);
			umaSkeleton.BeginSkeletonUpdate();

			if (umaMesh == null)
			{
				umaMesh = new MeshBuilder();
				umaMesh.cacheBoneWeights = !dontCacheBoneWeights;
			}

			PopulateSkeleton(combinedMeshArray);

			umaData.umaRecipe.ClearDNAConverters();
			for (int i = 0; i < umaData.umaRecipe.slotDataList.Length; i++)
			{
				SlotData slotData = umaData.umaRecipe.slotDataList[i];
				if (slotData != null)
				{
					umaData.umaRecipe.AddDNAUpdater(slotData.asset.slotDNA);
				}
			}
			umaSkeleton.ResetAll();
			AddHumanoidBones();
			MarkAnimatedBones();

			umaData.GotoTPose();

			umaData.ApplyDNA();
			umaData.FireDNAAppliedEvents();

			MergeSkeletons(combinedMeshArray);
			PopulateMatrix(combinedMeshArray);

			SkinnedMeshCombinerRetargeting.CombineMeshes(umaMesh, combinedMeshArray, inverseResolvedBoneMatrixes, umaData.blendShapeSettings);

			RecalculateUV();

	        umaMesh.ApplyDataToUnityMesh(myRenderer, umaSkeleton);
			umaMesh.ReleaseBuffers();
	        umaSkeleton.EndSkeletonUpdate();

	        ApplyBlendShapes();

			myRenderer.quality = SkinQuality.Bone4;
            //umaData.myRenderer.useLightProbes = true;
			if (updatedAtlas)
			{
				var materials = combinedMaterialList.ToArray();
				myRenderer.sharedMaterials = materials;
			}
			//umaData.myRenderer.sharedMesh.RecalculateBounds();
			myRenderer.sharedMesh.name = "UMAMesh";

			umaData.isShapeDirty = false;
            umaData.firstBake = false;

			umaData.umaGenerator.UpdateAvatar(umaData);
			//FireSlotAtlasNotification(umaData, materials);

#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this);
#endif
		}

		private void ApplyBlendShapes()
		{
			var blendShapeSettings = umaData.blendShapeSettings;
			if (blendShapeSettings.ignoreBlendShapes) return;
			var renderers = umaData.GetRenderers();
			foreach(var entry in blendShapeSettings.blendShapes)
			{
				if (!entry.Value.isBaked || blendShapeSettings.loadAllBlendShapes)
				{
					var weight = entry.Value.value * 100f; //Scale up to 1-100 for SetBlendShapeWeight.

					foreach (var renderer in renderers)
					{
						if (renderer == null)
							continue;
						int index = renderer.sharedMesh.GetBlendShapeIndex(name);
						if (index >= 0)
							renderer.SetBlendShapeWeight(index, weight);
					}
				}
			}
		}

		private void AddHumanoidBones()
		{
			var tpose = umaData.umaRecipe.raceData.TPose;
			if (tpose != null)
			{
				tpose.DeSerialize();
				for (int i = 0; i < tpose.humanInfo.Length; i++)
				{
					var bone = tpose.humanInfo[i];
					var hash = UMAUtils.StringToHash(bone.boneName);
					umaData.RegisterAnimatedBone(hash);
				}
			}
		}

		private void MarkAnimatedBones()
		{
			var animatedBones = umaData.GetAnimatedBones();
			animatedBonesCount = animatedBones.Length;
			foreach (var animatedBone in animatedBones)
			{
				umaSkeleton.SetAnimatedBone(animatedBone);
			}
		}

		private void MergeSkeletons(SkinnedMeshCombinerRetargeting.CombineInstance[] combinedInstances)
		{
			if (mergeBoneDictionary != null && mergeBoneDictionaryCapacity < animatedBonesCount)
				mergeBoneDictionary = null;

			if (mergeBoneDictionary == null)
			{
				mergeBoneDictionary = new Dictionary<int, int>(animatedBonesCount);
				mergeBoneDictionaryCapacity = animatedBonesCount;
			}
			var mergedBones = mergeBoneDictionary;
			foreach (var combineInstance in combinedInstances)
			{
				var meshData = combineInstance.meshData;
				combineInstance.targetBoneIndices = new int[meshData.boneNameHashes.Length];
				for (int i = 0; i < meshData.boneNameHashes.Length; i++)
				{
					var targetHash = umaSkeleton.ResolvePreservedHash(meshData.boneNameHashes[i]);
					int targetIndex;
					if (!mergedBones.TryGetValue(targetHash, out targetIndex))
					{
						targetIndex = mergedBones.Count;
						mergedBones.Add(targetHash, targetIndex);
					}
					combineInstance.targetBoneIndices[i] = targetIndex;
					if (!mergedBones.ContainsKey(targetHash))
					{
						mergedBones.Add(targetHash, mergedBones.Count);
					}
				}				
			}
			umaMesh.PrepareBones(mergedBones.Count);
			foreach (var entry in mergedBones)
			{
				umaMesh.boneNameHashes[entry.Value] = entry.Key;
			}
		}


		private void PopulateMatrix(SkinnedMeshCombinerRetargeting.CombineInstance[] combinedInstances)
		{
			foreach (var combineInstance in combinedInstances)
			{
				var meshData = combineInstance.meshData;
				combineInstance.resolvedBoneMatrixes = new Matrix4x4[meshData.boneNameHashes.Length];
				for(int i = 0; i < meshData.boneNameHashes.Length; i++)
				{
					var boneNameHash = meshData.boneNameHashes[i];
					var boneMatrix = umaSkeleton.GetLocalToWorldMatrix(boneNameHash);
					combineInstance.resolvedBoneMatrixes[i] = boneMatrix * meshData.bindPoses[i];
				}
			}

			ListHelper<Matrix4x4>.AllocateArray(ref _inverseResolvedBoneMatrixes, out inverseResolvedBoneMatrixes, umaMesh.bonesCount);
			var rootMatrix = umaSkeleton.GetLocalToWorldMatrix(umaSkeleton.rootBoneHash);
			for (int i = 0; i < umaMesh.bonesCount; i++)
			{
				var boneMatrix = umaSkeleton.GetLocalToWorldMatrix(umaMesh.boneNameHashes[i]);
				umaMesh.bindPoses[i] = boneMatrix.inverse * rootMatrix;
				inverseResolvedBoneMatrixes[i] = (boneMatrix * umaMesh.bindPoses[i]).inverse;
			}
		}

		private void PopulateSkeleton(SkinnedMeshCombinerRetargeting.CombineInstance[] combinedInstances)
		{
			foreach (var combineInstance in combinedInstances)
			{
				var meshData = combineInstance.meshData;
				for (int i = 0; i < meshData.umaBoneCount; i++)
				{
					var umaBone = meshData.umaBones[i];
					if (!umaSkeleton.BoneAddedThisUpdate(umaBone.hash))
					{
						umaSkeleton.AddBone(umaBone);
					}
				}
			}
		}


		//private void FireSlotAtlasNotification(UMAData umaData, Material[] materials)
		//{
		//    for (int atlasIndex = 0; atlasIndex < umaData.atlasList.atlas.Count; atlasIndex++)
		//    {
		//        for (int materialDefinitionIndex = 0; materialDefinitionIndex < umaData.atlasList.atlas[atlasIndex].atlasMaterialDefinitions.Count; materialDefinitionIndex++)
		//        {
		//            var materialDefinition = umaData.atlasList.atlas[atlasIndex].atlasMaterialDefinitions[materialDefinitionIndex];
		//            var slotData = materialDefinition.source.slotData;
		//            if (slotData.SlotAtlassed != null)
		//            {
		//                slotData.SlotAtlassed.Invoke(umaData, slotData, materials[atlasIndex], materialDefinition.atlasRegion);
		//            }
		//        }
		//    }
		//    SlotData[] slots = umaData.umaRecipe.slotDataList;
		//    for (int slotIndex = 0; slotIndex < slots.Length; slotIndex++)
		//    {
		//        var slotData = slots[slotIndex];
		//        if (slotData == null) continue;
		//        if (slotData.textureNameList.Length == 1 && string.IsNullOrEmpty(slotData.textureNameList[0]))
		//        {
		//            if (slotData.SlotAtlassed != null)
		//            {
		//                slotData.SlotAtlassed.Invoke(umaData, slotData, materials[atlasIndex], materialDefinition.atlasRegion);
		//            }
		//        }
		//    }
		//}

        protected SkinnedMeshCombinerRetargeting.CombineInstance[] BuildCombineInstances()
        {
			var combinedMeshList = new List<SkinnedMeshCombinerRetargeting.CombineInstance>();

			SkinnedMeshCombinerRetargeting.CombineInstance combineInstance;

            for (int materialIndex = 0; materialIndex < umaData.generatedMaterials.materials.Count; materialIndex++)
            {
				var generatedMaterial = umaData.generatedMaterials.materials[materialIndex];
				combinedMaterialList.Add(generatedMaterial.material);

				for (int materialDefinitionIndex = 0; materialDefinitionIndex < generatedMaterial.materialFragments.Count; materialDefinitionIndex++)
                {
					var materialDefinition = generatedMaterial.materialFragments[materialDefinitionIndex];
					var slotData = materialDefinition.slotData;
					combineInstance = new SkinnedMeshCombinerRetargeting.CombineInstance();
					combineInstance.meshData = slotData.asset.meshData;
					foreach(var boneHash in slotData.asset.animatedBoneHashes)
					{
						umaData.RegisterAnimatedBone(boneHash);
					}
					combineInstance.targetSubmeshIndices = new int[combineInstance.meshData.subMeshCount];
					for (int i = 0; i < combineInstance.meshData.subMeshCount; i++)
					{
						combineInstance.targetSubmeshIndices[i] = -1;
					}
					combineInstance.targetSubmeshIndices[slotData.asset.subMeshIndex] = materialIndex;
                    combinedMeshList.Add(combineInstance);
					for (int i = 0; i < materialDefinition.overlayData.Length; i++)
					{
						var occlusion = materialDefinition.overlayData[i].asset.GetOcclusion(slotData.asset.nameHash, slotData.asset.subMeshIndex);
						if (occlusion != null)
						{
							if (combineInstance.triangleOcclusion == null)
								combineInstance.triangleOcclusion = new int[combineInstance.meshData.subMeshCount][];
							combineInstance.triangleOcclusion[slotData.asset.subMeshIndex] = occlusion;
						}
					}

					if (slotData.asset.SlotAtlassed != null)
					{
						slotData.asset.SlotAtlassed.Invoke(umaData, slotData, generatedMaterial.material, materialDefinition.atlasRegion);
					}
                }
            }
			return combinedMeshList.ToArray();
        }

		protected void RecalculateUV()
        {
            int idx = 0;
            //Handle Atlassed Verts
            for (int materialIndex = 0; materialIndex < umaData.generatedMaterials.materials.Count; materialIndex++)
            {
				var generatedMaterial = umaData.generatedMaterials.materials[materialIndex];
				if (generatedMaterial.umaMaterial.materialType != UMAMaterial.MaterialType.Atlas)
				{
					var fragment = generatedMaterial.materialFragments[0];
					int vertexCount = fragment.slotData.asset.meshData.vertices.Length;
					idx += vertexCount;
					continue;
				}

				for (int materialDefinitionIndex = 0; materialDefinitionIndex < generatedMaterial.materialFragments.Count; materialDefinitionIndex++)
                {
					var fragment = generatedMaterial.materialFragments[materialDefinitionIndex];
					var tempAtlasRect = fragment.atlasRegion;
					int vertexCount = fragment.slotData.asset.meshData.vertices.Length;
					float atlasXMin = tempAtlasRect.xMin / atlasResolution;
					float atlasXMax = tempAtlasRect.xMax / atlasResolution;
					float atlasXRange = atlasXMax - atlasXMin;
					float atlasYMin = tempAtlasRect.yMin / atlasResolution;
					float atlasYMax = tempAtlasRect.yMax / atlasResolution;
					float atlasYRange = atlasYMax - atlasYMin;

					while (vertexCount-- > 0)
                    {
						umaMesh.uv[idx].x = atlasXMin + atlasXRange * umaMesh.uv[idx].x;
						umaMesh.uv[idx].y = atlasYMin + atlasYRange * umaMesh.uv[idx].y;
						idx++;
                    }
				}
			}
        }
	}
}
