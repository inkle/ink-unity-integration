using UnityEngine;
using UnityEditor;
using System;
using System.IO;
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
		private const string includeKey = "INCLUDE ";

		public bool compileAutomatically = false;

		// The full file path
		public string absoluteFilePath {
			get {
				if(inkAsset == null) 
					return null;
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
				return AssetDatabase.GetAssetPath(inkAsset);
			}
		}

		// The content of the .ink file
		public string fileContents;

		// A reference to the ink file
		public DefaultAsset inkAsset;

		// If file that contains this file as an include, if one exists.
		public DefaultAsset master;
		public InkFile masterInkFile {
			get {
				if(master == null)
					return null;
				else
					return InkLibrary.GetInkFileWithFile(master);
			}
		}

		// The files included by this file
		public List<DefaultAsset> includes = new List<DefaultAsset>();

		// Is this ink file a master file, or is it included by another file?
		public bool isMaster {
			get {
				return master == null;
			}
		}

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

		public int lastCompileDateTime;

		public List<DefaultAsset> circularIncludeReferences = new List<DefaultAsset>();
		public bool hasCircularIncludeReferences {
			get {
				return circularIncludeReferences.Count > 0;
			}
		}

		[System.Serializable]
		public class InkFileLog {
			public string content;
			public int lineNumber;

			public InkFileLog (string content, int lineNumber = -1) {
				this.content = content;
				this.lineNumber = lineNumber;
			}
		}

		public InkFile (DefaultAsset inkFile) {
			Debug.Assert(inkFile != null);
			this.inkAsset = inkFile;
//			fileContents = File.OpenText(absoluteFilePath).ReadToEnd();
//			GetIncludedFiles();
		}

		public void GetIncludedFiles () {
			string[] includedFilePaths = GetIncludedFilePaths();
			includes.Clear();
			if(includedFilePaths == null) 
				return;
			foreach(string includePath in includedFilePaths) {
				string localIncludePath = includePath.Substring(Application.dataPath.Length-6);
				DefaultAsset includedInkFileJSONAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(localIncludePath);
				InkFile includedInkFile = InkLibrary.GetInkFileWithFile(includedInkFileJSONAsset);
				if(includedInkFile == null)
					Debug.LogError("Expected Ink file at "+localIncludePath+" but file was not found.");
				else if (includedInkFile.includes.Contains(inkAsset)) {
					includedInkFile.includes.Remove(inkAsset);
					includedInkFile.circularIncludeReferences.Add(inkAsset);
					circularIncludeReferences.Add(includedInkFile.inkAsset);
					Debug.LogError("Circular INCLUDE reference between "+filePath+" and "+includedInkFile.filePath+". Neither files will be compiled until this is resolved.");
				} else
					includes.Add(includedInkFileJSONAsset);
			}
		}
		
		private string[] GetIncludedFilePaths() {
			string inklecatePath = InkEditorUtils.GetInklecateFilePath();
			if(inklecatePath == null) {
				UnityEngine.Debug.LogWarning("Inklecate (the ink compiler) not found in assets. This will prevent automatic building of JSON TextAsset files from ink story files.");
				return null;
			}
			string inkArguments = "-e "+"\""+Path.GetFileName(filePath) +"\"";
			Process process = new Process();
			process.StartInfo.WorkingDirectory = absoluteFolderPath;
			process.StartInfo.FileName = inklecatePath;
			process.StartInfo.Arguments = inkArguments;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute = false;
			process.EnableRaisingEvents = true;
			process.Start();
			process.WaitForExit();
			string[] splitOutput = process.StandardOutput.ReadToEnd().Split(new string[]{"\n"}, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < splitOutput.Length; i++) {
				splitOutput [i] = Path.Combine(absoluteFolderPath, splitOutput [i]);
			}
			return splitOutput;
		}

		public void FindCompiledJSONAsset () {
			jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath))+".json");
		}
	}
}