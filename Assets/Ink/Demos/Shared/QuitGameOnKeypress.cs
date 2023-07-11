using UnityEngine;

public class QuitGameOnKeypress : MonoBehaviour {
	
	public KeyCode key = KeyCode.Escape;
	
	void Update () {
		if(Input.GetKeyDown(key)) Application.Quit();
	}
}