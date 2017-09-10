using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;

public class LxfEditor
{
	string fileName = null;
	string filePath = null;
	
	public string Edit(string inputFileName)
	{
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
		XmlElement camera = (XmlElement)xmlDocument.DocumentElement.SelectSingleNode(".//Camera");
		camera.SetAttribute("distance", "0");
		camera.SetAttribute("transformation", "1,0,0,0,1,0,0,0,1,0,0,0");
		
		xmlDocument.Save(filePath + " edited.lxfml");
		return("Saved as: " + fileName + " edited.lxfml");
	}
}
