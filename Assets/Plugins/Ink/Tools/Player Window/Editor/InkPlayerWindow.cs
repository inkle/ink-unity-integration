using UnityEngine;
using UnityEditorInternal;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using Ink.Runtime;

namespace Ink.UnityIntegration {

	/// <summary>
	/// Ink player window. Tests stories in an editor window.
    /// Stories may be attached at runtime. InkPlayerWindow.DrawStoryPropertyField may be used for this.
	/// </summary>
	public class InkPlayerWindow : EditorWindow {

        /// <summary>
		/// Draws a property field for a story using GUILayout, allowing you to attach stories to the player window for debugging.
		/// </summary>
		/// <param name="story">Story.</param>
		/// <param name="label">Label.</param>
		public static void DrawStoryPropertyField (Story story, GUIContent label) {
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel(label);
			if(EditorApplication.isPlaying) {
				if(story != null) {
					if(InkPlayerWindow.isOpen) {
						InkPlayerWindow window = InkPlayerWindow.GetWindow(false);
						if(window.attached && window.story == story) {
							if(GUILayout.Button("Detach")) {
								InkPlayerWindow.Detach();
							}
						} else {
							if(GUILayout.Button("Attach")) {
								InkPlayerWindow.Attach(story);
							}
						}
					} else {
						if(GUILayout.Button("Open Player Window")) {
							InkPlayerWindow.GetWindow();
						}
					}
				} else {
					EditorGUI.BeginDisabledGroup(true);
					GUILayout.Button("Story cannot be null to attach to editor");
					EditorGUI.EndDisabledGroup();
				}
			} else {
				EditorGUI.BeginDisabledGroup(true);
				GUILayout.Button("Enter play mode to attach to editor");
				EditorGUI.EndDisabledGroup();
			}
			EditorGUILayout.EndHorizontal();
		}

        

		/// <summary>
		/// Draws a property field for a story using GUI, allowing you to attach stories to the player window for debugging.
		/// </summary>
		/// <param name="position">Position.</param>
		/// <param name="story">Story.</param>
		/// <param name="label">Label.</param>
		public static void DrawStoryPropertyField (Rect position, Story story, GUIContent label) {
			position = EditorGUI.PrefixLabel(position, label);
			InkPlayerWindow window = InkPlayerWindow.GetWindow(false);
			if(EditorApplication.isPlaying && story != null/* && story.state != null*/) {
				if(window.attached && window.story == story) {
					if(GUI.Button(position, "Detach")) {
						InkPlayerWindow.Detach();
					}
				} else {
					if(GUI.Button(position, "Attach")) {
						InkPlayerWindow.Attach(story);
					}
				}
			} else {
				EditorGUI.BeginDisabledGroup(true);
				GUI.Button(position, "Enter play mode to attach to editor");
				EditorGUI.EndDisabledGroup();
			}
		}

        // Allows you to change what the ink player does/is allowed to do when a story is loaded.
        // Especially handy for attached stories, where performance is critical or play controls might interfere with the game
        // TODO: Show these params somewhere slightly out of the way (debug mode for the window?) so you can fiddle with them mid-game if you <i>really</i> need to
        public struct InkPlayerParams {
            public bool disablePlayControls;
            public bool disableUndoHistory;
            public bool disableChoices;
            public bool disableStateLoading;
            public bool disableSettingVariables;

            public static InkPlayerParams Standard {
                get {
                    return new InkPlayerParams();
                }
            } 
            public static InkPlayerParams ForAttachedStories {
                get {
                    var inkPlayerParams = new InkPlayerParams();
                    inkPlayerParams.disablePlayControls = true;
                    inkPlayerParams.disableUndoHistory = true;
                    inkPlayerParams.disableChoices = true;
                    inkPlayerParams.disableStateLoading = true;
                    inkPlayerParams.disableSettingVariables = true;
                    return inkPlayerParams;
                }
            } 
        }

        // To make this actually practical to use it needs to remember the loaded story.
        // Right now it loses values before attaching.
        public class InkPlayerWindowState {
            static string settingsEditorPrefsKey = typeof(InkPlayerWindowState).Name +" Settings";
            public static Action OnCreateOrLoad;
            static InkPlayerWindowState _Instance;
            public static InkPlayerWindowState Instance {
                get {
                    if(_Instance == null) LoadOrCreateAndSave();
                    return _Instance;
                }
            }

            static InkPlayerWindowState LoadOrCreateAndSave () {
                Load();
                if(_Instance == null) CreateAndSave();
                return _Instance;
            }

            public static void CreateAndSave () {
                _Instance = new InkPlayerWindowState();
                Save(_Instance);
                if(OnCreateOrLoad != null) OnCreateOrLoad();
            }
            
            public static void Save () {
                Save(_Instance);
            }

            public static void Save (InkPlayerWindowState settings) {
                string data = JsonUtility.ToJson(settings);
                EditorPrefs.SetString(settingsEditorPrefsKey, data);
            }

            static void Load () {
                if(!EditorPrefs.HasKey(settingsEditorPrefsKey)) return;
                string data = EditorPrefs.GetString(settingsEditorPrefsKey);
                try {
                    _Instance = JsonUtility.FromJson<InkPlayerWindowState>(data);
                    if(OnCreateOrLoad != null) OnCreateOrLoad();
                } catch {
                    Debug.LogError("Save Data was corrupt and could not be parsed. New data created. Old data was:\n"+data);
                    CreateAndSave();
                }
            }


            public FunctionPanelState.FunctionParams functionParams = new FunctionPanelState.FunctionParams();
        }

		private const string windowTitle = "Ink Player";

		public static bool isOpen {get; private set;}

//		public StoryState storyState = new StoryState();
//		public class StoryState {
//		}
		public bool attached {get; private set;}
		bool attachedWhileInPlayMode {get; set;}

		TextAsset _storyJSONTextAsset;
		TextAsset storyJSONTextAsset {
			get {
				return _storyJSONTextAsset;
			} set {
				_storyJSONTextAsset = value;
				if (_storyJSONTextAsset != null) {
					string fullJSONFilePath = InkEditorUtils.UnityRelativeToAbsolutePath(AssetDatabase.GetAssetPath(storyJSONTextAsset));
					currentStoryJSONLastEditDateTime = File.GetLastWriteTime(fullJSONFilePath);
				}
			}
		}
		string storyJSON;
		DateTime currentStoryJSONLastEditDateTime;

