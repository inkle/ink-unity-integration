using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

/// <summary>
/// Holds a reference to an InkFile object for every .ink file detected in the Assets folder.
/// Provides helper functions to easily obtain these files.
/// </summary>
namespace Ink.UnityIntegration {
	public class InkLibrary : ScriptableObject {
		public static bool created {
			get {
				// If it's null, there's no InkLibrary asset in the project
				return _Instance != null;
			}
		}
		private static InkLibrary _Instance;
		public static InkLibrary Instance {
			get {
				if(_Instance == null) {
					if(InkEditorUtils.FindOrCreateSingletonScriptableObjectOfType<InkLibrary>(defaultPath, out _Instance)) {
						Rebuild();
					}
				}
				return _Instance;
			}
		}
		public const string defaultPath = "Assets/InkLibrary.asset";

		public List<InkFile> inkLibrary = new List<InkFile>();
		// If InkSettings' delayInPlayMode option is true, dirty files are added here when they're changed in play mode
		// This ensures they're remembered when you exit play mode and can be compiled
		public List<string> pendingCompilationStack = new List<string>();
		// The state of files currently being compiled. You can ignore this!
		public List<InkCompiler.CompilationStackItem> compilationStack = new List<InkCompiler.CompilationStackItem>();

		private void OnEnable() {
			_Instance = this;
		}

		private void OnDisable() {
			_Instance = null;
		}

		/// <summary>
		/// Removes and null references in the library
		/// </summary>
		public static bool Clean () {
            bool wasDirty = false;
			for (int i = InkLibrary.Instance.inkLibrary.Count - 1; i >= 0; i--) {
				InkFile inkFile = InkLibrary.Instance.inkLibrary[i];
				if (inkFile.inkAsset == null) {
					InkLibrary.Instance.inkLibrary.RemoveAt(i);
                    wasDirty = true;
                }
			}
            return wasDirty;
		}

		/// <summary>
		/// Updates the ink library. Executed whenever an ink file is changed by InkToJSONPostProcessor
		/// Can be called manually, but incurs a performance cost.
		/// </summary>
		public static void Rebuild () {
            // Remove any old file connections
            Clean();

            // Add any new file connections (if any are found it replaces the old library entirely)
			string[] inkFilePaths = GetAllInkFilePaths();
			bool inkLibraryChanged = false;
			List<InkFile> newInkLibrary = new List<InkFile>(inkFilePaths.Length);
			for (int i = 0; i < inkFilePaths.Length; i++) {
				InkFile inkFile = GetInkFileWithAbsolutePath(inkFilePaths [i]);
				// If the ink library doesn't have a representation for this file, then make one 
                if(inkFile == null) {
					inkLibraryChanged = true;
					string localAssetPath = InkEditorUtils.AbsoluteToUnityRelativePath(inkFilePaths [i]);
					DefaultAsset inkFileAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(localAssetPath);
					// If the ink file can't be found, it might not yet have been imported. We try to manually import it to fix this.
					if(inkFileAsset == null) {
						AssetDatabase.ImportAsset(localAssetPath);
						inkFileAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(localAssetPath);
						if(inkFileAsset == null) {
							Debug.LogWarning("Ink File Asset not found at "+localAssetPath+". This can occur if the .meta file has not yet been created. This issue should resolve itself, but if unexpected errors occur, rebuild Ink Library using Assets > Recompile Ink");
							continue;
						}
					}
					inkFile = new InkFile(inkFileAsset);
				}
				newInkLibrary.Add(inkFile);
			}
			if(inkLibraryChanged)
				Instance.inkLibrary = newInkLibrary;

            // Validate the meta files
			var metaFiles = Instance.inkLibrary.Select(x => x.metaInfo);
			bool metaFilesChanged = !InkMetaLibrary.Instance.metaLibrary.SequenceEqual(metaFiles);
			if(metaFilesChanged) 
				InkMetaLibrary.Instance.metaLibrary = metaFiles.ToList();

			InkMetaLibrary.RebuildInkFileConnections();

			foreach (InkFile inkFile in Instance.inkLibrary) inkFile.FindCompiledJSONAsset();
			Save();

			Debug.Log("Ink Library was rebuilt.");
		}

