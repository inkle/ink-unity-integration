using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Debug = UnityEngine.Debug;

/// <summary>
/// Holds a reference to an InkFile object for every .ink file detected in the Assets folder.
/// Provides helper functions to easily obtain these files.
/// </summary>
namespace Ink.UnityIntegration {
    #if UNITY_2020_1_OR_NEWER
    [FilePath("Library/asset", FilePathAttribute.Location.ProjectFolder)]
	public class InkLibrary : ScriptableSingleton<InkLibrary>, IEnumerable<InkFile> {
    #else
	public class InkLibrary : ScriptableObject, IEnumerable<InkFile> {
    #endif
        // Ink version. This should really come from the core ink code.
		public static System.Version inkVersionCurrent = new System.Version(1,2,0);
		public static System.Version unityIntegrationVersionCurrent = new System.Version(1,2,1);

		static string absoluteSavePath {
			get {
				return System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(),"Library","asset"));
			}
		}
		
		#if !UNITY_2020_1_OR_NEWER
		public static bool created {
			get {
				// If it's null, there's no InkLibrary loaded
				return (_instance != (Object) null);
			}
		}
		private static InkLibrary _instance;
		public static InkLibrary instance {
			get {
				if(!created)
                	LoadOrCreateInstance();
				return _instance;
			} private set {
				if(_instance == value) return;
				_instance = value;
            }
		}
        
		
		// This occurs on recompile, creation and load (note that data has not necessarily been loaded at this point!)
		protected InkLibrary () {
			if (created)
				Debug.LogError((object) "ScriptableSingleton already exists. Did you query the singleton in a constructor?");
			else {
				instance = this;
			}
		}

		public static void LoadOrCreateInstance () {
			InternalEditorUtility.LoadSerializedFileAndForget(absoluteSavePath);
			if(created) {
				if(InkEditorUtils.isFirstCompile) {
					Validate();
				}
			} else {
				instance = ScriptableObject.CreateInstance<InkLibrary>();
				instance.hideFlags = HideFlags.HideAndDontSave;
				Rebuild();
			}
		}
		
		public void Save (bool saveAsText) {
			if(!created) return;			
			InternalEditorUtility.SaveToSerializedFileAndForget((Object[]) new InkLibrary[1] {instance}, absoluteSavePath, saveAsText);
		}

		static void EnsureCreated () {
			if(!created) LoadOrCreateInstance();
		}
        #endif
        
        public class AssetSaver : UnityEditor.AssetModificationProcessor {
            static string[] OnWillSaveAssets(string[] paths) {
                instance.Save(true);
                return paths;
            }
        }

		public List<InkFile> inkLibrary = new List<InkFile>();
		Dictionary<DefaultAsset, InkFile> inkLibraryDictionary = new Dictionary<DefaultAsset, InkFile>();
		
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

		void OnValidate () {
            BuildLookupDictionary();
            // This is experimental! I'd like to see if it fixes the issue where assets have not yet been imported.
            EditorApplication.delayCall += () => {
                Validate();
            };
        }

        static void BuildLookupDictionary () {
            instance.inkLibraryDictionary.Clear();
			foreach(var inkFile in instance.inkLibrary) {
                instance.inkLibraryDictionary.Add(inkFile.inkAsset, inkFile);
            }
        }
        
