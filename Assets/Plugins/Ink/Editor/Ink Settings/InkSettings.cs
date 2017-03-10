using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using UnityEditorInternal;
using Debug = UnityEngine.Debug;
using Ink.Runtime;

/// <summary>
/// Holds a reference to an InkFile object for every .ink file detected in the Assets folder.
/// Provides helper functions to easily obtain these files.
/// </summary>
namespace Ink.UnityIntegration {
	public class InkSettings : ScriptableObject {
		public static bool created {
			get {
				return _Instance != null || FindSettings() != null;
			}
		}
		private static InkSettings _Instance;
		public static InkSettings Instance {
			get {
				if(_Instance == null)
					_Instance = FindOrCreateSettings();
				return _Instance;
			}
		}
		public const string defaultPath = "Assets/InkSettings.asset";
		public const string pathPlayerPrefsKey = "InkSettingsAssetPath";

		public TextAsset templateFile;
		public string templateFilePath {
			get {
				if(templateFile == null) return "";
				else return AssetDatabase.GetAssetPath(templateFile);
			}
		}

		public bool compileAutomatically = true;
		public bool handleJSONFilesAutomatically = true;

		public CustomInklecateOptions customInklecateOptions = new CustomInklecateOptions();
		[System.Serializable]
		public class CustomInklecateOptions {
			public bool runInklecateWithMono;
			public string additionalCompilerOptions;
			public DefaultAsset inklecate;
		}

		[MenuItem("Edit/Project Settings/Ink", false, 500)]
		public static void SelectFromProjectSettings() {
			Selection.activeObject = Instance;
		}

		private static InkSettings FindSettings () {
			if(EditorPrefs.HasKey(pathPlayerPrefsKey)) {
				InkSettings settings = AssetDatabase.LoadAssetAtPath<InkSettings>(EditorPrefs.GetString(pathPlayerPrefsKey));
				if(settings != null) return settings;
				else EditorPrefs.DeleteKey(pathPlayerPrefsKey);
			}

			string[] GUIDs = AssetDatabase.FindAssets("t:"+typeof(InkSettings).Name);
			if(GUIDs.Length > 0) {
				if(GUIDs.Length > 1) {
					for(int i = 1; i < GUIDs.Length; i++) {
						AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(GUIDs[i]));
					}
					Debug.LogWarning("More than one InkSettings was found. Deleted excess asset instances.");
				}
				string path = AssetDatabase.GUIDToAssetPath(GUIDs[0]);
				EditorPrefs.SetString(pathPlayerPrefsKey, path);
				return AssetDatabase.LoadAssetAtPath<InkSettings>(path);
			}
			return null;
		}

		private static InkSettings FindOrCreateSettings () {
			InkSettings tmpSettings = FindSettings();
			// If we couldn't find the asset in the project, create a new one.
			if(tmpSettings == null) {
				tmpSettings = CreateInkSettings ();
				Debug.Log("Created a new InkSettings file at "+defaultPath+" because one was not found.");
			}
			return tmpSettings;
		}
		
		private static InkSettings CreateInkSettings () {
			var asset = ScriptableObject.CreateInstance<InkSettings>();
			AssetDatabase.CreateAsset (asset, defaultPath);
			AssetDatabase.SaveAssets ();
			AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(asset));
			EditorPrefs.SetString(pathPlayerPrefsKey, defaultPath);
			return asset;
		}

		private static void Save () {
			EditorUtility.SetDirty(Instance);
			AssetDatabase.SaveAssets();
			EditorApplication.RepaintProjectWindow();
		}
	}	
}