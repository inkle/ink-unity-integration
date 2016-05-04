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

namespace Ink.UnityIntegration {
	// Helper class for ink files that maintains INCLUDE connections between ink files
	public class InkFile {
		
		// Constant value, used for searching for INCLUDE files in an InkFile.
		private const string includeKey = "INCLUDE ";
		
		// Constant for the number of characters in the word 'Assets', used for 
		// shortening paths when looking for .ink files in the Assets dir.
		private const int CharactersInAssets = 6;

		// The full file path
		public string absoluteFilePath;
		
		// The full file path to the .ink file represented by this object.
		public string absoluteFolderPath;
		
		// The file path relative to the Assets folder
		public string filePath;

		// The content of the .ink file
		public string fileContents;
		
		// The paths of the files included by this file
		public List<string> includePaths;
		
		// The loaded files included by this file
		public List<InkFile> includes;
		
		// If this file is included by another, the other is the master file.
		public InkFile master;

		// A reference to the ink file (UnityEngine.DefaultAsset)
		public UnityEngine.Object inkFile;
		
		// The compiled json file. Use this to start a story.
		public TextAsset jsonAsset;

		// CONSTRUCTOR.
		//		Param - string absFilePath - the absolute file path of the InkFile to be represented by this InkFile instance.
		public InkFile (string absoluteFilePath) {
			// Set absolute file
			this.absoluteFilePath = absoluteFilePath;
			absoluteFolderPath = Path.GetDirectoryName(absoluteFilePath);
			
			// Generate the path relative to the Assets directory.
			filePath = absoluteFilePath.Substring(Application.dataPath.Length - CharactersInAssets);
			
			// Read in and set the files contents on 'fileContents'
			fileContents = File.OpenText(absoluteFilePath).ReadToEnd();
			
			// Register a Unity Asset with the Asset Database.
			inkFile = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath);
			
			// find files 'included' by this ink file and its 'master' file.
			GetIncludePaths();
		}

		// Determines the paths of .ink files included by this .ink file, as well as the 'Master' file for this InkFIle.
		private void GetIncludePaths() {
			includePaths = new List<string>();
			
			// While True...start to search from character 0 for included files.
			for (int index = 0;;) {
				index = fileContents.IndexOf(includeKey, index);
				if (index == -1)
					return;
				
				// If we DID find an include file, we need to process it.
				// --- Start searching at the index of where we foudn our file and find the end 
				// --- of the line - this is the index of the character at the end of our include 
				// --- file.
				int lastIndex = fileContents.IndexOf("\n", index);
				if(lastIndex == -1) {
					// If we didn't find a newline char, then set the current index to after our last INCLUDE, and loop (ie search again).
					index += includeKey.Length;
				} else {
					// If we did, then that means we have a new include file to add to the includePaths List.
					// Sanitize file separators before adding to includePaths
					String toBeIncluded = Path.Combine(absoluteFolderPath, fileContents.Substring(index + IncludeKey.Length, lastIndex - (index+ + IncludeKey.Length)));
					toBeIncluded = toBeIncluded.Replace ('\\', '/');
					includePaths.Add(toBeIncluded);
					
					// update our index to after the last found INCLUDE and loop  (ie search again).
					index = lastIndex;
				}
			}
		}
		
		// Finds include files from paths and the list of all the ink files to check.
		// 		param - InkFile[] inkFiles - the inkfiles to search.  This method identifies this InkFile instance's 'Master' 
		//									 file as well as identifying InkFile that it includes.
		public void GetIncludes (InkFile[] inkFiles) {
			includes = new List<InkFile>();
			foreach (InkFile inkFile in inkFiles) {
				if(includePaths.Contains(inkFile.absoluteFilePath)) {
					inkFile.master = this;
					includes.Add(inkFile);
				}
			}
		}

		public void FindCompiledJSONAsset () {
			jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath))+".json");
		}
	}
}