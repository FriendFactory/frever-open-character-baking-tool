using UnityEngine;
using System;

namespace UMA.PowerTools
{
	public static class OverlayDataAssetOcclusionExtensions
	{
		public static void CleanUp()
		{
			pixels = null;
			pixelsTexture = null;
		}

		public static System.Int32[] GetOcclusion(this OverlayDataAsset asset, int slotNameHash, int subMesh)
		{
			if (subMesh < 0)
				return null;

			var occlusionIndex = asset.GetOcclusionIndex(slotNameHash);
			if (occlusionIndex < 0)
				return null;

			if (asset.OcclusionEntries[occlusionIndex].occlusion == null || asset.OcclusionEntries[occlusionIndex].occlusion.Length <= subMesh)
				return null;

			return asset.OcclusionEntries[occlusionIndex].occlusion[subMesh].occlusion;
		}

		public static void UpdateOcclusion(this OverlayDataAsset asset, SlotDataAsset slot)
		{
			var occlusionIndex = asset.GetOcclusionIndex(slot.nameHash);
			if (occlusionIndex < 0)
			{
				occlusionIndex = asset.OcclusionEntries == null ? 0 : asset.OcclusionEntries.Length;
				Array.Resize(ref asset.OcclusionEntries, occlusionIndex + 1);
#if UNITY_EDITOR
				UnityEditor.EditorUtility.SetDirty(asset);
#endif
				asset.OcclusionEntries[occlusionIndex] = new OverlayDataAsset.OcclusionEntry();
				asset.OcclusionEntries[occlusionIndex].slotNameHash = slot.nameHash;
				asset.SortOcclusion();
				occlusionIndex = asset.GetOcclusionIndex(slot.nameHash);
			}
			var occlusionEntry = asset.OcclusionEntries[occlusionIndex];

			var cutoutMask = asset.textureList[0];
			if (pixels == null || pixelsTexture != cutoutMask)
			{
				var temporaryRT = RenderTexture.GetTemporary(cutoutMask.width, cutoutMask.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
				Graphics.Blit(cutoutMask, temporaryRT);
				RenderTexture.active = temporaryRT;
				var workingBuffer = new Texture2D(cutoutMask.width, cutoutMask.height, TextureFormat.ARGB32, false, true);
				workingBuffer.ReadPixels(new Rect(0, 0, cutoutMask.width, cutoutMask.height), 0, 0, false);
				pixels = workingBuffer.GetPixels32();
				pixelsTexture = cutoutMask;
				RenderTexture.ReleaseTemporary(temporaryRT);
#if UNITY_EDITOR
				UnityEngine.Object.DestroyImmediate(workingBuffer, false);
#else
				UnityEngine.Object.Destroy(workingBuffer);
#endif
				stride = cutoutMask.width;
				uScale = (float)(cutoutMask.width - 1);
				vScale = (float)(cutoutMask.height - 1);
			}
			ProcessSlot(occlusionEntry, slot);
#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(asset);
#endif
		}

		static Color32[] pixels;
		static Texture pixelsTexture;
		static float uScale;
		static float vScale;
		static int stride;

		static int GetOcclusionIndex(this OverlayDataAsset asset, int slotNameHash)
		{
			return Array.BinarySearch(asset.OcclusionEntries, slotNameHash, OverlayDataAsset.OcclusionEntry.OcclusionEntryComparer.Instance);
		}

		private static void ProcessSlot(OverlayDataAsset.OcclusionEntry entry, SlotDataAsset slot)
		{
			bool[] vertexCutout = new bool[slot.meshData.vertexCount];
			for (int i = 0; i < slot.meshData.vertexCount; i++)
			{
				var uv = slot.meshData.uv[i];
				var x = Mathf.RoundToInt(uScale * uv.x);
				var y = Mathf.RoundToInt(vScale * uv.y);
				vertexCutout[i] = pixels[y * stride + x].r == 0;
			}
			Array.Resize(ref entry.occlusion, slot.meshData.subMeshCount);
			for (int i = 0; i < slot.meshData.subMeshCount; i++)
			{
				var triangles = slot.meshData.submeshes[i].triangles;
				var occlussionEntries = (triangles.Length/3 + 31) / 32;

				Array.Resize(ref entry.occlusion[i].occlusion, occlussionEntries);
				ProcessSubMesh(slot.meshData.submeshes[i].triangles, vertexCutout, entry.occlusion[i].occlusion);
			}
		}

		private static void ProcessSubMesh(int[] triangles, bool[] vertexCutout, System.Int32[] occlussion)
		{
			int i = 0;
			uint mask = 0;
			uint modifier = 1;
			int occlusionIndex = 0;
			while (i < triangles.Length)
			{
				var v1 = vertexCutout[triangles[i++]];
				var v2 = vertexCutout[triangles[i++]];
				var v3 = vertexCutout[triangles[i++]];
				if (!(v1 && v2 && v3))
				{
					mask += modifier;
				}
				modifier = modifier << 1;
				if (modifier == 0)
				{
					occlussion[occlusionIndex++] = (System.Int32)mask;
					mask = 0;
					modifier = 1;
				}
			}
			if (modifier != 1)
				occlussion[occlusionIndex++] = (System.Int32)mask;
		}
	}
}