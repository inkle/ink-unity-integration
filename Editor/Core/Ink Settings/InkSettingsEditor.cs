using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

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

			DrawSettings(serializedObject);

			if(GUI.changed && target != null)
				EditorUtility.SetDirty(target);
			serializedObject.ApplyModifiedProperties();
	    }

		#if UNITY_2018_1_OR_NEWER
		[SettingsProvider]
		public static SettingsProvider CreateInkSettingsProvider() {
			// First parameter is the path in the Settings window.
			// Second parameter is the scope of this setting: it only appears in the Project Settings window.
			var provider = new SettingsProvider("Project/Ink", SettingsScope.Project) {
				// By default the last token of the path is used as display name if no label is provided.
				label = "Ink",
				// Create the SettingsProvider and initialize its drawing (IMGUI) function in place:
				guiHandler = (searchContext) => {
                    // Drawing the SO makes them disabled, and I have no idea why. Drawing manually until fixed.
					// var settings = InkSettings.GetSerializedSettings();
					DrawSettings(InkSettings.instance);
				},

				// Populate the search keywords to enable smart search filtering and label highlighting:
				// keywords = new HashSet<string>(new[] { "Number", "Some String" })
			};
			return provider;
		}
		#endif

        static string versionLabel => string.Format("Ink Unity Integration version "+InkLibrary.unityIntegrationVersionCurrent+"\nInk version "+InkLibrary.inkVersionCurrent+"\nInk story format version "+Ink.Runtime.Story.inkVersionCurrent+"\nInk save format version "+Ink.Runtime.StoryState.kInkSaveStateVersion);
		static void DrawSettings (InkSettings settings) {
			var cachedLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 260;

			EditorGUILayout.HelpBox(versionLabel, MessageType.Info);

			if(settings.templateFile == null) {
				EditorGUILayout.HelpBox("Template not found. Ink files created via Assets > Create > Ink will be blank.", MessageType.Info);
			}
			settings.templateFile = (TextAsset)EditorGUILayout.ObjectField(new GUIContent("Ink Template", "Optional. The default content of files created via Assets > Create > Ink."), settings.templateFile, typeof(TextAsset));
			settings.defaultJsonAssetPath = (DefaultAsset)EditorGUILayout.ObjectField(new GUIContent("New JSON Path", "By default, story JSON files are placed next to the ink. Drag a folder here to place new JSON files there instead."), settings.defaultJsonAssetPath, typeof(DefaultAsset));
            settings.compileAutomatically = EditorGUILayout.Toggle(new GUIContent("Compile All Ink Automatically", "When disabled, automatic compilation can be enabled on a per-story basis via the inspector for a master story file. This can be helpful when you have several stories in a single project."), settings.compileAutomatically);
            settings.delayInPlayMode = EditorGUILayout.Toggle(new GUIContent("Delay compilation if in Play Mode", "When enabled, ink compilation is delayed if in play mode. Files will be compiled on re-entering edit mode."), settings.delayInPlayMode);
            settings.printInkLogsInConsoleOnCompile = EditorGUILayout.Toggle(new GUIContent("Print ink TODOs in console on compile", "When enabled, ink lines starting with TODO are printed in the console."), settings.printInkLogsInConsoleOnCompile);
            settings.handleJSONFilesAutomatically = EditorGUILayout.Toggle(new GUIContent("Handle JSON Automatically", "Whether JSON files are moved, renamed and deleted along with their ink files."), settings.handleJSONFilesAutomatically);
			settings.compileTimeout = EditorGUILayout.IntField(new GUIContent("Compile Timeout", "The max time the compiler will attempt to compile for in case of unhanded errors. You may need to increase this for very large ink projects."), settings.compileTimeout);

			EditorGUIUtility.labelWidth = cachedLabelWidth;

            EditorGUILayout.Separator();
            if(GUILayout.Button("Show changelog")) {
                InkUnityIntegrationStartupWindow.ShowWindow();
            }
		}
		static void DrawSettings (SerializedObject settings) {
			var cachedLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 260;
			EditorGUI.BeginChangeCheck();

			EditorGUILayout.HelpBox(versionLabel, MessageType.Info);

			if(settings.FindProperty("templateFile").objectReferenceValue == null) {
				EditorGUILayout.HelpBox("Template not found. Ink files created via Assets > Create > Ink will be blank.", MessageType.Info);
			}
			EditorGUILayout.PropertyField(settings.FindProperty("templateFile"), new GUIContent("Ink Template", "Optional. The default content of files created via Assets > Create > Ink."));
			EditorGUILayout.PropertyField(settings.FindProperty("defaultJsonAssetPath"), new GUIContent("New JSON Path", "By default, story JSON files are placed next to the ink. Drag a folder here to place new JSON files there instead."));
            EditorGUILayout.PropertyField(settings.FindProperty("compileAutomatically"), new GUIContent("Compile All Ink Automatically", "When disabled, automatic compilation can be enabled on a per-story basis via the inspector for a master story file. This can be helpful when you have several stories in a single project."));
            EditorGUILayout.PropertyField(settings.FindProperty("delayInPlayMode"), new GUIContent("Delay compilation if in Play Mode", "When enabled, ink compilation is delayed if in play mode. Files will be compiled on re-entering edit mode."));
            EditorGUILayout.PropertyField(settings.FindProperty("printInkLogsInConsoleOnCompile"), new GUIContent("Print ink TODOs in console on compile", "When enabled, ink lines starting with TODO are printed in the console."));
            EditorGUILayout.PropertyField(settings.FindProperty("handleJSONFilesAutomatically"), new GUIContent("Handle JSON Automatically", "Whether JSON files are moved, renamed and deleted along with their ink files."));
			EditorGUILayout.PropertyField(settings.FindProperty("compileTimeout"), new GUIContent("Compile Timeout", "The max time the compiler will attempt to compile for in case of unhanded errors. You may need to increase this for very large ink projects."));
            
            if(EditorGUI.EndChangeCheck()) {
				settings.ApplyModifiedProperties();
			}
			EditorGUIUtility.labelWidth = cachedLabelWidth;
            
            EditorGUILayout.Separator();
            if(GUILayout.Button("Show changelog")) {
                InkUnityIntegrationStartupWindow.ShowWindow();
            }
		}
	}
}