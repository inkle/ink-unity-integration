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
		}

		public void GetIncludedFiles () {
			InkIncludeParser includeParser = new InkIncludeParser(fileContents);
			includes.Clear();
			foreach(string includePath in includeParser.includeFilenames) {
				string localIncludePath = Path.Combine(Path.GetDirectoryName(filePath), includePath);
				DefaultAsset includedInkFileJSONAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(localIncludePath);
				InkFile includedInkFile = InkLibrary.GetInkFileWithFile(includedInkFileJSONAsset);
				if(includedInkFile == null)
					Debug.LogError("Expected Ink file at "+localIncludePath+" but file was not found.");
				else if (includedInkFile.includes.Contains(inkAsset)) {
//					includedInkFile.includes.Remove(inkAsset);
//					includedInkFile.circularIncludeReferences.Add(inkAsset);
//					circularIncludeReferences.Add(includedInkFile.inkAsset);
					Debug.LogError("Circular INCLUDE reference between "+filePath+" and "+includedInkFile.filePath+". Neither files will be compiled until this is resolved.");
				} else
					includes.Add(includedInkFileJSONAsset);
			}
		}

		public void FindCompiledJSONAsset () {
			jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath))+".json");
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
	            var includeRegex = new Regex (@"^\s*INCLUDE\s+(.+)$", RegexOptions.Multiline);
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