		public static void CreateOrReadUpdatedInkFiles (List<string> importedInkAssets) {
			foreach (var importedAssetPath in importedInkAssets) {
				InkFile inkFile = InkLibrary.GetInkFileWithPath(importedAssetPath);
				if(inkFile == null) {
					DefaultAsset asset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(importedAssetPath);
					inkFile = new InkFile(asset);
					Instance.inkLibrary.Add(inkFile);
					InkMetaLibrary.Instance.metaLibrary.Add(new InkMetaFile(inkFile));
				} else {
					inkFile.metaInfo.ParseContent();
				}
			}
			// Now we've updated all the include paths for the ink library we can create master/child references between them.
			InkMetaLibrary.RebuildInkFileConnections();
		}

        // Finds absolute file paths of all the ink files in Application.dataPath
		private static string[] GetAllInkFilePaths () {
			string[] inkFilePaths = Directory.GetFiles(Application.dataPath, "*.ink", SearchOption.AllDirectories);
			for (int i = 0; i < inkFilePaths.Length; i++) {
				inkFilePaths [i] = InkEditorUtils.SanitizePathString(inkFilePaths [i]);
			}
			return inkFilePaths;
		}

		public static void Save () {
			EditorUtility.SetDirty(Instance);
			AssetDatabase.SaveAssets();
			EditorApplication.RepaintProjectWindow();
		}

		// All the master files
		public static IEnumerable<InkFile> GetMasterInkFiles () {
			if(Instance.inkLibrary == null) yield break;
			foreach (InkFile inkFile in Instance.inkLibrary) {
				if(inkFile.metaInfo.isMaster) 
					yield return inkFile;
			}
		}

		// All the master files which are dirty and are set to compile
		public static IEnumerable<InkFile> GetFilesRequiringRecompile () {
			foreach(InkFile inkFile in InkLibrary.GetMasterInkFiles ()) {
				if(inkFile.metaInfo.requiresCompile && (InkSettings.Instance.compileAutomatically || inkFile.compileAutomatically)) 
					yield return inkFile;
			}
		}

		// All the master files which are set to compile
		public static IEnumerable<InkFile> FilesCompiledByRecompileAll () {
			foreach(InkFile inkFile in InkLibrary.GetMasterInkFiles ()) {
				if(InkSettings.Instance.compileAutomatically || inkFile.compileAutomatically) 
					yield return inkFile;
			}
		}

		/// <summary>
		/// Gets the ink file from the .ink file reference.
		/// </summary>
		/// <returns>The ink file with path.</returns>
		/// <param name="file">File asset.</param>
		/// <param name="addIfMissing">Adds the file if missing from inkLibrary.</param>
		public static InkFile GetInkFileWithFile (DefaultAsset file, bool addIfMissing = false) {
			if(InkLibrary.Instance.inkLibrary == null) return null;
			foreach(InkFile inkFile in Instance.inkLibrary) {
				if(inkFile.inkAsset == file) {
					return inkFile;
				}
			}

			if (addIfMissing) {
				InkFile newFile = new InkFile(file);
				Instance.inkLibrary.Add(newFile);
				Debug.Log(file + " missing from ink library. Adding it now.");
				return newFile;
			} else {
				Debug.LogWarning (file + " missing from ink library. Please rebuild.");
				return null;
			}
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
			foreach(InkFile inkFile in Instance.inkLibrary) {
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

		public static InkCompiler.CompilationStackItem GetCompilationStackItem (InkFile inkFile) {
			foreach(var x in Instance.compilationStack) {
				if(x.inkFile == inkFile) 
					return x;
			}
			return null;
		}
	}	
}