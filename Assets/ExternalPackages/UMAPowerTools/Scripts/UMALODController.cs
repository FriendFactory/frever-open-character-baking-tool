using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Events;


namespace UMA.PowerTools
{
	public class UMALODController : MonoBehaviour
	{
		public UMAGeneratorBase umaGenerator;
		public UMALODManager lodManager;
		public bool CreatePaused;

		[Serializable]
		public class LODEntry
		{
			public int MeshLOD;
			public float atlasResolutionScale;
			public float checkInterval;
			public float enterRange;
			public float leaveRange;
			public SkinQuality skinQuality;
			public bool disableAnimations;
			public int index;

			#region non serialized fields
			private LinkedList<UMALODAvatar> incomplete = new LinkedList<UMALODAvatar>();
			private LinkedList<UMALODAvatar> complete = new LinkedList<UMALODAvatar>();
			private LinkedList<UMALODAvatar> invisible = new LinkedList<UMALODAvatar>();
			private LinkedList<UMALODAvatar> AtGenerator = new LinkedList<UMALODAvatar>();
			[NonSerialized]
			public LODEntry higherLOD;
			[NonSerialized]
			public LODEntry lowerLOD;
			private float lastCheck = -1;
			[NonSerialized]
			public float enterRangeSquared;
			[NonSerialized]
			public float leaveRangeSquared;
			[NonSerialized]
			public UMALODController controller;
			#endregion

			public void PrepareLayer(LODEntry higherLOD, LODEntry lowerLOD, UMALODController controller, int index)
			{
				this.lowerLOD = lowerLOD;
				this.higherLOD = higherLOD;
				enterRangeSquared = enterRange * enterRange;
				leaveRangeSquared = leaveRange * leaveRange;
				this.controller = controller;
				this.index = index;
			}
			public void Enter(UMALODAvatar autoLoader)
			{
				if (autoLoader.CurrentMeshLOD == MeshLOD)
				{
					if (autoLoader.node.List != null)
					{
						autoLoader.node.List.Remove(autoLoader.node);
					}
					UpdateAutoLoader(autoLoader);
					return;
				}
				if (autoLoader.CurrentMeshLOD == int.MinValue)
				{
					invisible.AddLast(autoLoader.node);
				}
				else
				{
					incomplete.AddLast(autoLoader.node);
				}
			}

			private void UpdateAutoLoader(UMALODAvatar autoLoader)
			{
				AtGenerator.AddLast(autoLoader.node);
				autoLoader.Show(this);
				controller.SetupEvents(autoLoader);
			}
		
			internal void Update()
			{
				if (lastCheck + checkInterval < Time.time)
				{
					lastCheck = Time.time;
					UpdateList(invisible);
					UpdateList(incomplete);
					UpdateList(complete);
				}
			}

			private void UpdateList(LinkedList<UMALODAvatar> list)
			{
				var iterator = list.First;
				while (iterator != null)
				{
					var distance = controller.NearestRangeSquared(iterator.Value);
					if (distance > leaveRangeSquared)
					{
						// move it to lower quality LOD
						var autoLoaderNode = iterator;
						iterator = iterator.Next;
						list.Remove(autoLoaderNode);
						var nextLOD = lowerLOD;
						while (nextLOD != null)
						{
							if (distance < nextLOD.leaveRangeSquared)
							{
								nextLOD.Enter(autoLoaderNode.Value);
								break;
							}
							nextLOD = nextLOD.lowerLOD;
						}
						if (nextLOD == null)
						{

							controller.OutOfRange.AddLast(autoLoaderNode);
							autoLoaderNode.Value.Hide();
						}
						// iterator is pointing to next element in list... loop on.
					}
					else if (higherLOD != null && distance < higherLOD.enterRangeSquared)
					{
						// move it to higher quality LOD
						var autoLoaderNode = iterator;
						iterator = iterator.Next;
						list.Remove(autoLoaderNode);
						var nextLOD = higherLOD;
						while (nextLOD.higherLOD != null && distance < nextLOD.higherLOD.enterRangeSquared)
						{
							nextLOD = nextLOD.higherLOD;
						}
						nextLOD.Enter(autoLoaderNode.Value);
						// iterator is pointing to next element in list... loop on.
					}
					else
					{
						// LOD on target, move to next.
						iterator = iterator.Next;
					}
				}
			}

