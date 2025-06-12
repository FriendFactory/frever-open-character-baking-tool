using UnityEngine; 
using System.Collections;

namespace UMA.PowerTools
{
	[AddComponentMenu("Camera-Control/Mouse Look")]
	public class CameraMoveController : MonoBehaviour
	{
		public float sensitivityX = 5;
		public float sensitivityY = 5;

		public float minimumY = -40;
		public float maximumY = 45;

		public float turnSpeed = 120;
		public float moveSpeed = 4;
		public float runMultiplier = 3;

		float rotationX = 0F;
		float rotationY = 0F;
		Rigidbody physX;

		Quaternion originalRotation;

		void Update()
		{
			bool rotated = false;
			if (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2))
			{
				rotationX += Input.GetAxis("Mouse X") * sensitivityX;
				rotationY += Input.GetAxis("Mouse Y") * sensitivityY;

				rotationY = ClampAngle(rotationY, minimumY, maximumY);
				rotated = true;
			}
			if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
			{
				var forward = transform.forward;
				forward.y = 0;
				physX.MovePosition(transform.position + forward.normalized * moveSpeed * (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? runMultiplier : 1f) * UMATime.deltaTime);
			}
			if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
			{
				var forward = transform.forward;
				forward.y = 0;
				physX.MovePosition(transform.position - forward.normalized * moveSpeed * UMATime.deltaTime);
			}
			if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
			{
				rotationX -= turnSpeed * UMATime.deltaTime;
				rotated = true;
			}
			if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
			{
				rotationX += turnSpeed * UMATime.deltaTime;
				rotated = true;
			}
			if (rotated)
			{
				rotationX = NormalizeAngle(rotationX);
				Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
				Quaternion yQuaternion = Quaternion.AngleAxis(rotationY, -Vector3.right);

				transform.localRotation = originalRotation * xQuaternion * yQuaternion;
			}

		}

		void Start()
		{
			originalRotation = transform.localRotation;
			physX = GetComponent<Rigidbody>();
		}

		public static float NormalizeAngle(float angle)
		{
			if (angle < -360)
				angle += 360;
			if (angle > 360)
				angle -= 360;
			return angle;
		}

		public static float ClampAngle(float angle, float min, float max)
		{
			return Mathf.Clamp(NormalizeAngle(angle), min, max);
		}

	}
}

