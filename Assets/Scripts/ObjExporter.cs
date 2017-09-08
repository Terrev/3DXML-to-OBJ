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
		Directory.CreateDirectory(Manager.exportPath);
		
		// Main model
		if (Manager.meshes.Count != 0)
		{
			// obj
			MeshHandler.Start();
			StringBuilder meshString = new StringBuilder();
			meshString.Append("mtllib ").Append(Manager.exportFileName).Append(".mtl\n");
			for (int i = 0; i < Manager.meshes.Count; i++)
			{
				meshString.Append("\ng ").Append("Mesh" + i).Append("\n");
				meshString.Append("usemtl ").Append(Manager.colors[Manager.meshes[i].material].legoName).Append("\n\n");
				meshString.Append(MeshHandler.MeshToString(Manager.meshes[i]));
			}
			File.WriteAllText(Manager.exportPath + "\\" + Manager.exportFileName + ".obj", meshString.ToString());
			Debug.Log("Saved file " + Manager.exportFileName + ".obj");
			
			// mtl
			StringBuilder mtlString = new StringBuilder();
			for (int i = 0; i < Manager.sortedColors.Count; i++)
			{
				mtlString.Append("newmtl ").Append(Manager.sortedColors[i].legoName).Append("\n");
				mtlString.Append("Kd ").Append(Manager.sortedColors[i].rgba.r).Append(" ").Append(Manager.sortedColors[i].rgba.g).Append(" ").Append(Manager.sortedColors[i].rgba.b).Append("\n");
				mtlString.Append("d ").Append(Manager.sortedColors[i].rgba.a).Append("\n");
			}
			File.WriteAllText(Manager.exportPath + "\\" + Manager.exportFileName + ".mtl", mtlString.ToString());
			Debug.Log("Saved file " + Manager.exportFileName + ".mtl");
		}
		
		// Decal model
		if (Manager.meshesUV.Count != 0)
		{
			// obj
			MeshHandler.Start();
			StringBuilder meshStringUV = new StringBuilder();
			meshStringUV.Append("mtllib ").Append(Manager.exportFileName).Append("_UV.mtl\n");
			for (int i = 0; i < Manager.meshesUV.Count; i++)
			{
				meshStringUV.Append("\ng ").Append("MeshUV" + i).Append("\n");
				// asdasjdhkjfsfgkjlh
				meshStringUV.Append("usemtl ").Append(Manager.textures[Manager.meshesUV[i].material].textureName).Append("\n\n");
				meshStringUV.Append(MeshHandler.MeshToString(Manager.meshesUV[i]));
				if (!Manager.usedTextures.Contains(Manager.textures[Manager.meshesUV[i].material]))
				{
					Manager.usedTextures.Add(Manager.textures[Manager.meshesUV[i].material]);
				}
			}
			File.WriteAllText(Manager.exportPath + "\\" + Manager.exportFileName + "_UV.obj", meshStringUV.ToString());
			Debug.Log("Saved file " + Manager.exportFileName + "_UV.obj");
			
			// mtl
			StringBuilder mtlStringUV = new StringBuilder();
			for (int i = 0; i < Manager.usedTextures.Count; i++)
			{
				byte[] bytes = Manager.usedTextures[i].texture.EncodeToPNG();
				mtlStringUV.Append("newmtl ").Append(Manager.usedTextures[i].textureName).Append("\n");
				mtlStringUV.Append("Kd 1 1 1").Append("\n");
				mtlStringUV.Append("map_Kd ").Append(Manager.usedTextures[i].textureName).Append(".png\n");
				mtlStringUV.Append("map_d ").Append(Manager.usedTextures[i].textureName).Append(".png\n");
				File.WriteAllBytes(Manager.exportPath + "\\" + Manager.usedTextures[i].textureName + ".png", bytes);
				Debug.Log("Saved file " + Manager.usedTextures[i].textureName + ".png");
			}
			File.WriteAllText(Manager.exportPath + "\\" + Manager.exportFileName + "_UV.mtl", mtlStringUV.ToString());
			Debug.Log("Saved file " + Manager.exportFileName + "_UV.mtl");
		}
	}
}