using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using Debug = UnityEngine.Debug;
using System.Collections;
using System.Collections.Generic;

using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using Object = UnityEngine.Object;

namespace Ink.UnityIntegration {
	// Helper class for ink files that maintains INCLUDE connections between ink files
	[System.Serializable]
	public sealed class InkFile {
		
		private const string includeKey = "INCLUDE ";

		// The full file path
		public string absoluteFilePath;
		public string absoluteFolderPath;
		// The file path relative to the Assets folder
		public string filePath;

		// The content of the .ink file
		public string fileContents;

		// A reference to the ink file (UnityEngine.DefaultAsset)
		public Object inkFile;

		// If file that contains this file as an include, if one exists. (UnityEngine.DefaultAsset)
		public Object master;

		// The files included by this file (UnityEngine.DefaultAsset)
		public List<Object> includes;

		// The compiled json file. Use this to start a story.
		public TextAsset jsonAsset;

		public List<string> errors = new List<string>();
		public List<string> warnings = new List<string>();
		public List<string> todos = new List<string>();

		public InkFile (string absoluteFilePath) {
			this.absoluteFilePath = absoluteFilePath;
			absoluteFolderPath = Path.GetDirectoryName(absoluteFilePath);
			filePath = absoluteFilePath.Substring(Application.dataPath.Length-6);
			inkFile = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath);
			Refresh();
		}

		public void Refresh () {
			fileContents = File.OpenText(absoluteFilePath).ReadToEnd();
			GetIncludedFiles();
		}

		public void GetIncludedFiles () {
			List<string> includedFilePaths = GetIncludedFilePaths();
			includes.Clear();
			foreach(string includePath in includedFilePaths) {
				includes.Add(AssetDatabase.LoadAssetAtPath<DefaultAsset>(includePath.Substring(Application.dataPath.Length-6)));
			}
		}
		
		private List<string> GetIncludedFilePaths() {
			if (String.IsNullOrEmpty(includeKey))
				throw new ArgumentException("the string to find may not be empty", "value");
			List<string> includePaths = new List<string>();
			for (int index = 0;;) {
				index = fileContents.IndexOf(includeKey, index);
				if (index == -1)
					return includePaths;
				
				int lastIndex = fileContents.IndexOf("\n", index);
				if(lastIndex == -1) {
					index += includeKey.Length;
				} else {
					includePaths.Add(Path.Combine(absoluteFolderPath, fileContents.Substring(index + includeKey.Length, lastIndex - (index+ + includeKey.Length))));
					index = lastIndex;
				}
			}
		}

		public void FindCompiledJSONAsset () {
			jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath))+".json");
		}
	}
}