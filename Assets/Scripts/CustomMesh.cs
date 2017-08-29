using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Unity's mesh class has vertex/face count limits that were getting in the way

public class CustomMesh
{
	public int[] triangles;
	public Vector3[] vertices;
	public Vector3[] normals;
	public Vector2[] uv;
	public int material;
}
