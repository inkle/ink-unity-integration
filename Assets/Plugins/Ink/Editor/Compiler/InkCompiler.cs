using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;

using Debug = UnityEngine.Debug;

namespace Ink.UnityIntegration {
	[InitializeOnLoad]
	public static class InkCompiler {
		
		public static bool compiling {
			get {
				return InkLibrary.Instance.compilationStack.Count > 0;
			}
		}

		public delegate void OnCompileInkEvent (InkFile inkFile);
		public static event OnCompileInkEvent OnCompileInk;

		[Serializable]
		public class CompilationStackItem {
			public enum State {
				Idle,
				Compiling,
				Importing
			}

			public Process process;
			public State state = State.Idle;
			public InkFile inkFile;
			public string inkAbsoluteFilePath;
			public string jsonAbsoluteFilePath;
			public List<string> output = new List<string>();
			public List<string> errorOutput = new List<string>();
			public DateTime startTime;
			public float timeTaken;

			public CompilationStackItem () {
				startTime = DateTime.Now;
			}
		}

		static InkCompiler () {
			EditorApplication.playmodeStateChanged += OnPlayModeChange;
			EditorApplication.update += Update;
		}

		private static void Update () {
			if(!InkLibrary.created) 
				return;

			for (int i = InkLibrary.Instance.compilationStack.Count - 1; i >= 0; i--) {
				var compilingFile = InkLibrary.Instance.compilationStack [i];
				if (compilingFile.state == CompilationStackItem.State.Compiling) {
					compilingFile.timeTaken = (float)((DateTime.Now - compilingFile.startTime).TotalSeconds);
					if (compilingFile.timeTaken > InkSettings.Instance.compileTimeout) {
						if (compilingFile.process != null) {	
							compilingFile.process.Exited -= OnCompileProcessComplete;
							compilingFile.process.Kill ();
						}
						InkLibrary.Instance.compilationStack.RemoveAt(i);
						if(InkLibrary.Instance.compilationStack.Count == 0) EditorUtility.ClearProgressBar();
						Debug.LogError("Ink Compiler timed out for "+compilingFile.inkAbsoluteFilePath+".\n. Compilation should never take more than a few seconds, but for large projects or slow computers you may want to increase the timeout time in the InkSettings file.\nIf this persists there may be another issue; or else check an ink file exists at this path and try Assets/Recompile Ink, else please report as a bug with the following error log at this address: https://github.com/inkle/ink/issues\nError log:\n"+compilingFile.errorOutput);
					}
				} else if (compilingFile.state == CompilationStackItem.State.Importing) {
					// This covers a rare bug that I've not pinned down
					var timeTaken = (float)((DateTime.Now - compilingFile.startTime).TotalSeconds);
					if (timeTaken > InkSettings.Instance.compileTimeout + 2) {
						if (compilingFile.process != null) {	
							compilingFile.process.Exited -= OnCompileProcessComplete;
							compilingFile.process.Kill ();
						}
						InkLibrary.Instance.compilationStack.RemoveAt(i);
						if(InkLibrary.Instance.compilationStack.Count == 0) EditorUtility.ClearProgressBar();
						Debug.LogError("Ink Compiler timed out for "+compilingFile.inkAbsoluteFilePath+" while the file was importing.\n. Please report as a bug with the following error log at this address: https://github.com/inkle/ink/issues\nError log:\n"+compilingFile.errorOutput);
					}
				}
			}
			if(InkLibrary.Instance.compilationStack.Count > 0) {
				int numCompiling = InkLibrary.FilesInCompilingStackInState(CompilationStackItem.State.Compiling).Count;
				string message = "Compiling .Ink File "+(InkLibrary.Instance.compilationStack.Count-numCompiling)+" of "+InkLibrary.Instance.compilationStack.Count;
				float progress = 0;
				foreach (var compilingFile in InkLibrary.Instance.compilationStack) {
					if (compilingFile.state == CompilationStackItem.State.Compiling)
						progress += compilingFile.timeTaken / InkSettings.Instance.compileTimeout;
					if (compilingFile.state == CompilationStackItem.State.Importing)
						progress += 1;
				}
				progress /= InkLibrary.Instance.compilationStack.Count;
				EditorUtility.DisplayProgressBar("Compiling Ink...", message, progress);
			}
		}

