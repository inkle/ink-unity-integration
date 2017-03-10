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
			EditorGUILayout.HelpBox("This file caches information about ink files in your project.", MessageType.Info);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("inkLibrary"), true);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("compilationStack"), true);
			if(GUI.changed && target != null)         
				EditorUtility.SetDirty(target);
			serializedObject.ApplyModifiedProperties();
	    }
	}
}