		private Story _story;
		public Story story {
            get {
                return _story;
            } private set {
                if(_story != null) {
                    _story.onDidContinue -= OnDidContinue;
                    _story.onMakeChoice -= OnMakeChoice;
                    _story.onEvaluateFunction -= OnEvaluateFunction;
                    _story.onChoosePathString -= OnChoosePathString;
                }
                _story = value;
                if(_story != null) {
                    _story.onDidContinue += OnDidContinue;
                    _story.onMakeChoice += OnMakeChoice;
                    _story.onEvaluateFunction += OnEvaluateFunction;
                    _story.onChoosePathString += OnChoosePathString;
                }
            }
        }
        void OnDidContinue () {
			AddContent(story.currentText.Trim());
            AddWarningsAndErrorsToHistory();
        }
        void OnMakeChoice (Choice choice) {
            storyHistory.Add(new InkPlayerHistoryContentItem(choice.text.Trim(), InkPlayerHistoryContentItem.ContentType.StoryChoice));		
            AddWarningsAndErrorsToHistory();
        }
        void OnEvaluateFunction (string functionName, object[] arguments) {
            StringBuilder sb = new StringBuilder("OnEvaluateFunction: ");
            sb.Append(functionName);
            if(arguments != null) {
                sb.Append(" with args: ");
                for (int i = 0; i < arguments.Length; i++) {
                    if(arguments[i] == null) sb.Append("NULL");
                    else {
                        sb.Append(arguments[i]);
                        sb.Append(" (");
                        sb.Append(arguments[i].GetType().Name);
                        sb.Append(")");
                    }
                    if(i < arguments.Length-1) sb.Append(", ");
                }
            }
            storyHistory.Add(new InkPlayerHistoryContentItem(sb.ToString(), InkPlayerHistoryContentItem.ContentType.StoryEvaluateFunction));		
            AddWarningsAndErrorsToHistory();
        }
        void OnChoosePathString (string pathString, object[] arguments) {
            StringBuilder sb = new StringBuilder("ChoosePathString: ");
            sb.Append(pathString);
            if(arguments != null) {
                sb.Append(" with args: ");
                for (int i = 0; i < arguments.Length; i++) {
                    if(arguments[i] == null) sb.Append("NULL");
                    else {
                        sb.Append(arguments[i]);
                        sb.Append(" (");
                        sb.Append(arguments[i].GetType().Name);
                        sb.Append(")");
                    }
                    if(i < arguments.Length-1) sb.Append(", ");
                }
            }
            storyHistory.Add(new InkPlayerHistoryContentItem(sb.ToString(), InkPlayerHistoryContentItem.ContentType.StoryChoosePathString));
            AddWarningsAndErrorsToHistory();
        }

        void AddWarningsAndErrorsToHistory () {
            if(story.hasWarning) {
                foreach(var warning in story.currentWarnings) {
                    storyHistory.Add(new InkPlayerHistoryContentItem(warning.Trim(), InkPlayerHistoryContentItem.ContentType.StoryWarning));
                    if(displayOptions.displayWarningsInConsole) {
                        Debug.LogWarning("Ink Warning: "+warning.Trim());
                    }
                }
            }
            if(story.hasError) {
                foreach(var error in story.currentErrors) {
                    storyHistory.Add(new InkPlayerHistoryContentItem(error.Trim(), InkPlayerHistoryContentItem.ContentType.StoryError));
                    if(displayOptions.displayErrorsInConsole) {
                        Debug.LogError("Ink Error: "+error.Trim());
                    }
                }
            }
        }

		private TextAsset _storyStateTextAsset;
		public TextAsset storyStateTextAsset {
			get {
				return _storyStateTextAsset;
			} set {
				if(_storyStateTextAsset == value) 
					return;
				_storyStateTextAsset = value;
				if(_storyStateTextAsset != null)
					storyStateValid = InkEditorUtils.CheckStoryStateIsValid(storyJSONTextAsset.text, storyStateTextAsset.text);
			}
		}
		
		private UndoHistory<InkPlayerHistoryItem> storyStateHistory;
		private List<InkPlayerHistoryContentItem> storyHistory;
		
		private Exception playStoryException;
		private bool storyStateValid = false;

		public PlayerOptions playerOptions = new PlayerOptions();
		public class PlayerOptions {
			public bool continueAutomatically = true;
			public bool chooseAutomatically = false;
		}

//		private WindowState windowState = new WindowState();
//		public class WindowState {
		public Vector2 scrollPosition;
		public bool showingContentPanel = true;
		public Vector2 contentScrollPosition;
		public bool showingChoicesPanel = true;
		public bool showingSaveLoadPanel;
		public bool showingProfileData;

		public bool showingFunctionsPanel;

		public Vector2 variablesScrollPosition;
//		}

		public bool showingDivertsPanel;
		private DivertPanelState divertPanelState = new DivertPanelState();
		public class DivertPanelState {
			public Vector2 divertsScrollPosition;
			public string divertCommand = String.Empty;
		}

		private FunctionPanelState functionPanelState = new FunctionPanelState();
		ReorderableList functionInputList;
		public class FunctionPanelState {
		    public class FunctionParams {
                public class FunctionInput {
                    public enum FunctionInputType {
                        Int,
                        String,
                        Bool,
                        InkVariable
                    }
                    public FunctionInputType type;
                    public int intValue;
                    public string stringValue;
                    public bool boolValue;
                    public string inkVariablePath;
                    public object inkVariableValue;
                }
                public string functionName = String.Empty;
                public List<FunctionInput> inputs = new List<FunctionInput>();
            }
			public FunctionParams functionParams = new FunctionParams();
			public string testedFunctionName = null;
			public object functionReturnValue = null;
		}

		public bool showingVariablesPanel;
		public bool showingObservedVariablesPanel;
		private VariablesPanelState variablesPanelState = new VariablesPanelState();
		public class VariablesPanelState {
			public string searchString = "";
			public Dictionary<string, ObservedVariable> observedVariables = new Dictionary<string, ObservedVariable>();
		}

		public DisplayOptions displayOptions = new DisplayOptions();
		public class DisplayOptions {
            [Flags]
			public enum VisibilityOptions {
                Warnings = 1 << 0,
                Errors = 1 << 1,
                Choice = 1 << 2,
                Function = 1 << 3,
                ChoosePathString = 1 << 4,
                DebugNotes = 1 << 5,
                TimeStamp = 1 << 6,
                EmptyEntries = 1 << 7,
            }
            public VisibilityOptions visibilityOptions = (VisibilityOptions)(((int)VisibilityOptions.Warnings) + ((int)VisibilityOptions.Errors));

			public bool displayWarningsInConsole = true;
			public bool displayErrorsInConsole = true;
		}

        public InkPlayerParams playerParams;

		[MenuItem("Window/Ink Player %#i", false, 2300)]
		public static InkPlayerWindow GetWindow () {
			return GetWindow<InkPlayerWindow>(windowTitle, true);
		}

		public static InkPlayerWindow GetWindow (bool focus) {
			return GetWindow<InkPlayerWindow>(windowTitle, focus);
		}
		
		void OnEnable () {
			isOpen = true;
			storyStateHistory = new UndoHistory<InkPlayerHistoryItem>();
			storyHistory = new List<InkPlayerHistoryContentItem>();
			
            InkPlayerWindowState.OnCreateOrLoad += () => {
                functionPanelState.functionParams = InkPlayerWindowState.Instance.functionParams;
                BuildFunctionInputList();
            };
            
            BuildFunctionInputList();
			
            EditorApplication.update += Update;
		}

		void OnDestroy () {
			isOpen = false;
		}

		private void Update () {
			if(story != null && playerOptions.chooseAutomatically) {
				if(story.currentChoices.Count > 0) {
					MakeRandomChoice();
				}
			}
		}

		public static InkPlayerWindow Attach (Story story) {
            return Attach(story, InkPlayerWindow.InkPlayerParams.ForAttachedStories);
        }
		public static InkPlayerWindow Attach (Story story, InkPlayerParams inkPlayerParams) {
			InkPlayerWindow window = GetWindow();
			window.AttachInstance(story, inkPlayerParams);
            return window;
		}

        public void AttachInstance (Story story) {
            AttachInstance(story, InkPlayerWindow.InkPlayerParams.ForAttachedStories);
        }
        // Loads an existing story to the player window. Handy for debugging stories running in games in editor.
        public void AttachInstance (Story story, InkPlayerParams inkPlayerParams) {
			Clear();
			playerOptions.continueAutomatically = false;
			playerOptions.chooseAutomatically = false;
            playerParams = inkPlayerParams;
			this.story = story;
			attached = true;
			attachedWhileInPlayMode = EditorApplication.isPlaying;
        }

