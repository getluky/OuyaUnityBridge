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
import android.util.Base64;
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
import javax.crypto.Cipher;
import javax.crypto.SecretKey;
import javax.crypto.spec.IvParameterSpec;
import javax.crypto.spec.SecretKeySpec;

import java.io.IOException;
import java.io.UnsupportedEncodingException;
import java.security.*;
import java.security.spec.X509EncodedKeySpec;
import java.text.ParseException;
import java.security.GeneralSecurityException;

import com.unity3d.player.UnityPlayer;
import com.unity3d.player.UnityPlayerActivity;
import com.unity3d.player.UnityPlayerNativeActivity;
import com.unity3d.player.UnityPlayerProxyActivity;
import java.util.*;
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
     * The current value is the OUYA sample default so that UUID calls will work. Replace this with
     * your developer ID from the portal.
     */
    public static final String DEVELOPER_ID = "310a8f51-4d6e-4ae5-bda0-b93878e5f5d0";

    /**
     * The application key. This is used to decrypt encrypted receipt responses. This should be replaced with the
     * application key obtained from the OUYA developers website.
     */

    private static final byte[] APPLICATION_KEY = {
            (byte) 0x00,
    };

    /**
     * Before this app will run, you must define some purchasable items on the developer website. Once
     * you have defined those items, put their Product IDs in the List below.
     * <p/>
     * The Product IDs below are those in our developer account. You should change them.
     */

    public static final List<Purchasable> PRODUCT_IDENTIFIER_LIST = Arrays.asList(
        new Purchasable("item-example")
        );

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

    private static final String PRODUCTS_INSTANCE_STATE_KEY = "Products";

    /**
     * The saved instance state key for receipts
     */

    private static final String RECEIPTS_INSTANCE_STATE_KEY = "Receipts";

    /**
     * The ID used to track the activity started by an authentication intent during a purchase.
     */

    private static final int PURCHASE_AUTHENTICATION_ACTIVITY_ID = 1;

    /**
     * The ID used to track the activity started by an authentication intent during a request for
     * the gamers UUID.
     */

    private static final int GAMER_UUID_AUTHENTICATION_ACTIVITY_ID = 2;

    private OuyaFacade ouyaFacade;

    private UserManager userManager;
    private List<Product> mProductList;
    private List<Receipt> mReceiptList;

	//indicates the Unity player has loaded
	private Boolean mEnableUnity = true;

	private Boolean mEnableLogging = false;

	private InputManager mInputManager = null;
	private InputManager.InputDeviceListener minputDeviceListener = null;

	private static ControllerState[] playerStates;

	private String mGamerUuid;

	private IntentFilter accountsChangedFilter;

	private boolean mPaused = false;

    /**
     * The outstanding purchase request UUIDs.
     */

    private final Map<String, String> mOutstandingPurchaseRequests = new HashMap<String, String>();


    /**
     * The cryptographic key for this application
     */

    private PublicKey mPublicKey;

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
        OuyaController.init(this);
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

        // Create a PublicKey object from the key data downloaded from the developer portal.
        try {
            X509EncodedKeySpec keySpec = new X509EncodedKeySpec(APPLICATION_KEY);
            KeyFactory keyFactory = KeyFactory.getInstance("RSA");
            mPublicKey = keyFactory.generatePublic(keySpec);
        } catch (Exception e) {
            Log.e(LOG_TAG, "Unable to create encryption key", e);
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
        // Immediately request an up-to-date copy of receipts.
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
        if (resultCode == RESULT_OK) {
            switch (requestCode) {
                case GAMER_UUID_AUTHENTICATION_ACTIVITY_ID:
                    fetchGamerUUID();
                    break;
                case PURCHASE_AUTHENTICATION_ACTIVITY_ID:
                    restartInterruptedPurchase();
                    break;
            }
    	}
    }

    /**
     * Restart an interrupted purchase
     */

    private void restartInterruptedPurchase() {
        final String suspendedPurchaseId = OuyaPurchaseHelper.getSuspendedPurchase(this);
        if (suspendedPurchaseId == null) {
            return;
        }

        try {
            for (Product thisProduct : mProductList) {
                if (suspendedPurchaseId.equals(thisProduct.getIdentifier())) {
                    requestPurchase(thisProduct.getIdentifier());
                    break;
                }
            }
        }
        catch (Exception ex) {
            Log.e(LOG_TAG, "Error during purchase restart request", ex);
            showError(ex.getMessage());
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
        // reinitialize controllers
        OuyaController.init(this);
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

				showError("Could not fetch product information (error " + errorCode + ":" + errorMessage);
			}

		});
	}

    public boolean isRunningOnOuyaHardware() {
        boolean rc = ouyaFacade.isRunningOnOUYAHardware();
        // The log message is partly here for debugging, partly to remind you not to call this each frame!
        Log.i(LOG_TAG, "ouyaFacade.isRunningOnOuyaHardware returned " + ("" + rc));
        return rc;
    }

    public int getOdkVersionNumber() {
        int rc = ouyaFacade.getOdkVersionNumber();
        Log.i(LOG_TAG, "ouyaFacade.getOdkVersionNumber returned " + ("" + rc));
        return rc;
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
                boolean wasHandledByAuthHelper =
                    OuyaAuthenticationHelper.handleError(
                        OuyaUnityActivity.this,
                        errorCode,
                        errorMessage,
                        optionalData,
                        GAMER_UUID_AUTHENTICATION_ACTIVITY_ID,
                        new OuyaResponseListener<Void>() {
                            @Override
                            public void onSuccess(Void result) {
                                // Retry the fetch if the error was handled
                                fetchGamerUUID();
                            }

                            @Override
                            public void onFailure(int errorCode, String errorMessage, Bundle optionalData) {

                                showError("Unable to fetch gamer UUID (error " + errorCode + ": " + errorMessage + ")");
                            }

                            @Override
                            public void onCancel() {
                                showError("Unable to fetch gamer UUID (Attempt to get account cancelled)");
                            }
                        });
                if (!wasHandledByAuthHelper) {
                    showError("Unable to fetch gamer UUID" + errorCode + ": " + errorMessage + ")");
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
        Gson gson = new Gson();
        String json = gson.toJson(mReceiptList);
        UnityPlayer.UnitySendMessage("OuyaBridge", "didFetchReceipts", json);
	}

	public void requestPurchase(final String productId)
        throws GeneralSecurityException, UnsupportedEncodingException, JSONException {
        SecureRandom sr = SecureRandom.getInstance("SHA1PRNG");

        // This is an ID that allows you to associate a successful purchase with
        // it's original request. The server does nothing with this string except
        // pass it back to you, so it only needs to be unique within this instance
        // of your app to allow you to pair responses with requests.
        String uniqueId = Long.toHexString(sr.nextLong());

        JSONObject purchaseRequest = new JSONObject();
        purchaseRequest.put("uuid", uniqueId);
        purchaseRequest.put("identifier", productId);
        purchaseRequest.put("testing", "true"); // This value is only needed for testing, not setting it results in a live purchase
        String purchaseRequestJson = purchaseRequest.toString();

        byte[] keyBytes = new byte[16];
        sr.nextBytes(keyBytes);
        SecretKey key = new SecretKeySpec(keyBytes, "AES");

        byte[] ivBytes = new byte[16];
        sr.nextBytes(ivBytes);
        IvParameterSpec iv = new IvParameterSpec(ivBytes);

        Cipher cipher = Cipher.getInstance("AES/CBC/PKCS5Padding", "BC");
        cipher.init(Cipher.ENCRYPT_MODE, key, iv);
        byte[] payload = cipher.doFinal(purchaseRequestJson.getBytes("UTF-8"));

        cipher = Cipher.getInstance("RSA/ECB/PKCS1Padding", "BC");
        cipher.init(Cipher.ENCRYPT_MODE, mPublicKey);
        byte[] encryptedKey = cipher.doFinal(keyBytes);

        Purchasable purchasable =
                new Purchasable(
                        productId,
                        Base64.encodeToString(encryptedKey, Base64.NO_WRAP),
                        Base64.encodeToString(ivBytes, Base64.NO_WRAP),
                        Base64.encodeToString(payload, Base64.NO_WRAP) );

        synchronized (mOutstandingPurchaseRequests) {
            mOutstandingPurchaseRequests.put(uniqueId, productId);
        }
        ouyaFacade.requestPurchase(purchasable, new PurchaseListener(productId));
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

			}
		} catch (Exception e) {
			Log.w("Unity", "Exception occurred getting controller state for player " + playerNum + ": " + e.toString());
		}


	    return handled || super.onKeyDown(keyCode, event);
	}

	@Override
	public boolean onKeyUp(int keyCode, KeyEvent event)
	{

        // A special MENU KeyUp event is triggered at the same time as its KeyDown event
        // in the OUYA SDK. We tell the Unity layer to handle this specially and emulate
        // a 1-frame menu button press.
        if (keyCode == OuyaController.BUTTON_MENU) {
            int playerNum = OuyaController.getPlayerNumByDeviceId(event.getDeviceId());
            UnityPlayer.UnitySendMessage("OuyaBridge", "MenuButtonPressed", "" + playerNum);
        }

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
     * Display an error to the user. We're using a toast for simplicity.
     */

    private void showError(final String errorMessage) {
        Toast.makeText(OuyaUnityActivity.this, errorMessage, Toast.LENGTH_LONG).show();
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
			List<Receipt> receipts;
			try {
                JSONObject response = new JSONObject(receiptResponse);
                if (response.has("key") && response.has("iv")) {
                    receipts = helper.decryptReceiptResponse(response, mPublicKey);
                } else {
                    receipts = helper.parseJSONReceiptResponse(receiptResponse);
                }
			} catch (GeneralSecurityException e) {
				throw new RuntimeException(e);
            } catch (JSONException e) {
                if(e.getMessage().contains("ENCRYPTED")) {
                    // This is a hack for some testing code which will be removed
                    // before the consumer release
                    try {
                        receipts = helper.parseJSONReceiptResponse(receiptResponse);
                    } catch (IOException ioe) {
                        throw new RuntimeException(ioe);
                    }
                } else {
                    throw new RuntimeException(e);
                }
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
			showError("Could not fetch receipts (error " + errorCode + ": " + errorMessage + ")");
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
            Product product;
            String id;
			try {
                OuyaEncryptionHelper helper = new OuyaEncryptionHelper();

                JSONObject response = new JSONObject(result);
                if (response.has("key") && response.has("iv")) {
                    id = helper.decryptPurchaseResponse(response, mPublicKey);
                    String storedProductId;
                    synchronized (mOutstandingPurchaseRequests) {
                        storedProductId = mOutstandingPurchaseRequests.remove(id);
                    }
                    if (storedProductId == null || !storedProductId.equals(mProductId)) {
                        onFailure(OuyaErrorCodes.THROW_DURING_ON_SUCCESS, "Purchased product is not the same as purchase request product", Bundle.EMPTY);
                        return;
                    }
                } else {
                    product = new Product(new JSONObject(result));
                    if (!mProductId.equals(product.getIdentifier())) {
                        onFailure(OuyaErrorCodes.THROW_DURING_ON_SUCCESS, "Purchased product is not the same as purchase request product", Bundle.EMPTY);
                        return;
                    }
                }
            } catch (ParseException e) {
                onFailure(OuyaErrorCodes.THROW_DURING_ON_SUCCESS, e.getMessage(), Bundle.EMPTY);
                return;
			} catch (JSONException e) {
                if(e.getMessage().contains("ENCRYPTED")) {
                    // This is a hack for some testing code which will be removed
                    // before the consumer release
                    try {
                        product = new Product(new JSONObject(result));
                        if(!mProductId.equals(product.getIdentifier())) {
                            onFailure(OuyaErrorCodes.THROW_DURING_ON_SUCCESS, "Purchased product is not the same as purchase request product", Bundle.EMPTY);
                            return;
                        }
                    } catch (JSONException jse) {
                        onFailure(OuyaErrorCodes.THROW_DURING_ON_SUCCESS, e.getMessage(), Bundle.EMPTY);
                        return;
                    }
                } else {
                    onFailure(OuyaErrorCodes.THROW_DURING_ON_SUCCESS, e.getMessage(), Bundle.EMPTY);
                    return;
                }
			} catch (IOException e) {
				onFailure(OuyaErrorCodes.THROW_DURING_ON_SUCCESS, e.getMessage(), Bundle.EMPTY);
                return;
			} catch (GeneralSecurityException e) {
				onFailure(OuyaErrorCodes.THROW_DURING_ON_SUCCESS, e.getMessage(), Bundle.EMPTY);
                return;
			}

            // Report success back to Unity
            UnityPlayer.UnitySendMessage("OuyaBridge", "didPurchaseProductId", mProductId);
            // Re-request receipts to keep receipt data up to date
            requestReceipts();
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
            // Suspend failure purchases
            OuyaPurchaseHelper.suspendPurchase(OuyaUnityActivity.this, mProductId);

            boolean wasHandledByHelper =
                OuyaAuthenticationHelper.handleError(
                    OuyaUnityActivity.this,
                    errorCode,
                    errorMessage,
                    optionalData,
                    PURCHASE_AUTHENTICATION_ACTIVITY_ID,
                    new OuyaResponseListener<Void>() {
                        @Override
                        public void onSuccess(Void result) {
                            restartInterruptedPurchase(); // Retry the purchase
                        }

                        @Override
                        public void onFailure(int errorCode, String errorMessage, Bundle optionalData) {
                            showError("Unable to make purchase (error " +
                                                    errorCode + ": " + errorMessage + ")");
                        }

                        @Override
                        public void onCancel() {
                            showError("Unable to make purchase");
                        }
                    });
            if (!wasHandledByHelper) {

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
                                try {
                                    requestPurchase(mProductId);
                                }
                                catch (Exception e) {
                                    Log.e(LOG_TAG, "Error during purchase", e);
                                    showError(e.getMessage());
                                }
                            }
                        })
                        .setNegativeButton(R.string.cancel, null)
                        .show();
            }
	    }
	}
}