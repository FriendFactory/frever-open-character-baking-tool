using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UMA.PowerTools
{
	[Serializable]
	public class UMALODAnimationControllerSet : ScriptableObject
	{
#if UNITY_EDITOR
		[MenuItem("Assets/Create/UMA LOD Animation Controller Set")]
		static void CreateLODAnimationControllerMenu()
		{
			CustomAssetUtility.CreateAsset<UMALODAnimationControllerSet>();
		}
#endif
	
		[Serializable]
		public class LODAnimationControllerEntry
		{
			public int meshLOD;
			public RuntimeAnimatorController animationController;
		}
	
		public LODAnimationControllerEntry[] Conversions = new LODAnimationControllerEntry[0];
	
		internal RuntimeAnimatorController GetController(int meshLOD)
		{
			RuntimeAnimatorController result = null;
			int bestMatch = int.MinValue;
			foreach (var entry in Conversions)
			{
				if (entry.meshLOD <= meshLOD && entry.meshLOD > bestMatch)
				{
					bestMatch = entry.meshLOD;
					result = entry.animationController;
				}
			}
			return result;
		}
	}
}