﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UnityEngine;

public class LxfEditor
{
	bool editCamera;
	bool editColors;
	bool resetColors;
	string fileName = null;
	string filePath = null;
	
	public string Edit(string inputFileName, bool camera, bool colors, bool colorReset)
	{
		if (camera == false && colors == false && colorReset == false)
		{
			return("Please choose at least one edit");
		}
		editCamera = camera;
		editColors = colors;
		resetColors = colorReset;
		if (inputFileName.EndsWith(".lxf", true, null))
		{
			fileName = inputFileName.Substring(0, inputFileName.Length - 4);
			filePath = Manager.directoryInfo.FullName + "\\Models\\" + fileName;
			
			if (File.Exists(filePath + ".lxf"))
			{
				return(EditXml(LoadLxf()));
			}
			else
			{
				return("LXF not found!");
			}
		}
		else if (inputFileName.EndsWith(".lxfml", true, null))
		{
			fileName = inputFileName.Substring(0, inputFileName.Length - 6);
			filePath = Manager.directoryInfo.FullName + "\\Models\\" + fileName;
			
			if (File.Exists(filePath + ".lxfml"))
			{
				return(EditXml(LoadLxfml()));
			}
			else
			{
				return("LXFML not found!");
			}
		}
		else
		{
			fileName = inputFileName;
			filePath = Manager.directoryInfo.FullName + "\\Models\\" + fileName;
			
			if (File.Exists(filePath + ".lxfml"))
			{
				return(EditXml(LoadLxfml()));
			}
			else if (File.Exists(filePath + ".lxf"))
			{
				return(EditXml(LoadLxf()));
			}
			else
			{
				return("LXF or LXFML not found!");
			}
		}
	}
	
	XmlDocument LoadLxf()
	{
		string unzipPath = Application.temporaryCachePath + "\\b";
		ZipUtil.Unzip(filePath + ".lxf", unzipPath);
		
		// As far as I know, the LXFMLs within the LXFs produced by LDD are always named IMAGE100.LXFML... But just in case the name is ever different, we search for it
		string[] files = Directory.GetFiles(unzipPath, "*.lxfml");
		string unzippedLxfml = Path.GetFileName(files[0]);
		
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.LoadXml(File.ReadAllText(unzipPath + "\\" + unzippedLxfml));
		Directory.Delete(unzipPath, true);
		return xmlDocument;
	}
	
	XmlDocument LoadLxfml()
	{
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.LoadXml(File.ReadAllText(filePath + ".lxfml"));
		return xmlDocument;
	}
	
	string EditXml(XmlDocument xmlDocument)
	{
		if (editCamera)
		{
			XmlElement camera = (XmlElement)xmlDocument.DocumentElement.SelectSingleNode(".//Camera");
			if (camera == null)
			{
				// Models from LU don't have a camera saved at all, so we'll add one
				Debug.Log("Adding new camera to LXFML");
				XmlElement newCameras = xmlDocument.CreateElement("Cameras");
				XmlElement newCamera = xmlDocument.CreateElement("Camera");
				newCamera.SetAttribute("refID", "0");
				newCamera.SetAttribute("fieldOfView", "80");
				newCamera.SetAttribute("distance", "0");
				newCamera.SetAttribute("transformation", "1,0,0,0,1,0,0,0,1,0,0,0");
				newCameras.AppendChild(newCamera);
				xmlDocument.DocumentElement.AppendChild(newCameras);
				XmlElement bricks = (XmlElement)xmlDocument.DocumentElement.SelectSingleNode(".//Bricks");
				bricks.SetAttribute("cameraRef", "0");
			}
			else
			{
				// Edit existing camera
				camera.SetAttribute("distance", "0");
				camera.SetAttribute("transformation", "1,0,0,0,1,0,0,0,1,0,0,0");
			}
		}
		
		if (editColors)
		{
			System.Random rng = new System.Random();
			
			// Autogenerated list of colors that have been given variations in Materials.xml
			string[] colorsWithVariationsArray = File.ReadAllLines(Application.streamingAssetsPath + "\\Autogenerated\\Colors with Variations.txt");
			List<string> colorsWithVariations = new List<string>(colorsWithVariationsArray);
			
			// User-defined list of colors NOT to swap out with variations
			string[] variationExclusionArray = File.ReadAllLines(Application.streamingAssetsPath + "\\Color Variation Exclusion.txt");
			List<string> variationExclusion = new List<string>(variationExclusionArray);
			
			StringBuilder newStringBuilder = new StringBuilder();
			XmlNodeList partNodes = xmlDocument.DocumentElement.SelectNodes("//Part");
			for (int i = 0; i < partNodes.Count; i++)
			{
				string originalString = partNodes[i].Attributes["materials"].Value;
				Char delimiter = ',';
				string[] substrings = originalString.Split(delimiter);
				for (int j = 0; j < substrings.Length; j++)
				{
					if (colorsWithVariations.Contains(substrings[j]) && !variationExclusion.Contains(substrings[j]))
					{
						//int variation = rng.Next(0, 13);
						int variation = rng.Next(6 - (int)Manager.variationStrength, 7 + (int)Manager.variationStrength);
						if (variation <= 9)
						{
							newStringBuilder.Append("9000").Append(variation).Append(substrings[j]).Append(",");
						}
						else
						{
							newStringBuilder.Append("900").Append(variation).Append(substrings[j]).Append(",");
						}
					}
					else
					{
						newStringBuilder.Append(substrings[j]).Append(",");
					}
				}
				string newString = newStringBuilder.ToString();
				newStringBuilder.Length = 0;
				newString = newString.Substring(0, newString.Length - 1);
				XmlElement asdf = (XmlElement)partNodes[i];
				asdf.SetAttribute("materials", newString);
			}
		}
		
		if (resetColors)
		{
			StringBuilder newStringBuilder = new StringBuilder();
			XmlNodeList partNodes = xmlDocument.DocumentElement.SelectNodes("//Part");
			for (int i = 0; i < partNodes.Count; i++)
			{
				string originalString = partNodes[i].Attributes["materials"].Value;
				Char delimiter = ',';
				string[] substrings = originalString.Split(delimiter);
				for (int j = 0; j < substrings.Length; j++)
				{
					// If the material ID begins with 900, strip the first 5 characters from it (gives you the original color ID)
					if (substrings[j].StartsWith("900"))
					{
						newStringBuilder.Append(substrings[j].Substring(5, substrings[j].Length - 5)).Append(",");
					}
					else
					{
						newStringBuilder.Append(substrings[j]).Append(",");
					}
				}
				string newString = newStringBuilder.ToString();
				newStringBuilder.Length = 0;
				newString = newString.Substring(0, newString.Length - 1);
				XmlElement asdf = (XmlElement)partNodes[i];
				asdf.SetAttribute("materials", newString);
			}
		}
		
		StringBuilder blah = new StringBuilder();
		if (editCamera)
		{
			blah.Append(" CAM_SET");
		}
		if (editColors)
		{
			blah.Append(" COLORS_" + Manager.variationStrength);
		}
		if (resetColors)
		{
			blah.Append(" COLORS_RESET");
		}
		xmlDocument.Save(filePath + blah + ".lxfml");
		return("Saved as: " + fileName + blah + ".lxfml");
	}
}
