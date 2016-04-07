using UnityEngine;
using UnityEditor;

namespace Ink.UnityIntegration {
	public abstract class ObjectInspector {
		public Object target;
		public SerializedObject serializedObject;

		public abstract bool IsValid(string assetPath);
		public virtual void OnEnable () {}
		public virtual void OnDisable () {}
		public virtual void OnHeaderGUI () {}
		public virtual void OnInspectorGUI() {}
	}
}