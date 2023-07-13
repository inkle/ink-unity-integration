using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Ink.UnityIntegration;
using UnityEditor.Compilation;
using UnityEngine.Networking;

// Should be run to update files in the package folder from the root of the repo, and to create demo and release packages.
public static class PublishingTools {
	static string AssetsParentDirectory => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
	static string IntegrationPath => Path.GetFullPath(Path.Combine(AssetsParentDirectory, "Packages", "Ink"));

	[MenuItem("Publishing/Prepare for publishing (run all tasks)", false, 1)]
	public static void PreparePublish() {
		SyncPackageJsonVersion();
		CreateDemoPackages();
		SyncReadme();
		CreatePackage();
		DescribeNextSteps();
	}

	[MenuItem("Publishing/Tasks/Create .unitypackage for demos")]
	public static void CreateDemoPackages () {
		var assetsDemosDir = Path.Combine(Application.dataPath, "Ink", "Demos");
		var demoDirs = Directory.GetDirectories(assetsDemosDir);
		var packageDemoDirectory = Path.Combine(IntegrationPath, "Demos");
		// Copy each demo in Assets/Demos into a .unitypackage in the Ink directory.
		foreach(var demoDir in demoDirs) {
			if(!Directory.Exists(packageDemoDirectory)) Directory.CreateDirectory(packageDemoDirectory);
			var demoDirName = Path.GetFileName(demoDir);
			var packagePath = Path.Combine(packageDemoDirectory, demoDirName+".unitypackage");
			var flags = ExportPackageOptions.Recurse;
			AssetDatabase.ExportPackage("Assets/Ink/Demos/"+demoDirName, packagePath, flags);
			Debug.Log("PublishingTools.CreateDemoPackages: Packaged "+demoDirName+" at '" + packagePath + "'");
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
		var nextVersion = InkLibrary.unityIntegrationVersionCurrent.ToString();
		if (prevVersion == nextVersion) {
			Debug.LogWarning("SyncPackageJsonVersion: package.json version was already " + nextVersion + ". Did you forget to update it in InkLibrary?");
		} else {
			json = Regex.Replace(
				json,
				pattern,
				"\"version\": \"" + nextVersion + "\""
			);
			File.WriteAllText(packageJsonPath, json);
			Debug.Log("PublishingTools.Updated package.json from " + prevVersion + " to " + nextVersion);
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

		Debug.Log("PublishingTools.SyncReadme: Updated '" + destPath + "'");
	}

    public static void MoveFilesRecursively(DirectoryInfo source, DirectoryInfo target) {
        Directory.CreateDirectory(target.FullName);

        // Copy each file into the new directory.
        foreach (FileInfo fi in source.GetFiles()) {
            fi.MoveTo(Path.Combine(target.FullName, fi.Name));
        }

        // Copy each subdirectory using recursion.
        foreach (DirectoryInfo diSourceSubDir in source.GetDirectories()) {
            DirectoryInfo nextTargetSubDir =
                target.CreateSubdirectory(diSourceSubDir.Name);
            MoveFilesRecursively(diSourceSubDir, nextTargetSubDir);
        }

		source.Delete();
		new FileInfo(source.FullName+".meta").Delete();
    }

	[MenuItem("Publishing/Tasks/Create .unitypackage")]
	public static void CreatePackage () {
		EditorApplication.LockReloadAssemblies();
		// #if UNITY_2019_4_OR_NEWER
		// AssetDatabase.DisallowAutoRefresh();
		// #endif
		var assetsInkPath = Path.Combine(Application.dataPath, "Ink");
		var copiedFiles = new List<(string packagesFile, string assetsFile)>();
		var copiedDirectories = new List<(DirectoryInfo packagesDirectory, DirectoryInfo assetsDirectory)>();
		
		// Work out which files need to be copied into Assets for the Package
		var files = Directory.GetFiles(IntegrationPath);
		foreach (var filePath in files) {
			var fileExtension = Path.GetExtension(filePath);
			if(fileExtension == ".meta" || fileExtension == ".DS_Store") continue;
			var fileName = Path.GetFileName(filePath);
			if(fileName == "package.json") continue;
			copiedFiles.Add((filePath, Path.Combine(assetsInkPath, fileName)));
		}
		
		var integrationDirs = Directory.GetDirectories(IntegrationPath);
		foreach(var dir in integrationDirs) {
			var dirName = Path.GetFileName(dir);
			if(dirName == "Demos") continue;
			copiedDirectories.Add((new DirectoryInfo(dir), new DirectoryInfo(Path.Combine(assetsInkPath, dirName))));
		}

		// Move files from Packages into Assets
		foreach(var rootPath in copiedFiles) 
			new FileInfo(rootPath.packagesFile).MoveTo(rootPath.assetsFile);
		foreach(var rootPath in copiedDirectories)
			MoveFilesRecursively(rootPath.packagesDirectory, rootPath.assetsDirectory);
		
		// I believe this creates meta files but I can't recall!
		AssetDatabase.Refresh();

		// Create a .unitypackage
		var version = InkLibrary.unityIntegrationVersionCurrent;
		var packageExportPath = string.Format("../Ink Unity Integration {0}.{1}.{2}.unitypackage", version.Major, version.Minor, version.Build);
		AssetDatabase.ExportPackage("Assets/Ink", packageExportPath, ExportPackageOptions.Recurse);
		
		// Move files back to Packages
		foreach (var rootPath in copiedFiles)
			new FileInfo(rootPath.assetsFile).MoveTo(rootPath.packagesFile);
		foreach(var rootPath in copiedDirectories) 
			MoveFilesRecursively(rootPath.assetsDirectory, rootPath.packagesDirectory);
		
		EditorApplication.UnlockReloadAssemblies();
		AssetDatabase.Refresh();
		CompilationPipeline.RequestScriptCompilation();
		Debug.Log("PublishingTools.CreatePackage: Created .unitypackage at "+Path.GetFullPath(Path.Combine(Application.dataPath, packageExportPath)));
	}
	
	
	
	[MenuItem("Publishing/Show Helper Window")]
	static void DescribeNextSteps() {
		PublishingToolsHelperWindow.ShowWindow();
	}
	
	public class PublishingToolsHelperWindow : EditorWindow {
		// Vector2 scrollPosition;
		
		public static void ShowWindow () {
			PublishingToolsHelperWindow window = GetWindow(typeof(PublishingToolsHelperWindow), true, "Ink Publishing Tools", true) as PublishingToolsHelperWindow;
		}
		
		void OnGUI () {
			EditorGUILayout.BeginVertical();
			EditorGUILayout.LabelField("Version "+InkLibrary.unityIntegrationVersionCurrent, EditorStyles.centeredGreyMiniLabel);
		
			if (GUILayout.Button("Unlock")) {
				EditorApplication.UnlockReloadAssemblies();
			}
			if (GUILayout.Button("Prepare for publishing (run all tasks)")) {
				PreparePublish();
			}
			if (GUILayout.Button("Show Package")) {
				EditorUtility.RevealInFinder(Path.GetFullPath(Path.Combine(Application.dataPath, "../..")));
			}
			if (GUILayout.Button("Draft GitHub Release")) {
				// 1.1.7
				var version = UnityWebRequest.EscapeURL($"v{InkLibrary.unityIntegrationVersionCurrent}");
				
				var title = UnityWebRequest.EscapeURL($"{InkLibrary.unityIntegrationVersionCurrent} is out!");
				
				var packageDirectory = InkEditorUtils.FindAbsolutePluginDirectory();
				var changelogText = File.ReadAllText(Path.Combine(packageDirectory, "CHANGELOG.md"));
				var versionSections = Regex.Split(changelogText, "## "); // Split markdown text into version sections
					
				StringBuilder sb = new StringBuilder();
				if (versionSections.Length > 1) {
					var section = versionSections[1];
					var lines = section.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries); // Split each section into lines
					for (int i = 1; i < lines.Length; i++) {
						var bulletPoint = lines[i].TrimStart('-').TrimStart(' ');
						sb.AppendLine($"• {bulletPoint}");
					}
				}
				var body = UnityWebRequest.EscapeURL($"{sb.ToString()}");

				
				Application.OpenURL($"https://github.com/inkle/ink-unity-integration/releases/new?tag={version}&title={title}&body={body}");
			}
			EditorGUILayout.EndVertical();
		}
	}
}