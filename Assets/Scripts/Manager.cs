using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;
using UnityEngine.SceneManagement;
using B83.MeshHelper;

public class Manager : MonoBehaviour
{
	XmlNamespaceManager xmlNamespaceManager;
	public static List<CustomMesh> meshes = new List<CustomMesh>();
	public static List<CustomMesh> meshesUV = new List<CustomMesh>();
	public static string inputFileName = "FileName";
	public static string fileName = null;
	public static string path = null;
	public static bool atStart = true;
	bool meshSizeFlag = false;
	bool exitConfirmation = false;
	public Light sceneLight;
	public Material testMaterial;
	public Material testMaterialUV;
	
    void OnGUI()
	{
		if (atStart)
		{
			inputFileName = GUI.TextField(new Rect(10, 10, 250, 25), inputFileName, 100);
			if (GUI.Button(new Rect(10, 40, 250, 25), "Export with welding"))
			{
				DoStuff(true, true);
			}
			if (GUI.Button(new Rect(10, 70, 250, 25), "Export without welding"))
			{
				DoStuff(true, false);
			}
			#if UNITY_EDITOR // Just for testing things
			if (GUI.Button(new Rect(10, 100, 250, 25), "View with welding"))
			{
				DoStuff(false, true);
			}
			if (GUI.Button(new Rect(10, 130, 250, 25), "View without welding"))
			{
				DoStuff(false, false);
			}
			#endif
		}
		if (!atStart)
		{
			if (GUI.Button(new Rect(10, 10, 110, 25), "Back"))
			{
				SceneManager.LoadScene("Scene");
			}
			if (GUI.Button(new Rect(10, 40, 110, 25), "Toggle shadows"))
			{
				if (sceneLight.shadows == LightShadows.Soft)
				{
					sceneLight.shadows = LightShadows.None;
				}
				else
				{
					sceneLight.shadows = LightShadows.Soft;
				}
			}
			GUI.Box (new Rect (0,Screen.height - 40,Screen.width,40), "Exported to:\n" + path);
		}
		if (meshSizeFlag)
		{
			GUI.Box (new Rect (0,Screen.height - 80,Screen.width,40), "One or more meshes are too large to view in this program.\nYour exported model is unaffected by this.");
		}
		if (exitConfirmation)
		{
			GUI.Box (new Rect (Screen.width / 2 - 65,Screen.height / 2 - 25,130,60), "Exit?");
			if(GUI.Button(new Rect(Screen.width / 2 - 50 - 5,Screen.height / 2,50,25), "Yes"))
			{
				Application.Quit();
			}
			if(GUI.Button(new Rect(Screen.width / 2 - 50 + 55,Screen.height / 2,50,25), "No"))
			{
				exitConfirmation = false;
			}
		}
    }
	
	void Start()
	{
		meshes.Clear();
		meshesUV.Clear();
		atStart = true;
		meshSizeFlag = false;
	}
	
	void Update()
	{
		if (Input.GetKeyDown("escape"))
		{
			exitConfirmation = !exitConfirmation;
		}
	}
	
	void DoStuff(bool export, bool weld)
	{
		Load();
		if (meshes.Count + meshesUV.Count != 0)
		{
			if (weld)
			{
				MeshWelder meshWelder = new MeshWelder();
				foreach (CustomMesh customMesh in meshes)
				{
					meshWelder.customMesh = customMesh;
					meshWelder.Weld(false);
				}
				foreach (CustomMesh customMesh in meshesUV)
				{
					meshWelder.customMesh = customMesh;
					meshWelder.Weld(true);
				}
			}
			
			if (export)
			{
				ObjExporter objExporter = new ObjExporter();
				objExporter.DoExport();
			}
			
			// Load the meshes into Unity's mesh class, if they fit into its vertex limit
			for (int i = 0; i < meshes.Count; i++)
			{
				Debug.Log("Mesh" + i + ": " + meshes[i].vertices.Length + " verts, " + (meshes[i].triangles.Length / 3) + " tris");
				if (meshes[i].vertices.Length < 65534 && (meshes[i].triangles.Length / 3) < 65534)
				{
					GameObject newGameObject = new GameObject("Mesh" + i);
					newGameObject.transform.localScale = new Vector3(-1.0f, 1.0f, 1.0f);
					MeshFilter meshFilter = newGameObject.AddComponent<MeshFilter>();
					MeshRenderer meshRenderer = newGameObject.AddComponent<MeshRenderer>();
					meshRenderer.material = testMaterial;
					
					Mesh mesh = new Mesh();
					meshFilter.mesh = mesh;
					mesh.vertices = meshes[i].vertices;
					mesh.normals = meshes[i].normals;
					mesh.triangles = meshes[i].triangles;
				}
				else
				{
					Debug.Log("Cannot display Mesh" + i + " as it is too large");
					meshSizeFlag = true;
				}
			}
			
			for (int i = 0; i < meshesUV.Count; i++)
			{
				Debug.Log("MeshUV" + i + ": " + meshesUV[i].vertices.Length + " verts, " + (meshesUV[i].triangles.Length / 3) + " tris, " + meshesUV[i].uv.Length + " UVs");
				if (meshesUV[i].vertices.Length < 65534 && (meshesUV[i].triangles.Length / 3) < 65534)
				{
					GameObject newGameObject = new GameObject("MeshUV" + i);
					newGameObject.transform.localScale = new Vector3(-1.0f, 1.0f, 1.0f);
					MeshFilter meshFilter = newGameObject.AddComponent<MeshFilter>();
					MeshRenderer meshRenderer = newGameObject.AddComponent<MeshRenderer>();
					meshRenderer.material = testMaterialUV;
					
					Mesh mesh = new Mesh();
					meshFilter.mesh = mesh;
					mesh.vertices = meshesUV[i].vertices;
					mesh.normals = meshesUV[i].normals;
					mesh.uv = meshesUV[i].uv;
					mesh.triangles = meshesUV[i].triangles;
				}
				else
				{
					Debug.Log("Cannot display MeshUV" + i + " as it is too large");
					meshSizeFlag = true;
				}
			}
			
			atStart = false;
		}
		else
		{
			atStart = true;
		}
	}
	
