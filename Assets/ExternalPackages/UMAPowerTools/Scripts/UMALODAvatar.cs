using UnityEngine;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Collections;
using System.Collections.Generic;
using UMA;

namespace UMA.PowerTools
{
	public class UMALODAvatar : UMALODRecipeAvatar
	{
		public float rangeMultiplierSquared = 1;
		[NonSerialized]
		public UMALODController.LODEntry lodLevel;
		[NonSerialized]
		public LinkedListNode<UMALODAvatar> node;
		[NonSerialized]
		public int priorityPosition;

		public UMALODController controller;
		public UMALODAnimationControllerSet LODAnimationController;
		public UMALODAvatar()
		{
			node = new LinkedListNode<UMALODAvatar>(this);
		}

		public override void Start()
		{
			base.Start();
			if (umaData.umaRecipe == null)
			{
				umaData.umaRecipe = new UMAData.UMARecipe();
			}
			if (controller != null && node.List == null)
			{
				controller.RegisterAutoLoaderAvatar(this);
			}
		}

		public void Show(UMALODController.LODEntry lodLevel)
		{
			this.lodLevel = lodLevel;
			if (LODAnimationController != null)
			{
				animationController = LODAnimationController.GetController(lodLevel.MeshLOD);
			}
			Show(lodLevel.MeshLOD, lodLevel.atlasResolutionScale);
		}
		
		public override void Hide()
		{
			base.Hide();
			lodLevel = null;
		}

		public bool IsShown { get { return lodLevel != null; } }
	}
}
