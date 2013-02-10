using UnityEngine;
using System.Collections;

public class ShowReceiptsAndProducts : MonoBehaviour {
	
	void OnGUI() {
		
		GUILayout.BeginArea(new Rect(Screen.width/2, 0, Screen.width/2, Screen.height));
		GUILayout.Label("Products:\n");
		foreach (OuyaBridge.Product product in OuyaBridge.products) {
			GUILayout.BeginHorizontal();
			GUILayout.Label(product.ToString());
			if (GUILayout.Button("Purchase")) {
				Debug.Log("Would purchase product");
				OuyaBridge.PurchaseProduct(product.identifier);
			}
			GUILayout.EndHorizontal();
		}	
		GUILayout.Label("Receipts:\n");
		foreach (OuyaBridge.Receipt receipt in OuyaBridge.receipts) {
			GUILayout.Label(receipt.ToString());
		}	
		
		if (GUILayout.Button("Fetch Gamer UUID")) {
			OuyaBridge.FetchGamerUUID();	
		}
		if (OuyaBridge.activeGamerUuid != null) {
			GUILayout.Label("Retrieved Gamer UUID: " + OuyaBridge.activeGamerUuid);
		}
		if (GUILayout.Button("Refresh Receipts")) {
			OuyaBridge.RefreshReceipts();
		}
		GUILayout.EndArea();
			
		
	}
}
