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
			else
				base.OnHeaderGUI();
		}

		public override void OnInspectorGUI () {
			if(inspector != null) {
				GUI.enabled = true;
				inspector.OnInspectorGUI();
			}
			else
				base.OnInspectorGUI();
		}

		private DefaultAssetInspector FindObjectInspector () {
			var assembly = Assembly.GetExecutingAssembly();
			var assetPath = AssetDatabase.GetAssetPath(target);
			foreach(var type in assembly.GetTypes()) {
				if(type.IsSubclassOf(typeof(DefaultAssetInspector))) {
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