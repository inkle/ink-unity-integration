// Automatically creates JSON files from an ink placed within the Assets/Ink folder.
using UnityEngine;
using UnityEditor;
using System.IO;
using Debug = UnityEngine.Debug;
using System.Collections.Generic;
using System.Linq;

namespace Ink.UnityIntegration {
	
	public class InkPostProcessor : AssetPostprocessor {
		// Several assets moved at the same time can cause unity to call OnPostprocessAllAssets several times as a result of moving additional files, or simply due to minor time differences.
		// This queue tells the compiler which files to recompile after moves have completed.
		// Not a perfect solution - If Unity doesn't move all the files in the same attempt you can expect some error messages to appear on compile.
		private static List<string> queuedMovedInkFileAssets = new List<string>();
		
		// We should make this a stack, similar to GUI.BeginDisabledGroup.
		public static bool disabled = false;
		// I'd like to make this a public facing setting sometime. Options are async or immediate.
		public static bool compileImmediatelyOnImport = false;
		// Recompiles any ink files as a result of an ink file (re)import
		private static void OnPostprocessAllAssets (string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
			if(disabled) return;
			if(deletedAssets.Length > 0) {
				OnDeleteAssets(deletedAssets);
			}
			if(movedAssets.Length > 0) {
				OnMoveAssets(movedAssets);
			}
			if(importedAssets.Length > 0) {
				// Assets that are renamed are both moved and imported. We do all the work in OnMoveAssets, so do nothing here.
				var importedAssetsThatWerentRenames = importedAssets.Except(movedAssets).ToArray();
				OnImportAssets(importedAssetsThatWerentRenames);
			}
            #if !UNITY_2020_1_OR_NEWER
			if(InkLibrary.created)
            #endif
            InkLibrary.Clean();
		}

		private static void OnDeleteAssets (string[] deletedAssets) {
			bool deletedInk = false;
			foreach (var deletedAssetPath in deletedAssets) {
				if(InkEditorUtils.IsInkFile(deletedAssetPath)) {
					deletedInk = true;
					break;
				}
			}
			if(!deletedInk)
				return;

//			bool alsoDeleteJSON = false;
//			alsoDeleteJSON = EditorUtility.DisplayDialog("Deleting .ink file", "Also delete the JSON file associated with the deleted .ink file?", "Yes", "No"));
			List<InkFile> masterFilesAffected = new List<InkFile>();
			for (int i = InkLibrary.instance.inkLibrary.Count - 1; i >= 0; i--) {
				var inkFile = InkLibrary.instance.inkLibrary[i];
				// If this file was deleted...
				if(inkFile.inkAsset == null) {
					// Mark the master files to be recompiled (note that those files might also have been deleted)
					if(!inkFile.isMaster) {
						foreach(var masterInkAsset in inkFile.masterInkAssets) {
							if(masterInkAsset != null) {
								var masterInkFile = InkLibrary.GetInkFileWithFile(masterInkAsset);
								if(!masterFilesAffected.Contains(masterInkFile))
									masterFilesAffected.Add(masterInkFile);
							}
						}
					}
					// Delete the associated json file
					if(InkSettings.instance.handleJSONFilesAutomatically) {
                        var assetPath = AssetDatabase.GetAssetPath(inkFile.jsonAsset);
						if(assetPath != null && assetPath != string.Empty) {
                            AssetDatabase.DeleteAsset(assetPath);
                        }
                    }
					// Finally, remove it from the ink library
					InkLibrary.RemoveAt(i);
				}
			}

			// After deleting files, we might have broken some include references, so we rebuild them. There's probably a faster way to do this, or we could probably just remove any null references, but this is a bit more robust.
			InkLibrary.RebuildInkFileConnections();
			
			foreach(var masterInkFile in masterFilesAffected) {
				if(InkSettings.instance.ShouldCompileInkFileAutomatically(masterInkFile)) {
					InkCompiler.CompileInk(masterInkFile);
				}
			}
		}

