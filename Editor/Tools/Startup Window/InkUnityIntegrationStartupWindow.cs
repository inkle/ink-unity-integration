using System;
using System.IO;
using System.Text;
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
			// Centre on the main editor window. Screen.currentResolution is in physical pixels (wrong on
			// retina/scaled displays, pushing the window off toward the corner); the main window rect is in
			// the same coordinate space as EditorWindow.position.
			var main = EditorGUIUtility.GetMainWindowPosition();
			window.position = new Rect(main.x + (main.width - size.x) * 0.5f, main.y + (main.height - size.y) * 0.5f, size.x, size.y);
			EditorPrefs.SetInt(editorPrefsKeyForVersionSeen, announcementVersion);
		}

		void OnEnable () {
			var packageDirectory = InkEditorUtils.FindAbsolutePluginDirectory();
			if (packageDirectory != null) {
				var changelogPath = Path.Combine(packageDirectory, "CHANGELOG.md");
				if (File.Exists(changelogPath)) changelogText = StripUnsupportedGlyphs(File.ReadAllText(changelogPath));
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

			if (announcementVersionPreviouslySeen == -1) {
				var newToInk = new Label("New to ink?");
				newToInk.style.unityFontStyleAndWeight = FontStyle.Bold;
				newToInk.style.marginTop = 6;
				root.Add(newToInk);
			}

			var buttons = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 8 } };
			buttons.Add(Grow(new Button(() => Application.OpenURL("https://www.inklestudios.com/ink/")) { text = "About Ink" }));
#if UNITY_6000_0_OR_NEWER
			// Unity 6+ ships a UI Toolkit font with emoji coverage, so use the real emoji.
			buttons.Add(Grow(new Button(() => Application.OpenURL("https://www.patreon.com/inkle")) { text = "❤️ Support Us! ❤️" }));
#else
			// 2022.3's default font renders the emoji + its variation selector as a box; use a plain heart.
			buttons.Add(Grow(new Button(() => Application.OpenURL("https://www.patreon.com/inkle")) { text = "♥ Support Us! ♥" }));
#endif
			buttons.Add(Grow(new Button(() => Application.OpenURL("https://discord.gg/inkle")) { text = "Discord Community + Support" }));
			root.Add(buttons);

			// Divider between the action buttons and the migration/changelog below. Semi-transparent grey
			// reads on both the light and dark editor skins.
			var separator = new VisualElement { style = { height = 1, marginTop = 10, marginBottom = 6, backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.5f) } };
			root.Add(separator);

			// Offer the 1.x migration only while there are leftover compiled .json files to clean up.
			// Sits just above the changelog so it's the last thing before the notes.
			if (InkMigrationTool.HasLegacyJson()) {
				var migrate = new VisualElement { style = { marginTop = 8, marginBottom = 4 } };
				migrate.Add(new HelpBox("This project has compiled .json files from ink 1.x that are no longer used.", HelpBoxMessageType.Info));
				// No standard Unity "primary button" style exists, so make it prominent with size + weight
				// (theme-safe, unlike a hardcoded accent colour).
				var migrateButton = new Button { text = "Migrate Ink Project (1.x → 2.0)" };
				migrateButton.clicked += () => {
					InkMigrationTool.Migrate();
					// Migrate() re-scans; drop the prompt once there are no leftover .json left to clean up.
					if (!InkMigrationTool.HasLegacyJson()) migrate.RemoveFromHierarchy();
				};
				migrateButton.style.height = 32;
				migrateButton.style.fontSize = 13;
				migrateButton.style.unityFontStyleAndWeight = FontStyle.Bold;
				migrateButton.style.marginTop = 4;
				migrate.Add(migrateButton);
				root.Add(migrate);
			}

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

		// Unity 6+ ships a UI Toolkit font with emoji coverage, so leave the changelog untouched — emoji and
		// their variation selectors render fine. On 2022.3 the default font renders them as boxes, so strip
		// the variation selectors (U+FE00-FE0F) and non-BMP emoji (surrogate pairs). Remove this whole
		// fallback once the minimum supported version is Unity 6.
		static string StripUnsupportedGlyphs (string text) {
#if UNITY_6000_0_OR_NEWER
			return text;
#else
			var sb = new StringBuilder(text.Length);
			foreach (var c in text) {
				if (c >= (char)0xFE00 && c <= (char)0xFE0F) continue; // variation selectors (FE00-FE0F)
				if (char.IsSurrogate(c)) continue;                    // non-BMP emoji (surrogate pairs)
				sb.Append(c);
			}
			return sb.ToString();
#endif
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
