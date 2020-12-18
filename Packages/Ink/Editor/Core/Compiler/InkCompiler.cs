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
		public static bool hasLockedUnityCompilation = false;
        
        private static List<Action> onCompleteActions = new List<Action>();

		[Serializable]
		public class CompilationStackItem {
			public enum State {
				// Default state, item is about to be queued for compilation
				Queued,
				
				// Item is now owned by the thread pool and being compiled
				Compiling,
				
				// Compilation has finished, item to be processed for errors and result handled
				Complete,
			}

			public State state = State.Queued;
			public bool immediate;
			public InkFile inkFile;
			public string compiledJson;
			public string inkAbsoluteFilePath;
			public string jsonAbsoluteFilePath;
			public List<InkCompilerLog> logOutput = new List<InkCompilerLog>();
			public List<string> unhandledErrorOutput = new List<string>();
			public DateTime startTime;
			public DateTime endTime;

			public float timeTaken {
				get {
					if(state == State.Complete) return (float)(endTime - startTime).TotalSeconds;
					else return (float)(DateTime.Now - startTime).TotalSeconds;
				}
			}

			public CompilationStackItem () {}
		}

		// This always runs after the InkEditorUtils constructor
		[InitializeOnLoadMethod]
		static void OnProjectLoadedInEditor() {
			#if UNITY_2017_1_OR_NEWER
			EditorApplication.playModeStateChanged += OnPlayModeChange;
			#else
			EditorApplication.playmodeStateChanged += LegacyOnPlayModeChange;
			#endif
			EditorApplication.update += Update;
            // I really don't know if this can fire, since it assumes that it compiled so can't have been locked. But safety first!
            EditorApplication.UnlockReloadAssemblies();
			#if UNITY_2019_4_OR_NEWER
			// This one, on the other hand, seems to actually occur sometimes - presumably because c# compiles at the same time as the ink.
			if(InkEditorUtils.disallowedAutoRefresh) {
				InkEditorUtils.disallowedAutoRefresh = false;
				try {
					AssetDatabase.AllowAutoRefresh();
				} catch (Exception e) {
					Debug.LogWarning("Failed AllowAutoRefresh "+e);
				}
			}
			#endif
		}
		
		private static void Update () {
			// If we're not compiling but have locked C# compilation then now is the time to reset
			if ((!InkLibrary.created || !compiling) && hasLockedUnityCompilation) {
				hasLockedUnityCompilation = false;
				EditorApplication.UnlockReloadAssemblies();
			}

			if(!InkLibrary.created) 
				return;

			if(compiling) {
				// Check for timeouts, in case of an unhandled bug with this system/the ink compiler!
				for (int i = InkLibrary.Instance.compilationStack.Count - 1; i >= 0; i--) {
					var compilingFile = InkLibrary.Instance.compilationStack [i];
					if (compilingFile.state == CompilationStackItem.State.Compiling) {
						if (compilingFile.timeTaken > InkSettings.Instance.compileTimeout) {
							// TODO - Cancel the thread if it's still going. Not critical, since its kinda fine if it compiles a bit later, but it's not clear.
							RemoveCompilingFile(i);
							Debug.LogError("Ink Compiler timed out for "+compilingFile.inkAbsoluteFilePath+".\nCompilation should never take more than a few seconds, but for large projects or slow computers you may want to increase the timeout time in the InkSettings file.\nIf this persists there may be another issue; or else check an ink file exists at this path and try Assets/Recompile Ink, else please report as a bug with the following error log at this address: https://github.com/inkle/ink/issues\nError log:\n"+string.Join("\n",compilingFile.unhandledErrorOutput.ToArray()));
							TryCompileNextFileInStack();
						}
					}
				}

				// When all files have compiled, run the complete function.
				if(InkLibrary.NumFilesInCompilingStackInState(CompilationStackItem.State.Compiling) == 0) {
					if(InkLibrary.NumFilesInCompilingStackInState(CompilationStackItem.State.Queued) == 0) {
						DelayedComplete();
					} else {
						// We used to avoid calling this here in favour of calling it CompileInkThreaded but it seems that it doesn't run when called there, for some reason.
						// If someone can make this work please let me know!
						TryCompileNextFileInStack();
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
            InkLibrary.SaveToFile();
            // Progress bar prevents delayCall callback from firing in Linux Editor, locking the
            // compilation until it times out. Let's just not show progress bars in Linux Editor    
            #if !UNITY_EDITOR_LINUX
            if (InkLibrary.Instance.compilationStack.Count == 0) EditorUtility.ClearProgressBar();
            #endif
        }

		static void UpdateProgressBar () {
			if(InkLibrary.Instance.compilationStack.Count == 0) return;
			int numCompiling = InkLibrary.NumFilesInCompilingStackInState(CompilationStackItem.State.Compiling);
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
			#if UNITY_2019_4_OR_NEWER
			if(!InkEditorUtils.disallowedAutoRefresh) {
				InkEditorUtils.disallowedAutoRefresh = true;
				try {
					AssetDatabase.DisallowAutoRefresh();
				} catch (Exception e) {
					Debug.LogWarning("Failed DisallowAutoRefresh "+e);
				}
			}
			#endif
            
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
			if(inkFile == null) {
				Debug.LogError("Tried to compile ink file but input was null.");
				return;
			}
			if(!inkFile.isMaster)
				Debug.LogWarning("Compiling InkFile which is an include. Any file created is likely to be invalid. Did you mean to call CompileInk on inkFile.master?");

			// If we've not yet locked C# compilation do so now
			if (!hasLockedUnityCompilation)
			{
				hasLockedUnityCompilation = true;
				EditorApplication.LockReloadAssemblies();
			}
			
            InkLibrary.RemoveFromPendingCompilationStack(inkFile);
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
				jsonAbsoluteFilePath = inkFile.absoluteJSONPath,
				state = CompilationStackItem.State.Queued,
				immediate = immediate
			};

			InkLibrary.AddToCompilationStack(pendingFile);

			TryCompileNextFileInStack();
		}




		private static void TryCompileNextFileInStack () {
			if(!compiling) return;
			InkCompiler.CompilationStackItem fileToCompile = null;
			foreach(var x in InkLibrary.Instance.compilationStack) {
				if(x.state == CompilationStackItem.State.Compiling) return;
				if(x.state == CompilationStackItem.State.Queued) {
					fileToCompile = x;
					break;
				}
			}
			if(fileToCompile != null) {
				if(fileToCompile.immediate) {
					CompileInkThreaded(fileToCompile);
				} else {
					if(EditorApplication.isCompiling) Debug.LogWarning("Was compiling scripts when ink compilation started! This seems to cause the thread to cancel and complete, but the work isn't done. It may cause a timeout.");
					ThreadPool.QueueUserWorkItem(CompileInkThreaded, fileToCompile);
				}
			} else {
			}
		}

		private static void BeginCompilingFile(CompilationStackItem item) {
			if(item.state != CompilationStackItem.State.Queued) return;
			item.state = CompilationStackItem.State.Compiling;
			item.startTime = DateTime.Now;
		}
		private static void CompleteCompilingFile(CompilationStackItem item) {
			if(item.state != CompilationStackItem.State.Compiling) return;
			item.state = CompilationStackItem.State.Complete;
			item.endTime = DateTime.Now;
			if (item.timeTaken > InkSettings.Instance.compileTimeout * 0.6f)
				Debug.LogWarning ("Compilation for "+Path.GetFileName(item.inkFile.filePath)+" took over 60% of the time required to timeout the compiler. Consider increasing the compile timeout on the InkSettings file.");
		}

		private static void CompileInkThreaded(object itemObj) {
			CompilationStackItem item = (CompilationStackItem) itemObj;
			if(item.state == CompilationStackItem.State.Compiling) {
				Debug.LogWarning("CompileInkThreaded was called on a file that is already compiling! This is most likely a threading bug. Please report this!");
				return;
			}
			BeginCompilingFile(item);

			var inputString = File.ReadAllText(item.inkAbsoluteFilePath);
			var compiler = new Compiler(inputString, new Compiler.Options
			{
				countAllVisits = true,
				fileHandler = new UnityInkFileHandler(Path.GetDirectoryName(item.inkAbsoluteFilePath)),
				errorHandler = (string message, ErrorType type) => {
					InkCompilerLog log;
					if(InkCompilerLog.TryParse(message, out log)) {
						if(string.IsNullOrEmpty(log.fileName)) log.fileName = Path.GetFileName(item.inkAbsoluteFilePath);
						item.logOutput.Add(log);
					} else {
						Debug.LogWarning("Couldn't parse log "+message);
					}
				}
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

			CompleteCompilingFile(item);

			// This doesn't seem to execute when called in a thread, and I apparently don't have a bloody clue how threads work.
			// If someone can make this work, I'd rather that TryCompileNextFileInStack ran directly after CompileInkThreaded finishes.
			// I couldn't make it work, so I've put TryCompileNextFileInStack in Update instead. Bleh!
			// TryCompileNextFileInStack();
		}

		// When all files in stack have been compiled. This is called via update because Process events run in another thread.
		private static void DelayedComplete () {
			if(InkLibrary.NumFilesInCompilingStackInState(CompilationStackItem.State.Compiling) > 0) {
				Debug.LogWarning("Delayed, but a file is now compiling! You can ignore this warning.");
				return;
			}
			bool errorsFound = false;
			StringBuilder filesCompiledLog = new StringBuilder("Files compiled:");

			// Create and import compiled files
			AssetDatabase.StartAssetEditing();
			foreach (var compilingFile in InkLibrary.Instance.compilationStack) {
				// Complete status is also set when an error occured, in these cases 'compiledJson' will be null so there's no import to process
				if (compilingFile.compiledJson == null) continue;
				
				// Write new compiled data to the file system
				File.WriteAllText(compilingFile.jsonAbsoluteFilePath, compilingFile.compiledJson, Encoding.UTF8);
				AssetDatabase.ImportAsset(compilingFile.inkFile.jsonPath);
			}
			AssetDatabase.StopAssetEditing();

			foreach (var compilingFile in InkLibrary.Instance.compilationStack) {
				// Load and store a reference to the compiled file
				compilingFile.inkFile.FindCompiledJSONAsset();
				
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
					compilingFile.inkFile.unhandledCompileErrors = compilingFile.unhandledErrorOutput;
					errorsFound = true;
				} else {
					SetOutputLog(compilingFile);
					bool errorsInEntireStory = false;
					bool warningsInEntireStory = false;
					foreach(var inkFile in compilingFile.inkFile.inkFilesInIncludeHierarchy) {
						if(inkFile.hasErrors) {
							errorsInEntireStory = true;
						}
						if(inkFile.hasWarnings) {
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

			InkLibrary.ClearCompilationStack();
			
			#if !UNITY_EDITOR_LINUX
			EditorUtility.ClearProgressBar();
			#endif
			
			#if UNITY_2019_4_OR_NEWER
			if(InkEditorUtils.disallowedAutoRefresh) {
				InkEditorUtils.disallowedAutoRefresh = false;
				try {
					AssetDatabase.AllowAutoRefresh();
				} catch (Exception e) {
					Debug.LogWarning("Failed AllowAutoRefresh "+e);
				}
			}
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
			pendingFile.inkFile.errors.Clear();
			pendingFile.inkFile.warnings.Clear();
			pendingFile.inkFile.todos.Clear();

			foreach(var childInkFile in pendingFile.inkFile.inkFilesInIncludeHierarchy) {
				childInkFile.unhandledCompileErrors.Clear();
				childInkFile.errors.Clear();
				childInkFile.warnings.Clear();
				childInkFile.todos.Clear();
			}

			foreach(var output in pendingFile.logOutput) {
				if(output.type == ErrorType.Error) {
					pendingFile.inkFile.errors.Add(output);
					Debug.LogError("Ink "+output.type+": "+output.content + " (at "+output.fileName+":"+output.lineNumber+")", pendingFile.inkFile.inkAsset);
				} else if (output.type == ErrorType.Warning) {
					pendingFile.inkFile.warnings.Add(output);
					Debug.LogWarning("Ink "+output.type+": "+output.content + " (at "+output.fileName+" "+output.lineNumber+")", pendingFile.inkFile.inkAsset);
				} else if (output.type == ErrorType.Author) {
					pendingFile.inkFile.todos.Add(output);
					if(InkSettings.Instance.printInkLogsInConsoleOnCompile)
						Debug.Log("Ink Log: "+output.content + " (at "+output.fileName+" "+output.lineNumber+")", pendingFile.inkFile.inkAsset);
				}
			}
		}



		public static List<InkFile> GetUniqueMasterInkFilesToCompile (List<string> importedInkAssets) {
			List<InkFile> masterInkFiles = new List<InkFile>();
			foreach (var importedAssetPath in importedInkAssets) {
                foreach(var masterInkFile in GetMasterFilesIncludingInkAssetPath(importedAssetPath)) {
					if (!masterInkFiles.Contains(masterInkFile) && (InkSettings.Instance.compileAutomatically || masterInkFile.compileAutomatically)) {
						masterInkFiles.Add(masterInkFile);
					}
				}
            }
			return masterInkFiles;
		}

		// An ink file might actually have several owners! This should be reflected here.
        public static IEnumerable<InkFile> GetMasterFilesIncludingInkAssetPath (string importedAssetPath) {
            InkFile inkFile = InkLibrary.GetInkFileWithPath(importedAssetPath);
            // Trying to catch a rare (and not especially important) bug that seems to happen occasionally when opening a project
            // It's probably this - I've noticed it before in another context.
            Debug.Assert(InkSettings.Instance != null, "No ink settings file. This is a bug. For now you should be able to fix this via Assets > Rebuild Ink Library");
            // I've caught it here before
            Debug.Assert(inkFile != null, "No internal InkFile reference at path "+importedAssetPath+". This is a bug. For now you can fix this via Assets > Rebuild Ink Library");
            Debug.Assert(inkFile != null);
            return inkFile.masterInkFilesIncludingSelf;
        }
	}
}
