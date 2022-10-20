using Ink.Runtime;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

[System.Serializable]
[CustomPropertyDrawer(typeof (InkListChangeHandler))]
public class InkListChangeHandlerDrawer : PropertyDrawer {
    bool initialized;
    ReorderableList currentItemsList;
    
    public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
		EditorGUI.BeginProperty (position, label, property);
		var y = position.y;
		property.isExpanded = EditorGUI.Foldout(new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight), property.isExpanded, property.displayName, true);
		EditorGUI.PropertyField(new Rect(position.x+EditorGUIUtility.labelWidth, y, position.width-EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight), property.FindPropertyRelative("_variableName"), GUIContent.none);
		if(property.isExpanded) {
			EditorGUI.indentLevel++;
			var indentedPosition = EditorGUI.IndentedRect(position);
			
			if (!initialized) Initialize(property);
			
			y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			
			EditorGUI.BeginDisabledGroup(true);
			EditorGUI.PropertyField(new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight), property.FindPropertyRelative("observing"));
			y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			
			currentItemsList.DoList(new Rect(indentedPosition.x, y, indentedPosition.width, currentItemsList.GetHeight()));
			EditorGUI.EndDisabledGroup();

			EditorGUI.indentLevel--;
		}
		EditorGUI.EndProperty ();
    }
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
		if(property.isExpanded) {
			if (!initialized) Initialize(property);
			return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2 + currentItemsList.GetHeight();
		} else {
			return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		}
    }

    void Initialize (SerializedProperty property) {
        var list = GetValueFromObject<List<InkListItem>>(property.serializedObject.targetObject, property.propertyPath+"._currentListItems");
		if(list == null) list = new List<InkListItem>();
		currentItemsList = new ReorderableList(list, typeof(List<InkListItem>), false, true, false, false) {
            drawElementCallback = (Rect r, int i, bool isActive, bool isFocused) => {
                r.yMin += 1;
                r.yMax -= 2;
				EditorGUI.TextField(r, list[i].fullName);
            },
            drawHeaderCallback = (Rect r) => GUI.Label(r, "Current List Items")
        };
        initialized = true;
    }

    static T GetValueFromObject<T>(object obj, string propertyPath) {
		Debug.Assert(obj != null);
        MemberInfo memberInfo = null;
		string[] parts = propertyPath.Split('.');
		int partIndex = -1;
		foreach (string part in parts) {
			partIndex++;
			if(obj is T) return (T)obj;
			memberInfo = obj.GetType().GetMember(part, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.FlattenHierarchy|BindingFlags.Instance).FirstOrDefault();
			if(memberInfo == null)continue;
			object x = null;
			if(memberInfo is FieldInfo) x = ((FieldInfo)memberInfo).GetValue(obj);
			if(memberInfo is PropertyInfo) x = ((PropertyInfo)memberInfo).GetValue(obj, null);			
			if (x is IList) {
				if(partIndex == parts.Length) {
				} else if((partIndex+2) < parts.Length) {
					int indexStart = parts[partIndex+2].IndexOf("[")+1;
					int collectionElementIndex = Int32.Parse(parts[partIndex+2].Substring(indexStart, parts[partIndex+2].Length-indexStart-1));
					IList list = x as IList;
					obj = (x as IList)[collectionElementIndex];
					continue;
				} else {

				}
			}

			if(memberInfo is FieldInfo) obj = ((FieldInfo)memberInfo).GetValue(obj);
			if(memberInfo is PropertyInfo) obj = ((PropertyInfo)memberInfo).GetValue(obj, null);
		}
			
		if(!(obj is T)) return default(T);
		return (T)obj;
	}
}