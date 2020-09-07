using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;

public static class PublishingTools {
	[MenuItem("Publishing/Create Example.unitypackage")]
	public static void CreateExamplePackage () {
		// Copy Assets/InkExample into a .unitypackage in the Ink directory.
		var packagePath = Path.Combine(Application.dataPath, "Plugins", "Ink", "Example", "Example.unitypackage");
		var flags = ExportPackageOptions.Recurse;
		AssetDatabase.ExportPackage("Assets/InkExample", packagePath, flags);

		// Refresh or else the new unitypackage may not be visible in Project window.
		AssetDatabase.Refresh();

		Debug.Log("Created '" + packagePath + "'");
	}
}
