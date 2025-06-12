using UnityEngine;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Collections;
using System.Collections.Generic;
using UMA;
using UnityEngine.Events;
#if UNITY_5_5_OR_NEWER
using UnityEngine.Profiling;
#endif

namespace UMA.PowerTools
{
	public class UMALODRecipeAvatar : UMAAvatarBase
	{
		[NonSerialized]
		public GameObject oldChild;
		public UMALODManager lodManager;
		private UMAData.UMARecipe originalRecipe;

		private int loadingMeshLOD;
		private float loadingAtlasResolutionScale;

		private int _currentMeshLOD = int.MinValue;
		public int CurrentMeshLOD { get { return _currentMeshLOD; } private set { _currentMeshLOD = value; } }
		private float _currentAtlasResolutionScale = 1f;
		public float CurrentAtlasResolutionScale { get { return _currentAtlasResolutionScale; } private set { _currentAtlasResolutionScale = value; } }

		public override void Load(UMARecipeBase umaRecipe, params UMARecipeBase[] umaAdditionalRecipes)
		{
			if (umaRecipe == null) return;
			Profiler.BeginSample("Load");
			umaRecipe.Load(umaData.umaRecipe, context);
			umaData.AddAdditionalRecipes(umaAdditionalRecipes, context);
			RecipeUpdated();
			Show(0, 1f);
			Profiler.EndSample();
		}

		public void RecipeUpdated()
		{
			originalRecipe = umaData.umaRecipe;
			umaData.umaRecipe = originalRecipe.Mirror();
			umaData.umaRecipe.slotDataList = null;
			CurrentMeshLOD = -1;
		}

		public override void Hide()
		{
			base.Hide();
			CurrentMeshLOD = -1;
		}

		public override void Show()
		{
			if (umaRecipe != null)
			{
				Load(umaRecipe);
			}
			else
			{
				RecipeUpdated();
				Show(0, 1f);
			}
		}

		public void Show(int meshLOD, float atlasScale)
		{
			if (CurrentMeshLOD != meshLOD)
			{
				if (CurrentMeshLOD == int.MinValue)
				{
					if (umaData.umaRecipe == null)
					{
						umaData.umaRecipe = new UMAData.UMARecipe();
					}
					umaRecipe.Load(umaData.umaRecipe, context);
					umaData.AddAdditionalRecipes(umaAdditionalRecipes, context);
					RecipeUpdated();
				}
				else
				{
					umaData.umaRecipe = originalRecipe.Mirror();
					umaData.umaRecipe.slotDataList = null;
				}			

				lodManager.ProcessUMARecipe(originalRecipe, umaData.umaRecipe, meshLOD);
				umaData.CharacterUpdated.AddListener(new UnityAction<UMAData>(umaData_OnCharacterUpdated));

				umaData.atlasResolutionScale = atlasScale;
				loadingMeshLOD = meshLOD;
				UpdateNewRace();
			}
			else
			{
				if (CurrentAtlasResolutionScale != atlasScale)
				{
					umaData.CharacterUpdated.AddListener(new UnityAction<UMAData>(umaData_OnCharacterUpdated));

					loadingAtlasResolutionScale = atlasScale;
					umaData.atlasResolutionScale = atlasScale;
					umaData.Dirty(false, true, false);
				}
				else
				{
					umaData.CharacterUpdated.AddListener(new UnityAction<UMAData>(umaData_OnCharacterUpdated));

					loadingAtlasResolutionScale = atlasScale;
					umaData.atlasResolutionScale = atlasScale;
					umaData.Dirty(false, true, false);
				}
			}
		}

		void umaData_OnCharacterUpdated(UMAData obj)
		{
			CurrentAtlasResolutionScale = loadingAtlasResolutionScale;
			CurrentMeshLOD = loadingMeshLOD;
			umaData.CharacterUpdated.RemoveListener(new UnityAction<UMAData>(umaData_OnCharacterUpdated));
		}
	}
}
