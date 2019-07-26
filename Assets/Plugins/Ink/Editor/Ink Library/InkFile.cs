using UnityEngine;
using UnityEditor;
using System.IO;
using Debug = UnityEngine.Debug;

namespace Ink.UnityIntegration {
	// Helper class for ink files that maintains INCLUDE connections between ink files
	[System.Serializable]
	public sealed class InkFile {
		
		public bool compileAutomatically = false;
		// A reference to the ink file
		public DefaultAsset inkAsset;

        //specify json destination folder (if None, default to same folder as ink file)
        public DefaultAsset jsonAssetPath;

		// The compiled json file. Use this to start a story.
		public TextAsset jsonAsset;

		[System.NonSerialized]
		private InkMetaFile _metaInfo;
		public InkMetaFile metaInfo {
			get {
				if(_metaInfo == null || _metaInfo.inkAsset == null)
					_metaInfo = InkMetaLibrary.GetInkMetaFile(this);
				return _metaInfo;
			} set {
				_metaInfo = value;
			}
		}

		// The file path relative to the Assets folder (Assets/Ink/Story.ink)
		public string filePath {
			get {
				if(inkAsset == null) 
					return null;

				return InkEditorUtils.SanitizePathString(AssetDatabase.GetAssetPath(inkAsset));
			}
		}

		// The full file path (C:/Users/Inkle/HeavensVault/Assets/Ink/Story.ink)
		public string absoluteFilePath {
			get {
				if(inkAsset == null) 
					return null;
				return InkEditorUtils.UnityRelativeToAbsolutePath(filePath);
			}
		}

		public string absoluteFolderPath {
			get {
				return InkEditorUtils.SanitizePathString(Path.GetDirectoryName(absoluteFilePath));
			}
		}

		// The path of any compiled json file. Relative to assets folder.
        public string jsonPath {
			get {
                var _filePath = filePath;
                Debug.Assert(_filePath != null, "File path for ink file is null! The ink library requires rebuilding.");

                DefaultAsset jsonFolder = jsonAssetPath;
                if (jsonFolder == null) // no path specified for this specific file
                {
                    if(InkSettings.Instance.defaultJsonAssetPath != null) 
                    {
                        // use default path in InkSettings
                        jsonFolder = InkSettings.Instance.defaultJsonAssetPath;
                    }

                    if (jsonFolder == null)
                    {
                        //fallback to same folder as .ink file
                        jsonFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(Path.GetDirectoryName(_filePath));
                    }
                }

                Debug.Assert(jsonFolder != null, "JSON folder not found for ink file at path "+_filePath);

                string jsonPath = AssetDatabase.GetAssetPath(jsonFolder);
                string strJsonAssetPath = InkEditorUtils.CombinePaths(jsonPath, Path.GetFileNameWithoutExtension(_filePath)) + ".json";
                return strJsonAssetPath;
			}
		}

		public string absoluteJSONPath {
			get {
				if(inkAsset == null) 
					return null;
				return InkEditorUtils.UnityRelativeToAbsolutePath(jsonPath);
			}
		}

		public InkFile (DefaultAsset inkAsset) {
			Debug.Assert(inkAsset != null);
			this.inkAsset = inkAsset;
		}

		public void FindCompiledJSONAsset () {
            Debug.Assert(inkAsset != null);
            jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);
		}

		public override string ToString () {
			return string.Format ("[InkFile: filePath={0}]", filePath);
		} 
	}
}