OuyaUnityBridge
===============

Latest Version: 1.0.6
ODK Version: 1.0.6

A minimalist, unofficial bridge between the OUYA ODK and Unity 4. Adheres to polling-based Input convention in Unity.

About
---------------

### Controller Input Preface

This is an unofficial Unity-Ouya bridge that provides an abstraction mapped to Unity-standard Input design so that experienced developers with large codebases can jump right in instead of rewriting for event-based input. The official Unity plugin optimizes for event-based input instead of polling-based input. This is a minimalist polling-based solution with configurable input, and it layers Unity Input idioms on top of ODK's OuyaController class to allow for faster porting. The OuyaUnityBridge is mostly appropriate to experienced developers who show up from time to time with a large project and would have a very hard time switching over to event-based input (like me).

It does not have all the bells and whistles of the official Ouya Unity plugin, so PLEASE only use this if you know what you're doing and need it. This is based on the iap-sample-app and the Unity plugin's Java compilation approach. If you don't have a big project to port, I would recommend the official Unity plugin, as it's going to be the most up-to-date and supports a bunch of different controllers.

On the device, it uses the OuyaController API to monitor and update controller input, so any controller supported by OuyaController API should work. In the editor, it simply falls back to regular Unity input.

If you understand and use Unity Input virtual axes and buttons or keycodes heavily, OuyaInput basically replaces Input calls GetButton/Up/Down, GetKey/Up/Down, GetAxis, anyKey, and anyKeyDown, which then map ouya buttons and axes to your existing buttons, axes, or keycodes used by your game. It uses the definitions on the OuyaInput emulatedControllers (visible in the inspector) to define what buttons map to both virtual names and keycodes.

Again, if you can't get this to work or don't already rely heavily on Unity Input functions, you should probably use the official OUYA Unity plugin instead.

See the wiki for OuyaInput's default virtual axis / button names and KeyCodes for OUYA controller inputs:

https://github.com/getluky/OuyaUnityBridge/wiki

### How to use OuyaInput.cs

* Drop the OuyaBridge prefab into your splash/loading scene.
* Customize the OuyaInput virtual axes and buttons to match your desired virtual (or key-based) input definitions.
* Replace Input.GetButton/GetAxis/GetKey/anyKey/anyKeyDown calls with OuyaInput call.


### IAP and UUIDs

OuyaBridge also supports IAP and OuyaFacade calls in the ODK. The OuyaUnityActivity.java must be customized with your developer ID and product lists first.

When the application starts, it will automatically try to populate OuyaBridge.receipts and OuyaBridge.products just like the default iap-sample-app. A few seconds after startup, you can probably just look directly into these static arrays.

If you need to call into the ODK, you can use the following static methods:
* OuyaBridge.PurchaseProduct(string productId) - purchases a product by string ID, but you should make sure it exists in OuyaBridge.products first!
* OuyaBridge.FetchGamerUUID() - gets the gamer's UUID.
* OuyaBridge.RefreshReceipts()
* OuyaBridge.RequestProducts()
* OuyaBridge.GetOdkVersionNumber() - returns integer-based ODK ver.
* OuyaBridge.IsRunningOnOuyaHardware() - should tell you whether it's running on OUYA hardware, but I think this is currently broken in the ODK.

... which respond asynchronously. You can subscribe to the following events in OuyaBridge.cs for notifications:
* onDevicesChanged()
* onReceiptsUpdated()
* onProductPurchased(string productIdentifier)
* onProductsUpdated()
* onGamerUuidFetched(string uuid)
* onOuyaPause()
* onOuyaResume()

...or simply observe the static activeGamerUuid, receipts and products variables of OuyaBridge.

