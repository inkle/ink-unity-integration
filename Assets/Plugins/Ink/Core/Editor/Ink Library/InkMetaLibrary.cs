using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;

/// <summary>
/// Holds a reference to an InkFile object for every .ink file detected in the Assets folder.
/// Provides helper functions to easily obtain these files.
/// </summary>
namespace Ink.UnityIntegration {

	[System.Serializable]
	public class InkMetaLibrary {
		public static bool created {
			get {
				return _Instance != null || FindLibrary() != null;
			}
		}
		private static InkMetaLibrary _Instance;
		public static InkMetaLibrary Instance {
			get {
				if(_Instance == null) {
					_Instance = FindOrCreateLibrary();
					foreach(var inkFile in InkLibrary.Instance.inkLibrary) {
						inkFile.metaInfo = GetInkMetaFile(inkFile);
					}
				}
				return _Instance;
			}
		}
		public const string pathPlayerPrefsKeyPrefix = "InkMetaLibrary";
		public static string pathPlayerPrefsKey {
			get {
				return pathPlayerPrefsKeyPrefix+" "+Application.productName;
			}
		}

		public List<InkMetaFile> metaLibrary = new List<InkMetaFile>();

		private static InkMetaLibrary FindOrCreateLibrary () {
			_Instance = FindLibrary();
			// If we couldn't find the asset in the project, create a new one.
			if(_Instance == null) {
				_Instance = new InkMetaLibrary();
				InkMetaLibrary.Rebuild();
			} else {
				bool valid = ValidateLibrary();
				if(!valid) {
					RebuildInkFileConnections();
					Debug.LogWarning("Ink Meta Library data corrupted. This can happen when Unity updates its library. Updated file connections but recommend recompiling.");
				}
			}
			return _Instance;
		}

		private static InkMetaLibrary FindLibrary () {
			if(EditorPrefs.HasKey(pathPlayerPrefsKey)) {
				try
				{
					InkMetaLibrary library = new InkMetaLibrary();
					var libraryJSON = EditorPrefs.GetString(pathPlayerPrefsKey);
					EditorJsonUtility.FromJsonOverwrite(libraryJSON, library);
					return library;
				} catch {
					return null;
				}
			}
			return null;
		}
        
        // Detects and fixes missing/bad file references.
        // Unity has a nasty habit of changing GUIDs on launch which this can detect and correct by comparing file paths with the file
		private static bool ValidateLibrary () {
			bool foundDiscrepancy = false;
			foreach(var metaFile in _Instance.metaLibrary) {
				if(metaFile.inkAsset == null || AssetDatabase.GetAssetPath(metaFile.inkAsset) != metaFile.inkAssetPath) {
					metaFile.inkAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(metaFile.inkAssetPath);
					if(metaFile.inkAsset == null) {
						foundDiscrepancy = true;
						Debug.LogWarning("Ink file for asset "+AssetDatabase.GetAssetPath(metaFile.inkAsset)+" was not found. Path was "+metaFile.inkAssetPath);
					}
				}
                // If the library is invalid then metaFile.masterInkAsset can return some random file instead, which causes errors in metaFile.masterInkFile's getter.
                // Since I can't rely on masterInkAsset being correct between opening/closing Unity I think this function should always set it from masterInkAssetPath.
                // For optimisation reasons we can probably check if it's already loaded/accurate first.
                // Do the same with the above too!
				if(metaFile.masterInkAssetPath != string.Empty && (metaFile.masterInkAsset == null || AssetDatabase.GetAssetPath(metaFile.masterInkAsset) != metaFile.masterInkAssetPath)) {
                	metaFile.masterInkAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(metaFile.masterInkAssetPath);
					if(metaFile.masterInkAsset == null) {
						foundDiscrepancy = true;
						Debug.LogWarning("Ink file for master asset "+AssetDatabase.GetAssetPath(metaFile.masterInkAsset)+" was not found. Path was "+metaFile.masterInkAssetPath);
					}
				}
			}
			return !foundDiscrepancy;
		}

