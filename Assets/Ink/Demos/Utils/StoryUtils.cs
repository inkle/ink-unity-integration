using Ink.Runtime;
using UnityEngine;
using System.Linq;

// Bit of a grab bag of useful story related functions. 
public static class StoryUtils {
    

	public static int[] GetValuesFromInkListVariable(InkList listVar) {
		if (listVar == null) return new int[0];
		var ids = new int[listVar.Count];

		int i = 0;
		foreach (var listItem in listVar) {
			ids[i] = (listItem.Value);
			i++;
		}
		return ids;
	}

	public static string[] GetKeysFromInkList(InkList listVar) {
		if(listVar == null) return new string[0];
		var ids = new string[listVar.Count];

		int i = 0;
		foreach(var listItem in listVar) {
			ids[i] = (listItem.Key.itemName);
			i++;
		}
		return ids;
	}
}