How to Use:
---------------
* Import OuyaUnityBridge.unitypackage to your existing project (https://github.com/getluky/OuyaUnityBridge/blob/master/OuyaUnityBridge.unitypackage?raw=true)
* Make sure your build target is for Android
* Change the Minimum API level to Jelly Bean (16)
* Change Resolution and Presentation > Default Orientation to Landscape Left
* Edit Plugins/OuyaBridge/OuyaBridge.cs and update JAVA_APP_CLASS. Replace com.goodhustle.ouyaunitybridge with your bundle id.
* Edit Plugins/Android/src/ApplicationManifest.xml.  Replace com.goodhustle.ouyaunitybridge with your bundle id.
* Edit Plugins/Android/src/OuyaUnityActivity.java with your application's bundle identifier in the package def, add your developer id, product ids and any shared secrets
* In Unity, drag the OuyaBridge GameObject into the first scene in your game.
* Adjust default deadzones as required in OuyaInput component settings
* Click "Reset to Defaults" to define new virtual emulated keys, buttons, and axes. Modify as desired.
* *** Make sure OuyaInput is at the TOP of Edit > Project Settings > Script Execution Order. ***
* Add "UNITY_OUYA" to the list of Build Settings > Other Settings > Scripting Define Symbols in your Android build
* Open Window > Ouya Panel, and set up java paths correctly.
* Click Compile within the Ouya Panel. Check the console for errors. You can now Build and Run to test on the OUYA.
* To test with non-OUYA controllers (or even your keyboard) in the editor, open the Unity Input manager and set up virtual inputs corresponding to the same names as used in the OuyaInput emulated controller definitions.
* Edit your existing code, and replace Input.<function> calls with OuyaInput.<function> for the functions listed below.


### A note on Pausing/Overlays

While developing the Java side, I noticed that the ODK calls the Activity's onPause method when showing IAP confirmation dialogs. If the UnityPlayer is paused, this will effectively stop execution safely if the gamer is in the middle of action, but the display will black out under the dialogue. I have set this to not pause by default, but instead clear out all input temporarily, as most games may only have IAP possible within a menu system that doesn't require time pausing. To change this setting, edit OuyaUnityActivity.java and set UNITY_PAUSE_ON_OUYA_OVERLAYS to true. In either case, OuyaBridge.didPause and OuyaBridge.didResume are called if you want to do custom handling here.

With Beast Boxing Turbo, to support the way that an app can be paused with a double-tap/long press of the system button, I have listeners for onPause and onResume that control AudioListener.pause (to disable music), and also that call GL.InvalidateState() on resume.

Example Unity Project
----------------
You can also import and install the example Unity Project included in the github repo to test out controller input and IAP together. Simply edit Plugins/Android/src/OuyaUnityActivity.java and add in your developer ID and products, recompile java, double check that the above configuration is all set, then Build and Run. Use the touchpad to try clicking on product purchase buttons, fetch Gamer UUID buttons, or the refresh receipts button.

Changelog
-----------------
1.0.6
* Updated ODK SDK to 1.0.6
* Addressed crash issues from external devices (USB/Bluetooth)
* Addressed performance issues from external devices

1.0.2
* Updated ODK SDK to 1.0.2
1.0 - Based on ODK 1.0.1
* Adds in a one-frame simulated press for single-tap on the OUYA MENU/SYSTEM button.
* Updates IAP structure to work with encrypted receipts and helpers, based on the iap sample app.
* Adds Product.localizedPrice() on the Unity side.
* Adds OuyaBridge.RequestProducts
* Handles disconnect/reconnect of controllers while maintaining player number.
* Better UI layout in Example App
* Simulate onProductPurchased event in editor when calling PurchaseProduct
* Calls for IsRunningOnOuyaHardware and GetOdkVersionNumber.
* Recover from connection of an uninitialized device with playerNum = -1
* Added editor script (Windows > Key Window) to assist with converting .der to byte array.
0.1 - Initial release, based on ODK 0.6 and Unity 4.


License
-----------------
As is the official ODK, OuyaUnityBridge is licensed under the Apache 2.0 License.

Copyright (c) 2013 Goodhustle Studios, Inc.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

This project incorporates work covered by the following copyright and permission notice:

Copyright (C) 2012 OUYA, Inc.
Copyright (C) 2012 Tagenigma LLC
Copyright (C) 2012 Hashbang Games

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