		private static void OnPlayModeChange () {
			if(EditorApplication.isPlayingOrWillChangePlaymode) {
				if(compiling)
					Debug.LogWarning("Entered Play Mode while Ink was still compiling. Recommend exiting and re-entering play mode.");
			}
		}

		/// <summary>
		/// Starts a System.Process that compiles a master ink file, creating a playable JSON file that can be parsed by the Ink.Story class
		/// </summary>
		/// <param name="inkFile">Ink file.</param>
		public static void CompileInk (InkFile inkFile) {
			if(inkFile == null) {
				Debug.LogError("Tried to compile ink file "+inkFile.filePath+", but input was null.");
				return;
			}
			if(!inkFile.metaInfo.isMaster)
				Debug.LogWarning("Compiling InkFile which is an include. Any file created is likely to be invalid. Did you mean to call CompileInk on inkFile.master?");
			if(InkLibrary.GetCompilationStackItem(inkFile) != null) {
				UnityEngine.Debug.LogWarning("Tried compiling ink file, but file is already compiling. "+inkFile.filePath);
				return;
			}

			string inklecatePath = InkEditorUtils.GetInklecateFilePath();
			if(inklecatePath == null) {
				UnityEngine.Debug.LogWarning("Inklecate (the ink compiler) not found in assets. This will prevent automatic building of JSON TextAsset files from ink story files.");
				return;
			}
			if(Application.platform == RuntimePlatform.OSXEditor) {
				SetInklecateFilePermissions(inklecatePath);
			}
			if(inklecatePath.Contains("'")){
				Debug.LogError("Due to a Unity bug, Inklecate path cannot contain an apostrophe. Ink will not compile until this is resolved. Path is '"+inklecatePath+"'");
				return;
			}
			// This hasn't been affecting us lately. Left it in so we can easily restore it in case of future bugs.
			/* else if(inklecatePath.Contains(" ")){
				Debug.LogWarning("Inklecate path should not contain a space. This might lead to compilation failing. Path is '"+inklecatePath+"'. If you don't see any compilation errors, you can ignore this warning.");
			}*/
			string inputPath = InkEditorUtils.CombinePaths(inkFile.absoluteFolderPath, Path.GetFileName(inkFile.filePath));
			string outputPath = InkEditorUtils.CombinePaths(inkFile.absoluteFolderPath, Path.GetFileNameWithoutExtension(Path.GetFileName(inkFile.filePath))) + ".json";
			string inkArguments = InkSettings.Instance.customInklecateOptions.additionalCompilerOptions + " -c -o " + "\"" + outputPath + "\" \"" + inputPath + "\"";

			CompilationStackItem pendingFile = new CompilationStackItem();
			pendingFile.inkFile = InkLibrary.GetInkFileWithAbsolutePath(inputPath);
			pendingFile.inkAbsoluteFilePath = inputPath;
			pendingFile.jsonAbsoluteFilePath = outputPath;
			pendingFile.state = CompilationStackItem.State.Compiling;
			InkLibrary.Instance.compilationStack.Add(pendingFile);
			InkLibrary.Save();

			Process process = new Process();
			if( InkSettings.Instance.customInklecateOptions.runInklecateWithMono && Application.platform != RuntimePlatform.WindowsEditor ) {
				if(File.Exists(_libraryMono)) {
					process.StartInfo.FileName = _libraryMono;
				} else if(File.Exists(_usrMono)) {
					process.StartInfo.FileName = _usrMono;
				} else {
					Debug.LogError("Mono was not found on machine");
					return;
				}
				process.StartInfo.Arguments = inklecatePath + " " + inkArguments;
			} else {
				process.StartInfo.FileName = inklecatePath;
				process.StartInfo.Arguments = inkArguments;
			}

			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.CreateNoWindow = true;
			process.EnableRaisingEvents = true;
			process.OutputDataReceived += OnProcessOutput;
			// For some reason having this line enabled spams the output and error streams with null and "???" (only on OSX?)
			// Rather than removing unhandled error detection I thought it'd be best to just catch those messages and ignore them instead.
			process.ErrorDataReceived += OnProcessError;
			process.Exited += OnCompileProcessComplete;
			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
			pendingFile.process = process;
			// If you'd like to run this command outside of unity, you could instead run process.StartInfo.Arguments in the command line.
		}

