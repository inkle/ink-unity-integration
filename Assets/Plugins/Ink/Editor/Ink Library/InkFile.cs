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
		public string absoluteFilePath {
			get {
				return Path.Combine(Application.dataPath, filePath.Substring(7));
			}
		}
		public string absoluteFolderPath {
			get {
				return Path.GetDirectoryName(absoluteFilePath);
			}
		}
		// The file path relative to the Assets folder
		public string filePath {
			get {
				return AssetDatabase.GetAssetPath(inkFile);
			}
		}

		// The content of the .ink file
		public string fileContents;

		// A reference to the ink file (UnityEngine.DefaultAsset)
		public Object inkFile;

		// If file that contains this file as an include, if one exists. (UnityEngine.DefaultAsset)
		public Object master;

		// The files included by this file (UnityEngine.DefaultAsset)
		public List<Object> includes = new List<Object>();

		// The compiled json file. Use this to start a story.
		public TextAsset jsonAsset;


		public List<InkFileLog> errors = new List<InkFileLog>();
		public bool hasErrors {
			get {
				return errors.Count > 0;
			}
		}

		public List<InkFileLog> warnings = new List<InkFileLog>();
		public bool hasWarnings {
			get {
				return warnings.Count > 0;
			}
		}

		public List<InkFileLog> todos = new List<InkFileLog>();
		public bool hasTodos {
			get {
				return todos.Count > 0;
			}
		}
//		public int lastCompileDateTime;

		[System.Serializable]
		public class InkFileLog {
			public Object file;
			public string content;
			public int lineNumber;

			public InkFileLog (Object file, string content, int lineNumber = -1) {
				this.file = file;
				this.content = content;
				this.lineNumber = lineNumber;
			}
		}

		public InkFile (DefaultAsset inkFile) {
			Debug.Assert(inkFile != null);
			this.inkFile = inkFile;
//			this.filePath = AssetDatabase.GetAssetPath(inkFile);
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