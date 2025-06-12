using UnityEngine;
using System.Collections;
using UnityEngine.Events;

namespace UMA.PowerTools.Demo
{
	public class LODDemoCluster : MonoBehaviour
	{
		public float range;
		public int characters;
		public LODDemoScript demoScript;

		void Start()
		{
			SpawnCharacters();
		}

		public void SpawnCharacters()
		{
			float pi2 = Mathf.PI * 2f;
			float angleDelta = pi2 / characters;
			float angle = 0;
			var thisTransform = GetComponent<Transform>();
			for (int i = 0; i < characters; i++)
			{
				Vector3 position = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * (range * Random.Range(0.1f, 1f));
				var go = new GameObject(i.ToString());
				var goTransform = go.GetComponent<Transform>();
				goTransform.parent = thisTransform;
				goTransform.localPosition = position;
				goTransform.localRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
				var avatar = go.AddComponent<UMALODAvatar>();
				avatar.umaRecipe = demoScript.recipes[Random.Range(0, demoScript.recipes.Length)];
				avatar.LODAnimationController = demoScript.animationControllerSet;
				avatar.animationController = demoScript.animatorController;
				avatar.context = demoScript.context;
				avatar.CharacterUpdated = new UMADataEvent();
				avatar.CharacterUpdated.AddListener(new UnityAction<UMAData>(onCharacterUpdated));
				avatar.umaGenerator = demoScript.generator;
				avatar.lodManager = demoScript.controller.lodManager;
				avatar.Initialize();
				if (demoScript.controller != null && demoScript.controller.Initialized)
				{
					demoScript.controller.RegisterAutoLoaderAvatar(avatar);
				}
				angle += angleDelta;
			}
		}

		private void onCharacterUpdated(UMAData umaData)
		{
		}

		void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.blue;
			Gizmos.DrawWireSphere(GetComponent<Transform>().position, range);
		}
	}
}