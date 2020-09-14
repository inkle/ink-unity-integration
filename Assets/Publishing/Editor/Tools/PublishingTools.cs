using Ink.UnityIntegration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class PublishingTools {
	static string AssetsParentDirectory => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
	static string IntegrationPath => Path.GetFullPath(Path.Combine(AssetsParentDirectory, "Packages", "Ink"));

	[MenuItem("Publishing/Prepare for publishing (run all tasks)", false, 1)]
	public static void PreparePublish() {
		SyncPackageJsonVersion();
		CreateDemoPackages();
		SyncReadme();
		Debug.LogWarning("TODO: Create asset store unitypackage");
	}

	[MenuItem("Publishing/Tasks/Create .unitypackage")]
	public static void CreatePackage () {
		// var packagePath = Path.Combine(Application.dataPath, "InkPlugin"+".unitypackage");
		// AssetDatabase.ExportPackage("Packages/Ink", packagePath, ExportPackageOptions.Recurse);
		
		var integrationDirs = Directory.GetDirectories(IntegrationPath);
		var assetsInkPath = Path.Combine(Application.dataPath, "Ink");
		// Copy the plugin into assets, make a package
		foreach(var dir in integrationDirs) {
			var dirName = Path.GetFileName(dir);
			if(dirName == "Demos") continue;
			
			CopyFilesRecursively(new DirectoryInfo(dir), new DirectoryInfo(Path.Combine(assetsInkPath, dirName)));
			
			// var packageDemoDirectory = Path.Combine(IntegrationPath, "Demos");
			// if(!Directory.Exists(packageDemoDirectory)) Directory.CreateDirectory(packageDemoDirectory);
			// var packagePath = Path.Combine(packageDemoDirectory, demoDirName+".unitypackage");
			// Debug.Log("Created '" + packagePath + "'");
		}
		// Refresh to reveal the unitypackage in the Project window.
		AssetDatabase.Refresh();
		AssetDatabase.ExportPackage("Assets/Ink", "InkPlugin"+".unitypackage", ExportPackageOptions.Recurse);
		var assetsInkDirs = Directory.GetDirectories(assetsInkPath);
		foreach(var dir in assetsInkDirs) {
			var dirName = Path.GetFileName(dir);
			if(dirName == "Demos") continue;
			
			var assetsRelativeDir = dir.Substring(AssetsParentDirectory.Length);
			Debug.Log("DELETE"+ assetsRelativeDir);
			AssetDatabase.DeleteAsset(assetsRelativeDir);
		}
		AssetDatabase.Refresh();
	}

	[MenuItem("Publishing/Tasks/Create .unitypackage for demos")]
	public static void CreateDemoPackages () {
		var assetsDemosDir = Path.Combine(Application.dataPath, "Ink", "Demos");
		var demoDirs = Directory.GetDirectories(assetsDemosDir);
		// Copy each demo in Assets/Demos into a .unitypackage in the Ink directory.
		foreach(var demoDir in demoDirs) {
			var demoDirName = Path.GetFileName(demoDir);
			var packageDemoDirectory = Path.Combine(IntegrationPath, "Demos");
			if(!Directory.Exists(packageDemoDirectory)) Directory.CreateDirectory(packageDemoDirectory);
			var packagePath = Path.Combine(packageDemoDirectory, demoDirName+".unitypackage");
			var flags = ExportPackageOptions.Recurse;
			AssetDatabase.ExportPackage("Assets/Ink/Demos/"+demoDirName, packagePath, flags);
			Debug.Log("Created '" + packagePath + "'");
		}
		// Refresh to reveal the unitypackage in the Project window.
		AssetDatabase.Refresh();
	}

	[MenuItem("Publishing/Tasks/Update package.json version")]
	public static void SyncPackageJsonVersion() {
		const string pattern = @"""version"": ""([^""]+)""";
		var packageJsonPath = Path.Combine(IntegrationPath, "package.json");
		var json = File.ReadAllText(packageJsonPath);

		var match = Regex.Match(json, pattern);
		var prevVersion = match.Groups[1].Value;
		var nextVersion = InkLibrary.versionCurrent.ToString();
		if (prevVersion == nextVersion) {
			Debug.LogError("package.json version was already " + nextVersion + ". Did you forget to update it in InkLibrary?");
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

	const string SyncReadmeItemName = "Publishing/Tasks/Update package README.md";
	[MenuItem(SyncReadmeItemName)]
	public static void SyncReadme() {
		var sourcePath = Path.Combine(Application.dataPath, "..", "README.md");
		var destPath = Path.Combine(IntegrationPath, "README.md");

		var content = File.ReadAllText(sourcePath);
		File.WriteAllText(destPath, content);

		// Refresh to reveal the README in the Project window.
		AssetDatabase.Refresh();

		Debug.Log("Updated '" + destPath + "'");
	}

	public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target) {
		foreach (DirectoryInfo dir in source.GetDirectories())
			CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
		foreach (FileInfo file in source.GetFiles())
			file.CopyTo(Path.Combine(target.FullName, file.Name));
	}
}
