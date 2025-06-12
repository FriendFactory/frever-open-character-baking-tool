using UnityEngine;
using System.Collections;

public class DistanceSlider : MonoBehaviour 
{
	public Transform Near;
	public Transform Far;
	public Transform Target;
	public void ChangeValue(float lerp)
	{
		Target.position = Vector3.Lerp(Near.position, Far.position, lerp);
	}
}
