using UnityEngine;
using UnityEditorInternal;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using Ink.Runtime;
using Ink.UnityIntegration.Debugging;

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
            DrawStoryPropertyField(story, InkPlayerParams.ForAttachedStories, label);
        }
		public static void DrawStoryPropertyField (Story story, InkPlayerParams playerParams, GUIContent label) {
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel(label);
			if(EditorApplication.isPlaying) {
				if(story != null) {
					if(InkPlayerWindow.isOpen) {
						// InkPlayerWindow window = InkPlayerWindow.GetWindow(false);
						if(InkPlayerWindow.attached && InkPlayerWindow.story == story) {
							if(GUILayout.Button("Detach")) {
								InkPlayerWindow.Detach();
							}
						} else {
							if(GUILayout.Button("Attach")) {
								InkPlayerWindow.Attach(story, playerParams);
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

            if(story != null) {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Can Continue", story.canContinue.ToString());
                EditorGUI.BeginDisabledGroup(true);
                if(GUILayout.Button("Continue")) {
                    story.Continue();
                }
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField("Current Text", story.currentText);
                foreach(var choice in story.currentChoices) {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Choice "+choice.index, choice.text);
                    EditorGUI.BeginDisabledGroup(true);
                    if(GUILayout.Button("Choose")) {
                        story.ChooseChoiceIndex(choice.index);
                    }
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }
		}

        

		/// <summary>
		/// Draws a property field for a story using GUI, allowing you to attach stories to the player window for debugging.
		/// </summary>
		/// <param name="position">Position.</param>
		/// <param name="story">Story.</param>
		/// <param name="label">Label.</param>
		public static void DrawStoryPropertyField (Rect position, Story story, GUIContent label) {
			position = EditorGUI.PrefixLabel(position, label);
			InkPlayerWindow.GetWindow(false);
			if(EditorApplication.isPlaying && story != null/* && story.state != null*/) {
				if(InkPlayerWindow.attached && InkPlayerWindow.story == story) {
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


		
        [System.Serializable]
        public class InkPlayerWindowState {
            static string settingsEditorPrefsKey = typeof(InkPlayerWindowState).Name +" Settings";
            public static event Action OnCreateOrLoad;
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
                    if(_Instance != null) if(OnCreateOrLoad != null) OnCreateOrLoad();
                } catch {
                    Debug.LogError("Save Data was corrupt and could not be parsed. New data created. Old data was:\n"+data);
                    CreateAndSave();
                }
            }

            public string lastStoryJSONAssetPath;
            public bool lastStoryWasPlaying;
            public TextAsset TryGetLastStoryJSONAsset () {
                if(lastStoryJSONAssetPath == null) return null;
                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(lastStoryJSONAssetPath);
                if(asset == null) {
                    lastStoryJSONAssetPath = null;
                    Save();
                }
                return asset;
            }

		    public StoryPanelState storyPanelState = new StoryPanelState() {showing=true};
    		public BaseStoryPanelState choicePanelState = new BaseStoryPanelState() {showing=true};
            public DivertPanelState divertPanelState = new DivertPanelState();
            public NamedContentPanelState namedContentPanelState = new NamedContentPanelState();
			public FunctionPanelState functionPanelState = new FunctionPanelState();
            // public FunctionPanelState.FunctionParams functionParams = new FunctionPanelState.FunctionParams();
    		public VariablesPanelState variablesPanelState = new VariablesPanelState();
    		public ObservedVariablesPanelState observedVariablesPanelState = new ObservedVariablesPanelState();
    		public BaseStoryPanelState saveLoadPanelState = new BaseStoryPanelState();
    		public BaseStoryPanelState profilerPanelState = new BaseStoryPanelState();
        }

        
		public static PlayerOptions playerOptions = new PlayerOptions();
		[System.Serializable]
		public class PlayerOptions {
			public bool continueAutomatically = true;
			public bool chooseAutomatically = false;
			public float continueAutomaticallyTimeInterval = 0.1f;
			public float chooseAutomaticallyTimeInterval = 0.1f;
		}

        
		private const string windowTitle = "Ink Player";

		public static bool isOpen {get; private set;}

//		public StoryState storyState = new StoryState();
//		public class StoryState {
//		}
		public static bool attached {get; private set;}
		static bool attachedWhileInPlayMode {get; set;}

		static TextAsset _storyJSONTextAsset;
		static TextAsset storyJSONTextAsset {
			get {
				return _storyJSONTextAsset;
			} set {
				if(_storyJSONTextAsset == value) return;
                _storyJSONTextAsset = value;
				if (_storyJSONTextAsset != null) {
                    InkPlayerWindowState.Instance.lastStoryJSONAssetPath = AssetDatabase.GetAssetPath(storyJSONTextAsset);
					string fullJSONFilePath = InkEditorUtils.UnityRelativeToAbsolutePath(InkPlayerWindowState.Instance.lastStoryJSONAssetPath);
					currentStoryJSONLastEditDateTime = File.GetLastWriteTime(fullJSONFilePath);
				} else {
                    InkPlayerWindowState.Instance.lastStoryJSONAssetPath = null;
                }
                InkPlayerWindowState.Save();
			}
		}
		static string storyJSON;
		static DateTime currentStoryJSONLastEditDateTime;

		private static Story _story;
		public static Story story {
            get {
                return _story;
            } private set {
                if(_story != null) {
                    OnUnsetStory();
                }
                _story = value;
                if(_story != null) {
                    OnSetStory();
                }
            }
        }

		private static TextAsset _storyStateTextAsset;
		public static TextAsset storyStateTextAsset {
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
		
		private static UndoHistory<InkPlayerHistoryItem> storyStateHistory = new UndoHistory<InkPlayerHistoryItem>();
		private static List<InkHistoryContentItem> storyHistory = new List<InkHistoryContentItem>();

		
		private static Exception playStoryException;
		private static bool storyStateValid = false;


//		private WindowState windowState = new WindowState();
//		public class WindowState {
		static Vector2 _scrollPosition;
		public static Vector2 scrollPosition {
            get {
                return _scrollPosition;
            } set {
                if(_scrollPosition == value) return;
                _scrollPosition = value;
            }
        }
//		}

		
		
        [System.Serializable]
        public class BaseStoryPanelState {
			public bool showing;
            [SerializeField]
            Vector2 _scrollPosition;
            public Vector2 scrollPosition {
                get {
                    return _scrollPosition;
                } set {
                    if(_scrollPosition == value) return;
                    _scrollPosition = value;
                    // Deselect lines on scrolling because otherwise unity does weird things with the selection
                    // Disabled because it was deselecting the search field as you typed
                    // GUI.FocusControl(null);
                }
            }
		}

		[System.Serializable]
        public class StoryPanelState : BaseStoryPanelState {
            public DisplayOptions displayOptions = new DisplayOptions();
            public string searchString = string.Empty;
		}


        [System.Serializable]
		public class NamedContentPanelState : BaseStoryPanelState {
            public string searchString = string.Empty;
		}

        [System.Serializable]
		public class DivertPanelState : BaseStoryPanelState {
		    public string divertCommand = String.Empty;
		}

		static ReorderableList functionInputList;
        [System.Serializable]
		public class FunctionPanelState : BaseStoryPanelState {
            [System.Serializable]
		    public class FunctionParams {
                [System.Serializable]
                public class FunctionInput {
                    public enum FunctionInputType {
                        Int,
                        String,
                        Bool,
                        InkVariable,
                        InkListVariable
                    }
                    public FunctionInputType type;
                    public int intValue;
                    public string stringValue;
                    public bool boolValue;
                    public string inkVariablePath;
                    public object inkVariableValue;
                    public InkList inkListVariableValue;
                    public string inkListVariablePath;

                    public void RefreshInkVariableValue (Story story) {
                        if(!string.IsNullOrWhiteSpace(inkVariablePath)) inkVariableValue = story.variablesState[inkVariablePath];
                        else inkVariableValue = null;
                    }
                    public void RefreshInkListVariableValue (Story story) {
                        inkListVariableValue = null;
                        try {
                            if(!string.IsNullOrWhiteSpace(inkListVariablePath)) 
                                inkListVariableValue = Ink.Runtime.InkList.FromString(inkListVariablePath, story);
                        } catch {}
                    }
                }
                public string functionName = String.Empty;
                public List<FunctionInput> inputs = new List<FunctionInput>();
            }
			public FunctionParams functionParams = new FunctionParams();
			public string testedFunctionName = null;
			public object functionReturnValue = null;
		}

		[System.Serializable]
        public class VariablesPanelState : BaseStoryPanelState {
			public string searchString = string.Empty;
			public List<string> expandedVariables = new List<string>();
		}
        public class ObservedVariablesPanelState : BaseStoryPanelState {
			public List<string> observedVariableNames = new List<string>();
			public Dictionary<string, ObservedVariable> observedVariables = new Dictionary<string, ObservedVariable>();
		}

		[System.Serializable]
		public class DisplayOptions {
            [Flags]
			public enum VisibilityOptions {
                Warnings = 1 << 0,
                Errors = 1 << 1,
                Content = 1 << 2,
                PresentedChoices = 1 << 3,
                SelectedChoice = 1 << 4,
                Function = 1 << 5,
                ChoosePathString = 1 << 6,
                DebugNotes = 1 << 7,
                TimeStamp = 1 << 8,
                EmptyEntries = 1 << 9,
                Tags = 1 << 10,
            }
            public VisibilityOptions visibilityOptions = VisibilityOptions.Warnings | VisibilityOptions.Errors | VisibilityOptions.Content;

			public bool displayWarningsInConsole = true;
			public bool displayErrorsInConsole = true;
		}

        public static InkPlayerParams playerParams;

        static GUIStyle searchTextFieldStyle;
        static GUIStyle searchCancelButtonStyle;
        
        public static DateTime dateTimeNow;


        static float lastOnGUITime = -1f;
        static float lastUpdateTime = -1f;

        public enum AutoScrollMode {
            NONE,
            Snap,
            Smooth
        }
        static AutoScrollMode markedForScrollToBottom;
        
        static AutoScrollMode markedForScrollToSelectedLine;
        static InkHistoryContentItem selectedLine;
        
        static bool doingAutoscroll;
        static float autoscrollTarget;
        static float autoscrollVelocity;
        static float autoscrollSmoothTime = 0.25f;

        static float timeUntilNextAutomaticChoice = 0;
        static float timeUntilNextAutomaticContinue = 0;




		[MenuItem("Window/Ink Player %#i", false, 2300)]
		public static InkPlayerWindow GetWindow () {
            System.Type windowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
			return GetWindow<InkPlayerWindow>(windowTitle, true, windowType);
		}

		public static InkPlayerWindow GetWindow (bool focus) {
            System.Type windowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
			return GetWindow<InkPlayerWindow>(windowTitle, focus, windowType);
		}
		
		void OnEnable () {
			if(isOpen) return;
			isOpen = true;

            InkPlayerWindowState.OnCreateOrLoad += () => {
                // InkPlayerWindowState.Instance.functionPanelState.functionParams = InkPlayerWindowState.Instance.functionParams;
                BuildFunctionInputList();
            };
            
            BuildFunctionInputList();
			
            EditorApplication.update += Update;

			if(story == null && !EditorApplication.isPlayingOrWillChangePlaymode) {
				var lastLoadedStory = InkPlayerWindowState.Instance.TryGetLastStoryJSONAsset();
				if(lastLoadedStory != null) {
					if(InkPlayerWindowState.Instance.lastStoryWasPlaying) {
						LoadAndPlay(lastLoadedStory);
					} else {
						TryPrepareInternal(lastLoadedStory);
					}
				}
			}
		}

		void OnDisable () {
			EditorApplication.update -= Update;
		}

        private void OnBecameVisible() {
            if(doingAutoscroll) {
                InkPlayerWindowState.Instance.storyPanelState.scrollPosition = new Vector2(InkPlayerWindowState.Instance.storyPanelState.scrollPosition.x, autoscrollTarget);
                doingAutoscroll = false;
            }
        }
    
        private void OnBecameInvisible() {}

		void OnDestroy () {
			isOpen = false;
		}

		private static void Update () {
            var time = Time.realtimeSinceStartup;
            var deltaTime = 0f;
            if(lastUpdateTime != -1)
                deltaTime = time - lastUpdateTime;
            lastUpdateTime = time;
            
			if(story != null) {
                timeUntilNextAutomaticChoice -= deltaTime;
				if(story.currentChoices.Count > 0 && playerOptions.chooseAutomatically) {
                    if(timeUntilNextAutomaticChoice <= 0) {
                        MakeRandomChoice();
                        timeUntilNextAutomaticChoice = playerOptions.chooseAutomaticallyTimeInterval;
                    }
				}
				if(story.canContinue && playerOptions.continueAutomatically) {
                    timeUntilNextAutomaticContinue -= deltaTime;
                    if(timeUntilNextAutomaticContinue <= 0) {
                        if(playerOptions.continueAutomaticallyTimeInterval == 0) {
                            story.ContinueMaximally();
                        } else {
                            ContinueStory();
                        }
                        timeUntilNextAutomaticContinue = playerOptions.continueAutomaticallyTimeInterval;
                    }
				}
			}
		}




        

        static void OnDidContinue () {
			AddStoryContent(story.currentText.Trim(), story.currentTags);
            if(story.currentChoices != null) {
                foreach(var choice in story.currentChoices) {
                    AddToHistory(InkHistoryContentItem.CreateForPresentChoice(choice));
                }
            }
            AddWarningsAndErrorsToHistory();
        }
        static void OnMakeChoice (Choice choice) {
            AddToHistory(InkHistoryContentItem.CreateForMakeChoice(choice));		
            AddWarningsAndErrorsToHistory();
        }
        static void OnEvaluateFunction (string functionName, object[] arguments) {
            StringBuilder sb = new StringBuilder(functionName);
            if(arguments != null && arguments.Length > 0) {
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
            AddToHistory(InkHistoryContentItem.CreateForEvaluateFunction(sb.ToString().Trim()));		
            AddWarningsAndErrorsToHistory();
        }
        static void OnCompleteEvaluateFunction (string functionName, object[] arguments, string textOutput, object result) {
            StringBuilder sb = new StringBuilder(functionName);
            if(arguments != null && arguments.Length > 0) {
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
                bool hasTextOutput = textOutput != null && textOutput != string.Empty;
                if(hasTextOutput) sb.Append(" text output is: "+textOutput);
                if(result != null) sb.Append(" result is: "+result);
                if(!hasTextOutput && result == null) sb.Append("has no output");
            }
            AddToHistory(InkHistoryContentItem.CreateForCompleteEvaluateFunction(sb.ToString().Trim()));		
            AddWarningsAndErrorsToHistory();
        }
        static void OnChoosePathString (string pathString, object[] arguments) {
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
            AddToHistory(InkHistoryContentItem.CreateForChoosePathString(sb.ToString().Trim()));
            AddWarningsAndErrorsToHistory();
        }

		static void AddWarningsAndErrorsToHistory () {
            if(story.hasWarning) {
                foreach(var warning in story.currentWarnings) {
                    AddToHistory(InkHistoryContentItem.CreateForWarning(warning.Trim()));
                    if(InkPlayerWindowState.Instance.storyPanelState.displayOptions.displayWarningsInConsole) {
                        Debug.LogWarning("Ink Warning: "+warning.Trim());
                    }
                }
            }
            if(story.hasError) {
                foreach(var error in story.currentErrors) {
                    AddToHistory(InkHistoryContentItem.CreateForError(error.Trim()));
                    if(InkPlayerWindowState.Instance.storyPanelState.displayOptions.displayErrorsInConsole) {
                        Debug.LogError("Ink Error: "+error.Trim());
                    }
                }
            }
        }





        // Loads an existing story to the player window. Handy for debugging stories running in games in editor.
		public static void Attach (Story story) {
            Attach(story, InkPlayerWindow.InkPlayerParams.ForAttachedStories);
        }
		public static void Attach (Story story, InkPlayerParams inkPlayerParams) {
			Clear();
			playerOptions.continueAutomatically = false;
			playerOptions.chooseAutomatically = false;
            playerParams = inkPlayerParams;
			InkPlayerWindow.story = story;
			attached = true;
			attachedWhileInPlayMode = EditorApplication.isPlaying;
		}
		
		public static void Detach () {
			GetWindow();
			DetachInstance();
		}

        static void DetachInstance () {
            attached = false;
			story = null;
        }

		public static void LoadAndPlay (TextAsset storyJSONTextAsset) {
			GetWindow();
			if(InkPlayerWindow.story != null) {
				if(EditorUtility.DisplayDialog("Story in progress", "The Ink Player Window is already playing a story. Would you like to stop it and load the new story?", "Stop and load", "Cancel")) {
					InkPlayerWindow.Stop();
					InkPlayerWindow.Play(storyJSONTextAsset);
				}
			} else {
				InkPlayerWindow.Play(storyJSONTextAsset);
			}
		}

		public static void LoadAndPlay (string storyJSON) {
			GetWindow();
			if(InkPlayerWindow.story != null) {
				if(EditorUtility.DisplayDialog("Story in progress", "The Ink Player Window is already playing a story. Would you like to stop it and load the new story?", "Stop and load", "Cancel")) {
					InkPlayerWindow.Stop();
					InkPlayerWindow.Play(storyJSON);
				}
			} else {
				InkPlayerWindow.Play(storyJSON);
			}
		}

		static void Play (TextAsset storyJSONTextAsset) {
			Play(storyJSONTextAsset, InkPlayerParams.Standard);
		}
		static void Play (TextAsset storyJSONTextAsset, InkPlayerParams inkPlayerParams) {
			if(TryPrepareInternal(storyJSONTextAsset)) {
				InkPlayerWindow.playerParams = inkPlayerParams;
				PlayInternal();
			}
		}
        static void Play (string storyJSON) {
			Play(storyJSON, InkPlayerParams.Standard);
		}
		static void Play (string storyJSON, InkPlayerParams inkPlayerParams) {
			if(TryPrepareInternal(storyJSON)) {
				InkPlayerWindow.playerParams = inkPlayerParams;
				PlayInternal();
			}
		}

		static void PlayInternal () {
			story = new Story(storyJSON);
			story.allowExternalFunctionFallbacks = true;
		}

		// Loads the story, ready to be played
		static bool TryPrepareInternal (TextAsset newStoryJSONTextAsset) {
            // This forces a refresh
			storyJSONTextAsset = null;
            storyJSONTextAsset = newStoryJSONTextAsset;
			if(storyJSONTextAsset == null || !InkEditorUtils.CheckStoryIsValid(storyJSONTextAsset.text, out playStoryException))
				return false;
			storyJSON = storyJSONTextAsset.text;
			return true;
		}
		static bool TryPrepareInternal (string newStoryJSON) {
			if(!InkEditorUtils.CheckStoryIsValid(storyJSON, out playStoryException))
				return false;
			InkPlayerWindow.storyJSONTextAsset = null;
			InkPlayerWindow.storyJSON = newStoryJSON;
			return true;
		}

        static void OnUnsetStory () {
            _story.onDidContinue -= OnDidContinue;
            _story.onMakeChoice -= OnMakeChoice;
            _story.onEvaluateFunction -= OnEvaluateFunction;
            _story.onCompleteEvaluateFunction -= OnCompleteEvaluateFunction;
            _story.onChoosePathString -= OnChoosePathString;
            foreach(var observedVariableName in InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariableNames) {
                UnobserveVariable(observedVariableName, false);
            }
            InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariables.Clear();

			InkPlayerWindowState.Instance.lastStoryWasPlaying = false;
			InkPlayerWindowState.Save();
        }

        static void OnSetStory () {
            _story.onDidContinue += OnDidContinue;
            _story.onMakeChoice += OnMakeChoice;
            _story.onEvaluateFunction += OnEvaluateFunction;
            _story.onCompleteEvaluateFunction += OnCompleteEvaluateFunction;
            _story.onChoosePathString += OnChoosePathString;
            
            // Recalculate function ink variables
            foreach(var input in InkPlayerWindowState.Instance.functionPanelState.functionParams.inputs) {
                if(input.type == FunctionPanelState.FunctionParams.FunctionInput.FunctionInputType.InkVariable) {
                    input.RefreshInkVariableValue(story);
                } else if(input.type == FunctionPanelState.FunctionParams.FunctionInput.FunctionInputType.InkListVariable) {
                    input.RefreshInkListVariableValue(story);
                }
            }
            
            // Reobserve variables
            var variablesToObserve = new List<string>(InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariableNames);
            InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariables.Clear();
            InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariableNames.Clear();
            foreach(var observedVariableName in variablesToObserve) {
                if(_story.variablesState.Contains(observedVariableName)) {
                    var observedVariable = ObserveVariable(observedVariableName, true);
                    observedVariable.AddValueState(_story.variablesState[observedVariableName]);
                }
            }

			InkPlayerWindowState.Instance.lastStoryWasPlaying = true;
			InkPlayerWindowState.Save();
			
			PingAutomator();
        }

		static void PingAutomator () {
            if(playerParams.disablePlayControls) return;
			if(story.canContinue && playerOptions.continueAutomatically) {
				TryContinue();
			} else if(story.currentChoices.Count > 0 && playerOptions.chooseAutomatically) { 
				MakeRandomChoice();
			}
		}
		
		
		static void Stop () {
			Clear ();
		}

		static void Clear () {
			if(storyStateHistory != null) storyStateHistory.Clear();
			if(storyHistory != null) storyHistory.Clear();
			story = null;
		}
		
		static void Restart () {
			Stop();
			if(storyJSONTextAsset != null)
				Play(storyJSONTextAsset);
			else
				Play(storyJSON);
		}
		
		static void ContinueStory () {
			story.Continue();
		}

		static void AddStoryContent (string content, List<string> tags) {
			AddToHistory(InkHistoryContentItem.CreateForContent(content, tags));
			if(!playerParams.disableUndoHistory) AddToStateHistory();
		}
		
        static void AddToHistory (InkHistoryContentItem content) {
            storyHistory.Add(content);
            ScrollToBottom();
        }

		static void AddToStateHistory () {
			InkPlayerHistoryItem historyItem = new InkPlayerHistoryItem(story.state.ToJson(), new List<InkHistoryContentItem>(storyHistory));
			storyStateHistory.AddToUndoHistory(historyItem);
		}
		
		static void Undo () {
			InkPlayerHistoryItem item = storyStateHistory.Undo();
			story.state.LoadJson(item.inkStateJSON);
			storyHistory = new List<InkHistoryContentItem>(item.storyHistory);
			ScrollToBottom();
		}
		
		static void Redo () {
			InkPlayerHistoryItem item = storyStateHistory.Redo();
			story.state.LoadJson(item.inkStateJSON);
			storyHistory = new List<InkHistoryContentItem>(item.storyHistory);
			ScrollToBottom();
		}

		static void SaveStoryState (string storyStateJSON) {
			AddToHistory(InkHistoryContentItem.CreateForDebugNote("Saved state"));

			// Text asset can be null if we attached to an existing story rather than loading our own
			string dirPath = string.Empty;
			string storyName = "story";
			if( storyJSONTextAsset != null ) {
				dirPath = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(storyJSONTextAsset));
				storyName = storyJSONTextAsset.name;
			}

			storyStateTextAsset = InkEditorUtils.CreateStoryStateTextFile(storyStateJSON, dirPath, storyName+"_SaveState");
		}

		static void LoadStoryState (string storyStateJSON) {
			storyHistory.Clear();
			storyStateHistory.Clear();
			AddToHistory(InkHistoryContentItem.CreateForDebugNote("Loaded state"));
			story.state.LoadJson(storyStateJSON);
		}

		static void ScrollToBottom (bool instant = false) {
            markedForScrollToBottom = instant ? AutoScrollMode.Snap : AutoScrollMode.Smooth;
		}

		static void ScrollToSelectedLine (bool instant = false) {
            markedForScrollToSelectedLine = instant ? AutoScrollMode.Snap : AutoScrollMode.Smooth;
		}

		static void TryContinue () {
			if(!story.canContinue) 
				return;
			// if(playerOptions.continueAutomatically) {
			// 	while (story.canContinue) {
			// 		ContinueStory();
			// 	}
			// } else {
				ContinueStory();
			// }
		}
		
		void OnGUI () {			            
            if(searchTextFieldStyle == null) searchTextFieldStyle = GUI.skin.FindStyle("ToolbarSeachTextField");
            if(searchCancelButtonStyle == null) searchCancelButtonStyle = GUI.skin.FindStyle("ToolbarSeachCancelButton");

            dateTimeNow = System.DateTime.Now;
            var time = Time.realtimeSinceStartup;
            var deltaTime = 0f;
            if(lastOnGUITime != -1)
                deltaTime = time - lastOnGUITime;
            lastOnGUITime = time;
            
            if(doingAutoscroll) {
                var newY = Mathf.SmoothDamp(InkPlayerWindowState.Instance.storyPanelState.scrollPosition.y, autoscrollTarget, ref autoscrollVelocity, autoscrollSmoothTime, Mathf.Infinity, deltaTime);
                InkPlayerWindowState.Instance.storyPanelState.scrollPosition = new Vector2(InkPlayerWindowState.Instance.storyPanelState.scrollPosition.x, newY);
                if(Mathf.Abs(InkPlayerWindowState.Instance.storyPanelState.scrollPosition.y - autoscrollTarget) < 0.1f) doingAutoscroll = false;
            }

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
				DrawStoryHistory();
				DrawChoices();
				DrawProfilerData();
				DrawSaveLoad();
				DrawNamedContent();
				DrawDiverts();
				DrawFunctions();
				DrawVariables();
                InkPlayerWindowState.Save();
			} else {
				// EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.6f));
				GUILayout.Space(40);
				EditorGUILayout.LabelField("Use this window to play and debug ink stories", EditorStyles.boldLabel);
				GUILayout.Space(10);
				EditorGUILayout.LabelField("You can tether your story as it runs in Play Mode to this window, allowing you to view and edit variables and divert in real time!", EditorStyles.wordWrappedLabel);
				GUILayout.Space(20);
				if(GUILayout.Button("View Documentation")) {
					Application.OpenURL("https://github.com/inkle/ink-unity-integration/blob/master/Documentation/InkPlayerWindow.md");
				}
				// EditorGUILayout.EndVertical();
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
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.HelpBox ("Story JSON file has changed. Reload or restart to play updated story.", MessageType.Warning);
						EditorGUILayout.BeginVertical();
						if(GUILayout.Button("Reload")) {
							var storyStateJSON = story.state.ToJson();
							Play(storyJSONTextAsset, InkPlayerWindow.playerParams);
							story.state.LoadJson(storyStateJSON);
						}
						if(GUILayout.Button("Restart")) {
							Restart();
						}
						EditorGUILayout.EndVertical();
						EditorGUILayout.EndHorizontal();
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
				if(GUILayout.Button(new GUIContent("Undo", "Undo the last continue or choice"), EditorStyles.toolbarButton, GUILayout.Width(36))) {
					Undo();
				}
				EditorGUI.EndDisabledGroup();
				EditorGUI.BeginDisabledGroup(playerParams.disableUndoHistory || !storyStateHistory.canRedo);
				if(GUILayout.Button(new GUIContent("Redo", "Redo the last continue or choice"), EditorStyles.toolbarButton, GUILayout.Width(36))) {
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
			playerOptions.continueAutomaticallyTimeInterval = playerOptions.chooseAutomaticallyTimeInterval = GUILayout.HorizontalSlider(playerOptions.continueAutomaticallyTimeInterval, 1f, 0f, GUILayout.Width(80));
			GUILayout.EndHorizontal();
		}
			



		
		#region Story
		void DrawStoryHistory () {
			DisplayStoryHeader();
			if(InkPlayerWindowState.Instance.storyPanelState.showing)
				DisplayStoryBody ();
		}

        void DisplayStoryHeader () {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			InkPlayerWindowState.Instance.storyPanelState.showing = EditorGUILayout.Foldout(InkPlayerWindowState.Instance.storyPanelState.showing, "Content", true);
			
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
            Enum newVisibilityOptions = EditorGUILayout.EnumFlagsField(GUIContent.none, InkPlayerWindowState.Instance.storyPanelState.displayOptions.visibilityOptions, EditorStyles.toolbarDropDown, GUILayout.Width(80));
    		InkPlayerWindowState.Instance.storyPanelState.displayOptions.visibilityOptions = (DisplayOptions.VisibilityOptions)(int)Convert.ChangeType(newVisibilityOptions, typeof(DisplayOptions.VisibilityOptions));
			if(EditorGUI.EndChangeCheck()) {
				ScrollToBottom();
			}

            bool changed = DrawSearchBar(ref InkPlayerWindowState.Instance.storyPanelState.searchString);
            if(changed) {
                if(selectedLine != null) ScrollToSelectedLine(true);
                else ScrollToBottom();
            }

			EditorGUILayout.EndHorizontal();
        }

        void CopyStoryHistoryToClipboard () {
            StringBuilder sb = new StringBuilder("Story Log\n");
			foreach(InkHistoryContentItem content in storyHistory) {
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

        bool ShouldShowContentWithSearchString (string contentString, string searchString) {
            if(StringContains(contentString, InkPlayerWindowState.Instance.storyPanelState.searchString, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        bool ShouldShowContent (InkHistoryContentItem content, DisplayOptions.VisibilityOptions visibilityOpts) {
            switch(content.contentType) {
                case InkHistoryContentItem.ContentType.PresentedContent: {
                    if((visibilityOpts & DisplayOptions.VisibilityOptions.Content) != 0) {
                        if(content.content.Length == 0 && (visibilityOpts & DisplayOptions.VisibilityOptions.EmptyEntries) == 0) return false;
                        else return true;
                    } else return false;
                }
                case InkHistoryContentItem.ContentType.ChooseChoice:
                    return (visibilityOpts & DisplayOptions.VisibilityOptions.SelectedChoice) != 0;
                case InkHistoryContentItem.ContentType.PresentedChoice:
                    return (visibilityOpts & DisplayOptions.VisibilityOptions.PresentedChoices) != 0;
                case InkHistoryContentItem.ContentType.DebugNote:
                    return (visibilityOpts & DisplayOptions.VisibilityOptions.DebugNotes) != 0;
                case InkHistoryContentItem.ContentType.Warning:
                    return (visibilityOpts & DisplayOptions.VisibilityOptions.Warnings) != 0;
                case InkHistoryContentItem.ContentType.Error:
                    return (visibilityOpts & DisplayOptions.VisibilityOptions.Errors) != 0;
                case InkHistoryContentItem.ContentType.ChoosePathString:
                    return (visibilityOpts & DisplayOptions.VisibilityOptions.ChoosePathString) != 0;
                case InkHistoryContentItem.ContentType.EvaluateFunction:
                    return (visibilityOpts & DisplayOptions.VisibilityOptions.Function) != 0;
                default: break;
            }
            return false;
        }
        
        // TODO - Only update this when the story or the search string/visibility options change.
        static List<InkHistoryContentItem> validHistory = new List<InkHistoryContentItem>();
        void GetValidHistory () {
            validHistory.Clear();
            bool doingSearch = !string.IsNullOrWhiteSpace(InkPlayerWindowState.Instance.storyPanelState.searchString);
            var visibilityOpts = InkPlayerWindowState.Instance.storyPanelState.displayOptions.visibilityOptions;
            var count = storyHistory.Count;
            for(int i = 0; i < count; i++) {
                var content = storyHistory[i];
                if(doingSearch && !ShouldShowContentWithSearchString(content.content, InkPlayerWindowState.Instance.storyPanelState.searchString)) continue;
				if(!ShouldShowContent(content, visibilityOpts)) continue;
                validHistory.Add(content);
            }
        }
        void DisplayStoryBody () {
			GetValidHistory();
    
            float storyContentMargin = 4;
            float contentSpacing = 8;
            
            var timestampWidth = 58;
            var contentTypeWidth = 26;
            var tagsWidth = 160;

            var visibilityOptions = InkPlayerWindowState.Instance.storyPanelState.displayOptions.visibilityOptions;
            bool showTimestamp = (visibilityOptions & DisplayOptions.VisibilityOptions.TimeStamp) != 0; 
            bool showTags = (visibilityOptions & DisplayOptions.VisibilityOptions.Tags) != 0; 

			var lastRect = GUILayoutUtility.GetLastRect();
            var containerWidth = position.width - GUI.skin.verticalScrollbar.fixedWidth;
            
            var lineWidth = containerWidth - storyContentMargin * 2;
            
            var contentWidth = lineWidth;
            if(showTimestamp) {
                contentWidth -= timestampWidth;
                contentWidth -= contentSpacing;
            }
            contentWidth -= contentTypeWidth;
            contentWidth -= contentSpacing;
            if(showTags) {
                contentWidth -= tagsWidth;
                contentWidth -= contentSpacing;
			}
            float minScrollRectHeight = 30;
            float maxScrollRectHeight = 480;
            float totalHeight = 0;
            float[] heights = new float[storyHistory.Count];
            int selectedLineIndex = -1;
            float selectedLineY = -1;
            for(int i = 0; i < validHistory.Count; i++) {
                var content = validHistory[i];
                heights[i] = EditorStyles.wordWrappedLabel.CalcHeight(new GUIContent(content.content), contentWidth);
            	if(showTags) {
					var tagsHeight = EditorStyles.wordWrappedLabel.CalcHeight(new GUIContent(GetTagsString(content.tags)), tagsWidth);
					heights[i] = Mathf.Max(heights[i], tagsHeight);
				}
                heights[i] += storyContentMargin * 2;
                if(content == selectedLine) {
                    selectedLineIndex = i;
                    selectedLineY = totalHeight;
                }
				totalHeight += heights[i];
            }

            float scrollRectHeight = Mathf.Clamp(totalHeight, minScrollRectHeight, maxScrollRectHeight);
            
            var viewportRect = new Rect(0,lastRect.yMax,position.width, scrollRectHeight);
            var containerRect = new Rect(0,0,containerWidth, totalHeight);
            
            var newScrollPos = GUI.BeginScrollView(viewportRect, InkPlayerWindowState.Instance.storyPanelState.scrollPosition, containerRect, false, true);
            if(newScrollPos != InkPlayerWindowState.Instance.storyPanelState.scrollPosition) {
                doingAutoscroll = false;
                InkPlayerWindowState.Instance.storyPanelState.scrollPosition = newScrollPos;
            }

            var y = 0f;
            var panelTop = InkPlayerWindowState.Instance.storyPanelState.scrollPosition.y;
            var panelBottom = InkPlayerWindowState.Instance.storyPanelState.scrollPosition.y + scrollRectHeight;
            // int numShown = 0;
            // var log = "";

			// This appears to be necessary, else the selected text moves around when scrolling!
			if(doingAutoscroll) {
				GUI.FocusControl(null);
			}

            for(int i = 0; i < validHistory.Count; i++) {
                var endY = y + heights[i];
                if(panelTop <= endY && panelBottom >= y) {
                    // if(numShown == 0) {
                    //     log += "Total space "+totalHeight+" Scroll "+InkPlayerWindowState.Instance.storyPanelState.storyScrollPosition.y+" Space "+y+", showing: ";
                    // }
                    var content = validHistory[i];
                    var lineContainerRect = new Rect(0, y, containerWidth, heights[i]);
                    var lineRect = new Rect(lineContainerRect.x + storyContentMargin, lineContainerRect.y + storyContentMargin, lineContainerRect.width - storyContentMargin * 2, lineContainerRect.height - storyContentMargin * 2);
                    
                    GUIStyle lineStyle = null;
                    if(selectedLine == content) lineStyle = historyItemBGStyleSelected.guiStyle;
                    else lineStyle = i % 2 == 0 ? historyItemBGStyleDark.guiStyle : historyItemBGStyleLight.guiStyle;
                    
					GUI.Box(lineContainerRect, GUIContent.none, lineStyle);
					if(Event.current.type == EventType.MouseDown && lineContainerRect.Contains(Event.current.mousePosition)) {
						if(Event.current.button == 0) {
							selectedLine = content;
							// To avoid disruption, only scroll when the line is close to the edge of the panel
							var targetY = GetTargetScrollPositionToCenterStoryLine(i, false);
							if(Mathf.Abs(targetY-InkPlayerWindowState.Instance.storyPanelState.scrollPosition.y) > viewportRect.height * 0.25f) {
								ScrollToSelectedLine();
							}
						}
					}
                    string textToDisplay = String.Empty;
                    
                    var x = lineRect.x;
                    if(showTimestamp) {
                        var timeRect = new Rect(x, lineRect.y, timestampWidth, lineRect.height);
                        DisplayLineTime(timeRect, content);
                        x += timestampWidth;
                        x += contentSpacing;
                    }
                    var iconRect = new Rect(x, lineRect.y, contentTypeWidth, lineRect.height);
                    DisplayLineIcon(iconRect, content);
                    x += contentTypeWidth;
                    x += contentSpacing;
                    
                    var contentRect = new Rect(x, lineRect.y, contentWidth, lineRect.height);
					DisplayLine(contentRect, content);
					x += contentWidth;
					x += contentSpacing;
					if(showTags) {
						var tagsRect = new Rect(x, lineRect.y, tagsWidth, lineRect.height);
                        DisplayTags(tagsRect, content);
                        x += tagsWidth;
                        x += contentSpacing;
					}

                    if(Event.current.type == EventType.MouseDown && lineContainerRect.Contains(Event.current.mousePosition)) {
						if(Event.current.button == 1) {
							if(GUI.GetNameOfFocusedControl() != content.GetHashCode().ToString()) {
								GUI.FocusControl(null);
								var contextMenu = new GenericMenu();
								contextMenu.AddItem(new GUIContent("Copy"), false, () => {
									GUIUtility.systemCopyBuffer = content.content;
								});
								contextMenu.ShowAsContext();
								Event.current.Use();
							}
						}
					}

					// log += i+", ";
                }
                y = endY;
            }

            var lineX = storyContentMargin;
            if(showTimestamp) {
                lineX += timestampWidth;
                lineX += contentSpacing * 0.5f;
                GUI.Box(new Rect(lineX, 0, 1, containerRect.height), "", dividerLineStyle.guiStyle);
                lineX += contentSpacing * 0.5f;
            }

            lineX += contentTypeWidth;
            lineX += contentSpacing * 0.5f;
            GUI.Box(new Rect(lineX, 0, 1, containerRect.height), "", dividerLineStyle.guiStyle);
            lineX += contentSpacing * 0.5f;
			
			if(showTags) {
				lineX += contentWidth;
				lineX += contentSpacing * 0.5f;
                GUI.Box(new Rect(lineX, 0, 1, containerRect.height), "", dividerLineStyle.guiStyle);
                lineX += contentSpacing * 0.5f;
			}

            GUI.EndScrollView();
            GUILayout.Space(viewportRect.height);

            if(Event.current.type == EventType.Layout) {
                if(markedForScrollToBottom != AutoScrollMode.NONE) {
                    var targetPosition = totalHeight - viewportRect.height;
                    if(markedForScrollToBottom == AutoScrollMode.Smooth) {
                        doingAutoscroll = true;
                        autoscrollTarget = targetPosition;
                    } else if(markedForScrollToBottom == AutoScrollMode.Snap) {
                        doingAutoscroll = false;
                        InkPlayerWindowState.Instance.storyPanelState.scrollPosition = new Vector2(InkPlayerWindowState.Instance.storyPanelState.scrollPosition.x, targetPosition);
                    }
                    autoscrollVelocity = 0;
                    markedForScrollToBottom = AutoScrollMode.NONE;
                }
                if(markedForScrollToSelectedLine != AutoScrollMode.NONE && selectedLineIndex != -1) {
                    var targetPosition = GetTargetScrollPositionToCenterStoryLine(selectedLineIndex);
                    if(markedForScrollToSelectedLine == AutoScrollMode.Smooth) {
                        doingAutoscroll = true;
                        autoscrollTarget = targetPosition;
                    } else if(markedForScrollToSelectedLine == AutoScrollMode.Snap) {
                        doingAutoscroll = false;
                        InkPlayerWindowState.Instance.storyPanelState.scrollPosition = new Vector2(InkPlayerWindowState.Instance.storyPanelState.scrollPosition.x, targetPosition);
                    }
                    autoscrollVelocity = 0;
                    markedForScrollToSelectedLine = AutoScrollMode.NONE;
                }
            }

			float GetTargetScrollPositionToCenterStoryLine (int lineIndex, bool clamped = true) {
				var targetY = (selectedLineY + heights[lineIndex] * 0.5f) - viewportRect.height * 0.5f;
				if(clamped) targetY = Mathf.Clamp(targetY, 0, totalHeight - viewportRect.height);
				return targetY;
			}
		}

        static ColoredBackgroundGUIStyle _historyItemBGStyleDark;
        static ColoredBackgroundGUIStyle historyItemBGStyleDark {
            get {
                if(_historyItemBGStyleDark == null) _historyItemBGStyleDark = new ColoredBackgroundGUIStyle(new Color(0.8470589f,0.8470589f,0.8470589f,1), new Color(0.21f,0.21f,0.21f,1f), new Color(0.92f,0.92f,0.92f,1), new Color(0.3f,0.3f,0.3f,1f));
                return _historyItemBGStyleDark;
            }
        }

        static ColoredBackgroundGUIStyle _historyItemBGStyleLight;
        static ColoredBackgroundGUIStyle historyItemBGStyleLight {
            get {
                if(_historyItemBGStyleLight == null) _historyItemBGStyleLight = new ColoredBackgroundGUIStyle(new Color(0.8745099f,0.8745099f,0.8745099f,1f), new Color(0.23f,0.23f,0.23f,1f), new Color(0.92f,0.92f,0.92f,1), new Color(0.3f,0.3f,0.3f,1f));
                return _historyItemBGStyleLight;
            }
        }

        static ColoredBackgroundGUIStyle _historyItemBGStyleSelected;
        static ColoredBackgroundGUIStyle historyItemBGStyleSelected {
            get {
                if(_historyItemBGStyleSelected == null) _historyItemBGStyleSelected = new ColoredBackgroundGUIStyle(new Color(0.3920879f,0.6161963f,0.9339623f,1f), new Color(0.243137255f,0.37254902f,0.588235294f,1f));
                return _historyItemBGStyleSelected;
            }
        }

        static ColoredBackgroundGUIStyle _dividerLineStyle;
        static ColoredBackgroundGUIStyle dividerLineStyle {
            get {
                if(_dividerLineStyle == null) _dividerLineStyle = new ColoredBackgroundGUIStyle(new Color(0.85f,0.85f,0.85f,1f), new Color(0.25f,0.25f,0.25f,1f));
                return _dividerLineStyle;
            }
        }

        void DisplayLineTime (Rect rect, InkHistoryContentItem content) {
            EditorGUI.LabelField(rect, new GUIContent(content.time.ToLongTimeString()));
        }

        void DisplayLineIcon (Rect rect, InkHistoryContentItem content) {
            var visibilityOptions = InkPlayerWindowState.Instance.storyPanelState.displayOptions.visibilityOptions;
			if(content.contentType == InkHistoryContentItem.ContentType.ChooseChoice && (visibilityOptions & DisplayOptions.VisibilityOptions.SelectedChoice) != 0) {
                var icon = new GUIContent("*", "Selected Choice");
                EditorGUI.LabelField(rect, icon);
			} else if(content.contentType == InkHistoryContentItem.ContentType.PresentedChoice && (visibilityOptions & DisplayOptions.VisibilityOptions.PresentedChoices) != 0) {
                var icon = new GUIContent("*?", "Presented Choice");
                EditorGUI.LabelField(rect, icon);
			} else if(content.contentType == InkHistoryContentItem.ContentType.EvaluateFunction && (visibilityOptions & DisplayOptions.VisibilityOptions.Function) != 0) {
                var icon = new GUIContent("f(x)", "Took Function");
                EditorGUI.LabelField(rect, icon);
			} else if(content.contentType == InkHistoryContentItem.ContentType.ChoosePathString && (visibilityOptions & DisplayOptions.VisibilityOptions.ChoosePathString) != 0) {
                var icon = new GUIContent("->", "Took Path");
                EditorGUI.LabelField(rect, icon);
			} else if(content.contentType == InkHistoryContentItem.ContentType.DebugNote && (visibilityOptions & DisplayOptions.VisibilityOptions.DebugNotes) != 0) {
                var icon = new GUIContent("//", "Debug Note");
                EditorGUI.LabelField(rect, icon);
			} else if(content.contentType == InkHistoryContentItem.ContentType.Warning && (visibilityOptions & DisplayOptions.VisibilityOptions.Warnings) != 0) {
                var icon = EditorGUIUtility.IconContent("console.warnicon.sml");
                icon.tooltip = "Warning";
                EditorGUI.LabelField(rect, icon);
			} else if(content.contentType == InkHistoryContentItem.ContentType.Error && (visibilityOptions & DisplayOptions.VisibilityOptions.Errors) != 0) {
                var icon = EditorGUIUtility.IconContent("console.erroricon.sml");
                icon.tooltip = "Error";
                EditorGUI.LabelField(rect, icon);
			} else {
                // var icon = EditorGUIUtility.IconContent("console.infoicon.sml");
                // EditorGUI.LabelField(rect, icon);
            }
        }

		void DisplayLine (Rect rect, InkHistoryContentItem content) {
            float timeSinceLastWrite = (float)(dateTimeNow - content.time).TotalSeconds;
            var revealTime = 0.8f;
            var l = Mathf.InverseLerp(revealTime, 0, timeSinceLastWrite);
            var newColor = new Color(GUI.color.r, GUI.color.g, GUI.color.b, 0);
            var color = Color.Lerp(GUI.color, newColor, l);
            var oldGUIColor = GUI.color; 
            GUI.color = color;
			GUI.SetNextControlName(content.GetHashCode().ToString());
            EditorGUI.SelectableLabel(rect, content.content, EditorStyles.wordWrappedLabel);
            GUI.color = oldGUIColor;
        }

		void DisplayTags (Rect rect, InkHistoryContentItem content) {
            float timeSinceLastWrite = (float)(dateTimeNow - content.time).TotalSeconds;
            var revealTime = 0.8f;
            var l = Mathf.InverseLerp(revealTime, 0, timeSinceLastWrite);
            var newColor = new Color(GUI.color.r, GUI.color.g, GUI.color.b, 0);
            var color = Color.Lerp(GUI.color, newColor, l);
            var oldGUIColor = GUI.color; 
            GUI.color = color;
			GUI.SetNextControlName(content.GetHashCode().ToString());
            EditorGUI.SelectableLabel(rect, GetTagsString(content.tags), EditorStyles.wordWrappedLabel);
            GUI.color = oldGUIColor;
        }

		string GetTagsString (List<string> tags) {
			return (tags == null || tags.Count == 0) ? string.Empty : string.Join("\n", tags);
		}
		#endregion






		#region Choices
		void DrawChoices () {
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			InkPlayerWindowState.Instance.choicePanelState.showing = EditorGUILayout.Foldout(InkPlayerWindowState.Instance.choicePanelState.showing, "Choices", true);
			EditorGUILayout.EndHorizontal();
			if(InkPlayerWindowState.Instance.choicePanelState.showing)
				DisplayChoices ();
		}

		void DisplayChoices () {
			EditorGUI.BeginDisabledGroup(playerParams.disableChoices);
			GUILayout.BeginVertical();
			if(story.canContinue) {
                EditorGUI.BeginDisabledGroup(playerOptions.continueAutomatically);
				if(GUILayout.Button(new GUIContent("Continue", "Continues once"))) {
					ContinueStory();
				}
				if(GUILayout.Button(new GUIContent("Continue Maximally", "Continues until the next choice"))) {
					while(story.canContinue) {
						ContinueStory();
					}
				}
                EditorGUI.EndDisabledGroup();
			} else if(story.currentChoices.Count > 0) {
                EditorGUI.BeginDisabledGroup(playerOptions.chooseAutomatically);
				foreach(Choice choice in story.currentChoices) {
					GUILayout.BeginHorizontal();
                    if(GUILayout.Button(new GUIContent(choice.text.Trim(), "Index: "+choice.index.ToString()+"\nSourcePath: "+choice.sourcePath.Trim()))) {
						MakeChoice(choice);
					}
                    GUILayout.EndHorizontal();
				}
                EditorGUI.EndDisabledGroup();
			} else {
				GUILayout.Label("Reached end of story");
			}

			GUILayout.EndVertical();
			EditorGUI.EndDisabledGroup();
		}

		static void MakeRandomChoice () {
			MakeChoice(story.currentChoices[UnityEngine.Random.Range(0, story.currentChoices.Count)]);
		}

		static void MakeChoice (Choice choice) {
			story.ChooseChoiceIndex(choice.index);
			if(!playerParams.disableUndoHistory) AddToStateHistory();
			TryContinue();
		}
		#endregion
		




		#region SaveLoad
		static void DrawSaveLoad () {
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			InkPlayerWindowState.Instance.saveLoadPanelState.showing = EditorGUILayout.Foldout(InkPlayerWindowState.Instance.saveLoadPanelState.showing, "Story State", true);
			EditorGUILayout.EndHorizontal();
			if(InkPlayerWindowState.Instance.saveLoadPanelState.showing)
				DrawSaveLoadPanel ();
		}

		static void DrawSaveLoadPanel () {
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
		#endregion




		

		#region Diverts
		void DrawNamedContent () {
			DrawNamedContentHeader();
			if(InkPlayerWindowState.Instance.namedContentPanelState.showing)
				DrawNamedContentPanel ();
		}

        void DrawNamedContentHeader () {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			InkPlayerWindowState.Instance.namedContentPanelState.showing = EditorGUILayout.Foldout(InkPlayerWindowState.Instance.namedContentPanelState.showing, "Named Content", true);
			
			EditorGUI.BeginDisabledGroup(!InkPlayerWindowState.Instance.namedContentPanelState.showing);
            bool changed = DrawSearchBar(ref InkPlayerWindowState.Instance.namedContentPanelState.searchString);
            if(changed) InkPlayerWindowState.Instance.namedContentPanelState.scrollPosition = Vector2.zero;
            EditorGUI.EndDisabledGroup();

			EditorGUILayout.EndHorizontal();
        }

		void DrawNamedContentPanel () {
			GUILayout.BeginVertical();
			DrawNamedContentContainer(string.Empty, story.mainContentContainer);
			GUILayout.EndVertical();
		}

		void DrawNamedContentContainer (string currentPath, Container container) {
			if(container == null || container.namedOnlyContent == null) return;
			EditorGUI.indentLevel++;
			foreach(var contentKVP in container.namedOnlyContent) {
				var newPath = currentPath.Length == 0 ? contentKVP.Key : currentPath+"."+contentKVP.Key;
				DrawNamedContent(newPath, contentKVP);
			}
			EditorGUI.indentLevel--;
		}
		void DrawNamedContent (string currentPath, KeyValuePair<string, Runtime.Object> contentKVP) {
			GUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(new GUIContent(currentPath, "Path"), GUILayout.Width(200));
			EditorGUILayout.LabelField(new GUIContent(story.state.VisitCountAtPathString(currentPath).ToString(), "Read count"), GUILayout.Width(40));
			if (GUILayout.Button("Divert", GUILayout.Width(80))) {
				AddToHistory(InkHistoryContentItem.CreateForDebugNote("Diverted to '"+currentPath+"'"));
				story.ChoosePathString(currentPath);
				PingAutomator();
			}
			GUILayout.EndHorizontal();
			
			var namedContainer = contentKVP.Value as Container;
			DrawNamedContentContainer(currentPath, namedContainer);		
		}
		#endregion


		#region Diverts
		void DrawDiverts () {
			DrawDivertsHeader();
			if(InkPlayerWindowState.Instance.divertPanelState.showing)
				DrawDivertsPanel ();
		}

        void DrawDivertsHeader () {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			InkPlayerWindowState.Instance.divertPanelState.showing = EditorGUILayout.Foldout(InkPlayerWindowState.Instance.divertPanelState.showing, "Diverts", true);
			EditorGUILayout.EndHorizontal();
        }

		void DrawDivertsPanel () {
			GUILayout.BeginVertical();
			GUILayout.BeginHorizontal();
			InkPlayerWindowState.Instance.divertPanelState.divertCommand = EditorGUILayout.TextField("Divert command", InkPlayerWindowState.Instance.divertPanelState.divertCommand);
			EditorGUI.BeginDisabledGroup(InkPlayerWindowState.Instance.divertPanelState.divertCommand == "");
			if (GUILayout.Button("Divert")) {
				AddToHistory(InkHistoryContentItem.CreateForDebugNote("Diverted to '"+InkPlayerWindowState.Instance.divertPanelState.divertCommand+"'"));
				story.ChoosePathString(InkPlayerWindowState.Instance.divertPanelState.divertCommand);
				PingAutomator();
			}
			EditorGUI.EndDisabledGroup();
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
		}
		#endregion




		#region Functions
		void DrawFunctions () {
			DrawFunctionsHeader();
			if(InkPlayerWindowState.Instance.functionPanelState.showing)
				DrawFunctionsPanel ();
		}

        void DrawFunctionsHeader () {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			InkPlayerWindowState.Instance.functionPanelState.showing = EditorGUILayout.Foldout(InkPlayerWindowState.Instance.functionPanelState.showing, "Functions", true);
			EditorGUILayout.EndHorizontal();
        }

		void DrawFunctionsPanel () {
			GUILayout.BeginVertical();

            DrawFunctionInput();
			DrawFunctionOutput();

			GUILayout.EndVertical();
		}

        void DrawFunctionInput () {
			// TODO - Autocomplete function names using this, which I should add to Story.cs ( ask joe first! )	
			// public IEnumerable<string> allFunctionNames {
			//     get {
			//         return mainContentContainer.namedContent.Keys;
			//     }
			// }

            GUILayout.BeginVertical(GUI.skin.box);
			EditorGUI.BeginChangeCheck();
			InkPlayerWindowState.Instance.functionPanelState.functionParams.functionName = EditorGUILayout.TextField("Function Name", InkPlayerWindowState.Instance.functionPanelState.functionParams.functionName);
			if(EditorGUI.EndChangeCheck()) {
				InkPlayerWindowState.Instance.functionPanelState.testedFunctionName = null;
				InkPlayerWindowState.Instance.functionPanelState.functionReturnValue = null;
			}
			functionInputList.DoLayoutList();
			bool functionIsValid = InkPlayerWindowState.Instance.functionPanelState.functionParams.functionName != String.Empty && story.HasFunction(InkPlayerWindowState.Instance.functionPanelState.functionParams.functionName);
			EditorGUI.BeginDisabledGroup(!functionIsValid);
			if (GUILayout.Button(new GUIContent("Execute", "Runs the function"))) {
				AddToHistory(InkHistoryContentItem.CreateForDebugNote("Execute function '"+InkPlayerWindowState.Instance.functionPanelState.functionParams.functionName+"'"));
				string outputContent = null;
				object[] allInput = new object[InkPlayerWindowState.Instance.functionPanelState.functionParams.inputs.Count];
				for (int i = 0; i < InkPlayerWindowState.Instance.functionPanelState.functionParams.inputs.Count; i++) {
					var input = InkPlayerWindowState.Instance.functionPanelState.functionParams.inputs[i];
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
					case FunctionPanelState.FunctionParams.FunctionInput.FunctionInputType.InkListVariable:
						obj = input.inkListVariableValue;
						break;
					}
					allInput[i] = obj;
				}

				InkPlayerWindowState.Instance.functionPanelState.functionReturnValue = story.EvaluateFunction(InkPlayerWindowState.Instance.functionPanelState.functionParams.functionName, out outputContent, allInput);
				if(outputContent != null)
					AddStoryContent(outputContent, null);
				InkPlayerWindowState.Instance.functionPanelState.testedFunctionName = InkPlayerWindowState.Instance.functionPanelState.functionParams.functionName;
			}
			EditorGUI.EndDisabledGroup();
			GUILayout.EndVertical();
        }

        void DrawFunctionOutput () {
            bool functionIsValid = InkPlayerWindowState.Instance.functionPanelState.functionParams.functionName != String.Empty && story.HasFunction(InkPlayerWindowState.Instance.functionPanelState.functionParams.functionName);
            if(functionIsValid && InkPlayerWindowState.Instance.functionPanelState.functionParams.functionName == InkPlayerWindowState.Instance.functionPanelState.testedFunctionName) {
                GUILayout.BeginVertical(GUI.skin.box);
				if(InkPlayerWindowState.Instance.functionPanelState.functionReturnValue == null) {
					EditorGUILayout.LabelField("Output (Null)");
				} else if(InkPlayerWindowState.Instance.functionPanelState.functionReturnValue is string) {
					EditorGUILayout.TextField("Output (String)", (string)InkPlayerWindowState.Instance.functionPanelState.functionReturnValue);
				} else if(InkPlayerWindowState.Instance.functionPanelState.functionReturnValue is float) {
					EditorGUILayout.FloatField("Output (Float)", (float)InkPlayerWindowState.Instance.functionPanelState.functionReturnValue);
				} else if(InkPlayerWindowState.Instance.functionPanelState.functionReturnValue is int) {
					EditorGUILayout.IntField("Output (Int)", (int)InkPlayerWindowState.Instance.functionPanelState.functionReturnValue);
				} else if(InkPlayerWindowState.Instance.functionPanelState.functionReturnValue is InkList) {
					EditorGUILayoutInkListField(new GUIContent("Output (InkList)"), (InkList)InkPlayerWindowState.Instance.functionPanelState.functionReturnValue);
				} else {
					EditorGUILayout.LabelField("Function returned unexpected type "+InkPlayerWindowState.Instance.functionPanelState.functionReturnValue.GetType().Name+".");
				}
                GUILayout.EndVertical();
			}
        }

		void BuildFunctionInputList () {
			functionInputList = new ReorderableList(InkPlayerWindowState.Instance.functionPanelState.functionParams.inputs, typeof(FunctionPanelState.FunctionParams.FunctionInput), true, true, true, true);
			functionInputList.drawHeaderCallback = (Rect rect) => {
				EditorGUI.LabelField(rect, "Inputs");
			};
			functionInputList.elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2;
			functionInputList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
				var input = InkPlayerWindowState.Instance.functionPanelState.functionParams.inputs[index];
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
					{
                        var halfInput = new Rect(inputRect.x, inputRect.y, Mathf.RoundToInt(inputRect.width * 0.5f) - 5, inputRect.height);
                        var halfInput2 = new Rect(inputRect.x + Mathf.RoundToInt(inputRect.width * 0.5f) + 5, inputRect.y, Mathf.RoundToInt(inputRect.width * 0.5f) - 10, inputRect.height);
                        EditorGUI.BeginChangeCheck();
                        input.inkVariablePath = EditorGUI.TextField(halfInput, input.inkVariablePath);
                        if(EditorGUI.EndChangeCheck()) input.RefreshInkVariableValue(story);
                        
                        EditorGUI.BeginDisabledGroup(true);
                        DrawVariable(halfInput2, GUIContent.none, input.inkVariableValue);
                        EditorGUI.EndDisabledGroup();
                    }
					break;
				case FunctionPanelState.FunctionParams.FunctionInput.FunctionInputType.InkListVariable:
					{
                        var halfInput = new Rect(inputRect.x, inputRect.y, Mathf.RoundToInt(inputRect.width * 0.5f) - 5, inputRect.height);
                        var halfInput2 = new Rect(inputRect.x + Mathf.RoundToInt(inputRect.width * 0.5f) + 5, inputRect.y, Mathf.RoundToInt(inputRect.width * 0.5f) - 10, inputRect.height);
                        EditorGUI.BeginChangeCheck();
                        input.inkListVariablePath = EditorGUI.TextField(halfInput, input.inkListVariablePath);
                        if(EditorGUI.EndChangeCheck()) input.RefreshInkListVariableValue(story);
                        EditorGUI.BeginDisabledGroup(true);
                        DrawVariable(halfInput2, GUIContent.none, input.inkListVariableValue);
                        EditorGUI.EndDisabledGroup();
					}
                    break;
				}
			};
		}
		#endregion



		
		#region Variables
		void DrawVariables () {
			if(InkEditorUtils.StoryContainsVariables(story)) {
				DrawVariablesHeader();
				if(InkPlayerWindowState.Instance.variablesPanelState.showing)
					DrawVariablesPanel ();

				if(InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariables.Count > 0) {
					DrawObservedVariablesHeader();
					if(InkPlayerWindowState.Instance.observedVariablesPanelState.showing)
						DrawObservedVariablesPanel ();
				}
			}
		}

        void DrawVariablesHeader () {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            InkPlayerWindowState.Instance.variablesPanelState.showing = EditorGUILayout.Foldout(InkPlayerWindowState.Instance.variablesPanelState.showing, "Variables", true);

            EditorGUI.BeginDisabledGroup(!InkPlayerWindowState.Instance.variablesPanelState.showing);
            bool changed = DrawSearchBar(ref InkPlayerWindowState.Instance.variablesPanelState.searchString);
            if(changed) InkPlayerWindowState.Instance.variablesPanelState.scrollPosition = Vector2.zero;
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }

		void DrawVariablesPanel () {
			GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandHeight(true));
			InkPlayerWindowState.Instance.variablesPanelState.scrollPosition = EditorGUILayout.BeginScrollView(InkPlayerWindowState.Instance.variablesPanelState.scrollPosition);
			string variableToChange = null;
			object newVariableValue = null;
			foreach(string variable in story.variablesState) {
				if(!string.IsNullOrWhiteSpace(InkPlayerWindowState.Instance.variablesPanelState.searchString) && !StringContains(variable, InkPlayerWindowState.Instance.variablesPanelState.searchString, StringComparison.OrdinalIgnoreCase)) continue;
				EditorGUILayout.BeginHorizontal();
				object variableValue = story.variablesState[variable];
				EditorGUI.BeginChangeCheck();
				variableValue = DrawVariable(new GUIContent(variable), variable, variableValue);
				if(EditorGUI.EndChangeCheck() && story.variablesState[variable] != variableValue) {
					variableToChange = variable;
					newVariableValue = variableValue;
				}

				if(InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariables.ContainsKey(variable)) {
					if(GUILayout.Button(new GUIContent("<-", "Un-observe this variable"), GUILayout.Width(24))) {
						UnobserveVariable(variable, true);
					}
				} else {
					if(GUILayout.Button(new GUIContent("->", "Click to observe this variable, tracking changes"), GUILayout.Width(24))) {
						var observedVariable = ObserveVariable(variable, true);
                        observedVariable.AddValueState(variableValue);
					}
				}
				EditorGUILayout.EndHorizontal();
			}
			if(variableToChange != null) {
				AddToHistory(InkHistoryContentItem.CreateForDebugNote("Change '"+variableToChange+"' from '"+story.variablesState[variableToChange]+"' to '"+newVariableValue+"'"));
				story.variablesState[variableToChange] = newVariableValue;
				variableToChange = null;
				newVariableValue = null;
			}
			
			EditorGUILayout.EndScrollView();
			GUILayout.EndVertical();
		}

        static ObservedVariable ObserveVariable (string variableName, bool alsoAddToCache) {
            if(InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariables.ContainsKey(variableName)) return InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariables[variableName];
            var observedVariable = new ObservedVariable(variableName);
            observedVariable.variableObserver = (_variableName, newValue) => {
                observedVariable.AddValueState(newValue);
            };
            story.ObserveVariable(variableName, observedVariable.variableObserver);
            InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariables.Add(variableName, observedVariable);
            if(alsoAddToCache) {
                InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariableNames.Add(variableName);
                if(InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariables.Count != InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariableNames.Count) {
                    Debug.LogError(InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariables.Count +" "+ InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariableNames.Count);
                    InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariableNames.Clear();
                    InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariables.Clear();
                }
            }
            return observedVariable;
        }

        static void UnobserveVariable (string variableName, bool alsoRemoveFromCache) {
            if(!InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariables.ContainsKey(variableName)) return;
            
            var observedVariable = InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariables[variableName];
            story.RemoveVariableObserver(observedVariable.variableObserver, variableName);
            InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariables.Remove(variableName);
            if(alsoRemoveFromCache) {
                InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariableNames.Remove(variableName);
                if(InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariables.Count != InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariableNames.Count) {
                    Debug.LogError(InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariables.Count +" "+ InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariableNames.Count);
                    InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariableNames.Clear();
                    InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariables.Clear();
                }
            }
        }

		object DrawVariable (GUIContent guiContent, string variableName, object variableValue) {
            EditorGUILayout.BeginHorizontal();
			if(variableValue is string) {
                EditorGUI.BeginDisabledGroup(playerParams.disableSettingVariables);
				variableValue = EditorGUILayout.TextField(guiContent, (string)variableValue);
                EditorGUI.EndDisabledGroup();
			} else if(variableValue is float) {
                EditorGUI.BeginDisabledGroup(playerParams.disableSettingVariables);
				variableValue = EditorGUILayout.FloatField(guiContent, (float)variableValue);
                EditorGUI.EndDisabledGroup();
			} else if(variableValue is int) {
                EditorGUI.BeginDisabledGroup(playerParams.disableSettingVariables);
				variableValue = EditorGUILayout.IntField(guiContent, (int)variableValue);
                EditorGUI.EndDisabledGroup();
			} else if(variableValue is bool) {
                EditorGUI.BeginDisabledGroup(playerParams.disableSettingVariables);
				variableValue = EditorGUILayout.Toggle(guiContent, (bool)variableValue);
                EditorGUI.EndDisabledGroup();
			} else if(variableValue is InkList) {
                EditorGUILayoutInkListField(guiContent, (InkList)variableValue, variableName);
			} else if(variableValue is Ink.Runtime.Path) {
				var c = new GUIContent(((Ink.Runtime.Path)variableValue).ToString()+" (Ink.Runtime.Path)");
				EditorGUILayout.LabelField(guiContent, c);
			} else if(variableValue == null) {
				EditorGUILayout.LabelField(guiContent, new GUIContent("InkPlayerError: Variable is null"));
			} else {
				EditorGUILayout.LabelField(guiContent, new GUIContent("InkPlayerError: Variable is of unexpected type "+variableValue.GetType().Name+"."));
			}
            EditorGUILayout.EndHorizontal();
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
				var inkList = (InkList)variableValue;
				c.text += " (InkList)";
                if(inkList.Any()) {
                    bool first = true;
                    foreach(var item in inkList) {
                        if(!first) c.text += ", ";
                        c.text += item.ToString();
                        first = false;
                    }
                } else {
                    c.text += " Empty";
                }
				EditorGUI.LabelField(rect, c);
			} else if(variableValue is Ink.Runtime.Path) {
				var c = new GUIContent(((Ink.Runtime.Path)variableValue).ToString()+" (Ink.Runtime.Path)");
				EditorGUI.LabelField(rect, c);
			} else if(variableValue == null) {
				EditorGUI.LabelField(rect, variable, new GUIContent("InkPlayerError: Variable is null"));
			} else {
				EditorGUI.LabelField(rect, variable, new GUIContent("InkPlayerError: Variable is of unexpected type "+variableValue.GetType().Name+"."));
			}
			return variableValue;
		}

		void DrawObservedVariablesHeader () {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            InkPlayerWindowState.Instance.observedVariablesPanelState.showing = EditorGUILayout.Foldout(InkPlayerWindowState.Instance.observedVariablesPanelState.showing, "Observed Variables", true);
            EditorGUILayout.EndHorizontal();
        }

		void DrawObservedVariablesPanel () {
			List<string> allToRemove = new List<string>();
			foreach(var observedVariable in InkPlayerWindowState.Instance.observedVariablesPanelState.observedVariables) {
				bool removeVariable = DrawObservedVariable(observedVariable.Value);
				if(removeVariable)
					allToRemove.Add(observedVariable.Key);
			}
			foreach(var toRemove in allToRemove) {
				UnobserveVariable(toRemove, true);
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
				GUILayout.BeginVertical(GUILayout.ExpandHeight(false));
				observedVariable.scrollPosition = EditorGUILayout.BeginScrollView(observedVariable.scrollPosition, GUI.skin.box);
				
				foreach(var value in observedVariable.values) {
					DrawVariable(new GUIContent(value.dateTime.ToLongTimeString()), observedVariable.variable, value.state);
				}
				
				EditorGUILayout.EndScrollView();
				GUILayout.EndVertical();
			}

			return false;
		}
		#endregion





		#region Profiler
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
		ProfileNode _profilerResultRootNode;
		Ink.Runtime.Profiler _currentStoryProfiler;
		Ink.Runtime.Profiler _previousStoryProfiler;


		void DrawProfilerData() {

			// Don't show profiler data at all if you've never clicked Profile button
			if( _profilerResultRootNode == null && !isProfiling ) return;

			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			InkPlayerWindowState.Instance.profilerPanelState.showing = EditorGUILayout.Foldout(InkPlayerWindowState.Instance.profilerPanelState.showing, "Profiler data", true);
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

			if(InkPlayerWindowState.Instance.profilerPanelState.showing) {
				if(isProfiling) {
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
		#endregion
		




        


        
		#region Utils
		static bool StringContains(string str, string toCheck, StringComparison comp) {
            if(toCheck.Length == 0) return false;
			return str.IndexOf(toCheck, comp) >= 0;
		}

		static bool DrawSearchBar (ref string searchString) {
			var lastString = searchString;
			searchString = GUILayout.TextField(searchString, searchTextFieldStyle);
			if (GUILayout.Button("", searchCancelButtonStyle)) {
				searchString = string.Empty;
			}
			return lastString != searchString;
		}

        static void EditorGUILayoutInkListField (GUIContent guiContent, InkList inkList, string expandedVariableKey = null) {
            if(inkList.Any()) {
                var show = expandedVariableKey == null ? true : InkPlayerWindowState.Instance.variablesPanelState.expandedVariables.Contains(expandedVariableKey);
                var c = new GUIContent(guiContent);
                c.text += " (InkList with "+inkList.Count+" entries)";
                EditorGUILayout.BeginVertical();
                
                EditorGUI.BeginChangeCheck();
                show = EditorGUILayout.Foldout(show, c, true);
                if(EditorGUI.EndChangeCheck() && expandedVariableKey != null) {
                    if(show) InkPlayerWindowState.Instance.variablesPanelState.expandedVariables.Add(expandedVariableKey);
                    else InkPlayerWindowState.Instance.variablesPanelState.expandedVariables.Remove(expandedVariableKey);
                }
                
                if(show) {
                    EditorGUI.indentLevel++;
                    foreach(var item in inkList) {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(new GUIContent(item.Key.fullName));
                        // Disabled until I can be bothered to integrate this into the change detection system
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.IntField(item.Value, GUILayout.Width(100));
                        EditorGUI.EndDisabledGroup();
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();
            } else {
                var c = new GUIContent(guiContent);
                c.text += " (InkList)";
                EditorGUILayout.PrefixLabel(c);
                EditorGUILayout.LabelField("Empty");
            }
        }

        // static void EditorGUILayoutInkListField (string text, InkList inkList) {
        //     EditorGUILayoutInkListField(new GUIContent(text), inkList);
        // }

        // static void EditorGUILayoutInkListField (GUIContent content, InkList inkList) {
        //     EditorGUILayout.BeginVertical(GUI.skin.box);
        //     EditorGUILayout.LabelField("InkList with "+inkList.Count+" values:", EditorStyles.boldLabel);
        //     foreach(var item in inkList) {
        //         EditorGUILayout.LabelField(item.Key.ToString()+" ("+item.Value.ToString()+")");
        //     }
        //     EditorGUILayout.EndVertical();
        // }
        static void EditorGUIInkListField (Rect rect, GUIContent content, InkList inkList, string variableName) {
            EditorGUI.PrefixLabel(rect, content);
            if(inkList.Any()) {
                if(GUILayout.Button("Log Contents")) {
                    string log = "Log for InkList "+variableName+":";
                    foreach(var item in inkList)
                        log += item.ToString() + " / ";
                    Debug.Log(log);
                }
            } else {
                EditorGUI.LabelField(rect, "Empty");
            }
        }
		#endregion
	}

    // Keeps a history of state changes for an ink variable. Handy for debugging.
	public class ObservedVariable {
		public string variable;
		public Story.VariableObserver variableObserver;
		public List<ObservedVariableState> values = new List<ObservedVariableState>();
		public bool expanded = true;
		public Vector2 scrollPosition = Vector2.zero;

		public class ObservedVariableState {
			public object state;
			public DateTime dateTime;
			public ObservedVariableState (object state) {
                // Make sure to clone any object ref types! (just InkList at time of writing)
                if(state is InkList) state = new InkList((InkList)state);
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


    public class ColoredBackgroundGUIStyle {
        public GUIStyle guiStyle;
        public ColoredBackgroundGUIStyle (Color color) : this (color, color) {}
        public ColoredBackgroundGUIStyle (Color colorFree, Color colorPro) {
            guiStyle = new GUIStyle();

            var texture = new Texture2D( 1, 1 );
            texture.SetPixel(0, 0, EditorGUIUtility.isProSkin ? colorPro : colorFree);
            texture.Apply();
            guiStyle.normal.background = texture;
        }
        public ColoredBackgroundGUIStyle (Color colorFree, Color colorPro, Color hoverColorFree, Color hoverColorPro) {
            guiStyle = new GUIStyle();

            var texture = new Texture2D( 1, 1 );
            texture.SetPixel(0, 0, EditorGUIUtility.isProSkin ? colorPro : colorFree);
            texture.Apply();
            guiStyle.normal.background = texture;

            var hoverTexture = new Texture2D( 1, 1 );
            hoverTexture.SetPixel(0, 0, EditorGUIUtility.isProSkin ? hoverColorPro : hoverColorFree);
            hoverTexture.Apply();
			guiStyle.hover.background = hoverTexture;
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
		public List<InkHistoryContentItem> storyHistory;
		
		public InkPlayerHistoryItem (string inkStateJSON, List<InkHistoryContentItem> storyHistory) {
			this.inkStateJSON = inkStateJSON;
			this.storyHistory = storyHistory;
		}
	}
}