			public void OnCharacterUpdated(UMALODAvatar autoloader)
			{
				for (int i = 0; i < autoloader.umaData.rendererCount; i++)
					autoloader.umaData.GetRenderer(i).quality = skinQuality;
				autoloader.umaData.animator.enabled = true;
				if (disableAnimations)
				{
					autoloader.umaData.animator.Update(0f);
					autoloader.umaData.animator.enabled = false;
				}
				if (autoloader.node.List == AtGenerator)
				{
					AtGenerator.Remove(autoloader.node);
					complete.AddLast(autoloader.node);
				}
				else
				{
					throw new NotImplementedException();
				}
			}

			internal void UnRegisterAutoLoaderAvatar(UMALODAvatar autoLoader)
			{
				if (autoLoader.node.List != AtGenerator)
				{
					autoLoader.node.List.Remove(autoLoader.node);
					if (autoLoader.IsShown) autoLoader.Hide();
				}
			}

			internal void FillQueueInvisible()
			{
				if (invisible.Count == 0) return;
				FillQueue(invisible);
			}

			internal void FillQueueIncomplete()
			{
				if (incomplete.Count == 0) return;
				FillQueue(incomplete);
			}

			private void FillQueue(LinkedList<UMALODAvatar> list)
			{
				int freeSpots = controller.QueueSpotsFree;
				int characters = list.Count;
				if (characters > freeSpots) characters = freeSpots;

				do
				{
					var iterator = list.First;
					list.Remove(iterator);
					UpdateAutoLoader(iterator.Value);
					controller.RegisterPriority(iterator.Value);
					iterator = iterator.Next;
					characters--;
				} while (characters > 0);
			}

			internal void DebugStuff()
			{
				Debug.Log("Layer Enter Range^2: " + enterRangeSquared);
				Debug.Log("Layer Invisible: " + invisible.Count);
			}

			public int renderingCount { get { return AtGenerator.Count; } }
			public int incompleteCount { get { return incomplete.Count; } }
			public int completeCount { get { return complete.Count; } }
			public int inVisibleCount { get { return invisible.Count; } }

			internal void RebuildAll()
			{
				while (complete.First != null)
				{
					var value =complete.First;
					complete.Remove(value);
					value.Value.Hide();
					invisible.AddLast(value);
				}
				while (incomplete.First != null)
				{
					var value = incomplete.First;
					incomplete.Remove(value);
					value.Value.Hide();
					invisible.AddLast(value);
				}
			}

			public void RunPerCharacter(Action<UMALODAvatar> action, LODFilter filter)
			{
				if ((filter & LODFilter.complete) != 0)
				{
					RunPerCharacter(action, complete);
				}
				if ((filter & LODFilter.generating) != 0)
				{
					RunPerCharacter(action, AtGenerator);
				}
				if ((filter & LODFilter.incomplete) != 0)
				{
					RunPerCharacter(action, incomplete);
				}
				if ((filter & LODFilter.hidden) != 0)
				{
					RunPerCharacter(action, invisible);
				}				
			}

			private void RunPerCharacter(Action<UMALODAvatar> action, LinkedList<UMALODAvatar> list)
			{
				var iterator = list.First;
				while (iterator != null)
				{
					var autoloader = iterator.Value;
					iterator = iterator.Next;
					action(autoloader);
				}
			}
		}

		public LODEntry[] LOD = new LODEntry[0];
		public Transform[] pointsOfInterest;
		private LODEntry lastLOD;
		public int GeneratorQueueCount;
		private int QueueSpotsFree;
		public bool debugCounters;
		public bool IsIdle { get { return QueueSpotsFree == GeneratorQueueCount; } }
		public float checkInterval;
		private float lastCheck;

		[NonSerialized]
		public bool Initialized = false;

		private UMALODAvatar[] prioritizedList;
	
		private LinkedList<UMALODAvatar> OutOfRange = new LinkedList<UMALODAvatar>();

		public enum AutoLoaderNotification { Showing, Show, Hide, ChangedLOD }
		public delegate void AvatarChanged(UMALODAvatar avatar, AutoLoaderNotification message);
		public event AvatarChanged onAvatarChanged;
		void Start()
		{
			Initialized = true;
			if (lodManager == null) lodManager = GetComponent<UMALODManager>();
			if (LOD.Length == 0)
			{
				LOD = new LODEntry[]
				{
					new LODEntry()
				};
			}
			if (pointsOfInterest == null || pointsOfInterest.Length == 0)
			{
				pointsOfInterest = new Transform[] { GetComponent<Transform>() };
			}
			ReOrganizeLODLayers();

			prioritizedList = new UMALODAvatar[GeneratorQueueCount];
			QueueSpotsFree = GeneratorQueueCount;
		}

