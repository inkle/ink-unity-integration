using UnityEngine;
using UnityEditor;
using Debug = UnityEngine.Debug;

/// <summary>
/// Holds a reference to an InkFile object for every .ink file detected in the Assets folder.
/// Provides helper functions to easily obtain these files.
/// </summary>
namespace Ink.UnityIntegration {
	public class InkSettingsAssetSaver : UnityEditor.AssetModificationProcessor {
        static string[] OnWillSaveAssets(string[] paths) {
            InkSettings.SaveToFile();
            return paths;
        }
    }

	public class InkSettings : ScriptableObject {
		public static bool created {
			get {
                // If it's null, there's just no InkSettings asset in the project
                return _Instance != null;
            }
		}

		static string absoluteSavePath {
			get {
				return System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"..","ProjectSettings","InkSettings.asset"));

			}
		}
		public static void SaveToFile () {
			UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new[] { Instance }, absoluteSavePath, true);
		}
		private static InkSettings _Instance;
		public static InkSettings Instance {
			get {
				if(_Instance == null) {
					Object[] objects = UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget(absoluteSavePath);
					if (objects != null && objects.Length > 0) {
						Instance = objects[0] as InkSettings;
					} else {
						Instance = ScriptableObject.CreateInstance<InkSettings>();
						SaveToFile();

					}
				}
				return _Instance;
			} private set {
                if(_Instance == value) return;
				_Instance = value;
			}
		}

		
		
		public TextAsset templateFile;
		public string templateFilePath {
			get {
				if(templateFile == null) return "";
				else return AssetDatabase.GetAssetPath(templateFile);
			}
		}


        public DefaultAsset defaultJsonAssetPath;

        public bool compileAutomatically = true;
		public bool delayInPlayMode = true;
		public bool handleJSONFilesAutomatically = true;

		public int compileTimeout = 30;
		
		public bool printInkLogsInConsoleOnCompile;

		public CustomInklecateOptions customInklecateOptions = new CustomInklecateOptions();
		[System.Serializable]
		public class CustomInklecateOptions {
			#if UNITY_EDITOR_LINUX
			public bool runInklecateWithMono = true;
			#else
			public bool runInklecateWithMono;
			#endif
			public string[] monoPaths = {
				"/usr/bin/mono", 
				"/usr/local/bin/mono", 
				"/Library/Frameworks/Mono.framework/Versions/Current/Commands/mono"
			};
			public string additionalCompilerOptions;
			public DefaultAsset inklecate;
		}

		#if UNITY_EDITOR && !UNITY_2018_1_OR_NEWER
		[MenuItem("Edit/Project Settings/Ink", false, 500)]
		public static void SelectFromProjectSettings() {
			Selection.activeObject = Instance;
		}
		#elif UNITY_EDITOR && UNITY_2018_1_OR_NEWER
		public static SerializedObject GetSerializedSettings() {
			return new SerializedObject(Instance);
		}
		#endif

        private static void Save () {
			EditorUtility.SetDirty(Instance);
			AssetDatabase.SaveAssets();
			EditorApplication.RepaintProjectWindow();
		}
	}	
}
