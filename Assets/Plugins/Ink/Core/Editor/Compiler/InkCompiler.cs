using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using Debug = UnityEngine.Debug;

namespace Ink.UnityIntegration {
	[InitializeOnLoad]
	public static class InkCompiler {
		
		public static bool compiling {
			get {
				return InkLibrary.Instance.compilationStack.Count > 0;
			}
		}
		public static bool buildBlocked = false;
		static bool playModeBlocked = false;

		public delegate void OnCompileInkEvent (InkFile inkFile);
		public static event OnCompileInkEvent OnCompileInk;

		// Track if we've currently locked compilation of Unity C# Scripts
		private static bool hasLockedUnityCompilation = false;
        
        private static List<Action> onCompleteActions = new List<Action>();

		[Serializable]
		public class CompilationStackItem {
			public enum State {
				// Default state, item is about to be queued for compilation
				Idle,
				
				// Item is no owned by the thread pool and being compiled
				Compiling,
				
				// Compilation has finished, item to be processed for errors and result handled
				Complete,
			}

			public State state = State.Idle;
			public InkFile inkFile;
			public string compiledJson;
			public string inkAbsoluteFilePath;
			public string jsonAbsoluteFilePath;
			public List<string> output = new List<string>();
			public List<string> unhandledErrorOutput = new List<string>();
			public DateTime startTime;

			public float timeTaken {
				get {
					return (float)(DateTime.Now - startTime).TotalSeconds;
				}
			}

			public CompilationStackItem () {
				startTime = DateTime.Now;
			}
		}

		// Utility class for the ink compiler, used to work out how to find include files and their contents
		private class UnityInkFileHandler : IFileHandler
		{
			private readonly string rootDirectory;

			public UnityInkFileHandler(string rootDirectory)
			{
				this.rootDirectory = rootDirectory;
			}
			
			public string ResolveInkFilename(string includeName)
			{
				return Path.Combine(rootDirectory, includeName);
			}

			public string LoadInkFileContents(string fullFilename)
			{
				return File.ReadAllText(fullFilename);
			}
		}

		static InkCompiler () {
			#if UNITY_2017_1_OR_NEWER
			EditorApplication.playModeStateChanged += OnPlayModeChange;
			#else
			EditorApplication.playmodeStateChanged += LegacyOnPlayModeChange;
			#endif
			EditorApplication.update += Update;
		}

		private static void Update () {
			if(!InkLibrary.created) 
				return;

			// If we're not compiling but have locked C# compilation then now is the time to reset
			if (!compiling && hasLockedUnityCompilation)
			{
				hasLockedUnityCompilation = false;
				EditorApplication.UnlockReloadAssemblies();
			}

			// When all files have compiled, run the complete function.
			if(compiling && InkLibrary.FilesInCompilingStackInState(CompilationStackItem.State.Compiling).Count == 0) {
				DelayedComplete();
			}
			
            
            for (int i = InkLibrary.Instance.compilationStack.Count - 1; i >= 0; i--) {
                var compilingFile = InkLibrary.Instance.compilationStack [i];
                if (compilingFile.state == CompilationStackItem.State.Compiling) {
                    if (compilingFile.timeTaken > InkSettings.Instance.compileTimeout) {
                        // TODO - Cancel the thread if it's still going. Not critical, since its kinda fine if it compiles a bit later, but it's not clear.
                        RemoveCompilingFile(i);
                        Debug.LogError("Ink Compiler timed out for "+compilingFile.inkAbsoluteFilePath+".\nCompilation should never take more than a few seconds, but for large projects or slow computers you may want to increase the timeout time in the InkSettings file.\nIf this persists there may be another issue; or else check an ink file exists at this path and try Assets/Recompile Ink, else please report as a bug with the following error log at this address: https://github.com/inkle/ink/issues\nError log:\n"+string.Join("\n",compilingFile.unhandledErrorOutput.ToArray()));
                    }
                }
            }


			// If we're not showing a progress bar in Linux this whole step is superfluous
			#if !UNITY_EDITOR_LINUX
			UpdateProgressBar();
			#endif
		}