		static void OnProcessOutput (object sender, DataReceivedEventArgs e) {
			Process process = (Process)sender;
			ProcessOutput(process, e.Data);
		}

		static void OnProcessError (object sender, DataReceivedEventArgs e) {
			Process process = (Process)sender;
			ProcessError(process, e.Data);
		}

		static void OnCompileProcessComplete(object sender, System.EventArgs e) {
			Process process = (Process)sender;
			CompilationStackItem pendingFile = InkLibrary.GetCompilationStackItem(process);
			pendingFile.state = CompilationStackItem.State.Importing;
			if(InkLibrary.FilesInCompilingStackInState(CompilationStackItem.State.Compiling).Count == 0) {
				// This event runs in another thread, preventing us from calling some UnityEditor functions directly. Instead, we delay till the next inspector update.
				EditorApplication.delayCall += Delay;
			}
		}

		private static void ProcessOutput (Process process, string message) {
			if (message == null || message.Length == 0 || message == "???")
				return;
			CompilationStackItem compilingFile = InkLibrary.GetCompilationStackItem(process);
			compilingFile.output.Add(message);
		}

		private static void ProcessError (Process process, string message) {
			if (message == null || message.Length == 0 || message == "???")
				return;
			CompilationStackItem compilingFile = InkLibrary.GetCompilationStackItem(process);
			compilingFile.errorOutput.Add(message);
		}

		private static void Delay () {
			if(InkLibrary.FilesInCompilingStackInState(CompilationStackItem.State.Compiling).Count > 0) {
				Debug.LogWarning("Delayed, but a file is now compiling! You can ignore this warning.");
				return;
			}
			float longestTimeTaken = 0;
			bool errorsFound = false;
			StringBuilder filesCompiledLog = new StringBuilder("Files compiled:");
			foreach (var compilingFile in InkLibrary.Instance.compilationStack) {
				longestTimeTaken = Mathf.Max (compilingFile.timeTaken);
				filesCompiledLog.AppendLine().Append(compilingFile.inkFile.filePath);
				if(compilingFile.errorOutput.Count > 0) {
					filesCompiledLog.Append(" (With unhandled error)");
					StringBuilder errorLog = new StringBuilder ();
					errorLog.Append ("Unhandled error(s) occurred compiling Ink file ");
					errorLog.Append ("'");
					errorLog.Append (compilingFile.inkFile.filePath);
					errorLog.Append ("'");
					errorLog.AppendLine ("! Please report following error(s) as a bug:");
					foreach (var error in compilingFile.errorOutput)
						errorLog.AppendLine (error);
					Debug.LogError(errorLog);
					compilingFile.inkFile.metaInfo.compileErrors = compilingFile.errorOutput;
					errorsFound = true;
				} else {
					SetOutputLog(compilingFile);
					bool errorsInEntireStory = false;
					bool warningsInEntireStory = false;
					foreach(var inkFile in compilingFile.inkFile.metaInfo.inkFilesInIncludeHierarchy) {
						if(inkFile.metaInfo.hasErrors) {
							errorsInEntireStory = true;
						}
						if(inkFile.metaInfo.hasWarnings) {
							warningsInEntireStory = true;
						}
					}
					if(errorsInEntireStory) {
						filesCompiledLog.Append(" (With error)");
						errorsFound = true;
					} else {
						string localJSONAssetPath = InkEditorUtils.AbsoluteToUnityRelativePath(compilingFile.jsonAbsoluteFilePath);
						AssetDatabase.ImportAsset (localJSONAssetPath);
						compilingFile.inkFile.jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset> (localJSONAssetPath);
					}
					if(warningsInEntireStory) {
						filesCompiledLog.Append(" (With warning)");
					}
				}
			}

			if (longestTimeTaken > InkSettings.Instance.compileTimeout * 0.6f)
				Debug.LogWarning ("Compilation took over 60% of the time required to timeout the compiler. Consider increasing the compile timeout on the InkSettings file.");

			foreach (var compilingFile in InkLibrary.Instance.compilationStack) {
				if (OnCompileInk != null) {
					OnCompileInk (compilingFile.inkFile);
				}
			}

			StringBuilder outputLog = new StringBuilder ();
			if(errorsFound) {
				outputLog.Append ("Ink compilation completed with errors at ");
				outputLog.AppendLine (DateTime.Now.ToLongTimeString ());
				outputLog.Append (filesCompiledLog.ToString());
				Debug.LogWarning(outputLog);
			} else {
				outputLog.Append ("Ink compilation completed at ");
				outputLog.AppendLine (DateTime.Now.ToLongTimeString ());
				outputLog.Append (filesCompiledLog.ToString());
				Debug.Log(outputLog);
			}
			InkLibrary.Instance.compilationStack.Clear();

			InkLibrary.Save();
			InkMetaLibrary.Save();

			EditorUtility.ClearProgressBar();
			if(EditorApplication.isPlayingOrWillChangePlaymode) {
				Debug.LogWarning("Ink just finished recompiling while in play mode. Your runtime story may not be up to date.");
			}
		}

