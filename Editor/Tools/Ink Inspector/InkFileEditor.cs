using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ink.UnityIntegration {
	/// <summary>
	/// Inspector for the compiled InkFile asset produced by InkImporter. Shows the file's master/include
	/// status, compiler diagnostics, INCLUDE relationships (from the cached InkIncludeGraph), edit/compile
	/// dates, a source preview and actions — everything the old InkInspector showed, adapted to the importer.
	/// </summary>
	[CustomEditor(typeof(InkFile))]
	public class InkFileEditor : Editor {
		const int maxPreviewChars = 16000;

		public override VisualElement CreateInspectorGUI () {
			var inkFile = (InkFile)target;
			var assetPath = AssetDatabase.GetAssetPath(inkFile);
			var root = new VisualElement();

			// Header: master/include label + Open button.
			var header = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center } };
			header.Add(Bold(inkFile.isMaster ? "Master File" : "Include File"));
			header.Add(new Button(() => AssetDatabase.OpenAsset(inkFile)) { text = "Open" });
			root.Add(header);

			var recursive = InkIncludeGraph.GetRecursiveIncludeErrorPaths(assetPath);
			if (recursive.Count > 0)
				root.Add(new HelpBox("A recursive INCLUDE connection exists in this file's INCLUDE hierarchy:\n" +
					string.Join("\n", recursive.ConvertAll(p => "• " + p)), HelpBoxMessageType.Error));

			if (inkFile.hasUnhandledCompileErrors) {
				root.Add(new HelpBox("The compiler failed unexpectedly. This may be a compiler bug — please report it.", HelpBoxMessageType.Error));
				var reportRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
				reportRow.Add(Grow(new Button(() => Application.OpenURL("https://github.com/inkle/ink-unity-integration/issues")) { text = "Report via GitHub" }));
				reportRow.Add(Grow(new Button(() => Application.OpenURL("mailto:info@inklestudios.com?subject=Ink%20compiler%20bug")) { text = "Report via Email" }));
				root.Add(reportRow);
			}

			AddLogs(root, inkFile, "Errors", inkFile.errors, HelpBoxMessageType.Error);
			AddLogs(root, inkFile, "Warnings", inkFile.warnings, HelpBoxMessageType.Warning);
			AddLogs(root, inkFile, "To do", inkFile.todos, HelpBoxMessageType.Info);

			var includes = InkIncludeGraph.GetDirectIncludes(assetPath);
			if (includes.Count > 0) AddFileList(root, "Included Files", includes);
			if (!inkFile.isMaster) {
				var includedBy = InkIncludeGraph.GetIncludedBy(assetPath);
				if (includedBy.Count > 0) AddFileList(root, "Included By", includedBy);
			}

			var dates = BuildDates(inkFile, assetPath);
			if (!string.IsNullOrEmpty(dates)) root.Add(new HelpBox(dates, HelpBoxMessageType.None));

			var play = new Button(() => InkPlayerWindow.LoadAndPlay(inkFile)) { text = "Play in Ink Player" };
			play.SetEnabled(!string.IsNullOrEmpty(inkFile.storyJson));
			root.Add(play);

			root.Add(BuildTextFoldout("Compiled JSON", inkFile.storyJson));
			root.Add(BuildTextFoldout("Source", ReadSource(assetPath)));

			// Apply THIS file's per-object icon last — after the Included By/Files object fields have set
			// theirs — because the inspector header reflects whichever object had its icon set last in the
			// build. Setting it here keeps the header showing this file's icon, not the last file listed.
			InkBrowserIcons.ApplyInstanceIcon(inkFile);
			return root;
		}

		static Label Bold (string text) {
			var label = new Label(text);
			label.style.unityFontStyleAndWeight = FontStyle.Bold;
			return label;
		}

		static T Grow<T> (T element) where T : VisualElement {
			element.style.flexGrow = 1;
			return element;
		}

		static void AddLogs (VisualElement root, InkFile inkFile, string label, List<InkCompilerLog> logs, HelpBoxMessageType type) {
			if (logs == null || logs.Count == 0) return;
			root.Add(Bold(label));
			foreach (var log in logs) {
				var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
				row.Add(Grow(new HelpBox($"{log.content} (at {log.relativeFilePath}:{log.lineNumber})", type)));
				var line = log.lineNumber;
				row.Add(new Button(() => AssetDatabase.OpenAsset(inkFile, line)) { text = "Open" });
				root.Add(row);
			}
		}

		static void AddFileList (VisualElement root, string label, IReadOnlyList<string> paths) {
			root.Add(Bold(label));
			foreach (var path in paths) {
				var file = AssetDatabase.LoadAssetAtPath<InkFile>(path);
				// Refresh the referenced file's per-object icon so the field shows its current badges. Without
				// this it can show a stale icon set on an earlier import (e.g. before it became a master file).
				InkBrowserIcons.ApplyInstanceIcon(file);
				var field = new ObjectField { objectType = typeof(InkFile), value = file };
				field.SetEnabled(false);
				root.Add(field);
			}
		}

		static Foldout BuildTextFoldout (string label, string text) {
			var foldout = new Foldout { text = label, value = false };
			var field = new TextField { multiline = true, isReadOnly = true, value = text ?? string.Empty };
			field.style.maxHeight = 200;
			foldout.Add(field);
			return foldout;
		}

		static string BuildDates (InkFile inkFile, string assetPath) {
			var sb = new StringBuilder();
			try { sb.Append("Last edit: ").Append(File.GetLastWriteTime(InkEditorUtils.UnityRelativeToAbsolutePath(assetPath))); }
			catch { /* file may be gone mid-edit */ }
			if (inkFile.compileDate.HasValue) sb.Append("\nLast compile: ").Append(inkFile.compileDate.Value);
			return sb.ToString();
		}

		static string ReadSource (string assetPath) {
			try {
				var source = File.ReadAllText(InkEditorUtils.UnityRelativeToAbsolutePath(assetPath));
				return source.Length > maxPreviewChars ? source.Substring(0, maxPreviewChars) + "\n…" : source;
			} catch { return string.Empty; }
		}
	}
}
