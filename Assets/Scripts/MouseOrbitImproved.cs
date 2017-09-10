using UnityEngine;
using System.Collections;

/*
	Modified from this, which was modified from the old stock MouseOrbit Unity script:
	http://wiki.unity3d.com/index.php?title=MouseOrbitImproved
*/

public class MouseOrbitImproved : MonoBehaviour
{
	public Transform target;
	public float distance = 20.0f;
	public float xSpeed = 10.0f;
	public float ySpeed = 10.0f;
	
	public float yMinLimit = -89.0f;
	public float yMaxLimit = 89.0f;
	
	public float distanceMin = 0.5f;
	public float distanceMax = 100f;
	
	float x = 0.0f;
	float y = 0.0f;
	
	void Start () 
	{
		Vector3 angles = transform.eulerAngles;
		x = angles.y;
		y = angles.x;
	}
	 
	void LateUpdate () 
	{
		if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.LeftControl)) && (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2)))
		{
			target.Translate(-transform.right * Input.GetAxis("Mouse X") * distance / 25);
			target.Translate(-transform.up * Input.GetAxis("Mouse Y") * distance / 25);
		}
		else if (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2)) 
		{
			x += Input.GetAxis("Mouse X") * xSpeed;
			y -= Input.GetAxis("Mouse Y") * ySpeed;
		}
		y = ClampAngle(y, yMinLimit, yMaxLimit);
		
		Quaternion rotation = Quaternion.Euler(y, x, 0);
		
		distance = Mathf.Clamp(distance - Input.GetAxis("Mouse ScrollWheel")*5, distanceMin, distanceMax);
		
		Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
		Vector3 position = rotation * negDistance + target.position;
		
		transform.rotation = rotation;
		transform.position = position;
	}
	
	public static float ClampAngle(float angle, float min, float max)
	{
		if (angle < -360F)
			angle += 360F;
		if (angle > 360F)
			angle -= 360F;
		return Mathf.Clamp(angle, min, max);
	}
}