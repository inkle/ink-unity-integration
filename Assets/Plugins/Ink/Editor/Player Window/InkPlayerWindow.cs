using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using Ink.Runtime;
using Debug = UnityEngine.Debug;

namespace Ink.UnityIntegration {

	/// <summary>
	/// Ink player window. Tests stories in an editor window.
	/// </summary>
	public class InkPlayerWindow : EditorWindow {
		private TextAsset storyJSONTextAsset;
		private string storyJSON;
		private Story story;
		private TextAsset _storyStateTextAsset;
		private TextAsset storyStateTextAsset {
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
		private bool displayChoicesInLog = false;
		private bool continueAutomatically = true;

		private Vector2 scrollPosition;

		bool showingContentPanel = true;
		private Vector2 contentScrollPosition;
		bool showingChoicesPanel = true;
		bool showingSaveLoadPanel;
		bool showingDivertsPanel;
		private Vector2 divertsScrollPosition;
		bool showingVariablesPanel;
		private Vector2 variablesScrollPosition;

		private string divertCommand;

		private Exception errors;
		bool storyStateValid = false;

		[MenuItem("Window/Ink Player %#i", false, 2300)]
		public static InkPlayerWindow GetWindow () {
			return GetWindow<InkPlayerWindow>("Ink Player", true);
		}
		
		void OnEnable () {
			storyStateHistory = new UndoHistory<InkPlayerHistoryItem>();
			storyHistory = new List<InkPlayerHistoryContentItem>();
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
			if(!InkEditorUtils.CheckStoryIsValid(storyJSONTextAsset.text, out errors))
				return;
			this.storyJSONTextAsset = storyJSONTextAsset;
			storyJSON = this.storyJSONTextAsset.text;
			InitStory();
			TryContinue();
		}

		void Play (string storyJSON) {
			if(!InkEditorUtils.CheckStoryIsValid(storyJSON, out errors))
				return;
			this.storyJSONTextAsset = null;
			this.storyJSON = storyJSON;
			InitStory();
			TryContinue();
		}

		void InitStory () {
			story = new Story(storyJSON);
			story.allowExternalFunctionFallbacks = true;
		}
		
		void Stop () {
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
			storyHistory.Add(new InkPlayerHistoryContentItem(story.currentText.Trim()));
			ScrollToBottom();
			AddToHistory();
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
			storyStateTextAsset = InkEditorUtils.CreateStoryStateTextFile(storyStateJSON, System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(storyJSONTextAsset)), storyJSONTextAsset.name+"_SaveState");
		}

		void LoadStoryState (string storyStateJSON) {
			storyHistory.Clear();
			storyStateHistory.Clear();
			story.state.LoadJson(storyStateJSON);
		}

		void ScrollToBottom () {
			contentScrollPosition.y = Mathf.Infinity;
		}

