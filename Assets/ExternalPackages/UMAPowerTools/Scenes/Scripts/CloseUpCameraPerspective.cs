using UnityEngine;
using System.Collections;

public class CloseUpCameraPerspective : MonoBehaviour 
{
	public RectTransform targetUIRect;
	public Rect rect;
	public Camera uiCamera;
	public Canvas canvas;

	void Start () 
	{
		var v = new Vector3[4];
		targetUIRect.GetWorldCorners(v);
		for (int i = 0; i < v.Length; i++)
		{
			v[i] = RectTransformUtility.WorldToScreenPoint(uiCamera, v[i]);
			//Debug.Log(v[i]);
		}
		var size = v[2] - v[0];
		var screenRect = new Rect(v[0].x, v[0].y, size.x, size.y);
		//canvas.
		//rect = targetUIRect.rect;
		var camera = GetComponent<Camera>();
		//Debug.Log(rect);
		//var screenRect = new Rect(Screen.width+rect.xMin, Screen.height+rect.yMin,rect.width, rect.height);
		camera.aspect = size.x/size.y;


		camera.rect = new Rect(screenRect.xMin/Screen.width,screenRect.yMin/Screen.height,screenRect.width/Screen.width, screenRect.height/Screen.height);	
	}
}