		/// <summary>
		/// Checks if the library is corrupt and rebuilds if necessary. Returns true if the library was valid
		/// </summary>
        public static bool Validate () {
            if(RequiresRebuild()) {
                Rebuild();
                Debug.LogWarning("InkLibrary was invalid and has been rebuilt. This can occur if files are moved/deleted while the editor is closed. You can ignore this warning.");
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
            #if !UNITY_2020_1_OR_NEWER
			EnsureCreated();
            #endif
			foreach(var inkFile in instance.inkLibrary) {
                if(inkFile == null) {
                    return true;
                }
                if(inkFile.inkAsset == null) {
                    // This can occur when the asset has not yet been imported!
                    return true;
                }
                if(!instance.inkLibraryDictionary.ContainsKey(inkFile.inkAsset)) {
                    return true;
                }
                foreach(var include in inkFile.includes) {
                    if(include == null) {
                        return true;
                    }
                    if(!instance.inkLibraryDictionary.ContainsKey(include)) {
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
			for (int i = instance.Count - 1; i >= 0; i--) {
				InkFile inkFile = instance[i];
				if (inkFile.inkAsset == null) {
					RemoveAt(i);
                    wasDirty = true;
                }
			}
            return wasDirty;
		}

        static void Add (InkFile inkFile) {
            instance.inkLibrary.Add(inkFile);
			SortInkLibrary();
			instance.inkLibraryDictionary.Add(inkFile.inkAsset, inkFile);
        }
        public static void RemoveAt (int index) {
            var inkFile = instance.inkLibrary[index];
            instance.inkLibrary.RemoveAt(index);
            instance.inkLibraryDictionary.Remove(inkFile.inkAsset);
        }
		static void SortInkLibrary () {
            instance.inkLibrary = instance.inkLibrary.OrderBy(x => x.filePath).ToList();
		}

		/// <summary>
		/// Updates the ink library. Executed whenever an ink file is changed by InkToJSONPostProcessor
		/// Can be called manually, but incurs a performance cost.
		/// </summary>
		public static void Rebuild () {
			// Disable the asset post processor in case any assetdatabase functions called as a result of this would cause further operations.
			InkPostProcessor.disabled = true;
			
            // Clear the old data
            instance.inkLibrary.Clear();
            instance.inkLibraryDictionary.Clear();

			// Reset the asset name
			instance.name = "Ink Library "+unityIntegrationVersionCurrent.ToString();
            
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
                            // If this occurs as a result assets not having been imported before OnValidate => Validate we should return immediately and set a flag to true.
                            // If an asset import is detected immediately after this via InkPostProcessor, then this rebuild may (will?) have been unnecessary anyway.
                            // At time of writing (11/05/21) I've not done this and am locally toying with EditorApplication.delayCall in OnValidate.
							Debug.LogWarning("Ink File Asset not found at "+localAssetPath+". This can occur if the .meta file has not yet been created. This issue should resolve itself, but if unexpected errors occur, rebuild Ink Library using Assets > Recompile Ink");
							continue;
						}
					}
					inkFile = new InkFile(inkFileAsset);
				}
                newInkLibrary.Add(inkFile);
			}
			if(inkLibraryChanged) {
				instance.inkLibrary = newInkLibrary;
				SortInkLibrary();
			}
            BuildLookupDictionary();

			RebuildInkFileConnections();

			foreach (InkFile inkFile in instance.inkLibrary) inkFile.FindCompiledJSONAsset();

			// if(InkSettings.instance.handleJSONFilesAutomatically) DeleteUnwantedCompiledJSONAssets();
			
			instance.Save(true);
			
			// Re-enable the ink asset post processor
			InkPostProcessor.disabled = false;
			Debug.Log("Ink Library was rebuilt.\n"+instance.inkLibrary.Count+" ink files are currently tracked.");
		}

		// To be used when adding .ink files. 
		// This process is typically handled by CreateOrReadUpdatedInkFiles, called from InkPostProcessor; but it may be desired to remove/disable the post processor.
		// In those cases, this is the correct way to ensure the ink library correctly processes the file.
		public static InkFile AddNewInkFile (DefaultAsset asset) {
			Debug.Assert(asset != null);
			// First, check if we've already got it in the library!
			foreach(var _inkFile in instance)
				if(_inkFile.inkAsset == asset)
					return _inkFile;
			// If not
			var inkFile = new InkFile(asset);
			inkFile.FindCompiledJSONAsset();
			Add(inkFile);
			RebuildInkFileConnections();
			return inkFile;
		}

