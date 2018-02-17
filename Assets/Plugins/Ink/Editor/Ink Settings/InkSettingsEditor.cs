using UnityEngine;
using UnityEditor;

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

			if(serializedObject.FindProperty("templateFile").objectReferenceValue == null) {
				EditorGUILayout.HelpBox("Template not found. Ink files created via Assets > Create > Ink will be blank.", MessageType.Info);
			}
			EditorGUILayout.PropertyField(serializedObject.FindProperty("templateFile"), new GUIContent("Ink Template", "Optional. The default content of files created via Assets > Create > Ink."));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultJsonAssetPath"), new GUIContent("New JSON Path", "By default, story JSON files are placed next to the ink. Drag a folder here to place new JSON files there instead."));

            data.compileAutomatically = EditorGUILayout.Toggle(new GUIContent("Compile All Ink Automatically", "When disabled, automatic compilation can be enabled on a per-story basis via the inspector for a master story file. This can be helpful when you have several stories in a single project."), data.compileAutomatically);
			data.delayInPlayMode = EditorGUILayout.Toggle(new GUIContent("Delay compilation if in Play Mode", "When enabled, ink compilation is delayed if in play mode. Files will be compiled on re-entering edit mode."), data.delayInPlayMode);

			data.handleJSONFilesAutomatically = EditorGUILayout.Toggle(new GUIContent("Handle JSON Automatically", "Whether JSON files are moved, renamed and deleted along with their ink files."), data.handleJSONFilesAutomatically);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("customInklecateOptions"), new GUIContent("Custom Inklecate (Advanced)", "For games using a custom version of ink"), true);

			EditorGUILayout.PropertyField(serializedObject.FindProperty("compileTimeout"), new GUIContent("Compile Timeout", "The max time the compiler will attempt to compile for in case of unhanded errors. You may need to increase this for very large ink projects."), true);

			if(GUI.changed && target != null)
				EditorUtility.SetDirty(target);
			serializedObject.ApplyModifiedProperties();
	    }
	}
}