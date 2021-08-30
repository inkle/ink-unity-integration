﻿// Automatically creates JSON files from an ink placed within the Assets/Ink folder.
using UnityEngine;
using UnityEditor;
using System.IO;
using Debug = UnityEngine.Debug;
using System.Collections.Generic;
using System.Linq;

namespace Ink.UnityIntegration {
	class InkPostProcessor : AssetPostprocessor {
		// Several assets moved at the same time can cause unity to call OnPostprocessAllAssets several times as a result of moving additional files, or simply due to minor time differences.
		// This queue tells the compiler which files to recompile after moves have completed.
		// Not a perfect solution - If Unity doesn't move all the files in the same attempt you can expect some error messages to appear on compile.
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0090 // Use 'new(...)'
		private static List<string> queuedMovedAssets = new List<string>();
#pragma warning restore IDE0090 // Use 'new(...)'
#pragma warning restore IDE0044 // Add readonly modifier
		public static bool disabled = false;
		// Recompiles any ink files as a result of an ink file (re)import
		private static void OnPostprocessAllAssets (string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
			if(disabled) return;
			if(deletedAssets.Length > 0) {
				OnDeleteAssets(deletedAssets);
			}
			if(movedAssets.Length > 0) {
				OnMoveAssets(movedAssets.Except(importedAssets).ToArray());
			}
			if(importedAssets.Length > 0) {
				OnImportAssets(importedAssets);
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
#pragma warning disable IDE0090 // Use 'new(...)'
			List<InkFile> masterFilesAffected = new List<InkFile>();
#pragma warning restore IDE0090 // Use 'new(...)'
			for (int i = InkLibrary.instance.inkLibrary.Count - 1; i >= 0; i--) {
				if(InkLibrary.instance.inkLibrary [i].inkAsset == null) {
					if(!InkLibrary.instance.inkLibrary[i].isMaster) {
						foreach(var masterInkFile in InkLibrary.instance.inkLibrary[i].masterInkFiles) {
							if(!masterFilesAffected.Contains(masterInkFile))
								masterFilesAffected.Add(masterInkFile);
						}
					}
					if(InkSettings.instance.handleJSONFilesAutomatically) {
                        var assetPath = AssetDatabase.GetAssetPath(InkLibrary.instance.inkLibrary[i].jsonAsset);
						if(assetPath != null && assetPath != string.Empty) {
                            AssetDatabase.DeleteAsset(assetPath);
                        }
                    }
					InkLibrary.RemoveAt(i);
				}
			}
			// After deleting files, we might have broken some include references, so we rebuild them. There's probably a faster way to do this, or we could probably just remove any null references, but this is a bit more robust.
			foreach(InkFile inkFile in InkLibrary.instance.inkLibrary) {
				inkFile.FindIncludedFiles();
			}
			foreach(InkFile masterFile in masterFilesAffected) {
				if(InkSettings.instance.compileAutomatically || masterFile.compileAutomatically) {
					InkCompiler.CompileInk(masterFile);
				}
			}
		}

		private static void OnMoveAssets (string[] movedAssets) {
			if (!InkSettings.instance.handleJSONFilesAutomatically) 
				return;
			
#pragma warning disable IDE0090 // Use 'new(...)'
			List<string> validMovedAssets = new List<string>();
#pragma warning restore IDE0090 // Use 'new(...)'
			for (var i = 0; i < movedAssets.Length; i++) {
				if(!InkEditorUtils.IsInkFile(movedAssets[i]))
					continue;
				validMovedAssets.Add(movedAssets[i]);
				queuedMovedAssets.Add(movedAssets[i]);

			}
			// Move compiled JSON files.
			// This can cause Unity to postprocess assets again.
			bool assetMoved = false;
			foreach(var inkFilePath in validMovedAssets) {
				InkFile inkFile = InkLibrary.GetInkFileWithPath(inkFilePath);
				if(inkFile == null) continue;
				if(inkFile.jsonAsset == null) continue;

				string jsonAssetPath = AssetDatabase.GetAssetPath(inkFile.jsonAsset);
				
				string movedAssetDir = Path.GetDirectoryName(inkFilePath);
				string movedAssetFile = Path.GetFileName(inkFilePath);
				string newPath = InkEditorUtils.CombinePaths(movedAssetDir, Path.GetFileNameWithoutExtension(movedAssetFile)) + ".json";
				AssetDatabase.MoveAsset(jsonAssetPath, newPath);
				assetMoved = true;
			}

			// Check if no JSON assets were moved (as a result of none needing to move, or this function being called as a result of JSON files being moved)
			if(!assetMoved && queuedMovedAssets.Count > 0) {
#pragma warning disable IDE0090 // Use 'new(...)'
				List<InkFile> filesToCompile = new List<InkFile>();
#pragma warning restore IDE0090 // Use 'new(...)'

				// Add the old master file to the files to be recompiled
				foreach(var inkFilePath in queuedMovedAssets) {
					InkFile inkFile = InkLibrary.GetInkFileWithPath(inkFilePath);
					if(inkFile == null) continue;
#pragma warning disable IDE0059 // Unnecessary assignment of a value
					foreach(var masterInkFile in inkFile.masterInkFilesIncludingSelf) {
#pragma warning restore IDE0059 // Unnecessary assignment of a value
						if(!filesToCompile.Contains(inkFile))
							filesToCompile.Add(inkFile);
					}
				}

				InkLibrary.RebuildInkFileConnections();

				// Add the new file to be recompiled
				foreach(var inkFilePath in queuedMovedAssets) {
					InkFile inkFile = InkLibrary.GetInkFileWithPath(inkFilePath);
					if(inkFile == null) continue;

#pragma warning disable IDE0059 // Unnecessary assignment of a value
					foreach(var masterInkFile in inkFile.masterInkFilesIncludingSelf) {
#pragma warning restore IDE0059 // Unnecessary assignment of a value
						if(!filesToCompile.Contains(inkFile))
							filesToCompile.Add(inkFile);
					}
				}

				queuedMovedAssets.Clear();

				// Compile any ink files that are deemed master files a rebuild
				foreach(var inkFile in filesToCompile) {
					if(inkFile.isMaster) {
						if(InkSettings.instance.compileAutomatically || inkFile.compileAutomatically) {
							InkCompiler.CompileInk(inkFile);
						}
					}
				}
			}
		}

		private static void OnImportAssets (string[] importedAssets) {
#pragma warning disable IDE0090 // Use 'new(...)'
			List<string> importedInkAssets = new List<string>();
#pragma warning restore IDE0090 // Use 'new(...)'
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