		// This is called from InkPostProcessor after ink file(s) has been added/changed.
		public static void CreateOrReadUpdatedInkFiles (List<string> importedInkAssets) {
            for (int i = 0; i < importedInkAssets.Count; i++) {
                string importedAssetPath = importedInkAssets[i];
                InkFile inkFile = GetInkFileWithPath(importedAssetPath);
				if(inkFile == null) {
					DefaultAsset asset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(importedAssetPath);
					if(asset == null) {
						// This file wasn't found! This is a rare bug. We remove the file from the list in this case, preventing it from causing further bugs.
						importedInkAssets.RemoveAt(i);
						i--;
						Debug.LogWarning("InkLibrary failed to load ink file at "+importedAssetPath+". It has been removed from the list of files. You can ignore this warning.");
					} else {
						// New file; create and add InkFile to represent it. Content is read in InkFile constructor.
						inkFile = new InkFile(asset);
						inkFile.FindCompiledJSONAsset();
						Add(inkFile);
					}
				} else {
					// Read content
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
			if(instance.inkLibrary == null) yield break;
			foreach (InkFile inkFile in instance.inkLibrary) {
				if(inkFile.isMaster) 
					yield return inkFile;
			}
		}

		public static IEnumerable<InkFile> GetInkFilesMarkedToCompileAsMasterFiles () {
			if(instance.inkLibrary == null) yield break;
			foreach (InkFile inkFile in instance.inkLibrary) {
				if(inkFile.isMaster) 
					yield return inkFile;
			}
		}

		// All the master files which are dirty and are set to compile
		public static IEnumerable<InkFile> GetFilesRequiringRecompile () {
			foreach(InkFile inkFile in GetInkFilesMarkedToCompileAsMasterFiles ()) {
				if(InkSettings.instance.ShouldCompileInkFileAutomatically(inkFile) && inkFile.requiresCompile) 
					yield return inkFile;
			}
		}

		// All the master files which are set to compile
		public static IEnumerable<InkFile> FilesCompiledByRecompileAll () {
			foreach(InkFile inkFile in GetInkFilesMarkedToCompileAsMasterFiles ()) {
				if(InkSettings.instance.ShouldCompileInkFileAutomatically(inkFile)) 
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
			if(instance.inkLibrary == null) return null;
			
			if (!file) {
				Debug.LogError("Can't add null file.");
				return null;
			}

            if(instance.inkLibraryDictionary == null) {
				Debug.LogWarning("GetInkFileWithFile: inkLibraryDictionary was null! This should never occur, but is handled following a user reported bug. If this has never been seen long after 12/08/2020, it can be safely removed");
				BuildLookupDictionary();
			}
			foreach(InkFile inkFile in instance.inkLibrary) {
				if(inkFile.inkAsset == file) {
					return inkFile;
				}
			}

			var missingFileHasProperFileExtension = Path.GetExtension(AssetDatabase.GetAssetPath(file)) == InkEditorUtils.inkFileExtension;
			if (addIfMissing) {
				InkFile newFile = new InkFile(file);
				instance.inkLibrary.Add(newFile);
				if(missingFileHasProperFileExtension) Debug.Log(file + " missing from ink library. Adding it now.");
				else Debug.LogWarning("File "+file + " is missing the .ink extension, but is believed to be an ink file. All ink files should use the .ink file extension! A common effect of this is forcing the InkLibrary to rebuild unexpectedly when the file is detected as an include of another file.");
				return newFile;
			}

			System.Text.StringBuilder listOfFiles = new System.Text.StringBuilder();
			foreach(InkFile inkFile in instance.inkLibrary) {
				listOfFiles.AppendLine(inkFile.ToString());
			}
			if(missingFileHasProperFileExtension) Debug.LogWarning (file + " missing from ink library. Please rebuild.\nFiles in Library:\n"+listOfFiles);
			else Debug.LogWarning (file + " is missing from ink library. It is also missing the .ink file extension. All ink files should use the .ink file extension! \nFiles in Library:\n"+listOfFiles);
			return null;
		}

		/// <summary>
		/// Gets the ink file with path relative to Assets folder, for example: "Assets/Ink/myStory.ink".
		/// </summary>
		/// <returns>The ink file with path.</returns>
		/// <param name="path">Path.</param>
		public static InkFile GetInkFileWithPath (string path) {
			if(instance.inkLibrary == null) return null;
			foreach(InkFile inkFile in instance.inkLibrary) {
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
			if(instance.inkLibrary == null) return null;
			foreach(InkFile inkFile in instance.inkLibrary) {
				if(inkFile.absoluteFilePath == absolutePath) {
					return inkFile;
				}
			}
			return null;
		}

		public static InkFile GetInkFileWithJSONFile (TextAsset inkJSONAsset) {
			if(instance.inkLibrary == null) return null;
			foreach(InkFile inkFile in instance.inkLibrary) {
				if(inkFile.jsonAsset == inkJSONAsset) {
					return inkFile;
				}
			}
			return null;
		}

		/// <summary>
		/// Rebuilds which files are master files and the connections between the files.
		/// INCLUDE is always relative to the master file. This means that every file should be assumed to be a master file until proven otherwise.
		/// </summary>
		/// We might consider updating this to allow rebuilding connections for specific files, since the most common case this is called is when a single file changes.
		/// The upside is that we wouldn't trigger warnings/errors that this function throws, for unrelated files. It's a bit risky so I've not done it yet.
		public static void RebuildInkFileConnections () {
			// Resets the connections between files
			foreach (InkFile inkFile in instance.inkLibrary) {
				inkFile.recursiveIncludeErrorPaths.Clear();
				inkFile.ClearAllHierarchyConnections();
			}
			
			
			// A dictionary which contains a list of all the ink files that INCLUDE a given ink file.
			// Once this is done we can determine which files are master files, and then assert that any INCLUDED files actually exist.
			Dictionary<InkFile, List<InkFile>> includedFileOwnerDictionary = new Dictionary<InkFile, List<InkFile>>();
			Dictionary<InkFile, List<InkFile>> recursiveIncludeLogs = new Dictionary<InkFile, List<InkFile>>();
			// Traverses each file to any file paths referenced using INCLUDE, using the original file as the source path when dealing with nested INCLUDES. 
			// Since not all of the files are guaranteed to be master files, we don't assert that the files actually exist at this time.
			foreach (InkFile inkFile in instance.inkLibrary) {
				BuildIncludeHierarchyAsIfMasterFile(inkFile, inkFile, recursiveIncludeLogs);
				// Recurse ink file includes for a (potential) master ink file, adding them to the file's list of includes if they exist
				static void BuildIncludeHierarchyAsIfMasterFile(InkFile potentialMasterInkFile, InkFile currentInkFile, Dictionary<InkFile, List<InkFile>> recursiveIncludeLogs) {
					if(currentInkFile.localIncludePaths.Count == 0) 
						return;
					foreach (var includePath in currentInkFile.localIncludePaths) {
						var includedFile = FindIncludedFile(potentialMasterInkFile.filePath, includePath);
						// Assets may not actually exist.
						// A typical and expected example is when an included file in a subfolder from it's master file has an INCLUDE, since file paths are always relative to the master file. 
						if (includedFile != null) {
							// We probably only need to show this error for files that are later proved to be master files
							if (potentialMasterInkFile == includedFile || potentialMasterInkFile.includes.Contains(includedFile.inkAsset)) {
								if(!recursiveIncludeLogs.ContainsKey(potentialMasterInkFile)) recursiveIncludeLogs.Add(potentialMasterInkFile, new List<InkFile>());
								recursiveIncludeLogs[potentialMasterInkFile].Add(currentInkFile); 
								continue;
							}
							Debug.Assert(includedFile.inkAsset != null);
							potentialMasterInkFile.includes.Add(includedFile.inkAsset);
							BuildIncludeHierarchyAsIfMasterFile(potentialMasterInkFile, includedFile, recursiveIncludeLogs);
						}
					}
					
					static InkFile FindIncludedFile(string masterFilePath, string includePath) {
						string localIncludePath = InkEditorUtils.CombinePaths(Path.GetDirectoryName(masterFilePath), includePath);
						// This enables parsing ..\ and the like. Can we use Path.GetFullPath instead?
						var fullIncludePath = new FileInfo(localIncludePath).FullName;
						localIncludePath = InkEditorUtils.AbsoluteToUnityRelativePath(fullIncludePath);
						DefaultAsset includedInkFileAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(localIncludePath);
						if(includedInkFileAsset != null) {
							return GetInkFileWithFile(includedInkFileAsset);
						}
						return null;
					}
				}
				
				foreach (var includedFile in inkFile.includes) {
					var includedInkFile = GetInkFileWithFile(includedFile);
					if(!includedFileOwnerDictionary.ContainsKey(includedInkFile)) includedFileOwnerDictionary.Add(includedInkFile, new List<InkFile>());
					includedFileOwnerDictionary[includedInkFile].Add(inkFile);
				}
			}
			
			// Now we've established which files are INCLUDED we can tidy up by detecting and removing non-master files.
			// It's not a master file then we remove all references to it as a master file in the includedFileOwnerDictionary.
			// We don't clear the includes list for those files (even though referenced files may be null), because the user may mark isMarkedToCompileAsMasterFile true at a later date.
			foreach (InkFile inkFile in instance.inkLibrary) {
				var isMasterFile = !includedFileOwnerDictionary.ContainsKey(inkFile) || includedFileOwnerDictionary[inkFile].Count == 0 || inkFile.isMarkedToCompileAsMasterFile;
				if (!isMasterFile) {
					foreach (var includedFileOwners in includedFileOwnerDictionary) {
						if(includedFileOwners.Key != inkFile) includedFileOwners.Value.Remove(inkFile);
					}
				}
			}
			
			// Master ink files and includedFileOwnerDictionary are now valid collections denoting master files and their includes. The final step is to add the masters in any included files.
			foreach (InkFile inkFile in instance.inkLibrary) {
				if (!includedFileOwnerDictionary.ContainsKey(inkFile) || includedFileOwnerDictionary[inkFile].Count == 0 || inkFile.isMarkedToCompileAsMasterFile) {
					foreach (var includedFile in inkFile.includes) {
						var includedInkFile = GetInkFileWithFile(includedFile);
						includedInkFile.masterInkAssets.Add(inkFile.inkAsset);
					}

				}
			}
			
			// Error logs for any master files that wanted to add recursive includes
			foreach (var recursiveIncludeLog in recursiveIncludeLogs) {
				if (recursiveIncludeLog.Key.isMaster) {
					recursiveIncludeLog.Key.recursiveIncludeErrorPaths.AddRange(recursiveIncludeLog.Value.Select(x => x.filePath));
					var files = string.Join("\n", recursiveIncludeLog.Key.recursiveIncludeErrorPaths);
					Debug.LogError("Recursive INCLUDE found in "+recursiveIncludeLog.Key.filePath+" at "+(recursiveIncludeLog.Value.Count == 1 ? "file:\n" : "files:\n")+files);
				}
			}
		}
		
		// Deletes any JSON ink assets that aren't expected to exist because their ink files aren't expected to be compiled
		public static void DeleteUnwantedCompiledJSONAssets() {
			foreach (InkFile inkFile in instance.inkLibrary) {
				if(!inkFile.isMaster && inkFile.jsonAsset != null) {
					AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(inkFile.jsonAsset));
					inkFile.jsonAsset = null;
				}
			}
		}
	}
}