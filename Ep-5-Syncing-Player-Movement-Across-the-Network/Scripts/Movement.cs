using UnityEngine;
using Unity.Netcode;

public class Movement : NetworkBehaviour
{

	public float characterSpeed = 0f; //Speed of character calculated at runtime
	public float movementSpeed = 10.0f; //Desired movement speed (forward-back-left right) of character
	public float characterRotationSpeed = 0f; //Rotation speed of character calculated at runtime
	public float rotationSpeed = 100.0f; //Desired rotation/turning speed of character

	public NetworkVariable<Vector3> Position = new NetworkVariable<Vector3>();
	public NetworkVariable<Quaternion> Rotation = new NetworkVariable<Quaternion>();

	//Update is called once per frame
	void Update()
	{
		if (IsLocalPlayer)
		{
			characterSpeed = movementSpeed * Time.deltaTime; //Calculates speed of character dependent of framerate
			characterRotationSpeed = rotationSpeed * Time.deltaTime; //Calculates rotation speed of character dependent of framerate

			var x = Input.GetAxis("Horizontal") * characterRotationSpeed;
			var z = Input.GetAxis("Vertical") * characterSpeed;

			transform.Rotate(0, x, 0);
			transform.Translate(0, 0, z);

			UpdatePlayerPositionServerRpc(transform.position.x, transform.position.y, transform.position.z);
			UpdatePlayerRotationServerRpc(transform.rotation);

		} else
        {
			transform.position = Position.Value;
			transform.rotation = Rotation.Value;
        }

	}

	[ServerRpc]
	void UpdatePlayerPositionServerRpc(float xPos, float yPos, float zPos)
    {
		Position.Value = new Vector3(xPos, yPos, zPos);
    }

	[ServerRpc]
	void UpdatePlayerRotationServerRpc(Quaternion rotation)
    {
		Rotation.Value = rotation;
    }
}
