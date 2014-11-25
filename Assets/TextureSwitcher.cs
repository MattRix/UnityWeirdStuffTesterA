using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class TextureSwitcher : MonoBehaviour 
{
	public UnityEngine.Object obj;

	public string path;

	public void OnValidate()
	{
#if UNITY_EDITOR
		var mat = renderer.sharedMaterial;

		if(obj != null)
		{
			var tex2D = obj as Texture2D;

			path = AssetDatabase.GetAssetPath(tex2D);

			obj = null;
		}

		if(path != "")
		{
			Texture2D texture = new Texture2D(0,0,TextureFormat.ARGB32,false);
//
			var bytes = File.ReadAllBytes(path);
//
			texture.LoadImage(bytes);
//
			mat.mainTexture = texture;
			mat.mainTexture.hideFlags = HideFlags.DontSave;
		}

		Debug.Log ("hide flags " + mat.mainTexture.hideFlags);
//					mat.mainTexture.hideFlags = HideFlags.DontSave;
//		Debug.Log ("setting don't save");
#endif
	}
}
