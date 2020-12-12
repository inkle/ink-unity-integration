using UnityEditor;
using UnityEngine;

namespace Ink.UnityIntegration {
	[InitializeOnLoad]
	public class InkUnityIntegrationStartupWindow : EditorWindow {
		const string editorPrefsKeyForVersionSeen = "Ink Unity Integration Startup Window Version Confirmed";
		const int announcementVersion = 1;
		
		Vector2 scrollPosition;
		static int announcementVersionPreviouslySeen;

		private static Texture2D _logoIcon;
		public static Texture2D logoIcon {
			get {
				if(_logoIcon == null) {
					_logoIcon = Resources.Load<Texture2D>("InkLogoIcon");
				}
				return _logoIcon;
			}
		}

		static InkUnityIntegrationStartupWindow () {
			UnityEditor.EditorApplication.delayCall += TryCreateWindow;      
		}

		static void TryCreateWindow() {
			announcementVersionPreviouslySeen = EditorPrefs.GetInt(editorPrefsKeyForVersionSeen, -1);
			if(announcementVersion != announcementVersionPreviouslySeen) {
				InkUnityIntegrationStartupWindow window = EditorWindow.GetWindow(typeof(InkUnityIntegrationStartupWindow), true, "Ink Update "+InkLibrary.versionCurrent.ToString(), true) as InkUnityIntegrationStartupWindow;
				window.minSize = new Vector2(200, 200);
				var size = new Vector2(520, 320);
				window.position = new Rect((Screen.currentResolution.width-size.x) * 0.5f, (Screen.currentResolution.height-size.y) * 0.5f, size.x, size.y);
				EditorPrefs.SetInt(editorPrefsKeyForVersionSeen, announcementVersion);
			}
		}
		
		void OnGUI ()
		{
			EditorGUILayout.BeginVertical();
			var areaSize = new Vector2(80,80);
			GUILayout.BeginArea(new Rect((position.width-areaSize.x)*0.5f, 15, areaSize.x, areaSize.y));
			EditorGUILayout.BeginVertical();
			EditorGUILayout.LabelField(new GUIContent(logoIcon), GUILayout.Width(areaSize.x), GUILayout.Height(areaSize.x*((float)logoIcon.height/logoIcon.width)));
			GUILayout.Space(5);
			EditorGUILayout.LabelField("Version "+InkLibrary.versionCurrent.ToString(), EditorStyles.centeredGreyMiniLabel);
			EditorGUILayout.LabelField("Ink version "+Ink.Runtime.Story.inkVersionCurrent.ToString(), EditorStyles.centeredGreyMiniLabel);
			EditorGUILayout.EndVertical();
			GUILayout.EndArea();

			GUILayout.Space(20+areaSize.y);
			
			// EditorGUILayout.LabelField("Thanks for using Ink! This window will appear to highlight new updates", EditorStyles.boldLabel);

			if(announcementVersionPreviouslySeen == -1) {
				EditorGUILayout.BeginVertical(GUI.skin.box);
				EditorGUILayout.LabelField("New to ink?", EditorStyles.boldLabel);
				EditorGUILayout.EndVertical();
			}

			EditorGUILayout.Space();
			
			{
				scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
				{
					EditorGUILayout.BeginVertical(GUI.skin.box);
					EditorGUILayout.LabelField("Version 0.9.60:", EditorStyles.boldLabel);
					EditorGUILayout.LabelField("• Moved InkLibrary and InkSettings from Assets into Library and ProjectSettings.", EditorStyles.wordWrappedLabel);
					EditorGUILayout.LabelField("   ‣ InkLibrary should no longer be tracked in source control.", EditorStyles.wordWrappedLabel);
					EditorGUILayout.LabelField("   ‣ Changes to InkSettings must be migrated manually.", EditorStyles.wordWrappedLabel);
					EditorGUILayout.LabelField("   ‣ The InkLibrary and InkSettings files in your project folder should be deleted.", EditorStyles.wordWrappedLabel);
					EditorGUILayout.LabelField("• Added a divertable list of knots, stitches and other named content to the Ink Editor Window, replacing the Diverts subpanel.", EditorStyles.wordWrappedLabel);
					EditorGUILayout.EndVertical();
				}

				EditorGUILayout.EndScrollView();
			}

			{
				EditorGUILayout.BeginHorizontal();
			
				if (GUILayout.Button("About Ink")) {
					Application.OpenURL("https://www.inklestudios.com/ink/");
				}
				if (GUILayout.Button("❤️Support Us!❤️")) {
					Application.OpenURL("https://www.patreon.com/inkle");
				}
				if (GUILayout.Button("Close")) {
					Close();
				}
				EditorGUILayout.EndHorizontal();
			}
			EditorGUILayout.Space();

			EditorGUILayout.EndVertical();
		}

		
	}
}