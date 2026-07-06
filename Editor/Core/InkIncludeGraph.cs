using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ink.UnityIntegration {
	/// <summary>
	/// Editor-side cache of the .ink INCLUDE graph, rebuilt whenever ink files change. Provides fast
	/// master detection, includes / included-by lookups and recursive-include detection — the caching
	/// role InkLibrary used to serve, but without the old compilation queue or per-file metadata objects.
	///
	/// Ink resolves every INCLUDE path (even in nested includes) relative to the master/root file, so the
	/// graph is built by walking each file's include tree "as if it were the master", resolving against
	/// that root's directory (matching the old InkLibrary.BuildIncludeHierarchyAsIfMasterFile). A file is
	/// a master unless it's reached as an include of some file; that status is recorded on its InkImporter.
	/// </summary>
	[InitializeOnLoad]
	class InkIncludeGraph : AssetPostprocessor {
		// file -> the files it directly INCLUDEs (resolved in a master's context; existing files only).
		static Dictionary<string, List<string>> _directIncludes;
		// file -> the root files whose include tree contains it.
		static Dictionary<string, List<string>> _includedBy;
		// files that are included by some other file (i.e. not masters).
		static HashSet<string> _included;

		static InkIncludeGraph () {
			EditorApplication.delayCall += EnsureBuilt;
		}

		static void OnPostprocessAllAssets (string[] imported, string[] deleted, string[] moved, string[] movedFrom) {
			bool inkChanged = imported.Any(InkEditorUtils.IsInkFile)
				|| deleted.Any(InkEditorUtils.IsInkFile)
				|| moved.Any(InkEditorUtils.IsInkFile)
				|| movedFrom.Any(InkEditorUtils.IsInkFile);
			if (inkChanged) EditorApplication.delayCall += Rebuild;
		}

		static void EnsureBuilt () {
			if (_directIncludes == null) Rebuild();
		}

		static void Rebuild () {
			var allInkPaths = AssetDatabase.FindAssets("glob:\"*.ink\"")
				.Select(AssetDatabase.GUIDToAssetPath)
				.Where(p => !string.IsNullOrEmpty(p))
				.ToList();
			var allSet = new HashSet<string>(allInkPaths);

			_directIncludes = new Dictionary<string, List<string>>();
			_includedBy = new Dictionary<string, List<string>>();
			_included = new HashSet<string>();

			// Walk every file's include tree as if it were the master, resolving relative to its directory.
			foreach (var root in allInkPaths) {
				var rootDir = System.IO.Path.GetDirectoryName(root)?.Replace('\\', '/');
				Traverse(root, root, rootDir, allSet, new HashSet<string>());
			}

			// A file is a master unless some root's tree includes it. Record on the importer, reimport changes.
			var toReimport = new List<string>();
			foreach (var path in allInkPaths) {
				if (!(AssetImporter.GetAtPath(path) is InkImporter importer)) continue;
				bool shouldBeMaster = !_included.Contains(path);
				if (importer.IsMasterFile != shouldBeMaster) {
					var so = new SerializedObject(importer);
					so.FindProperty("isMasterFile").boolValue = shouldBeMaster;
					so.ApplyModifiedPropertiesWithoutUndo();
					toReimport.Add(path);
				}
			}
			if (toReimport.Count == 0) return;
			AssetDatabase.StartAssetEditing();
			try {
				foreach (var path in toReimport)
					AssetImporter.GetAtPath(path).SaveAndReimport();
			} finally {
				AssetDatabase.StopAssetEditing();
			}
		}

		static void Traverse (string root, string current, string rootDir, HashSet<string> allSet, HashSet<string> stack) {
			if (!stack.Add(current)) return; // recursive INCLUDE — stop; GetRecursiveIncludeErrorPaths reports it.
			foreach (var raw in InkImporter.GetRawIncludes(current)) {
				var resolved = InkImporter.ResolveIncludePath(rootDir, raw);
				if (!allSet.Contains(resolved)) continue; // only edges to real files (wrong-context guesses miss)
				AddEdge(_directIncludes, current, resolved);
				AddEdge(_includedBy, resolved, root);
				_included.Add(resolved);
				Traverse(root, resolved, rootDir, allSet, stack);
			}
			stack.Remove(current);
		}

		static void AddEdge (Dictionary<string, List<string>> map, string key, string value) {
			if (!map.TryGetValue(key, out var list)) { list = new List<string>(); map[key] = list; }
			if (!list.Contains(value)) list.Add(value);
		}

		// ---- Fast cached queries (used by the inspector) ----

		public static bool IsMaster (string assetPath) {
			EnsureBuilt();
			return !_included.Contains(assetPath);
		}

		public static IReadOnlyList<string> GetDirectIncludes (string assetPath) {
			EnsureBuilt();
			return _directIncludes.TryGetValue(assetPath, out var v) ? v : (IReadOnlyList<string>)Array.Empty<string>();
		}

		/// <summary>The master files whose include tree contains this file.</summary>
		public static IReadOnlyList<string> GetIncludedBy (string assetPath) {
			EnsureBuilt();
			if (!_includedBy.TryGetValue(assetPath, out var roots)) return Array.Empty<string>();
			return roots.Where(IsMaster).ToList();
		}

		/// <summary>Returns include paths that form a recursive (circular) INCLUDE reachable from this file.</summary>
		public static List<string> GetRecursiveIncludeErrorPaths (string assetPath) {
			EnsureBuilt();
			var offending = new List<string>();
			Visit(assetPath, new HashSet<string>(), new HashSet<string>(), offending);
			return offending;
		}

		static void Visit (string path, HashSet<string> stack, HashSet<string> done, List<string> offending) {
			if (stack.Contains(path)) {
				if (!offending.Contains(path)) offending.Add(path);
				return;
			}
			if (done.Contains(path)) return;
			stack.Add(path);
			if (_directIncludes.TryGetValue(path, out var includes))
				foreach (var include in includes) Visit(include, stack, done, offending);
			stack.Remove(path);
			done.Add(path);
		}
	}
}
