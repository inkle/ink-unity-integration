using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Ink.UnityIntegration {
	// Helper class for ink files that maintains INCLUDE connections between ink files
	[Serializable]
    // FIXME: do we still need the concept of includes and master files?
	public class InkFile : ScriptableObject {
    // Ink version. This should really come from the core ink code.
		public static System.Version inkVersionCurrent = new System.Version(1,2,0);
		public static System.Version unityIntegrationVersionCurrent = new System.Version(1,2,1);

		public string storyJson;

		public bool isMaster => !isIncludeFile;

		// Fatal unhandled errors that should be reported as compiler bugs.
		public List<string> unhandledCompileErrors = new List<string>();
		public bool hasUnhandledCompileErrors => unhandledCompileErrors.Count > 0;

		public List<string> recursiveIncludeErrorPaths = new List<string>();
		public bool hasRecursiveIncludeErrorPaths => recursiveIncludeErrorPaths.Count > 0;

		// Fatal errors caused by errors in the user's ink script.
		public List<InkCompilerLog> errors = new List<InkCompilerLog>();
		public bool hasErrors => errors.Count > 0;

		public List<InkCompilerLog> warnings = new List<InkCompilerLog>();
		public bool hasWarnings => warnings.Count > 0;

		public List<InkCompilerLog> todos = new List<InkCompilerLog>();
		public bool hasTodos => todos.Count > 0;

		/// <summary>
		/// Gets the last edit date of the file.
		/// </summary>
		/// <value>The last edit date of the file.</value>
		public DateTime lastEditDate => File.GetLastWriteTime(AssetDatabase.GetAssetPath(this));

		public List<InkFile> masterInkAssets = new List<InkFile>();
		public IEnumerable<InkFile> masterInkFiles {
			get {
				// FIXME
				return new InkFile[0];
				// foreach(var masterInkAsset in masterInkAssets) {
				// 	yield return InkLibrary.GetInkFileWithFile(masterInkAsset);
				// }
			}
		}
		public IEnumerable<InkFile> masterInkFilesIncludingSelf {
			get {
				// A file can be both a master file AND be included by many other files. Return all the master files fitting this description.
				if(isMaster) yield return this;
				foreach(var masterInkFile in masterInkFiles) {
					yield return masterInkFile;
				}
			}
		}

		// Is this ink file included by another ink file?
		public bool isIncludeFile => masterInkAssets.Count > 0;


		// The files referenced by this file via the INCLUDE keyword
		// We cache the paths of the files to be included for performance, giving us more freedom to refresh the actual includes list without needing to parse all the text.
		public List<string> localIncludePaths = new List<string>();
		// The asset references for the included files. Unlike localIncludePaths this contains include files
		public List<InkFile> includes = new List<InkFile>();
		// The InkFiles of the includes of this file
		public List<InkFile> includesInkFiles {
			get {
				List<InkFile> _includesInkFiles = new List<InkFile>();
				foreach(var child in includes) {
					if(child == null) {
						// FIXME:
						// Debug.LogError("Error compiling ink: Ink file include in "+filePath+" is null.", inkAsset);
						continue;
					}
					// FIXME:
					// _includesInkFiles.Add(InkLibrary.GetInkFileWithFile(child));
				}
				return _includesInkFiles;
			}
		}

		public void ClearAllHierarchyConnections() {
			masterInkAssets.Clear();
			includes.Clear();
		}

		// Returns the contents of the .ink file.
		public string GetFileContents () {
			// FIXME:
			// if(inkAsset == null) {
			// 	Debug.LogWarning("Ink file asset is null! Rebuild library using Assets > Rebuild Ink Library");
			// 	return "";
			// }
			// return File.ReadAllText(absoluteFilePath);
			return string.Empty;
		}

		// Parses the ink file and caches any information we may want to access without incurring a performance cost.
		// Currently this only scans for includePaths, which are later used by InkLibrary.RebuildInkFileConnections.
		public void ParseContent () {
			localIncludePaths.Clear();
			localIncludePaths.AddRange(InkIncludeParser.ParseIncludes(GetFileContents()));
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
			// FIXME:
			return string.Empty;
			// return $"[InkFile: filePath={filePath}]";
		} 
	}
}