		public static void Detach () {
			InkPlayerWindow window = GetWindow();
			window.DetachInstance();
		}

        void DetachInstance () {
            attached = false;
			story = null;
        }

		public static void LoadAndPlay (TextAsset storyJSONTextAsset) {
			InkPlayerWindow window = GetWindow();
			if(window.story != null) {
				if(EditorUtility.DisplayDialog("Story in progress", "The Ink Player Window is already playing a story. Would you like to stop it and load the new story?", "Stop and load", "Cancel")) {
					window.Stop();
					window.Play(storyJSONTextAsset);
				}
			} else {
				window.Play(storyJSONTextAsset);
			}
		}

		public static void LoadAndPlay (string storyJSON) {
			InkPlayerWindow window = GetWindow();
			if(window.story != null) {
				if(EditorUtility.DisplayDialog("Story in progress", "The Ink Player Window is already playing a story. Would you like to stop it and load the new story?", "Stop and load", "Cancel")) {
					window.Stop();
					window.Play(storyJSON);
				}
			} else {
				window.Play(storyJSON);
			}
		}

		void Play (TextAsset storyJSONTextAsset) {
			Play(storyJSONTextAsset, InkPlayerParams.Standard);
		}
		void Play (TextAsset storyJSONTextAsset, InkPlayerParams inkPlayerParams) {
			this.storyJSONTextAsset = storyJSONTextAsset;
			if(!InkEditorUtils.CheckStoryIsValid(storyJSONTextAsset.text, out playStoryException))
				return;
			storyJSON = this.storyJSONTextAsset.text;
            this.playerParams = inkPlayerParams;
			PlayInternal();
		}
        void Play (string storyJSON) {
			Play(storyJSON, InkPlayerParams.Standard);
		}
		void Play (string storyJSON, InkPlayerParams inkPlayerParams) {
			if(!InkEditorUtils.CheckStoryIsValid(storyJSON, out playStoryException))
				return;
			this.storyJSONTextAsset = null;
			this.storyJSON = storyJSON;
            this.playerParams = inkPlayerParams;
			PlayInternal();
		}

		void PlayInternal () {
			story = new Story(storyJSON);
			story.allowExternalFunctionFallbacks = true;
			PingAutomator();
		}

		void PingAutomator () {
            if(playerParams.disablePlayControls) return;
			if(story.canContinue && playerOptions.continueAutomatically) {
				TryContinue();
			} else if(story.currentChoices.Count > 0 && playerOptions.chooseAutomatically) { 
				MakeRandomChoice();
			}
		}
		
		void Stop () {
			Clear ();
		}

		void Clear () {
			functionPanelState.testedFunctionName = null;
			variablesPanelState.searchString = "";
			storyStateHistory.Clear();
			storyHistory.Clear();
			story = null;
		}
		
		void Restart () {
			Stop();
			if(storyJSONTextAsset != null)
				Play(storyJSONTextAsset);
			else
				Play(storyJSON);
		}
		
		void ContinueStory () {
			story.Continue();
		}

		void AddContent (string content) {
			storyHistory.Add(new InkPlayerHistoryContentItem(content, InkPlayerHistoryContentItem.ContentType.StoryContent));
			ScrollToBottom();
			if(!playerParams.disableUndoHistory) AddToHistory();
		}
		
		void AddToHistory () {
			InkPlayerHistoryItem historyItem = new InkPlayerHistoryItem(story.state.ToJson(), new List<InkPlayerHistoryContentItem>(storyHistory));
			storyStateHistory.AddToUndoHistory(historyItem);
		}
		
		void Undo () {
			InkPlayerHistoryItem item = storyStateHistory.Undo();
			story.state.LoadJson(item.inkStateJSON);
			storyHistory = new List<InkPlayerHistoryContentItem>(item.storyHistory);
			ScrollToBottom();
		}
		
		void Redo () {
			InkPlayerHistoryItem item = storyStateHistory.Redo();
			story.state.LoadJson(item.inkStateJSON);
			storyHistory = new List<InkPlayerHistoryContentItem>(item.storyHistory);
			ScrollToBottom();
		}

		void SaveStoryState (string storyStateJSON) {
			storyHistory.Add(new InkPlayerHistoryContentItem("Saved state", InkPlayerHistoryContentItem.ContentType.DebugNote));

			// Text asset can be null if we attached to an existing story rather than loading our own
			string dirPath = "";
			string storyName = "story";
			if( storyJSONTextAsset != null ) {
				dirPath = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(storyJSONTextAsset));
				storyName = storyJSONTextAsset.name;
			}

			storyStateTextAsset = InkEditorUtils.CreateStoryStateTextFile(storyStateJSON, dirPath, storyName+"_SaveState");
		}

		void LoadStoryState (string storyStateJSON) {
			storyHistory.Clear();
			storyStateHistory.Clear();
			storyHistory.Add(new InkPlayerHistoryContentItem("Loaded state", InkPlayerHistoryContentItem.ContentType.DebugNote));
			story.state.LoadJson(storyStateJSON);
		}

		void ScrollToBottom () {
			contentScrollPosition.y = Mathf.Infinity;
		}

		void TryContinue () {
			if(!story.canContinue) 
				return;
			if(playerOptions.continueAutomatically) {
				while (story.canContinue) {
					ContinueStory();
				}
			} else {
				ContinueStory();
			}
		}
		
		void OnGUI () {
            this.Repaint();
			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            if(story == null && attached) DetachInstance();

			DisplayHeader();

			if(playStoryException == null) {
				DisplayStoryControlGroup();
			} else {
				DisplayErrors();
			}

			if(story != null && story.state != null) {
				DrawStory();
				DrawChoices();
				DrawProfilerData();
				DrawSaveLoad();
				DrawDiverts();
				DrawFunctions();
				DrawVariables();
                InkPlayerWindowState.Save();
			}

			EditorGUILayout.EndScrollView();

		}
		
		void DisplayHeader () {
			if(attached) {
				EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                var headerTitle = new System.Text.StringBuilder("Attached");
                if(attachedWhileInPlayMode != EditorApplication.isPlaying) {
                    if(attachedWhileInPlayMode) headerTitle.Append(" (Ex-play-mode story)");
                }
				GUILayout.Label(new GUIContent(headerTitle.ToString(), "This story reference has been attached from elsewhere"));
				if (GUILayout.Button(new GUIContent("Detach", "Detach from the loaded external story"), EditorStyles.toolbarButton)) {
					Detach();
				}
				EditorGUILayout.EndHorizontal();
			} else {
				EditorGUILayout.BeginVertical();
				EditorGUI.BeginChangeCheck();
				storyJSONTextAsset = EditorGUILayout.ObjectField("Story JSON", storyJSONTextAsset, typeof(TextAsset), false) as TextAsset;
				if(EditorGUI.EndChangeCheck()) {
					if(storyJSONTextAsset == null) {
						story = null;
						playStoryException = null;
					} else {
						Stop();
						Play(storyJSONTextAsset);
					}
				}
				if(storyJSONTextAsset != null && storyJSON != null) {
					string fullJSONFilePath = InkEditorUtils.UnityRelativeToAbsolutePath(AssetDatabase.GetAssetPath(storyJSONTextAsset));
					var updatedStoryJSONLastEditDateTime = File.GetLastWriteTime(fullJSONFilePath);
					if (currentStoryJSONLastEditDateTime != updatedStoryJSONLastEditDateTime ) {
						EditorGUILayout.HelpBox ("Story JSON file has changed. Restart to play updated story.", MessageType.Warning);
					}
				}
				EditorGUILayout.EndVertical();
			}
		}
		
