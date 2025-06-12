using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace UMA.PowerTools
{
	[ExecuteInEditMode]
	[System.Serializable]
	public class PowerPackPersistance : MonoBehaviour
	{
#if UNITY_EDITOR
		public List<UMAAvatarBase> loadedAvatars = new List<UMAAvatarBase>();
		[System.NonSerialized]
		private HashSet<UMAAvatarBase> avatarsHashSet = new HashSet<UMAAvatarBase>();

		public void Awake()
		{
			if (loadedAvatars.Count != avatarsHashSet.Count)
			{
				avatarsHashSet.Clear();
				EditorApplication.playmodeStateChanged += new EditorApplication.CallbackFunction(PlayModeChanging);
				avatarsHashSet = new HashSet<UMAAvatarBase>();
				foreach (var avatar in loadedAvatars)
				{
					if (avatar != null)
					{
						avatarsHashSet.Add(avatar);
					}
				}
			}
		}

		public void OnDestroy()
		{
			EditorApplication.playmodeStateChanged -= new EditorApplication.CallbackFunction(PlayModeChanging);
		}

		public static PowerPackPersistance GetInstance()
		{
			var gameObject = GameObject.Find("PowerPackPersistance");
			if (gameObject == null)
			{
				gameObject = new GameObject("PowerPackPersistance", typeof(PowerPackPersistance));
				gameObject.hideFlags = HideFlags.HideInHierarchy;
			}
			return gameObject.GetComponent<PowerPackPersistance>();
		}

		void Update()
		{
			if (UnityEditor.BuildPipeline.isBuildingPlayer)
			{
				if (loadedAvatars.Count > 0)
				{
					Debug.LogError("Design time Uma Avatars, while building.");
				}
			}
		}

		public void ShowAvatar(UMAAvatarBase autoLoaderAvatar)
		{
			ShowAvatar(autoLoaderAvatar, null);
		}

		public void ShowAvatar(UMAAvatarBase autoLoaderAvatar, System.Action<UMAData> callback)
		{
			if (!avatarsHashSet.Contains(autoLoaderAvatar))
			{
				loadedAvatars.Add(autoLoaderAvatar);
				avatarsHashSet.Add(autoLoaderAvatar);
				var editorAvatarGO = new GameObject("UMAEditorAvatar", typeof(UMAEditorAvatar));
				editorAvatarGO.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
				editorAvatarGO.transform.parent = autoLoaderAvatar.transform;
				editorAvatarGO.transform.localPosition = Vector3.zero;
				editorAvatarGO.transform.localRotation = Quaternion.identity;
				UMAEditorAvatar editorAvatar = GetEditorAvatar(autoLoaderAvatar);
				if( editorAvatar == null )
				{
					avatarsHashSet.Remove(autoLoaderAvatar);
					loadedAvatars.Remove(autoLoaderAvatar);
					DestroyImmediate(editorAvatarGO);
					return;
				}
				if (!editorAvatar.Show(autoLoaderAvatar.umaRecipe, autoLoaderAvatar.animationController, callback))
				{
					HideAvatar(autoLoaderAvatar);
				}
			}
		}

		public void HideAvatar(UMAAvatarBase autoLoaderAvatar)
		{
			if (avatarsHashSet.Contains(autoLoaderAvatar))
			{
				loadedAvatars.Remove(autoLoaderAvatar);
				avatarsHashSet.Remove(autoLoaderAvatar);
				UMAEditorAvatar editorAvatar = GetEditorAvatar(autoLoaderAvatar);
				if( editorAvatar != null)
					editorAvatar.Hide();
			}
			else
			{
				UMAEditorAvatar editorAvatar = GetEditorAvatar(autoLoaderAvatar);
				if (editorAvatar != null)
				{
					editorAvatar.Hide();
				}
				else
				{
					DestroyImmediate(autoLoaderAvatar.gameObject);
				}
			}
		}

		public UMAEditorAvatar GetEditorAvatar(UMAAvatarBase autoLoaderAvatar)
		{
			var avatarGO = autoLoaderAvatar.transform.Find("UMAEditorAvatar");
			if (avatarGO != null)
			{
				return avatarGO.GetComponent<UMAEditorAvatar>();
			}
			return null;
		}

		public bool HasAvatar(UMAAvatarBase autoLoaderAvatar)
		{
			return avatarsHashSet.Contains(autoLoaderAvatar);
		}

		void PlayModeChanging()
		{
			HideAll();
			DestroyImmediate(gameObject);
		}

		public void HideAll()
		{
			foreach (var avatar in loadedAvatars)
			{
				if (avatar != null)
				{
					var EditorAvatar = GetEditorAvatar(avatar);
					if (EditorAvatar != null)
					{
						EditorAvatar.Hide();
					}
				}
			}
			avatarsHashSet.Clear();
			loadedAvatars.Clear();
		}

		public void Release()
		{
			if (loadedAvatars.Count == 0)
			{
				DestroyImmediate(gameObject);
			}
		}
#endif
	}
}
