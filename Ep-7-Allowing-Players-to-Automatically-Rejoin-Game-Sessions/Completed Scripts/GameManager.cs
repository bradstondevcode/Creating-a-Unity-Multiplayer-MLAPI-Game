using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class GameManager : MonoBehaviour {

	//[SyncVar(hook = "OnChangeTime")]
    public float gameTimeLeft = 0f;
	public float timeLeft = 0f;
	public float totalGameMinutes = 3f;

	public bool gameStarted = false;
	public Text gameTime;

	public int totalSpots = 0;
	public int parkingSpotsReached = 0;
	public Text parkingSpotsLeft;

	public float powerPoints = 0f;
	public float powerPointsFactor = 2.5f;
	public Text powerPointsCollected;

	public Dictionary<string, PlayerSessionData> savedPlayerSessionData = new Dictionary<string, PlayerSessionData>();

	// Use this for initialization
	void Start () {
		gameStarted = true;

		timeLeft = 60 * totalGameMinutes;
		gameTime.text = "Time Left: " + timeLeft.ToString("N0") + " Seconds";

		//Check for saved PlayerID
		string playerID = PlayerPrefs.GetString("playerID");

		//If playerID doesn't exist generate one and save it to PlayerPrefs
		if (playerID == "")
        {
			Debug.Log("Player ID is null");
			string newPlayerID = System.Guid.NewGuid().ToString();
			PlayerPrefs.SetString("playerID", newPlayerID);
            PlayerPrefs.Save();
        }
		else
        {
			Debug.Log("Current PlayerID: " + playerID);
        }

	}

	// Update is called once per frame
	void Update () {

	}

    void OnParkingSpotsReachedChanged(int parkingSpots){
		parkingSpotsLeft.text = "Parking Spots: " + parkingSpots + "/" + totalSpots;
	}

	void OnPowerPointsChanged(float powerPointsCol){
		powerPointsCollected.text = "Power: " + powerPointsCol.ToString("N0");
	}
}
