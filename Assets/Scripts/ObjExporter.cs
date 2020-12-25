using UnityEngine;
using System.Collections;
using System.Collections.Generic;
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
		if (m.uv != null)
		{
			sb.Append("\n");
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
			string[] exportExclusionArray = File.ReadAllLines(Application.streamingAssetsPath + "\\Color Export Exclusion.txt");
			List<string> exportExclusion = new List<string>(exportExclusionArray);
			
			// obj
			MeshHandler.Start();
			StringBuilder meshString = new StringBuilder();
			meshString.Append("mtllib ").Append(Manager.exportFileName).Append(".mtl\n");

			// New feature hacked in - this is the part where I stop caring again
			if (Manager.whatMeshesToMerge == WhatMeshesToMerge.OpaqueOnly)
			{
				List<CustomMesh> meshesTransparent = new List<CustomMesh>();

				meshString.Append("\ng Opaque\n");
				for (int i = 0; i < Manager.meshes.Count; i++)
				{
					if (!exportExclusion.Contains(Manager.colors[Manager.meshes[i].material].id.ToString()))
					{
						if (Manager.colors[Manager.meshes[i].material].rgba.a == 1)
						{
							meshString.Append("\nusemtl ").Append(Manager.colors[Manager.meshes[i].material].legoName).Append("\n\n");
							meshString.Append(MeshHandler.MeshToString(Manager.meshes[i]));
						}
						else
						{
							meshesTransparent.Add(Manager.meshes[i]);
						}
					}
				}
				for (int i = 0; i < meshesTransparent.Count; i++)
				{
					meshString.Append("\ng Transparent").Append(i).Append("\n");
					meshString.Append("\nusemtl ").Append(Manager.colors[meshesTransparent[i].material].legoName).Append("\n\n");
					meshString.Append(MeshHandler.MeshToString(meshesTransparent[i]));
				}
			}
			// The old behavior - export all as one group with the file name, or individual groups/meshes as they were in the original 3DXML
			else
			{
				if (Manager.whatMeshesToMerge == WhatMeshesToMerge.All)
				{
					meshString.Append("\ng ").Append(Manager.exportFileName).Append("\n");
				}
				for (int i = 0; i < Manager.meshes.Count; i++)
				{
					if (!exportExclusion.Contains(Manager.colors[Manager.meshes[i].material].id.ToString()))
					{
						if (Manager.whatMeshesToMerge == WhatMeshesToMerge.None)
						{
							meshString.Append("\ng Mesh").Append(i).Append("\n");
						}
						meshString.Append("\nusemtl ").Append(Manager.colors[Manager.meshes[i].material].legoName).Append("\n\n");
						meshString.Append(MeshHandler.MeshToString(Manager.meshes[i]));
					}
				}
			}

			File.WriteAllText(Manager.exportPath + "\\" + Manager.exportFileName + ".obj", meshString.ToString());
			Debug.Log("Saved file " + Manager.exportFileName + ".obj");
			
			// mtl
			StringBuilder mtlString = new StringBuilder();
			for (int i = 0; i < Manager.sortedColors.Count; i++)
			{
				if (!exportExclusion.Contains(Manager.sortedColors[i].id.ToString()))
				{
					mtlString.Append("newmtl ").Append(Manager.sortedColors[i].legoName).Append("\n");
					mtlString.Append("Kd ").Append(Manager.sortedColors[i].rgba.r).Append(" ").Append(Manager.sortedColors[i].rgba.g).Append(" ").Append(Manager.sortedColors[i].rgba.b).Append("\n");
					mtlString.Append("d ").Append(Manager.sortedColors[i].rgba.a).Append("\n");
				}
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
			if (Manager.whatMeshesToMerge == WhatMeshesToMerge.All)
			{
				meshStringUV.Append("\ng ").Append(Manager.exportFileName).Append("_UV\n");
			}
			for (int i = 0; i < Manager.meshesUV.Count; i++)
			{
				if (Manager.whatMeshesToMerge == WhatMeshesToMerge.None || Manager.whatMeshesToMerge == WhatMeshesToMerge.OpaqueOnly)
				{
					meshStringUV.Append("\ng MeshUV").Append(i).Append("\n");
				}
				// asdasjdhkjfsfgkjlh
				meshStringUV.Append("\nusemtl ").Append(Manager.textures[Manager.meshesUV[i].material].textureName).Append("\n\n");
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
				mtlStringUV.Append("newmtl ").Append(Manager.usedTextures[i].textureName).Append("\n");
				mtlStringUV.Append("Kd 1 1 1\n");
				mtlStringUV.Append("map_Kd ").Append(Manager.usedTextures[i].textureName).Append(".png\n");
				mtlStringUV.Append("map_d ").Append(Manager.usedTextures[i].textureName).Append(".png\n");
				File.WriteAllBytes(Manager.exportPath + "\\" + Manager.usedTextures[i].textureName + ".png", Manager.usedTextures[i].png);
				Debug.Log("Saved file " + Manager.usedTextures[i].textureName + ".png");
			}
			File.WriteAllText(Manager.exportPath + "\\" + Manager.exportFileName + "_UV.mtl", mtlStringUV.ToString());
			Debug.Log("Saved file " + Manager.exportFileName + "_UV.mtl");
		}
	}
}