		public void ReOrganizeLODLayers()
		{
			lastLOD = LOD[LOD.Length-1];
			if (LOD.Length > 1)
			{
				LOD[0].PrepareLayer(null, LOD[1], this, 0);
				LOD[LOD.Length - 1].PrepareLayer(LOD[LOD.Length - 2], null, this, LOD.Length - 1);
				for (int i = 1; i < LOD.Length - 1; i++)
				{
					LOD[i].PrepareLayer(LOD[i - 1], LOD[i + 1], this, i);
				}
			}
			else if (LOD.Length == 1)
			{
				LOD[0].PrepareLayer(null, null, this, 0);
			}
		}

		public void RegisterAutoLoaderAvatar(UMALODAvatar autoLoader)
		{
			autoLoader.lodManager = lodManager;
			float actualRange = NearestRangeSquared(autoLoader);
			if (actualRange < lastLOD.enterRangeSquared)
			{
				for (int i = 0; i < LOD.Length; i++)
				{
					if (actualRange < LOD[i].enterRangeSquared)
					{
						LOD[i].Enter(autoLoader);
						return;
					}
				}
			}
			OutOfRange.AddLast(autoLoader.node);
		}

		public void UnRegisterAutoLoaderAvatar(UMALODAvatar autoLoader)
		{
			if (autoLoader.lodLevel != null)
			{
				autoLoader.lodLevel.UnRegisterAutoLoaderAvatar(autoLoader);
			}
			else
			{
				autoLoader.node.List.Remove(autoLoader.node);
			}
		}

		public float NearestRangeSquared(UMALODAvatar autoLoader)
		{
			float nearestDist2 = float.MaxValue;
			foreach (var poi in pointsOfInterest)
			{
				float dist2 = (poi.position - autoLoader.transform.position).sqrMagnitude;
				if (dist2 < nearestDist2)
				{
					nearestDist2 = dist2;
				}
			}
			return nearestDist2 * autoLoader.rangeMultiplierSquared;
		}
		bool IsInRange(UMALODAvatar autoLoader, float rangeSquared, out float actualSquaredRange)
		{
			float nearestDist2 = float.MaxValue;
			foreach(var poi in pointsOfInterest)
			{
				float dist2 = (poi.position - autoLoader.transform.position).sqrMagnitude;
				if( dist2 < nearestDist2 )
				{
					nearestDist2 = dist2;
				}
			}
			actualSquaredRange = nearestDist2 * autoLoader.rangeMultiplierSquared;
			return rangeSquared > actualSquaredRange;
		}

		public bool pauseUpdates = false;
		void Update()
		{
			//if (CreatePaused)
			//{
			//	if (!pauseUpdates && QueueSpotsFree != 0)
			//	{
			//		FillQueue();
			//	}
			//	return;
			//}
			if (debugCounters)
			{
				debugCounters = false;
				foreach (var layer in LOD)
				{
					layer.DebugStuff();
				}
			}
			foreach(var layer in LOD)
			{
				layer.Update();
			}

			if (lastCheck + checkInterval < Time.time)
			{
				lastCheck = Time.time;
				UpdateList(OutOfRange);
			}

			if (!pauseUpdates && QueueSpotsFree != 0)
			{
				FillQueue();
			}
		}

		private void UpdateList(LinkedList<UMALODAvatar> list)
		{
			var iterator = list.First;
			while (iterator != null)
			{
				var distance = NearestRangeSquared(iterator.Value);
				if (distance < lastLOD.enterRangeSquared)
				{
					var autoLoaderNode = iterator;
					iterator = iterator.Next;
					list.Remove(autoLoaderNode);
					var nextLOD = lastLOD;
					while (nextLOD.higherLOD != null && distance < nextLOD.higherLOD.enterRangeSquared)
					{
						nextLOD = nextLOD.higherLOD;
					}
					nextLOD.Enter(autoLoaderNode.Value);
					// iterator is pointing to next element in list... loop on.
				}
				else
				{
					iterator = iterator.Next;
				}
			}
		}

		private int fillQueuePosition;
		private void FillQueue()
		{
			fillQueuePosition = 0;
			foreach(var layer in LOD)
			{
				layer.FillQueueInvisible();
				if (QueueSpotsFree == 0) return;
				layer.FillQueueIncomplete();
				if (QueueSpotsFree == 0) return;
			}
		}

