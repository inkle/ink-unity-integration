using UnityEngine;
using UnityEditor;
using Debug = UnityEngine.Debug;

/// <summary>
/// Holds a reference to an InkFile object for every .ink file detected in the Assets folder.
/// Provides helper functions to easily obtain these files.
/// </summary>
namespace Ink.UnityIntegration {
    #if UNITY_2020_1_OR_NEWER
    [FilePath("ProjectSettings/InkSettings.asset", FilePathAttribute.Location.ProjectFolder)]
	public class InkSettings : ScriptableSingleton<InkSettings> {
    #else
	public class InkSettings : ScriptableObject {
    #endif
        #if !UNITY_2020_1_OR_NEWER
		public static bool created {
			get {
                // If it's null, there's just no InkSettings asset in the project
                return _instance != null;
            }
		}
		static string absoluteSavePath {
			get {
				return System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"..","ProjectSettings","InkSettings.asset"));

			}
		}
		public static void SaveStatic (bool saveAsText) {
			UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new[] { instance }, absoluteSavePath, saveAsText);
		}
        public void Save (bool saveAsText) {
			UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget((UnityEngine.Object[]) new InkSettings[1] {this}, absoluteSavePath, saveAsText);
		}

		private static InkSettings _instance;
		public static InkSettings instance {
			get {
				if(_instance == null) {
					Object[] objects = UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget(absoluteSavePath);
					if (objects != null && objects.Length > 0) {
						instance = objects[0] as InkSettings;
					} else {
						instance = ScriptableObject.CreateInstance<InkSettings>();
						instance.Save(true);
					}
				}
				return _instance;
			} private set {
                if(_instance == value) return;
				_instance = value;
			}
		}
        #else
		public static void SaveStatic (bool saveAsText) {
			instance.Save(saveAsText);
		}
        #endif

        public class AssetSaver : UnityEditor.AssetModificationProcessor {
            static string[] OnWillSaveAssets(string[] paths) {
                InkSettings.instance.Save(true);
                return paths;
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

		#if UNITY_EDITOR && !UNITY_2018_1_OR_NEWER
		[MenuItem("Edit/Project Settings/Ink", false, 500)]
		public static void SelectFromProjectSettings() {
			Selection.activeObject = instance;
		}
		#elif UNITY_EDITOR && UNITY_2018_1_OR_NEWER
		public static SerializedObject GetSerializedSettings() {
			return new SerializedObject(instance);
		}
		#endif
        
		// Deletes the persistent version of this asset that we used to use prior to 0.9.71
		void OnEnable () {
			if(!Application.isPlaying && EditorUtility.IsPersistent(this)) {
				var path = AssetDatabase.GetAssetPath(this);
				if(!string.IsNullOrEmpty(path)) {
					#if !UNITY_2020_1_OR_NEWER
                    if(_instance == this) _instance = null;
					#endif
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(this));
					AssetDatabase.Refresh();
					return;
				}
			}
		}
	}	
}