		void DisplayErrors () {
			EditorGUILayout.HelpBox(playStoryException.Message, MessageType.Error);
		}

		void DisplayStoryControlGroup () {
			EditorGUILayout.BeginHorizontal (EditorStyles.toolbar);

			if(story == null) {
				EditorGUI.BeginDisabledGroup(storyJSONTextAsset == null);
				if(GUILayout.Button(new GUIContent("Start", "Run the story"), EditorStyles.toolbarButton)) {
					Play(storyJSONTextAsset, InkPlayerParams.Standard);
				}
				EditorGUI.EndDisabledGroup();
			} else {
				EditorGUI.BeginDisabledGroup(playerParams.disablePlayControls);
				if(GUILayout.Button(new GUIContent("Stop", "Stop the story"), EditorStyles.toolbarButton)) {
					Stop();
				}
				if(GUILayout.Button(new GUIContent("Restart", "Restarts the story"), EditorStyles.toolbarButton)) {
					Restart();
				}
				EditorGUI.EndDisabledGroup();
			}

			GUILayout.FlexibleSpace();

			// Profiler button
			if( story != null ) {
				var profileButtonTitle = new GUIContent(
					isProfiling ? "Stop Profiling" : "Profile", 
					(isProfiling ? "Stop" : "Start") + " Profiling");
				isProfiling = GUILayout.Toggle(isProfiling, profileButtonTitle, EditorStyles.toolbarButton);

				GUILayout.FlexibleSpace();
			}
				
			// Undo/Redo
			if(story != null) {
				EditorGUI.BeginDisabledGroup(playerParams.disableUndoHistory || !storyStateHistory.canUndo);
				if(GUILayout.Button(new GUIContent("Undo", "Undo the last continue or choice"), EditorStyles.toolbarButton)) {
					Undo();
				}
				EditorGUI.EndDisabledGroup();
				EditorGUI.BeginDisabledGroup(playerParams.disableUndoHistory || !storyStateHistory.canRedo);
				if(GUILayout.Button(new GUIContent("Redo", "Redo the last continue or choice"), EditorStyles.toolbarButton)) {
					Redo();
				}
				EditorGUI.EndDisabledGroup();
			}

			GUILayout.FlexibleSpace();

            EditorGUI.BeginDisabledGroup(playerParams.disablePlayControls);
			EditorGUI.BeginChangeCheck();
			var newContinueAutomatically = GUILayout.Toggle(playerOptions.continueAutomatically && !playerParams.disablePlayControls, new GUIContent("Auto-Continue", "Continues content automatically"), EditorStyles.toolbarButton);
			if(EditorGUI.EndChangeCheck()) {
                if(!playerParams.disablePlayControls) playerOptions.continueAutomatically = newContinueAutomatically;
				PingAutomator();
			}

			EditorGUI.BeginChangeCheck();
			var newChooseAutomatically = GUILayout.Toggle(playerOptions.chooseAutomatically && !playerParams.disablePlayControls, new GUIContent("Auto-Choice", "Makes choices automatically"), EditorStyles.toolbarButton);
			if(EditorGUI.EndChangeCheck()) {
                if(!playerParams.disablePlayControls) playerOptions.chooseAutomatically = newChooseAutomatically;
            }
            EditorGUI.EndDisabledGroup();

			GUILayout.EndHorizontal();
		}
			
		void DrawStory () {
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			showingContentPanel = EditorGUILayout.Foldout(showingContentPanel, "Content", true);
			
            if(GUILayout.Button(new GUIContent("Clear", "Clears the output"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false))) {
                storyHistory.Clear();
				ScrollToBottom();
			}
            GUILayout.Space(6);
            if(GUILayout.Button(new GUIContent("Copy", "Copy the output to clipboard"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false))) {
                CopyStoryHistoryToClipboard();
			}
            
			EditorGUI.BeginChangeCheck();
            // TODO: tooltips for options. I'd REALLY like for it not to show "Mixed ..." in the box mais c'est la vie
            // TODO: Add a "default" option in the dropdown
            Enum newVisibilityOptions = EditorGUILayout.EnumFlagsField(GUIContent.none, displayOptions.visibilityOptions, EditorStyles.toolbarDropDown, GUILayout.Width(120));
    		displayOptions.visibilityOptions = (DisplayOptions.VisibilityOptions)(int)Convert.ChangeType(newVisibilityOptions, typeof(DisplayOptions.VisibilityOptions));
			if(EditorGUI.EndChangeCheck()) {
				ScrollToBottom();
			}

			EditorGUILayout.EndHorizontal();
			if(showingContentPanel)
				DisplayStoryBody ();
		}

        void CopyStoryHistoryToClipboard () {
            StringBuilder sb = new StringBuilder("Story Log\n");
			foreach(InkPlayerHistoryContentItem content in storyHistory) {
                sb.AppendLine();
                sb.Append(content.time.ToShortDateString());
                sb.Append(" ");
                sb.Append(content.time.ToLongTimeString());
                sb.Append(" (");
                sb.Append(content.contentType.ToString());
                sb.Append(") ");
                sb.Append(content.content);
            }
            GUIUtility.systemCopyBuffer = sb.ToString();
        }

		void DisplayStoryBody () {
			GUILayout.BeginVertical();
			
			contentScrollPosition = EditorGUILayout.BeginScrollView(contentScrollPosition);
			int i = 0;
			foreach(InkPlayerHistoryContentItem content in storyHistory) {
				if(content.contentType == InkPlayerHistoryContentItem.ContentType.StoryContent) {
					DisplayContent(content, i);
				} else if(content.contentType == InkPlayerHistoryContentItem.ContentType.StoryChoice && (displayOptions.visibilityOptions & DisplayOptions.VisibilityOptions.Choice) != 0) {
					DisplayContent(content, i);
				} else if(content.contentType == InkPlayerHistoryContentItem.ContentType.DebugNote && (displayOptions.visibilityOptions & DisplayOptions.VisibilityOptions.DebugNotes) != 0) {
					DisplayContent(content, i);
				} else if(content.contentType == InkPlayerHistoryContentItem.ContentType.StoryWarning && (displayOptions.visibilityOptions & DisplayOptions.VisibilityOptions.Warnings) != 0) {
					DisplayContent(content, i);
				} else if(content.contentType == InkPlayerHistoryContentItem.ContentType.StoryError && (displayOptions.visibilityOptions & DisplayOptions.VisibilityOptions.Errors) != 0) {
					DisplayContent(content, i);
				} else if(content.contentType == InkPlayerHistoryContentItem.ContentType.StoryChoosePathString && (displayOptions.visibilityOptions & DisplayOptions.VisibilityOptions.ChoosePathString) != 0) {
					DisplayContent(content, i);
				} else if(content.contentType == InkPlayerHistoryContentItem.ContentType.StoryEvaluateFunction && (displayOptions.visibilityOptions & DisplayOptions.VisibilityOptions.Function) != 0) {
					DisplayContent(content, i);
				}
                i++;
			}
			EditorGUILayout.EndScrollView();
			GUILayout.EndVertical();
		}

