using UnityEngine;
using System.Collections;

public class ShowReceiptsAndProducts : MonoBehaviour {
	
	bool clickedThisFrame = false;
	
	float okWidth = 1280f;
	float okHeight = 720f;
	
	void Start() {
		
		okWidth = Screen.width*0.9f;
		okHeight = Screen.height*0.9f;
	}
	
	void Update() {
		// Workaround for weird UnityGUI behavior of the OUYA virtual mouse. It seems to be passing
		// click events multiple times in a single frame!
		clickedThisFrame = false;
	}
	
	void OnGUI() {
		
		GUILayout.BeginArea(new Rect(Screen.width*0.05f + okWidth/2, Screen.height*0.05f, okWidth/2, okHeight));
		GUILayout.Label("Products:\n");
		foreach (OuyaBridge.Product product in OuyaBridge.products) {
			GUILayout.BeginHorizontal();
			GUILayout.Label(product.ToString());
			if (GUILayout.Button("Purchase") && !clickedThisFrame) {
				Debug.Log(Time.frameCount + " Would purchase product");
				OuyaBridge.PurchaseProduct(product.identifier);
				clickedThisFrame = true;
			}
			GUILayout.EndHorizontal();
		}	
		GUILayout.Label("Receipts:\n");
		foreach (OuyaBridge.Receipt receipt in OuyaBridge.receipts) {
			GUILayout.Label(receipt.ToString());
		}	
		
		if (GUILayout.Button("Fetch Gamer UUID") && !clickedThisFrame) {
			Debug.Log (Time.frameCount + " fetching gamer UUID");
			OuyaBridge.FetchGamerUUID();	
			clickedThisFrame = true;
		}
		if (OuyaBridge.activeGamerUuid != null) {
			GUILayout.Label("Retrieved Gamer UUID: " + OuyaBridge.activeGamerUuid);
		}
		if (GUILayout.Button("Refresh Receipts") && !clickedThisFrame) {
			OuyaBridge.RefreshReceipts();
			clickedThisFrame = true;
		}
		GUILayout.EndArea();
			
		
	}
}
