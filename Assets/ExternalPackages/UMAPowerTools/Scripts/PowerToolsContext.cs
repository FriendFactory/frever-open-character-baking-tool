using UnityEngine;
using System.Collections;

namespace UMA.PowerTools
{
	public class PowerToolsContext : MonoBehaviour
	{
		public RuntimeAnimatorController animationController;
		public UMALODManager lodManager;
		public UMAGeneratorBase generator;
		public UMALODController controller;
		public UMAContext umaContext;

		static PowerToolsContext _instance;
		public void Awake()
		{
			_instance = this;
		}
		public static PowerToolsContext FindInstance()
		{
			if (_instance == null)
			{
				Debug.Log("Searching");
				var contextGO = GameObject.Find("UMAContext");
				if (contextGO != null)
					_instance = contextGO.GetComponent<PowerToolsContext>();
				if (_instance == null)
				{
					contextGO = GameObject.Find("PowerToolsContext");
					if (contextGO != null)
						_instance = contextGO.GetComponent<PowerToolsContext>();
				}
			}
			return _instance;
		}
	}
}