        static void RemoveCompilingFile (int index) {
            InkLibrary.Instance.compilationStack.RemoveAt(index);
            InkLibrary.Save();
            // Progress bar prevents delayCall callback from firing in Linux Editor, locking the
            // compilation until it times out. Let's just not show progress bars in Linux Editor    
            #if !UNITY_EDITOR_LINUX
            if (InkLibrary.Instance.compilationStack.Count == 0) EditorUtility.ClearProgressBar();
            #endif
        }

		static void UpdateProgressBar () {
			if(InkLibrary.Instance.compilationStack.Count == 0) return;
			int numCompiling = InkLibrary.FilesInCompilingStackInState(CompilationStackItem.State.Compiling).Count;
			string message = "Compiling .Ink File "+(InkLibrary.Instance.compilationStack.Count-numCompiling)+" of "+InkLibrary.Instance.compilationStack.Count+".";
			if(playModeBlocked) message += " Will enter play mode when complete.";
			if(buildBlocked || playModeBlocked || EditorApplication.isPlaying) EditorUtility.DisplayProgressBar("Compiling Ink...", message, GetEstimatedCompilationProgress());
			else EditorUtility.ClearProgressBar();
		}

		public static float GetEstimatedCompilationProgress () {
			if(!compiling) return 1;
			float progress = 0;
			foreach (var compilingFile in InkLibrary.Instance.compilationStack) {
				if (compilingFile.state == CompilationStackItem.State.Compiling)
					progress += compilingFile.timeTaken / InkSettings.Instance.compileTimeout;
				if (compilingFile.state == CompilationStackItem.State.Complete)
					progress += 1;
			}
			progress /= InkLibrary.Instance.compilationStack.Count;
			return progress;
		}

		#if UNITY_2017_1_OR_NEWER
		static void OnPlayModeChange (PlayModeStateChange mode) {
			if(mode == PlayModeStateChange.EnteredEditMode && InkLibrary.Instance.pendingCompilationStack.Count > 0)
				CompilePendingFiles();
			if(mode == PlayModeStateChange.ExitingEditMode && compiling)
				BlockPlayMode();
			if(mode == PlayModeStateChange.EnteredPlayMode && compiling)
				EnteredPlayModeWhenCompiling();
		}
		
		#else
		
		static void LegacyOnPlayModeChange () {
			if(!EditorApplication.isPlayingOrWillChangePlaymode && EditorApplication.isPlaying && InkLibrary.Instance.pendingCompilationStack.Count > 0) 
				CompilePendingFiles();
			if(EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying && compiling)
				BlockPlayMode();
			if(EditorApplication.isPlayingOrWillChangePlaymode && EditorApplication.isPlaying && compiling)
				EnteredPlayModeWhenCompiling();
		}
		#endif

		static void CompilePendingFiles () {
			InkLibrary.CreateOrReadUpdatedInkFiles (InkLibrary.Instance.pendingCompilationStack);
			foreach (var pendingMasterFile in GetUniqueMasterInkFilesToCompile(InkLibrary.Instance.pendingCompilationStack))
				InkCompiler.CompileInk(pendingMasterFile);
		}

		static void BlockPlayMode () {
			EditorApplication.isPlaying = false;
			var percentage = String.Format("{0:P0}.", GetEstimatedCompilationProgress());
			Debug.LogWarning("Delayed entering play mode because Ink is still compiling ("+percentage+"). Will enter play mode on completion.");
			playModeBlocked = true;
		}

		static void EnteredPlayModeWhenCompiling () {
			Debug.LogError("Entered Play Mode while Ink was still compiling! Your story will not be up to date. Recommend exiting and re-entering play mode.\nWe normally delay entering play mode when compiling, so you've found an edge case!");
		}

