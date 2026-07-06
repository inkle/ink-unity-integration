using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ink.UnityIntegration {
	/// <summary>
	/// The "what's new" window shown once after the plugin updates (unless suppressed in Ink settings).
	/// Displays the changelog; also opened via the "Show changelog" button in Project Settings ▸ Ink.
	/// </summary>
	[InitializeOnLoad]
	public class InkUnityIntegrationStartupWindow : EditorWindow {
		const string editorPrefsKeyForVersionSeen = "Ink Unity Integration Startup Window Version Confirmed";
		const int announcementVersion = 2;

		static int announcementVersionPreviouslySeen;
		static string changelogText;

		static InkUnityIntegrationStartupWindow () {
			EditorApplication.delayCall += TryCreateWindow;
		}

		static void TryCreateWindow () {
			if (InkSettings.instance.suppressStartupWindow) return;
			announcementVersionPreviouslySeen = EditorPrefs.GetInt(editorPrefsKeyForVersionSeen, -1);
			if (announcementVersion != announcementVersionPreviouslySeen) {
				ShowWindow();
			}
		}

		public static void ShowWindow () {
			var window = GetWindow(typeof(InkUnityIntegrationStartupWindow), true, "Ink Update " + InkEditorUtils.unityIntegrationVersionCurrent, true) as InkUnityIntegrationStartupWindow;
			window.minSize = new Vector2(200, 200);
			var size = new Vector2(520, 480);
			window.position = new Rect((Screen.currentResolution.width - size.x) * 0.5f, (Screen.currentResolution.height - size.y) * 0.5f, size.x, size.y);
			EditorPrefs.SetInt(editorPrefsKeyForVersionSeen, announcementVersion);
		}

		void OnEnable () {
			var packageDirectory = InkEditorUtils.FindAbsolutePluginDirectory();
			if (packageDirectory != null) {
				var changelogPath = Path.Combine(packageDirectory, "CHANGELOG.md");
				if (File.Exists(changelogPath)) changelogText = File.ReadAllText(changelogPath);
			}
		}

		void CreateGUI () {
			var root = rootVisualElement;
			root.style.paddingTop = 10;
			root.style.paddingLeft = 10;
			root.style.paddingRight = 10;
			root.style.paddingBottom = 10;

			if (InkEditorUtils.inkLogoIcon != null) {
				var logo = new Image { image = InkEditorUtils.inkLogoIcon, scaleMode = ScaleMode.ScaleToFit };
				logo.style.height = 80;
				logo.style.marginBottom = 4;
				root.Add(logo);
			}
			root.Add(CenteredGrey("Version " + InkEditorUtils.unityIntegrationVersionCurrent));
			root.Add(CenteredGrey("Ink version " + InkEditorUtils.inkVersionCurrent));

			// Offer the 1.x migration only while there are leftover compiled .json files to clean up.
			if (InkMigrationTool.HasLegacyJson()) {
				var migrate = new VisualElement { style = { marginTop = 8 } };
				migrate.Add(new HelpBox("This project has compiled .json files from ink 1.x that are no longer used.", HelpBoxMessageType.Info));
				migrate.Add(new Button(InkMigrationTool.Migrate) { text = "Migrate Ink Project from 1.x" });
				root.Add(migrate);
			}

			if (announcementVersionPreviouslySeen == -1) {
				var newToInk = new Label("New to ink?");
				newToInk.style.unityFontStyleAndWeight = FontStyle.Bold;
				newToInk.style.marginTop = 6;
				root.Add(newToInk);
			}

			var buttons = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 8 } };
			buttons.Add(Grow(new Button(() => Application.OpenURL("https://www.inklestudios.com/ink/")) { text = "About Ink" }));
			buttons.Add(Grow(new Button(() => Application.OpenURL("https://www.patreon.com/inkle")) { text = "❤️ Support Us! ❤️" }));
			buttons.Add(Grow(new Button(() => Application.OpenURL("https://discord.gg/inkle")) { text = "Discord Community + Support" }));
			buttons.Add(Grow(new Button(Close) { text = "Close" }));
			root.Add(buttons);

			if (!string.IsNullOrEmpty(changelogText)) {
				var scroll = new ScrollView { style = { flexGrow = 1, marginTop = 8 } };
				foreach (var rawLine in changelogText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)) {
					var element = BuildChangelogLine(rawLine);
					if (element != null) scroll.Add(element);
				}
				root.Add(scroll);
			}
		}

		// Renders one line of the markdown changelog: headers (#, ##, ### ...) at decreasing sizes,
		// "- " lines as bullets, everything else as a plain paragraph.
		static VisualElement BuildChangelogLine (string rawLine) {
			var line = rawLine.Trim();
			if (line.Length == 0) return null;

			int level = 0;
			while (level < line.Length && line[level] == '#') level++;
			if (level > 0 && level < line.Length && line[level] == ' ') {
				var header = new Label(line.Substring(level + 1).Trim());
				header.style.unityFontStyleAndWeight = FontStyle.Bold;
				header.style.fontSize = level <= 1 ? 18 : level == 2 ? 15 : level == 3 ? 12 : 11;
				header.style.marginTop = level <= 2 ? 10 : 6;
				header.style.marginBottom = 2;
				header.style.whiteSpace = WhiteSpace.Normal;
				return header;
			}

			var label = new Label(line.StartsWith("- ") ? "• " + line.Substring(2).Trim() : line);
			label.style.whiteSpace = WhiteSpace.Normal;
			return label;
		}

		static Label CenteredGrey (string text) {
			var label = new Label(text);
			label.style.unityTextAlign = TextAnchor.MiddleCenter;
			label.style.color = new Color(0.5f, 0.5f, 0.5f);
			label.style.fontSize = 10;
			return label;
		}

		static T Grow<T> (T element) where T : VisualElement {
			element.style.flexGrow = 1;
			return element;
		}
	}
}
