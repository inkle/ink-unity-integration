using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Ink.UnityIntegration {
	[CustomEditor(typeof(UnityEngine.Object), true)]
	public class ObjectEditor : Editor {

		private ObjectInspector objectInspector;

		private void OnEnable () {
			objectInspector = FindObjectInspector ();
			if(objectInspector != null) {
				objectInspector.serializedObject = serializedObject;
				objectInspector.target = target;
				objectInspector.OnEnable();
			}
		}

		private void OnDisable () {
			if(objectInspector != null)
				objectInspector.OnDisable();
		}

		public override void OnInspectorGUI () {
			if(objectInspector != null)
				objectInspector.OnInspectorGUI();
			else if (target.GetType() != typeof(UnityEditor.DefaultAsset))
				base.OnInspectorGUI();
		}

		private ObjectInspector FindObjectInspector () {
			List<string> assembliesToCheck = new List<string>{"Assembly-CSharp-Editor", "Assembly-CSharp-Editor-firstpass", "Assembly-UnityScript-Editor", "Assembly-UnityScript-Editor-firstpass"};
			string assetPath = AssetDatabase.GetAssetPath(target);
			Assembly[] referencedAssemblies = System.AppDomain.CurrentDomain.GetAssemblies();
			for(int i = 0; i < referencedAssemblies.Length; ++i) {
				if(!assembliesToCheck.Contains(referencedAssemblies[i].GetName().Name))
					continue;
				foreach(var type in referencedAssemblies[i].GetTypes()) {
					if(!type.IsSubclassOf(typeof(ObjectInspector))) 
						continue;
					ObjectInspector objectInspector = (ObjectInspector)Activator.CreateInstance(type);
					if(objectInspector.IsValid(assetPath)) {
						objectInspector.target = target;
						return objectInspector;
					}
				}
			}
			return null;
		}
	}
}