		public static void CompileInk (params InkFile[] inkFiles) {
            CompileInk(inkFiles, false, null);
        }
		public static void CompileInk (InkFile[] inkFiles, bool immediate, Action onComplete) {
            InkLibrary.Validate();
            if(onComplete != null) onCompleteActions.Add(onComplete);
			StringBuilder filesCompiledLog = new StringBuilder("Files compiled:");
			foreach (var inkFile in inkFiles) filesCompiledLog.AppendLine().Append(inkFile.filePath);
			
			StringBuilder outputLog = new StringBuilder ();
			outputLog.Append ("Ink compilation started at ");
			outputLog.AppendLine (DateTime.Now.ToLongTimeString ());
			outputLog.Append (filesCompiledLog.ToString());
			Debug.Log(outputLog);
			
			foreach(var inkFile in inkFiles) {
				CompileInkInternal (inkFile, immediate);
			}
		}

		/// <summary>
		/// Starts a System.Process that compiles a master ink file, creating a playable JSON file that can be parsed by the Ink.Story class
		/// </summary>
		/// <param name="inkFile">Ink file.</param>
		private static void CompileInkInternal (InkFile inkFile, bool immediate) {

			// If we've not yet locked C# compilation do so now
			if (!hasLockedUnityCompilation)
			{
				hasLockedUnityCompilation = true;
				EditorApplication.LockReloadAssemblies();
			}

            RemoveFromPendingCompilationStack(inkFile);

			if(inkFile == null) {
				Debug.LogError("Tried to compile ink file but input was null.");
				return;
			}
			if(!inkFile.metaInfo.isMaster)
				Debug.LogWarning("Compiling InkFile which is an include. Any file created is likely to be invalid. Did you mean to call CompileInk on inkFile.master?");
			if(InkLibrary.GetCompilationStackItem(inkFile) != null) {
				UnityEngine.Debug.LogWarning("Tried compiling ink file, but file is already compiling. "+inkFile.filePath);
				return;
			}

			string inputPath = InkEditorUtils.CombinePaths(inkFile.absoluteFolderPath, Path.GetFileName(inkFile.filePath));
			Debug.Assert(inkFile.absoluteFilePath == inputPath);

			CompilationStackItem pendingFile = new CompilationStackItem
			{
				inkFile = InkLibrary.GetInkFileWithAbsolutePath(inputPath),
				inkAbsoluteFilePath = inputPath,
				jsonAbsoluteFilePath = inkFile.jsonPath,
				state = CompilationStackItem.State.Compiling
			};

			InkLibrary.Instance.compilationStack.Add(pendingFile);
			InkLibrary.Save();
			if(immediate) {
                CompileInkThreaded(pendingFile);
                Update();
			} else {
                if(EditorApplication.isCompiling) Debug.LogWarning("Was compiling scripts when ink compilation started! This seems to cause the thread to cancel and complete, but the work isn't done. It may cause a timeout.");
                ThreadPool.QueueUserWorkItem(CompileInkThreaded, pendingFile);
            }
		}

		private static void CompileInkThreaded(object itemObj)
		{
			CompilationStackItem item = (CompilationStackItem) itemObj;

			var inputString = File.ReadAllText(item.inkAbsoluteFilePath);
			var compiler = new Compiler(inputString, new Compiler.Options
			{
				countAllVisits = true,
				fileHandler = new UnityInkFileHandler(Path.GetDirectoryName(item.inkAbsoluteFilePath))
			});

			try
			{
				var compiledStory = compiler.Compile();
				if (compiledStory != null)
					item.compiledJson = compiledStory.ToJson();
			}
			catch (SystemException e)
			{
				item.unhandledErrorOutput.Add(string.Format(
					"Ink Compiler threw exception \nError: {0}\n---- Trace ----\n{1}\n--------\n", e.Message,
					e.StackTrace));
			}

			item.output.AddRange(compiler.errors);
			item.output.AddRange(compiler.warnings);
			
			item.state = CompilationStackItem.State.Complete;
		}

