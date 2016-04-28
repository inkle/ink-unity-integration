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
				importedAssets = importedAssets.Except(movedAssets).ToArray();
				OnImportAssets(importedAssets);
			}
		}

		static void OnImportAssets (string[] importedAssets) {
			foreach (var deletedAssetPath in importedAssets) {
				Debug.Log("Imported Asset: "+deletedAssetPath);
			}

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

		static void OnDeleteAssets (string[] deletedAssets) {
			foreach (var deletedAssetPath in deletedAssets) {
				if(Path.GetExtension(deletedAssetPath) != InkEditorUtils.inkFileExtension) 
					continue;
				InkFile inkFile = InkLibrary.GetInkFileWithAbsolutePath(deletedAssetPath);
				bool tue = InkLibrary.Instance.inkLibrary.Remove(inkFile);
				Debug.Log("Deleted Asset: " + inkFile+" "+tue);
			}
		}

		static void OnMoveAssets (string[] movedAssets, string[] movedFromAssetPaths) {
			for (var i = 0; i < movedAssets.Length; i++) {
				if(Path.GetExtension(movedAssets[i]) != InkEditorUtils.inkFileExtension) 
					continue;
				Debug.Log(movedAssets[i]);
				InkFile inkFile = InkLibrary.GetInkFileWithAbsolutePath(movedAssets[i]);
//				if(inkFile != null) 
				Debug.Log(inkFile);
//				Debug.Log(movedFromAssetPaths[i]+" "+inkFile);
//				inkFile.SetAbsoluteFilePath(movedAssets[i]);
//				Debug.Log("Moved Asset: " + movedAssets[i] + " from: " + movedFromAssetPaths[i]+" "+inkFile);
			}
		}

		static void PostprocessInklecate (string inklecateFileLocation) {
			Debug.Log("Inklecate updated. Recompiling all Ink files...");
			InkCompiler.RecompileAll();
		}

		static void PostprocessInkFiles (List<string> importedInkAssets) {
			Debug.ClearDeveloperConsole();
			InkLibrary.Refresh();
			List<string> inkAssetsToCompile = new List<string>();
			foreach (var importedAssetPath in importedInkAssets) {
				InkFile file = InkLibrary.GetInkFileWithPath(importedAssetPath);
				if(file.master != null) {
					InkFile master = InkLibrary.GetInkFileWithFile((DefaultAsset)file.master);
					if(!inkAssetsToCompile.Contains(master.absoluteFilePath))
						inkAssetsToCompile.Add(master.absoluteFilePath);
				} else if (file.master == null && !inkAssetsToCompile.Contains(file.absoluteFilePath))
					inkAssetsToCompile.Add(file.absoluteFilePath);
			}

			foreach (var inkAssetToCompile in inkAssetsToCompile) {
				InkCompiler.CompileInk(inkAssetToCompile);
			}
		}
	}
}