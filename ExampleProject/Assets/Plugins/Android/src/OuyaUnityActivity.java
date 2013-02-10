/*
 * Copyright (c) 2013 Goodhustle Studios, Inc.
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
 *
 * This file incorporates work covered by the following copyright and permission notice:
 * 
 * Copyright (C) 2012 OUYA, Inc.
 * Copyright (C) 2012 Tagenigma LLC
 * Copyright (C) 2012 Hashbang Games
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

package com.goodhustle.ouyaunitybridge;

import tv.ouya.console.api.*;
import tv.ouya.console.internal.util.Strings;

import android.accounts.AccountManager;
import android.app.Activity;
import android.app.AlertDialog;
import android.os.SystemClock;
import android.content.*;
import android.hardware.input.InputManager; //API 16
import android.hardware.input.InputManager.InputDeviceListener; //API 16
import android.os.Bundle;
import android.os.Parcelable;
import android.util.Log;
import android.view.KeyEvent;
import android.view.MotionEvent;
import android.view.InputDevice;
import android.widget.FrameLayout;
import android.widget.LinearLayout.LayoutParams;
import android.widget.RelativeLayout;
import android.widget.Toast;

import org.json.JSONException;
import org.json.JSONObject;

import com.google.gson.Gson;

import java.io.IOException;
import java.security.GeneralSecurityException;

import com.unity3d.player.UnityPlayer;
import com.unity3d.player.UnityPlayerActivity;
import com.unity3d.player.UnityPlayerNativeActivity;
import com.unity3d.player.UnityPlayerProxyActivity;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collections;
import java.util.Comparator;
import java.util.List;

import tv.ouya.console.api.OuyaController;

public class OuyaUnityActivity extends Activity implements InputDeviceListener
{


    /**
     * The tag for log messages
     */

    private static final String LOG_TAG = "OuyaUnityActivity";

	//the Unity Player
	private UnityPlayer mUnityPlayer;

    /**
     * Log onto the developer website (you should have received a URL, a username and a password in email)
     * and get your developer ID. Plug it in here. Use your developer ID, not your developer UUID.
     * <p/>
     * The current value is just a sample developer account. You should change it.
     */
    public static final String DEVELOPER_ID = "";

    /**
     * The application key. This is used to decrypt encrypted receipt responses. This should be replaced with the
     * application key obtained from the OUYA developers website.
     */

    private static final byte[] APPLICATION_KEY = { 0x00 };

    /**
     * Before this app will run, you must define some purchasable items on the developer website. Once
     * you have defined those items, put their Product IDs in the List below.
     * <p/>
     * The Product IDs below are those in our developer account. You should change them.
     */

    public static final List<Purchasable> PRODUCT_IDENTIFIER_LIST = Arrays.asList(new Purchasable("long_sword"), 
    	new Purchasable("sharp_axe"), 
    	new Purchasable("__DECLINED__THIS_PURCHASE"));
    
    /**
     * @IMPORTANT
	 * The following line determines whether execution in the Unity application
     * will continue when the OUYA SDK opens up its own overlays. 
     * If pause is not enabled, Input will be bypassed when the Unity player is paused because
     * the java layer will stop sending input. 
     * If pause is enabled, then the game will not show underneath the popup,
     * but it will completely stop execution
	 */
    public static boolean UNITY_PAUSE_ON_OUYA_OVERLAYS = false;

    /**
     * The saved instance state key for products
     */

    protected static final String PRODUCTS_INSTANCE_STATE_KEY = "Products";

    /**
     * The saved instance state key for receipts
     */

    protected static final String RECEIPTS_INSTANCE_STATE_KEY = "Receipts";

    /**
     * The ID used to track the activity started by an authentication intent
     */

    protected static final int AUTHENTICATION_ACTIVITY_ID = 1;

    protected OuyaFacade ouyaFacade;
	
    protected UserManager userManager;
    protected List<Product> mProductList;
    protected List<Receipt> mReceiptList;

	//indicates the Unity player has loaded
	protected Boolean mEnableUnity = true;

	protected Boolean mEnableLogging = false;

	protected InputManager mInputManager = null;
	protected InputManager.InputDeviceListener minputDeviceListener = null;

	protected static ControllerState[] playerStates;

	protected String mGamerUuid;

	protected IntentFilter accountsChangedFilter;

	protected boolean mPaused = false;

    /**
     * Broadcast listener to handle re-requesting the receipts when a user has re-authenticated
     */

    private BroadcastReceiver mAuthChangeReceiver = new BroadcastReceiver() {
        @Override
        public void onReceive(Context context, Intent intent) {
        	// Refresh receipts
            requestReceipts();
            // Refresh gamer UUID
            fetchGamerUUID();
        }
    };
	@Override
	protected void onCreate(Bundle savedInstanceState) 
	{
		super.onCreate(savedInstanceState);

// Initialize ouyaFacade
		ouyaFacade = OuyaFacade.getInstance();
		ouyaFacade.init(this, DEVELOPER_ID);

        // Uncomment this line to test against the server using "fake" credits.
        // This will also switch over to a separate "test" purchase history.
        //ouyaFacade.setTestMode();

        userManager = UserManager.getInstance(this);


		playerStates = new ControllerState[OuyaController.MAX_CONTROLLERS];
        for (int i=0; i<OuyaController.MAX_CONTROLLERS; i++)
        {
                playerStates[i] = new ControllerState();
        }

        // Attempt to restore the product and receipt list from the savedInstanceState Bundle
        if(savedInstanceState != null) {
            if(savedInstanceState.containsKey(PRODUCTS_INSTANCE_STATE_KEY)) {
                Parcelable[] products = savedInstanceState.getParcelableArray(PRODUCTS_INSTANCE_STATE_KEY);
                mProductList = new ArrayList<Product>(products.length);
                for(Parcelable product : products) {
                    mProductList.add((Product) product);
                }
                addProducts();
            }
            if(savedInstanceState.containsKey(RECEIPTS_INSTANCE_STATE_KEY))  {
                Parcelable[] receipts = savedInstanceState.getParcelableArray(RECEIPTS_INSTANCE_STATE_KEY);
                mReceiptList = new ArrayList<Receipt>(receipts.length);
                for(Parcelable receipt : receipts) {
                    mReceiptList.add((Receipt) receipt);
                }
                addReceipts();
            }
        }

        // Request the product list if it could not be restored from the savedInstanceState Bundle
        if(mProductList == null) {
            requestProducts();
        }

		// Create the UnityPlayer
        mUnityPlayer = new UnityPlayer(this);
        int glesMode = mUnityPlayer.getSettings().getInt("gles_mode", 1);
        boolean trueColor8888 = false;
        mUnityPlayer.init(glesMode, trueColor8888);
        setContentView(R.layout.main);

        // Add the Unity view
        FrameLayout layout = (FrameLayout) findViewById(R.id.unityLayout);
        LayoutParams lp = new LayoutParams (LayoutParams.FILL_PARENT, LayoutParams.FILL_PARENT);
        layout.addView(mUnityPlayer.getView(), 0, lp);

		// Set the focus
        RelativeLayout mainLayout = (RelativeLayout) findViewById(R.id.mainLayout);
		mainLayout.setFocusableInTouchMode(true);

	}
	@Override
	protected void onStart() 
	{
		super.onStart();

		requestReceipts();

		// Register to receive notifications about account changes. This will re-query the receipt
		// list in order to ensure it is always up to date for whomever is logged in.
		accountsChangedFilter = new IntentFilter();
		accountsChangedFilter.addAction(AccountManager.LOGIN_ACCOUNTS_CHANGED_ACTION);
		registerReceiver(mAuthChangeReceiver, accountsChangedFilter);

		// listen for controller changes - http://developer.android.com/reference/android/hardware/input/InputManager.html#registerInputDeviceListener%28android.hardware.input.InputManager.InputDeviceListener,%20android.os.Handler%29
		Context context = getBaseContext();
		mInputManager = (InputManager)context.getSystemService(Context.INPUT_SERVICE);
		mInputManager.registerInputDeviceListener (this, null);

		sendDevices();
	}