        private static Color historyItemBGColor1Free = new Color(0.81f,0.81f,0.81f,1);
        private static Color historyItemBGColor1Pro = new Color(0.21f,0.21f,0.21f,1);
        private static Texture2D _historyItemBGTexture1;
        private static Texture2D historyItemBGTexture1 {
            get {
                if( _historyItemBGTexture1 == null ) {
                    _historyItemBGTexture1 = new Texture2D( 1, 1 );
                    _historyItemBGTexture1.SetPixel(0, 0, EditorGUIUtility.isProSkin ? historyItemBGColor1Pro : historyItemBGColor1Free);
                    _historyItemBGTexture1.Apply();
                }
                return _historyItemBGTexture1;
            }
        }
        private static GUIStyle _historyItemBGStyle1;
        private static GUIStyle historyItemBGStyle1 {
            get {
                if( _historyItemBGStyle1 == null ) {
                    _historyItemBGStyle1 = new GUIStyle();
                    _historyItemBGStyle1.normal.background = historyItemBGTexture1;
                }
                return _historyItemBGStyle1;
            }
        }


        private static Color historyItemBGColor2Free = new Color(0.83f,0.83f,0.83f,1f);
        private static Color historyItemBGColor2Pro = new Color(0.23f,0.23f,0.23f,1f);
        private static Texture2D _historyItemBGTexture2;
        private static Texture2D historyItemBGTexture2 {
            get {
                if( _historyItemBGTexture2 == null ) {
                    _historyItemBGTexture2 = new Texture2D( 1, 1 );
                    _historyItemBGTexture2.SetPixel(0, 0, EditorGUIUtility.isProSkin ? historyItemBGColor2Pro : historyItemBGColor2Free);
                    _historyItemBGTexture2.Apply();
                }
                return _historyItemBGTexture2;
            }
        }
        private static GUIStyle _historyItemBGStyle2;
        private static GUIStyle historyItemBGStyle2 {
            get {
                if( _historyItemBGStyle2 == null ) {
                    _historyItemBGStyle2 = new GUIStyle();
                    _historyItemBGStyle2.normal.background = historyItemBGTexture2;
                }
                return _historyItemBGStyle2;
            }
        }
		void DisplayContent(InkPlayerHistoryContentItem content, int index)  {
            if((displayOptions.visibilityOptions & DisplayOptions.VisibilityOptions.EmptyEntries) == 0 && content.content == string.Empty) {
                return;
            }

            EditorGUILayout.BeginHorizontal(index % 2 == 0 ? historyItemBGStyle1 : historyItemBGStyle2);
            string s = String.Empty;
			if((displayOptions.visibilityOptions & DisplayOptions.VisibilityOptions.TimeStamp) != 0) 
                s = "("+content.time.ToLongTimeString()+") ";
			if(content.contentType == InkPlayerHistoryContentItem.ContentType.StoryChoice && (displayOptions.visibilityOptions & DisplayOptions.VisibilityOptions.Choice) != 0) {
                var icon = EditorGUIUtility.IconContent("d_Animation.Play");
                EditorGUILayout.LabelField(icon, GUILayout.Width(icon.image.width), GUILayout.Height(icon.image.height));
				s += " > ";
			} else if(content.contentType == InkPlayerHistoryContentItem.ContentType.DebugNote && (displayOptions.visibilityOptions & DisplayOptions.VisibilityOptions.DebugNotes) != 0) {
                // var icon = EditorGUIUtility.IconContent("UnityEditor.ConsoleWindow");
                // EditorGUILayout.LabelField(icon, GUILayout.Width(icon.image.width), GUILayout.Height(icon.image.height));
				s += " // ";
			} else if(content.contentType == InkPlayerHistoryContentItem.ContentType.StoryWarning && (displayOptions.visibilityOptions & DisplayOptions.VisibilityOptions.Warnings) != 0) {
                var icon = EditorGUIUtility.IconContent("console.warnicon.sml");
                EditorGUILayout.LabelField(icon, GUILayout.Width(icon.image.width), GUILayout.Height(icon.image.height));
				s += " WARNING: ";
			} else if(content.contentType == InkPlayerHistoryContentItem.ContentType.StoryError && (displayOptions.visibilityOptions & DisplayOptions.VisibilityOptions.Errors) != 0) {
                var icon = EditorGUIUtility.IconContent("console.erroricon.sml");
                EditorGUILayout.LabelField(icon, GUILayout.Width(icon.image.width), GUILayout.Height(icon.image.height));
				s += " ERROR: ";
			} else {
                var icon = EditorGUIUtility.IconContent("console.infoicon.sml");
                EditorGUILayout.LabelField(icon, GUILayout.Width(icon.image.width), GUILayout.Height(icon.image.height));
            }
			s += content.content;
			DisplayLine(s);
            EditorGUILayout.EndHorizontal();
		}
		void DisplayLine (string content) {
			float width = position.width-28;
			float height = EditorStyles.wordWrappedLabel.CalcHeight(new GUIContent(content), width);
			EditorGUILayout.SelectableLabel(content, EditorStyles.wordWrappedLabel, GUILayout.ExpandHeight(true), GUILayout.Width(width), GUILayout.Height(height));
		}

		void DrawChoices () {
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			showingChoicesPanel = EditorGUILayout.Foldout(showingChoicesPanel, "Choices", true);
			EditorGUILayout.EndHorizontal();
			if(showingChoicesPanel)
				DisplayChoices ();
		}

		void DisplayChoices () {
			EditorGUI.BeginDisabledGroup(playerParams.disableChoices);
			GUILayout.BeginVertical();
			if(story.canContinue) {
				if(GUILayout.Button(new GUIContent("Continue", "Continues once"))) {
					ContinueStory();
				}
				if(GUILayout.Button(new GUIContent("Continue Maximally", "Continues until the next choice"))) {
					while(story.canContinue) {
						ContinueStory();
					}
				}
			} else if(story.currentChoices.Count > 0) {
				foreach(Choice choice in story.currentChoices) {
					GUILayout.BeginHorizontal();
                    if(GUILayout.Button(new GUIContent(choice.text.Trim(), "Index: "+choice.index.ToString()+"\nSourcePath: "+choice.sourcePath.Trim()))) {
						MakeChoice(choice);
					}
                    GUILayout.EndHorizontal();
				}
			} else {
				GUILayout.Label("Reached end of story");
			}

			GUILayout.EndVertical();
			EditorGUI.EndDisabledGroup();
		}

		void MakeRandomChoice () {
			MakeChoice(story.currentChoices[UnityEngine.Random.Range(0, story.currentChoices.Count)]);
		}

		void MakeChoice (Choice choice) {
			story.ChooseChoiceIndex(choice.index);
			if(!playerParams.disableUndoHistory) AddToHistory();
			TryContinue();
		}

		void DrawSaveLoad () {
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			showingSaveLoadPanel = EditorGUILayout.Foldout(showingSaveLoadPanel, "Story State", true);
			EditorGUILayout.EndHorizontal();
			if(showingSaveLoadPanel)
				DrawSaveLoadPanel ();
		}

