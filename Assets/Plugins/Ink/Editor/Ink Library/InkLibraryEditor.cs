using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;

namespace Ink.UnityIntegration {

	[CustomEditor(typeof(InkLibrary))]
	public class InkLibraryEditor : Editor {
		private bool debugLibrary = false;
		private ReorderableList list;

		#pragma warning disable
		protected InkLibrary data;
		
		public void OnEnable() {
			data = (InkLibrary) target;
			list = new ReorderableList(data.inkLibrary, typeof(InkFile), false, true, false, false);
//			list.elementHeight = 60;
			list.drawHeaderCallback = (Rect rect) => {  
    			EditorGUI.LabelField(rect, "Ink Library");
			};

			list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
				InkFile inkFile = ((List<InkFile>)list.list)[index];
				Rect objectFieldRect = new Rect(rect.x, rect.y, rect.width - 80, rect.height-4);
				if(!inkFile.isMaster) {
					objectFieldRect.x += 14;
					objectFieldRect.width -= 14;
				}
				Rect selectRect = new Rect(objectFieldRect.xMax, rect.y, 80, rect.height-4);
//				Rect titleRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
//				Rect inkAssetRect = new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * 1, rect.width, EditorGUIUtility.singleLineHeight);
//				Rect jsonAssetRect = new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * 2, rect.width, EditorGUIUtility.singleLineHeight);
//
//				inkFile.compileAutomatically = EditorGUI.Toggle(titleRect, "Compile Automatically", inkFile.compileAutomatically);
				EditorGUI.BeginDisabledGroup(true);
				inkFile.inkAsset = EditorGUI.ObjectField(objectFieldRect, inkFile.inkAsset, typeof(DefaultAsset)) as DefaultAsset;
				EditorGUI.EndDisabledGroup();
				if(GUI.Button(selectRect, "Select")) {
					Selection.activeObject = inkFile.inkAsset;
				}
//				inkFile.jsonAsset = EditorGUI.ObjectField(jsonAssetRect, new GUIContent("JSON File"), inkFile.jsonAsset, typeof(TextAsset)) as TextAsset;
			};
		}
		
		public override void OnInspectorGUI() {
			serializedObject.Update();
			data.compileAutomatically = EditorGUILayout.Toggle(new GUIContent("Compile Ink Automatically", "When disabled, automatic compilation can be enabled on a per-story basis via the inspector for a master story file."), data.compileAutomatically);
			data.handleJSONFilesAutomatically = EditorGUILayout.Toggle(new GUIContent("Handle JSON Automatically", "Whether JSON files are moved, renamed and deleted along with their ink files."), data.handleJSONFilesAutomatically);
			debugLibrary = EditorGUILayout.Toggle(new GUIContent("Show Library (Debugging)", "Hidden by default, you can view the ink library for debugging purposes. Be wary if editing!"), debugLibrary);
			if(debugLibrary) {
//				base.OnInspectorGUI();
				EditorGUILayout.PropertyField(serializedObject.FindProperty("inkLibrary"), true);
				EditorGUILayout.PropertyField(serializedObject.FindProperty("compilationStack"), true);
			}
			if(GUI.changed && target != null)         
				EditorUtility.SetDirty(target);
			serializedObject.ApplyModifiedProperties();
	    }
	}
}