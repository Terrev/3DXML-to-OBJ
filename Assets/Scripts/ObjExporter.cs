using UnityEngine;
using System.Collections;
using System.IO;
using System.Text;

/*
	Modified from this:
	http://wiki.unity3d.com/index.php?title=ExportOBJ
	Which was modified from this:
	http://wiki.unity3d.com/index.php?title=ObjExporter
	It's a bit sloppy, but it works...
*/

public class MeshHandler
{
	private static int StartIndex = 0;
	
	public static void Start()
	{
		StartIndex = 0;
	}
	
	public static string MeshToString(CustomMesh m) 
	{
		int numVertices = 0;
		
		StringBuilder sb = new StringBuilder();
		
		foreach(Vector3 v in m.vertices)
		{
			numVertices++;
			sb.Append(string.Format("v {0} {1} {2}\n",v.x,v.y,v.z));
		}
		sb.Append("\n");
		foreach(Vector3 v in m.normals) 
		{
			sb.Append(string.Format("vn {0} {1} {2}\n",v.x,v.y,v.z));
		}
		sb.Append("\n");
		if (m.uv != null)
		{
			foreach(Vector3 v in m.uv) 
			{
				sb.Append(string.Format("vt {0} {1}\n",v.x,v.y));
			}
		}
		if (m.uv != null)
		{
			for (int i=0;i<m.triangles.Length;i+=3)
			{
				sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n", 
					m.triangles[i]+1+StartIndex, m.triangles[i+1]+1+StartIndex, m.triangles[i+2]+1+StartIndex));
			}
		}
		else
		{
			for (int i=0;i<m.triangles.Length;i+=3)
			{
				sb.Append(string.Format("f {0}//{0} {1}//{1} {2}//{2}\n", 
					m.triangles[i]+1+StartIndex, m.triangles[i+1]+1+StartIndex, m.triangles[i+2]+1+StartIndex));
			}
		}
		
		StartIndex += numVertices;
		return sb.ToString();
	}
}

public class ObjExporter
{
	public void DoExport()
	{
		if (Manager.meshes.Count != 0)
		{
			MeshHandler.Start();
			StringBuilder meshString = new StringBuilder();
			for (int i = 0; i < Manager.meshes.Count; i++)
			{
				meshString.Append("g ").Append("Mesh" + i).Append("\n");
				meshString.Append(MeshHandler.MeshToString(Manager.meshes[i]));
			}
			System.IO.File.WriteAllText(Manager.path + "\\" + Manager.fileName + ".obj", meshString.ToString());
			Debug.Log("Saved file " + Manager.fileName + ".obj");
		}
		
		if (Manager.meshesUV.Count != 0)
		{
			MeshHandler.Start();
			StringBuilder meshStringUV = new StringBuilder();
			for (int i = 0; i < Manager.meshesUV.Count; i++)
			{
				meshStringUV.Append("g ").Append("MeshUV" + i).Append("\n");
				meshStringUV.Append(MeshHandler.MeshToString(Manager.meshesUV[i]));
			}
			System.IO.File.WriteAllText(Manager.path + "\\" + Manager.fileName + "UV.obj", meshStringUV.ToString());
			Debug.Log("Saved file " + Manager.fileName + "UV.obj");
		}
	}
}