		public static void Rebuild () {
			Instance.metaLibrary.Clear();
			foreach(var inkFile in InkLibrary.Instance.inkLibrary) {
				inkFile.metaInfo.inkAssetPath = AssetDatabase.GetAssetPath(inkFile.metaInfo.inkAsset);
				inkFile.metaInfo.masterInkAssetPath = AssetDatabase.GetAssetPath(inkFile.metaInfo.masterInkAsset);
				Instance.metaLibrary.Add(inkFile.metaInfo);
			}
		}

		public static void Save () {
			Rebuild();
			var instanceJSON = EditorJsonUtility.ToJson(Instance);
			EditorPrefs.SetString(pathPlayerPrefsKey, instanceJSON);
		}

		public static InkMetaFile GetInkMetaFile (InkFile inkFile) {
			if(Instance.metaLibrary == null) return null;
			foreach(var metaFile in Instance.metaLibrary) {
				if(metaFile.inkAsset == inkFile.inkAsset) {
					return metaFile;
				}
			}
			InkMetaFile meta = new InkMetaFile(inkFile);
			Instance.metaLibrary.Add(meta);
			return meta;
		}


		/// <summary>
		/// Rebuilds which files are master files and the connections between the files.
		/// </summary>
		public static void RebuildInkFileConnections () {
			Queue<InkFile> inkFileQueue = new Queue<InkFile>(InkLibrary.Instance.inkLibrary);
			while (inkFileQueue.Count > 0) {
				InkFile inkFile = inkFileQueue.Dequeue();
				inkFile.metaInfo.parent = null;
				inkFile.metaInfo.masterInkAsset = null;
				inkFile.metaInfo.ParseContent();
				inkFile.metaInfo.FindIncludedFiles(true);
				foreach (InkFile includedInkFile in inkFile.metaInfo.includesInkFiles) {
					if (!inkFileQueue.Contains(includedInkFile)) {
						inkFileQueue.Enqueue(includedInkFile);
					}
				}
			}
			// We now set the master file for ink files. As a file can be in an include hierarchy, we need to do this in two passes.
			// First, we set the master file to the file that includes an ink file.
			foreach (InkFile inkFile in InkLibrary.Instance.inkLibrary) {
				if(inkFile.metaInfo.includes.Count == 0) 
					continue;
				foreach (InkFile otherInkFile in InkLibrary.Instance.inkLibrary) {
					if(inkFile == otherInkFile) 
						continue;
					if(inkFile.metaInfo.includes.Contains(otherInkFile.inkAsset)) {
						otherInkFile.metaInfo.parent = inkFile.inkAsset;
					}
				}
			}
			// Next, we create a list of all the files owned by the actual master file, which we obtain by travelling up the parent tree from each file.
			Dictionary<InkFile, List<InkFile>> masterChildRelationships = new Dictionary<InkFile, List<InkFile>>();
			foreach (InkFile inkFile in InkLibrary.Instance.inkLibrary) {
				if(inkFile.metaInfo.parent == null) 
					continue;
				InkFile parent = inkFile.metaInfo.parentInkFile;
				while (parent.metaInfo.parent != null) {
					parent = parent.metaInfo.parentInkFile;
				}
				if(!masterChildRelationships.ContainsKey(parent)) {
					masterChildRelationships.Add(parent, new List<InkFile>());
				}
				masterChildRelationships[parent].Add(inkFile);
			}
			// Finally, we set the master file of the children
			foreach (var inkFileRelationship in masterChildRelationships) {
				foreach(InkFile childInkFile in inkFileRelationship.Value) {
					childInkFile.metaInfo.masterInkAsset = inkFileRelationship.Key.inkAsset;
					if(InkSettings.Instance.handleJSONFilesAutomatically && childInkFile.jsonAsset != null) {
						AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(childInkFile.jsonAsset));
						childInkFile.jsonAsset = null;
					}
				}
			}
			InkMetaLibrary.Save();
		}
	}	
}