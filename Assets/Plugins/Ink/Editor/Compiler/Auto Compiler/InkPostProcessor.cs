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
			List<string> importedInkAssets = new List<string>();
			string inklecateFileLocation = null;

			foreach (var importedAssetPath in importedAssets) {
				if(Path.GetExtension(importedAssetPath) == ".ink")
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
			InkLibrary.Refresh();
			List<string> inkAssetsToCompile = new List<string>();
			foreach (var importedAssetPath in importedInkAssets) {
				InkFile file = InkLibrary.GetInkFileWithPath(importedAssetPath);
				if(file.master != null && !inkAssetsToCompile.Contains(file.master.absoluteFilePath))
					inkAssetsToCompile.Add(file.master.absoluteFilePath);
				else if (file.master == null && !inkAssetsToCompile.Contains(file.absoluteFilePath))
					inkAssetsToCompile.Add(file.absoluteFilePath);
			}

			foreach (var inkAssetToCompile in inkAssetsToCompile) {
				InkCompiler.CompileInk(inkAssetToCompile);
			}
		}
	}
}