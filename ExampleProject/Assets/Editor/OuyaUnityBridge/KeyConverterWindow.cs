using UnityEngine;
using System.Collections;
using UnityEditor;

public class KeyConverterWindow : EditorWindow {
	
    [MenuItem ("Window/Key Converter")]
    public static void ShowWindow () {
        EditorWindow.GetWindow(typeof(KeyConverterWindow));
    }
	
	
	string derText = "";
	
	Rect windowRect = new Rect(20, 20, 400, 400);
	
	void OnGUI() {
		
		GUILayout.Label ("Enter Key Here.");
		derText = GUILayout.TextArea(derText);
		
		if (derText != "") {
			GUILayout.Label("Java code (Copy and Paste):");
			GUILayout.TextArea(ConvertDerText(derText));
		}
		
		
	}
	
	
	string ConvertDerText(string derText) {
		int linePos = 0;
		string converted = "";
		string[] lines = derText.Split("\n"[0]);
		foreach (string line in lines) {
			string[] quads = line.Split (" "[0]);
			foreach (string quad in quads) {
				if (quad.Length == 4) {
					converted += "(byte) 0x" + quad.Substring(0,2) + ",(byte) 0x" + quad.Substring(2) + ",";
					linePos = (linePos + 1) % 4;
					if (linePos == 0) converted += "\n";
				}
			}
		}
		return converted;
	}
	
}
