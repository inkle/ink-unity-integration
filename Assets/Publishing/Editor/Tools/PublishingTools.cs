using Ink.UnityIntegration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using System.Reflection;
#if UNITY_2019_3_OR_NEWER
using UnityEditor.Compilation;
#endif

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
		EditorApplication.LockReloadAssemblies();
		// #if UNITY_2019_4_OR_NEWER
		// AssetDatabase.DisallowAutoRefresh();
		// #endif
		var assetsInkPath = Path.Combine(Application.dataPath, "Ink");
		List<KeyValuePair<DirectoryInfo, DirectoryInfo>> rootPaths = new List<KeyValuePair<DirectoryInfo, DirectoryInfo>>();
		// Copy the plugin into assets, make a package
		var integrationDirs = Directory.GetDirectories(IntegrationPath);
		foreach(var dir in integrationDirs) {
			var dirName = Path.GetFileName(dir);
			if(dirName == "Demos") continue;
			rootPaths.Add(new KeyValuePair<DirectoryInfo, DirectoryInfo>(new DirectoryInfo(dir), new DirectoryInfo(Path.Combine(assetsInkPath, dirName))));
		}

		// Move files from Packages into Assets
		foreach(var rootPath in rootPaths) {
			MoveFilesRecursively(rootPath.Key, rootPath.Value);
		}
		// TODO - when we switch to 2019.4, get this working!
		// This refresh causes errors until you alt-tab and back because it forces a script recompile but the files are moved back before it's done.
		// To fix it, we can block recompilation (I dont think you can do this, or even if it'd work) or need to wait until compilation is done before copying the files back.
		AssetDatabase.Refresh();
		// We can use this callback to achieve this.
		// CompilationPipeline.compilationFinished += (object sender) => {
			// CompilationPipeline.RequestScriptCompilation();
		// }

		// Create a .unitypackage
		var version = InkLibrary.versionCurrent;
		var packageExportPath = string.Format("../Ink Unity Integration {0}.{1}.{2}.unitypackage", version.Major, version.Minor, version.Build);
		AssetDatabase.ExportPackage("Assets/Ink", packageExportPath, ExportPackageOptions.Recurse);
		
		// Move files back to Packages
		foreach(var rootPath in rootPaths) {
			MoveFilesRecursively(rootPath.Value, rootPath.Key);
		}
		
		EditorApplication.UnlockReloadAssemblies();
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
}