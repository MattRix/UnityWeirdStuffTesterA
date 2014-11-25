using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection;
using InspectorPair = FGInspectorReflector.InspectorPair;


[InitializeOnLoad]
public class FGInspectorManager
{
	static GameObject activeGO;
	
	static List<Checker> checkers = new List<Checker>();
	
	static FGInspectorManager()
	{
		EditorApplication.update += OnUpdate;
	}
	
	static void OnUpdate()
	{
		foreach(var checker in checkers)
		{
			bool shouldEnable = checker.checkFunc();
			
			if(shouldEnable && !checker.isEnabled) //enable it
			{
				checker.isEnabled = true;
				FGInspectorReflector.AddPairs(checker.pairs);
				FGInspectorReflector.Apply();
				//				FGInspectorReflector.LogPairsForTarget(checker.targetType);
			}
			else if(!shouldEnable && checker.isEnabled) //disable it
			{
				checker.isEnabled = false;
				FGInspectorReflector.RemovePairs(checker.pairs);
				FGInspectorReflector.Apply();
				//				FGInspectorReflector.LogPairsForTarget(checker.targetType);
			}
		}
	}
	
	static public void Match(Type targetType, Type inspectorType, Func<bool> checkFunc)
	{
		var checker = new Checker(targetType,inspectorType, checkFunc);
		checkers.Add(checker);
	}
	
	private class Checker
	{
		public Type targetType;
		public Type inspectorType;
		public Func<bool> checkFunc;
		
		public InspectorPair[] pairs;
		public bool isEnabled = false;
		
		public Checker(Type targetType, Type inspectorType, Func<bool> checkFunc)
		{
			this.targetType = targetType;
			this.inspectorType = inspectorType;
			this.checkFunc = checkFunc;
			
			this.pairs = new InspectorPair[]
			{
				new InspectorPair(targetType,inspectorType,true,false),
				new InspectorPair(targetType,inspectorType,true,true)
			};
		}
	}
	
}



//This class allows you to change which inspectors are used for which types of objects

[InitializeOnLoad]
public class FGInspectorReflector
{
	public static Type type_CustomEditorAttributes;
	public static FieldInfo field_kSCustomMultiEditors;
	public static FieldInfo field_kSCustomEditors;
	public static Type type_MonoEditorType;
	public static FieldInfo field_targetType;
	public static FieldInfo field_inspectorType;
	public static FieldInfo field_canEditChildTypes;
	
	public static List<InspectorPair>pairs = new List<InspectorPair>();
	
	static FGInspectorReflector() 
	{
		var editorTypes = typeof(Editor).Assembly.GetTypes();
		
		type_CustomEditorAttributes = editorTypes.FirstOrDefault(t => t.Name == "CustomEditorAttributes");
		type_MonoEditorType = editorTypes.FirstOrDefault(t => t.Name == "MonoEditorType");
		field_kSCustomMultiEditors = type_CustomEditorAttributes.GetField("kSCustomMultiEditors",BindingFlags.NonPublic | BindingFlags.Static);
		field_kSCustomEditors = type_CustomEditorAttributes.GetField("kSCustomEditors",BindingFlags.NonPublic | BindingFlags.Static);
		
		field_targetType = type_MonoEditorType.GetField("m_InspectedType");
		field_inspectorType = type_MonoEditorType.GetField("m_InspectorType");
		field_canEditChildTypes = type_MonoEditorType.GetField("m_EditorForChildClasses");
		
		//calling this forces it to initialize the Unity standard type list
		type_CustomEditorAttributes.GetMethod("FindCustomEditorTypeByType",BindingFlags.Static | BindingFlags.NonPublic).Invoke(null,new object[]{null,true});
		
		PopulatePairs(false);
		PopulatePairs(true);
	}
	
	//set Unity pairs based on our custom pairs
	static void PopulatePairs(bool isMulti) 
	{
		FieldInfo field = isMulti ? field_kSCustomMultiEditors : field_kSCustomEditors;
		
		IList objects = field.GetValue(null) as IList;
		
		foreach(var obj in objects)
		{ 
			var pair = new InspectorPair();
			pair.targetType = (Type)field_targetType.GetValue(obj);
			pair.inspectorType = (Type)field_inspectorType.GetValue(obj);
			pair.canEditChildTypes = (bool)field_canEditChildTypes.GetValue(obj);
			pair.isMulti = isMulti;
			pairs.Add(pair);
		}
	}
	
