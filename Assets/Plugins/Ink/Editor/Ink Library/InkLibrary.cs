using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using UnityEditorInternal;
using Debug = UnityEngine.Debug;
using Ink.Runtime;

/// <summary>
/// Holds a reference to an InkFile object for every .ink file detected in the Assets folder.
/// Provides helper functions to easily obtain these files.
/// </summary>
namespace Ink.UnityIntegration {
	public static class InkLibrary {
		public static InkFile[] inkLibrary;

		/// <summary>
		/// Updates the ink library. Executed whenever an ink file is changed by InkToJSONPostProcessor
		/// Can be called manually, but incurs a performance cost.
		/// </summary>
		public static void Refresh () {
			string[] inkFilePaths = Directory.GetFiles(Application.dataPath, "*.ink", SearchOption.AllDirectories);
			inkLibrary = new InkFile[inkFilePaths.Length];
			for (int i = 0; i < inkFilePaths.Length; i++) {
				inkLibrary [i] = new InkFile (inkFilePaths [i]);
			}
			foreach (InkFile inkFile in inkLibrary) {
				if(inkFile.includePaths.Count > 0) {
					inkFile.GetIncludes(inkLibrary);
				}
			}
			foreach (InkFile inkFile in inkLibrary) {
				inkFile.FindCompiledJSONAsset();
			}
		}

		public static List<InkFile> GetMasterInkFiles () {
			List<InkFile> masterInkFiles = new List<InkFile>();
			foreach (InkFile inkFile in inkLibrary) {
				if(inkFile.master == null) {
					masterInkFiles.Add(inkFile);
				}
			}
			return masterInkFiles;
		}

		/// <summary>
		/// Gets the ink file with path relative to Assets folder, for example: "Assets/Ink/myStory.ink".
		/// </summary>
		/// <returns>The ink file with path.</returns>
		/// <param name="path">Path.</param>
		public static InkFile GetInkFileWithPath (string path) {
			foreach(InkFile inkFile in inkLibrary) {
				if(inkFile.filePath == path) {
					return inkFile;
				}
			}
			return null;
		}

		/// <summary>
		/// Gets the ink file with absolute path.
		/// </summary>
		/// <returns>The ink file with path.</returns>
		/// <param name="path">Path.</param>
		public static InkFile GetInkFileWithAbsolutePath (string absolutePath) {
			foreach(InkFile inkFile in inkLibrary) {
				if(inkFile.absoluteFilePath == absolutePath) {
					return inkFile;
				}
			}
			return null;
		}
	}	
}