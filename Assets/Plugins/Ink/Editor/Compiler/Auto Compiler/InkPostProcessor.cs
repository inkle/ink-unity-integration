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
					if(!InkLibrary.Instance.inkLibrary[i].isMaster && InkLibrary.Instance.inkLibrary[i].master != null && !masterFilesAffected.Contains(InkLibrary.Instance.inkLibrary[i].masterInkFile)) {
						masterFilesAffected.Add(InkLibrary.Instance.inkLibrary[i].masterInkFile);
					}
					if(InkLibrary.Instance.handleJSONFilesAutomatically)
						AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(InkLibrary.Instance.inkLibrary[i].jsonAsset));
					InkLibrary.Instance.inkLibrary.RemoveAt(i);
				}
			}
			// After deleting files, we might have broken some include references, so we rebuild them. There's probably a faster way to do this, or we could probably just remove any null references, but this is a bit more robust.
			foreach(InkFile inkFile in InkLibrary.Instance.inkLibrary) {
				inkFile.FindIncludedFiles();
			}
			foreach(InkFile masterFile in masterFilesAffected) {
				InkCompiler.CompileInk(masterFile);
			}
		}

		private static void OnMoveAssets (string[] movedAssets) {
			if (!InkLibrary.Instance.handleJSONFilesAutomatically) 
				return;
			for (var i = 0; i < movedAssets.Length; i++) {
				if(Path.GetExtension(movedAssets[i]) != InkEditorUtils.inkFileExtension) 
					continue;
				InkFile inkFile = InkLibrary.GetInkFileWithPath(movedAssets[i]);
				if(inkFile != null) {
					string jsonAssetPath = AssetDatabase.GetAssetPath(inkFile.jsonAsset);
					
					string movedAssetDir = Path.GetDirectoryName(movedAssets[i]);
					string movedAssetFile = Path.GetFileName(movedAssets[i]);
					string newPath = InkEditorUtils.CombinePaths(movedAssetDir, Path.GetFileNameWithoutExtension(movedAssetFile)) + ".json";
					AssetDatabase.MoveAsset(jsonAssetPath, newPath);
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
				}
				inkFile.ParseContent();
			}
			// Now we've updated all the include paths for the ink library we can create master/child references between them.
			InkLibrary.RebuildInkFileConnections();
		}

		private static List<InkFile> GetUniqueMasterInkFilesToCompile (List<string> importedInkAssets) {
			List<InkFile> masterInkFiles = new List<InkFile>();
			foreach (var importedAssetPath in importedInkAssets) {
				InkFile inkFile = InkLibrary.GetInkFileWithPath(importedAssetPath);
				if(inkFile.isMaster && !masterInkFiles.Contains(inkFile) && (InkLibrary.Instance.compileAutomatically || inkFile.compileAutomatically)) {
					masterInkFiles.Add(inkFile);
				} else if(!inkFile.isMaster && !masterInkFiles.Contains(inkFile.masterInkFile) && (InkLibrary.Instance.compileAutomatically || inkFile.masterInkFile.compileAutomatically)) {
					masterInkFiles.Add(inkFile.masterInkFile);
				}
			}
			return masterInkFiles;
		}
	}
}