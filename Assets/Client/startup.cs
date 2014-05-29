﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using gsFramework;
using SimpleJSON;

/* startup is the main class for the client. It holds information
 * about the player(ID) and queries the server for map data. It also
 * holds information about the players GUI interaction (selection).
 */ 
public class gameState {
	public int dimensions;
	public List<SolReg> regions;
	public void Update(byte[] data) {
		char[] chars = new char[data.Length / sizeof(char)];
		System.Buffer.BlockCopy (data, 0, chars, 0, data.Length);
		string data_str = new string (chars);
		//Deserialize the json array into gamedata.
		JSONArray s = JSON.Parse (data_str).AsArray;
		dimensions = s [0].AsInt;
		/* Naiive game-state systems read in the entire
		 * game state each frame or tick. This needs to be
		 * implemented using a much more efficient algorithm
		 * (only send players what they need/make client-side
		 * predictions.
		 */
		foreach (JSONNode node in s[1].AsArray) {
			SolReg r = new SolReg();
			r.sector = node[0].AsInt;
			r.id = node[1].AsInt;
			r.x = node[2].AsFloat;
			r.z = node[3].AsFloat;
			r.scale = node[4].AsFloat;
			r.texture = "texture" + node[5].AsInt.ToString ();
			r.owner = node[6].AsInt;
			r.income = node[7].AsInt;
			r.slots = node[8].AsInt;
			foreach(JSONNode adj in node[9].AsArray) {
				r.adjacent.Add (adj.AsInt);
			}
		}
	}
}

public class startup : MonoBehaviour {
	public SolReg selectedSR;
	GameObject selector;
	GameObject selected;
	GameObject guiController;

	gameState state;
	private enum Actions
		{
		ACTION_SEND_CHAT,
		ACTION_GET_CHAT,
		ACTION_UPDATE_GAMESTATE,
		ACTION_PLAYER_EVENT
		}
	private string server_url = "http://deco3800-14.uqcloud.net/game.php";
	private Hashtable header = new Hashtable ();
	void Startup() {
		header.Add ("Content-Type", "text/json");
		state = new gameState ();
	}

	public IEnumerator UpdateGame() {
		string request = "{action:" + Actions.ACTION_UPDATE_GAMESTATE + "}";
		byte[] data = new byte[request.Length * sizeof(char)];
		System.Buffer.BlockCopy (request.ToCharArray (), 0, data, 0, data.Length);
		header["Content-Length"] = data.Length;
		WWW www = new WWW (server_url, data, header);
		yield return www;
		state.Update (www.bytes);
	}

	private int tick = 0; 
	private int tick_step = 100; //How often to request the game state.
	void Update() {
		/* Apply the current game state to the locally running game. */
		if (tick % tick_step == 0) {
			StartCoroutine (UpdateGame ());
			tick = 0;
		}
		tick++;
	}

	public void SelectSR(SolReg srSelection) {
		// If a previous selection exists, deselect. 
		if (selectedSR.id != 0) {
			print(selectedSR.id);
			selected.GetComponent<planetScript>().Deselect();
		}

		print("STARTUP: Selected Region "+srSelection.id);

		// Update current selection in this script.
		selectedSR = srSelection;
		selected = GameObject.Find("Region "+selectedSR.id);

		// Tell GUI which region is selected.
		guiController = GameObject.Find("MainGUI");
		guiController.GetComponent<MainGUI>().selectedSR = selectedSR;
	}

	GameObject CreateSelector() {
		GameObject selector = GameObject.CreatePrimitive (PrimitiveType.Sphere);
		
		// selector is invisible until a selection is made
		selector.renderer.enabled = false;
		
		return selector;
	}

	void UpdateSelector(SolReg SR) {
		selector.renderer.enabled = true;
		selector.transform.localPosition = new Vector3(SR.x, 0.1f, SR.z);
		selector.transform.localScale = new Vector3(1.8f, 0, 1.8f);
		selector.renderer.material.color = Color.white;
	}

	void DrawRegion (SolReg region) {
		GameObject sphere = GameObject.CreatePrimitive (PrimitiveType.Sphere);
		sphere.transform.localPosition = new Vector3(region.x, 0, region.z);
		sphere.transform.localRotation = Quaternion.Euler (90, 0, 0);

		Material mat = Resources.Load (region.texture, typeof(Material)) as Material;
		sphere.renderer.material = mat;
		Texture2D tex = Resources.Load (region.texture, typeof(Texture2D)) as Texture2D;
		sphere.renderer.material.mainTexture = tex;


		// attach script to the new sphere
		sphere.AddComponent("planetScript");
		
		// pass reference to region to the script
		sphere.GetComponent<planetScript>().SetSR(region);

		// give the GO a meaningful name
		sphere.name = "Region "+ region.id;
	}
}
