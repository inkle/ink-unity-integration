using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Ink.UnityIntegration {
	[CustomEditor(typeof(DefaultAsset), true)]
	public class DefaultAssetEditor : Editor {

		private DefaultAssetInspector inspector;

		private void OnEnable () {
			inspector = FindObjectInspector ();
			if(inspector != null) {
				inspector.editor = this;
				inspector.serializedObject = serializedObject;
				inspector.target = target;
				inspector.OnEnable();
			}
		}

		private void OnDisable () {
			if(inspector != null)
				inspector.OnDisable();
		}

		protected override void OnHeaderGUI () {
			if(inspector != null) {
				inspector.OnHeaderGUI();
			}
			else if (target.GetType() != typeof(UnityEditor.DefaultAsset))
				base.OnHeaderGUI();
		}

		public override void OnInspectorGUI () {
			if(inspector != null) {
				GUI.enabled = true;
				inspector.OnInspectorGUI();
			}
			else if (target.GetType() != typeof(UnityEditor.DefaultAsset))
				base.OnInspectorGUI();
		}

		private DefaultAssetInspector FindObjectInspector () {
			List<string> assembliesToCheck = new List<string>{"Assembly-CSharp-Editor", "Assembly-CSharp-Editor-firstpass", "Assembly-UnityScript-Editor", "Assembly-UnityScript-Editor-firstpass"};
			string assetPath = AssetDatabase.GetAssetPath(target);
			Assembly[] referencedAssemblies = System.AppDomain.CurrentDomain.GetAssemblies();
			for(int i = 0; i < referencedAssemblies.Length; ++i) {
				if(!assembliesToCheck.Contains(referencedAssemblies[i].GetName().Name))
					continue;
				foreach(var type in referencedAssemblies[i].GetTypes()) {
					if(!type.IsSubclassOf(typeof(DefaultAssetInspector))) 
						continue;
					DefaultAssetInspector objectInspector = (DefaultAssetInspector)Activator.CreateInstance(type);
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