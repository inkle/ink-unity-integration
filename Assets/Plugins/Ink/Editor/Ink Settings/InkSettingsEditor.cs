using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;

namespace Ink.UnityIntegration {

	[CustomEditor(typeof(InkSettings))]
	public class InkSettingsEditor : Editor {

		#pragma warning disable
		protected InkSettings data;
		
		public void OnEnable() {
			data = (InkSettings) target;
		}
		
		public override void OnInspectorGUI() {
			serializedObject.Update();
			if (GUILayout.Button(new GUIContent("Recompile All", "Rebuilds the ink library and recompiles all files. Do this if you're getting unusual errors."))) {
				InkCompiler.RecompileAll();
			}

			if(serializedObject.FindProperty("templateFile").objectReferenceValue == null) {
				EditorGUILayout.HelpBox("Template not found. New files will be blank.", MessageType.Info);
			}
			EditorGUILayout.PropertyField(serializedObject.FindProperty("templateFile"));

			data.compileAutomatically = EditorGUILayout.Toggle(new GUIContent("Compile Ink Automatically", "When disabled, automatic compilation can be enabled on a per-story basis via the inspector for a master story file."), data.compileAutomatically);
			data.handleJSONFilesAutomatically = EditorGUILayout.Toggle(new GUIContent("Handle JSON Automatically", "Whether JSON files are moved, renamed and deleted along with their ink files."), data.handleJSONFilesAutomatically);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("customInklecateOptions"), new GUIContent("Custom Inklecate (Advanced)", "For games using a custom version of ink"), true);

			if(GUI.changed && target != null)         
				EditorUtility.SetDirty(target);
			serializedObject.ApplyModifiedProperties();
	    }
	}
}