		private static void SetOutputLog (CompilationStackItem pendingFile) {
			pendingFile.inkFile.metaInfo.errors.Clear();
			pendingFile.inkFile.metaInfo.warnings.Clear();
			pendingFile.inkFile.metaInfo.todos.Clear();

			foreach(var childInkFile in pendingFile.inkFile.metaInfo.inkFilesInIncludeHierarchy) {
				childInkFile.metaInfo.compileErrors.Clear();
				childInkFile.metaInfo.errors.Clear();
				childInkFile.metaInfo.warnings.Clear();
				childInkFile.metaInfo.todos.Clear();
			}

			foreach(string output in pendingFile.output) {
				var match = _errorRegex.Match(output);
				if (match.Success) {
					string errorType = null;
					string filename = null;
					int lineNo = -1;
					string message = null;
					
					var errorTypeCapture = match.Groups["errorType"];
					if( errorTypeCapture != null ) {
						errorType = errorTypeCapture.Value;
					}
					
					var filenameCapture = match.Groups["filename"];
					if (filenameCapture != null)
						filename = filenameCapture.Value;
					
					var lineNoCapture = match.Groups["lineNo"];
					if (lineNoCapture != null)
						lineNo = int.Parse (lineNoCapture.Value);
					
					var messageCapture = match.Groups["message"];
					if (messageCapture != null)
						message = messageCapture.Value.Trim();
					
					
					string logFilePath = InkEditorUtils.CombinePaths(Path.GetDirectoryName(pendingFile.inkFile.filePath), filename);
					InkFile inkFile = InkLibrary.GetInkFileWithPath(logFilePath);
					if(inkFile == null)
						inkFile = pendingFile.inkFile;
					
					string pathAndLineNumberString = "\n"+inkFile.filePath+":"+lineNo;
					if(errorType == "ERROR") {
						inkFile.metaInfo.errors.Add(new InkMetaFile.InkFileLog(message, lineNo));
						Debug.LogError("INK "+errorType+": "+message + pathAndLineNumberString);
					} else if (errorType == "WARNING") {
						inkFile.metaInfo.warnings.Add(new InkMetaFile.InkFileLog(message, lineNo));
						Debug.LogWarning("INK "+errorType+": "+message + pathAndLineNumberString);
					} else if (errorType == "TODO") {
						inkFile.metaInfo.todos.Add(new InkMetaFile.InkFileLog(message, lineNo));
						Debug.Log("INK "+errorType+": "+message + pathAndLineNumberString);
					}
				}
			}
		}

		private const string _usrMono = "/usr/local/bin/mono";
		private const string _libraryMono = "/Library/Frameworks/Mono.framework/Versions/Current/Commands/mono";

		private static Regex _errorRegex = new Regex(@"(?<errorType>ERROR|WARNING|TODO|RUNTIME ERROR):(?:\s(?:'(?<filename>[^']*)'\s)?line (?<lineNo>\d+):)?(?<message>.*)");

		// The asset store version of this plugin removes execute permissions. We can't run unless they're restored.
		private static void SetInklecateFilePermissions (string inklecatePath) {
			Process process = new Process();
			process.StartInfo.WorkingDirectory = Path.GetDirectoryName(inklecatePath);
			process.StartInfo.FileName = "chmod";
			process.StartInfo.Arguments = "+x "+ Path.GetFileName(inklecatePath);
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute = false;
			process.EnableRaisingEvents = true;
			process.Start();
			process.WaitForExit();
		}
	}
}