		// When all files in stack have been compiled. This is called via update because Process events run in another thread.
		private static void DelayedComplete () {
			if(InkLibrary.FilesInCompilingStackInState(CompilationStackItem.State.Compiling).Count > 0) {
				Debug.LogWarning("Delayed, but a file is now compiling! You can ignore this warning.");
				return;
			}
			float longestTimeTaken = 0;
			bool errorsFound = false;
			StringBuilder filesCompiledLog = new StringBuilder("Files compiled:");
			foreach (var compilingFile in InkLibrary.Instance.compilationStack) {
				
				// Complete status is also set when an error occured, in these cases 'compiledJson' will be null so there's no import to process
				if (compilingFile.compiledJson != null)
				{
					// Write new compiled data to the file system
					File.WriteAllText(compilingFile.jsonAbsoluteFilePath, compilingFile.compiledJson, Encoding.UTF8);
                    AssetDatabase.ImportAsset(compilingFile.jsonAbsoluteFilePath);
                    var jsonObject = AssetDatabase.LoadAssetAtPath<TextAsset>(compilingFile.inkFile.jsonPath);

					// Update the jsonAsset reference
					compilingFile.inkFile.jsonAsset = jsonObject;
				}

				longestTimeTaken = Mathf.Max (compilingFile.timeTaken);
				filesCompiledLog.AppendLine().Append(compilingFile.inkFile.filePath);
				filesCompiledLog.Append(string.Format(" ({0}s)", compilingFile.timeTaken));
				if(compilingFile.unhandledErrorOutput.Count > 0) {
					filesCompiledLog.Append(" (With unhandled error)");
					StringBuilder errorLog = new StringBuilder ();
					errorLog.Append ("Unhandled error(s) occurred compiling Ink file ");
					errorLog.Append ("'");
					errorLog.Append (compilingFile.inkFile.filePath);
					errorLog.Append ("'");
					errorLog.AppendLine ("! Please report following error(s) as a bug:");
					foreach (var error in compilingFile.unhandledErrorOutput)
						errorLog.AppendLine (error);
					Debug.LogError(errorLog);
					compilingFile.inkFile.metaInfo.unhandledCompileErrors = compilingFile.unhandledErrorOutput;
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
				Debug.LogError(outputLog);
			} else {
				outputLog.Append ("Ink compilation completed at ");
				outputLog.AppendLine (DateTime.Now.ToLongTimeString ());
				outputLog.Append (filesCompiledLog.ToString());
				Debug.Log(outputLog);
			}

			InkLibrary.Instance.compilationStack.Clear();
			InkLibrary.Save();
			InkMetaLibrary.Save();

			#if !UNITY_EDITOR_LINUX
			EditorUtility.ClearProgressBar();
			#endif
			
            // This is now allowed, if compiled manually. I've left this code commented out because at some point we might want to track what caused a file to compile. 
            // if(EditorApplication.isPlayingOrWillChangePlaymode && InkSettings.Instance.delayInPlayMode) {
			// 	Debug.LogError("Ink just finished recompiling while in play mode. This should never happen when InkSettings.Instance.delayInPlayMode is true!");
			// }
            
            buildBlocked = false;

			if(playModeBlocked) {
                playModeBlocked = false;
				if(!errorsFound) {
					// Delaying gives the editor a frame to clear the progress bar.
					EditorApplication.delayCall += () => {
						Debug.Log("Compilation completed, entering play mode.");
						EditorApplication.isPlaying = true;
					};
				} else {
					Debug.LogWarning("Play mode not entered after ink compilation because ink had errors.");
				}
			}

            foreach(var onCompleteAction in onCompleteActions) {
                if(onCompleteAction != null) onCompleteAction();
            }
            onCompleteActions.Clear();
		}

		private static void SetOutputLog (CompilationStackItem pendingFile) {
			pendingFile.inkFile.metaInfo.errors.Clear();
			pendingFile.inkFile.metaInfo.warnings.Clear();
			pendingFile.inkFile.metaInfo.todos.Clear();

			foreach(var childInkFile in pendingFile.inkFile.metaInfo.inkFilesInIncludeHierarchy) {
				childInkFile.metaInfo.unhandledCompileErrors.Clear();
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
						Debug.LogError("INK "+errorType+": "+message + pathAndLineNumberString, inkFile.inkAsset);
					} else if (errorType == "WARNING") {
						inkFile.metaInfo.warnings.Add(new InkMetaFile.InkFileLog(message, lineNo));
						Debug.LogWarning("INK "+errorType+": "+message + pathAndLineNumberString, inkFile.inkAsset);
					} else if (errorType == "TODO") {
						inkFile.metaInfo.todos.Add(new InkMetaFile.InkFileLog(message, lineNo));
						Debug.Log("INK "+errorType+": "+message + pathAndLineNumberString, inkFile.inkAsset);
					}
				}
			}
		}

