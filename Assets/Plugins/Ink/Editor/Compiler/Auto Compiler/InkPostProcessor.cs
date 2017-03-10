// Automatically creates JSON files from an ink placed within the Assets/Ink folder.
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using Debug = UnityEngine.Debug;
using System.Collections;
using System.Collections.Generic;

using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Ink.UnityIntegration {
	class InkPostProcessor : AssetPostprocessor {
		// Several assets moved at the same time can cause unity to call OnPostprocessAllAssets several times as a result of moving additional files, or simply due to minor time differences.
		// This queue tells the compiler which files to recompile after moves have completed.
		// Not a perfect solution - If Unity doesn't move all the files in the same attempt you can expect some error messages to appear on compile.
		private static List<string> queuedMovedAssets = new List<string>();

		// Recompiles any ink files as a result of an ink file (re)import
		private static void OnPostprocessAllAssets (string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
			if(deletedAssets.Length > 0) {
				OnDeleteAssets(deletedAssets);
			}
			if(movedAssets.Length > 0) {
				OnMoveAssets(movedAssets.Except(importedAssets).ToArray());
			}
			if(importedAssets.Length > 0) {
				OnImportAssets(importedAssets);
			}
			if(InkLibrary.created)
				InkLibrary.Clean();
		}

		private static void OnDeleteAssets (string[] deletedAssets) {
			bool deletedInk = false;
			foreach (var deletedAssetPath in deletedAssets) {
				if(Path.GetExtension(deletedAssetPath) == InkEditorUtils.inkFileExtension) {
					deletedInk = true;
					break;
				}
			}
			if(!deletedInk)
				return;

//			bool alsoDeleteJSON = false;
//			alsoDeleteJSON = EditorUtility.DisplayDialog("Deleting .ink file", "Also delete the JSON file associated with the deleted .ink file?", "Yes", "No"));
			List<InkFile> masterFilesAffected = new List<InkFile>();
			for (int i = InkLibrary.Instance.inkLibrary.Count - 1; i >= 0; i--) {
				if(InkLibrary.Instance.inkLibrary [i].inkAsset == null) {
					if(!InkLibrary.Instance.inkLibrary[i].metaInfo.isMaster && InkLibrary.Instance.inkLibrary[i].metaInfo.masterInkAsset != null && !masterFilesAffected.Contains(InkLibrary.Instance.inkLibrary[i].metaInfo.masterInkFile)) {
						masterFilesAffected.Add(InkLibrary.Instance.inkLibrary[i].metaInfo.masterInkFile);
					}
					if(InkSettings.Instance.handleJSONFilesAutomatically)
						AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(InkLibrary.Instance.inkLibrary[i].jsonAsset));
					InkLibrary.Instance.inkLibrary.RemoveAt(i);
				}
			}
			// After deleting files, we might have broken some include references, so we rebuild them. There's probably a faster way to do this, or we could probably just remove any null references, but this is a bit more robust.
			foreach(InkFile inkFile in InkLibrary.Instance.inkLibrary) {
				inkFile.metaInfo.FindIncludedFiles();
			}
			foreach(InkFile masterFile in masterFilesAffected) {
				InkCompiler.CompileInk(masterFile);
			}
		}

		private static void OnMoveAssets (string[] movedAssets) {
			if (!InkSettings.Instance.handleJSONFilesAutomatically) 
				return;
			
			List<string> validMovedAssets = new List<string>();
			for (var i = 0; i < movedAssets.Length; i++) {
				if(Path.GetExtension(movedAssets[i]) != InkEditorUtils.inkFileExtension) 
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
				List<InkFile> filesToCompile = new List<InkFile>();

				// Add the old master file to the files to be recompiled
				foreach(var inkFilePath in queuedMovedAssets) {
					InkFile inkFile = InkLibrary.GetInkFileWithPath(inkFilePath);
					if(inkFile == null) continue;

					InkFile masterInkFile = inkFile;
					if(!inkFile.metaInfo.isMaster)
						masterInkFile = inkFile.metaInfo.masterInkFile;
					
					if(!filesToCompile.Contains(masterInkFile))
						filesToCompile.Add(masterInkFile);
				}

				InkMetaLibrary.RebuildInkFileConnections();

				// Add the new file to be recompiled
				foreach(var inkFilePath in queuedMovedAssets) {
					InkFile inkFile = InkLibrary.GetInkFileWithPath(inkFilePath);
					if(inkFile == null) continue;

					InkFile masterInkFile = inkFile;
					if(!inkFile.metaInfo.isMaster)
						masterInkFile = inkFile.metaInfo.masterInkFile;
					
					if(!filesToCompile.Contains(masterInkFile))
						filesToCompile.Add(masterInkFile);
					
				}

				queuedMovedAssets.Clear();

				// Compile any ink files that are deemed master files a rebuild
				foreach(var inkFile in filesToCompile) {
					if(inkFile.metaInfo.isMaster)
						InkCompiler.CompileInk(inkFile);
				}
			}
		}

		private static void OnImportAssets (string[] importedAssets) {
			List<string> importedInkAssets = new List<string>();
			string inklecateFileLocation = null;
			foreach (var importedAssetPath in importedAssets) {
				if(Path.GetExtension(importedAssetPath) == InkEditorUtils.inkFileExtension) 
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
			Debug.Log("Inklecate updated. Recompiling all Ink files...");
			InkCompiler.RecompileAll();
		}

		private static void PostprocessInkFiles (List<string> importedInkAssets) {
//			foreach (var importedAssetPath in importedInkAssets) {
//				Debug.Log("Imported Ink: "+importedAssetPath);
//			}
			CreateOrReadUpdatedInkFiles (importedInkAssets);
			foreach (var inkAssetToCompile in GetUniqueMasterInkFilesToCompile (importedInkAssets)) {
				InkCompiler.CompileInk(inkAssetToCompile);
			}
		}

		private static void CreateOrReadUpdatedInkFiles (List<string> importedInkAssets) {
			foreach (var importedAssetPath in importedInkAssets) {
				InkFile inkFile = InkLibrary.GetInkFileWithPath(importedAssetPath);
				if(inkFile == null) {
					DefaultAsset asset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(importedAssetPath);
					inkFile = new InkFile(asset);
					InkLibrary.Instance.inkLibrary.Add(inkFile);
					InkMetaLibrary.Instance.metaLibrary.Add(new InkMetaFile(inkFile));
				} else {
					inkFile.metaInfo.ParseContent();
				}
			}
			// Now we've updated all the include paths for the ink library we can create master/child references between them.
			InkMetaLibrary.RebuildInkFileConnections();
		}

		private static List<InkFile> GetUniqueMasterInkFilesToCompile (List<string> importedInkAssets) {
			List<InkFile> masterInkFiles = new List<InkFile>();
			foreach (var importedAssetPath in importedInkAssets) {
				InkFile inkFile = InkLibrary.GetInkFileWithPath(importedAssetPath);
				if(!masterInkFiles.Contains(inkFile.metaInfo.masterInkFileIncludingSelf) && (InkSettings.Instance.compileAutomatically || inkFile.metaInfo.masterInkFileIncludingSelf.compileAutomatically)) {
					masterInkFiles.Add(inkFile.metaInfo.masterInkFileIncludingSelf);
				}
			}
			return masterInkFiles;
		}
	}
}