		void DrawSaveLoadPanel () {
			GUILayout.BeginVertical();

			EditorGUILayout.BeginHorizontal();
			string currentStateJSON = story.state.ToJson();
			if(currentStateJSON.Length < 20000) {
				EditorGUILayout.TextField("Current State JSON", currentStateJSON);
			} else {
				EditorGUILayout.TextField("Current State JSON", "Too long to display!");
			}
			EditorGUI.BeginDisabledGroup(GUIUtility.systemCopyBuffer == currentStateJSON);
			if (GUILayout.Button("Copy To Clipboard")) {
				GUIUtility.systemCopyBuffer = currentStateJSON;
			}
			EditorGUI.EndDisabledGroup();
			if (GUILayout.Button("Save As...")) {
				SaveStoryState(currentStateJSON);
			}
			EditorGUILayout.EndHorizontal();

			EditorGUI.BeginDisabledGroup(playerParams.disableStateLoading);
			EditorGUILayout.BeginHorizontal();
			EditorGUI.BeginChangeCheck();
			storyStateTextAsset = EditorGUILayout.ObjectField("Load Story State JSON File", storyStateTextAsset, typeof(TextAsset), false) as TextAsset;
			EditorGUI.BeginDisabledGroup(storyStateTextAsset == null);
			if (GUILayout.Button("Load")) {
				LoadStoryState(storyStateTextAsset.text);
			}
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();
			if(storyStateTextAsset != null && !storyStateValid) {
				EditorGUILayout.HelpBox("Loaded story state file is not valid.", MessageType.Error);
			}
			GUILayout.EndVertical();
			EditorGUI.EndDisabledGroup();
		}

		void DrawDiverts () {
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			showingDivertsPanel = EditorGUILayout.Foldout(showingDivertsPanel, "Diverts", true);
			EditorGUILayout.EndHorizontal();
			if(showingDivertsPanel)
				DrawDivertsPanel ();
		}

		void DrawDivertsPanel () {
			GUILayout.BeginVertical();
			GUILayout.BeginHorizontal();
			divertPanelState.divertCommand = EditorGUILayout.TextField("Divert command", divertPanelState.divertCommand);
			EditorGUI.BeginDisabledGroup(divertPanelState.divertCommand == "");
			if (GUILayout.Button("Divert")) {
				storyHistory.Add(new InkPlayerHistoryContentItem("Diverted to '"+divertPanelState.divertCommand+"'", InkPlayerHistoryContentItem.ContentType.DebugNote));
				story.ChoosePathString(divertPanelState.divertCommand);
				PingAutomator();
			}
			EditorGUI.EndDisabledGroup();
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
		}

		void DrawFunctions () {
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			showingFunctionsPanel = EditorGUILayout.Foldout(showingFunctionsPanel, "Functions", true);
			EditorGUILayout.EndHorizontal();
			if(showingFunctionsPanel)
				DrawFunctionsPanel ();
		}

		void DrawFunctionsPanel () {
			GUILayout.BeginVertical();

            DrawFunctionInput();
			DrawFunctionOutput();

			GUILayout.EndVertical();
		}

        void DrawFunctionInput () {
            GUILayout.BeginVertical();
			EditorGUI.BeginChangeCheck();
			functionPanelState.functionParams.functionName = EditorGUILayout.TextField("Function", functionPanelState.functionParams.functionName);
			if(EditorGUI.EndChangeCheck()) {
				functionPanelState.testedFunctionName = null;
				functionPanelState.functionReturnValue = null;
			}
			functionInputList.DoLayoutList();
			bool functionIsValid = functionPanelState.functionParams.functionName != String.Empty && story.HasFunction(functionPanelState.functionParams.functionName);
			EditorGUI.BeginDisabledGroup(!functionIsValid);
			if (GUILayout.Button(new GUIContent("Execute", "Runs the function"))) {
				storyHistory.Add(new InkPlayerHistoryContentItem("Execute function '"+functionPanelState.functionParams.functionName+"'", InkPlayerHistoryContentItem.ContentType.DebugNote));
				string outputContent = null;
				object[] allInput = new object[functionPanelState.functionParams.inputs.Count];
				for (int i = 0; i < functionPanelState.functionParams.inputs.Count; i++) {
					var input = functionPanelState.functionParams.inputs[i];
					object obj = null;
					switch(input.type) {
					case FunctionPanelState.FunctionParams.FunctionInput.FunctionInputType.Int:
						obj = input.intValue;
						break;
					case FunctionPanelState.FunctionParams.FunctionInput.FunctionInputType.String:
						obj = input.stringValue;
						break;
					case FunctionPanelState.FunctionParams.FunctionInput.FunctionInputType.Bool:
						obj = input.boolValue;
						break;
					case FunctionPanelState.FunctionParams.FunctionInput.FunctionInputType.InkVariable:
						obj = input.inkVariableValue;
						break;
					}
					allInput[i] = obj;
				}

				functionPanelState.functionReturnValue = story.EvaluateFunction(functionPanelState.functionParams.functionName, out outputContent, allInput);
				if(outputContent != null)
					AddContent(outputContent);
				functionPanelState.testedFunctionName = functionPanelState.functionParams.functionName;
			}
			EditorGUI.EndDisabledGroup();
			GUILayout.EndVertical();
        }

        void DrawFunctionOutput () {
            bool functionIsValid = functionPanelState.functionParams.functionName != String.Empty && story.HasFunction(functionPanelState.functionParams.functionName);
            if(functionIsValid && functionPanelState.functionParams.functionName == functionPanelState.testedFunctionName) {
				if(functionPanelState.functionReturnValue == null) {
					EditorGUILayout.LabelField("Output (Null)");
				} else if(functionPanelState.functionReturnValue is string) {
					EditorGUILayout.TextField("Output (String)", (string)functionPanelState.functionReturnValue);
				} else if(functionPanelState.functionReturnValue is float) {
					EditorGUILayout.FloatField("Output (Float)", (float)functionPanelState.functionReturnValue);
				} else if(functionPanelState.functionReturnValue is int) {
					EditorGUILayout.IntField("Output (Int)", (int)functionPanelState.functionReturnValue);
				} else {
					EditorGUILayout.LabelField("Function returned unexpected type "+functionPanelState.functionReturnValue.GetType().Name+".");
				}
			}
        }

