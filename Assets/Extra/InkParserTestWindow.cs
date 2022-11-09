/*
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Ink.UnityIntegration;
using Ink.UnityIntegration.Debugging;

[InitializeOnLoad]
public class InkParserTestWindow : EditorWindow {

	[System.Serializable]
	public class InkParserTestWindowSettings : SerializedScriptableSingleton<InkParserTestWindowSettings> {
		public string cultureCode;
		public bool showInkValidator;
		public string inkValidatorText;
		public List<string> inkValidatorTags = new List<string>();
		public bool hasValidChoice;
		public ScriptChoice validatedChoice;
		public bool hasValidContent;
		public ScriptContent validatedContent;
		public bool showCurrentParsedChoices;
		public bool showStoryLog;
	}

	private const string windowTitle = "Ink Parser Window";
	private const float labelWidth = 300;
	public Vector2 scrollPosition = Vector2.zero;
	public InkParserTestWindowSettings settings;
	ReorderableList _tagList;
	ReorderableList tagList {
		get {
			if(_tagList == null) {
				_tagList = new ReorderableList(settings.inkValidatorTags, typeof(string));
				_tagList.drawHeaderCallback = (Rect rect) => {
					EditorGUI.LabelField(rect, "Tags");
					if(GUI.Button(new Rect(rect.xMax-80,rect.y,80,rect.height), "Clear")) {
                        tagList.list.Clear();
                    }
				};
				_tagList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
					rect.x += ReorderableList.Defaults.dragHandleWidth;
					rect.width -= ReorderableList.Defaults.dragHandleWidth;
					settings.inkValidatorTags[index] = EditorGUI.TextField(rect, GUIContent.none, settings.inkValidatorTags[index]);
				};
				_tagList.onAddCallback = (ReorderableList list) => {
					settings.inkValidatorTags.Add(string.Empty);
				};
			}
			return _tagList;
		}
	}
	
	
	[MenuItem(GameEditorUtils.menuItemPath+"/Ink Parser", false, 2400)]
	static InkParserTestWindow Init () {
		InkParserTestWindow window = EditorWindow.GetWindow(typeof(InkParserTestWindow), false, windowTitle) as InkParserTestWindow;
		window.titleContent = new GUIContent(windowTitle);
		return window;
	}

	static InkParserTestWindow () {
        InkPlayerWindow.contextMenuDelegates.Add((GenericMenu contextMenu, InkHistoryContentItem content) => {
            contextMenu.AddItem(new GUIContent("Test parsing"), false, () => {
                var window = Init();
                InkParserTestWindowSettings.Instance.inkValidatorText = content.content;
                InkParserTestWindowSettings.Instance.inkValidatorTags = content.tags;
                window.Parse(InkParserTestWindowSettings.Instance.inkValidatorText, InkParserTestWindowSettings.Instance.inkValidatorTags);
                InkParserTestWindowSettings.Save();
            });
        });
    }

	void OnFocus () {
		settings = InkParserTestWindowSettings.Instance;
		Parse(settings.inkValidatorText, settings.inkValidatorTags);
		UnityEditor.Undo.undoRedoPerformed += OnUndoRedo;
	}
	void OnUnfocus () {
		settings = InkParserTestWindowSettings.Instance;
		Parse(settings.inkValidatorText, settings.inkValidatorTags);
		UnityEditor.Undo.undoRedoPerformed -= OnUndoRedo;
	}

	void OnGUI () {
		if(settings == null) return;
		Repaint();
        
		GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
		scrollPosition = GUILayout.BeginScrollView(scrollPosition);
		
		Draw();
		DrawParsers();
		
		GUILayout.EndScrollView();
		GUILayout.EndVertical();
		InkParserTestWindowSettings.Save(settings);
	}
	
	void Draw () {
		Undo.RecordObject(settings, "Ink Validator Text");
		

		// Culture Test
		EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
		EditorGUILayout.LabelField("Culture");
        EditorGUILayout.EndHorizontal();
		EditorGUILayout.BeginVertical(GUI.skin.box);

		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Current Culture", System.Globalization.CultureInfo.CurrentCulture.ToString());
		EditorGUI.BeginDisabledGroup(System.Globalization.CultureInfo.DefaultThreadCurrentCulture == null);
		if(GUILayout.Button("Reset", GUILayout.Width(120))) {
			System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.DefaultThreadCurrentCulture;
			Parse(settings.inkValidatorText, settings.inkValidatorTags);
		}
		EditorGUI.EndDisabledGroup();
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.PrefixLabel(new GUIContent("Culture"));
		if(GUILayout.Button("en-US", EditorStyles.miniButtonLeft, GUILayout.Width(48))) {
			settings.cultureCode = "en-US";
		}
		if(GUILayout.Button("en-GB", EditorStyles.miniButtonMid, GUILayout.Width(48))) {
			settings.cultureCode = "en-GB";
		}
		if(GUILayout.Button("fr-FR", EditorStyles.miniButtonMid, GUILayout.Width(48))) {
			settings.cultureCode = "fr-FR";
		}
		if(GUILayout.Button("es-ES", EditorStyles.miniButtonRight, GUILayout.Width(48))) {
			settings.cultureCode = "es-ES";
		}
		settings.cultureCode = EditorGUILayout.TextField(settings.cultureCode);
		if(GUILayout.Button("Apply", GUILayout.Width(120))) {
			System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(settings.cultureCode);
			Parse(settings.inkValidatorText, settings.inkValidatorTags);
		}
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.EndVertical();


		// Input
		EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
		EditorGUILayout.LabelField("Input");
        EditorGUILayout.EndHorizontal();
		EditorGUILayout.BeginVertical(GUI.skin.box);

		EditorGUI.BeginChangeCheck();
		EditorGUILayout.BeginHorizontal();
		
		settings.inkValidatorText = EditorGUILayout.TextField("Raw Content", settings.inkValidatorText);

        var indexOfHash = settings.inkValidatorText.IndexOf('#');
        if(indexOfHash != -1) {
		    if(GUILayout.Button("Extract Tags", GUILayout.Width(100))) {
                var newText = settings.inkValidatorText.Substring(0, indexOfHash).TrimEnd();
                var tag = settings.inkValidatorText.Substring(indexOfHash+1, settings.inkValidatorText.Length-(indexOfHash+1)).TrimStart();
                settings.inkValidatorText = newText;
                settings.inkValidatorTags.Add(tag);
            }
        }

		EditorGUILayout.EndHorizontal();
		tagList.DoLayoutList();
		if(EditorGUI.EndChangeCheck()) {
			Parse(settings.inkValidatorText, settings.inkValidatorTags);
		}
		EditorGUILayout.EndVertical();

		// Output
		// EditorGUILayout.BeginVertical(GUI.skin.box);
		if(settings.hasValidChoice) {
			var choice = settings.validatedChoice;
			// EditorGUILayout.BeginVertical(GUI.skin.box);
			
			DrawScriptChoice(choice);

			// EditorGUILayout.EndVertical();
		}
		if(settings.hasValidContent) {
			var content = settings.validatedContent;
			// EditorGUILayout.BeginVertical(GUI.skin.box);

			DrawScriptContent(content);
			
			// EditorGUILayout.EndVertical();
		}
		// EditorGUILayout.EndVertical();
	}
	
	void Parse (string inkValidatorText, List<string> tags) {
		// Reset
		settings.hasValidChoice = settings.hasValidContent = false;
		try {
			// Parse as choice
			var choice = new Ink.Runtime.Choice();
			choice.text = inkValidatorText;
			
			settings.hasValidChoice = ScriptChoice.TryParse(choice, out settings.validatedChoice);

			// Parse as content
			settings.hasValidContent = ScriptContent.TryParse(inkValidatorText, tags, out settings.validatedContent);
		} catch {

		}
	}

	void DrawScriptContent (ScriptContent content) {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
		EditorGUILayout.LabelField("Parsed as Content");
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginVertical(GUI.skin.box);
		if(content == null) EditorGUILayout.LabelField("Content is null");
		else DrawFieldsForType(content);
        EditorGUILayout.EndVertical();
	}

	void DrawScriptChoice(ScriptChoice choice) {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
		EditorGUILayout.LabelField("Parsed as Choice");
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginVertical(GUI.skin.box);
		if(choice == null) EditorGUILayout.LabelField("Choice is null");
		// EditorGUILayout.LabelField(choice.rawContent, EditorStyles.boldLabel);
		else DrawFieldsForType(choice);
        EditorGUILayout.EndVertical();
	}

	static void DrawFieldsForType (object obj, int depth = 0) {
		if(depth > 2) return;
		var indent = EditorGUI.indentLevel;
		var cachedLabelWidth = EditorGUIUtility.labelWidth;
		EditorGUIUtility.labelWidth = labelWidth;
		Type type = obj.GetType();
		if(depth == 0)
        	EditorGUILayout.LabelField(type.Name, EditorStyles.boldLabel);
		EditorGUI.indentLevel = depth+1;
		FieldInfo[] properties = type.GetFields();
		foreach (FieldInfo property in properties) {
			var value = property.GetValue(obj);
			var propertyNameString = property.Name+" ("+property.FieldType+(value != null && property.FieldType!=value.GetType() ? " cast as "+value.GetType().ToString() : "")+")";
			var valueString = value == null ? "NULL" : value.ToString();
			EditorGUILayout.LabelField(propertyNameString, valueString);
			if(value != null) {
				var castType = value.GetType();
				var showChildFields = value != null && !(castType.IsPrimitive || castType == typeof(String) || castType.IsEnum || castType == typeof(Ink.Runtime.Choice));
				if(showChildFields)
					DrawFieldsForType(value, depth+1);
			}
		}
		EditorGUIUtility.labelWidth = cachedLabelWidth;
		EditorGUI.indentLevel = indent;
	}

	void OnUndoRedo () {
		Parse(settings.inkValidatorText, settings.inkValidatorTags);
	}

	void DrawParsers () {
		EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
		EditorGUILayout.LabelField("Content Parsers");
        EditorGUILayout.EndHorizontal();
		EditorGUILayout.BeginVertical(GUI.skin.box);
		foreach(var parser in ScriptContent.parseMethods.OrderBy(x => x.priority)) {
			Type type = parser.parserType;
			var info = type.GetProperty("parserRegex", BindingFlags.NonPublic | BindingFlags.Static);
			object value = info?.GetValue(null);

			// EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			// EditorGUILayout.LabelField(parser.Key);
			// EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			
			EditorGUILayout.LabelField(parser.parserType.Name, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			EditorGUILayout.IntField("Priority", parser.priority);
			EditorGUILayout.TextField("Regex", value == null ? "???" : value.ToString());
			EditorGUI.indentLevel--;

			EditorGUILayout.EndVertical();
		}
		EditorGUILayout.EndVertical();
		
		
		EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
		EditorGUILayout.LabelField("Choice Parsers");
        EditorGUILayout.EndHorizontal();
		EditorGUILayout.BeginVertical(GUI.skin.box);
		foreach(var parser in ScriptChoice.parseMethods.OrderBy(x => x.priority)) {
			Type type = parser.parserType;
			var info = type.GetProperty("parserRegex", BindingFlags.NonPublic | BindingFlags.Static);
			object value = info?.GetValue(null);

			// EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			// EditorGUILayout.LabelField(parser.Key);
			// EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			
			EditorGUILayout.LabelField(parser.parserType.Name, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			EditorGUILayout.IntField("Priority", parser.priority);
			EditorGUILayout.TextField("Regex", value == null ? "???" : value.ToString());
			EditorGUI.indentLevel--;

			EditorGUILayout.EndVertical();
		}
		EditorGUILayout.EndVertical();
	}
}
*/