		internal void RegisterPriority(UMALODAvatar autoLoaderAvatar)
		{
			while (prioritizedList[fillQueuePosition] != null)
			{
				fillQueuePosition++;
			}
			autoLoaderAvatar.priorityPosition = fillQueuePosition;
			prioritizedList[fillQueuePosition] = autoLoaderAvatar;
			fillQueuePosition++;
			QueueSpotsFree--;
		}

		internal void SetupEvents(UMALODAvatar autoLoader)
		{
			autoLoader.priorityPosition = -1;
			autoLoader.umaData.CharacterUpdated.AddListener(new UnityAction<UMAData>(umaData_OnUpdated));
		}

		void umaData_OnUpdated(UMAData umaData)
		{
			umaData.CharacterUpdated.RemoveListener(new UnityAction<UMAData>(umaData_OnUpdated));
			var autoloader = umaData.GetComponent<UMALODAvatar>();
			autoloader.lodLevel.OnCharacterUpdated(autoloader);
			if (autoloader.priorityPosition >= 0)
			{
				prioritizedList[autoloader.priorityPosition] = null;
				autoloader.priorityPosition = -1;
				QueueSpotsFree++;
			}
			SendAvatarChanged(autoloader, AutoLoaderNotification.Show);
			if (CreatePaused) umaData.gameObject.SetActive(false);
		}

		public enum LODFilter
		{
			complete = 1,
			incomplete = 2,
			hidden = 4,
			generating = 8,
			visible = 3,
			all = 15
		};
		public void RunPerCharacter(Action<UMALODAvatar> action, LODFilter filter)
		{
			foreach (var layer in LOD)
			{
				layer.RunPerCharacter(action, filter);
			}
		}

		private void ActivateCharacter(UMALODAvatar umaData)
		{
			umaData.gameObject.SetActive(true);
		}

		public void ActivateAll()
		{
			RunPerCharacter(ActivateCharacter, LODFilter.visible);
		}

		public bool ShowGizmosWhenNotSelected;

		void OnDrawGizmos()
		{
			if (ShowGizmosWhenNotSelected)
			{
				foreach (var layer in LOD)
				{
					foreach(var poi in pointsOfInterest)
					{
						Gizmos.color = Color.green;
						Gizmos.DrawWireSphere(poi.position, layer.enterRange);
						Gizmos.color = Color.red;
						Gizmos.DrawWireSphere(poi.position, layer.leaveRange);
					}
				}
			}
		}

		void OnDrawGizmosSelected()
		{
			if (!ShowGizmosWhenNotSelected)
			{
				foreach (var layer in LOD)
				{
					foreach (var poi in pointsOfInterest)
					{
						Gizmos.color = Color.green;
						Gizmos.DrawWireSphere(poi.position, layer.enterRange);
						Gizmos.color = Color.red;
						Gizmos.DrawWireSphere(poi.position, layer.leaveRange);
					}
				}
			}
		}