	void Load()
	{
		if (inputFileName.EndsWith(".3dxml", true, null))
		{
			fileName = inputFileName.Substring(0, inputFileName.Length - 6);
		}
		else
		{
			fileName = inputFileName;
		}
		
		System.IO.DirectoryInfo directoryInfo = System.IO.Directory.GetParent(Application.dataPath);
		path = directoryInfo.FullName + "\\Models\\" + fileName;
		
		if (File.Exists(path + ".3dxml"))
		{
			ZipUtil.Unzip(path + ".3dxml", path);
			
			string[] files = System.IO.Directory.GetFiles(path, "*.3dxml");
			string xmlFileName = Path.GetFileName(files[0]);
			
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(System.IO.File.ReadAllText(path + "\\" + xmlFileName));
			
			xmlNamespaceManager = new XmlNamespaceManager(xmlDocument.NameTable);
			xmlNamespaceManager.AddNamespace("a", "http://www.3ds.com/xsd/3DXML");
			xmlNamespaceManager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
			xmlNamespaceManager.AddNamespace("xlink", "http://www.w3.org/1999/xlink");
			
			XmlNodeList representations = xmlDocument.DocumentElement.SelectNodes("//a:Representation", xmlNamespaceManager);
			Debug.Log("Representations: " + representations.Count);
			
			foreach (XmlNode representation in representations)
			{
				// Meshes without UVs
				if (representation.SelectSingleNode(".//a:Face[@triangles]", xmlNamespaceManager) != null
				&& representation.SelectSingleNode(".//a:TextureCoordinates", xmlNamespaceManager) == null)
				{
					meshes.Add(RepresentationToMesh(representation));
				}
				// Meshes with UVs
				else if (representation.SelectSingleNode(".//a:Face[@triangles]", xmlNamespaceManager) != null
				&& representation.SelectSingleNode(".//a:TextureCoordinates", xmlNamespaceManager) != null)
				{
					meshesUV.Add(RepresentationToMesh(representation));
				}
			}
			
			File.Delete(path + "\\" + xmlFileName);
			File.Delete(path + "\\Manifest.xml");
		}
		else
		{
			inputFileName = "Model not found!";
		}
	}
	
	CustomMesh RepresentationToMesh (XmlNode representation)
	{
		XmlNode faceNode = representation.SelectSingleNode(".//a:Face", xmlNamespaceManager);
		string faces = faceNode.Attributes["triangles"].Value.TrimEnd();
		
		XmlNode positionsNode = representation.SelectSingleNode(".//a:Positions", xmlNamespaceManager);
		string positions = positionsNode.InnerText.TrimEnd();
		
		XmlNode normalsNode = representation.SelectSingleNode(".//a:Normals", xmlNamespaceManager);
		string normals = normalsNode.InnerText.TrimEnd();
		
		int[] facesArray = Array.ConvertAll(faces.Split(' '), int.Parse);
		
		float[] positionsFloatArray = Array.ConvertAll(positions.Split(' '), Single.Parse);
		
		float[] normalsFloatArray = Array.ConvertAll(normals.Split(' '), Single.Parse);
		
		Vector3[] positionsArray = new Vector3[positionsFloatArray.Length / 3];
		for (int i = 0, j = 0; i < positionsArray.Length; i++, j += 3)
		{
			positionsArray[i] = new Vector3(positionsFloatArray[j], positionsFloatArray[j + 1], positionsFloatArray[j + 2]);
		}
		
		Vector3[] normalsArray = new Vector3[normalsFloatArray.Length / 3];
		for (int i = 0, j = 0; i < normalsArray.Length; i++, j += 3)
		{
			normalsArray[i] = new Vector3(normalsFloatArray[j], normalsFloatArray[j + 1], normalsFloatArray[j + 2]);
		}
		
		CustomMesh customMesh = new CustomMesh();
		customMesh.vertices = positionsArray;
		customMesh.normals = normalsArray;
		customMesh.triangles = facesArray;
		
		// Only for meshes with UVs
		if (representation.SelectSingleNode(".//a:TextureCoordinates", xmlNamespaceManager) != null)
		{
			string textureCoordinates = representation.SelectSingleNode(".//a:TextureCoordinates", xmlNamespaceManager).InnerText.TrimEnd();
			textureCoordinates = textureCoordinates.Replace(",", "");
			float[] uvFloatArray = Array.ConvertAll(textureCoordinates.Split(' '), Single.Parse);
			Vector2[] uvArray = new Vector2[uvFloatArray.Length / 2];
			for (int i = 0, j = 0; i < uvArray.Length; i++, j += 2)
			{
				uvArray[i] = new Vector2(uvFloatArray[j], -uvFloatArray[j + 1] + 1.0f);
			}
			customMesh.uv = uvArray;
		}
		
		return customMesh;
	}
}
