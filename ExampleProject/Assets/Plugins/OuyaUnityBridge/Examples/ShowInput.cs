using UnityEngine;
using System.Collections;

public class ShowInput : MonoBehaviour {
	
	void Start() {
		
	}
	

	void OnGUI() {
		GUILayout.BeginArea(new Rect(0, 0, Screen.width/2, Screen.height));
		
		GUILayout.Label("Time since launch: " + Time.realtimeSinceStartup);
		
		GUILayout.Label("O Button: " + OuyaInput.GetKey(KeyCode.Joystick1Button0));
		GUILayout.Label("U Button: " + OuyaInput.GetKey(KeyCode.Joystick1Button3));
		GUILayout.Label("Y Button: " + OuyaInput.GetKey(KeyCode.Joystick1Button2));
		GUILayout.Label("A Button: " + OuyaInput.GetKey(KeyCode.Joystick1Button1));
		
		GUILayout.Label("LB Button: " + OuyaInput.GetKey(KeyCode.Joystick1Button4));
		GUILayout.Label("RB Button: " + OuyaInput.GetKey(KeyCode.Joystick1Button5));
		GUILayout.Label("LT Button: " + OuyaInput.GetKey(KeyCode.Joystick1Button6));
		GUILayout.Label("RT Button: " + OuyaInput.GetKey(KeyCode.Joystick1Button7));
		
		GUILayout.Label("L3 Button: " + OuyaInput.GetKey(KeyCode.Joystick1Button8));
		GUILayout.Label("R3 Button: " + OuyaInput.GetKey(KeyCode.Joystick1Button9));
		
		GUILayout.Label("DPadCenter Button: " + OuyaInput.GetKey(KeyCode.Joystick1Button10));
		GUILayout.Label("DPadUp Button: " + OuyaInput.GetKey(KeyCode.Joystick1Button14));
		GUILayout.Label("DPadDown Button: " + OuyaInput.GetKey(KeyCode.Joystick1Button11));
		GUILayout.Label("DPadLeft Button: " + OuyaInput.GetKey(KeyCode.Joystick1Button12));
		GUILayout.Label("DPadRight Button: " + OuyaInput.GetKey(KeyCode.Joystick1Button13));
		
		GUILayout.Label("System Button: " + OuyaInput.GetKey(KeyCode.Joystick1Button15));
		
		
		GUILayout.Label("Left Stick: " + OuyaInput.GetAxis("Joy1 Horizontal") + "x" + OuyaInput.GetAxis("Joy1 Vertical"));
		GUILayout.Label("Right Stick: " + OuyaInput.GetAxis("Joy1 RightHorizontal") + "x" + OuyaInput.GetAxis("Joy1 RightVertical"));
		
		GUILayout.Label("Left Trigger: " + OuyaInput.GetAxis("Joy1 LeftTrigger"));
		GUILayout.Label("Right Trigger: " + OuyaInput.GetAxis("Joy1 RightTrigger"));
		
		GUILayout.EndArea();
		
		
	}
}
