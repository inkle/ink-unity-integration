using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Ink.UnityIntegration;

[CustomEditor(typeof(BasicInkExample))]
public class BasicInkExampleEditor : Editor {
	public override void OnInspectorGUI () {
		Repaint();
		base.OnInspectorGUI ();
		var realTarget = target as BasicInkExample;
		var story = realTarget.story;
		InkEditorUtils.DrawStoryPropertyField(story, new GUIContent("Story"));
	}
}
