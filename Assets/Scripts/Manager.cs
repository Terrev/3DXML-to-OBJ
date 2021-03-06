﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UnityEngine;
using UnityEngine.SceneManagement;
using B83.MeshHelper;
using Hjg.Pngcs;

// This tool started simply as a 3DXML to OBJ converter
// Then ideas for a bunch of other related, helpful features started coming in
// I added them
// It became a mess
// Good luck spelunking your way through this
// If I ever make any substantial changes/additions past this point, it'll likely be as a new project
// Maybe with .dae exporting and vertex colors and stuff
// AT THE MOMENT I am very tired of working on this, so as long as it works, whatever, good enough lol

public enum WhatMeshesToMerge
{
	None,
	All,
	OpaqueOnly
}

public class Manager : MonoBehaviour
{
	// Directory of exe (up one level from Application.dataPath), set in Start()
	public static DirectoryInfo directoryInfo;
	
	// Paths for unzipping 3DXMLs and LXFMLs, also set in Start()
	public static string unzipPathA = "";
	public static string unzipPathB = "";
	
	// XML namespace thingy for the 3DXML file
	XmlNamespaceManager xmlNamespaceManager;
	
	// Meshes loaded from the 3DXML
	public static List<CustomMesh> meshes = new List<CustomMesh>();
	public static List<CustomMesh> meshesUV = new List<CustomMesh>();
	
	// Colors present in the 3DXML
	public static List<CustomColor> colors = new List<CustomColor>();
	// Same as above, but sorted by official ID (after those have been looked up)
	// Just used to make the order in the exported MTL file nicer
	public static List<CustomColor> sortedColors = new List<CustomColor>();
	
	// Textures present in the 3DXML
	public static List<CustomTexture> textures = new List<CustomTexture>();
	// Textures actually used in the model - currently only set and used by stuff in ObjExporter
	// Used to ensure the grid texture doesn't get exported, since we're not exporting the grid geometry either
	public static List<CustomTexture> usedTextures = new List<CustomTexture>();
	// Just a flag for if textures have been loaded from the 3DXML yet or not
	bool hasLoadedTextures = false;
	
	// 3DXML file name inputted by user
	string inputFileName = "FileName";
	// inputFileName with special characters removed, also the name of the folder the exported model is placed in
	public static string exportFileName = null;
	// Full path to the folder containing the exported model
	public static string exportPath = null;
	
	// Paths to all the custom palette txt files
	string[] customPaletteFiles;
	// Palette options presented to user (0 is no custom palette, then the custom palettes are added)
	List<string> paletteChoices = new List<string>();
	// The user's UI selection
	int selectedPalette = 0;
	
	// LXF/LXFML editing stuff
	string lxfInputFileName = "FileName";
	bool editCamera = true;
	bool editColors = false;
	bool resetColors = false;
	int colorVariationSelection = 0;
	string[] colorVariationOptions = new string[] {" No edit", " Add color variation", " Remove color variation"};
	string[] meshMergingOptions = new string[] { " None", " All", " Opaque only" };
	public static float variationStrength = 6.0f;
	
	// Materials.xml editing
	bool applyCustomPalette = true;
	bool addColorVariations = true;
	bool hasSavedXml = false;
	string savedXmlName = "Materials edited.xml";
	List<string> paletteChoices2 = new List<string>();
	int selectedPalette2 = 0;
	
	// Updating internal definitions
	string materialsInputFileName = "Materials.xml";
	string latestXml = null;
	
	// Misc UI/state stuff
	static bool advancedMode = false;
	bool atStart = true;
	public static bool wireframe = false;
	bool exitConfirmation = false;
	bool export = true;
	bool weld = true;
	public static WhatMeshesToMerge whatMeshesToMerge = WhatMeshesToMerge.None;
	bool shadows = true;
	bool forceGarbageCollection = true; // This only saves a relatively tiny amount of total allocated memory (like 20 MB with my test model), but whatever, I guess I'll leave it in
	
	// Things to assign in the inspector
	public Light sceneLight;
	public MonoBehaviour cameraScript;
	public Material baseMaterial;
	public Material baseMaterialTransparent;
	public Material baseMaterialUV;
	