		private static Regex _errorRegex = new Regex(@"(?<errorType>ERROR|WARNING|TODO|RUNTIME ERROR):(?:\s(?:'(?<filename>[^']*)'\s)?line (?<lineNo>\d+):)?(?<message>.*)");



        

        static void RemoveFromPendingCompilationStack (InkFile inkFile) {
            InkLibrary.Instance.pendingCompilationStack.Remove(inkFile.filePath);
            foreach(var includeFile in inkFile.metaInfo.inkFilesInIncludeHierarchy) {
                InkLibrary.Instance.pendingCompilationStack.Remove(includeFile.filePath);
            }
        }




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

		public static List<InkFile> GetUniqueMasterInkFilesToCompile (List<string> importedInkAssets) {
			List<InkFile> masterInkFiles = new List<InkFile>();
			foreach (var importedAssetPath in importedInkAssets) {
                var masterInkFile = GetMasterFileFromInkAssetPath(importedAssetPath);
                if (!masterInkFiles.Contains(masterInkFile.metaInfo.masterInkFileIncludingSelf) && (InkSettings.Instance.compileAutomatically || masterInkFile.metaInfo.masterInkFileIncludingSelf.compileAutomatically)) {
                    masterInkFiles.Add(masterInkFile.metaInfo.masterInkFileIncludingSelf);
                }
            }
			return masterInkFiles;
		}

        public static InkFile GetMasterFileFromInkAssetPath (string importedAssetPath) {
            InkFile inkFile = InkLibrary.GetInkFileWithPath(importedAssetPath);
            // Trying to catch a rare (and not especially important) bug that seems to happen occasionally when opening a project
            // It's probably this - I've noticed it before in another context.
            Debug.Assert(InkSettings.Instance != null, "No ink settings file. This is a bug. For now you should be able to fix this via Assets > Rebuild Ink Library");
            // I've caught it here before
            Debug.Assert(inkFile != null, "No internal InkFile reference at path "+importedAssetPath+". This is a bug. For now you can fix this via Assets > Rebuild Ink Library");
            Debug.Assert(inkFile.metaInfo != null);
            Debug.Assert(inkFile.metaInfo.masterInkFileIncludingSelf != null);
            return inkFile.metaInfo.masterInkFileIncludingSelf;
        }



		//Replacement until Unity upgrades .Net
		public static bool IsNullOrWhiteSpace(string s){
			return (string.IsNullOrEmpty(s) || IsWhiteSpace(s));
		}

		//Returns true if string is only white space
		public static bool IsWhiteSpace(string s){
			foreach(char c in s){
				if(c != ' ' && c != '\t') return false;
			}
			return true;
		}
	}
}
