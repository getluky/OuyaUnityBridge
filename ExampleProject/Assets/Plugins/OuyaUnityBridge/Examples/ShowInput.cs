using UnityEngine;
using System.Collections;

public class ShowInput : MonoBehaviour {
	
	bool isOnOuyaHardware = false;
	int odkVersionNumber = 0;
	
	float okWidth = 1280f;
	float okHeight = 720f;
	
	IEnumerator Start() {
		
		odkVersionNumber = OuyaBridge.GetOdkVersionNumber();
		
		okWidth = Screen.width*0.9f;
		okHeight = Screen.height*0.9f;
		yield return new WaitForSeconds(1.0f);
		isOnOuyaHardware = OuyaBridge.IsRunningOnOuyaHardware();
	}
	

	void OnGUI() {
		
		for (int i=0; i<OuyaBridge.devices.Length; i++) {
			int playerNum = OuyaBridge.devices[i].player;
			GUILayout.BeginArea(new Rect(Screen.width*0.05f + playerNum * okWidth/4, Screen.height*0.05f + 400, okWidth/2, okHeight));
			
			GUILayout.Label("Frame #: " + Time.frameCount);
			/* This appears to not be working so I'm disabling it. */
			/*
			if (isOnOuyaHardware) {
				GUILayout.Label ("RUNNING ON OUYA HARDWARE");
			} else {
				GUILayout.Label ("NOT RUNNING ON OUYA HARDWARE");
			}*/
			GUILayout.Label ("SDK Ver " + odkVersionNumber);
			GUILayout.Label ("Player Number: " + playerNum);
			// About the 1-indexing instead of 0-indexing: this is Unity Input's standard for keycodes, so I followed it.
			
			GUILayout.Label("O Button: " + OuyaInput.GetButton(string.Format("Joy{0} Fire1", playerNum+1))); // Using Virtual Button
			GUILayout.Label("U Button: " + OuyaInput.GetButton(string.Format("Joy{0} Fire3", playerNum+1)));
			GUILayout.Label("Y Button: " + OuyaInput.GetButton(string.Format("Joy{0} Fire2", playerNum+1)));
			GUILayout.Label("A Button: " + OuyaInput.GetButton(string.Format("Joy{0} Jump", playerNum+1)));
			
			GUILayout.Label("LB Button: " + OuyaInput.GetButton(string.Format("Joy{0} LeftBumper", playerNum+1)));
			GUILayout.Label("RB Button: " + OuyaInput.GetButton(string.Format("Joy{0} RightBumper", playerNum+1)));
			GUILayout.Label("LT Button: " + OuyaInput.GetButton(string.Format("Joy{0} LeftTrigger", playerNum+1)));
			GUILayout.Label("RT Button: " + OuyaInput.GetButton(string.Format("Joy{0} RightTrigger", playerNum+1)));
			// Example of GetKey by name
			GUILayout.Label("L3 Button: " + OuyaInput.GetKey(string.Format("Joystick{0}Button8", playerNum+1)) + "(using GetKey(Key Name))");
			
			GUILayout.Label("R3 Button: " + OuyaInput.GetButton(string.Format("Joy{0} RightStick", playerNum+1)));
			
			GUILayout.Label("DPadCenter Button: " + OuyaInput.GetButton(string.Format("Joy{0} DPadCenter", playerNum+1)));
			GUILayout.Label("DPadUp Button: " + OuyaInput.GetButton(string.Format("Joy{0} DPadUp", playerNum+1)));
			GUILayout.Label("DPadDown Button: " + OuyaInput.GetButton(string.Format("Joy{0} DPadDown", playerNum+1)));
			GUILayout.Label("DPadLeft Button: " + OuyaInput.GetButton(string.Format("Joy{0} DPadLeft", playerNum+1)));
			// Example of GetKey by keycode - press dpad right to test
			KeyCode keyCode = KeyCode.Joystick1Button13;
			switch (playerNum) {
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
			
			//GUILayout.Label("System Button: " + OuyaInput.GetButton(string.Format("Joy{0} System", playerNum+1)));
			
			
			GUILayout.Label("Left Stick: " + OuyaInput.GetAxis("Joy" + (playerNum+1) + " Horizontal") + "x" + OuyaInput.GetAxis("Joy" + (playerNum+1) + " Vertical"));
			GUILayout.Label("Right Stick: " + OuyaInput.GetAxis("Joy" + (playerNum+1) + " RightHorizontal") + "x" + OuyaInput.GetAxis("Joy" + (playerNum+1) + " RightVertical"));
			
			GUILayout.Label("Left Trigger: " + OuyaInput.GetAxis("Joy" + (playerNum+1) + " LeftTrigger"));
			GUILayout.Label("Right Trigger: " + OuyaInput.GetAxis("Joy" + (playerNum+1) + " RightTrigger"));
			GUILayout.EndArea();
		}
		
		
		
	}
}
