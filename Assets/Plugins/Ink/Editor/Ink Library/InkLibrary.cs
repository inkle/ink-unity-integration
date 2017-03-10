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
	public class InkLibrary : ScriptableObject {
		public static bool created {
			get {
				return _Instance != null || FindLibrary() != null;
			}
		}
		private static InkLibrary _Instance;
		public static InkLibrary Instance {
			get {
				if(_Instance == null)
					_Instance = FindOrCreateLibrary();
				return _Instance;
			}
		}
		public const string defaultPath = "Assets/InkLibrary.asset";
		public const string pathPlayerPrefsKey = "InkLibraryAssetPath";

		public List<InkFile> inkLibrary = new List<InkFile>();
		public List<InkCompiler.CompilationStackItem> compilationStack = new List<InkCompiler.CompilationStackItem>();

		/// <summary>
		/// Removes and null references in the library
		/// </summary>
		public static void Clean () {
			for (int i = InkLibrary.Instance.inkLibrary.Count - 1; i >= 0; i--) {
				InkFile inkFile = InkLibrary.Instance.inkLibrary[i];
				if (inkFile.inkAsset == null)
					InkLibrary.Instance.inkLibrary.RemoveAt(i);
			}
		}

		private static InkLibrary FindLibrary () {
			if(EditorPrefs.HasKey(pathPlayerPrefsKey)) {
				InkLibrary library = AssetDatabase.LoadAssetAtPath<InkLibrary>(EditorPrefs.GetString(pathPlayerPrefsKey));
				if(library != null) return library;
				else EditorPrefs.DeleteKey(pathPlayerPrefsKey);
			}

			string[] GUIDs = AssetDatabase.FindAssets("t:"+typeof(InkLibrary).Name);
			if(GUIDs.Length > 0) {
				if(GUIDs.Length > 1) {
					for(int i = 1; i < GUIDs.Length; i++) {
						AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(GUIDs[i]));
					}
					Debug.LogWarning("More than one InkLibrary was found. Deleted excess asset instances.");
				}
				string path = AssetDatabase.GUIDToAssetPath(GUIDs[0]);
				EditorPrefs.SetString(pathPlayerPrefsKey, path);
				return AssetDatabase.LoadAssetAtPath<InkLibrary>(path);
			}
			return null;
		}

		private static InkLibrary FindOrCreateLibrary () {
			InkLibrary tmpSettings = FindLibrary();
			// If we couldn't find the asset in the project, create a new one.
			if(tmpSettings == null) {
				tmpSettings = CreateInkLibrary ();
				Debug.Log("Created a new InkLibrary file at "+defaultPath+" because one was not found.");
				InkLibrary.Rebuild();
			}
			return tmpSettings;
		}
		
		private static InkLibrary CreateInkLibrary () {
			var asset = ScriptableObject.CreateInstance<InkLibrary>();
			AssetDatabase.CreateAsset (asset, defaultPath);
			AssetDatabase.SaveAssets ();
			AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(asset));
			EditorPrefs.SetString(pathPlayerPrefsKey, defaultPath);
			return asset;
		}

		public static string[] GetAllInkFilePaths () {
			string[] inkFilePaths = Directory.GetFiles(Application.dataPath, "*.ink", SearchOption.AllDirectories);
			for (int i = 0; i < inkFilePaths.Length; i++) {
				inkFilePaths [i] = InkEditorUtils.SanitizePathString(inkFilePaths [i]);
			}
			return inkFilePaths;
		}

		/// <summary>
		/// Updates the ink library. Executed whenever an ink file is changed by InkToJSONPostProcessor
		/// Can be called manually, but incurs a performance cost.
		/// </summary>
		public static void Rebuild () {
			Debug.Log("Rebuilding Ink Library...");

			string[] inkFilePaths = GetAllInkFilePaths();

			List<InkFile> newInkLibrary = new List<InkFile>(inkFilePaths.Length);
			for (int i = 0; i < inkFilePaths.Length; i++) {
				InkFile inkFile = GetInkFileWithAbsolutePath(inkFilePaths [i]);
				if(inkFile == null) {
					string localAssetPath = inkFilePaths [i].Substring(Application.dataPath.Length-6);
					DefaultAsset inkFileAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(localAssetPath);
					// If the ink file can't be found, it might not yet have been imported. We try to manually import it to fix this.
					if(inkFileAsset == null) {
						AssetDatabase.ImportAsset(localAssetPath);
						inkFileAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(localAssetPath);
						if(inkFileAsset == null) {
							Debug.LogWarning("Ink File Asset not found at "+localAssetPath+". This can occur if the .meta file has not yet been created. This issue should resolve itself, but if unexpected errors occur, rebuild Ink Library using  > Recompile Ink");
							continue;
						}
					}
					inkFile = new InkFile(inkFileAsset);
				}
				newInkLibrary.Add(inkFile);
			}

			Instance.inkLibrary = newInkLibrary;

			InkMetaLibrary.Instance.metaLibrary.Clear();
			foreach (InkFile inkFile in Instance.inkLibrary) {
				InkMetaLibrary.Instance.metaLibrary.Add(inkFile.metaInfo);
			}
			InkMetaLibrary.RebuildInkFileConnections();

			foreach (InkFile inkFile in Instance.inkLibrary) {
				inkFile.FindCompiledJSONAsset();
			}
			Save();
		}

		public static void Save () {
			EditorUtility.SetDirty(Instance);
			AssetDatabase.SaveAssets();
			EditorApplication.RepaintProjectWindow();
		}

		public static List<InkFile> GetMasterInkFiles () {
			List<InkFile> masterInkFiles = new List<InkFile>();
			if(Instance.inkLibrary == null) return masterInkFiles;
			foreach (InkFile inkFile in Instance.inkLibrary) {
				if(inkFile.metaInfo.isMaster) {
					masterInkFiles.Add(inkFile);
				}
			}
			return masterInkFiles;
		}

		/// <summary>
		/// Gets the ink file from the .ink file reference.
		/// </summary>
		/// <returns>The ink file with path.</returns>
		/// <param name="path">Path.</param>
		public static InkFile GetInkFileWithFile (DefaultAsset file) {
			if(InkLibrary.Instance.inkLibrary == null) return null;
			foreach(InkFile inkFile in InkLibrary.Instance.inkLibrary) {
				if(inkFile.inkAsset == file) {
					return inkFile;
				}
			}
			return null;
		}

		/// <summary>
		/// Gets the ink file with path relative to Assets folder, for example: "Assets/Ink/myStory.ink".
		/// </summary>
		/// <returns>The ink file with path.</returns>
		/// <param name="path">Path.</param>
		public static InkFile GetInkFileWithPath (string path) {
			if(Instance.inkLibrary == null) return null;
			foreach(InkFile inkFile in Instance.inkLibrary) {
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
			if(InkLibrary.Instance.inkLibrary == null) return null;
			foreach(InkFile inkFile in InkLibrary.Instance.inkLibrary) {
				if(inkFile.absoluteFilePath == absolutePath) {
					return inkFile;
				}
			}
			return null;
		}

		public static List<InkCompiler.CompilationStackItem> FilesInCompilingStackInState (InkCompiler.CompilationStackItem.State state) {
			List<InkCompiler.CompilationStackItem> items = new List<InkCompiler.CompilationStackItem>();
			foreach(var x in Instance.compilationStack) {
				if(x.state == state) 
					items.Add(x);
			}
			return items;
		}

		public static InkCompiler.CompilationStackItem GetCompilationStackItem (string inkAbsoluteFilePath) {
			foreach(var x in Instance.compilationStack) {
				if(x.inkAbsoluteFilePath == inkAbsoluteFilePath) 
					return x;
			}
			Debug.LogError("Fatal Error compiling Ink! No file found! Please report this as a bug. "+inkAbsoluteFilePath);
			return null;
		}

		public static InkCompiler.CompilationStackItem GetCompilationStackItem (InkFile inkFile) {
			foreach(var x in Instance.compilationStack) {
				if(x.inkFile == inkFile) 
					return x;
			}
			return null;
		}
	}	
}