using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Ink.UnityIntegration;

// Before a build, make sure all ink is freshly compiled and fail the build if any master file has errors.
// With the ScriptedImporter, ink is compiled on import, so this just forces a synchronous reimport and
// then inspects the resulting InkFile assets (rather than watching a compilation queue).
class InkPreBuildValidationCheck : IPreprocessBuildWithReport
{
	public int callbackOrder => 0;

	public void OnPreprocessBuild (BuildReport report) {
		// Ensure everything is compiled with the latest source before we inspect results.
		InkEditorUtils.ForceRecompileAllInkFilesSync();

		var filesWithErrors = new List<string>();
		foreach (var guid in AssetDatabase.FindAssets("glob:\"*.ink\"")) {
			var path = AssetDatabase.GUIDToAssetPath(guid);
			var inkFile = AssetDatabase.LoadAssetAtPath<InkFile>(path);
			if (inkFile != null && inkFile.hasErrors) filesWithErrors.Add(path);
		}

		if (filesWithErrors.Count > 0) {
			throw new BuildFailedException(
				"Ink compilation errors must be fixed before building. Files with errors:\n" +
				string.Join("\n", filesWithErrors));
		}
	}
}
