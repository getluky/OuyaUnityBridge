/*
 * Copyright (C) 2012-2013 Goodhustle Studios, Inc.
 * Author: Gordon Luk <goodhustle.com/contact> 
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(OuyaInput))]
public class OuyaInputEditor : Editor {
	
	private const int NUM_CONTROLLERS = 4;
	
	private float defaultStickDeadZone = 0.25f;
	private float defaultTriggerDeadZone = 0.25f;
	
	public override void OnInspectorGUI() {
		
		OuyaInput input = target as OuyaInput;
		
		defaultStickDeadZone = EditorGUILayout.FloatField("Default Stick Dead Zone", defaultStickDeadZone);
		defaultTriggerDeadZone = EditorGUILayout.FloatField("Default Trigger Dead Zone", defaultTriggerDeadZone);
		
		EditorGUILayout.LabelField("The above settings only take effect once you Reset to Defaults below.");
		EditorGUILayout.Separator();
		if (GUILayout.Button("Reset to Defaults")) {
			// 4 controllers max for OUYA
			// number of emulated button slots
			input.emulatedControllers = new OuyaInputMapping.Controller[NUM_CONTROLLERS];
			
			for (int i=0; i<NUM_CONTROLLERS; i++) {
				
				OuyaInputMapping.Axis[] emulatedAxes = new OuyaInputMapping.Axis[6];
				// Need space for additional "any joystick" mappings
				OuyaInputMapping.Key[] emulatedKeys = new OuyaInputMapping.Key[16];
				
				string unityJoyPrefix = (i + 1).ToString();
				string playerPrefix = "Joy" + unityJoyPrefix + " ";
				
				int j=0; // current axis
				int k=0; // current key
				
				emulatedAxes[j++] = new OuyaInputMapping.Axis(playerPrefix + "Horizontal", playerPrefix + "Axis 1", OuyaAxis.AXIS_LSTICK_X, false, defaultStickDeadZone, i);
				emulatedAxes[j++] = new OuyaInputMapping.Axis(playerPrefix + "Vertical", playerPrefix + "Axis 2", OuyaAxis.AXIS_LSTICK_Y, true, defaultStickDeadZone, i);
				emulatedAxes[j++] = new OuyaInputMapping.Axis(playerPrefix + "RightHorizontal", playerPrefix + "Axis 3", OuyaAxis.AXIS_RSTICK_X, false, defaultStickDeadZone, i);
				emulatedAxes[j++] = new OuyaInputMapping.Axis(playerPrefix + "RightVertical", playerPrefix + "Axis 4", OuyaAxis.AXIS_RSTICK_Y, true, defaultStickDeadZone, i);
				emulatedAxes[j++] = new OuyaInputMapping.Axis(playerPrefix + "LeftTrigger", playerPrefix + "Axis 5", OuyaAxis.AXIS_LTRIGGER, false, defaultTriggerDeadZone, i);
				emulatedAxes[j++] = new OuyaInputMapping.Axis(playerPrefix + "RightTrigger", playerPrefix + "Axis 6", OuyaAxis.AXIS_RTRIGGER, false, defaultTriggerDeadZone, i);

				emulatedKeys[k++] = new OuyaInputMapping.Key(playerPrefix + "Fire1", OuyaKey.BUTTON_O, "Joystick" + unityJoyPrefix + "Button0", KeyCode.Joystick1Button0, i);
				emulatedKeys[k++] = new OuyaInputMapping.Key(playerPrefix + "Jump", OuyaKey.BUTTON_A, "Joystick" + unityJoyPrefix + "Button1", KeyCode.Joystick1Button1, i);
				emulatedKeys[k++] = new OuyaInputMapping.Key(playerPrefix + "Fire2", OuyaKey.BUTTON_Y, "Joystick" + unityJoyPrefix + "Button2", KeyCode.Joystick1Button2, i);
				emulatedKeys[k++] = new OuyaInputMapping.Key(playerPrefix + "Fire3", OuyaKey.BUTTON_U, "Joystick" + unityJoyPrefix + "Button3", KeyCode.Joystick1Button3, i);
				emulatedKeys[k++] = new OuyaInputMapping.Key(playerPrefix + "LeftBumper", OuyaKey.BUTTON_LB, "Joystick" + unityJoyPrefix + "Button4", KeyCode.Joystick1Button4, i);
				emulatedKeys[k++] = new OuyaInputMapping.Key(playerPrefix + "RightBumper", OuyaKey.BUTTON_RB, "Joystick" + unityJoyPrefix + "Button5", KeyCode.Joystick1Button5, i);
				emulatedKeys[k++] = new OuyaInputMapping.Key(playerPrefix + "LeftTrigger", OuyaKey.BUTTON_LT, "Joystick" + unityJoyPrefix + "Button6", KeyCode.Joystick1Button6, i);
				emulatedKeys[k++] = new OuyaInputMapping.Key(playerPrefix + "RightTrigger", OuyaKey.BUTTON_RT, "Joystick" + unityJoyPrefix + "Button7", KeyCode.Joystick1Button7, i);
				emulatedKeys[k++] = new OuyaInputMapping.Key(playerPrefix + "LeftStick", OuyaKey.BUTTON_L3, "Joystick" + unityJoyPrefix + "Button8", KeyCode.Joystick1Button8, i);
				emulatedKeys[k++] = new OuyaInputMapping.Key(playerPrefix + "RightStick", OuyaKey.BUTTON_R3, "Joystick" + unityJoyPrefix + "Button9", KeyCode.Joystick1Button9, i);
				emulatedKeys[k++] = new OuyaInputMapping.Key(playerPrefix + "DPadCenter", OuyaKey.BUTTON_DPAD_CENTER, "Joystick" + unityJoyPrefix + "Button10", KeyCode.Joystick1Button10, i);
				emulatedKeys[k++] = new OuyaInputMapping.Key(playerPrefix + "DPadDown", OuyaKey.BUTTON_DPAD_DOWN, "Joystick" + unityJoyPrefix + "Button11", KeyCode.Joystick1Button11, i);
				emulatedKeys[k++] = new OuyaInputMapping.Key(playerPrefix + "DPadLeft", OuyaKey.BUTTON_DPAD_LEFT, "Joystick" + unityJoyPrefix + "Button12", KeyCode.Joystick1Button12, i);
				emulatedKeys[k++] = new OuyaInputMapping.Key(playerPrefix + "DPadRight", OuyaKey.BUTTON_DPAD_RIGHT, "Joystick" + unityJoyPrefix + "Button13", KeyCode.Joystick1Button13, i);
				emulatedKeys[k++] = new OuyaInputMapping.Key(playerPrefix + "DPadUp", OuyaKey.BUTTON_DPAD_UP, "Joystick" + unityJoyPrefix + "Button14", KeyCode.Joystick1Button14, i);
				emulatedKeys[k++] = new OuyaInputMapping.Key(playerPrefix + "System", OuyaKey.BUTTON_SYSTEM, "Joystick" + unityJoyPrefix + "Button15", KeyCode.Joystick1Button15, i);
				
				input.emulatedControllers[i] = new OuyaInputMapping.Controller();
				input.emulatedControllers[i].playerNum = i;
				input.emulatedControllers[i].axes = emulatedAxes;
				input.emulatedControllers[i].keys = emulatedKeys;
			}
			
			
		}
		EditorUtility.SetDirty(target);
		
		DrawDefaultInspector();
	}
}