		/* Old Stuff

 
				private void SendToGenerator()
				{
					if (activeAtGenerator == null && showing.Count > 0)
					{
						activeAtGenerator = showing.First;
						activeAtGenerator.Value.umaGenerator = umaGenerator;
						activeAtGenerator.Value.Show();
						DestroyObject(activeAtGenerator.Value.umaData.umaRoot.GetComponent<Locomotion>());
						if (!activeAtGenerator.Value.autoLoaderNotificationRegistered)
						{
							activeAtGenerator.Value.umaData.OnCharacterUpdated += new System.Action<UMAData>(umaData_OnUpdated);
							activeAtGenerator.Value.autoLoaderNotificationRegistered = true;
						}
						var rootMotion = activeAtGenerator.Value.umaData.umaRoot.AddComponent<RootMotionHandler>();
						rootMotion.rootMotionReceiver = activeAtGenerator.Value.transform;
						showing.RemoveFirst();
					}
					if (queuedAtGenerator == null && showing.Count > 0)
					{
						queuedAtGenerator = showing.First;
						queuedAtGenerator.Value.umaGenerator = umaGenerator;
						queuedAtGenerator.Value.Show();
						DestroyObject(queuedAtGenerator.Value.umaData.umaRoot.GetComponent<Locomotion>());
						if (!queuedAtGenerator.Value.autoLoaderNotificationRegistered)
						{
							queuedAtGenerator.Value.umaData.OnCharacterUpdated += new System.Action<UMAData>(umaData_OnUpdated);
							queuedAtGenerator.Value.autoLoaderNotificationRegistered = true;
						}
						var rootMotion = queuedAtGenerator.Value.umaData.umaRoot.AddComponent<RootMotionHandler>();
						rootMotion.rootMotionReceiver = queuedAtGenerator.Value.transform;
						showing.RemoveFirst();
					}
				} 

				void umaData_OnUpdated(UMAData obj)
				{
					var autoloader = obj.GetComponent<AutoLoaderAvatar>();

					obj.umaRoot.GetComponent<RootMotionHandler>().animator = obj.animator;
					if (queuedAtGenerator != null && queuedAtGenerator.Value.umaData == obj)
					{
						HandleShown(queuedAtGenerator);
						queuedAtGenerator = null;
					}
					else if (activeAtGenerator != null && activeAtGenerator.Value.umaData == obj)
					{
						HandleShown(activeAtGenerator);
						activeAtGenerator = queuedAtGenerator;
						queuedAtGenerator = null;
					}
					else
					{
						if (activeAtGenerator == null)
						{
							activeAtGenerator = queuedAtGenerator;
							queuedAtGenerator = null;
						}
						if (obj.gameObject == null)
						{
							Debug.LogError("Null umadata updated.", obj.gameObject);
						}
						Debug.LogError("Unknown umadata updated.", obj.gameObject);
						//Destroy(obj.gameObject);
					}
					var avatar = obj.GetComponent<AutoLoaderAvatar>();
					SendAvatarChanged(avatar, AutoLoaderNotification.Show);
					SendToGenerator();
				}

				private void HandleShown(LinkedListNode<AutoLoaderAvatar> avatar)
				{
					avatar.Value.umaData.OnCharacterUpdated -= new System.Action<UMAData>(umaData_OnUpdated);
					if (UsesLOD)
					{
						if (avatar.Value.node.List != null)
						{
							avatar.Value.node.List.Remove(avatar);
						}
						LOD[avatar.Value.MeshLOD].Enter(this, avatar.Value);
					}				

					if (avatar.Value.umaGenerator == umaGenerator)
					{
						visible.AddLast(avatar);
					}
					else
					{
						avatar.Value.Hide();
						hidden.AddLast(avatar);
						SendAvatarChanged(avatar.Value, AutoLoaderNotification.Hide);
					}
				}

				private void UpdateLists()
				{
					LinkedListNode<AutoLoaderAvatar> avatars = visible.First;
					while (avatars != null)
					{
						var next = avatars.Next;
						if (!IsInRange(avatars.Value, destroyRange))
						{
							avatars.Value.Hide();
							visible.Remove(avatars);
							hidden.AddLast(avatars);
							SendAvatarChanged(avatars.Value, AutoLoaderNotification.Hide);
						}
						avatars = next;
					}

					avatars = hidden.First;
					while (avatars != null)
					{
						var next = avatars.Next;
						if (IsInRange(avatars.Value, spawnRange))
						{
							hidden.Remove(avatars);
							showing.AddLast(avatars);
							SendAvatarChanged(avatars.Value, AutoLoaderNotification.Showing);
						}
						avatars = next;
					}

					avatars = showing.First;
					while (avatars != null)
					{
						var next = avatars.Next;
						if (!IsInRange(avatars.Value, spawnRange))
						{
							showing.Remove(avatars);
							SendAvatarChanged(avatars.Value, AutoLoaderNotification.Hide);
							hidden.AddLast(avatars);
						}
						avatars = next;
					}
				}

				internal void DestroyAllAutoLoaderAvatars()
				{
					foreach (var entry in visible)
					{
						SendAvatarChanged(entry, AutoLoaderNotification.Hide);
						entry.Hide();
					}
					foreach (var entry in hidden)
					{
						entry.Hide();
					}
					foreach (var entry in showing)
					{
						SendAvatarChanged(entry, AutoLoaderNotification.Hide);
						entry.Hide();
					}

					visible.Clear();
					hidden.Clear();
					showing.Clear();
					activeAtGenerator = null;
					queuedAtGenerator = null;
				}

				public int visibleCount { get { return visible.Count; } }

				public int totalCount { get { return visible.Count + showing.Count + hidden.Count + (queuedAtGenerator != null ? 1 : 0) + (activeAtGenerator != null ? 1 : 0);  } }
			}
 
		 * */
		public void SendAvatarChanged(UMALODAvatar avatar, AutoLoaderNotification message)
		{
			if (onAvatarChanged != null)
			{
				onAvatarChanged(avatar, message);
			}
		}

		public int visibleCount { get { return 0; } }

		public void RebuildAll()
		{
			foreach (var layer in LOD)
			{
				layer.RebuildAll();
			}
		}

		public int outOfRangeCount { get { return OutOfRange.Count; } }
	}
}