		void TryContinue () {
			if(!story.canContinue) 
				return;
			if(continueAutomatically) {
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
			DisplayHeader();
			if(errors == null) {
				DisplayControlbar();
			} else {
				DisplayErrors();
			}
			if(story == null) {
				EditorGUILayout.EndScrollView();
				return;
			}
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			showingContentPanel = EditorGUILayout.Foldout(showingContentPanel, "Content");
			EditorGUILayout.EndHorizontal();
			if(showingContentPanel)
				DisplayStory ();

			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			showingChoicesPanel = EditorGUILayout.Foldout(showingChoicesPanel, "Choices");
			EditorGUILayout.EndHorizontal();
			if(showingChoicesPanel)
				DisplayChoices ();

			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			showingSaveLoadPanel = EditorGUILayout.Foldout(showingSaveLoadPanel, "Story State");
			EditorGUILayout.EndHorizontal();
			if(showingSaveLoadPanel)
				DisplaySaveLoad ();

			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			showingDivertsPanel = EditorGUILayout.Foldout(showingDivertsPanel, "Diverts");
			EditorGUILayout.EndHorizontal();
			if(showingDivertsPanel)
				DisplayDiverts ();

			if(InkEditorUtils.StoryContainsVariables(story)) {
				EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
				showingVariablesPanel = EditorGUILayout.Foldout(showingVariablesPanel, "Variables");
				EditorGUILayout.EndHorizontal();
				if(showingVariablesPanel)
					DisplayVariables ();
			}
			EditorGUILayout.EndScrollView();
		}
		
		void DisplayHeader () {
			EditorGUILayout.BeginVertical();
			EditorGUI.BeginChangeCheck();
			storyJSONTextAsset = EditorGUILayout.ObjectField("Story JSON", storyJSONTextAsset, typeof(TextAsset), false) as TextAsset;
			if(EditorGUI.EndChangeCheck()) {
				if(storyJSONTextAsset == null) {
					story = null;
				} else {
					Stop();
					Play(storyJSONTextAsset);
				}
			}
			if(storyJSONTextAsset != null && storyJSON != null && storyJSONTextAsset.text != storyJSON) {
				EditorGUILayout.HelpBox("Story JSON file has changed. Restart to play updated story.", MessageType.Warning);
			}
			EditorGUILayout.EndVertical();
		}
		
		void DisplayErrors () {
			EditorGUILayout.HelpBox(errors.Message, MessageType.Error);
		}
		
		void DisplayControlbar () {
			EditorGUILayout.BeginHorizontal (EditorStyles.toolbar);

			if(story == null) {
				EditorGUI.BeginDisabledGroup(storyJSONTextAsset == null);
				if(GUILayout.Button("Start", EditorStyles.toolbarButton)) {
					Play(storyJSONTextAsset);
				}
				EditorGUI.EndDisabledGroup();
			} else {
				GUILayout.BeginHorizontal();
				if(GUILayout.Button("Stop", EditorStyles.toolbarButton)) {
					Stop();
				}
				if(GUILayout.Button("Restart", EditorStyles.toolbarButton)) {
					Restart();
				}
				EditorGUI.BeginDisabledGroup(!storyStateHistory.canUndo);
				if(GUILayout.Button("Undo", EditorStyles.toolbarButton)) {
					Undo();
				}
				EditorGUI.EndDisabledGroup();
				EditorGUI.BeginDisabledGroup(!storyStateHistory.canRedo);
				if(GUILayout.Button("Redo", EditorStyles.toolbarButton)) {
					Redo();
				}
				EditorGUI.EndDisabledGroup();
				GUILayout.FlexibleSpace();
				EditorGUI.BeginChangeCheck();
				displayChoicesInLog = GUILayout.Toggle(displayChoicesInLog, "Display Choices", EditorStyles.toolbarButton);
				if(EditorGUI.EndChangeCheck()) {
					ScrollToBottom();
				}
				EditorGUI.BeginChangeCheck();
				continueAutomatically = GUILayout.Toggle(continueAutomatically, "Auto-Continue", EditorStyles.toolbarButton);
				if(EditorGUI.EndChangeCheck() && continueAutomatically) {
					while(story.canContinue) {
						ContinueStory();
					}
				}
				GUILayout.EndHorizontal();
			}
			
			EditorGUILayout.EndHorizontal ();
		}
		
		void DisplayStory () {
			GUILayout.BeginVertical();
			
			contentScrollPosition = EditorGUILayout.BeginScrollView(contentScrollPosition);
			
			foreach(InkPlayerHistoryContentItem content in storyHistory) {
				if(content.isChoice) {
					if(displayChoicesInLog)
						DisplayLine(" > "+content.content);
				} else {
					DisplayLine(content.content);
				}
			}
			EditorGUILayout.EndScrollView();
			GUILayout.EndVertical();
		}

		void DisplayLine (string content) {
			float width = position.width-28;
			float height = EditorStyles.wordWrappedLabel.CalcHeight(new GUIContent(content), width);
			EditorGUILayout.SelectableLabel(content, EditorStyles.wordWrappedLabel, GUILayout.ExpandHeight(true), GUILayout.Width(width), GUILayout.Height(height));
		}
		
		void DisplayChoices () {
			GUILayout.BeginVertical();
			
			if(story.canContinue) {
				if(GUILayout.Button("Continue")) {
					ContinueStory();
				}
				if(GUILayout.Button("Continue Maximally")) {
					while(story.canContinue) {
						ContinueStory();
					}
				}
			} else if(story.currentChoices.Count > 0) {
				foreach(Choice choice in story.currentChoices) {
					if(GUILayout.Button(choice.text.Trim())) {
						storyHistory.Add(new InkPlayerHistoryContentItem(choice.text.Trim(), true));
						story.ChooseChoiceIndex(choice.index);
						AddToHistory();
						TryContinue();
					}
				}
			} else {
				GUILayout.Label("Reached end of story");
			}

			GUILayout.EndVertical();
		}

		void DisplaySaveLoad () {
			GUILayout.BeginVertical();

			EditorGUILayout.BeginHorizontal();
			string currentStateJSON = story.state.ToJson();
			EditorGUILayout.TextField("Current State JSON", currentStateJSON);
			EditorGUI.BeginDisabledGroup(GUIUtility.systemCopyBuffer == currentStateJSON);
//			if (GUILayout.Button("Copy To Clipboard")) {
//				GUIUtility.systemCopyBuffer = currentStateJSON;
//			}
			EditorGUI.EndDisabledGroup();
			if (GUILayout.Button("Save As...")) {
				SaveStoryState(currentStateJSON);
			}
			EditorGUILayout.EndHorizontal();

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
		}

		void DisplayDiverts () {
			GUILayout.BeginVertical();
//			divertsScrollPosition = EditorGUILayout.BeginScrollView(divertsScrollPosition);
			divertCommand = EditorGUILayout.TextField("Divert command", divertCommand);
			EditorGUI.BeginDisabledGroup(divertCommand == null || divertCommand == "");
			if (GUILayout.Button("Divert")) {
				story.ChoosePathString(divertCommand);
				TryContinue();
			}
			EditorGUI.EndDisabledGroup();
//			EditorGUILayout.EndScrollView();
			GUILayout.EndVertical();
		}

		void DisplayVariables () {
			GUILayout.BeginVertical(GUI.skin.box, GUILayout.MaxHeight(80));
			variablesScrollPosition = EditorGUILayout.BeginScrollView(variablesScrollPosition);

			string variableToChange = null;
			object newVariableValue = null;
			foreach(string variable in story.variablesState) {
				object variableValue = story.variablesState[variable];
				EditorGUI.BeginChangeCheck();
				if(variableValue is string) {
					variableValue = EditorGUILayout.TextField(variable, (string)variableValue);
				} else if(variableValue is float) {
					variableValue = EditorGUILayout.FloatField(variable, (float)variableValue);
				} else if(variableValue is int) {
					variableValue = EditorGUILayout.IntField(variable, (int)variableValue);
				}
				if(EditorGUI.EndChangeCheck()) {
					variableToChange = variable;
					newVariableValue = variableValue;
				}
			}
			if(variableToChange != null) {
				story.variablesState[variableToChange] = newVariableValue;
				variableToChange = null;
				newVariableValue = null;
			}
			
			EditorGUILayout.EndScrollView();
			GUILayout.EndVertical();
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
		public string content;
		public bool isChoice = false;

		public InkPlayerHistoryContentItem (string text, bool isChoice = false) {
			this.content = text;
			this.isChoice = isChoice;
		}
	}
}