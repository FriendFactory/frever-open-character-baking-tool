using UnityEngine;
using System.Collections;

namespace UMA.PowerTools.Demo
{
	public class CrappyTeleportScript : MonoBehaviour
	{
		public Transform destination;

		void OnTriggerEnter(Collider other)
		{
			other.transform.position = destination.position + transform.position.normalized * 2;
		}
	}
}