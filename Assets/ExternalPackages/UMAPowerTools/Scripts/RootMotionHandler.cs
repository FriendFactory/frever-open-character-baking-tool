using UnityEngine;
using System.Collections;


namespace UMA.PowerTools
{
	public class RootMotionHandler : MonoBehaviour
	{
		void Start()
		{
			animator = GetComponent<Animator>();
		}
		public Animator animator;
		public Transform rootMotionReceiver;
		void OnAnimatorMove()
		{
			if (animator != null)
			{
				rootMotionReceiver.localPosition += animator.deltaPosition;
				rootMotionReceiver.localRotation = rootMotionReceiver.localRotation * animator.deltaRotation;
			}
		}
	}
}
