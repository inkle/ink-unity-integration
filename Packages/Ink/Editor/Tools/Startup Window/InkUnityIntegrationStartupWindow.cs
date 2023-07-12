using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Ink.UnityIntegration {
	[InitializeOnLoad]
	public class InkUnityIntegrationStartupWindow : EditorWindow {
		const string editorPrefsKeyForVersionSeen = "Ink Unity Integration Startup Window Version Confirmed";
		const int announcementVersion = 2;
		
		Vector2 scrollPosition;
		static int announcementVersionPreviouslySeen;
		static string changelogText;

		static InkUnityIntegrationStartupWindow () {
			EditorApplication.delayCall += TryCreateWindow;
		}

		static void TryCreateWindow() {
			if (InkSettings.instance.suppressStartupWindow) return;
			announcementVersionPreviouslySeen = EditorPrefs.GetInt(editorPrefsKeyForVersionSeen, -1);
			if(announcementVersion != announcementVersionPreviouslySeen) {
				ShowWindow();
			}
		}
		
        public static void ShowWindow () {
            InkUnityIntegrationStartupWindow window = GetWindow(typeof(InkUnityIntegrationStartupWindow), true, "Ink Update "+InkLibrary.unityIntegrationVersionCurrent, true) as InkUnityIntegrationStartupWindow;
            window.minSize = new Vector2(200, 200);
            var size = new Vector2(520, 320);
            window.position = new Rect((Screen.currentResolution.width-size.x) * 0.5f, (Screen.currentResolution.height-size.y) * 0.5f, size.x, size.y);
            EditorPrefs.SetInt(editorPrefsKeyForVersionSeen, announcementVersion);
        }

        void OnEnable() {
	        var packageDirectory = InkEditorUtils.FindAbsolutePluginDirectory();
	        changelogText = File.ReadAllText(Path.Combine(packageDirectory, "CHANGELOG.md"));
        }
        
		void OnGUI ()
		{
			EditorGUILayout.BeginVertical();
			var areaSize = new Vector2(90,90);
			GUILayout.BeginArea(new Rect((position.width-areaSize.x)*0.5f, 15, areaSize.x, areaSize.y));
			EditorGUILayout.BeginVertical();
			EditorGUILayout.LabelField(new GUIContent(InkEditorUtils.inkLogoIcon), GUILayout.Width(areaSize.x), GUILayout.Height(areaSize.x*((float)InkEditorUtils.inkLogoIcon.height/InkEditorUtils.inkLogoIcon.width)));
			GUILayout.Space(5);
			EditorGUILayout.LabelField("Version "+InkLibrary.unityIntegrationVersionCurrent, EditorStyles.centeredGreyMiniLabel);
			EditorGUILayout.LabelField("Ink version "+InkLibrary.inkVersionCurrent, EditorStyles.centeredGreyMiniLabel);
			EditorGUILayout.EndVertical();
			GUILayout.EndArea();


			GUILayout.Space(20+areaSize.y);
			
			if(announcementVersionPreviouslySeen == -1) {
				EditorGUILayout.BeginVertical(GUI.skin.box);
				EditorGUILayout.LabelField("New to ink?", EditorStyles.boldLabel);
				EditorGUILayout.EndVertical();
			}

			{
				EditorGUILayout.BeginHorizontal();
			
				if (GUILayout.Button("About Ink")) {
					Application.OpenURL("https://www.inklestudios.com/ink/");
				}
				if (GUILayout.Button("❤️Support Us!❤️")) {
					Application.OpenURL("https://www.patreon.com/inkle");
				}
				if (GUILayout.Button("Discord Community+Support")) {
					Application.OpenURL("https://discord.gg/inkle");
				}
				if (GUILayout.Button("Close")) {
					Close();
				}
				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.Space();
			
			if(changelogText != null) {
				scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
				
				var versionSections = Regex.Split(changelogText, "## "); // Split markdown text into version sections
				foreach (var section in versionSections) {
					if (string.IsNullOrWhiteSpace(section)) continue;

					var lines = section.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries); // Split each section into lines
					var version = lines[0]; // First line is version

					EditorGUILayout.BeginVertical(GUI.skin.box);
					EditorGUILayout.LabelField($"{version}", EditorStyles.boldLabel);
					for (int i = 1; i < lines.Length; i++) {
						var bulletPoint = lines[i].TrimStart('-').TrimStart(' ');
						EditorGUILayout.LabelField($"• {bulletPoint}", EditorStyles.wordWrappedLabel);
					}

					EditorGUILayout.EndVertical();
				}

				EditorGUILayout.EndScrollView();
			}
			EditorGUILayout.Space();

			EditorGUILayout.EndVertical();
		}
	}
}