	//set Unity pairs based on our custom pairs
	static public void Apply() 
	{
		IList singleObjects = field_kSCustomEditors.GetValue(null) as IList;
		IList multiObjects = field_kSCustomMultiEditors.GetValue(null) as IList;
		
		singleObjects.Clear();
		multiObjects.Clear();
		
		foreach(var pair in pairs)
		{
			var met = Activator.CreateInstance(type_MonoEditorType);
			field_targetType.SetValue(met,pair.targetType);
			field_inspectorType.SetValue(met,pair.inspectorType);
			field_canEditChildTypes.SetValue(met,pair.canEditChildTypes);
			
			if(pair.isMulti)
			{
				multiObjects.Add(met);
			}
			else
			{
				singleObjects.Add(met);
			}
		}
	}
	
	static public InspectorPair[] GetAllPairsForTarget(Type targetType)
	{
		var returnPairs = new List<InspectorPair>();
		
		foreach(var pair in pairs)
		{
			if(pair.targetType == targetType)
			{
				returnPairs.Add(pair);
			}
		}
		
		return returnPairs.ToArray();
	}
	
	static public InspectorPair GetPair(Type targetType, Type inspectorType, bool canEditChildTypes, bool isMulti)
	{
		foreach(var pair in pairs)
		{
			if(pair.targetType == targetType && 
			   pair.inspectorType == inspectorType && 
			   pair.canEditChildTypes == canEditChildTypes &&
			   pair.isMulti == isMulti)
			{
				return pair;
			}
		}
		return null;
	}
	
	static public void RemovePair(InspectorPair pair)
	{
		pairs.Remove(pair);
	}
	
	static public void RemovePairs(InspectorPair[] pairsToRemove)
	{
		foreach(var pair in pairsToRemove)
		{
			pairs.Remove(pair);
		}
	}
	
	static public void AddPair(InspectorPair pair)
	{
		pairs.Insert(0,pair);//add the pair at the start so it has first priority
	}
	
	static public void AddPairs(InspectorPair[] pairsToAdd)
	{
		foreach(var pair in pairsToAdd)
		{
			pairs.Insert(0,pair);//add the pair at the start so it has first priority
		}
	}
	
	public class InspectorPair
	{
		public Type targetType;
		public Type inspectorType;
		public bool canEditChildTypes;
		public bool isMulti;
		
		public InspectorPair(){}
		public InspectorPair(Type targetType, Type inspectorType, bool canEditChildTypes, bool isMulti)
		{
			this.targetType = targetType;
			this.inspectorType = inspectorType;
			this.canEditChildTypes = canEditChildTypes;
			this.isMulti = isMulti;
		}
	}
	
	//	[MenuItem("Magic/Remove All Inspector Pairs")]
	//	public static void RemoveAllInspectorPairs() 
	//	{
	//		pairs.Clear();
	//		Apply();
	//		
	//		LogPairs();
	//	}
	
	//log out Unity's current list of pairs
	
	public static void LogPairs()
	{
		LogPairs(false);
		LogPairs(true);
	}
	
	public static void LogPairs(bool isMulti)
	{
		FieldInfo field = isMulti ? field_kSCustomMultiEditors : field_kSCustomEditors;
		IList objects = field.GetValue(null) as IList;
		
		Debug.Log(isMulti ? "Multi edit types" : "Single edit types");
		
		foreach(var obj in objects)
		{ 
			var targetType = (Type)field_targetType.GetValue(obj);
			var inspectorType = (Type)field_inspectorType.GetValue(obj);
			
			Debug.Log(targetType + " inspected by: " + inspectorType);
		}
	}
	
	public static void LogPairsForTarget(Type checkTargetType)
	{
		for(int i = 0; i<2; i++) //loop so we do it once for singles, once for multis
		{
			bool isMulti = i == 1;
			FieldInfo field = isMulti ? field_kSCustomMultiEditors : field_kSCustomEditors;
			IList objects = field.GetValue(null) as IList;
			
			foreach(var obj in objects)
			{ 
				var targetType = (Type)field_targetType.GetValue(obj);
				
				if(targetType == checkTargetType)
				{
					var inspectorType = (Type)field_inspectorType.GetValue(obj);
					
					Debug.Log(targetType + " inspected by: " + inspectorType + " isMulti? " + isMulti);
				}
			}
		}
	}
	
}

