using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Ink.UnityIntegration {
	/// <summary>
	/// Parses INCLUDE statements out of ink source text (ignoring commented-out ones). Used to work out
	/// the include graph without compiling.
	/// </summary>
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
}
