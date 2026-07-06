using UnityEditor;
using System.Linq;
using Debug = UnityEngine.Debug;

namespace Ink.UnityIntegration {

	// The InkImporter (ScriptedImporter) compiles .ink files on import. This postprocessor only logs a
	// concise summary of which ink files were (re)imported, preserving the peace-of-mind logging the
	// old queue-based compiler used to print.
	public class InkPostProcessor : AssetPostprocessor {
		private static void OnPostprocessAllAssets (string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
			LogImportedInkFiles(importedAssets);
		}

		static void LogImportedInkFiles (string[] importedAssets) {
			var inkFiles = importedAssets.Where(InkEditorUtils.IsInkFile).ToArray();
			if(inkFiles.Length == 0) return;
			Debug.Log($"Ink import complete ({inkFiles.Length} file{(inkFiles.Length == 1 ? "" : "s")}):\n{string.Join("\n", inkFiles)}");
		}
	}
}
