using UnityEngine;
using UnityEditor;
using System.Collections;
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
	public class InkLibrary : ScriptableObject, IEnumerable<InkFile> {
		public static System.Version versionCurrent = new System.Version(0,9,61);
		public static bool created {
			get {
				// If it's null, there's no InkLibrary asset in the project
				return _Instance != null;
			}
		}

		static string absoluteSavePath {
			get {
				return System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"..","Library","InkLibrary.asset"));

			}
		}
		private static InkLibrary _Instance;
		public static InkLibrary Instance {
			get {
				if(!created)
                	LoadOrCreateInstance();
				return _Instance;
			} private set {
				if(_Instance == value) return;
				_Instance = value;
				CreateDictionary();
                Validate();
            }
		}

		public List<InkFile> inkLibrary = new List<InkFile>();
		Dictionary<DefaultAsset, InkFile> inkLibraryDictionary;
		// If InkSettings' delayInPlayMode option is true, dirty files are added here when they're changed in play mode
		// This ensures they're remembered when you exit play mode and can be compiled
		public List<string> pendingCompilationStack = new List<string>();
		// The state of files currently being compiled. You can ignore this!
		public List<InkCompiler.CompilationStackItem> compilationStack = new List<InkCompiler.CompilationStackItem>();
		// When compiling we call AssetDatabase.DisallowAutoRefresh. 
		// We NEED to remember to re-allow it or unity stops registering file changes!
		// The issue is that you need to pair calls perfectly, and you can't even use a try-catch to get around it.
		// So - we cache if we've disabled auto refresh here, since this persists across plays.
		// This does have one issue - this setting is saved even when unity re-opens, but the internal asset refresh state isn't.
		// We need this to reset on launching the editor.
		// We currently fix this by setting it false on InkEditorUtils.OnOpenUnityEditor
		// A potentially better approach is to use playerprefs for this, since it's really nothing to do with the library.
		public bool disallowedAutoRefresh;
		
        public int Count {
            get {
                return inkLibrary.Count;
            }
        }
        public InkFile this[int key] {
            get {
                return inkLibrary[key];
            } set {
                inkLibrary[key] = value;
            }
        }
        IEnumerator<InkFile> IEnumerable<InkFile>.GetEnumerator() {
            return inkLibrary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return inkLibrary.GetEnumerator();
        }

		[InitializeOnLoadMethod]
		private static void Initialize() {
			// Not sure we actually need this? It should create itself perfectly well.
			if(!created) LoadOrCreateInstance();
		}

		public static void LoadOrCreateInstance () {
			Object[] objects = UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget(absoluteSavePath);
			if (objects != null && objects.Length > 0) {
				Instance = objects[0] as InkLibrary;
			} else {
				_Instance = ScriptableObject.CreateInstance<InkLibrary>();
				Rebuild();
				SaveToFile();
			}
		}
		
		public static void SaveToFile () {
			UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new[] { Instance }, absoluteSavePath, true);
		}

		static void EnsureCreated () {
			if(!created) LoadOrCreateInstance();
		}

        static void CreateDictionary () {
            Instance.inkLibraryDictionary = new Dictionary<DefaultAsset, InkFile>();
            foreach(var inkFile in Instance.inkLibrary) {
                Instance.inkLibraryDictionary.Add(inkFile.inkAsset, inkFile);
            }
        }
        
		/// <summary>
		/// Checks if the library is corrupt and rebuilds if necessary. Returns true if the library was valid
		/// </summary>
        public static bool Validate () {
            if(RequiresRebuild()) {
                Rebuild();
                Debug.LogWarning("InkLibrary was invalid and has been rebuilt. You can ignore this warning.");
				return false;
            } else {
				return true;
			}
        }
        
		/// <summary>
		/// Checks if the library is corrupt and requires a Rebuild. 
        /// This can happen when asset IDs change, causing the wrong file to be referenced.
        /// This occassionally occurs from source control.
        /// This is a fairly performant check.
		/// </summary>
        static bool RequiresRebuild () {
			EnsureCreated();
			foreach(var inkFile in Instance.inkLibrary) {
                if(inkFile == null) {
                    return true;
                }
                if(inkFile.inkAsset == null) {
                    return true;
                }
                if(!Instance.inkLibraryDictionary.ContainsKey(inkFile.inkAsset)) {
                    return true;
                }
                if(inkFile.inkAsset == null) {
                    return true;
                }
                foreach(var include in inkFile.includes) {
                    if(include == null) {
                        return true;
                    }
                    if(!Instance.inkLibraryDictionary.ContainsKey(include)) {
                        return true;
                    }
                } 
            }
            return false;
        }

		/// <summary>
		/// Removes and null references in the library
		/// </summary>
		public static bool Clean () {
            bool wasDirty = false;
			for (int i = InkLibrary.Instance.Count - 1; i >= 0; i--) {
				InkFile inkFile = InkLibrary.Instance[i];
				if (inkFile.inkAsset == null) {
					InkLibrary.RemoveAt(i);
                    wasDirty = true;
                }
			}
            return wasDirty;
		}

        public static void Add (InkFile inkFile) {
            Instance.inkLibrary.Add(inkFile);
			SortInkLibrary();
			Instance.inkLibraryDictionary.Add(inkFile.inkAsset, inkFile);
        }
        public static void RemoveAt (int index) {
            var inkFile = Instance.inkLibrary[index];
            Instance.inkLibrary.RemoveAt(index);
            Instance.inkLibraryDictionary.Remove(inkFile.inkAsset);
        }
		static void SortInkLibrary () {
            Instance.inkLibrary = Instance.inkLibrary.OrderBy(x => x.filePath).ToList();
		}

		/// <summary>
		/// Updates the ink library. Executed whenever an ink file is changed by InkToJSONPostProcessor
		/// Can be called manually, but incurs a performance cost.
		/// </summary>
		public static void Rebuild () {
			// Disable the asset post processor in case any assetdatabase functions called as a result of this would cause further operations.
			InkPostProcessor.disabled = true;
			
            // Remove any old file connections
            Clean();

			// Reset the asset name
			Instance.name = "Ink Library "+versionCurrent.ToString();
            
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
			if(inkLibraryChanged) {
				Instance.inkLibrary = newInkLibrary;
				SortInkLibrary();
			}
            CreateDictionary();

			RebuildInkFileConnections();

			foreach (InkFile inkFile in Instance.inkLibrary) inkFile.FindCompiledJSONAsset();
			SaveToFile();
			
			// Re-enable the ink asset post processor
			InkPostProcessor.disabled = false;
			Debug.Log("Ink Library was rebuilt.");
		}

		public static void CreateOrReadUpdatedInkFiles (List<string> importedInkAssets) {
			foreach (var importedAssetPath in importedInkAssets) {
				InkFile inkFile = InkLibrary.GetInkFileWithPath(importedAssetPath);
				if(inkFile == null) {
					DefaultAsset asset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(importedAssetPath);
					inkFile = new InkFile(asset);
					Add(inkFile);
				} else {
					inkFile.ParseContent();
				}
			}
			// Now we've updated all the include paths for the ink library we can create master/child references between them.
			RebuildInkFileConnections();
		}

        // Finds absolute file paths of all the ink files in Application.dataPath
		private static string[] GetAllInkFilePaths () {
			string[] inkFilePaths = Directory.GetFiles(Application.dataPath, "*.ink", SearchOption.AllDirectories);
			for (int i = 0; i < inkFilePaths.Length; i++) {
				inkFilePaths [i] = InkEditorUtils.SanitizePathString(inkFilePaths [i]);
			}
			return inkFilePaths;
		}

		// All the master files
		public static IEnumerable<InkFile> GetMasterInkFiles () {
			if(Instance.inkLibrary == null) yield break;
			foreach (InkFile inkFile in Instance.inkLibrary) {
				if(inkFile.isMaster) 
					yield return inkFile;
			}
		}

		// All the master files which are dirty and are set to compile
		public static IEnumerable<InkFile> GetFilesRequiringRecompile () {
			foreach(InkFile inkFile in InkLibrary.GetMasterInkFiles ()) {
				if(inkFile.requiresCompile && (InkSettings.Instance.compileAutomatically || inkFile.compileAutomatically)) 
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
		/// <param name="path">Path.</param>
		public static InkFile GetInkFileWithFile (DefaultAsset file) {
			if(InkLibrary.Instance.inkLibrary == null) return null;
            if(Instance.inkLibraryDictionary == null) {
				Debug.LogWarning("GetInkFileWithFile: inkLibraryDictionary was null! This should never occur, but is handled following a user reported bug. If this has never been seen long after 12/08/2020, it can be safely removed");
				CreateDictionary();
			}
			foreach(InkFile inkFile in Instance.inkLibrary) {
				if(inkFile.inkAsset == file) {
					return inkFile;
				}
			}
			System.Text.StringBuilder listOfFiles = new System.Text.StringBuilder();
			foreach(InkFile inkFile in Instance.inkLibrary) {
				listOfFiles.AppendLine(inkFile.ToString());
			}
			Debug.LogWarning (file + " missing from ink library. Please rebuild.\n"+listOfFiles);
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
			foreach(InkFile inkFile in Instance.inkLibrary) {
				if(inkFile.absoluteFilePath == absolutePath) {
					return inkFile;
				}
			}
			return null;
		}


		/// <summary>
		/// Rebuilds which files are master files and the connections between the files.
		/// </summary>
		public static void RebuildInkFileConnections () {
			foreach (InkFile inkFile in InkLibrary.Instance.inkLibrary) {
				inkFile.parents = new List<DefaultAsset>();
				inkFile.masterInkAssets = new List<DefaultAsset>();
				inkFile.ParseContent();
				inkFile.FindIncludedFiles();
			}
			// We now set the master file for ink files. As a file can be in an include hierarchy, we need to do this in two passes.
			// First, we set the master file to the file that includes an ink file.
			foreach (InkFile inkFile in InkLibrary.Instance.inkLibrary) {
				if(inkFile.includes.Count == 0) 
					continue;
				foreach (InkFile otherInkFile in InkLibrary.Instance.inkLibrary) {
					if(inkFile == otherInkFile) 
						continue;
					if(inkFile.includes.Contains(otherInkFile.inkAsset)) {
						if(!otherInkFile.parents.Contains(inkFile.inkAsset)) {
							otherInkFile.parents.Add(inkFile.inkAsset);
						}
					}
				}
			}
			// Next, we create a list of all the files owned by the actual master file, which we obtain by travelling up the parent tree from each file.
			Dictionary<InkFile, List<InkFile>> masterChildRelationships = new Dictionary<InkFile, List<InkFile>>();
			foreach (InkFile inkFile in InkLibrary.Instance.inkLibrary) {
				foreach(var parentInkFile in inkFile.parentInkFiles) {
					InkFile lastMasterInkFile = parentInkFile;
					InkFile masterInkFile = parentInkFile;
					while (masterInkFile.parents.Count != 0) {
						// This shouldn't just pick first, but iterate the whole lot! 
						// I didn't feel like writing a recursive algorithm until it's actually needed though - a file included by several parents is already a rare enough case!
						masterInkFile = masterInkFile.parentInkFiles.First();
						lastMasterInkFile = masterInkFile;
					}
					if(lastMasterInkFile.parents.Count > 1) {
						Debug.LogError("The ink ownership tree has another master file that is not discovered! This is an oversight of the current implementation. If you requres this feature, please take a look at the comment in the code above - if you solve it let us know and we'll merge it in!");
					}
					if(!masterChildRelationships.ContainsKey(masterInkFile)) {
						masterChildRelationships.Add(masterInkFile, new List<InkFile>());
					}
					masterChildRelationships[masterInkFile].Add(inkFile);
				}

				// if(inkFile.parent == null) 
				// 	continue;
				// InkFile parent = inkFile.parentInkFile;
				// while (parent.metaInfo.parent != null) {
				// 	parent = parent.metaInfo.parentInkFile;
				// }
				// if(!masterChildRelationships.ContainsKey(parent)) {
				// 	masterChildRelationships.Add(parent, new List<InkFile>());
				// }
				// masterChildRelationships[parent].Add(inkFile);
			}
			// Finally, we set the master file of the children
			foreach (var inkFileRelationship in masterChildRelationships) {
				foreach(InkFile childInkFile in inkFileRelationship.Value) {
					if(!childInkFile.masterInkAssets.Contains(inkFileRelationship.Key.inkAsset)) {
						childInkFile.masterInkAssets.Add(inkFileRelationship.Key.inkAsset);
					} else {
						Debug.LogWarning("Child file already contained master file reference! This is weird!");
					}
					if(InkSettings.Instance.handleJSONFilesAutomatically && childInkFile.jsonAsset != null) {
						AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(childInkFile.jsonAsset));
						childInkFile.jsonAsset = null;
					}
				}
			}
		}


		


        public static void AddToPendingCompilationStack (string filePath) {
			if(!InkLibrary.Instance.pendingCompilationStack.Contains(filePath)) {
				InkLibrary.Instance.pendingCompilationStack.Add(filePath);
				SaveToFile();
			}
		}
        public static void RemoveFromPendingCompilationStack (InkFile inkFile) {
            InkLibrary.Instance.pendingCompilationStack.Remove(inkFile.filePath);
            foreach(var includeFile in inkFile.inkFilesInIncludeHierarchy) {
                InkLibrary.Instance.pendingCompilationStack.Remove(includeFile.filePath);
            }
			InkLibrary.SaveToFile();
        }

		public static int NumFilesInCompilingStackInState (InkCompiler.CompilationStackItem.State state) {
			int count = 0;
			foreach(var x in Instance.compilationStack) {
				if(x.state == state) 
					count++;
			}
			return count;
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