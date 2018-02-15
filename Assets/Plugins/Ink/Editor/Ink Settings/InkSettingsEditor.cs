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
				EditorGUILayout.HelpBox("Template not found. New files will be blank.", MessageType.Info);
			}
			EditorGUILayout.PropertyField(serializedObject.FindProperty("templateFile"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultJsonAssetPath"));

            data.compileAutomatically = EditorGUILayout.Toggle(new GUIContent("Compile All Ink Automatically", "When disabled, automatic compilation must be enabled on a per-story basis via the inspector for a master story file. This can be helpful when you have several stories in a single project."), data.compileAutomatically);
			data.delayInPlayMode = EditorGUILayout.Toggle(new GUIContent("Delay compilation if in Play Mode", "When enabled, ink compilation is delayed if in play mode. Files will be compiled on re-entering edit mode."), data.delayInPlayMode);

			data.handleJSONFilesAutomatically = EditorGUILayout.Toggle(new GUIContent("Handle JSON Automatically", "Whether JSON files are moved, renamed and deleted along with their ink files."), data.handleJSONFilesAutomatically);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("customInklecateOptions"), new GUIContent("Custom Inklecate (Advanced)", "For games using a custom version of ink"), true);

			EditorGUILayout.PropertyField(serializedObject.FindProperty("compileTimeout"), new GUIContent("Compile Timeout", "The max time the compiler will attempt to compile for in case of unhanded errors."), true);

			if(GUI.changed && target != null)
				EditorUtility.SetDirty(target);
			serializedObject.ApplyModifiedProperties();
	    }
	}
}