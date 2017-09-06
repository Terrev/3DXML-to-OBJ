using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UnityEngine;
using UnityEngine.SceneManagement;
using B83.MeshHelper;

// This grew bigger than I anticipated, and I just added stuff as I needed it, so it got a bit cluttered
// That said, I'm not sure it'd be worth the effort to restructure it

public class Manager : MonoBehaviour
{
	XmlNamespaceManager xmlNamespaceManager;
	public static List<CustomMesh> meshes = new List<CustomMesh>();
	public static List<CustomMesh> meshesUV = new List<CustomMesh>();
	public static List<CustomColor> colors = new List<CustomColor>();
	public static List<CustomColor> legoColors = new List<CustomColor>();
	public static List<CustomColor> sortedColors = new List<CustomColor>();
	public static List<CustomTexture> textures = new List<CustomTexture>();
	public static List<CustomTexture> usedTextures = new List<CustomTexture>();
	public static bool hasLoadedTextures;
	public static string inputFileName = "FileName";
	public static string fileName = null;
	public static string path = null;
	public static bool atStart = true;
	public static bool wireframe = false;
	static bool exportWithWelding = true;
	string[] customPaletteFiles;
	List<string> paletteChoices = new List<string>();
	static int selectedPalette = 0;
	bool meshSizeFlag = false;
	bool exitConfirmation = false;
	static bool developerMenu = false;
	DirectoryInfo directoryInfo;
	public Light sceneLight;
	public Material baseMaterial;
	public Material baseMaterialTransparent;
	public Material baseMaterialUV;
	
	// Awkwardly tacked on stuff for changing the camera postion in LXFs/LXFMLs
	string inputFileNameLxf = "FileName";
	string fileNameLxf = null;
	string pathLxf = null;
	
