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

	public override void OnNetworkSpawn()
    {
        //base.OnNetworkSpawn();

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
			GetComponent<Drive>().enabled = false;
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
		
}
