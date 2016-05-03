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
				InkLibrary tmpSettings = AssetDatabase.LoadAssetAtPath<InkLibrary>(defaultSettingsPath);
				if(tmpSettings != null) 
					return true;
				string[] GUIDs = AssetDatabase.FindAssets("t:"+typeof(InkLibrary).Name);
				if(GUIDs.Length > 0)
					return true;
				return false;
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
		public const string defaultSettingsPath = "Assets/Plugins/Ink/Editor/Ink Library/InkLibrary.asset";

		public bool compileAutomatically = true;
		public List<InkFile> inkLibrary = new List<InkFile>();
		public List<InkCompiler.CompilationStackItem> compilationStack = new List<InkCompiler.CompilationStackItem>();

		private static InkLibrary FindOrCreateLibrary () {
			InkLibrary tmpSettings = AssetDatabase.LoadAssetAtPath<InkLibrary>(defaultSettingsPath);
			if(tmpSettings == null) {
				string[] GUIDs = AssetDatabase.FindAssets("t:"+typeof(InkLibrary).Name);
				if(GUIDs.Length > 0) {
					string path = AssetDatabase.GUIDToAssetPath(GUIDs[0]);
					tmpSettings = AssetDatabase.LoadAssetAtPath<InkLibrary>(path);
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
				InkLibrary.Refresh();
				Debug.LogWarning("No ink library was found. Created new at "+defaultSettingsPath);
			}
			return tmpSettings;
		}
		
		private static InkLibrary CreateInkLibrary () {
			var asset = ScriptableObject.CreateInstance<InkLibrary>();
			AssetDatabase.CreateAsset (asset, defaultSettingsPath);
			AssetDatabase.SaveAssets ();
			AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(asset));
			return asset;
		}

		public static string[] GetAllInkFilePaths () {
			string[] inkFilePaths = Directory.GetFiles(Application.dataPath, "*.ink", SearchOption.AllDirectories);
			for (int i = 0; i < inkFilePaths.Length; i++) {
				inkFilePaths [i] = inkFilePaths [i].Replace('\\', '/');
			}
			return inkFilePaths;
		}
		/// <summary>
		/// Updates the ink library. Executed whenever an ink file is changed by InkToJSONPostProcessor
		/// Can be called manually, but incurs a performance cost.
		/// </summary>
		public static void Refresh () {
			Debug.Log("Refreshing Ink Library");
			string[] inkFilePaths = GetAllInkFilePaths();

			List<InkFile> newInkLibrary = new List<InkFile>(inkFilePaths.Length);
			for (int i = 0; i < inkFilePaths.Length; i++) {
				InkFile inkFile = GetInkFileWithAbsolutePath(inkFilePaths [i]);
				if(inkFile == null) 
					inkFile = new InkFile(AssetDatabase.LoadAssetAtPath<DefaultAsset>(inkFilePaths [i].Substring(Application.dataPath.Length-6)));
				else
					inkFile.circularIncludeReferences.Clear();
				newInkLibrary.Add(inkFile);
			}


			InkLibrary.Instance.inkLibrary = newInkLibrary;

			foreach (InkFile inkFile in InkLibrary.Instance.inkLibrary) {
				inkFile.fileContents = File.OpenText(inkFile.absoluteFilePath).ReadToEnd();
				inkFile.GetIncludedFiles();
			}
			RebuildMasterFiles();
			foreach (InkFile inkFile in InkLibrary.Instance.inkLibrary) {
				inkFile.FindCompiledJSONAsset();
			}

			EditorUtility.SetDirty(InkLibrary.Instance);
			AssetDatabase.SaveAssets();
		}

		/// <summary>
		/// Rebuilds which files are master files.
		/// </summary>
		public static void RebuildMasterFiles () {
			foreach (InkFile inkFile in InkLibrary.Instance.inkLibrary)
				inkFile.master = null;
			foreach (InkFile inkFile in InkLibrary.Instance.inkLibrary) {
				if(inkFile.includes.Count == 0) 
					continue;
				foreach (InkFile otherInkFile in InkLibrary.Instance.inkLibrary) {
					if(inkFile == otherInkFile) 
						continue;
					if(inkFile.includes.Contains(otherInkFile.inkAsset)) {
						otherInkFile.master = inkFile.inkAsset;
					}
				}
			}
		}
		public static List<InkFile> GetMasterInkFiles () {
			List<InkFile> masterInkFiles = new List<InkFile>();
			if(InkLibrary.Instance.inkLibrary == null) return masterInkFiles;
			foreach (InkFile inkFile in InkLibrary.Instance.inkLibrary) {
				if(inkFile.isMaster) {
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
			if(InkLibrary.Instance.inkLibrary == null) return null;
			foreach(InkFile inkFile in InkLibrary.Instance.inkLibrary) {
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
			foreach(var x in InkLibrary.Instance.compilationStack) {
				if(x.state == state) 
					items.Add(x);
			}
			return items;
		}

		public static InkCompiler.CompilationStackItem GetCompilationStackItem (string inkAbsoluteFilePath) {
			foreach(var x in InkLibrary.Instance.compilationStack) {
				if(x.inkAbsoluteFilePath == inkAbsoluteFilePath) 
					return x;
			}
			Debug.LogError("Fatal Error compiling Ink! No file found! Please report this as a bug. "+inkAbsoluteFilePath);
			return null;
		}

		public static InkCompiler.CompilationStackItem GetCompilationStackItem (InkFile inkFile) {
			foreach(var x in InkLibrary.Instance.compilationStack) {
				if(x.inkFile == inkFile) 
					return x;
			}
			return null;
		}
	}	
}