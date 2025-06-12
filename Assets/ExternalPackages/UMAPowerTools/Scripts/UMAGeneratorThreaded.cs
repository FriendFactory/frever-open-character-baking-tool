using UnityEngine;
using System.Collections.Generic;
using UMA;
using System.Threading;
using System;

namespace UMA.PowerTools
{
	public class UMAGeneratorThreaded : UMAGenerator
	{
		public override void Awake()
		{
			Debug.LogWarning("UMAGeneratorThreaded for uma2 is unneeded, falling back to the default generator.");
			base.Awake();
		}
	}
}
