using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using UnityEditorInternal;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;
using Ink.Runtime;

/// <summary>
/// Rebuilds the ink meta library whenever scripts compile, as I was finding that you can't rely on the meta library to serialize and deserialize object references reliably.
/// </summary>
namespace Ink.UnityIntegration {


	/*
	[InitializeOnLoad]
	public static class InkLibraryRebuilder {
		static InkLibraryRebuilder () {
			EditorApplication.update += AfterCompile;
    	}

    	static void AfterCompile () {
    		Debug.Log("LAUNCH");
			EditorApplication.update -= AfterCompile;
			InkMetaLibrary.Instance.metaLibrary.Clear();
			InkMetaLibrary.RebuildInkFileConnections();
//			InkMetaLibrary.Rebuild();
    	}
	

		[MenuItem("Assets/Clear", false, 120)]
		public static void CreateNewInkFile () {
			foreach (InkFile inkFile in InkLibrary.Instance.inkLibrary) {
				inkFile.metaInfo = new InkMetaFile(inkFile);
			}
		}
		[MenuItem("Assets/Rebuild", false, 120)]
		public static void Rebuild () {
			InkMetaLibrary.RebuildInkFileConnections();
		}
	}
	*/
}