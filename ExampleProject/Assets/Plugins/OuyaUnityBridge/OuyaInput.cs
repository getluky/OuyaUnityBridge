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
/*
 * About:
 * 	This is an unofficial Unity-Ouya bridge that provides an abstraction mapped to Unity-standard Input design
 *  so that experienced developers with large codebases can jump right in instead of rewriting for event-based input.
 *  It does not have all the bells and whistles of the official Ouya Unity plugin, so PLEASE only use this if you know what
 *  you're doing and need it.
 *
 *  On the device, it uses the OuyaController ouya-sdk API to monitor and update controller input, so
 *  any controller supported by OuyaController API should work. In the editor, it simply falls back to regular
 *  Unity input.
 * 
 *  Again, if you can't get this to work, you should probably use the official OUYA Unity plugin instead.
 * 
 *  To report issues or submit pull requests, visit the Github page at:
 * 
 * 
 * Usage:
 *  To use this class:
 * 		* Import the package into your project
 * 		* Edit Plugins/OuyaBridge/OuyaBridge.cs, Plugins/Android/src/ApplicationManifest.xml, and Plugins/Android/src/OuyaUnityActivity.java with
 * 			your developer id.
 * 		* Editor Plugins/Android/src/OuyaUnityActivity and add your product ids and any shared secrets.
 * 		* In Unity, drag the OuyaBridge GameObject into the first scene in your game.
 * 		* Adjust default deadzones as required in OuyaInput component settings
 * 		* Click "Reset to Defaults" to define new virtual emulated keys, buttons, and axes. Modify as desired.
 * 		* *** Make sure OuyaInput is at the TOP of Edit > Project Settings > Script Execution Order. ***
 * 		* Add "UNITY_OUYA" to the list of Build Settings > Other Settings > Scripting Define Symbols in your Android build
 * 		* Open Window > Ouya Panel, and set up java paths correctly.
 *		* Run Compile within the Ouya Panel. Check the console for errors.
 * 		* To test with non-OUYA controllers (or even your keyboard) in the editor, open the Unity Input manager and set up
 * 			virtual inputs corresponding to the same names as used in the OuyaInput emulated controller definitions.
 * 		* Edit your existing code, and replace Input.<function> calls with OuyaInput.<function> for the functions listed below.
 *  
 *  You may substitute OuyaInput for regular Unity Input calls for the following functions, and it will passthru to standard input in the editor:
 * 		* GetButton, GetButtonUp, GetButtonDown
 * 		* GetKey, GetKeyUp, GetKeyDown (both string and keycode-based)
 * 		* GetAxis
 * 		* anyKey
 * 		* anyKeyDown
 *  
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// For use in configuring virtual inputs
/// </summary>
public enum OuyaKey
{
    NONE = -1,
    BUTTON_O,
    BUTTON_U,
    BUTTON_Y,
    BUTTON_A,
    BUTTON_LB,
    BUTTON_LT,
    BUTTON_RB,
    BUTTON_RT,
    BUTTON_L3,
    BUTTON_R3,
    BUTTON_SYSTEM,
    AXIS_LSTICK_X,
    AXIS_LSTICK_Y,
    AXIS_RSTICK_X,
    AXIS_RSTICK_Y,
    BUTTON_DPAD_UP,
    BUTTON_DPAD_RIGHT,
    BUTTON_DPAD_DOWN,
    BUTTON_DPAD_LEFT,
    BUTTON_DPAD_CENTER
}
/// <summary>
/// For use in configuring virtual inputs.
/// </summary>
public enum OuyaAxis
{
    NONE = -1,
    AXIS_LSTICK_X,
    AXIS_LSTICK_Y,
    AXIS_RSTICK_X,
    AXIS_RSTICK_Y,
    AXIS_LTRIGGER,
    AXIS_RTRIGGER
}

[System.Serializable]
public class OuyaInputMapping {
	[System.Serializable]
	public class Axis {
		public string virtualAxisName;
		public string virtualAxisName2;
		public OuyaAxis ouyaAxis;
		[HideInInspector]
		public float value = 0;
		public bool invert;
		public float deadZone = 0.1f;
		public int playerNum = 0;
		
		public Axis(string virtualAxisName, string virtualAxisName2, OuyaAxis ouyaAxis, bool invert, float deadZone, int playerNum) {
			this.virtualAxisName = virtualAxisName;
			this.virtualAxisName2 = virtualAxisName2;
			this.ouyaAxis = ouyaAxis;
			this.invert = invert;
			this.deadZone = deadZone;
			this.playerNum = playerNum;
		}
	}
	[System.Serializable]
	public class Key {
		public string virtualButtonName;
		public OuyaKey ouyaKey;
		public string keyName;
		public KeyCode keyCode;
		[HideInInspector]
		public bool down = false;
		[HideInInspector]
		public int eventFrame = 0;
		[HideInInspector]
		public bool downThisFrame = false;
		[HideInInspector]
		public bool upThisFrame = false;
		public int playerNum = 0;
		
		public Key(string virtualButtonName, OuyaKey ouyaKey, string keyName, KeyCode keyCode, int playerNum) {
			this.virtualButtonName = virtualButtonName;
			this.ouyaKey = ouyaKey;
			this.keyName = keyName;
			this.keyCode = keyCode;
			this.playerNum = playerNum;
		}
	}
	
	[System.Serializable]
	public class Controller {
		public int playerNum;
		public Axis[] axes;
		public Key[] keys;
	}
}

[RequireComponent(typeof(OuyaBridge))]
public class OuyaInput : MonoBehaviour {
	
	

	public OuyaInputMapping.Controller[] emulatedControllers;
	
	private static OuyaInput _instance = null;
	public static OuyaInput Instance {
		get {
			if (_instance == null) {
				var go = new GameObject("OuyaInput");
				_instance = go.AddComponent<OuyaInput>();
				_instance.Init();
			}
			return _instance;
		}
	}
	
	
	
	public string virtualHorizontalAxis = "Joy1 Horizontal";
	public string virtualVerticalAxis = "Joy1 Vertical";
	
	
#if UNITY_OUYA
	
	// Fast lookup dictionaries
	
	private Dictionary<int, Dictionary<OuyaAxis, OuyaInputMapping.Axis>> playerToOuyaAxisMappings = new Dictionary<int, Dictionary<OuyaAxis, OuyaInputMapping.Axis>>();
	private Dictionary<int, Dictionary<OuyaKey, OuyaInputMapping.Key>> playerToOuyaKeyMappings = new Dictionary<int, Dictionary<OuyaKey, OuyaInputMapping.Key>>();
	
	private Dictionary<string, OuyaInputMapping.Axis> virtualAxisNameToEmulatedAxis = new Dictionary<string, OuyaInputMapping.Axis>();
	
	private Dictionary<string, OuyaInputMapping.Key> virtualButtonNameToEmulatedKey = new Dictionary<string, OuyaInputMapping.Key>();
	private Dictionary<string, OuyaInputMapping.Key> keyNameToEmulatedKey = new Dictionary<string, OuyaInputMapping.Key>();
	private Dictionary<KeyCode, OuyaInputMapping.Key> keyCodeToEmulatedKey = new Dictionary<KeyCode, OuyaInputMapping.Key>();
	
	private static int keysDown = 0;
	
	private AndroidJavaClass jc;
	private AndroidJavaObject [] playerStates = null;
	
#endif
	void Awake() {
		if (_instance == null) {
			_instance = this;
			DontDestroyOnLoad(gameObject);
			Init();
		} else {
			Destroy(gameObject);
		}
	}
	
	
	void OnEnable() {
        ClearAllInputs();
	}
	
	void OnDestroy() {
#if UNITY_OUYA && !UNITY_EDITOR
		if (jc != null) jc.Dispose();
		if (playerStates != null) {
			for (int i=0; i<playerStates.Length; i++) {
				playerStates[i].Dispose();
			}
		}
#endif
	}
	
	public static bool anyKey 
	{
		get
		{
#if UNITY_OUYA && !UNITY_EDITOR
			return (keysDown > 0 || Input.anyKey);
#else
			return Input.anyKey;
#endif
				
		}
	}
	
	public static bool anyKeyDown
	{
		get
		{
#if UNITY_OUYA && !UNITY_EDITOR
			for (int p=0,pmax=Instance.emulatedControllers.Length;p<pmax;p++) {
				foreach (OuyaInputMapping.Key key in Instance.emulatedControllers[p].keys)
				{
					if (key.downThisFrame) 
						return true;
				}
			}
			return Input.anyKeyDown;
#else
			return Input.anyKeyDown;
#endif
		}
	}
	
	void Init() {
		Debug.Log("OuyaInput.Init()"); 
		
#if UNITY_OUYA
		if (!Application.isEditor) {
			
			playerStates = new AndroidJavaObject[4]; // Hardcoded, add this to OuyaSDK.NUM_CONTROLLERS or something.
			
			keysDown = 0;
			
			// Clear lookup dictionaries
			playerToOuyaAxisMappings.Clear();
			playerToOuyaKeyMappings.Clear();
			virtualAxisNameToEmulatedAxis.Clear();
			
			virtualButtonNameToEmulatedKey.Clear();
			keyNameToEmulatedKey.Clear();
			keyCodeToEmulatedKey.Clear();
			
			// Populate OUYA-player mappings
			for (int p=0; p<=3; p++) {
				OuyaInputMapping.Controller controller = emulatedControllers[p];
				Dictionary<OuyaAxis, OuyaInputMapping.Axis> axisMappings = new Dictionary<OuyaAxis, OuyaInputMapping.Axis>();
				Dictionary<OuyaKey, OuyaInputMapping.Key> keyMappings = new Dictionary<OuyaKey, OuyaInputMapping.Key>();
				for (int i=0; i<controller.axes.Length; i++) {
					axisMappings.Add(controller.axes[i].ouyaAxis, controller.axes[i]);
				}		
				for (int i=0; i<controller.keys.Length; i++) {
					keyMappings.Add(controller.keys[i].ouyaKey, controller.keys[i]);
				}
				playerToOuyaAxisMappings.Add(p, axisMappings);
				playerToOuyaKeyMappings.Add(p, keyMappings);
				// Populate lookup dictionaries
				for (int i=0; i<controller.axes.Length; i++) {
					// But not if our emulated axis / key names are already cached
					if (!virtualAxisNameToEmulatedAxis.ContainsKey(controller.axes[i].virtualAxisName))
						virtualAxisNameToEmulatedAxis.Add(controller.axes[i].virtualAxisName, controller.axes[i]);
					if (!virtualAxisNameToEmulatedAxis.ContainsKey(controller.axes[i].virtualAxisName2))
						virtualAxisNameToEmulatedAxis.Add(controller.axes[i].virtualAxisName2, controller.axes[i]);
				}
				for (int i=0; i<controller.keys.Length; i++) {
					if (!virtualButtonNameToEmulatedKey.ContainsKey(controller.keys[i].virtualButtonName))
						virtualButtonNameToEmulatedKey.Add(controller.keys[i].virtualButtonName, controller.keys[i]);
					if (!keyNameToEmulatedKey.ContainsKey(controller.keys[i].keyName)) 
						keyNameToEmulatedKey.Add(controller.keys[i].keyName, controller.keys[i]);
					if (!keyCodeToEmulatedKey.ContainsKey(controller.keys[i].keyCode)) 
						keyCodeToEmulatedKey.Add(controller.keys[i].keyCode, controller.keys[i]);
				}
			}
		
		}
#endif
	}
	
	public static void ClearAllInputs() {
		Input.ResetInputAxes();
		for (int p=0,pmax=Instance.emulatedControllers.Length;p<pmax;p++) {
			foreach (OuyaInputMapping.Key emulatedKey in Instance.emulatedControllers[p].keys) 
			{
				emulatedKey.down = false;
				emulatedKey.downThisFrame = false;
				emulatedKey.upThisFrame = false;
			}
			foreach (OuyaInputMapping.Axis emulatedAxis in Instance.emulatedControllers[p].axes) 
			{
				emulatedAxis.value = 0f;
			}
		}
	}
	
	
	protected static string GetAllJoystickString(string specificJoystickKey) {
		// Split by string length
		// Example: Joystick1Button0
		// 0,8 - Joystick
		// 8,1 - #
		// 9,6 - Button
		// 15,1 - #
		return string.Format("JoystickButton{0}", specificJoystickKey.Substring(15,1));
	}
	
	protected static KeyCode GetAllJoystickKeyCode(KeyCode specificJoystickKey) {
		// I don't really want a huge lookup switch here, so we'll jump to string and back.
		string allJoystickString = GetAllJoystickString(specificJoystickKey.ToString());
		return (KeyCode)System.Enum.Parse(typeof(KeyCode), allJoystickString);
	}
	
	
	protected static string GetSpecificJoystickString(string allJoystickString, int joystickNum) {
		// JoystickNum is zero indexed, while the keystrings/codes are 1-indexed.
		return string.Format("Joystick{0}Button{1}", (joystickNum + 1), allJoystickString.Substring(14,1));	
	}
	
	
	protected static KeyCode GetSpecificJoystickKeyCode(KeyCode allJoystickKeyCode, int joystickNum) {
		// Another case of avoiding a giant switch statement.
		return (KeyCode)System.Enum.Parse(typeof(KeyCode), GetSpecificJoystickString(allJoystickKeyCode.ToString(), joystickNum));
	}
	
	/// <summary>
	/// Determines whether this string refers to an "All Joysticks" shortcut.
	/// </summary>
	/// <returns>
	/// <c>true</c> if this instance is all joystick string the specified keyString; otherwise, <c>false</c>.
	/// </returns>
	/// <param name='keyString'>
	/// If set to <c>true</c> key string.
	/// </param>
	protected static bool IsAllJoystickString(string keyString) {
		return (keyString.Substring(8,1) == "B");
	}
	/// <summary>
	/// Determines whether this instance is all joystick key code the specified keyCode. Goes up to JoystickButton15 (max default on ouya)
	/// </summary>
	/// <returns>
	/// <c>true</c> if this instance is all joystick key code the specified keyCode; otherwise, <c>false</c>.
	/// </returns>
	/// <param name='keyCode'>
	/// If set to <c>true</c> key code.
	/// </param>
	protected static bool IsAllJoystickKeyCode(KeyCode keyCode) {
		// Would have to test to see if this is faster than allocating string and testing substr.
		// Simply guessing that it is for now.
		return (keyCode == KeyCode.JoystickButton0 ||
			keyCode == KeyCode.JoystickButton1 ||
			keyCode == KeyCode.JoystickButton2 ||
			keyCode == KeyCode.JoystickButton3 ||
			keyCode == KeyCode.JoystickButton4 ||
			keyCode == KeyCode.JoystickButton5 ||
			keyCode == KeyCode.JoystickButton6 ||
			keyCode == KeyCode.JoystickButton7 ||
			keyCode == KeyCode.JoystickButton8 ||
			keyCode == KeyCode.JoystickButton9 ||
			keyCode == KeyCode.JoystickButton10 ||
			keyCode == KeyCode.JoystickButton11 ||
			keyCode == KeyCode.JoystickButton12 ||
			keyCode == KeyCode.JoystickButton13 ||
			keyCode == KeyCode.JoystickButton14 ||
			keyCode == KeyCode.JoystickButton15);
	}
	
	void SetAxisValue(int playerNum, OuyaAxis axisCode, float axisVal) {
#if UNITY_OUYA && !UNITY_EDITOR	
		// Map input axis to emulated axis
		OuyaInputMapping.Axis emulatedAxis = Instance.playerToOuyaAxisMappings[playerNum][axisCode];
		if (emulatedAxis.invert) 
			emulatedAxis.value = -axisVal;
		else
			emulatedAxis.value = axisVal;
		if (Mathf.Abs(emulatedAxis.value) < emulatedAxis.deadZone) 
			emulatedAxis.value = 0f;	
#endif
	}
				
	void SetButtonValue(int playerNum, OuyaKey keyCode, bool value) {
#if UNITY_OUYA && !UNITY_EDITOR
		OuyaInputMapping.Key emulatedKey = Instance.playerToOuyaKeyMappings[playerNum][keyCode];
	
		if (value) {
			if (!emulatedKey.down) 
			{
				Debug.Log("Handling button down " + keyCode + " " + value);
		
				emulatedKey.eventFrame = Time.frameCount;
				
				emulatedKey.downThisFrame = true;
				keysDown += 1;
			}
		} else {
			if (emulatedKey.down) 
			{
				Debug.Log("Handling button up " + keyCode + " " + value);
		
				emulatedKey.eventFrame = Time.frameCount;
			
				emulatedKey.upThisFrame = true;	
				keysDown -= 1;
			}
		}
		emulatedKey.down = value;
#endif
	}

#if UNITY_OUYA && !UNITY_EDITOR
	/// <summary>
	/// Resets all single-frame input flags. MUST be run FIRST in script order, or 
	/// single-frame input flags will not be read.
	/// </summary>
	void Update () 
	{
		
		for (int p=0,pmax=Instance.emulatedControllers.Length;p<pmax;p++) {
			foreach (OuyaInputMapping.Key emulatedKey in emulatedControllers[p].keys) 
			{
				emulatedKey.downThisFrame = false;
				emulatedKey.upThisFrame = false;
			}
		}
		
		// Now that everything is cleared, recreate virtual input values for next frame
		
		if (!Application.isLoadingLevel) {
			// Only query connected devices
			for (int i=0,imax=OuyaBridge.devices.Length;i<imax;i++) {
				// Players are zero-indexed as well.
				if (jc == null) {
					jc = new AndroidJavaClass(OuyaBridge.JAVA_APP_CLASS);
				}
				if (jc != null) {
					if (playerStates[i] == null) {
						playerStates[i] = jc.CallStatic<AndroidJavaObject>("GetControllerState", i);
					}
					if (playerStates[i] != null) {
						
						SetAxisValue(i, OuyaAxis.AXIS_LSTICK_X, playerStates[i].Get<float>("AxisLSX"));
						SetAxisValue(i, OuyaAxis.AXIS_LSTICK_Y, playerStates[i].Get<float>("AxisLSY"));
						SetAxisValue(i, OuyaAxis.AXIS_RSTICK_X, playerStates[i].Get<float>("AxisRSX"));
						SetAxisValue(i, OuyaAxis.AXIS_RSTICK_Y, playerStates[i].Get<float>("AxisRSY"));
						SetAxisValue(i, OuyaAxis.AXIS_LTRIGGER, playerStates[i].Get<float>("AxisLT"));
						SetAxisValue(i, OuyaAxis.AXIS_RTRIGGER, playerStates[i].Get<float>("AxisRT"));
						
						SetButtonValue(i, OuyaKey.BUTTON_O, playerStates[i].Get<bool>("ButtonO"));
						SetButtonValue(i, OuyaKey.BUTTON_U, playerStates[i].Get<bool>("ButtonU"));
						SetButtonValue(i, OuyaKey.BUTTON_Y, playerStates[i].Get<bool>("ButtonY"));
						SetButtonValue(i, OuyaKey.BUTTON_A, playerStates[i].Get<bool>("ButtonA"));
						
						SetButtonValue(i, OuyaKey.BUTTON_LT, playerStates[i].Get<bool>("ButtonL2"));
						SetButtonValue(i, OuyaKey.BUTTON_LB, playerStates[i].Get<bool>("ButtonL1"));
						SetButtonValue(i, OuyaKey.BUTTON_L3, playerStates[i].Get<bool>("ButtonL3"));
						SetButtonValue(i, OuyaKey.BUTTON_RT, playerStates[i].Get<bool>("ButtonR2"));
						SetButtonValue(i, OuyaKey.BUTTON_RB, playerStates[i].Get<bool>("ButtonR1"));
						SetButtonValue(i, OuyaKey.BUTTON_R3, playerStates[i].Get<bool>("ButtonR3"));
						
						SetButtonValue(i, OuyaKey.BUTTON_DPAD_UP, playerStates[i].Get<bool>("ButtonDPU"));
						SetButtonValue(i, OuyaKey.BUTTON_DPAD_DOWN, playerStates[i].Get<bool>("ButtonDPD"));
						SetButtonValue(i, OuyaKey.BUTTON_DPAD_LEFT, playerStates[i].Get<bool>("ButtonDPL"));
						SetButtonValue(i, OuyaKey.BUTTON_DPAD_RIGHT, playerStates[i].Get<bool>("ButtonDPR"));
					}
				}
			}
		}
	}
#endif	
	
    /// <summary>
    /// Wrap Unity's method
    /// </summary>
    /// <param name="inputName"></param>
    /// <returns></returns>
    public static float GetAxis(string inputName)
    {
#if UNITY_OUYA && !UNITY_EDITOR
		// Special handling for "Horizontal" and "Vertical" inputs
		if (inputName == "Horizontal" && Instance.virtualAxisNameToEmulatedAxis.ContainsKey(Instance.virtualHorizontalAxis)) 
		{
			return Instance.virtualAxisNameToEmulatedAxis[Instance.virtualHorizontalAxis].value;	
		} 
		else if (inputName == "Vertical" && Instance.virtualAxisNameToEmulatedAxis.ContainsKey(Instance.virtualVerticalAxis))
		{	
			return Instance.virtualAxisNameToEmulatedAxis[Instance.virtualVerticalAxis].value;
		} 
		else if (Instance.virtualAxisNameToEmulatedAxis.ContainsKey(inputName))
		{
			return Instance.virtualAxisNameToEmulatedAxis[inputName].value;
		} 
		
		return 0f;
#else
        return Input.GetAxis(inputName);
#endif
    }
	
	/// <summary>
    /// Raw input - calibration not supported!
    /// </summary>
    /// <param name="inputName"></param>
    /// <returns></returns>
    public static float GetAxisRaw(string inputName)
	{
#if UNITY_OUYA && !UNITY_EDITOR
		return GetAxis(inputName);
#else
		return Input.GetAxisRaw(inputName);
#endif
	}

    /// <summary>
    /// Wrap Unity's method
    /// </summary>
    /// <param name="inputName"></param>
    /// <returns></returns>
    public static bool GetButton(string inputName)
    {
#if UNITY_OUYA && !UNITY_EDITOR
		if (Instance.virtualButtonNameToEmulatedKey.ContainsKey(inputName)) 
		{
			return Instance.virtualButtonNameToEmulatedKey[inputName].down;
		}
		return false;
#else
        return Input.GetButton(inputName);
#endif
    }
	
	
    /// <summary>
    /// Wrap Unity's method
    /// </summary>
    /// <param name="inputName"></param>
    /// <returns></returns>
    public static bool GetButtonDown(string inputName)
    {
#if UNITY_OUYA && !UNITY_EDITOR
		if (Instance.virtualButtonNameToEmulatedKey.ContainsKey(inputName)) 
		{
			return Instance.virtualButtonNameToEmulatedKey[inputName].downThisFrame;
		}
		return false;
#else
        return Input.GetButtonDown(inputName);
#endif
    }

    /// <summary>
    /// Wrap Unity's method
    /// </summary>
    /// <param name="inputName"></param>
    /// <returns></returns>
    public static bool GetButtonUp(string inputName)
    {
#if UNITY_OUYA && !UNITY_EDITOR
		if (Instance.virtualButtonNameToEmulatedKey.ContainsKey(inputName)) 
		{
			return Instance.virtualButtonNameToEmulatedKey[inputName].upThisFrame;
		}
		return false;
#else
        return Input.GetButtonUp(inputName);
#endif
    }

    /// <summary>
    /// Wrap Unity's method
    /// </summary>
    /// <param name="inputName"></param>
    /// <returns></returns>
    public static bool GetKey(string inputName)
    {
#if UNITY_OUYA && !UNITY_EDITOR
		if (IsAllJoystickString(inputName)) 
		{
			return (GetKey(GetSpecificJoystickString(inputName, 0)) ||
				GetKey(GetSpecificJoystickString(inputName, 1)) ||
				GetKey(GetSpecificJoystickString(inputName, 2)) ||
				GetKey(GetSpecificJoystickString(inputName, 3)));
		} 
		else if (Instance.keyNameToEmulatedKey.ContainsKey(inputName)) 
		{
			return Instance.keyNameToEmulatedKey[inputName].down;
		}
		return false;
#else
        return Input.GetKey(inputName);
#endif
    }
	
    /// <summary>
    /// Wrap Unity's method
    /// </summary>
    /// <param name="inputName"></param>
    /// <returns></returns>
    public static bool GetKeyDown(string inputName)
    {
#if UNITY_OUYA && !UNITY_EDITOR
		if (IsAllJoystickString(inputName)) 
		{
			return (GetKeyDown(GetSpecificJoystickString(inputName, 0)) ||
				GetKeyDown(GetSpecificJoystickString(inputName, 1)) ||
				GetKeyDown(GetSpecificJoystickString(inputName, 2)) ||
				GetKeyDown(GetSpecificJoystickString(inputName, 3)));
		} 
		else if (Instance.keyNameToEmulatedKey.ContainsKey(inputName)) 
		{
			return Instance.keyNameToEmulatedKey[inputName].downThisFrame;
		}
		return false;
#else
        return Input.GetKeyDown(inputName);
#endif
    }

    /// <summary>
    /// Wrap Unity's method
    /// </summary>
    /// <param name="inputName"></param>
    /// <returns></returns>
    public static bool GetKeyUp(string inputName)
    {
#if UNITY_OUYA && !UNITY_EDITOR
		if (IsAllJoystickString(inputName)) 
		{
			return (GetKeyUp(GetSpecificJoystickString(inputName, 0)) ||
				GetKeyUp(GetSpecificJoystickString(inputName, 1)) ||
				GetKeyUp(GetSpecificJoystickString(inputName, 2)) ||
				GetKeyUp(GetSpecificJoystickString(inputName, 3)));
		} else if (Instance.keyNameToEmulatedKey.ContainsKey(inputName)) 
		{
			return Instance.keyNameToEmulatedKey[inputName].upThisFrame;
		}
		return false;
#else
        return Input.GetKeyUp(inputName);
#endif
    }
	

    /// <summary>
    /// Wrap Unity's method
    /// </summary>
    /// <param name="inputName"></param>
    /// <returns></returns>
    public static bool GetKey(KeyCode inputCode)
    {
#if UNITY_OUYA && !UNITY_EDITOR	
		if (IsAllJoystickKeyCode(inputCode)) 
		{
			return (GetKey(GetSpecificJoystickKeyCode(inputCode, 0)) ||
				GetKey(GetSpecificJoystickKeyCode(inputCode, 1)) ||
				GetKey(GetSpecificJoystickKeyCode(inputCode, 2)) ||
				GetKey(GetSpecificJoystickKeyCode(inputCode, 3)));
		} 
		else if (Instance.keyCodeToEmulatedKey.ContainsKey(inputCode)) 
		{
			return Instance.keyCodeToEmulatedKey[inputCode].down;
		}
		return false;
#else
        return Input.GetKey(inputCode);
#endif
    }		
	
	    /// <summary>
    /// Wrap Unity's method
    /// </summary>
    /// <param name="inputName"></param>
    /// <returns></returns>
    public static bool GetKeyDown(KeyCode inputCode)
    {
#if UNITY_OUYA && !UNITY_EDITOR
		if (IsAllJoystickKeyCode(inputCode)) 
		{
			return (GetKeyDown(GetSpecificJoystickKeyCode(inputCode, 0)) ||
				GetKeyDown(GetSpecificJoystickKeyCode(inputCode, 1)) ||
				GetKeyDown(GetSpecificJoystickKeyCode(inputCode, 2)) ||
				GetKeyDown(GetSpecificJoystickKeyCode(inputCode, 3)));
		} 
		else if (Instance.keyCodeToEmulatedKey.ContainsKey(inputCode)) 
		{
			return Instance.keyCodeToEmulatedKey[inputCode].downThisFrame;
		}
		return false;
#else
        return Input.GetKeyDown(inputCode);
#endif
    }

    /// <summary>
    /// Wrap Unity's method
    /// </summary>
    /// <param name="inputName"></param>
    /// <returns></returns>
    public static bool GetKeyUp(KeyCode inputCode)
    {
#if UNITY_OUYA && !UNITY_EDITOR
		if (IsAllJoystickKeyCode(inputCode)) 
		{
			return (GetKeyUp(GetSpecificJoystickKeyCode(inputCode, 0)) ||
				GetKeyUp(GetSpecificJoystickKeyCode(inputCode, 1)) ||
				GetKeyUp(GetSpecificJoystickKeyCode(inputCode, 2)) ||
				GetKeyUp(GetSpecificJoystickKeyCode(inputCode, 3)));
		} 
		else if (Instance.keyCodeToEmulatedKey.ContainsKey(inputCode)) 
		{
			return Instance.keyCodeToEmulatedKey[inputCode].upThisFrame;
		}
		return false;
#else
        return Input.GetKeyUp(inputCode);
#endif
    }
	
}
