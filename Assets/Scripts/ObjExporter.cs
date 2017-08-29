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
		sb.Append("\n");
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
		// Main model
		if (Manager.meshes.Count != 0)
		{
			// obj
			MeshHandler.Start();
			StringBuilder meshString = new StringBuilder();
			meshString.Append("mtllib ").Append(Manager.fileName).Append(".mtl\n");
			for (int i = 0; i < Manager.meshes.Count; i++)
			{
				meshString.Append("\ng ").Append("Mesh" + i).Append("\n");
				meshString.Append("usemtl Material").Append(Manager.meshes[i].material).Append("\n\n");
				meshString.Append(MeshHandler.MeshToString(Manager.meshes[i]));
			}
			File.WriteAllText(Manager.path + "\\" + Manager.fileName + ".obj", meshString.ToString());
			Debug.Log("Saved file " + Manager.fileName + ".obj");
			
			// mtl
			StringBuilder mtlString = new StringBuilder();
			for (int i = 0; i < Manager.colors.Count; i++)
			{
				mtlString.Append("newmtl Material").Append(i).Append("\n");
				mtlString.Append("Kd ").Append(Manager.colors[i].r).Append(" ").Append(Manager.colors[i].g).Append(" ").Append(Manager.colors[i].b).Append(" ").Append("\n");
				mtlString.Append("d ").Append(Manager.colors[i].a).Append("\n");
			}
			File.WriteAllText(Manager.path + "\\" + Manager.fileName + ".mtl", mtlString.ToString());
			Debug.Log("Saved file " + Manager.fileName + ".mtl");
		}
		
		// Decal model
		if (Manager.meshesUV.Count != 0)
		{
			// obj
			MeshHandler.Start();
			StringBuilder meshStringUV = new StringBuilder();
			meshStringUV.Append("mtllib ").Append(Manager.fileName).Append("UV.mtl\n");
			for (int i = 0; i < Manager.meshesUV.Count; i++)
			{
				meshStringUV.Append("\ng ").Append("MeshUV" + i).Append("\n");
				meshStringUV.Append("usemtl MaterialUV").Append(Manager.meshesUV[i].material).Append("\n\n");
				meshStringUV.Append(MeshHandler.MeshToString(Manager.meshesUV[i]));
				if (!Manager.usedTextures.Contains(Manager.textures[Manager.meshesUV[i].material]))
				{
					Manager.usedTextures.Add(Manager.textures[Manager.meshesUV[i].material]);
				}
			}
			File.WriteAllText(Manager.path + "\\" + Manager.fileName + "UV.obj", meshStringUV.ToString());
			Debug.Log("Saved file " + Manager.fileName + "UV.obj");
			
			// mtl
			StringBuilder mtlStringUV = new StringBuilder();
			for (int i = 0; i < Manager.usedTextures.Count; i++)
			{
				mtlStringUV.Append("newmtl MaterialUV").Append(i).Append("\n");
				mtlStringUV.Append("Kd 1 1 1").Append("\n");
				mtlStringUV.Append("map_Kd Texture").Append(i).Append(".png\n");
				mtlStringUV.Append("map_d Texture").Append(i).Append(".png\n");
				byte[] bytes = Manager.usedTextures[i].EncodeToPNG();
				File.WriteAllBytes(Manager.path + "\\Texture" + i + ".png", bytes);
				Debug.Log("Saved file Texture" + i + ".png");
			}
			File.WriteAllText(Manager.path + "\\" + Manager.fileName + "UV.mtl", mtlStringUV.ToString());
			Debug.Log("Saved file " + Manager.fileName + "UV.mtl");
		}
	}
}