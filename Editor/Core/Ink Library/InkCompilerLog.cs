using System.Text.RegularExpressions;
using Debug = UnityEngine.Debug;

namespace Ink.UnityIntegration
{
    [System.Serializable]
	public class InkCompilerLog {
		public Ink.ErrorType type;
		public string content;
		public string relativeFilePath;
		public int lineNumber;

		public InkCompilerLog (Ink.ErrorType type, string content, string relativeFilePath, int lineNumber = -1) {
			this.type = type;
			this.content = content;
			this.relativeFilePath = relativeFilePath;
			this.lineNumber = lineNumber;
		}

		public string GetAbsoluteFilePath (InkFile masterInkFile) {
			Debug.Log(masterInkFile.absoluteFolderPath);
			Debug.Log(relativeFilePath);
			return System.IO.Path.Combine(masterInkFile.absoluteFolderPath, relativeFilePath);
		}

		public static bool TryParse (string rawLog, out InkCompilerLog log) {
			var match = _errorRegex.Match(rawLog);
			if (match.Success) {
				Ink.ErrorType errorType = Ink.ErrorType.Author;
				string relativeFilePath = null;
				int lineNo = -1;
				string message = null;
				
				var errorTypeCapture = match.Groups["errorType"];
				if( errorTypeCapture != null ) {
					var errorTypeStr = errorTypeCapture.Value;
					if(errorTypeStr == "AUTHOR" || errorTypeStr == "TODO") errorType = Ink.ErrorType.Author;
					else if(errorTypeStr == "WARNING") errorType = Ink.ErrorType.Warning;
					else if(errorTypeStr == "ERROR") errorType = Ink.ErrorType.Error;
					else Debug.LogWarning("Could not parse error type from "+errorTypeStr);
				}
				
				var filenameCapture = match.Groups["filename"];
				if (filenameCapture != null)
					relativeFilePath = filenameCapture.Value;
				
				var lineNoCapture = match.Groups["lineNo"];
				if (lineNoCapture != null)
					lineNo = int.Parse (lineNoCapture.Value);
				
				var messageCapture = match.Groups["message"];
				if (messageCapture != null)
					message = messageCapture.Value.Trim();
				log = new InkCompilerLog(errorType, message, relativeFilePath, lineNo);
				return true;
			} else {
				Debug.LogWarning("Could not parse InkFileLog from log: "+rawLog);
				log = null;
				return false;
			}
		}
		private static Regex _errorRegex = new Regex(@"(?<errorType>ERROR|WARNING|TODO):(?:\s(?:'(?<filename>[^']*)'\s)?line (?<lineNo>\d+):)?(?<message>.*)");
	}
}