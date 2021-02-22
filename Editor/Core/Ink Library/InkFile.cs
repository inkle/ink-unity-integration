using UnityEngine;
using UnityEditor;
using System.IO;
using Debug = UnityEngine.Debug;
using System.Collections.Generic;
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Ink.UnityIntegration {
	// Helper class for ink files that maintains INCLUDE connections between ink files
	[System.Serializable]
	public class InkFile {
		
		public bool compileAutomatically = false;
		// A reference to the ink file
		public DefaultAsset inkAsset;

        //specify json destination folder (if None, default to same folder as ink file)
        public DefaultAsset jsonAssetDirectory;

		// The compiled json file. Use this to start a story.
		public TextAsset jsonAsset;

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

                DefaultAsset jsonFolder = jsonAssetDirectory;
                if (jsonFolder == null) // no path specified for this specific file
                {
                    if(InkSettings.instance.defaultJsonAssetPath != null) 
                    {
                        // use default path in InkSettings
                        jsonFolder = InkSettings.instance.defaultJsonAssetPath;
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






		// Fatal unhandled errors that should be reported as compiler bugs.
		public List<string> unhandledCompileErrors = new List<string>();
		public bool hasUnhandledCompileErrors {
			get {
				return unhandledCompileErrors.Count > 0;
			}
		}

		// Fatal errors caused by errors in the user's ink script.
		public List<InkCompilerLog> errors = new List<InkCompilerLog>();
		public bool hasErrors {
			get {
				return errors.Count > 0;
			}
		}

		public List<InkCompilerLog> warnings = new List<InkCompilerLog>();
		public bool hasWarnings {
			get {
				return warnings.Count > 0;
			}
		}

		public List<InkCompilerLog> todos = new List<InkCompilerLog>();
		public bool hasTodos {
			get {
				return todos.Count > 0;
			}
		}

		public bool requiresCompile {
			get {
				if(!isMaster) return false;
				return jsonAsset == null || lastEditDate > lastCompileDate || hasUnhandledCompileErrors;
			}
		}

		/// <summary>
		/// Gets the last compile date of the story.
		/// </summary>
		/// <value>The last compile date of the story.</value>
		public DateTime lastCompileDate {
			get {
				if(isMaster) {
					string fullJSONFilePath = InkEditorUtils.UnityRelativeToAbsolutePath(AssetDatabase.GetAssetPath(jsonAsset));
					return File.GetLastWriteTime(fullJSONFilePath);
				} else {
					return default(DateTime);
				}
			}
		}

		/// <summary>
		/// Gets the last edit date of the file.
		/// </summary>
		/// <value>The last edit date of the file.</value>
		public DateTime lastEditDate {
			get {
				return File.GetLastWriteTime(absoluteFilePath);
			}
		}

		// File that contains this file as an include, if one exists.
		public List<DefaultAsset> parents;
		public IEnumerable<InkFile> parentInkFiles {
			get {
				if(parents != null && parents.Count != 0) {
					foreach(var parentInkAsset in parents) {
						yield return InkLibrary.GetInkFileWithFile(parentInkAsset);
					}
				}
			}
		}
		// Is this ink file a parent file?
		public bool isParent {
			get {
				return includes.Count > 0;
			}
		}

		public List<DefaultAsset> masterInkAssets;
		public IEnumerable<InkFile> masterInkFiles {
			get {
				if(masterInkAssets != null && masterInkAssets.Count != 0) {
					foreach(var masterInkAsset in masterInkAssets) {
						yield return InkLibrary.GetInkFileWithFile(masterInkAsset);
					}
				}
			}
		}
		public IEnumerable<InkFile> masterInkFilesIncludingSelf {
			get {
				if(isMaster) yield return this;
				else {
					foreach(var masterInkFile in masterInkFiles) {
						yield return masterInkFile;
					}
				}
			}
		}
		public DefaultAsset masterInkAsset;

		// Is this ink file a master file?
		public bool isMaster {
			get {
				return masterInkAssets == null || masterInkAssets.Count == 0;
			}
		}
		

		// The files included by this file
		// We cache the paths of the files to be included for performance, giving us more freedom to refresh the actual includes list without needing to parse all the text.
		public List<string> includePaths = new List<string>();
		public List<DefaultAsset> includes = new List<DefaultAsset>();
		// The InkFiles of the includes of this file
		public List<InkFile> includesInkFiles {
			get {
				List<InkFile> _includesInkFiles = new List<InkFile>();
				foreach(var child in includes) {
					if(child == null) {
						Debug.LogError("Error compiling ink: Ink file include in "+filePath+" is null.", inkAsset);
						continue;
					}
					_includesInkFiles.Add(InkLibrary.GetInkFileWithFile(child));
				}
				return _includesInkFiles;
			}
		}
		// The InkFiles in the include hierarchy of this file.
		public List<InkFile> inkFilesInIncludeHierarchy {
			get {
				List<InkFile> _inkFilesInIncludeHierarchy = new List<InkFile>();
				_inkFilesInIncludeHierarchy.Add(this);
				foreach(var child in includesInkFiles) {
					if (child == null)
						return null;
					_inkFilesInIncludeHierarchy.AddRange(child.inkFilesInIncludeHierarchy);
				}
				return _inkFilesInIncludeHierarchy;
			}
		}
	    




		public InkFile (DefaultAsset inkAsset) {
			Debug.Assert(inkAsset != null);
			this.inkAsset = inkAsset;
			
			ParseContent();
		}

		public void FindCompiledJSONAsset () {
            Debug.Assert(inkAsset != null);
            jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);
		}



		


		


//		public string content;
		// The contents of the .ink file
		public string GetFileContents () {
			if(inkAsset == null) {
				Debug.LogWarning("Ink file asset is null! Rebuild library using Assets > Rebuild Ink Library");
				return "";
			}
			return File.ReadAllText(absoluteFilePath);
		}

		public void ParseContent () {
			includePaths.Clear();
			includePaths.AddRange(InkIncludeParser.ParseIncludes(GetFileContents()));
		}

		public void FindIncludedFiles (bool addMissing = false) {
			includes.Clear();
			foreach(string includePath in includePaths) {
				string localIncludePath = InkEditorUtils.CombinePaths(Path.GetDirectoryName(filePath), includePath);
				// This enables parsing ..\ and the like. Can we use Path.GetFullPath instead?
				var fullIncludePath = new FileInfo(localIncludePath).FullName;
				localIncludePath = InkEditorUtils.AbsoluteToUnityRelativePath(fullIncludePath);
				DefaultAsset includedInkFileAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(localIncludePath);
				if(includedInkFileAsset == null) {
					Debug.LogError(filePath+ " expected child .ink asset at '"+localIncludePath+"' but file was not found.", inkAsset);
				} else {
					InkFile includedInkFile = InkLibrary.GetInkFileWithFile(includedInkFileAsset, addMissing);
					if(includedInkFile == null) {
						Debug.LogError(filePath+ " expected child InkFile from .ink asset at '"+localIncludePath+"' but file was not found.", inkAsset);
					} else if (includedInkFile.includes.Contains(inkAsset)) {
						Debug.LogError("Circular INCLUDE reference between '"+filePath+"' and '"+includedInkFile.filePath+"'.", inkAsset);
					} else
						includes.Add(includedInkFileAsset);
				}
			}
		}

		public static class InkIncludeParser {
			static Regex _includeRegex;
			static Regex includeRegex {
				get {
					if(_includeRegex == null) {
						_includeRegex = new Regex (@"^\s*INCLUDE\s+([^\r\n]+)\r*$", RegexOptions.Multiline);
					}
					return _includeRegex;
				}
			}
	        public static IEnumerable<string> ParseIncludes (string inkContents) {
	            return FindIncludes (EliminateComments(inkContents));
	        }

	        static string EliminateComments(string inkStr) {
	            var sb = new StringBuilder ();
	            int idx = 0;
	            while(idx < inkStr.Length) {
	                var commentStarterIdx = inkStr.IndexOf ('/', idx);
	                // Final string?
	                if (commentStarterIdx == -1 || commentStarterIdx >= inkStr.Length-2 ) {
	                    sb.Append (inkStr.Substring (idx, inkStr.Length - idx));
	                    break;
	                }
	                sb.Append (inkStr.Substring (idx, commentStarterIdx - idx));
	                var commentStarter = inkStr.Substring (commentStarterIdx, 2);
	                if (commentStarter == "//" || commentStarter == "/*") {
	                    int endOfCommentIdx = -1;
	                    // Single line comments
	                    if (commentStarter == "//") {
	                        endOfCommentIdx = inkStr.IndexOf ('\n', commentStarterIdx);
	                        if (endOfCommentIdx == -1)
	                            endOfCommentIdx = inkStr.Length;
	                        else if (inkStr [endOfCommentIdx - 1] == '\r')
	                            endOfCommentIdx = endOfCommentIdx - 1;
	                    } 
	                    // Block comments
	                    else if (commentStarter == "/*") {
	                        endOfCommentIdx = inkStr.IndexOf ("*/", idx);
	                        if (endOfCommentIdx == -1)
	                            endOfCommentIdx = inkStr.Length;
	                        else
	                            endOfCommentIdx += 2;
	                        // If there are *any* newlines, we should add one in here,
	                        // so that lines are spit up correctly
	                        if (inkStr.IndexOf ('\n', commentStarterIdx, endOfCommentIdx - commentStarterIdx) != -1)
	                            sb.Append ("\n");
	                    }
	                    // Skip over comment
	                    if (endOfCommentIdx > -1)
	                        idx = endOfCommentIdx;
	                } 
	                // Normal slash we need, not a comment
	                else {
	                    sb.Append ("/");
	                    idx = commentStarterIdx + 1;
	                }
	            }
	            return sb.ToString ();
	        }
	        static IEnumerable<string> FindIncludes(string str) {
	            MatchCollection matches = includeRegex.Matches(str);
	            foreach (Match match in matches)
	            {
	                var capture = match.Groups [1].Captures [0];
	                yield return capture.Value;
	            }
	        }
		}

		
		public override string ToString () {
			return string.Format ("[InkFile: filePath={0}]", filePath);
		} 
	}
}