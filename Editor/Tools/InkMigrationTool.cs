using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ink.UnityIntegration {
	/// <summary>
	/// One-time helper for upgrading a project from ink-unity-integration 1.x to 2.0.
	///
	/// The 1.x pipeline generated a .json TextAsset next to each .ink file. In 2.0 the compiled story is
	/// stored inside each .ink file's imported InkFile, so those .json files are no longer used. This tool
	/// finds and deletes them. Reference rewiring can't be automated (a TextAsset field can't hold an
	/// InkFile), so it also reminds you to update your code to reference InkFile / inkFile.storyJson.
	///
	/// It's offered contextually — in the Ink Update window and Project Settings ▸ Ink — only while legacy
	/// .json files exist (see HasLegacyJson), so it disappears once a project has migrated.
	/// </summary>
	public static class InkMigrationTool {
		const string codeReminder =
			"Also update your scripts: reference the .ink file's InkFile (not the old TextAsset) and use " +
			"new Story(inkFile.storyJson) instead of new Story(textAsset.text).";

		// Cached per-project (keyed by project path) so we don't scan the whole project on every settings
		// open or editor launch. The scan only re-runs when the plugin version changes (i.e. on upgrade).
		static string CheckedVersionKey => "Ink.MigrationCheckedVersion." + Application.dataPath;
		static string HasLegacyJsonKey => "Ink.HasLegacyJson." + Application.dataPath;

		/// <summary>
		/// True if the project still has compiled .json files left over from ink 1.x. Cheap: the project is
		/// only scanned the first time this is called after a plugin version change (an upgrade); otherwise
		/// it returns the cached result. Re-checked automatically after Migrate() runs.
		/// </summary>
		public static bool HasLegacyJson () {
			var current = InkEditorUtils.unityIntegrationVersionCurrent.ToString();
			if (EditorPrefs.GetString(CheckedVersionKey, string.Empty) != current) {
				EditorPrefs.SetString(CheckedVersionKey, current);
				RecheckLegacyJson();
			}
			return EditorPrefs.GetBool(HasLegacyJsonKey, false);
		}

		/// <summary>Forces a fresh scan and updates the cache. Cheap to call from infrequent UI (e.g. opening the settings tab).</summary>
		public static void RecheckLegacyJson () {
			EditorPrefs.SetBool(HasLegacyJsonKey, FindLegacyJson().Count > 0);
		}

		/// <summary>Finds and (after confirmation) deletes the leftover 1.x compiled .json files.</summary>
		public static void Migrate () {
			var oldJson = FindLegacyJson();
			if (oldJson.Count == 0) {
				EditorUtility.DisplayDialog("Migrate Ink Project (1.x → 2.0)",
					"No leftover compiled ink .json files were found — nothing to delete.\n\n" + codeReminder, "OK");
				return;
			}

			if (!EditorUtility.DisplayDialog("Migrate Ink Project (1.x → 2.0)",
				$"Found {oldJson.Count} compiled ink .json file(s) left over from ink 1.x. In 2.0 the compiled " +
				"story lives inside each .ink file's InkFile, so these are no longer used.\n\nDelete them?\n\n" + codeReminder,
				$"Delete {oldJson.Count} file(s)", "Cancel"))
				return;

			AssetDatabase.StartAssetEditing();
			try {
				foreach (var path in oldJson) AssetDatabase.DeleteAsset(path);
			} finally {
				AssetDatabase.StopAssetEditing();
			}
			RecheckLegacyJson();
			Debug.Log($"Ink migration: deleted {oldJson.Count} old compiled .json file(s):\n{string.Join("\n", oldJson)}\n\n{codeReminder}");
		}

		static List<string> FindLegacyJson () {
			return AssetDatabase.FindAssets("t:TextAsset")
				.Select(AssetDatabase.GUIDToAssetPath)
				.Where(p => p.EndsWith(".json") && IsCompiledInkJson(p))
				.ToList();
		}

		// A compiled ink story JSON begins with an "inkVersion" field, so we only need to sniff the start.
		static bool IsCompiledInkJson (string jsonPath) {
			try {
				using (var reader = new StreamReader(InkEditorUtils.UnityRelativeToAbsolutePath(jsonPath))) {
					var head = new char[256];
					int read = reader.Read(head, 0, head.Length);
					return new string(head, 0, read).Contains("inkVersion");
				}
			} catch {
				return false;
			}
		}
	}
}
