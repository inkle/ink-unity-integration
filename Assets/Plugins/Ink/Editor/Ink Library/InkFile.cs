using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Debug = UnityEngine.Debug;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using Object = UnityEngine.Object;

namespace Ink.UnityIntegration {
	// Helper class for ink files that maintains INCLUDE connections between ink files
	[System.Serializable]
	public sealed class InkFile {
		
		public bool compileAutomatically = false;
		// A reference to the ink file
		public DefaultAsset inkAsset;

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


		// The full file path
		public string absoluteFilePath {
			get {
				if(inkAsset == null) 
					return null;
				
				return InkEditorUtils.CombinePaths(Application.dataPath, filePath.Substring(7));
			}
		}
		public string absoluteFolderPath {
			get {
				return InkEditorUtils.SanitizePathString(Path.GetDirectoryName(absoluteFilePath));
			}
		}
		// The file path relative to the Assets folder
		public string filePath {
			get {
				return InkEditorUtils.SanitizePathString(AssetDatabase.GetAssetPath(inkAsset));
			}
		}


		public InkFile (DefaultAsset inkAsset) {
			Debug.Assert(inkAsset != null);
			this.inkAsset = inkAsset;
		}

		public void FindCompiledJSONAsset () {
			string jsonAssetPath = InkEditorUtils.CombinePaths(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath)) + ".json";
			jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonAssetPath);
		}

		public override string ToString () {
			return string.Format ("[InkFile: filePath={0}]", filePath);
		} 
	}

}