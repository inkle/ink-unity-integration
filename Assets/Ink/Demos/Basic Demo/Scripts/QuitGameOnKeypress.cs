using UnityEngine;
using System.Collections;

public class QuitGameOnKeypress : MonoBehaviour {
	
	public KeyCode key = KeyCode.Escape;
	
#pragma warning disable IDE0051 // Remove unused private members
	void Update () {
#pragma warning restore IDE0051 // Remove unused private members
		if(Input.GetKeyDown(key)) Application.Quit();
	}
}