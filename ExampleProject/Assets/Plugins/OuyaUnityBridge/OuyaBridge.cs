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
using LitJson;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Receives events passed through the Android UnityPlayer's UnitySendMessage.
/// Stores information about devices, receipts, products, and purchases.
/// Also can be used to make Purchases, fetch Receipts, and get Gamer UUID and Product List from the OUYA SDK.
/// </summary>

public class OuyaBridge : MonoBehaviour {
	public const string JAVA_APP_CLASS = "com.goodhustle.ouyaunitybridge.OuyaUnityActivity";
	
	// This is a builtin array for speed, as it may be used every frame to loop through controllers.
	public static Device[] devices = {};
	public static List<Receipt> receipts = new List<Receipt>();
	public static List<Product> products = new List<Product>();
	
	public static string activeGamerUuid = string.Empty;
	
	/// <summary>
	/// Events you may subscribe to to be notified of important ODK events.
	/// </summary>
	/// 
	
	public delegate void DeviceEvent();
	public static event DeviceEvent onDevicesChanged;
	
	public delegate void ProductEvent( string productIdentifier );
	public static event ProductEvent onProductPurchased;
	
	public delegate void ProductListEvent();
	public static event ProductListEvent onProductsUpdated;
		
	
	public delegate void GamerUuidEvent( string uuid );
	public static event GamerUuidEvent onGamerUuidFetched;
	
	public delegate void ReceiptsEvent();
	public static event ReceiptsEvent onReceiptsUpdated;
	
	public delegate void PauseResumeEvent();
	public static event PauseResumeEvent onOuyaPause;
	public static event PauseResumeEvent onOuyaResume;
	
	
	public static OuyaBridge _instance = null;
	public static OuyaBridge Instance {
		get {
			if (_instance == null) {
				var go = new GameObject("OuyaBridge");
				_instance = go.AddComponent<OuyaBridge>();
			}
			return _instance;
		}
	}
	
	#region Lifecycle
	
	void Awake() {
		if (_instance == null) {
			_instance = this;
			name = "OuyaBridge";
			DontDestroyOnLoad(gameObject);
		} else {
			Destroy(gameObject);
		}
	}
		
	#endregion
	
	#region ODK Functions
	/// <summary>
	/// Purchases the product based on identifier.
	/// </summary>
	/// <param name='productId'>
	/// Product identifier.
	/// </param>
	public static void PurchaseProduct( string productId ) {
#if UNITY_OUYA && !UNITY_EDITOR
		AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
		AndroidJavaObject activity = jc.GetStatic<AndroidJavaObject>("currentActivity");
		activity.Call("requestPurchase", productId);
#elif UNITY_EDITOR
		// For testing purposes, always assume purchase success and call listener
		if (onProductPurchased != null) {
			Debug.Log ("In editor mode, automatically testing successful purchase");
			onProductPurchased(productId);
		}	
#endif
	}
	/// <summary>
	/// Refreshes the receipts. Please note that this may not be necessary, as receipts are updated at 
	/// launch and after the product is purchased.
	/// </summary>
	public static void RefreshReceipts() {
#if UNITY_OUYA && !UNITY_EDITOR
		AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
		AndroidJavaObject activity = jc.GetStatic<AndroidJavaObject>("currentActivity");
		activity.Call("requestReceipts");
#endif
	}
	/// <summary>
	/// Fetchs the gamer UUID.
	/// </summary>
	public static void FetchGamerUUID() {
#if UNITY_OUYA && !UNITY_EDITOR
		AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
		AndroidJavaObject activity = jc.GetStatic<AndroidJavaObject>("currentActivity");
		activity.Call("fetchGamerUUID");
#endif
	}
	/// <summary>
	/// Determines whether this app is running on ouya hardware.
	/// </summary>
	public static bool IsRunningOnOuyaHardware() {
#if UNITY_OUYA && !UNITY_EDITOR
		AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
		AndroidJavaObject activity = jc.GetStatic<AndroidJavaObject>("currentActivity");
		bool rc = activity.Call<bool>("isRunningOnOuyaHardware");
		return rc;
#else
		return false;
#endif
	}
	/// <summary>
	/// Asks the OuyaFacade what our Integer-based OUYA SDK Version is.
	/// </summary>
	public static int GetOdkVersionNumber() {
#if UNITY_OUYA && !UNITY_EDITOR
		AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
		AndroidJavaObject activity = jc.GetStatic<AndroidJavaObject>("currentActivity");
		int rc = activity.Call<int>("getOdkVersionNumber");
		return rc;
#else
		return 0;
#endif
	}	
	
