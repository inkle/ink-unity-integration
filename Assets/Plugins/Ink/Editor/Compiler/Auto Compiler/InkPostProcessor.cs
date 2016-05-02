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
		static void OnPostprocessAllAssets (string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
			if(deletedAssets.Length > 0) {
				OnDeleteAssets(deletedAssets);
			}
			if(movedAssets.Length > 0) {
				OnMoveAssets(movedAssets, movedFromAssetPaths);
			}
			if(importedAssets.Length > 0) {
//				Debug.Log(importedAssets.Length);
//				importedAssets = importedAssets.Except(movedAssets).ToArray();
//				Debug.Log(importedAssets.Length);
				OnImportAssets(importedAssets);
			}
		}

		static void OnDeleteAssets (string[] deletedAssets) {
			bool deletedInk = false;
			foreach (var deletedAssetPath in deletedAssets) {
				if(Path.GetExtension(deletedAssetPath) == InkEditorUtils.inkFileExtension) {
					deletedInk = true;
					break;
				}
			}
			if(!deletedInk)
				return;
			for (int i = InkLibrary.Instance.inkLibrary.Count - 1; i >= 0; i--) {
				if(InkLibrary.Instance.inkLibrary [i].inkAsset == null)
					InkLibrary.Instance.inkLibrary.RemoveAt(i);
			}
		}

		static void OnMoveAssets (string[] movedAssets, string[] movedFromAssetPaths) {
			for (var i = 0; i < movedAssets.Length; i++) {
				if(Path.GetExtension(movedAssets[i]) != InkEditorUtils.inkFileExtension) 
					continue;
				InkFile inkFile = InkLibrary.GetInkFileWithPath(movedAssets[i]);
				if(inkFile != null) {
					string jsonAssetPath = AssetDatabase.GetAssetPath(inkFile.jsonAsset);
					AssetDatabase.RenameAsset(jsonAssetPath, Path.GetFileNameWithoutExtension(Path.GetFileName(movedAssets[i])));
				}
			}
		}

		static void OnImportAssets (string[] importedAssets) {
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
			
			if(PlayerSettings.apiCompatibilityLevel == ApiCompatibilityLevel.NET_2_0_Subset) {
				Debug.LogWarning("Detected PlayerSettings.apiCompatibilityLevel is .NET 2.0 Subset. Due to JSON.Net as used by ink-engine.dll, API Compatibility Level must be set to .NET 2.0 for standalone builds to function. Change this in PlayerSettings.");
			}
		}

		static void PostprocessInklecate (string inklecateFileLocation) {
			Debug.Log("Inklecate updated. Recompiling all Ink files...");
			InkCompiler.RecompileAll();
		}

		static void PostprocessInkFiles (List<string> importedInkAssets) {
			Debug.ClearDeveloperConsole();
//			foreach (var importedAssetPath in importedInkAssets) {
//				Debug.Log("Imported Ink: "+importedAssetPath);
//			}
			InkLibrary.Refresh();
//			foreach (var importedAssetPath in importedInkAssets) {
//				InkFile inkFile = InkLibrary.GetInkFileWithPath(importedAssetPath);
//				if(inkFile == null) {
////					InkLibrary.Instance.inkLibrary.Add(new InkFile(AssetDatabase.));
////					inkFile = new InkFile(AssetDatabase.LoadAssetAtPath<DefaultAsset>(importedAssetPath);
//				}
//			}
			List<InkFile> inkFilesToCompile = new List<InkFile>();
			foreach (var importedAssetPath in importedInkAssets) {
				InkFile inkFile = InkLibrary.GetInkFileWithPath(importedAssetPath);
				if(inkFile.isMaster && !inkFilesToCompile.Contains(inkFile) && (InkLibrary.Instance.compileAutomatically || inkFile.compileAutomatically)) {
					inkFilesToCompile.Add(inkFile);
				} else if(!inkFile.isMaster && !inkFilesToCompile.Contains(inkFile.masterInkFile) && (InkLibrary.Instance.compileAutomatically || inkFile.masterInkFile.compileAutomatically)) {
					inkFilesToCompile.Add(inkFile.masterInkFile);
				}
			}

			foreach (var inkAssetToCompile in inkFilesToCompile) {
				InkCompiler.CompileInk(inkAssetToCompile);
			}
		}
	}
}