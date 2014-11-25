using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;

public class SpecialBuilder
{
	[MenuItem ("Magic/Build")]
	static void BuildGame()
	{
		var saveDict = new Dictionary<Material,Texture>();

		var renderers = Texture.FindObjectsOfType<Renderer>();

		foreach(var renderer in renderers)
		{
			var mat = renderer.sharedMaterial;
			if(mat != null && mat.mainTexture != null)
			{
				saveDict.Add(mat,mat.mainTexture);
				mat.mainTexture = null;
			}
		}

		string[] scenes = new string[] {"Assets/MainScene.unity"};
		
		BuildPipeline.BuildPlayer(scenes, Application.dataPath + "/../Exports/Win/BuiltGame.exe", BuildTarget.StandaloneWindows, BuildOptions.None);

		foreach(var kv in saveDict)
		{
			kv.Key.mainTexture = kv.Value;
		}
	}
}
