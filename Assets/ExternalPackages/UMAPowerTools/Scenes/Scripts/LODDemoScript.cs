using UnityEngine;
using System.Collections.Generic;
using System;
using System.Text;

namespace UMA.PowerTools.Demo
{
	public class LODDemoScript : MonoBehaviour
	{
		public UMAContext context;
		public UMARecipeBase[] recipes = new UMARecipeBase[0];
		public UMALODAnimationControllerSet animationControllerSet;
		public RuntimeAnimatorController animatorController;
		public UMA.UMAGeneratorBase generator;
		public UMA.UMAGeneratorBase otherGenerator;
		public UMALODController controller;
		public bool showMiniCamera = true;
		private string characterNumberString = "";
		private string charNumber = "";
		public MonoBehaviour cameraController;
		public GameObject waitGO;
		public LODDemoCluster[] clusters;
		public float beginTime;
		public UnityEngine.UI.Text generatorText;
		public UnityEngine.UI.Text charNumberText;
		public UnityEngine.UI.Text charNumberStringText;
		private bool stringsDirty;
		public int characterBuilds;


		// Use this for initialization
		void Start()
		{
			controller.onAvatarChanged += new UMALODController.AvatarChanged(controller_onAvatarChanged);
			BuildGUIStrings();
			beginTime = Time.realtimeSinceStartup;
			characterBuilds = 0;
			randomSeed = UnityEngine.Random.seed;
		}

		void controller_onAvatarChanged(UMALODAvatar avatar, UMALODController.AutoLoaderNotification message)
		{
			stringsDirty = true;
			characterBuilds++;
		}

		public void DestroyCharacter(UMALODAvatar autoLoader)
		{
			controller.UnRegisterAutoLoaderAvatar(autoLoader);
			autoLoader.Hide();
			UMAUtils.DestroySceneObject(autoLoader.gameObject);
		}

		public void More()
		{
			if (!controller.CreatePaused && controller.IsIdle)
			{
				controller.CreatePaused = true;
                if( cameraController != null )
                    cameraController.enabled = false;
                if( waitGO != null )
                    waitGO.SetActive(true);
				controller.RunPerCharacter(DestroyCharacter, UMALODController.LODFilter.visible);
				randomSeed = UnityEngine.Random.seed;
				foreach (var cluster in clusters)
				{
					cluster.characters += 5;
					cluster.SpawnCharacters();
				}
				beginTime = Time.realtimeSinceStartup;
				//if (generator is UMAGeneratorThreaded) (generator as UMAGeneratorThreaded).ticks *= 4;
			}
		}

		public void Less()
		{
			if (!controller.CreatePaused && controller.IsIdle)
			{
				controller.CreatePaused = true;
                if( cameraController != null )
               		cameraController.enabled = false;
                if( waitGO != null )
                    waitGO.SetActive(true);
				controller.RunPerCharacter(DestroyCharacter, UMALODController.LODFilter.visible);
				randomSeed = UnityEngine.Random.seed;
				foreach (var cluster in clusters)
				{
					cluster.characters -= 5;
					cluster.SpawnCharacters();
				}
				beginTime = Time.realtimeSinceStartup;
				//if (generator is UMAGeneratorThreaded) (generator as UMAGeneratorThreaded).ticks *= 4;
			}
		}


		public int randomSeed;
		public void ToggleGenerator()
		{
			if (!controller.CreatePaused && controller.IsIdle)
			{
				UnityEngine.Random.seed = randomSeed;

				var swap = generator;
				generator = otherGenerator;
				otherGenerator = swap;
				otherGenerator.gameObject.SetActive(false);
				generator.gameObject.SetActive(true);
				controller.umaGenerator = generator;

				controller.CreatePaused = true;
                if( cameraController != null )
                    cameraController.enabled = false;
                if( waitGO != null )
                    waitGO.SetActive(true);
				controller.RunPerCharacter(DestroyCharacter, UMALODController.LODFilter.visible);
				foreach (var cluster in clusters)
				{
					cluster.SpawnCharacters();
				}
				beginTime = Time.realtimeSinceStartup;
                if( generatorText != null )
				{
					if ((generator as UMAGeneratorBuiltin).meshCombiner is UMADefaultMeshCombiner)
					{
						generatorText.text = "UMA 2 (Default)";
					}
					else
					{
						generatorText.text = "UMA Bone Baking (Power Tools)";
					}
				}
			}
		}


		private void InitilizationDone()
		{
			Debug.LogFormat("Generation Time: {0} seconds", Time.realtimeSinceStartup - beginTime);
			controller.CreatePaused = false;
			controller.ActivateAll();
			BuildGUIStrings();
			if( cameraController != null )
                cameraController.enabled = true;
			if( waitGO != null )
                waitGO.SetActive(false);
			//if (generator is UMAGeneratorThreaded) (generator as UMAGeneratorThreaded).ticks /= 4;
		}


		StringBuilder sb = new StringBuilder();
		private void BuildGUIStrings()
		{
			sb.Length = 0;
			int totalVisible = 0;
			for (int i = 0; i < controller.LOD.Length; i++)
			{
				var layer = controller.LOD[i];
				var visible = layer.completeCount;
				var dirty = layer.incompleteCount + layer.renderingCount;
				totalVisible += visible;
				totalVisible += dirty;
				sb.AppendFormat("LOD Layer {0} - {1} finished characters - {3} dirty characters - {2} missing characters\n", i, visible, layer.inVisibleCount, dirty);
			}
			characterNumberString = sb.ToString();
			if (controller.CreatePaused)
			{
				charNumber = string.Format("Total built {0}", totalVisible);
			}
			else
			{
				charNumber = string.Format("Total visible {0}, Total Out of Range {1}", totalVisible, controller.outOfRangeCount);
			}
            if( charNumberStringText != null )
                charNumberStringText.text = characterNumberString;
            if( charNumberText != null )
                charNumberText.text = charNumber;
		}

		void Update()
		{
			if (stringsDirty)
			{
				BuildGUIStrings();
				stringsDirty = false;
			}

			if (controller.CreatePaused)
			{
				if (controller.IsIdle)
				{
					InitilizationDone();
				}
				return;
			}

			if (Input.GetKeyDown(KeyCode.PageUp))
			{
				More();
			}

			if (Input.GetKeyDown(KeyCode.PageDown))
			{
				Less();
			}
			if (Input.GetKey(KeyCode.F1))
			{
				ToggleGenerator();
			}
		}

		private void UnLoadNPCs()
		{
			stringsDirty = true;
			controller.RebuildAll();
		}


		private void RebuildNPCs()
		{
			stringsDirty = true;
			throw new NotImplementedException();
			//controller.DestroyAllAutoLoaderAvatars();
			//for (int i = 0; i < characters; i++)
			//{
			//	var lucy = GameObject.Instantiate(npcPrefab) as GameObject;
			//	var loader = lucy.GetComponent<AutoLoaderAvatar>();
			//	loader.umaData = lucy.GetComponent<UMA.UMAData>();
			//	loader.umaData.umaRecipe = new UMA.UMAData.UMARecipe();
			//	loader.context = context;
			//	loader.umaRecipe = recipes[Random.Range(0, recipes.Length)];
			//	loader.transform.position = new Vector3(Random.Range(-8f, 8f), 0f, Random.Range(-8f, 8f));
			//	loader.animationController = animationController;
			//	loader.umaGenerator = generator;
			//	controller.RegisterAutoLoaderAvatar(loader);
			//}
		}
	}
}