	void Start()
	{
		// Set up unzip paths and clear out any stuff that may not have been deleted for whatever reason
		unzipPathA = Application.temporaryCachePath + "\\a";
		unzipPathB = Application.temporaryCachePath + "\\b";
		if (Directory.Exists(unzipPathA))
		{
			Directory.Delete(unzipPathA, true);
		}
		if (Directory.Exists(unzipPathB))
		{
			Directory.Delete(unzipPathB, true);
		}
		
		// Other setup stuff
		directoryInfo = Directory.GetParent(Application.dataPath);
		meshes.Clear();
		meshesUV.Clear();
		colors.Clear();
		sortedColors.Clear();
		textures.Clear();
		usedTextures.Clear();
		wireframe = false;
		
		// Get a list of all available custom palettes and add them to the UI choices (after the option for no custom palette)
		customPaletteFiles = Directory.GetFiles(Application.streamingAssetsPath + "\\Custom Palettes", "*.txt");
		paletteChoices.Add(" None");
		for (int i = 0; i < customPaletteFiles.Length; i++)
		{
			paletteChoices.Add(Path.GetFileName(customPaletteFiles[i]));
			paletteChoices[paletteChoices.Count - 1] = " " + paletteChoices[paletteChoices.Count - 1].Substring(0, paletteChoices[paletteChoices.Count - 1].Length - 4);
			
			paletteChoices2.Add(Path.GetFileName(customPaletteFiles[i]));
			paletteChoices2[paletteChoices2.Count - 1] = " " + paletteChoices2[paletteChoices2.Count - 1].Substring(0, paletteChoices2[paletteChoices2.Count - 1].Length - 4);
		}
		
		latestXml = File.ReadAllText(Application.streamingAssetsPath + "\\Autogenerated\\MostRecent.txt");
		
		// PlayerPrefs (saved settings)
		if (PlayerPrefs.HasKey("Advanced Mode"))
		{
			advancedMode = PlayerPrefs.GetInt("Advanced Mode")==1?true:false;
		}
		else
		{
			Debug.Log("No advanced mode setting saved");
		}
		
		if (PlayerPrefs.HasKey("Weld"))
		{
			weld = PlayerPrefs.GetInt("Weld")==1?true:false;
		}
		else
		{
			Debug.Log("No welding setting saved");
		}
		
		if (PlayerPrefs.HasKey("What Meshes To Merge"))
		{
			whatMeshesToMerge = (WhatMeshesToMerge)PlayerPrefs.GetInt("What Meshes To Merge");
		}
		else
		{
			Debug.Log("No mesh merging setting saved");
		}
		
		if (PlayerPrefs.HasKey("Shadows"))
		{
			shadows = PlayerPrefs.GetInt("Shadows")==1?true:false;
		}
		else
		{
			Debug.Log("No shadows setting saved");
		}
		UpdateShadows();
		
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
		
		if (PlayerPrefs.HasKey("Move Camera"))
		{
			editCamera = PlayerPrefs.GetInt("Move Camera")==1?true:false;
		}
		else
		{
			Debug.Log("No camera moving setting saved");
		}
		
		if (PlayerPrefs.HasKey("Edit Colors"))
		{
			colorVariationSelection = PlayerPrefs.GetInt("Edit Colors");
		}
		else
		{
			Debug.Log("No color editing setting saved");
		}
		
		if (PlayerPrefs.HasKey("Variation Strength"))
		{
			variationStrength = PlayerPrefs.GetInt("Variation Strength");
		}
		else
		{
			Debug.Log("No variation strength setting saved");
		}
	}
	
	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			exitConfirmation = !exitConfirmation;
		}
		if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.R))
		{
			Debug.Log("Clearing PlayerPrefs");
			PlayerPrefs.DeleteAll();
		}
	}
	
	void OnGUI()
	{
		/*
		if (GUI.Button(new Rect(Screen.width - 80, 40, 70, 25), forceGarbageCollection.ToString()))
		{
			forceGarbageCollection = !forceGarbageCollection;
		}
		*/
		
		if (atStart)
		{
			if (advancedMode)
			{
				// LXF/LXFML editing
				GUI.Box(new Rect(10, 10, 250, 225), "Edit LXF/LXFML");
				lxfInputFileName = GUI.TextField(new Rect(15, 35, 240, 25), lxfInputFileName, 100);
				if (GUI.Button(new Rect(15, 65, 240, 25), "Edit"))
				{
					PlayerPrefs.SetInt("Move Camera", editCamera?1:0);
					PlayerPrefs.SetInt("Edit Colors", colorVariationSelection);
					PlayerPrefs.SetInt("Variation Strength", (int)variationStrength);
					LxfEditor lxfEditor = new LxfEditor();
					lxfInputFileName = lxfEditor.Edit(lxfInputFileName, editCamera, editColors, resetColors);
				}
				editCamera = GUI.Toggle(new Rect(15, 95, 240, 25), editCamera, " Move camera to origin");
				GUI.Label (new Rect(15, 115, 240, 25), "Edit colors:");
				colorVariationSelection = GUI.SelectionGrid (new Rect(15, 135, 240, 58), colorVariationSelection, colorVariationOptions, 1, "toggle");
				// hhuughufgh
				if (colorVariationSelection == 0)
				{
					editColors = false;
					resetColors = false;
				}
				else if (colorVariationSelection == 1)
				{
					editColors = true;
					resetColors = false;
				}
				else if (colorVariationSelection == 2)
				{
					editColors = false;
					resetColors = true;
				}
				else
				{
					Debug.Log("Wat");
					colorVariationSelection = 0;
				}
				variationStrength = GUI.HorizontalSlider (new Rect(15, 217, 240, 25), variationStrength, 1.0f, 6.0f);
				variationStrength = Mathf.Round(variationStrength);
				string variationInfo = "Variation strength: " + variationStrength;
				if (variationStrength == 6.0f)
				{
					variationInfo = "Variation strength: " + variationStrength + " (LU style)";
				}
				GUI.Label (new Rect(15, 195, 240, 60), variationInfo);
				
				// 3DXML conversion
				GUI.Box(new Rect(10, 245, 250, Screen.height - 255), "Convert 3DXML to OBJ");
				inputFileName = GUI.TextField(new Rect(15, 270, 240, 25), inputFileName, 100);
				if (GUI.Button(new Rect(15, 300, 240, 25), "Convert"))
				{
					export = true;
					DoStuff(export, weld);
				}
				// Meh, we don't need this
				/*
				if (GUI.Button(new Rect(15, 300, 240, 25), "View without converting"))
				{
					export = false;
					DoStuff(export, weld);
				}
				*/
				weld = GUI.Toggle(new Rect(15, 330, 240, 25), weld, " Weld duplicate vertices");
				GUI.Label(new Rect(15, 350, 240, 25), "Merge meshes/groups:");
				whatMeshesToMerge = (WhatMeshesToMerge)GUI.SelectionGrid(new Rect(15, 370, 240, 58), (int)whatMeshesToMerge, meshMergingOptions, 1, "toggle");
				if ((int)whatMeshesToMerge > 2)
				{
					Debug.Log("Uh");
					whatMeshesToMerge = WhatMeshesToMerge.None;
				}
				GUI.Label (new Rect(15, 430, 240, 25), "Color replacement:");
				selectedPalette = GUI.SelectionGrid (new Rect(15, 450, 240, 22 * paletteChoices.Count), selectedPalette, paletteChoices.ToArray(), 1, "toggle");
				
				// Materials.xml editing
				int paletteSelectionHeight = 22 * paletteChoices2.Count;
				GUI.Box(new Rect(270, 10, 250, paletteSelectionHeight + 150), "Edit Materials.xml");
				applyCustomPalette = GUI.Toggle(new Rect(275, 35, 240, 25), applyCustomPalette, " Color replacement:");
				selectedPalette2 = GUI.SelectionGrid (new Rect(290, 55, 240, paletteSelectionHeight), selectedPalette2, paletteChoices2.ToArray(), 1, "toggle");
				addColorVariations = GUI.Toggle(new Rect(275, paletteSelectionHeight + 55, 240, 25), addColorVariations, " Add variations of opaque colors");
				if (GUI.Button(new Rect(275, paletteSelectionHeight + 80, 240, 25), "Edit"))
				{
					EditMaterialsXml();
				}
				if (hasSavedXml)
				{
					GUI.Label (new Rect(275, paletteSelectionHeight + 105, 240, 100), "Saved as:\n" + savedXmlName);
				}
				
				// Updating internal definitions
				GUI.Box(new Rect(270, paletteSelectionHeight + 170, 250, 190), "Update internal definitions");
				GUI.Label (new Rect(275, paletteSelectionHeight + 190, 240, 100), "Current color definitions from:\n" + latestXml);
				materialsInputFileName = GUI.TextField(new Rect(275, paletteSelectionHeight + 245, 240, 25), materialsInputFileName, 100);
				if (GUI.Button(new Rect(275, paletteSelectionHeight + 275, 240, 25), "Update color definitions"))
				{
					LoadColorsFromMaterialsXml();
				}
				GUI.Label (new Rect(275, paletteSelectionHeight + 305, 240, 25), "Loads from Decorations folder:");
				if (GUI.Button(new Rect(275, paletteSelectionHeight + 330, 240, 25), "Update decoration definitions"))
				{
					CalculateAllDecorationMD5s();
				}
				
				if (GUI.Button(new Rect(Screen.width - 80, 10, 70, 25), "Simple"))
				{
					advancedMode = false;
					PlayerPrefs.SetInt("Advanced Mode", 0);
				}
			}
			else
			{
				// simplified UI without color variation options
				// yep it's code copy-paste time
				// don't really care tbh
				
				// LXF/LXFML editing
				GUI.Box(new Rect(10, 10, 250, 85), "Move camera to origin in LXF/LXFML");
				lxfInputFileName = GUI.TextField(new Rect(15, 35, 240, 25), lxfInputFileName, 100);
				if (GUI.Button(new Rect(15, 65, 240, 25), "Move camera"))
				{
					LxfEditor lxfEditor = new LxfEditor();
					lxfInputFileName = lxfEditor.Edit(lxfInputFileName, true, false, false);
				}
				
				// 3DXML conversion
				GUI.Box(new Rect(10, 105, 250, Screen.height - 115), "Convert 3DXML to OBJ");
				inputFileName = GUI.TextField(new Rect(15, 130, 240, 25), inputFileName, 100);
				if (GUI.Button(new Rect(15, 160, 240, 25), "Convert"))
				{
					export = true;
					DoStuff(export, weld);
				}
				weld = GUI.Toggle(new Rect(15, 190, 240, 25), weld, " Weld duplicate vertices");
				GUI.Label(new Rect(15, 210, 240, 25), "Merge meshes/groups:");
				whatMeshesToMerge = (WhatMeshesToMerge)GUI.SelectionGrid(new Rect(15, 230, 240, 58), (int)whatMeshesToMerge, meshMergingOptions, 1, "toggle");
				if ((int)whatMeshesToMerge > 2)
				{
					Debug.Log("Uh");
					whatMeshesToMerge = WhatMeshesToMerge.None;
				}
				GUI.Label (new Rect(15, 290, 240, 25), "Color replacement:");
				selectedPalette = GUI.SelectionGrid (new Rect(15, 310, 240, 22 * paletteChoices.Count), selectedPalette, paletteChoices.ToArray(), 1, "toggle");
				
				if (GUI.Button(new Rect(Screen.width - 80, 10, 70, 25), "Advanced"))
				{
					advancedMode = true;
					PlayerPrefs.SetInt("Advanced Mode", 1);
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
				shadows = !shadows;
				UpdateShadows();
				PlayerPrefs.SetInt("Shadows", shadows?1:0);
			}
			if (GUI.Button(new Rect(10, 70, 110, 25), "Toggle wireframe"))
			{
				wireframe = !wireframe;
			}
			if (export)
			{
				GUI.Box(new Rect(0, Screen.height - 40, Screen.width, 40), "Exported to:\n" + exportPath);
			}
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
	
	void UpdateShadows()
	{
		if (shadows)
		{
			sceneLight.shadows = LightShadows.Soft;
		}
		else
		{
			sceneLight.shadows = LightShadows.None;
		}
	}
	
	void DoStuff(bool exportModel, bool weldModel)
	{
		PlayerPrefs.SetInt("Weld", weld?1:0);
		PlayerPrefs.SetInt("What Meshes To Merge", (int)whatMeshesToMerge);
		PlayerPrefs.SetString("Selected Palette", paletteChoices[selectedPalette]);
		
		Load3dxml();
		
		if (meshes.Count + meshesUV.Count != 0)
		{
			if (weldModel)
			{
				Debug.Log("Welding meshes...");
				DateTime start = DateTime.Now;

				MeshWelder meshWelder = new MeshWelder();
				foreach (CustomMesh customMesh in meshes)
				{
					meshWelder.customMesh = customMesh;
					meshWelder.Weld();
				}
				foreach (CustomMesh customMesh in meshesUV)
				{
					meshWelder.customMesh = customMesh;
					meshWelder.Weld();
				}

				Debug.Log(string.Format("Welding completed in {0} seconds", (DateTime.Now - start).TotalSeconds));
			}
			
			if (exportModel)
			{
				if (forceGarbageCollection)
				{
					// Flush things one more time to hopefully help keep memory from spiking
					System.GC.Collect();
					System.GC.WaitForPendingFinalizers();
				}
				
				ObjExporter objExporter = new ObjExporter();
				objExporter.DoExport();
			}
			
			ViewModel();
			
			cameraScript.enabled = true;
			atStart = false;
		}
		else
		{
			atStart = true;
		}
	}
	
	void Load3dxml()
	{
		string fileName;
		if (inputFileName.EndsWith(".3dxml", true, null))
		{
			fileName = inputFileName.Substring(0, inputFileName.Length - 6);
		}
		else
		{
			fileName = inputFileName;
		}
		
		string inputFilePath = directoryInfo.FullName + "\\Models\\" + fileName + ".3dxml";
		
		if (File.Exists(inputFilePath))
		{
			exportFileName = RemoveSpecialCharacters(fileName);
			exportPath = directoryInfo.FullName + "\\Models\\" + exportFileName;
			
			ZipUtil.Unzip(inputFilePath, unzipPathA);
			
			string[] files = Directory.GetFiles(unzipPathA, "*.3dxml");
			string xmlFileName = Path.GetFileName(files[0]);
			
			if (forceGarbageCollection)
			{
				// Unsure how much garbage unzipping produces but SURE, seems like it could be a lot, let's get it down to as low as possible before doing the hefty XML work
				System.GC.Collect();
				System.GC.WaitForPendingFinalizers();
			}
			
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(File.ReadAllText(unzipPathA + "\\" + xmlFileName));
			
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
							TextureFormat textureFormat = TextureFormat.RGBA32;
							if (texture.Attributes["format"].Value == "RGB")
							{
								textureFormat = TextureFormat.RGB24;
							}
							
							Texture2D tex1 = new Texture2D(int.Parse(texture.Attributes["width"].Value), int.Parse(texture.Attributes["height"].Value), textureFormat, false);
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
							newCustomTexture.png = tex2.EncodeToPNG();
							newCustomTexture.md5 = Md5Sum(newCustomTexture.png);
							textures.Add(newCustomTexture);
						}
						hasLoadedTextures = true;
					}
					meshesUV.Add(RepresentationToMesh(representation));
				}
			}
			
			if (forceGarbageCollection)
			{
				// NUKE ALL THE THINGS
				xmlDocument = null;
				xmlNamespaceManager = null;
				representations = null;
				System.GC.Collect();
				System.GC.WaitForPendingFinalizers();
			}
			
			ColorLookup();
			TextureLookup();
			
			Directory.Delete(unzipPathA, true);
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
	
	// Called by Load3dxml()
	void ColorLookup()
	{
		// List of official LDD colors
		List<CustomColor> lddColors = new List<CustomColor>();
		
		// Load Colors.txt into lddColors (contains IDs and RGBA values, can be regenerated from LDD's Materials.xml)
		string[] officialColors = File.ReadAllLines(Application.streamingAssetsPath + "\\Autogenerated\\Colors.txt");
		for (int i = 0; i < officialColors.Length; i++)
		{
			Char delimiter = ',';
			string[] substrings = officialColors[i].Split(delimiter);
			lddColors.Add(new CustomColor());
			lddColors[lddColors.Count - 1].id = int.Parse(substrings[0]);
			lddColors[lddColors.Count - 1].rgba = new Color32(byte.Parse(substrings[1]), byte.Parse(substrings[2]), byte.Parse(substrings[3]), byte.Parse(substrings[4]));
		}
		
		// Give the LDD colors names from Color Names.txt, when available (not all LDD colors are named)
		// This is a separate file from Colors.txt so names can be easily customized/added, without getting caught up in regeneration of Colors.txt
		// (LDD's Materials.xml doesn't contain the names, they're in separate localization files, and they aren't OBJ/MTL-valid material names by default either)
		string[] colorNames = File.ReadAllLines(Application.streamingAssetsPath + "\\Color Names.txt");
		for (int i = 0; i < colorNames.Length; i++)
		{
			Char delimiter = ',';
			string[] substrings = colorNames[i].Split(delimiter);
			for (int j = 0; j < lddColors.Count; j++)
			{
				if (lddColors[j].id == int.Parse(substrings[0]))
				{
					lddColors[j].legoName = substrings[1];
				}
			}
		}
		
		// Compare colors in 3DXML against lddColors
		for (int i = 0; i < colors.Count; i++)
		{
			for (int j = 0; j < lddColors.Count; j++)
			{
				// Known color, name is set to official ID, and official name if we have one
				if (colors[i].rgba == lddColors[j].rgba)
				{
					colors[i].id = lddColors[j].id;
					colors[i].isKnownColor = true;
					if (lddColors[j].legoName == null)
					{
						colors[i].legoName = colors[i].id.ToString();
					}
					else
					{
						colors[i].legoName = colors[i].id + "_" + lddColors[j].legoName;
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
			Debug.Log("Using color palette" + paletteChoices[selectedPalette]);
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
		
		// Make sorted list of colors for MTL - known colors sorted by ID, unknown colors go on at the end in original order from file
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
	
	// Called by Load3dxml()
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
	
	void ViewModel()
	{
		// Make GameObjects in the scene to show the model to the user
		// Meshes that are too large for Unity's mesh class are skipped, as are meshes we've excluded from exporting
		string[] exportExclusionArray = File.ReadAllLines(Application.streamingAssetsPath + "\\Color Export Exclusion.txt");
		List<string> exportExclusion = new List<string>(exportExclusionArray);
		
		for (int i = 0; i < meshes.Count; i++)
		{
			//Debug.Log("Mesh" + i + ": " + meshes[i].vertices.Length + " verts, " + (meshes[i].triangles.Length / 3) + " tris");
			if (!exportExclusion.Contains(colors[meshes[i].material].id.ToString()))
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
				// Technically, this is an inefficient way to do this; it leads to each mesh having its own unique material
				// In practice, it hardly matters at all - no batching happens anyway because of the negative scale on x
				// And even when the scale isn't set to negative on x, hardly any batching happens because LDD has already combined so much
				meshRenderer.material.color = colors[meshes[i].material].rgba;
				
				Mesh mesh = new Mesh();
				if (meshes[i].vertices.Length > 65534 || (meshes[i].triangles.Length / 3) > 65534)
				{
					mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
				}
				mesh.vertices = meshes[i].vertices;
				mesh.normals = meshes[i].normals;
				mesh.triangles = meshes[i].triangles;
				meshFilter.mesh = mesh;
			}
		}
		
		for (int i = 0; i < meshesUV.Count; i++)
		{
			//Debug.Log("MeshUV" + i + ": " + meshesUV[i].vertices.Length + " verts, " + (meshesUV[i].triangles.Length / 3) + " tris, " + meshesUV[i].uv.Length + " UVs");
			GameObject newGameObject = new GameObject("MeshUV" + i);
			newGameObject.transform.localScale = new Vector3(-1.0f, 1.0f, 1.0f);
			MeshFilter meshFilter = newGameObject.AddComponent<MeshFilter>();
			MeshRenderer meshRenderer = newGameObject.AddComponent<MeshRenderer>();
			meshRenderer.material = baseMaterialUV;
			// Like setting the colors on the main meshes above, this could be made more effecient, but it doesn't actually matter much
			meshRenderer.material.mainTexture = textures[meshesUV[i].material].texture;
			
			Mesh mesh = new Mesh();
			if (meshesUV[i].vertices.Length > 65534 || (meshesUV[i].triangles.Length / 3) > 65534)
			{
				mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
			}
			mesh.vertices = meshesUV[i].vertices;
			mesh.normals = meshesUV[i].normals;
			mesh.uv = meshesUV[i].uv;
			mesh.triangles = meshesUV[i].triangles;
			meshFilter.mesh = mesh;
		}
	}
	
	// Gets colors from a Materials.xml file and puts them into Colors.txt for use by the rest of the program
	void LoadColorsFromMaterialsXml()
	{
		string fileName;
		if (materialsInputFileName.EndsWith(".xml", true, null))
		{
			fileName = materialsInputFileName.Substring(0, materialsInputFileName.Length - 4);
		}
		else
		{
			fileName = materialsInputFileName;
		}
		
		string materialsXmlPath = directoryInfo.FullName + "\\" + fileName + ".xml";
		if (File.Exists(materialsXmlPath))
		{
			List<string> listOfColors = new List<string>();
			List<string> colorsWithVariations = new List<string>();
			
			XmlDocument materialsXml = new XmlDocument();
			materialsXml.LoadXml(File.ReadAllText(materialsXmlPath));
			
			XmlNodeList materialNodes = materialsXml.DocumentElement.SelectNodes("//Material");
			foreach (XmlNode materialNode in materialNodes)
			{
				// Add to list of colors
				listOfColors.Add(materialNode.Attributes["MatID"].Value + "," + materialNode.Attributes["Red"].Value + "," + materialNode.Attributes["Green"].Value + "," + materialNode.Attributes["Blue"].Value + "," + materialNode.Attributes["Alpha"].Value);
				
				// Add to list of colors with variations
				if (materialNode.Attributes["Alpha"].Value == "255" && materialNode.Attributes["MatID"].Value.StartsWith("90000"))
				{
					colorsWithVariations.Add(materialNode.Attributes["MatID"].Value.Substring(5, materialNode.Attributes["MatID"].Value.Length - 5));
				}
			}
			File.WriteAllLines(Application.streamingAssetsPath + "\\Autogenerated\\Colors.txt", listOfColors.ToArray());
			File.WriteAllLines(Application.streamingAssetsPath + "\\Autogenerated\\Colors with Variations.txt", colorsWithVariations.ToArray());
			string[] blah = new string[] {fileName + ".xml"};
			latestXml = blah[0];
			File.WriteAllLines(Application.streamingAssetsPath + "\\Autogenerated\\MostRecent.txt", blah);
			Debug.Log("Updated colors");
		}
		else
		{
			materialsInputFileName = "XML not found!";
		}
	}
	
	// Gets MD5s of all PNGs in the Decorations folder from LDD, saves them and their names/IDs to Decorations.txt
	void CalculateAllDecorationMD5s()
	{
		string decorationsPath = directoryInfo.FullName + "\\Decorations";
		if (Directory.Exists(decorationsPath))
		{
			List<string> text = new List<string>();
			
			string[] decorations = Directory.GetFiles(decorationsPath, "*.png");
			for (int i = 0; i < decorations.Length; i++)
			{
				/*
				// Load texture from png
				byte[] fileData = File.ReadAllBytes(decorations[i]);
				Texture2D texture = new Texture2D(2, 2); // Will automatically resize on LoadImage
				texture.LoadImage(fileData);
				
				string decorationName = Path.GetFileName(decorations[i]);
				// Using EncodeToPNG rather than GetRawTextureData so things will definitely be in the same format
				text.Add(decorationName.Substring(0, decorationName.Length-4) + "," + Md5Sum(tex2.EncodeToPNG()));
				*/
				
				// At some point in the past 2 years, LoadImage started giving incorrect results on some PNGs, distorting their colors
				// I think it may have to do with Unity choking on the gamma information in them somehow, but I'm not sure
				// So what used to take just a few lines in the commented out block of code above...
				// ... now takes a library (PngCs) and all the code below to achieve the same results
				// sigh
				
				string decorationName = Path.GetFileName(decorations[i]);
				
				PngReader pngReader = FileHelper.CreatePngReader(decorations[i]);
				pngReader.SetUnpackedMode(true);
				
				int width = pngReader.ImgInfo.Cols;
				int height = pngReader.ImgInfo.Rows;
				
				// We'll put the texture data in a Texture2D and then call Unity's EncodeToPNG, this will match the output PNGs from the 3DXML conversion
				Texture2D ourNewTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
				
				// RGBA (by far most LDD textures)
				if (pngReader.ImgInfo.Channels == 4)
				{
					byte[] rawTextureData = new byte[height * width * 4];
					// 3 AM shenanigans to reverse the order of the lines cause otherwise the image comes out upside down
					int currentLineStartPoint = rawTextureData.Length - (width * 4);
					for (int row = 0; row < height; row++)
					{
						int howFarInTheLine = 0;
						ImageLine imageLine = pngReader.ReadRowInt(row);
						for (int pixelStart = 0; pixelStart < width * 4; pixelStart += 4)
						{
							int whereWePutThis = currentLineStartPoint + howFarInTheLine;
							rawTextureData[whereWePutThis] = (byte)imageLine.Scanline[pixelStart];
							rawTextureData[whereWePutThis + 1] = (byte)imageLine.Scanline[pixelStart + 1];
							rawTextureData[whereWePutThis + 2] = (byte)imageLine.Scanline[pixelStart + 2];
							rawTextureData[whereWePutThis + 3] = (byte)imageLine.Scanline[pixelStart + 3];
							howFarInTheLine += 4;
						}
						currentLineStartPoint -= width * 4;
					}
					ourNewTexture.LoadRawTextureData(rawTextureData);
					text.Add(decorationName.Substring(0, decorationName.Length-4) + "," + Md5Sum(ourNewTexture.EncodeToPNG()));
				}
				
				// Grayscale + alpha (there's a fair few textures like this)
				else if (pngReader.ImgInfo.Channels == 2)
				{
					byte[] rawTextureData = new byte[height * width * 4];
					// 3 AM shenanigans to reverse the order of the lines cause otherwise the image comes out upside down
					int currentLineStartPoint = rawTextureData.Length - (width * 4);
					for (int row = 0; row < height; row++)
					{
						int howFarInTheLine = 0;
						ImageLine imageLine = pngReader.ReadRowInt(row);
						for (int pixelStart = 0; pixelStart < width * 2; pixelStart += 2)
						{
							int whereWePutThis = currentLineStartPoint + howFarInTheLine;
							rawTextureData[whereWePutThis] = (byte)imageLine.Scanline[pixelStart];
							rawTextureData[whereWePutThis + 1] = (byte)imageLine.Scanline[pixelStart];
							rawTextureData[whereWePutThis + 2] = (byte)imageLine.Scanline[pixelStart];
							rawTextureData[whereWePutThis + 3] = (byte)imageLine.Scanline[pixelStart + 1];
							howFarInTheLine += 4;
						}
						currentLineStartPoint -= width * 4;
					}
					ourNewTexture.LoadRawTextureData(rawTextureData);
					text.Add(decorationName.Substring(0, decorationName.Length-4) + "," + Md5Sum(ourNewTexture.EncodeToPNG()));
				}
				
				// RGB (only one texture like this in LDD at the time of this writing, could change the Texture2D format to RGB24 but it makes no difference in the end so whaaatever)
				else if (pngReader.ImgInfo.Channels == 3)
				{
					byte[] rawTextureData = new byte[height * width * 4];
					// 3 AM shenanigans to reverse the order of the lines cause otherwise the image comes out upside down
					int currentLineStartPoint = rawTextureData.Length - (width * 4);
					for (int row = 0; row < height; row++)
					{
						int howFarInTheLine = 0;
						ImageLine imageLine = pngReader.ReadRowInt(row);
						for (int pixelStart = 0; pixelStart < width * 3; pixelStart += 3)
						{
							int whereWePutThis = currentLineStartPoint + howFarInTheLine;
							rawTextureData[whereWePutThis] = (byte)imageLine.Scanline[pixelStart];
							rawTextureData[whereWePutThis + 1] = (byte)imageLine.Scanline[pixelStart + 1];
							rawTextureData[whereWePutThis + 2] = (byte)imageLine.Scanline[pixelStart + 2];
							rawTextureData[whereWePutThis + 3] = (byte)255;
							howFarInTheLine += 4;
						}
						currentLineStartPoint -= width * 4;
					}
					ourNewTexture.LoadRawTextureData(rawTextureData);
					text.Add(decorationName.Substring(0, decorationName.Length-4) + "," + Md5Sum(ourNewTexture.EncodeToPNG()));
				}
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
	
	// Only needed this once but leaving it in just in case
	// Gets colors from the BrickColors table from LU's database and saves them as a custom palette
	// Note that it leaves it up to the user to change the names/descriptions to be suitable for a MTL
	void LoadLUColors()
	{
		string xmlPath = directoryInfo.FullName + "\\BrickColors.xml";
		if (File.Exists(xmlPath))
		{
			List<string> text = new List<string>();
			
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(File.ReadAllText(xmlPath));
			
			XmlNodeList materialNodes = xmlDocument.DocumentElement.SelectNodes("//row");
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
	
	void EditMaterialsXml()
	{
		if (applyCustomPalette == false && addColorVariations == false)
		{
			Debug.Log("No edits selected");
		}
		else if (applyCustomPalette == true && paletteChoices2.Count == 0)
		{
			Debug.Log("No custom palettes available");
		}
		else
		{
			string materialsXmlPath = directoryInfo.FullName + "\\Materials.xml";
			if (File.Exists(materialsXmlPath))
			{
				XmlDocument materialsXml = new XmlDocument();
				materialsXml.LoadXml(File.ReadAllText(materialsXmlPath));
				
				if (applyCustomPalette)
				{
					CustomPaletteToMaterialsXml(materialsXml);
				}
				if (addColorVariations)
				{
					GenerateColorVariations(materialsXml);
				}
				
				StringBuilder sb = new StringBuilder();
				sb.Append("Materials");
				if (applyCustomPalette && paletteChoices2.Count != 0)
				{
					sb.Append(paletteChoices2[selectedPalette2]);
				}
				if (addColorVariations)
				{
					sb.Append(" with variations");
				}
				sb.Append(".xml");
				savedXmlName = sb.ToString();
				
				materialsXml.Save(directoryInfo.FullName + "\\" + savedXmlName);
				hasSavedXml = true;
			}
			else
			{
				Debug.Log("Could not find " + materialsXmlPath);
			}
		}
	}
	
	// Applies custom palette to Materials.xml
	void CustomPaletteToMaterialsXml(XmlDocument materialsXml)
	{
		if (paletteChoices2.Count == 0)
		{
			Debug.Log("No custom palettes available");
		}
		else
		{
			Debug.Log("Applying color palette" + paletteChoices2[selectedPalette2]);
			string[] customColors = File.ReadAllLines(customPaletteFiles[selectedPalette2]);
			
			XmlNodeList materialNodes = materialsXml.DocumentElement.SelectNodes("//Material");
			foreach (XmlNode materialNode in materialNodes)
			{
				for (int i = 0; i < customColors.Length; i++)
				{
					Char delimiter = ',';
					string[] substrings = customColors[i].Split(delimiter);
					if (materialNode.Attributes["MatID"].Value == substrings[0])
					{
						//Debug.Log("Editing " + materialNode.Attributes["MatID"].Value);
						XmlElement materialElement = (XmlElement)materialNode;
						materialElement.SetAttribute("Red", substrings[1]);
						materialElement.SetAttribute("Green", substrings[2]);
						materialElement.SetAttribute("Blue", substrings[3]);
						materialElement.SetAttribute("Alpha", substrings[4]);
					}
				}
			}
		}
	}
	
	// Appends variations of colors to Materials.xml
	void GenerateColorVariations(XmlDocument materialsXml)
	{
		XmlNodeList materialNodes = materialsXml.DocumentElement.SelectNodes("//Material");
		for (int i = 0; i < materialNodes.Count; i++)
		{
			// Only make variations of solid colors, not transparent colors
			if (materialNodes[i].Attributes["Alpha"].Value == "255")
			{
				//colorsWithVariations.Add(materialNodes[i].Attributes["MatID"].Value);
				// Matches LU color variation
				MakeVariation(materialsXml, materialNodes[i], "00", -6);
				MakeVariation(materialsXml, materialNodes[i], "01", -5);
				MakeVariation(materialsXml, materialNodes[i], "02", -4);
				MakeVariation(materialsXml, materialNodes[i], "03", -3);
				MakeVariation(materialsXml, materialNodes[i], "04", -2);
				MakeVariation(materialsXml, materialNodes[i], "05", -1);
				MakeVariation(materialsXml, materialNodes[i], "06", 0);
				MakeVariation(materialsXml, materialNodes[i], "07", 1);
				MakeVariation(materialsXml, materialNodes[i], "08", 2);
				MakeVariation(materialsXml, materialNodes[i], "09", 3);
				MakeVariation(materialsXml, materialNodes[i], "10", 4);
				MakeVariation(materialsXml, materialNodes[i], "11", 5);
				MakeVariation(materialsXml, materialNodes[i], "12", 6);
			}
		}
	}
	
	void MakeVariation(XmlDocument materialsXml, XmlNode materialNode, string variationID, int rgbTweak)
	{
		XmlElement variation = materialsXml.CreateElement("Material");
		variation.SetAttribute("MatID", "900" + variationID + materialNode.Attributes["MatID"].Value);
		variation.SetAttribute("Red", ColorClampInt(int.Parse(materialNode.Attributes["Red"].Value) + rgbTweak).ToString());
		variation.SetAttribute("Green", ColorClampInt(int.Parse(materialNode.Attributes["Green"].Value) + rgbTweak).ToString());
		variation.SetAttribute("Blue", ColorClampInt(int.Parse(materialNode.Attributes["Blue"].Value) + rgbTweak).ToString());
		variation.SetAttribute("Alpha", materialNode.Attributes["Alpha"].Value);
		variation.SetAttribute("MaterialType", materialNode.Attributes["MaterialType"].Value);
		materialsXml.DocumentElement.AppendChild(variation);
	}
	
	int ColorClampInt(int input)
	{
		if (input < 0)
		{
			return 0;
		}
		else if (input > 255)
		{
			return 255;
		}
		else
		{
			return input;
		}
	}
	
	// From http://wiki.unity3d.com/index.php?title=MD5
	// Used for comparing textures
	string Md5Sum(byte[] bytes)
	{
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
	
	// Used to generate the name for the export folder and OBJ/MTL, could potentially be used for other things too
	string RemoveSpecialCharacters(string str)
	{
		StringBuilder sb = new StringBuilder();
		foreach (char c in str)
		{
			if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '_')
			{
				sb.Append(c);
			}
			else if (c == ' ' || c == '-')
			{
				sb.Append("_");
			}
		}
		return sb.ToString();
	}
}
