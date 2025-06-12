using UnityEngine;
using System.Collections;
using UnityEditor;

namespace UMA.PowerTools
{
	public class DeleteHiddenRecipeToolsObjects : MonoBehaviour
	{
		[MenuItem("UMA/Power Tools/Find And Destroy Hidden Preview Objects")]
		static void FindAndDestroyHiddenRecipeToolsObjects()
		{
			// first delete all known avatars.
			var instance = PowerPackPersistance.GetInstance();
			instance.HideAll();
			instance.Release();

			// then find any leaks.
			var allEditorAvatars = FindObjectsOfType<UMAEditorAvatar>();
			foreach (var avatar in allEditorAvatars)
			{
				Debug.Log("Destroy leaked preview: " + avatar.name);
				avatar.Hide();
			}
		}
	}
}