		void BuildFunctionInputList () {
			functionInputList = new ReorderableList(functionPanelState.functionParams.inputs, typeof(FunctionPanelState.FunctionParams.FunctionInput), true, true, true, true);
			functionInputList.drawHeaderCallback = (Rect rect) => {
				EditorGUI.LabelField(rect, "Inputs");
			};
			functionInputList.elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2;
			functionInputList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
				var input = functionPanelState.functionParams.inputs[index];
				Rect typeRect = new Rect(rect.x, rect.y, 80, EditorGUIUtility.singleLineHeight);
				input.type = (FunctionPanelState.FunctionParams.FunctionInput.FunctionInputType)EditorGUI.EnumPopup(typeRect, input.type);
				Rect inputRect = new Rect(rect.x + 90, rect.y, rect.width - 90, EditorGUIUtility.singleLineHeight);
				switch(input.type) {
				case FunctionPanelState.FunctionParams.FunctionInput.FunctionInputType.Int:
					input.intValue = EditorGUI.IntField(inputRect, input.intValue);
					break;
				case FunctionPanelState.FunctionParams.FunctionInput.FunctionInputType.String:
					input.stringValue = EditorGUI.TextField(inputRect, input.stringValue);
					break;
				case FunctionPanelState.FunctionParams.FunctionInput.FunctionInputType.Bool:
					input.boolValue = EditorGUI.Toggle(inputRect, input.boolValue);
					break;
				case FunctionPanelState.FunctionParams.FunctionInput.FunctionInputType.InkVariable:
					var halfInput = new Rect(inputRect.x, inputRect.y, Mathf.RoundToInt(inputRect.width * 0.5f) - 5, inputRect.height);
					var halfInput2 = new Rect(inputRect.x + Mathf.RoundToInt(inputRect.width * 0.5f) + 5, inputRect.y, Mathf.RoundToInt(inputRect.width * 0.5f) - 10, inputRect.height);
					EditorGUI.BeginChangeCheck();
					input.inkVariablePath = EditorGUI.TextField(halfInput, input.inkVariablePath);
					if(EditorGUI.EndChangeCheck()) {
//						story.ListItemWithName(input.inkVariablePath).Values
						if(!StringIsWhiteSpace(input.inkVariablePath)) input.inkVariableValue = story.variablesState[input.inkVariablePath];
						else input.inkVariableValue = null;
					}
					EditorGUI.BeginDisabledGroup(true);
					DrawVariable(halfInput2, GUIContent.none, input.inkVariableValue);
					EditorGUI.EndDisabledGroup();
					break;
				}
			};
		}

		void DrawVariables () {
			if(InkEditorUtils.StoryContainsVariables(story)) {
				EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
				showingVariablesPanel = EditorGUILayout.Foldout(showingVariablesPanel, "Variables", true);

				EditorGUI.BeginDisabledGroup(!showingVariablesPanel);
				bool changed = DrawSearchBar(ref variablesPanelState.searchString);
				if(changed) variablesScrollPosition = Vector2.zero;
				EditorGUI.EndDisabledGroup();

				EditorGUILayout.EndHorizontal();
				if(showingVariablesPanel)
					DrawVariablesPanel ();

				if(variablesPanelState.observedVariables.Count > 0) {
					EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
					showingObservedVariablesPanel = EditorGUILayout.Foldout(showingObservedVariablesPanel, "Observed Variables", true);
					EditorGUILayout.EndHorizontal();
					if(showingObservedVariablesPanel)
						DrawObservedVariables ();
				}
			}
		}

		bool DrawSearchBar (ref string searchString) {
			var lastString = searchString;
			searchString = GUILayout.TextField(searchString, GUI.skin.FindStyle("ToolbarSeachTextField"));
			if (GUILayout.Button("", GUI.skin.FindStyle("ToolbarSeachCancelButton"))) {
				searchString = "";
				GUI.FocusControl(null);
			}
			return lastString != searchString;
		}

		void DrawVariablesPanel () {
			
			GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandHeight(true));
			variablesScrollPosition = EditorGUILayout.BeginScrollView(variablesScrollPosition);
			string variableToChange = null;
			object newVariableValue = null;
			foreach(string variable in story.variablesState) {
				if(!StringIsWhiteSpace(variablesPanelState.searchString) && !StringContains(variable, variablesPanelState.searchString, StringComparison.OrdinalIgnoreCase)) continue;
				EditorGUILayout.BeginHorizontal();
				object variableValue = story.variablesState[variable];
				EditorGUI.BeginChangeCheck();
				variableValue = DrawVariable(new GUIContent(variable), variableValue);
				if(EditorGUI.EndChangeCheck()) {
					variableToChange = variable;
					newVariableValue = variableValue;
				}

				if(variablesPanelState.observedVariables.ContainsKey(variable)) {
					if(GUILayout.Button(new GUIContent("->", "Un-observe this variable"), GUILayout.Width(24))) {
						variablesPanelState.observedVariables.Remove(variable);
					}
				} else {
					if(GUILayout.Button(new GUIContent("->", "Click to observe this variable, tracking changes"), GUILayout.Width(24))) {
						var observedVariable = new ObservedVariable(variable);
						story.ObserveVariable(variable, (variableName, newValue) => observedVariable.AddValueState(newValue));
						observedVariable.AddValueState(variableValue);
						variablesPanelState.observedVariables.Add(variable, observedVariable);
					}
				}
				EditorGUILayout.EndHorizontal();
			}
			if(variableToChange != null) {
				storyHistory.Add(new InkPlayerHistoryContentItem("Change '"+variableToChange+"' from '"+story.variablesState[variableToChange]+"' to '"+newVariableValue+"'", InkPlayerHistoryContentItem.ContentType.DebugNote));
				story.variablesState[variableToChange] = newVariableValue;
				variableToChange = null;
				newVariableValue = null;
			}
			
			EditorGUILayout.EndScrollView();
			GUILayout.EndVertical();
		}


		object DrawVariable (GUIContent variable, object variableValue) {
            EditorGUI.BeginDisabledGroup(playerParams.disableSettingVariables);
			if(variableValue is string) {
				variableValue = EditorGUILayout.TextField(variable, (string)variableValue);
			} else if(variableValue is float) {
				variableValue = EditorGUILayout.FloatField(variable, (float)variableValue);
			} else if(variableValue is int) {
				variableValue = EditorGUILayout.IntField(variable, (int)variableValue);
			} else if(variableValue is InkList) {
				var c = new GUIContent(variable);
				c.text += " (InkList)";
				EditorGUILayout.PrefixLabel(c);
				var inkList = (InkList)variableValue;
				if(inkList.Any()) {
					if(GUILayout.Button("Log Contents")) {
						string log = "Log for InkList "+variable+":";
						foreach(var item in inkList)
							log += "\n"+item.ToString();
						Debug.Log(log);
					}
				} else {
					EditorGUILayout.LabelField("Empty");
				}

			} else if(variableValue == null) {
				EditorGUILayout.LabelField(variable, new GUIContent("InkPlayerError: Variable is null"));
			} else {
				EditorGUILayout.LabelField(variable, new GUIContent("InkPlayerError: Variable is of unexpected type "+variableValue.GetType().Name+"."));
			}
            EditorGUI.EndDisabledGroup();
			return variableValue;
		}

		object DrawVariable (Rect rect, GUIContent variable, object variableValue) {
			if(variableValue is string) {
				variableValue = EditorGUI.TextField(rect, variable, (string)variableValue);
			} else if(variableValue is float) {
				variableValue = EditorGUI.FloatField(rect, variable, (float)variableValue);
			} else if(variableValue is int) {
				variableValue = EditorGUI.IntField(rect, variable, (int)variableValue);
			} else if(variableValue is InkList) {
				var c = new GUIContent(variable);
				c.text += " (InkList)";
				EditorGUI.PrefixLabel(rect, c);
				var inkList = (InkList)variableValue;
				if(inkList.Any()) {
					if(GUILayout.Button("Log Contents")) {
						string log = "Log for InkList "+variable.text+":";
						foreach(var item in inkList)
							log += "\n"+item.ToString();
						Debug.Log(log);
					}
				} else {
					EditorGUI.LabelField(rect, "Empty");
				}
			} else if(variableValue == null) {
				EditorGUI.LabelField(rect, variable, new GUIContent("InkPlayerError: Variable is null"));
			} else {
				EditorGUI.LabelField(rect, variable, new GUIContent("InkPlayerError: Variable is of unexpected type "+variableValue.GetType().Name+"."));
			}
			return variableValue;
		}

		void DrawObservedVariables () {
			List<string> allToRemove = new List<string>();
			foreach(var observedVariable in variablesPanelState.observedVariables) {
				bool removeVariable = DrawObservedVariable(observedVariable.Value);
				if(removeVariable)
					allToRemove.Add(observedVariable.Key);
			}
			foreach(var toRemove in allToRemove) {
				variablesPanelState.observedVariables.Remove(toRemove);
			}
		}

		bool DrawObservedVariable (ObservedVariable observedVariable) {
			GUILayout.BeginHorizontal();
			observedVariable.expanded = EditorGUILayout.Foldout(observedVariable.expanded, observedVariable.variable, true);
			if(GUILayout.Button("<-", GUILayout.Width(24))) {
				return true;
			}
			GUILayout.EndHorizontal();

			if(observedVariable.expanded) {
				GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandHeight(false));
				observedVariable.scrollPosition = EditorGUILayout.BeginScrollView(observedVariable.scrollPosition);
				
				foreach(var value in observedVariable.values) {
					DrawVariable(new GUIContent(value.dateTime.ToLongTimeString()), value.state);
				}
				
				EditorGUILayout.EndScrollView();
				GUILayout.EndVertical();
			}

			return false;
		}

		bool isProfiling {
			get {
				return _currentStoryProfiler != null;
			}
			set {
				var shouldBeProfiling = value;
				if( shouldBeProfiling != isProfiling ) {
					if( _currentStoryProfiler == null ) {
						_currentStoryProfiler = story.StartProfiling();
					} else {
						story.EndProfiling();
						_profilerResultRootNode = _currentStoryProfiler.rootNode;

						Debug.Log(_currentStoryProfiler.StepLengthReport());

						_previousStoryProfiler = _currentStoryProfiler;
						_currentStoryProfiler = null;
					}
				}
			}
		}

		void DrawProfilerData() {

			// Don't show profiler data at all if you've never clicked Profile button
			if( _profilerResultRootNode == null && !isProfiling ) return;

			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			showingProfileData = EditorGUILayout.Foldout(showingProfileData, "Profiler data", true);
			GUILayout.FlexibleSpace();
			if( _previousStoryProfiler != null && GUILayout.Button("Save mega log", EditorStyles.toolbarButton) ) {

				var path = EditorUtility.SaveFilePanel(
					"Save mega log",
					"",
					"megalog.txt",
					"txt");
				if( path != null && path.Length > 0 ) 
					File.WriteAllText(path, _previousStoryProfiler.Megalog());

			}
			EditorGUILayout.EndHorizontal();

			if( showingProfileData ) {
				if( isProfiling ) {
					EditorGUILayout.LabelField("Click 'Stop Profiling' to show profiling results.", EditorStyles.centeredGreyMiniLabel);
				} else {
					DisplayProfileDataNode(_profilerResultRootNode);
				}
			}
		}

		void DisplayProfileDataNode(ProfileNode node) {

			var key = node.key;
			if( key == null ) {
				if( node == _profilerResultRootNode )
					key = "TOTAL";
				else
					key = "?";
			}

			var nodeText = key + ": " + node.ownReport;

			if( node.hasChildren ) {
				node.openInUI = EditorGUILayout.Foldout(node.openInUI, nodeText, true);

				if( node.openInUI ) {
					EditorGUI.indentLevel++;

					foreach(var childNode in node.descendingOrderedNodes)
						DisplayProfileDataNode(childNode.Value);

					EditorGUI.indentLevel--;
				}
			} else {
				EditorGUILayout.LabelField(nodeText);
			}
		}

		static bool StringIsWhiteSpace (string str) {
			return str.All(c => Char.IsWhiteSpace(c));
		}

		static bool StringContains(string str, string toCheck, StringComparison comp) {
			return str.IndexOf(toCheck, comp) >= 0;
		}

		ProfileNode _profilerResultRootNode;
		Ink.Runtime.Profiler _currentStoryProfiler;
		Ink.Runtime.Profiler _previousStoryProfiler;
	}

    // Keeps a history of state changes for an ink variable. Handy for debugging.
	public class ObservedVariable {
		public string variable;
		public List<ObservedVariableState> values = new List<ObservedVariableState>();
		public bool expanded = true;
		public Vector2 scrollPosition = Vector2.zero;

		public class ObservedVariableState {
			public object state;
			public DateTime dateTime;
			public ObservedVariableState (object state) {
				this.state = state;
				dateTime = DateTime.Now;
			}
		}

		public ObservedVariable (string variable) {
			this.variable = variable;
		}
		public void AddValueState (object value) {
			values.Add(new ObservedVariableState(value));
		}
	}


	[System.Serializable]
	public class UndoHistory<T> where T : class {
		
		private int _undoHistoryIndex;
		public int undoHistoryIndex {
			get {
				return _undoHistoryIndex;
			} set {
				_undoHistoryIndex = Mathf.Clamp(value, 0, undoHistory.Count-1);
				if(OnChangeHistoryIndex != null) OnChangeHistoryIndex(undoHistory[undoHistoryIndex]);
			}
		}
		
		public List<T> undoHistory;
		public int maxHistoryItems = 100;
		
		public bool canUndo {
			get {
				return undoHistory.Count > 0 && undoHistoryIndex > 0;
			}
		}
		
		public bool canRedo {
			get {
				return undoHistory.Count > 0 && undoHistoryIndex < undoHistory.Count - 1;
			}
		}
		
		public delegate void OnUndoEvent(T historyItem);
		public event OnUndoEvent OnUndo;
		
		public delegate void OnRedoEvent(T historyItem);
		public event OnRedoEvent OnRedo;
		
		public delegate void OnChangeHistoryIndexEvent(T historyItem);
		public event OnChangeHistoryIndexEvent OnChangeHistoryIndex;
		
		public delegate void OnChangeUndoHistoryEvent();
		public event OnChangeUndoHistoryEvent OnChangeUndoHistory;
		
		public UndoHistory () {
			undoHistory = new List<T>();
			_undoHistoryIndex = -1;
		}
		
		public UndoHistory (int maxHistoryItems) : this () {
			this.maxHistoryItems = Mathf.Clamp(maxHistoryItems, 1, int.MaxValue);
		}
		
		public virtual void AddToUndoHistory (T state) {
			if(undoHistory.Count > 0 && undoHistory.Count - (undoHistoryIndex + 1) > 0) {
				undoHistory.RemoveRange(undoHistoryIndex + 1, undoHistory.Count - (undoHistoryIndex + 1));
			}
			
			if(undoHistory.Count >= maxHistoryItems) {
				undoHistory.RemoveAt (0);
				_undoHistoryIndex--;
			}
			
			undoHistory.Add (state);
			_undoHistoryIndex++;
			
			if(OnChangeUndoHistory != null) OnChangeUndoHistory();
		}
		
		public virtual void Clear () {
			undoHistory.Clear();
			_undoHistoryIndex = -1;
			if(OnChangeUndoHistory != null) OnChangeUndoHistory();
		}
		
		public virtual T Undo () {
			if(!canUndo) {
				if(undoHistory.Count > 0)
					return default(T);
			} else {
				undoHistoryIndex--;
				if(OnUndo != null) OnUndo(undoHistory[undoHistoryIndex]);
			}
			return undoHistory[undoHistoryIndex];
		}
		
		public virtual T Redo () {
			if(!canRedo) {
				if(undoHistory.Count > 0)
					return default(T);
			} else {
				undoHistoryIndex++;
				if(OnRedo != null) OnRedo(undoHistory[undoHistoryIndex]);
			}
			return undoHistory[undoHistoryIndex];
		}
		
		protected virtual void ApplyHistoryItem (T historyItem) {}
	}

	public class InkPlayerHistoryItem {
		public string inkStateJSON;
		public List<InkPlayerHistoryContentItem> storyHistory;
		
		public InkPlayerHistoryItem (string inkStateJSON, List<InkPlayerHistoryContentItem> storyHistory) {
			this.inkStateJSON = inkStateJSON;
			this.storyHistory = storyHistory;
		}
	}

	public class InkPlayerHistoryContentItem {
		public enum ContentType {
			StoryContent,
			StoryChoice,
			StoryEvaluateFunction,
			StoryChoosePathString,
			StoryWarning,
			StoryError,
			DebugNote
		}

		public string content;
		public ContentType contentType;
		public DateTime time;

		public InkPlayerHistoryContentItem (string text, ContentType contentType) {
			this.content = text;
			this.contentType = contentType;
			time = DateTime.Now;
		}
	}
}