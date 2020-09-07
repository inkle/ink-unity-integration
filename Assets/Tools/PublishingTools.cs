using Ink.UnityIntegration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class PublishingTools {
	static string IntegrationPath => Path.Combine(Application.dataPath, "..", "Packages", "Ink");

	[MenuItem("Publishing/Create Example.unitypackage")]
	public static void CreateExamplePackage () {
		// Copy Assets/InkExample into a .unitypackage in the Ink directory.
		var packagePath = Path.Combine(IntegrationPath, "Example", "Example.unitypackage");
		var flags = ExportPackageOptions.Recurse;
		AssetDatabase.ExportPackage("Assets/InkExample", packagePath, flags);

		// Refresh or else the new unitypackage may not be visible in Project window.
		AssetDatabase.Refresh();

		Debug.Log("Created '" + packagePath + "'");
	}

	[MenuItem("Publishing/Update package.json version")]
	public static void SyncPackageJsonVersion() {
		const string pattern = @"""version"": ""([^""]+)""";
		var packageJsonPath = Path.Combine(IntegrationPath, "package.json");
		var json = File.ReadAllText(packageJsonPath);

		var match = Regex.Match(json, pattern);
		var prevVersion = match.Groups[1].Value;
		var nextVersion = InkLibrary.versionCurrent.ToString();
		if (prevVersion == nextVersion) {
			Debug.LogError("package.json version was already " + nextVersion);
		} else {
			json = Regex.Replace(
				json,
				pattern,
				"\"version\": \"" + nextVersion + "\""
			);
			File.WriteAllText(packageJsonPath, json);
			Debug.Log("Updated package.json from " + prevVersion + " to " + nextVersion);
		}
	}
}
