using UnityEngine;
using System.Collections;

public class ShowInput : MonoBehaviour {
	
	void Start() {
		
	}
	

	void OnGUI() {
		GUILayout.BeginArea(new Rect(0, 0, Screen.width/2, Screen.height));
		for (int i=0; i<OuyaBridge.devices.Length; i++) {
			GUILayout.Label("Time since launch: " + Time.realtimeSinceStartup);
			// About the 1-indexing instead of 0-indexing: this is Unity Input's standard for keycodes, so I followed it.
			
			GUILayout.Label("O Button: " + OuyaInput.GetButton(string.Format("Joy{0} Fire1", i+1))); // Using Virtual Button
			GUILayout.Label("U Button: " + OuyaInput.GetButton(string.Format("Joy{0} Fire3", i+1)));
			GUILayout.Label("Y Button: " + OuyaInput.GetButton(string.Format("Joy{0} Fire2", i+1)));
			GUILayout.Label("A Button: " + OuyaInput.GetButton(string.Format("Joy{0} Jump", i+1)));
			
			GUILayout.Label("LB Button: " + OuyaInput.GetButton(string.Format("Joy{0} LeftBumper", i+1)));
			GUILayout.Label("RB Button: " + OuyaInput.GetButton(string.Format("Joy{0} RightBumper", i+1)));
			GUILayout.Label("LT Button: " + OuyaInput.GetButton(string.Format("Joy{0} LeftTrigger", i+1)));
			GUILayout.Label("RT Button: " + OuyaInput.GetButton(string.Format("Joy{0} RightTrigger", i+1)));
			// Example of GetKey by name
			GUILayout.Label("L3 Button: " + OuyaInput.GetKey(string.Format("Joystick{0}Button8", i+1)) + "(using GetKey(Key Name))");
			
			GUILayout.Label("R3 Button: " + OuyaInput.GetButton(string.Format("Joy{0} RightStick", i+1)));
			
			GUILayout.Label("DPadCenter Button: " + OuyaInput.GetButton(string.Format("Joy{0} DPadCenter", i+1)));
			GUILayout.Label("DPadUp Button: " + OuyaInput.GetButton(string.Format("Joy{0} DPadUp", i+1)));
			GUILayout.Label("DPadDown Button: " + OuyaInput.GetButton(string.Format("Joy{0} DPadDown", i+1)));
			GUILayout.Label("DPadLeft Button: " + OuyaInput.GetButton(string.Format("Joy{0} DPadLeft", i+1)));
			// Example of GetKey by keycode - press dpad right to test
			KeyCode keyCode = KeyCode.Joystick1Button13;
			switch (i) {
			case 0:
			default:
				break;
			case 1:
				keyCode = KeyCode.Joystick2Button13;
				break;
			case 2:
				keyCode = KeyCode.Joystick3Button13;
				break;
			case 3:
				keyCode = KeyCode.Joystick4Button13;
				break;
			}
			GUILayout.Label("DPadRight Button: " + OuyaInput.GetKey(keyCode) + " (using GetKey(KeyCode))");
			
			GUILayout.Label("System Button: " + OuyaInput.GetButton(string.Format("Joy{0} System", i+1)));
			
			
			GUILayout.Label("Left Stick: " + OuyaInput.GetAxis("Joy" + (i+1) + " Horizontal") + "x" + OuyaInput.GetAxis("Joy" + (i+1) + " Vertical"));
			GUILayout.Label("Right Stick: " + OuyaInput.GetAxis("Joy" + (i+1) + " RightHorizontal") + "x" + OuyaInput.GetAxis("Joy" + (i+1) + " RightVertical"));
			
			GUILayout.Label("Left Trigger: " + OuyaInput.GetAxis("Joy" + (i+1) + " LeftTrigger"));
			GUILayout.Label("Right Trigger: " + OuyaInput.GetAxis("Joy" + (i+1) + " RightTrigger"));
		}
		GUILayout.EndArea();
		
		
	}
}
