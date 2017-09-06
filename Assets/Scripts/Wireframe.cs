using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wireframe : MonoBehaviour
{
	void OnPreRender()
	{
		if (Manager.wireframe)
		{
			GL.wireframe = true;
		}
	}
	
	void OnPostRender()
	{
		if (Manager.wireframe)
		{
			GL.wireframe = false;
		}
	}
}
