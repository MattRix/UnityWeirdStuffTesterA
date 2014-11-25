using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class TextureSwitcher : MonoBehaviour 
{
	public Texture obj;

	public string path;

	[HideInInspector] public byte[] imageData;

	#if UNITY_EDITOR
	public void OnValidate()
	{
		if(obj != null)
		{
			var tex2D = obj as Texture2D;

			path = AssetDatabase.GetAssetPath(tex2D);

			obj = null;
		}

		if(path != "")
		{
			imageData = File.ReadAllBytes(path);
		}
	}
	#endif

	public void Update()
	{
		var mat = renderer.sharedMaterial;

		if(mat.mainTexture == null)
		{
			if(imageData != null)
			{
				Texture2D texture = new Texture2D(0,0,TextureFormat.ARGB32,false);
				texture.LoadImage(imageData);
				mat.mainTexture = texture;
			}
		}
	}
}
