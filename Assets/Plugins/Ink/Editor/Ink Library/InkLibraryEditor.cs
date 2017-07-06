using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;

namespace Ink.UnityIntegration {

	[CustomEditor(typeof(InkLibrary))]
	public class InkLibraryEditor : Editor {

		#pragma warning disable
		protected InkLibrary data;
		
		public void OnEnable() {
			data = (InkLibrary) target;
		}
		
		public override void OnInspectorGUI() {
			serializedObject.Update();

			if (GUILayout.Button(new GUIContent("Rebuild Library", "Rebuilds the ink library. Do this if you're getting unusual errors"))) {
				InkLibrary.Rebuild();
			}

			if (GUILayout.Button(new GUIContent("Recompile All", "Rebuilds the ink library and recompiles all files."))) {
				InkEditorUtils.RecompileAll();
			}

			EditorGUILayout.HelpBox("This file caches information about ink files in your project.", MessageType.Info);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("inkLibrary"), true);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("compilationStack"), true);
			if(GUI.changed && target != null)         
				EditorUtility.SetDirty(target);
			serializedObject.ApplyModifiedProperties();
	    }
	}
}