	#endregion
	
	#region Event Receivers
	
	public void didChangeDevices(string jsonData) {
		
		List<Device> deviceList = new List<Device>();
		deviceList = JsonMapper.ToObject<List<Device>>(jsonData);
		devices = new Device[deviceList.Count];
		int i=0;
		foreach (Device d in deviceList) {
			devices[i++] = d;
			// Uncomment to observe device connection information, useful for multicontroller debugging.
			// Debug.Log("Connecting " + d.ToString());
		}
		// Debug.Log("Devices refreshed: " + devices.Length + " devices connected");
		if (onDevicesChanged != null)
			onDevicesChanged();
	}
	
	public void didFetchReceipts(string jsonData) {
		receipts = JsonMapper.ToObject<List<Receipt>>(jsonData);
		if (onReceiptsUpdated != null)
			onReceiptsUpdated();
	}
	
	public void didFetchProducts(string jsonData) {
		products = JsonMapper.ToObject<List<Product>>(jsonData);
		if (onProductsUpdated != null)
			onProductsUpdated();
	}
	
	public void didPurchaseProductId(string productIdentifier) {
		if (onProductPurchased != null)
			onProductPurchased( productIdentifier );
	}
	
	public void didFetchGamerUuid(string uuid) {
		activeGamerUuid = uuid;
		if (onGamerUuidFetched != null)
			onGamerUuidFetched(uuid);	
	}
	
	public void didPause(string dummy) {
		OuyaInput.ClearAllInputs();	
		if (onOuyaPause != null)
			onOuyaPause();
	}
	
	public void didResume(string dummy) {
		if (onOuyaResume != null)
			onOuyaResume();
	}
	
	#endregion
	
	#region Getters
	
	public static Device getDeviceById(int deviceId) {
		
		for (int i=0,imax=devices.Length; i<imax; i++) {
			if (deviceId == devices[i].id) {
				return devices[i];
			}
		}
		return null;
	}
	
	public static Device getDeviceByPlayerId(int playerId) {
		
		for (int i=0,imax=devices.Length; i<imax; i++) {
			if (playerId == devices[i].player) {
				return devices[i];
			}
		}
		return null;
	}
	#endregion
	
   #region Data Structures
	
	public class Device
	{
		public int id = 0;
		public int player = 0;
		public string name = "";
		public override string ToString() {
			return string.Format("Device #{0} Player #{1}, Name: {2}", this.id, this.player, this.name);
		}
	}
	
    public class Product
    {
        public string identifier = string.Empty;
        public string name = string.Empty;
        public int priceInCents = 0;
		
		// Since OUYA returns all transaction amounts in USD, this is USD only for now.
		public string localizedPrice() {
			return "$" + ((decimal)this.priceInCents) / 100M;
		}
		
		public override string ToString() {
			return string.Format("Product ID: {0}, Name: {1}, Price (USD): ${2:#0.00}", 
				this.identifier, 
				this.name, 
				((decimal)this.priceInCents) / 100M);
		}
    }
	
	public class Receipt
	{
		
		public string generatedDate = string.Empty;
		public string identifier = string.Empty;
		public int priceInCents = 0;
		public string purchaseDate = string.Empty;
		public override string ToString() {
			return string.Format("Receipt Product ID: {0}, Price (USD): ${1:#0.00}, Purchased On: {2}, Generated On: {3}", 
				this.identifier, 
				((decimal)this.priceInCents) / 100M, 
				this.purchaseDate,
				this.generatedDate);
		}	
	}
	
	

    #endregion		
}