	void Start()
	{
		meshes.Clear();
		meshesUV.Clear();
		colors.Clear();
		legoColors.Clear();
		sortedColors.Clear();
		textures.Clear();
		usedTextures.Clear();
		hasLoadedTextures = false;
		atStart = true;
		wireframe = false;
		meshSizeFlag = false;
		directoryInfo = Directory.GetParent(Application.dataPath);
		
		customPaletteFiles = Directory.GetFiles(Application.streamingAssetsPath + "\\Custom Palettes", "*.txt");
		paletteChoices.Add(" None (use LDD colors)");
		for (int i = 0; i < customPaletteFiles.Length; i++)
		{
			paletteChoices.Add(Path.GetFileName(customPaletteFiles[i]));
			paletteChoices[paletteChoices.Count - 1] = " " + paletteChoices[paletteChoices.Count - 1].Substring(0, paletteChoices[paletteChoices.Count - 1].Length - 4);
		}
		
		if (PlayerPrefs.HasKey("Export With Welding"))
		{
			exportWithWelding = PlayerPrefs.GetInt("Export With Welding")==1?true:false;
		}
		else
		{
			Debug.Log("No welding export setting saved");
		}
		
		if (PlayerPrefs.HasKey("Selected Palette"))
		{
			for (int i = 0; i < paletteChoices.Count; i++)
			{
				if (paletteChoices[i] == PlayerPrefs.GetString("Selected Palette"))
				{
					selectedPalette = i;
				}
			}
		}
		else
		{
			Debug.Log("No selected palette saved");
		}
	}
	
	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			exitConfirmation = !exitConfirmation;
		}
		if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.D))
		{
			developerMenu = !developerMenu;
		}
		if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.R))
		{
			Debug.Log("Clearing PlayerPrefs");
			PlayerPrefs.DeleteAll();
		}
	}
	
	void OnGUI()
	{
		if (atStart)
		{
			GUI.Box(new Rect(10, 10, 250, 85), "Move camera to origin in LXF/LXFML");
			inputFileNameLxf = GUI.TextField(new Rect(15, 35, 240, 25), inputFileNameLxf, 100);
			if (GUI.Button(new Rect(15, 65, 240, 25), "Move camera"))
			{
				LxfOrLxfml();
			}
			
			GUI.Box(new Rect(10, 105, 250, Screen.height - 115), "Convert 3DXML to OBJ");
			inputFileName = GUI.TextField(new Rect(15, 130, 240, 25), inputFileName, 100);
			if (GUI.Button(new Rect(15, 160, 240, 25), "Convert"))
			{
				DoStuff(true, exportWithWelding);
			}
			exportWithWelding = GUI.Toggle(new Rect (15, 190, 240, 25), exportWithWelding, " Weld duplicate vertices");
			
			GUI.Label (new Rect (15, 210, 240, 25), "Custom color palette:");
			selectedPalette = GUI.SelectionGrid (new Rect (15, 230, 240, 20 * paletteChoices.Count), selectedPalette, paletteChoices.ToArray(), 1, "toggle");
			
			if (developerMenu)
			{
				if (GUI.Button(new Rect(270, 10, 250, 25), "View without converting"))
				{
					DoStuff(false, exportWithWelding);
				}
				if (GUI.Button(new Rect(270, 40, 250, 25), "Get official colors from Materials.xml"))
				{
					LoadOfficialColors();
				}
				if (GUI.Button(new Rect(270, 70, 250, 25), "Calculate all decoration MD5s"))
				{
					CalculateAllDecorationMD5s();
				}
				if (GUI.Button(new Rect(270, 100, 250, 25), "Get LU colors"))
				{
					LoadLUColors();
				}
			}
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
			if (GUI.Button(new Rect(10, 70, 110, 25), "Toggle wireframe"))
			{
				wireframe = !wireframe;
			}
			GUI.Box(new Rect(0, Screen.height - 40, Screen.width, 40), "Exported to:\n" + path);
		}
		if (meshSizeFlag)
		{
			GUI.Box(new Rect(0, Screen.height - 80, Screen.width, 40), "One or more meshes are too large to view in this program.\nYour exported model is unaffected by this.");
		}
		if (exitConfirmation)
		{
			GUI.Box(new Rect(Screen.width / 2 - 55, Screen.height / 2 - 30, 110, 60), "Exit?");
			if(GUI.Button(new Rect(Screen.width / 2 - 40 - 5, Screen.height / 2 - 5, 40, 25), "Yes"))
			{
				Application.Quit();
			}
			if(GUI.Button(new Rect(Screen.width / 2 - 40 + 45, Screen.height / 2 - 5, 40, 25), "No"))
			{
				exitConfirmation = false;
			}
		}
	}
	
	void DoStuff(bool export, bool weld)
	{
		PlayerPrefs.SetInt("Export With Welding", exportWithWelding?1:0);
		PlayerPrefs.SetString("Selected Palette", paletteChoices[selectedPalette]);
		
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
				//Debug.Log("Mesh" + i + ": " + meshes[i].vertices.Length + " verts, " + (meshes[i].triangles.Length / 3) + " tris");
				if (meshes[i].vertices.Length < 65534 && (meshes[i].triangles.Length / 3) < 65534)
				{
					GameObject newGameObject = new GameObject("Mesh" + i);
					newGameObject.transform.localScale = new Vector3(-1.0f, 1.0f, 1.0f);
					MeshFilter meshFilter = newGameObject.AddComponent<MeshFilter>();
					MeshRenderer meshRenderer = newGameObject.AddComponent<MeshRenderer>();
					if (colors[meshes[i].material].rgba.a < 1.0f)
					{
						meshRenderer.material = baseMaterialTransparent;
					}
					else
					{
						meshRenderer.material = baseMaterial;
					}
					// Technically, this is an ineffecient way to do this; it leads to each mesh having its own unique material
					// In practice, it hardly matters at all - no batching happens anyway because of the negative scale on x
					// And even when the scale isn't set to negative on x, hardly any batching happens because LDD has already combined so much
					meshRenderer.material.color = colors[meshes[i].material].rgba;
					
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
				//Debug.Log("MeshUV" + i + ": " + meshesUV[i].vertices.Length + " verts, " + (meshesUV[i].triangles.Length / 3) + " tris, " + meshesUV[i].uv.Length + " UVs");
				if (meshesUV[i].vertices.Length < 65534 && (meshesUV[i].triangles.Length / 3) < 65534)
				{
					GameObject newGameObject = new GameObject("MeshUV" + i);
					newGameObject.transform.localScale = new Vector3(-1.0f, 1.0f, 1.0f);
					MeshFilter meshFilter = newGameObject.AddComponent<MeshFilter>();
					MeshRenderer meshRenderer = newGameObject.AddComponent<MeshRenderer>();
					meshRenderer.material = baseMaterialUV;
					// Like setting the colors on the main meshes above, this could be made more effecient, but it doesn't actually matter much
					meshRenderer.material.mainTexture = textures[meshesUV[i].material].texture;
					
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
		
		path = directoryInfo.FullName + "\\Models\\" + fileName;
		
		if (File.Exists(path + ".3dxml"))
		{
			ZipUtil.Unzip(path + ".3dxml", path);
			
			string[] files = Directory.GetFiles(path, "*.3dxml");
			string xmlFileName = Path.GetFileName(files[0]);
			
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(File.ReadAllText(path + "\\" + xmlFileName));
			
			xmlNamespaceManager = new XmlNamespaceManager(xmlDocument.NameTable);
			xmlNamespaceManager.AddNamespace("a", "http://www.3ds.com/xsd/3DXML");
			xmlNamespaceManager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
			xmlNamespaceManager.AddNamespace("xlink", "http://www.w3.org/1999/xlink");
			
			XmlNodeList representations = xmlDocument.DocumentElement.SelectNodes("//a:Representation", xmlNamespaceManager);
			//Debug.Log("Representations: " + representations.Count);
			
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
					// Texture loading
					if (!hasLoadedTextures)
					{
						XmlNodeList xmlTextures = xmlDocument.DocumentElement.SelectNodes("//a:Pixel", xmlNamespaceManager);
						foreach (XmlNode texture in xmlTextures)
						{
							Texture2D tex1 = new Texture2D(int.Parse(texture.Attributes["width"].Value), int.Parse(texture.Attributes["height"].Value), TextureFormat.RGBA32, false);
							byte[] array = Convert.FromBase64String(texture.SelectSingleNode(".//a:Pixels", xmlNamespaceManager).InnerText);
							tex1.LoadRawTextureData(array);
							tex1.Apply();
							
							// Flipping
							Texture2D tex2 = new Texture2D(tex1.width, tex1.height);
							int xN = tex1.width;
							int yN = tex1.height;
							for (int i = 0; i < xN; i++)
							{
								for (int j = 0; j < yN; j++)
								{
									tex2.SetPixel(j, xN - i - 1, tex1.GetPixel(j, i));
								}
							}
							tex2.wrapMode = TextureWrapMode.Clamp;
							tex2.Apply();
							CustomTexture newCustomTexture = new CustomTexture();
							newCustomTexture.texture = tex2;
							newCustomTexture.md5 = Md5Sum(System.Text.Encoding.Default.GetString(tex2.EncodeToPNG()));
							textures.Add(newCustomTexture);
							hasLoadedTextures = true;
						}
					}
					meshesUV.Add(RepresentationToMesh(representation));
				}
			}
			
			ColorLookup();
			TextureLookup();
			
			File.Delete(path + "\\" + xmlFileName);
			File.Delete(path + "\\Manifest.xml");
		}
		else
		{
			inputFileName = "3DXML not found!";
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
			
			XmlNode materialNode = representation.SelectSingleNode(".//a:Material", xmlNamespaceManager);
			string textureString = materialNode.Attributes["texture"].Value;
			customMesh.material = int.Parse(textureString.Substring(20)) - 1;
		}
		else
		{
			XmlNode colorNode = representation.SelectSingleNode(".//a:Color", xmlNamespaceManager);
			// Using Color32 and Math.Round as loading the xml's values directly into the Color class gives slightly wrong results
			Color meshColor = new Color32((byte)Math.Round(float.Parse(colorNode.Attributes["red"].Value)*255), (byte)Math.Round(float.Parse(colorNode.Attributes["green"].Value)*255), (byte)Math.Round(float.Parse(colorNode.Attributes["blue"].Value)*255), (byte)Math.Round(float.Parse(colorNode.Attributes["alpha"].Value)*255));
			
			int hasColorResult = AlreadyHasColor(meshColor);
			if (hasColorResult == -1)
			{
				colors.Add(new CustomColor());
				colors[colors.Count - 1].rgba = meshColor;
				customMesh.material = colors.Count - 1;
				//Debug.Log("Adding new color");
			}
			else
			{
				customMesh.material = hasColorResult;
				//Debug.Log("Using existing color " + hasColorResult);
			}
		}
		
		return customMesh;
	}
	
	int AlreadyHasColor(Color color)
	{
		for (int i = 0; i < colors.Count; i++)
		{
			if (colors[i].rgba == color)
			{
				return i;
			}
		}
		return -1;
	}
	
	void ColorLookup()
	{
		// Load official colors into legoColors
		string[] officialColors = File.ReadAllLines(Application.streamingAssetsPath + "\\Autogenerated\\Colors.txt");
		for (int i = 0; i < officialColors.Length; i++)
		{
			Char delimiter = ',';
			string[] substrings = officialColors[i].Split(delimiter);
			legoColors.Add(new CustomColor());
			legoColors[legoColors.Count - 1].id = int.Parse(substrings[0]);
			legoColors[legoColors.Count - 1].rgba = new Color32(byte.Parse(substrings[1]), byte.Parse(substrings[2]), byte.Parse(substrings[3]), byte.Parse(substrings[4]));
		}
		
		// Load color names into legoColors
		string[] colorNames = File.ReadAllLines(Application.streamingAssetsPath + "\\Color Names.txt");
		for (int i = 0; i < colorNames.Length; i++)
		{
			Char delimiter = ',';
			string[] substrings = colorNames[i].Split(delimiter);
			for (int j = 0; j < legoColors.Count; j++)
			{
				if (legoColors[j].id == int.Parse(substrings[0]))
				{
					legoColors[j].legoName = substrings[1];
				}
			}
		}
		
		// Check colors in 3DXML against legoColors
		for (int i = 0; i < colors.Count; i++)
		{
			for (int j = 0; j < legoColors.Count; j++)
			{
				// Known color, name is set to official ID and official name if we have one
				if (colors[i].rgba == legoColors[j].rgba)
				{
					colors[i].id = legoColors[j].id;
					colors[i].isKnownColor = true;
					if (legoColors[j].legoName == null)
					{
						colors[i].legoName = colors[i].id.ToString();
					}
					else
					{
						colors[i].legoName = colors[i].id + "_" + legoColors[j].legoName;
					}
					break;
				}
				// Unknown color, name is just the RGB values
				else
				{
					colors[i].isKnownColor = false;
					Color32 unknownColor = new Color32();
					unknownColor = colors[i].rgba;
					StringBuilder unknownName = new StringBuilder();
					unknownName.Append(unknownColor.r).Append("_").Append(unknownColor.g).Append("_").Append(unknownColor.b).Append("_").Append(unknownColor.a);
					colors[i].legoName = unknownName.ToString();
				}
			}
		}
		
		// Apply custom palette color/name overrides
		if (selectedPalette == 0)
		{
			Debug.Log("Using LDD colors");
		}
		else
		{
			Debug.Log("Using color palette " + paletteChoices[selectedPalette]);
			string[] customColors = File.ReadAllLines(customPaletteFiles[selectedPalette - 1]);
			for (int i = 0; i < colors.Count; i++)
			{
				for (int j = 0; j < customColors.Length; j++)
				{
					Char delimiter = ',';
					string[] substrings = customColors[j].Split(delimiter);
					if (colors[i].id == int.Parse(substrings[0]))
					{
						colors[i].rgba = new Color32(byte.Parse(substrings[1]), byte.Parse(substrings[2]), byte.Parse(substrings[3]), byte.Parse(substrings[4]));
						if (substrings.Length >= 6)
						{
							colors[i].legoName = substrings[5];
						}
					}
				}
			}
		}
		
		// Make sorted list of colors for mtl - known colors sorted by ID, unknown colors go on at the end in original order from file
		List<int> colorsToSort = new List<int>();
		List<CustomColor> unknownColors = new List<CustomColor>();
		foreach (CustomColor customColor in colors)
		{
			if (customColor.isKnownColor)
			{
				colorsToSort.Add(customColor.id);
			}
			else
			{
				unknownColors.Add(customColor);
			}
		}
		colorsToSort.Sort();
		// For each sorted ID:
		// - Go through colors
		// - Find match, add color that matched the current ID to a list
		// Results in a list of colors in the same order as the sorted IDs
		foreach (int sortedID in colorsToSort)
		{
			for (int i = 0; i < colors.Count; i++)
			{
				if (colors[i].id == sortedID)
				{
					sortedColors.Add(colors[i]);
				}
			}
		}
		sortedColors.AddRange(unknownColors);
	}
	
	void TextureLookup()
	{
		string[] decorations = File.ReadAllLines(Application.streamingAssetsPath + "\\Autogenerated\\Decorations.txt");
		for (int i = 0; i < textures.Count; i++)
		{
			for (int j = 0; j < decorations.Length; j++)
			{
				Char delimiter = ',';
				string[] substrings = decorations[j].Split(delimiter);
				// If we get an MD5 match, set the texture's ID that MD5's ID
				if (textures[i].md5 == substrings[1])
				{
					textures[i].id = int.Parse(substrings[0]);
					textures[i].textureName = substrings[0];
					break;
				}
				// No match, will be named their MD5s
				else
				{
					textures[i].textureName = textures[i].md5;
				}
			}
		}
	}
	
	void LxfOrLxfml()
	{
		if (inputFileNameLxf.EndsWith(".lxf", true, null))
		{
			fileNameLxf = inputFileNameLxf.Substring(0, inputFileNameLxf.Length - 4);
			pathLxf = directoryInfo.FullName + "\\Models\\" + fileNameLxf;
			if (File.Exists(pathLxf + ".lxf"))
			{
				EditLxf();
			}
			else
			{
				inputFileNameLxf = "LXF not found!";
			}
		}
		else if (inputFileNameLxf.EndsWith(".lxfml", true, null))
		{
			fileNameLxf = inputFileNameLxf.Substring(0, inputFileNameLxf.Length - 6);
			pathLxf = directoryInfo.FullName + "\\Models\\" + fileNameLxf;
			if (File.Exists(pathLxf + ".lxfml"))
			{
				EditLxfml();
			}
			else
			{
				inputFileNameLxf = "LXFML not found!";
			}
		}
		else
		{
			fileNameLxf = inputFileNameLxf;
			pathLxf = directoryInfo.FullName + "\\Models\\" + fileNameLxf;
			if (File.Exists(pathLxf + ".lxfml"))
			{
				EditLxfml();
			}
			else if (File.Exists(pathLxf + ".lxf"))
			{
				EditLxf();
			}
			else
			{
				inputFileNameLxf = "LXF or LXFML not found!";
			}
		}
	}
	
	void EditLxf()
	{
		string unzippedLocation = Application.temporaryCachePath + "\\" + "unzip";
		ZipUtil.Unzip(pathLxf + ".lxf", unzippedLocation);
		
		// As far as I know, the LXFMLs within the LXFs produced by LDD are always named IMAGE100.LXFML... But just in case the name is ever different, we search for it
		string[] files = Directory.GetFiles(unzippedLocation, "*.lxfml");
		string lxfmlFileName = Path.GetFileName(files[0]);
		
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.LoadXml(File.ReadAllText(unzippedLocation + "\\" + lxfmlFileName));
		
		XmlElement camera = (XmlElement)xmlDocument.DocumentElement.SelectSingleNode(".//Camera");
		camera.SetAttribute("distance", "0");
		camera.SetAttribute("transformation", "1,0,0,0,1,0,0,0,1,0,0,0");
		
		xmlDocument.Save(pathLxf + " edited.lxfml");
		Directory.Delete(unzippedLocation, true);
		inputFileNameLxf = "Saved as: " + fileNameLxf + " edited.lxfml";
	}
	
	void EditLxfml()
	{
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.LoadXml(File.ReadAllText(pathLxf + ".lxfml"));
		
		XmlElement camera = (XmlElement)xmlDocument.DocumentElement.SelectSingleNode(".//Camera");
		camera.SetAttribute("distance", "0");
		camera.SetAttribute("transformation", "1,0,0,0,1,0,0,0,1,0,0,0");
		
		xmlDocument.Save(pathLxf + " edited.lxfml");
		inputFileNameLxf = "Saved as: " + fileNameLxf + " edited.lxfml";
	}
	
	void LoadOfficialColors()
	{
		string materialsXmlPath = directoryInfo.FullName + "\\Materials.xml";
		if (File.Exists(materialsXmlPath))
		{
			List<string> text = new List<string>();
			
			XmlDocument materialsXml = new XmlDocument();
			materialsXml.LoadXml(File.ReadAllText(materialsXmlPath));
			
			XmlNodeList materialNodes = materialsXml.DocumentElement.SelectNodes("//a:Material");
			foreach (XmlNode materialNode in materialNodes)
			{
				text.Add(materialNode.Attributes["MatID"].Value + "," + materialNode.Attributes["Red"].Value + "," + materialNode.Attributes["Green"].Value + "," + materialNode.Attributes["Blue"].Value + "," + materialNode.Attributes["Alpha"].Value);
			}
			File.WriteAllLines(Application.streamingAssetsPath + "\\Autogenerated\\Colors.txt", text.ToArray());
			Debug.Log("Loaded LDD colors");
		}
		else
		{
			Debug.Log("Could not find " + materialsXmlPath);
		}
	}
	
	void CalculateAllDecorationMD5s()
	{
		string decorationsPath = directoryInfo.FullName + "\\Decorations";
		if (Directory.Exists(decorationsPath))
		{
			List<string> text = new List<string>();
			
			string[] decorations = Directory.GetFiles(decorationsPath, "*.png");
			for (int i = 0; i < decorations.Length; i++)
			{
				// Load texture from png
				Texture2D texture = null;
				byte[] fileData;
				fileData = File.ReadAllBytes(decorations[i]);
				texture = new Texture2D(2, 2); // Will automatically resize on LoadImage
				texture.LoadImage(fileData);
				
				string fileName = Path.GetFileName(decorations[i]);
				// Using EncodeToPNG rather than GetRawTextureData so things will definitely be in the same format
				text.Add(fileName.Substring(0, fileName.Length-4) + "," + Md5Sum(System.Text.Encoding.Default.GetString(texture.EncodeToPNG())));
			}
			
			// Save the lists to files
			File.WriteAllLines(Application.streamingAssetsPath + "\\Autogenerated\\Decorations.txt", text.ToArray());
			Debug.Log("Calculated decoration MD5s");
		}
		else
		{
			Debug.Log("Could not find " + decorationsPath);
		}
	}
	
	void LoadLUColors()
	{
		string xmlPath = directoryInfo.FullName + "\\BrickColors.xml";
		if (File.Exists(xmlPath))
		{
			List<string> text = new List<string>();
			
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(File.ReadAllText(xmlPath));
			
			XmlNodeList materialNodes = xmlDocument.DocumentElement.SelectNodes("//a:row");
			foreach (XmlNode materialNode in materialNodes)
			{
				string legoID = materialNode.Attributes["legopaletteid"].Value;
				string red = Math.Round(float.Parse(materialNode.Attributes["red"].Value)*255).ToString();
				string green = Math.Round(float.Parse(materialNode.Attributes["green"].Value)*255).ToString();
				string blue = Math.Round(float.Parse(materialNode.Attributes["blue"].Value)*255).ToString();
				string alpha = Math.Round(float.Parse(materialNode.Attributes["alpha"].Value)*255).ToString();
				string name = "LU" + materialNode.Attributes["id"].Value + "_" + materialNode.Attributes["description"].Value;
				text.Add(legoID + "," + red + "," + green + "," + blue + "," + alpha + "," + name);
			}
			File.WriteAllLines(Application.streamingAssetsPath + "\\Custom Palettes\\LEGO Universe Unmodified.txt", text.ToArray());
			Debug.Log("Loaded LU colors");
		}
		else
		{
			Debug.Log("Could not find " + xmlPath);
		}
	}
	
	// From http://wiki.unity3d.com/index.php?title=MD5
	string Md5Sum(string strToEncrypt)
	{
		System.Text.UTF8Encoding ue = new System.Text.UTF8Encoding();
		byte[] bytes = ue.GetBytes(strToEncrypt);
	 
		// encrypt bytes
		System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
		byte[] hashBytes = md5.ComputeHash(bytes);
	 
		// Convert the encrypted bytes back to a string (base 16)
		string hashString = "";
	 
		for (int i = 0; i < hashBytes.Length; i++)
		{
			hashString += System.Convert.ToString(hashBytes[i], 16).PadLeft(2, '0');
		}
	 
		return hashString.PadLeft(32, '0');
	}
}
