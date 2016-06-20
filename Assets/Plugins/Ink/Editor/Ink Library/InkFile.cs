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

		// A reference to the ink file
		public DefaultAsset inkAsset;

		// If file that contains this file as an include, if one exists.
		public DefaultAsset parent;
		public InkFile parentInkFile {
			get {
				if(parent == null)
					return null;
				else
					return InkLibrary.GetInkFileWithFile(parent);
			}
		}
		// Is this ink file a parent file?
		public bool isParent {
			get {
				return includes.Count > 0;
			}
		}

		public DefaultAsset master;
		public InkFile masterInkFile {
			get {
				if(master == null)
					return null;
				else
					return InkLibrary.GetInkFileWithFile(master);
			}
		}
		// Is this ink file a master file?
		public bool isMaster {
			get {
				return master == null;
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
						Debug.LogError("Error compiling ink: Ink file include in "+filePath+" is null.");
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
				List<InkFile> _includesInkFiles = new List<InkFile>();
				foreach(var child in includesInkFiles) {
					_includesInkFiles.Add(child);
					_includesInkFiles.AddRange(child.inkFilesInIncludeHierarchy);
				}
				return _includesInkFiles;
			}
		}

		// The compiled json file. Use this to start a story.
		public TextAsset jsonAsset;

		// Fatal unhandled errors that should be reported as compiler bugs.
		public List<string> compileErrors = new List<string>();
		public bool hasCompileErrors {
			get {
				return errors.Count > 0;
			}
		}

		// Fatal errors caused by errors in the user's ink script.
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


		public bool requiresCompile {
			get {
				InkFile masterInkFileIncludingSelf = isMaster ? this : masterInkFile;
				if(masterInkFileIncludingSelf.jsonAsset == null) 
					return true;

				foreach(InkFile inkFile in masterInkFileIncludingSelf.inkFilesInIncludeHierarchy) {
					if(inkFile.hasCompileErrors) {
						return true;
					} else if(inkFile.hasErrors) {
						return true;
					} else if(inkFile.hasWarnings) {
						return true;
					} else if(inkFile.lastEditDate > lastCompileDate) {
						return true;
					}
				}
				return false;
			}
		}

		/// <summary>
		/// Gets the last compile date of the story.
		/// </summary>
		/// <value>The last compile date of the story.</value>
		public DateTime lastCompileDate {
			get {
				InkFile masterInkFileIncludingSelf = isMaster ? this : masterInkFile;
				string fullJSONFilePath = InkEditorUtils.CombinePaths(Application.dataPath, AssetDatabase.GetAssetPath(masterInkFileIncludingSelf.jsonAsset).Substring(7));
				return File.GetLastWriteTime(fullJSONFilePath);
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
		}

		// The contents of the .ink file
		public string GetFileContents () {
			return File.OpenText(absoluteFilePath).ReadToEnd();
		}

		public void ParseContent () {
			InkIncludeParser includeParser = new InkIncludeParser(GetFileContents());
			includePaths = includeParser.includeFilenames;
		}

		public void FindIncludedFiles () {
			includes.Clear();
			foreach(string includePath in includePaths) {
				string localIncludePath = InkEditorUtils.CombinePaths(Path.GetDirectoryName(filePath), includePath);
				DefaultAsset includedInkFileAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(localIncludePath);
				InkFile includedInkFile = InkLibrary.GetInkFileWithFile(includedInkFileAsset);
				if(includedInkFile == null) {
					Debug.LogError("Expected Ink file at "+localIncludePath+" but file was not found.");
				} else if (includedInkFile.includes.Contains(inkAsset)) {
					Debug.LogError("Circular INCLUDE reference between "+filePath+" and "+includedInkFile.filePath+".");
				} else
					includes.Add(includedInkFileAsset);
			}
		}

		public void FindCompiledJSONAsset () {
			string jsonAssetPath = InkEditorUtils.CombinePaths(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath)) + ".json";
			jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonAssetPath);
		}

		public class InkIncludeParser {
	        public InkIncludeParser (string inkContents)
	        {
	            _text = inkContents;
	        }
	        void Process()
	        {
	            _text = EliminateComments (_text);
	            FindIncludes (_text);
	        }
	        string EliminateComments(string inkStr)
	        {
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
	        void FindIncludes(string str)
	        {
	            _includeFilenames = new List<string> ();
	            var includeRegex = new Regex (@"^\s*INCLUDE\s+([^\r\n]+)\r*$", RegexOptions.Multiline);
	            MatchCollection matches = includeRegex.Matches(str);
	            foreach (Match match in matches)
	            {
	                var capture = match.Groups [1].Captures [0];
	                _includeFilenames.Add (capture.Value);
	            }
	        }
	            
	        public List<string> includeFilenames {
	            get {
	                if (_includeFilenames == null) {
	                    Process ();
	                }
	                return _includeFilenames;
	            }
	        }
	        List<string> _includeFilenames;
	        string _text;
	    }
	}
}