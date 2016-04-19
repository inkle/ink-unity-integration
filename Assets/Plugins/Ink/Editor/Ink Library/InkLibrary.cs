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
	public static class InkLibrary {
		private static InkLibraryScriptableObject _inkLibrarySO;
		private static InkLibraryScriptableObject inkLibrarySO {
			get {
				if(_inkLibrarySO == null)
					_inkLibrarySO = FindOrCreateLibrary();
				return _inkLibrarySO;
			}
		}
		public const string defaultSettingsPath = "Assets/Plugins/Ink/Editor/Ink Library/InkLibrary.asset";

		public static InkFile[] inkLibrary {
			get {
				return inkLibrarySO.inkLibrary;
			}
		}

		private static InkLibraryScriptableObject FindOrCreateLibrary () {
			InkLibraryScriptableObject tmpSettings = AssetDatabase.LoadAssetAtPath<InkLibraryScriptableObject>(defaultSettingsPath);
			if(tmpSettings == null) {
				string[] GUIDs = AssetDatabase.FindAssets("t:"+typeof(InkLibraryScriptableObject).Name);
				if(GUIDs.Length > 0) {
					string path = AssetDatabase.GUIDToAssetPath(GUIDs[0]);

					if(GUIDs.Length > 1) {
						for(int i = 1; i < GUIDs.Length; i++) {
							AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(GUIDs[i]));
						}
						Debug.LogWarning("More than one InkLibrary was found. Deleted excess libraries.");
					}
				}
				// If we couldn't find the asset in the project, create a new one.
			}
			if(tmpSettings == null) {
				tmpSettings = CreateInkLibrary ();
			}
			return tmpSettings;
		}
		
		private static InkLibraryScriptableObject CreateInkLibrary () {
			var asset = ScriptableObject.CreateInstance<InkLibraryScriptableObject>();
			AssetDatabase.CreateAsset (asset, defaultSettingsPath);
			AssetDatabase.SaveAssets ();
			AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(asset));
			return asset;
		}

		/// <summary>
		/// Updates the ink library. Executed whenever an ink file is changed by InkToJSONPostProcessor
		/// Can be called manually, but incurs a performance cost.
		/// </summary>
		public static void Refresh () {
			string[] inkFilePaths = Directory.GetFiles(Application.dataPath, "*.ink", SearchOption.AllDirectories);
			for (int i = 0; i < inkFilePaths.Length; i++) {
				inkFilePaths [i] = inkFilePaths [i].Replace('\\', '/');
			}

			InkFile[] newInkLibrary = new InkFile[inkFilePaths.Length];
			for (int i = 0; i < inkFilePaths.Length; i++) {
				InkFile inkFile = GetInkFileWithAbsolutePath(inkFilePaths [i]);
				if(inkFile == null)
					inkFile = new InkFile (inkFilePaths [i]);
				newInkLibrary [i] = inkFile;
			}
			foreach (InkFile inkFile in newInkLibrary) {
				if(inkFile.includePaths.Count > 0) {
					inkFile.GetIncludes(newInkLibrary);
				}
			}
			foreach (InkFile inkFile in newInkLibrary) {
				inkFile.FindCompiledJSONAsset();
			}
			inkLibrarySO.inkLibrary = newInkLibrary;
		}

		public static List<InkFile> GetMasterInkFiles () {
			List<InkFile> masterInkFiles = new List<InkFile>();
			if(inkLibrary == null) return masterInkFiles;
			foreach (InkFile inkFile in inkLibrary) {
				if(inkFile.master == null) {
					masterInkFiles.Add(inkFile);
				}
			}
			return masterInkFiles;
		}

		/// <summary>
		/// Gets the ink file with path relative to Assets folder, for example: "Assets/Ink/myStory.ink".
		/// </summary>
		/// <returns>The ink file with path.</returns>
		/// <param name="path">Path.</param>
		public static InkFile GetInkFileWithPath (string path) {
			if(inkLibrary == null) return null;
			foreach(InkFile inkFile in inkLibrary) {
				if(inkFile.filePath == path) {
					return inkFile;
				}
			}
			return null;
		}

		/// <summary>
		/// Gets the ink file with absolute path.
		/// </summary>
		/// <returns>The ink file with path.</returns>
		/// <param name="path">Path.</param>
		public static InkFile GetInkFileWithAbsolutePath (string absolutePath) {
			if(inkLibrary == null) return null;
			foreach(InkFile inkFile in inkLibrary) {
				if(inkFile.absoluteFilePath == absolutePath) {
					return inkFile;
				}
			}
			return null;
		}
	}	
}