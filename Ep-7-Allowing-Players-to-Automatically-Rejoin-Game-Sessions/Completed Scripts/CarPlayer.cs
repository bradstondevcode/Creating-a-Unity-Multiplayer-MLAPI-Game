using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class CarPlayer : NetworkBehaviour{

	public MeshRenderer carMesh;
	public MeshRenderer mapMarkerMesh;
	public float jumpForce = 2500f;
	public float boostForce = 2500f;
	public bool canJump = false;
	public bool canBoost = false;
	public GameObject playerCamera;

	public GameObject abilityPrefab;
	public Transform abilitySpawnPoint;
	public string abilitySpawnName;
	public float abilityForce = 0.0f;

	NetworkVariable<System.Guid> playerID = new NetworkVariable<System.Guid>();

	GameManager gameManager;

	public override void OnNetworkSpawn()
    {
		string loadedPlayerID = PlayerPrefs.GetString("playerID");

		gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

		if (IsLocalPlayer)
		{
			Debug.Log("Local Player is Setting Player ID : " + loadedPlayerID);
			SetPlayerIDServerRpc(loadedPlayerID);
		} else
        {
			if(IsServer)
				NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
		}

        GameObject.Find("Scene Camera").GetComponent<Camera>().enabled = false;
		GameObject.Find("Scene Camera").GetComponent<AudioSource>().enabled = false;
		GameObject.Find("Scene Camera").GetComponent<AudioListener>().enabled = false;

		GameObject playerCamera = gameObject.transform.Find("Player Camera").gameObject;
		playerCamera.SetActive(true);

		//Find car body transform nested in CarPlayer my searching full heirarchy
		Transform[] children = GetComponentsInChildren<Transform>();
		foreach (Transform child in children)
		{
			if (child.name == "car_body")
			{
				carMesh = child.gameObject.GetComponent<MeshRenderer>();
			}
			if (child.name == "Map-Marker")
			{
				mapMarkerMesh = child.gameObject.GetComponent<MeshRenderer>();
			}

			if (!IsLocalPlayer)
			{
				if (child.name == "CarMusicPlayer")
				{
					child.gameObject.GetComponent<AudioSource>().enabled = false;
				}
			}
		}

		Color randomColor = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
		carMesh.material.color = randomColor;
		if (mapMarkerMesh)
		{
			mapMarkerMesh.material.color = randomColor;
		}

		if (!IsLocalPlayer)
		{
			if (gameObject.transform.Find("Player Camera"))
			{
				Destroy(gameObject.transform.Find("Player Camera").gameObject);

			}

			GetComponent<AudioSource>().enabled = false;
			//GetComponent<Drive>().enabled = false;
			//GetComponent<Movement>().enabled = false;
			return;
		}

	}

	// Update is called once per frame
	void Update () {

		if (IsLocalPlayer)
        {
			if (Input.GetKeyDown(KeyCode.Space))
            {
				fireRocketServerRpc();
            }

			if (Input.GetKeyDown(KeyCode.Escape))
			{
				NetworkManager.Singleton.Shutdown();
			}

		}

	}

    public override void OnDestroy(){
		if (IsLocalPlayer) {
			GameObject.Find("Scene Camera").GetComponent<Camera>().enabled = true;
			GameObject.Find("Scene Camera").GetComponent<AudioSource>().enabled = true;
			GameObject.Find("Scene Camera").GetComponent<AudioListener>().enabled = true;
			if (playerCamera != null)
			{
				Debug.Log("Destroyed " + transform.name + "'s camera! Muhahahahaha!");
				Destroy(playerCamera);
			}
		}else
        {
			if (IsServer)
				NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
		}
	}

	[ServerRpc]
	void fireRocketServerRpc()
	{
		Debug.Log("This is Firing!");

		if (abilityPrefab != null)
		{
			abilitySpawnPoint = transform.Find(abilitySpawnName).transform;

			// Create the Bullet from the Bullet Prefab
			var abilityObject = (GameObject)Instantiate(
				abilityPrefab,
				abilitySpawnPoint.position,
				abilitySpawnPoint.rotation);

			abilityObject.GetComponent<NetworkObject>().Spawn();

			// Add velocity to the bullet
			abilityObject.GetComponent<Rigidbody>().velocity = abilityObject.transform.forward * abilityForce;

		}
	}

	[ServerRpc]
	void SetPlayerIDServerRpc(string guidString)
    {
		playerID.Value = System.Guid.Parse(guidString);

		if (gameManager.savedPlayerSessionData.ContainsKey(guidString))
        {
			PlayerSessionData psd = gameManager.savedPlayerSessionData[guidString];
			GetComponent<Drive>().Position.Value = psd.lastPos;
			GetComponent<Drive>().Rotation.Value = psd.lastRot;
			return;
		}
		
	}

	void OnClientDisconnectCallback(ulong clientId)
	{
		if (IsServer)
		{
			PlayerSessionData playerSessionData = new PlayerSessionData();
			playerSessionData.playerID = playerID.Value.ToString();

			Drive drive = GetComponent<Drive>();

			playerSessionData.lastPos = drive.Position.Value;
			playerSessionData.lastRot = drive.Rotation.Value;

			if (gameManager.savedPlayerSessionData.ContainsKey(playerSessionData.playerID))
            {
				gameManager.savedPlayerSessionData[playerSessionData.playerID] = playerSessionData;
			}
            else
            {
				gameManager.savedPlayerSessionData.Add(playerSessionData.playerID, playerSessionData);

			}

		}
	}

}
