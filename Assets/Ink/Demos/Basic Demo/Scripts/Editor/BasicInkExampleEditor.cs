using Ink.Runtime;
using Ink.UnityIntegration;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BasicInkExample))]
[InitializeOnLoad]
public class BasicInkExampleEditor : Editor {
    static bool storyExpanded;
    static BasicInkExampleEditor () {
        BasicInkExample.OnCreateStory += OnCreateStory;
    }

    static void OnCreateStory (Story story) {
        // If you'd like NOT to automatically show the window and attach (your teammates may appreciate it!) then replace "true" with "false" here. 
        InkPlayerWindow window = InkPlayerWindow.GetWindow(true);
        if(window != null) InkPlayerWindow.Attach(story);
    }
	public override void OnInspectorGUI () {
		Repaint();
		base.OnInspectorGUI ();
		var realTarget = target as BasicInkExample;
		var story = realTarget.story;
		InkPlayerWindow.DrawStoryPropertyField(story, ref storyExpanded, new GUIContent("Story"));
	}
}
