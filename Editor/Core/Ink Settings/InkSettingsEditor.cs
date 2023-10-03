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

			if (GUI.changed && target != null) {
				EditorUtility.SetDirty(target);
				((InkSettings) target).Save(true);
			}
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

        static void DrawSettings (InkSettings settings) {
	        EditorGUI.BeginChangeCheck();
	        
	        EditorGUI.indentLevel++;
			DrawVersions();
			EditorGUILayout.Separator();

			var cachedLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 260;

			EditorGUILayout.LabelField(new GUIContent("Settings"), EditorStyles.boldLabel);
			if(settings.templateFile == null) 
				DrawTemplateMissingWarning();
			settings.templateFile = (DefaultAsset)EditorGUILayout.ObjectField(new GUIContent("Ink Template", "Optional. The default content of files created via Assets > Create > Ink."), settings.templateFile, typeof(DefaultAsset));
      
			settings.defaultJsonAssetPath = (DefaultAsset)EditorGUILayout.ObjectField(new GUIContent("New JSON Path", "By default, story JSON files are placed next to the ink. Drag a folder here to place new JSON files there instead."), settings.defaultJsonAssetPath, typeof(DefaultAsset));
            settings.compileAllFilesAutomatically = EditorGUILayout.Toggle(new GUIContent("Compile All Ink Automatically", "When disabled, automatic compilation can be enabled on a per-story basis via the inspector for a master story file. This can be helpful when you have several stories in a single project."), settings.compileAllFilesAutomatically);
            settings.delayInPlayMode = EditorGUILayout.Toggle(new GUIContent("Delay compilation if in Play Mode", "When enabled, ink compilation is delayed if in play mode. Files will be compiled on re-entering edit mode."), settings.delayInPlayMode);
            settings.printInkLogsInConsoleOnCompile = EditorGUILayout.Toggle(new GUIContent("Print ink TODOs in console on compile", "When enabled, ink lines starting with TODO are printed in the console."), settings.printInkLogsInConsoleOnCompile);
            settings.handleJSONFilesAutomatically = EditorGUILayout.Toggle(new GUIContent("Handle JSON Automatically", "Whether JSON files are moved, renamed and deleted along with their ink files."), settings.handleJSONFilesAutomatically);
			settings.compileTimeout = EditorGUILayout.IntField(new GUIContent("Compile Timeout", "The max time the compiler will attempt to compile for in case of unhanded errors. You may need to increase this for very large ink projects."), settings.compileTimeout);
			settings.suppressStartupWindow = EditorGUILayout.Toggle(new GUIContent("Suppress Startup Window", "Prevent the \"what's new\" (the one that appears if you click the \"Show changelog\" button above) appearing when the version of this plugin has changed and Unity is opened. This can be useful for CI/CD pipelines, where auto-launching editor windows can fail to load due to a Unity bug."), settings.suppressStartupWindow);
			settings.automaticallyAddDefineSymbols = EditorGUILayout.Toggle(new GUIContent("Add define symbols", "If true, automatically adds INK_EDITOR and INK_RUNTIME to the define symbols in the build settings. This is handy for conditional code."), settings.automaticallyAddDefineSymbols);
			//// DrawDefineManagerButtons();
			
			EditorGUILayout.Separator();
			DrawRequestButton();

			EditorGUIUtility.labelWidth = cachedLabelWidth;
			EditorGUI.indentLevel--;
			
			if (EditorGUI.EndChangeCheck()) {
				if (settings.automaticallyAddDefineSymbols) InkDefineSymbols.AddGlobalDefine();
				else InkDefineSymbols.RemoveGlobalDefine();
				
				EditorUtility.SetDirty(settings);
				settings.Save(true);
			}
		}
        
		static void DrawSettings (SerializedObject settings) {
			DrawVersions();
			EditorGUILayout.Separator();

			var cachedLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 260;
			EditorGUI.BeginChangeCheck();

			EditorGUILayout.LabelField(new GUIContent("Settings"), EditorStyles.boldLabel);
			if(settings.FindProperty("templateFile").objectReferenceValue == null) 
				DrawTemplateMissingWarning();
			
			EditorGUILayout.PropertyField(settings.FindProperty("templateFile"), new GUIContent("Ink Template", "Optional. The default content of files created via Assets > Create > Ink."));
			EditorGUILayout.PropertyField(settings.FindProperty("defaultJsonAssetPath"), new GUIContent("New JSON Path", "By default, story JSON files are placed next to the ink. Drag a folder here to place new JSON files there instead."));
            EditorGUILayout.PropertyField(settings.FindProperty("compileAllFilesAutomatically"), new GUIContent("Compile All Ink Automatically", "When disabled, automatic compilation can be enabled on a per-story basis via the inspector for a master story file. This can be helpful when you have several stories in a single project."));
            EditorGUILayout.PropertyField(settings.FindProperty("delayInPlayMode"), new GUIContent("Delay compilation if in Play Mode", "When enabled, ink compilation is delayed if in play mode. Files will be compiled on re-entering edit mode."));
            EditorGUILayout.PropertyField(settings.FindProperty("printInkLogsInConsoleOnCompile"), new GUIContent("Print ink TODOs in console on compile", "When enabled, ink lines starting with TODO are printed in the console."));
            EditorGUILayout.PropertyField(settings.FindProperty("handleJSONFilesAutomatically"), new GUIContent("Handle JSON Automatically", "Whether JSON files are moved, renamed and deleted along with their ink files."));
			EditorGUILayout.PropertyField(settings.FindProperty("compileTimeout"), new GUIContent("Compile Timeout", "The max time the compiler will attempt to compile for in case of unhanded errors. You may need to increase this for very large ink projects."));
			EditorGUILayout.PropertyField(settings.FindProperty("suppressStartupWindow"), new GUIContent("Suppress Startup Window", "Prevent the \"what's new\" (the one that appears if you click the \"Show changelog\" button above) appearing when the version of this plugin has changed and Unity is opened. This can be useful for CI/CD pipelines, where auto-launching editor windows can fail to load due to a Unity bug."));
            EditorGUILayout.PropertyField(settings.FindProperty("automaticallyAddDefineSymbols"), new GUIContent("Add define symbols", "If true, automatically adds INK_EDITOR and INK_RUNTIME to the define symbols in the build settings. This is handy for conditional code."));
			//DrawDefineManagerButtons();
			
			EditorGUIUtility.labelWidth = cachedLabelWidth;
			
			EditorGUILayout.Separator();
			DrawRequestButton();
            
			if(EditorGUI.EndChangeCheck()) {
	            if (settings.FindProperty("automaticallyAddDefineSymbols").boolValue) InkDefineSymbols.AddGlobalDefine();
	            else InkDefineSymbols.RemoveGlobalDefine();
	            
				settings.ApplyModifiedProperties();
            }
		}


		static void DrawVersions () {
			EditorGUILayout.LabelField(new GUIContent("Version Info"), EditorStyles.boldLabel);
			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.TextField(new GUIContent("Plugin version", "The version of the Ink Unity Integration package."), InkLibrary.unityIntegrationVersionCurrent.ToString());
			EditorGUILayout.TextField(new GUIContent("Ink version", "The version of ink that is included by the Unity package, used to compile and play ink files."), InkLibrary.inkVersionCurrent.ToString());
			EditorGUILayout.TextField(new GUIContent("Ink story format version", "Significant changes to the Ink runtime are recorded by the story format version.\nCompatibility between different versions is limited; see comments at Ink.Runtime.Story.inkVersionCurrent for more details."), Ink.Runtime.Story.inkVersionCurrent.ToString());
			EditorGUILayout.TextField(new GUIContent("Ink save format version", "Version of the ink save/load system.\nCompatibility between different versions is limited; see comments at Ink.Runtime.StoryState.kInkSaveStateVersion for more details."), Ink.Runtime.StoryState.kInkSaveStateVersion.ToString());
			EditorGUI.EndDisabledGroup();
			if (GUILayout.Button("Show changelog", GUILayout.Width(140))) {
				InkUnityIntegrationStartupWindow.ShowWindow();
			}
		}

		static void DrawDefineManagerButtons() {
			EditorGUILayout.LabelField(new GUIContent("Defines"), EditorStyles.boldLabel);
			var hasDefines = InkDefineSymbols.HasGlobalDefines();
			EditorGUILayout.HelpBox("Adds INK_RUNTIME and INK_EDITOR #defines to the project for the current Build Target.", MessageType.Info);
			EditorGUI.BeginDisabledGroup(hasDefines);
			if (GUILayout.Button(new GUIContent("Add Global Define", "Adds INK_RUNTIME and INK_EDITOR defines to your ProjectSettings for the current Build Target."))) {
				InkDefineSymbols.AddGlobalDefine();
			}
			EditorGUI.EndDisabledGroup();
			EditorGUI.BeginDisabledGroup(!hasDefines);
			if (GUILayout.Button(new GUIContent("Remove Global Define", "Removes INK_RUNTIME and INK_EDITOR defines from your ProjectSettings for the current Build Target."))) {
				InkDefineSymbols.RemoveGlobalDefine();
			}
			EditorGUI.EndDisabledGroup();
		}
		
		static void DrawRequestButton() {
			EditorGUILayout.LabelField(new GUIContent("Support + Requests"), EditorStyles.boldLabel);
			
			EditorGUILayout.LabelField("Is there a setting you'd like? Or a feature you'd like to request?");
			// EditorGUILayout.BeginVertical(GUILayout.Width(220));
			EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("Reach us on Discord", GUILayout.Width(220))) {
                Application.OpenURL("https://discord.gg/inkle");
            }
            if(GUILayout.Button("Submit an issue on GitHub", GUILayout.Width(220))) {
                Application.OpenURL("https://github.com/inkle/ink-unity-integration/issues/new");
            }
			EditorGUILayout.EndHorizontal();
			// EditorGUILayout.EndVertical();
		}

		static void DrawTemplateMissingWarning () {
			EditorGUILayout.HelpBox("Template not found. Ink files created via Assets > Create > Ink will be blank.", MessageType.Info);
		}
	}
}