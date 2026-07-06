using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ink.UnityIntegration {
	/// <summary>
	/// Draws the ink settings, both as the InkSettings asset's inspector and in the Project Settings ▸ Ink page,
	/// sharing a single UI Toolkit tree between the two.
	/// </summary>
	[CustomEditor(typeof(InkSettings))]
	public class InkSettingsEditor : Editor {
		public override VisualElement CreateInspectorGUI () {
			return BuildUI(serializedObject);
		}

		[SettingsProvider]
		public static SettingsProvider CreateInkSettingsProvider () {
			return new SettingsProvider("Project/Ink", SettingsScope.Project) {
				label = "Ink",
				activateHandler = (searchContext, rootElement) => {
					// The settings tab opens rarely, so it's a good moment to re-check for leftover 1.x files.
					InkMigrationTool.RecheckLegacyJson();
					rootElement.Add(BuildUI(InkSettings.GetSerializedSettings()));
				},
			};
		}

		static VisualElement BuildUI (SerializedObject settings) {
			var root = new VisualElement { style = { marginTop = 4, marginLeft = 4, marginRight = 4 } };

			root.Add(BuildVersionInfo());
			root.Add(Header("Settings"));

			var templateProp = settings.FindProperty("templateFile");
			var templateWarning = new HelpBox("Template not found. Ink files created via Assets > Create > Ink will be blank.", HelpBoxMessageType.Info);
			root.Add(templateWarning);

			var template = new PropertyField(templateProp, "Ink Template") { tooltip = "Optional. The default content of files created via Assets > Create > Ink." };
			root.Add(template);
			root.Add(new PropertyField(settings.FindProperty("printInkLogsInConsoleOnCompile"), "Print ink TODOs in console on compile") { tooltip = "When enabled, ink lines starting with TODO are printed in the console." });
			root.Add(new PropertyField(settings.FindProperty("suppressStartupWindow"), "Suppress Startup Window") { tooltip = "Prevent the \"what's new\" window appearing when the plugin version changes. Useful for CI/CD." });

			var defineProp = settings.FindProperty("automaticallyAddDefineSymbols");
			root.Add(new PropertyField(defineProp, "Add define symbols") { tooltip = "Automatically adds INK_EDITOR and INK_RUNTIME to the scripting define symbols." });

			// Only shown while there are leftover 1.x .json files to clean up; disappears once migrated.
			if (InkMigrationTool.HasLegacyJson()) {
				var migrate = new VisualElement();
				migrate.Add(Header("Upgrade from 1.x to 2.0"));
				migrate.Add(new HelpBox("This project has compiled .json files from ink 1.x that are no longer used.", HelpBoxMessageType.Info));
				var migrateButton = new Button { text = "Migrate Ink Project (1.x → 2.0)" };
				migrateButton.clicked += () => {
					InkMigrationTool.Migrate();
					// Migrate() re-scans; drop the section once there are no leftover .json left to clean up.
					if (!InkMigrationTool.HasLegacyJson()) migrate.RemoveFromHierarchy();
				};
				migrate.Add(migrateButton);
				root.Add(migrate);
			}

			root.Add(Header("Support + Requests"));
			root.Add(BuildRequestButtons());

			// Bind the fields, keep the template warning in sync, apply the define-symbol side effect, and
			// persist changes (InkSettings uses a custom save, so binding alone doesn't write to disk).
			root.Bind(settings);
			void RefreshTemplateWarning () => templateWarning.style.display = templateProp.objectReferenceValue == null ? DisplayStyle.Flex : DisplayStyle.None;
			RefreshTemplateWarning();
			root.TrackSerializedObjectValue(settings, so => {
				RefreshTemplateWarning();
				if (defineProp.boolValue) InkDefineSymbols.AddGlobalDefine();
				else InkDefineSymbols.RemoveGlobalDefine();
				(so.targetObject as InkSettings)?.Save(true);
			});
			return root;
		}

		static Label Header (string text) {
			var label = new Label(text);
			label.style.unityFontStyleAndWeight = FontStyle.Bold;
			label.style.marginTop = 6;
			return label;
		}

		static VisualElement BuildVersionInfo () {
			var root = new VisualElement();
			root.Add(Header("Version Info"));
			root.Add(ReadOnlyText("Plugin version", InkEditorUtils.unityIntegrationVersionCurrent.ToString()));
			root.Add(ReadOnlyText("Ink version", InkEditorUtils.inkVersionCurrent.ToString()));
			root.Add(ReadOnlyText("Ink story format version", Ink.Runtime.Story.inkVersionCurrent.ToString()));
			root.Add(ReadOnlyText("Ink save format version", Ink.Runtime.StoryState.kInkSaveStateVersion.ToString()));
			var changelog = new Button(InkUnityIntegrationStartupWindow.ShowWindow) { text = "Show changelog" };
			changelog.style.width = 140;
			root.Add(changelog);
			return root;
		}

		static TextField ReadOnlyText (string label, string value) {
			var field = new TextField(label) { value = value, isReadOnly = true };
			field.SetEnabled(false);
			return field;
		}

		static VisualElement BuildRequestButtons () {
			var root = new VisualElement();
			root.Add(new Label("Is there a setting you'd like? Or a feature you'd like to request?"));
			var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
			row.Add(new Button(() => Application.OpenURL("https://discord.gg/inkle")) { text = "Reach us on Discord", style = { flexGrow = 1 } });
			row.Add(new Button(() => Application.OpenURL("https://github.com/inkle/ink-unity-integration/issues/new")) { text = "Submit an issue on GitHub", style = { flexGrow = 1 } });
			root.Add(row);
			return root;
		}
	}
}