/// In the iap sample, this calls super.onPause. Not sure why?

	@Override
	protected void onStop()
	{
		if (null != mInputManager)
		{
			try {
				mInputManager.unregisterInputDeviceListener(this);
			} catch (IllegalArgumentException e) {
				Log.w(LOG_TAG, "Already unregistered input listener at onPause");	
			}
		}
		// Unregister input listener
		try {
			unregisterReceiver(mAuthChangeReceiver);
		} catch (IllegalArgumentException e) {
			Log.w(LOG_TAG, "Already unregistered auth change receiver at onStop");
		}
		mUnityPlayer.pause();
		super.onStop();
	}

	@Override
	protected void onDestroy()
	{
		ouyaFacade.shutdown();
		userManager.shutdown();
		super.onDestroy();
		// Kill Unity player
		mUnityPlayer.quit();
	}
	


    @Override
    public void onPause()
	{
		if (null != mInputManager)
		{
			try {
				mInputManager.unregisterInputDeviceListener(this);
			} catch (IllegalArgumentException e) {
				Log.w(LOG_TAG, "Already unregistered input listener at onPause");	
			}
		}
		try {
			unregisterReceiver(mAuthChangeReceiver);
		} catch (IllegalArgumentException e) {
			Log.w(LOG_TAG, "Already unregistered auth change receiver at onPause");
		}
		// Clear out input
        for (int i=0; i<OuyaController.MAX_CONTROLLERS; i++)
        {
        	playerStates[i].Clear();
        }

        super.onPause();
        if (mEnableLogging) {
	        Log.i(LOG_TAG, "OuyaUnityActivity.onPause called");
	    }
        if (UNITY_PAUSE_ON_OUYA_OVERLAYS) {
			mUnityPlayer.pause();
		}
		// On the unity side, clear all current input and button flags.
		UnityPlayer.UnitySendMessage("OuyaBridge", "didPause", "");
		if (isFinishing()) {
			// Unfortunately this is returning true when hitting the home button.
			if (mEnableLogging) {
				Log.i(LOG_TAG, " - OuyaUnityActivity.onPause isFinishing, killing unity player!");
			}

			ouyaFacade.shutdown();
			userManager.shutdown();
			mUnityPlayer.quit();

		}
		mPaused = true;

    }

    @Override
    public void onResume()
	{
		super.onResume();

		if (null != mInputManager)
		{
			mInputManager.registerInputDeviceListener(this, null);
		}

		registerReceiver(mAuthChangeReceiver, accountsChangedFilter);
		UnityPlayer.UnitySendMessage("OuyaBridge", "didResume", "");
		if (UNITY_PAUSE_ON_OUYA_OVERLAYS) {
			mUnityPlayer.resume();
		}
		mPaused = false;
    }

    /**
     * Check for the result from a call through to the authentication intent. If the authentication
     * was successful then re-try the purchase.
     */
    @Override
    protected void onActivityResult(final int requestCode, final int resultCode, final Intent data) {
    	if (requestCode == AUTHENTICATION_ACTIVITY_ID) {
    		final SharedPreferences purchasePrefs = getProductIdSharedPreferences();
    		final String suspendedPurchaseId = purchasePrefs.getString("currentPurchase", null);
    		if (suspendedPurchaseId == null) {
    			return;
    		}

    		if (resultCode == RESULT_OK) {
    			SharedPreferences.Editor editor = purchasePrefs.edit();
    			editor.remove("currentPurchase");
    			requestPurchase(suspendedPurchaseId);
    		}
    	}
    }

    /**
     * Save the products and receipts if we're going for a restart.
     */
    @Override
    protected void onSaveInstanceState(final Bundle outState) {
    	if (mProductList != null) {
    		outState.putParcelableArray(PRODUCTS_INSTANCE_STATE_KEY, mProductList.toArray(new Product[mProductList.size()]));
    	}
    	if (mReceiptList != null) {
    		outState.putParcelableArray(RECEIPTS_INSTANCE_STATE_KEY, mReceiptList.toArray(new Receipt[mReceiptList.size()]));
    	}
    }

    /**
     * Get the shared preferences object which is used to store the productId when the user
     * is being sent for authentication
     */

    private SharedPreferences getProductIdSharedPreferences() {
        return getSharedPreferences("OuyaUnityActivity", MODE_PRIVATE);
    }

	/// Implements InputDeviceListener
	public @Override void onInputDeviceAdded(int deviceId)
	{
		if (mEnableLogging)
		{
			Log.i("Unity", "void onInputDeviceAdded(int deviceId) " + deviceId);
		}
		sendDevices();
	}
	public @Override void onInputDeviceChanged(int deviceId)
	{
		if (mEnableLogging)
		{
			Log.i("Unity", "void onInputDeviceAdded(int deviceId) " + deviceId);
		}
		sendDevices();
	}
	public @Override void onInputDeviceRemoved(int deviceId)
	{
		if (mEnableLogging)
		{
			Log.i("Unity", "void onInputDeviceRemoved(int deviceId) " + deviceId);
		}
		sendDevices();
	}


	void sendDevices()
	{
		//Get a list of all device id's and assign them to players.
		ArrayList<Device> devices = checkDevices();
		Gson gson = new Gson();
		String jsonData = gson.toJson(devices);
		UnityPlayer.UnitySendMessage("OuyaBridge", "didChangeDevices", jsonData);
	}	

	private void requestProducts() {
		ouyaFacade.requestProductList(PRODUCT_IDENTIFIER_LIST, new CancelIgnoringOuyaResponseListener<ArrayList<Product>>() {
			@Override
			public void onSuccess(final ArrayList<Product> products) {
				mProductList = products;
				addProducts();
			}

			@Override
			public void onFailure(int errorCode, String errorMessage, Bundle optionalData) {
				// @TODO: Handle failure more gracefully than toast.
				Toast.makeText(OuyaUnityActivity.this, "Could not fetch product information (error " + errorCode + ":" + errorMessage, Toast.LENGTH_LONG).show();
			}

		});
	}

	public void fetchGamerUUID() {
		ouyaFacade.requestGamerUuid(new CancelIgnoringOuyaResponseListener<String>() {
			@Override
			public void onSuccess(String result) {
				mGamerUuid = result;
				// Send back to unity
				UnityPlayer.UnitySendMessage("OuyaBridge", "didFetchGamerUuid", mGamerUuid);
			}

			@Override
			public void onFailure(int errorCode, String errorMessage, Bundle optionalData) {
				Log.w(LOG_TAG, "Fetch gamer UUID error (code " + errorCode + ": " + errorMessage + ")");
				if (errorCode == OuyaErrorCodes.NO_AUTHENTICATION_DATA) {
					// If there is no authentication data, we need to get the user to add an ouya account
					userManager.requestUserAddsAccount(OuyaUnityActivity.this, new OuyaResponseListener<Void>() {
						@Override
						public void onSuccess(Void result) {
							// Do nothing
						}

						@Override
						public void onFailure(int errorCode, String errorMessage, Bundle optionalData) {
							// @TODO: do something more than toast
							Toast.makeText(OuyaUnityActivity.this, "Unable to fetch gamer UUID (error " + errorCode + ": " + errorMessage + ")", Toast.LENGTH_LONG).show();
						}

						@Override
						public void onCancel() {
							Toast.makeText(OuyaUnityActivity.this, "Unable to fetch gamer UUID (Attempt to get account cancelled)", Toast.LENGTH_LONG).show();
						}
					});
				} else if (errorCode == OuyaErrorCodes.INVALID_AUTHENTICATION_DATA && optionalData.containsKey("intent")) {
					// If the authenticationd ata is invalid, and a re-authentication intent has been supplied,
					// start the re-authentication
					startActivity((Intent) optionalData.getParcelable("intent"));
				} else {
					Toast.makeText(OuyaUnityActivity.this, "Unable to fetch gamer UUID (error " + errorCode + ": " + errorMessage + ")", Toast.LENGTH_LONG).show();
				}
			}
		});
	}

	private void requestReceipts() {
		ouyaFacade.requestReceipts(new ReceiptListener());
	}

	private void addProducts() {
		// Send product information over to Unity.
		Gson gson = new Gson();
		String json = gson.toJson(mProductList);
		UnityPlayer.UnitySendMessage("OuyaBridge", "didFetchProducts", json);
	}

	private void addReceipts() {
		// Send receipt information over to Unity.
	}

	public void requestPurchase(final String productId) {
		ouyaFacade.requestPurchase(new Purchasable(productId), new PurchaseListener(productId));
	}

	private ArrayList<Device> checkDevices(){
		//Get a list of all device id's and assign them to players.
		ArrayList<Device> devices = new ArrayList<Device>();
		int[] deviceIds = InputDevice.getDeviceIds();
		
		
		int controllerCount = 1;
		for (int count=0; count < deviceIds.length; count++)
		{
			InputDevice d = InputDevice.getDevice(deviceIds[count]);
			if (!d.isVirtual())
			{
				if (d.getName().toUpperCase().indexOf("XBOX 360 WIRELESS RECEIVER") != -1 ||
					d.getName().toUpperCase().indexOf("OUYA GAME CONTROLLER") != -1 ||
					d.getName().toUpperCase().indexOf("MICROSOFT X-BOX 360 PAD") != -1 ||
					d.getName().toUpperCase().indexOf("IDROID:CON") != -1 ||
					d.getName().toUpperCase().indexOf("USB CONTROLLER") != -1)
				{
					Device device = new Device();
					device.id = d.getId();
					device.player = OuyaController.getPlayerNumByDeviceId(device.id);
					device.name = d.getName();
					devices.add(device);
					controllerCount++;
				}
				else  
				{
					Device device = new Device();
					device.id = d.getId();
					// Player is actually zero-indexed
					device.player = -1;
					device.name = d.getName();
					if (device.name.indexOf("gpio-keys") == -1) {
						// Skip gpio-keys!
						devices.add(device);	
					}
					
				}
			}
		}
		return devices;
	}	


	
	@Override
	public boolean onKeyDown(int keyCode, KeyEvent event) 
	{

		// Pass to OuyaController first, then process.
		boolean handled = OuyaController.onKeyDown(keyCode, event);
		if (mPaused) return handled || super.onKeyDown(keyCode, event);
		int playerNum = 0;
		try {
			playerNum = OuyaController.getPlayerNumByDeviceId(event.getDeviceId());     
		    ControllerState data = playerStates[playerNum];
			OuyaController c = OuyaController.getControllerByPlayer(playerNum);
			if (data != null)
			{
				data.ButtonO = c.getButton(OuyaController.BUTTON_O);
				data.ButtonU = c.getButton(OuyaController.BUTTON_U);
				data.ButtonY = c.getButton(OuyaController.BUTTON_Y);
				data.ButtonA = c.getButton(OuyaController.BUTTON_A);


				data.ButtonDPD = c.getButton(OuyaController.BUTTON_DPAD_DOWN);
				data.ButtonDPU = c.getButton(OuyaController.BUTTON_DPAD_UP);
				data.ButtonDPL = c.getButton(OuyaController.BUTTON_DPAD_LEFT);
				data.ButtonDPR = c.getButton(OuyaController.BUTTON_DPAD_RIGHT);
				data.ButtonL1 = c.getButton(OuyaController.BUTTON_L1);
				data.ButtonL2 = c.getButton(OuyaController.BUTTON_L2);
				data.ButtonL3 = c.getButton(OuyaController.BUTTON_L3);

				data.ButtonR1 = c.getButton(OuyaController.BUTTON_R1);
				data.ButtonR2 = c.getButton(OuyaController.BUTTON_R2);
				data.ButtonR3 = c.getButton(OuyaController.BUTTON_R3);

				data.ButtonSystem = c.getButton(OuyaController.BUTTON_SYSTEM);

			}
		} catch (Exception e) {
			Log.w("Unity", "Exception occurred getting controller state for player " + playerNum + ": " + e.toString());
		}

	    
	    return handled || super.onKeyDown(keyCode, event);
	}

	@Override
	public boolean onKeyUp(int keyCode, KeyEvent event) 
	{
		// Pass to OuyaController first, then process.
	    boolean handled = OuyaController.onKeyDown(keyCode, event);
	    if (mPaused) return handled || super.onKeyDown(keyCode, event);
		int playerNum = 0;
		try {
		    playerNum = OuyaController.getPlayerNumByDeviceId(event.getDeviceId());     
		    ControllerState data = playerStates[playerNum];
			OuyaController c = OuyaController.getControllerByPlayer(playerNum);
			if (data != null)
			{
				data.ButtonO = c.getButton(OuyaController.BUTTON_O);
				data.ButtonU = c.getButton(OuyaController.BUTTON_U);
				data.ButtonY = c.getButton(OuyaController.BUTTON_Y);
				data.ButtonA = c.getButton(OuyaController.BUTTON_A);


				data.ButtonDPD = c.getButton(OuyaController.BUTTON_DPAD_DOWN);
				data.ButtonDPU = c.getButton(OuyaController.BUTTON_DPAD_UP);
				data.ButtonDPL = c.getButton(OuyaController.BUTTON_DPAD_LEFT);
				data.ButtonDPR = c.getButton(OuyaController.BUTTON_DPAD_RIGHT);
				data.ButtonL1 = c.getButton(OuyaController.BUTTON_L1);
				data.ButtonL2 = c.getButton(OuyaController.BUTTON_L2);
				data.ButtonL3 = c.getButton(OuyaController.BUTTON_L3);

				data.ButtonR1 = c.getButton(OuyaController.BUTTON_R1);
				data.ButtonR2 = c.getButton(OuyaController.BUTTON_R2);
				data.ButtonR3 = c.getButton(OuyaController.BUTTON_R3);

				data.ButtonSystem = c.getButton(OuyaController.BUTTON_SYSTEM);
			}
		} catch (Exception e) {
			Log.w("Unity", "Exception occurred getting controller state for player " + playerNum + ": " + e.toString());
		}

	    return handled || super.onKeyDown(keyCode, event);


	}

	@Override
	public boolean onGenericMotionEvent(MotionEvent event) {
		// Pass to OuyaController first, then process.
		boolean handled = OuyaController.onGenericMotionEvent(event);
		if (mPaused) return handled || super.onGenericMotionEvent(event);

		// Check if this was a joystick or touch hover event

		int playerNum = 0;
		try {
			playerNum = OuyaController.getPlayerNumByDeviceId(event.getDeviceId());     
		    ControllerState data = playerStates[playerNum];
			OuyaController c = OuyaController.getControllerByPlayer(playerNum);
		    if (data != null)
			{

				data.AxisLSX = c.getAxisValue(OuyaController.AXIS_LS_X);
				data.AxisLSY = c.getAxisValue(OuyaController.AXIS_LS_Y);
				data.AxisRSX = c.getAxisValue(OuyaController.AXIS_RS_X);
				data.AxisRSY = c.getAxisValue(OuyaController.AXIS_RS_Y);
				data.AxisLT = c.getAxisValue(OuyaController.AXIS_L2);
				data.AxisRT = c.getAxisValue(OuyaController.AXIS_R2);

			}
		} catch (Exception e) {
			Log.i("Unity", "Exception occurred getting controller state for player " + playerNum + ": " + e.toString());
		}
	    
	    return handled || super.onGenericMotionEvent(event);
	}

	/*** 
	/* Unity Interface through JNI
	/*/


	public static ControllerState GetControllerState(int playerNum)
	{
		return playerStates[playerNum];
	}

	

	public class Device
	{
		public int id;
		public int player;
		public String name;
	}	


	public static class ControllerState
	{

        public float AxisLSX = 0;
        public float AxisLSY = 0;
        public float AxisRSX = 0;
        public float AxisRSY = 0;
        public float AxisLT = 0;
        public float AxisRT = 0;

		public boolean ButtonO = false;
		public boolean ButtonU = false;
		public boolean ButtonY = false;
		public boolean ButtonA = false;
		public boolean ButtonDPD = false;
		public boolean ButtonDPU = false;
		public boolean ButtonDPL = false;
		public boolean ButtonDPR = false;
		public boolean ButtonL1 = false;
		public boolean ButtonL2 = false;
		public boolean ButtonL3 = false;
		public boolean ButtonR1 = false;
		public boolean ButtonR2 = false;
		public boolean ButtonR3 = false;

		public boolean ButtonSystem = false;

		public void Clear() {
			AxisLSX = 0;
			AxisLSY = 0;
			AxisRSX = 0;
			AxisRSY = 0;
			AxisLT = 0;
			AxisRT = 0;

			ButtonO = false;
			ButtonU = false;
			ButtonY = false;
			ButtonA = false;
			ButtonDPD = false;
			ButtonDPU = false;
			ButtonDPL = false;
			ButtonDPR = false;
			ButtonL1 = false;
			ButtonL2 = false;
			ButtonL3 = false;
			ButtonR1 = false;
			ButtonR2 = false;
			ButtonR3 = false;
			ButtonSystem = false;

		}
	}

	/**
	 * The callback for list of user receipts
	 */
	private class ReceiptListener extends CancelIgnoringOuyaResponseListener<String> {
		/**
		 * Handle successful receipts fetch
		 *
		 * @param receiptResponse Server response.
		 */

		@Override
		public void onSuccess(String receiptResponse) {
			OuyaEncryptionHelper helper = new OuyaEncryptionHelper();
			final List<Receipt> receipts;
			try {
				// Determine whether this is encrypted JSON.
				if (receiptResponse.contains("{")) {
					receipts = helper.parseJSONReceiptResponse(receiptResponse);
				} else {
					receipts = helper.decryptReceiptResponse(receiptResponse, APPLICATION_KEY);
				}
			} catch (GeneralSecurityException e) {
				throw new RuntimeException(e);
			} catch (IOException e) {
				throw new RuntimeException(e);
			} catch (Exception e) {
				Log.w(LOG_TAG, "Receipt Listener received invalid response error (" + e.getMessage() + ")");
				return;
			}

			Collections.sort(receipts, new Comparator<Receipt>() {
				@Override
				public int compare(Receipt lhs, Receipt rhs) {
					return rhs.getPurchaseDate().compareTo(lhs.getPurchaseDate());
				}
			});

			mReceiptList = receipts;
			// Report receipt list back to Unity.
			Gson gson = new Gson();
			String receiptJson = gson.toJson(mReceiptList);
			UnityPlayer.UnitySendMessage("OuyaBridge", "didFetchReceipts", receiptJson);
		}

        /**
         * Handle a failure. Because displaying the receipts is not critical to the application we just show an error
         * message rather than asking the user to authenticate themselves just to start the application up.
         *
         * @param errorCode An HTTP error code between 0 and 999, if there was one. Otherwise, an internal error code from the
         *                  Ouya server, documented in the {@link OuyaErrorCodes} class.
         *
         * @param errorMessage Empty for HTTP error codes. Otherwise, a brief, non-localized, explanation of the error.
         *
         * @param optionalData A Map of optional key/value pairs which provide additional information.
         */
		@Override 
		public void onFailure(int errorCode, String errorMessage, Bundle optionalData) {
			Log.w(LOG_TAG, "Request Receipts error (code " + errorCode + ": " + errorMessage + ")");
			Toast.makeText(OuyaUnityActivity.this, "Could not fetch receipts (error " + errorCode + ": " + errorMessage + ")", Toast.LENGTH_LONG).show();
		}
	}

	/**
	 * Callback for purchases
	 */

	private class PurchaseListener extends CancelIgnoringOuyaResponseListener<String> {
		/** 
		* The ID of the product the user is trying to purchase. This is used in onFailure to start a re-purchase
		* if the user wishes to do so.
		*/

		private String mProductId;

		PurchaseListener(final String productId) {
			mProductId = productId;
		}

		@Override
		public void onSuccess(String result) {
			try {
				Product product;
				if (result.contains("{")) {
					product = new Product(new JSONObject(result));
				} else {
					product = OuyaEncryptionHelper.decryptProductResponse(result, APPLICATION_KEY);
				}
				// Show alert dialog?
				/*
	                new AlertDialog.Builder(OuyaUnityActivity.this)
	                        .setTitle(getString(R.string.alert_title))
	                        .setMessage("You have successfully purchased a " + product.getName() + " for " + Strings.formatDollarAmount(product.getPriceInCents()))
	                        .setPositiveButton(R.string.ok, new DialogInterface.OnClickListener() {
	                            @Override
	                            public void onClick(DialogInterface dialogInterface, int i) {
	                                dialogInterface.dismiss();
	                            }
	                        })
	                        .show();
	                        */
				// Report success back to Unity
				UnityPlayer.UnitySendMessage("OuyaBridge", "didPurchaseProductId", product.getIdentifier());
				// Re-request receipts to keep receipt data up to date
				requestReceipts();
			} catch (JSONException e) {
				onFailure(OuyaErrorCodes.THROW_DURING_ON_SUCCESS, e.getMessage(), Bundle.EMPTY);
			} catch (IOException e) {
				onFailure(OuyaErrorCodes.THROW_DURING_ON_SUCCESS, e.getMessage(), Bundle.EMPTY);
			} catch (GeneralSecurityException e) {
				onFailure(OuyaErrorCodes.THROW_DURING_ON_SUCCESS, e.getMessage(), Bundle.EMPTY);
			}
		}


	    /**
	     * Handle an error. If the OUYA framework supplies an intent this means that the user needs to
	     * either authenticate or re-authenticate themselves, so we start the supplied intent.
	     *
	     * @param errorCode An HTTP error code between 0 and 999, if there was one. Otherwise, an internal error code from the
	     *                  Ouya server, documented in the {@link OuyaErrorCodes} class.
	     *
	     * @param errorMessage Empty for HTTP error codes. Otherwise, a brief, non-localized, explanation of the error.
	     *
	     * @param optionalData A Map of optional key/value pairs which provide additional information.
	     */

	    @Override
	    public void onFailure(int errorCode, String errorMessage, Bundle optionalData) {
	    	if (errorCode == OuyaErrorCodes.INVALID_AUTHENTICATION_DATA && optionalData.containsKey("intent")) {
	    		// start re-auth
				SharedPreferences.Editor editor = getProductIdSharedPreferences().edit();
				editor.putString("currentPurchase", mProductId);
				editor.apply();
				startActivityForResult((Intent) optionalData.getParcelable("intent"), AUTHENTICATION_ACTIVITY_ID);
				return;
	    	}

	    	// Show the user the error and offer them ability to repurchase if they think the error is not permanent.
	    	            // Show the user the error and offer them the ability to re-purchase if they
	        // decide the error is not permanent.
	        new AlertDialog.Builder(OuyaUnityActivity.this)
	                .setTitle(getString(R.string.alert_title))
	                .setMessage("Unfortunately, your purchase failed [error code " + errorCode + " (" + errorMessage + ")]. Would you like to try again?")
	                .setPositiveButton(R.string.ok, new DialogInterface.OnClickListener() {
	                    @Override
	                    public void onClick(DialogInterface dialogInterface, int i) {
	                        dialogInterface.dismiss();
	                        requestPurchase(mProductId);
	                    }
	                })
	                .setNegativeButton(R.string.cancel, null)
	                .show();
	    }
	}
}