		private static void OnMoveAssets (string[] movedAssets) {
			if (!InkSettings.instance.handleJSONFilesAutomatically) 
				return;
			
			List<string> validMovedAssets = new List<string>();
			for (var i = 0; i < movedAssets.Length; i++) {
				if(!InkEditorUtils.IsInkFile(movedAssets[i]))
					continue;
				validMovedAssets.Add(movedAssets[i]);
				queuedMovedInkFileAssets.Add(movedAssets[i]);

			}
			// Move compiled JSON files.
			// This can cause Unity to postprocess assets again.
			foreach(var inkFilePath in validMovedAssets) {
				InkFile inkFile = InkLibrary.GetInkFileWithPath(inkFilePath);
				if(inkFile == null) continue;
				if(inkFile.jsonAsset == null) continue;

				string jsonAssetPath = AssetDatabase.GetAssetPath(inkFile.jsonAsset);
				
				string movedAssetDir = Path.GetDirectoryName(inkFilePath);
				string movedAssetFile = Path.GetFileName(inkFilePath);
				string newPath = InkEditorUtils.CombinePaths(movedAssetDir, Path.GetFileNameWithoutExtension(movedAssetFile)) + ".json";

				// On moving an ink file, we either recompile it, creating a new json file in the correct location, or we move the json file.
				if(InkSettings.instance.ShouldCompileInkFileAutomatically(inkFile)) {
					// We have to delay this, or it doesn't properly inform unity (there's no version of "ImportAsset" for delete); I guess it doesn't want OnPostprocessAllAssets to fire recursively.
					EditorApplication.delayCall += () => {
						AssetDatabase.DeleteAsset(jsonAssetPath);
						AssetDatabase.Refresh();
					};
				} else {
					if (string.IsNullOrEmpty(AssetDatabase.ValidateMoveAsset(jsonAssetPath, newPath))) {
						Debug.Assert(newPath==inkFile.jsonPath);
						EditorApplication.delayCall += () => {
							AssetDatabase.MoveAsset(jsonAssetPath, newPath);
							AssetDatabase.ImportAsset(newPath);
							AssetDatabase.Refresh();
							inkFile.FindCompiledJSONAsset();
						};
						// Debug.Log(jsonAssetPath+" to "+newPath);
					} else {
						// This will fire if the JSON file is also moved with the ink - in this case the json file will be in movedAssets.
						// Debug.Log($"Failed to move asset from path '{jsonAssetPath}' to '{newPath}'.");
					}
				}
			}
			// Check if no JSON assets were moved (as a result of none needing to move, or this function being called as a result of JSON files being moved)
			if(queuedMovedInkFileAssets.Count > 0) {
				List<InkFile> filesToCompile = new List<InkFile>();

				// Add the old master file to the files to be recompiled
				foreach(var inkFilePath in queuedMovedInkFileAssets) {
					InkFile inkFile = InkLibrary.GetInkFileWithPath(inkFilePath);
					if(inkFile == null) continue;
					foreach(var masterInkFile in inkFile.masterInkFilesIncludingSelf) {
						if(InkSettings.instance.ShouldCompileInkFileAutomatically(masterInkFile) && !filesToCompile.Contains(masterInkFile))
							filesToCompile.Add(masterInkFile);
					}
				}

				InkLibrary.RebuildInkFileConnections();

                // If rebuilding connections caused a file that was previously considered a master file to no longer be, then we remove it.
				for (int i = filesToCompile.Count - 1; i >= 0; i--)
                    if(!filesToCompile[i].isMaster) 
						filesToCompile.RemoveAt(i);

				// Add the new file to be recompiled
				foreach(var inkFilePath in queuedMovedInkFileAssets) {
					InkFile inkFile = InkLibrary.GetInkFileWithPath(inkFilePath);
					if(inkFile == null) continue;

					foreach(var masterInkFile in inkFile.masterInkFilesIncludingSelf) {
						if(InkSettings.instance.ShouldCompileInkFileAutomatically(masterInkFile) && !filesToCompile.Contains(masterInkFile))
							filesToCompile.Add(masterInkFile);
					}
				}

				queuedMovedInkFileAssets.Clear();
				

				// Compile any ink files that are deemed master files a rebuild
				InkCompiler.CompileInk(filesToCompile.ToArray(), compileImmediatelyOnImport);
			}
		}

		private static void OnImportAssets (string[] importedAssets) {
			List<string> importedInkAssets = new List<string>();
			string inklecateFileLocation = null;
			foreach (var importedAssetPath in importedAssets) {
				if(InkEditorUtils.IsInkFile(importedAssetPath))
					importedInkAssets.Add(importedAssetPath);
				else if (Path.GetFileName(importedAssetPath) == "inklecate" && Path.GetExtension(importedAssetPath) == "")
					inklecateFileLocation = importedAssetPath;
			}
			if(importedInkAssets.Count > 0)
				PostprocessInkFiles(importedInkAssets);
			if(inklecateFileLocation != null)
				PostprocessInklecate(inklecateFileLocation);
		}

		private static void PostprocessInklecate (string inklecateFileLocation) {
			// This should probably only recompile files marked to compile automatically, but it's such a rare case, and one where you probably do want to compile.
			// To fix, one day!
			Debug.Log("Inklecate updated. Recompiling all Ink files...");
			InkEditorUtils.RecompileAll();
		}

		private static void PostprocessInkFiles (List<string> importedInkAssets) {
			if(EditorApplication.isPlaying && InkSettings.instance.delayInPlayMode) {
				foreach(var fileToImport in importedInkAssets) {
					InkCompiler.AddToPendingCompilationStack(fileToImport);
				}
			} else {
				InkLibrary.CreateOrReadUpdatedInkFiles (importedInkAssets);
				InkCompiler.CompileInk(InkCompiler.GetUniqueMasterInkFilesToCompile (importedInkAssets).ToArray());
			}
		}
	}
}