using UnityEngine;
using System.Collections;
using Unity.Netcode;

public enum GearBox
{
	NEUTRAL,
	BRAKE,
	DRIVE,
	REVERSE
}

public class Drive : NetworkBehaviour {

	public GearBox currentGear;
	public Rigidbody rigidBody;
	public float velocity;
	public float maxVelocityAchieved;
	public float maxVelocityPossible = 55f;

	private WheelCollider[] wheels;
	public GameObject wheelShape;

	public float maxAngle = 30;
	public float currentAngle;
	public float maxTorque = 300;
	public float currentTorque;

	public float acceleration = 100f;
	private bool isBraking = false;
	public float maxBrakeTorque = 100f;

	public float minAccelerationVelocity = 5f;
	public float minimumBrakeVelocity = 2f;

	public float horizontalInput;
	public float verticalInput;

	public bool isGrounded = true;
	public float airborneTimer = 0.0f;
	public float airborneMinTime = 1.0f;

	public float midairRollFactor = 1f;
	public float maxMidairRollFactor = 10f;
	public float midairRollIncreaseFactor = 1.0f;

	public float accelBoostforce = 1000.0f;
	public ForceMode accelBoostForceMode;

	public Transform target;
	public float flightCorrectionSpeed = 0.1f;
	public float lerpValue = 0f;

	public float[] gearShiftVelocity;
	public int gearShiftIndex;

	public float currentGearVelocityMinusPlayerVelocity;
	public float currentGearVelocityMinusPreviousGearVelocity;
	public float currentGearVelocityPercentage;
	public float currentGearVolocityPercentageModifiter = 0.5f;

	public float maxReverseToForwardDelay = 0.3f;
	public float reverseToForwardDelay = 0.3f;

	private CarAudio carAudioScript;

	public bool isAccelerating;

	public GameManager gameManager;

	public NetworkVariable<Vector3> Position = new NetworkVariable<Vector3>();
	public NetworkVariable<Quaternion> Rotation = new NetworkVariable<Quaternion>();

	bool isLoading = true;

	// here we find all the WheelColliders down in the hierarchy
	public void Start()
	{
		rigidBody = GetComponent<Rigidbody>();
		wheels = GetComponentsInChildren<WheelCollider>();
		carAudioScript = GetComponent<CarAudio>();

        gameManager = GameObject.Find ("GameManager").GetComponent<GameManager> ();
	}

	public override void OnNetworkSpawn()
	{
		//Wait a short time before running update code (For loading in any needed pre-configurations
		Invoke("FinishedLoading", 1.0f);
	}

	void FinishedLoading()
    {
		isLoading = false;
    }

	public void Update()
	{

		if (!IsLocalPlayer)
        {
			transform.position = Position.Value;
			transform.rotation = Rotation.Value;
			return;
		}

		if (isLoading)
		{
			transform.position = Position.Value;

			if (Rotation.Value.x != 0)
				transform.rotation = Rotation.Value;

			return;
		}

		horizontalInput = Input.GetAxis ("Horizontal");
		verticalInput = Input.GetAxis ("Vertical");

		//Always track player horizontal input
		currentAngle = maxAngle * horizontalInput;

		if (isGrounded) {
			airborneTimer = 0.0f;
			midairRollFactor = 0.0f;
			currentTorque = maxTorque * verticalInput;
			GetComponent<Rigidbody> ().freezeRotation = false;
			lerpValue = 0f;

			Vector3 currentInputs = new Vector3 (horizontalInput, 0, verticalInput);

			Vector3 velocityXY = new Vector3 (rigidBody.velocity.x, 0, rigidBody.velocity.z);
			velocity = velocityXY.magnitude;

			//Lazy way to debug greatest acheived speed thus far
			if(velocity > maxVelocityAchieved)
			{
				maxVelocityAchieved = velocity;
			}

			if (currentInputs == Vector3.zero
			    && currentTorque == 0) {
				if (currentGear != GearBox.NEUTRAL) {
					currentGear = GearBox.NEUTRAL;
				}
			} else {
				CheckCurrentGear ();
			}

			if (verticalInput < 0) {

				if (velocity > minimumBrakeVelocity) {
					currentTorque -= maxBrakeTorque * Time.deltaTime;

					if (currentGear != GearBox.REVERSE) {
						if (currentGear != GearBox.BRAKE) {
							currentGear = GearBox.BRAKE;
							isAccelerating = false;
						}
					}
				} else {
					if (currentGear != GearBox.REVERSE) {
						currentGear = GearBox.REVERSE;
						isAccelerating = true;

						if(reverseToForwardDelay < maxReverseToForwardDelay)
						{
							reverseToForwardDelay = maxReverseToForwardDelay;
						}
					}
				}
			}

			if (verticalInput > 0) {

				if (velocity < minimumBrakeVelocity) {
					GetComponent<Rigidbody>().AddForce (transform.forward * accelBoostforce, accelBoostForceMode);
				}

				if (velocity < -minimumBrakeVelocity) {
					if (currentGear != GearBox.DRIVE) {
						if (currentGear != GearBox.BRAKE) {
							currentGear = GearBox.BRAKE;
							isAccelerating = false;
						}
					}
				} else { 
					if (currentGear != GearBox.DRIVE) {
						currentGear = GearBox.DRIVE;
					}

					if(velocity < gearShiftVelocity[gearShiftIndex] && gearShiftIndex < gearShiftVelocity.Length)
					{
						isAccelerating = true;
					}
					else
					{
						isAccelerating = false;
					}
				}

				if (velocity < minAccelerationVelocity) {
					currentTorque += acceleration * Time.deltaTime;
				}
			}
				
			//Track velocity in order to create relative audio

			if(velocity > gearShiftVelocity[gearShiftIndex])
			{
				if(verticalInput > 0)
				{
					if(gearShiftIndex < gearShiftVelocity.Length - 1)
					{
						ShiftUp();
					}
					else
					{
						isAccelerating = true;
					}
				}
			}
			else
			{
				//Always get distanct from current velocity to next velocity
				currentGearVelocityMinusPlayerVelocity = gearShiftVelocity[gearShiftIndex] - velocity;
				currentGearVelocityMinusPlayerVelocity = Mathf.Clamp(currentGearVelocityMinusPlayerVelocity, 0, maxVelocityPossible);
				currentGearVelocityPercentage = 1.0f - (currentGearVelocityMinusPlayerVelocity / currentGearVelocityMinusPreviousGearVelocity) + currentGearVolocityPercentageModifiter;
				currentGearVelocityPercentage = Mathf.Clamp(currentGearVelocityPercentage, 0, carAudioScript.maxPitch);

				if(isAccelerating)
				{
					if(gearShiftIndex > 0)
					{
						currentGearVelocityMinusPreviousGearVelocity = gearShiftVelocity[gearShiftIndex] - gearShiftVelocity[gearShiftIndex -1];
					}
					else
					{
						currentGearVelocityMinusPreviousGearVelocity = 1;
					}
				}
			}

			if(verticalInput <= 0)
			{
				if(verticalInput == 0)
				{
					isAccelerating = false;
				}
				else
				{
					isAccelerating = true;
					if(gearShiftIndex > 0)
					{
						if(velocity < gearShiftVelocity[gearShiftIndex])
						{
							ShiftDown();
						}
					}
				}
			}

		} else {

			airborneTimer += Time.deltaTime;
			gameManager.powerPoints += Time.deltaTime * gameManager.powerPointsFactor;

			if (airborneTimer > airborneMinTime) {
				GetComponent<Rigidbody> ().freezeRotation = false;
				if (midairRollFactor < maxMidairRollFactor) {
					midairRollFactor += midairRollIncreaseFactor * Time.deltaTime;
				}

				transform.Rotate (verticalInput * midairRollFactor, 0, -horizontalInput * midairRollFactor);
			} else {

				//Stop players rigid body rotation so they have more control in flight
				GetComponent<Rigidbody> ().freezeRotation = true;

				//When player first begins his flight, correct their rotation to allow for them a better chance to land upright
				lerpValue += Time.deltaTime * flightCorrectionSpeed;
				Vector3 targetDir = target.position - transform.position;
				float step = flightCorrectionSpeed * Time.deltaTime;
				Vector3 newDir = Vector3.RotateTowards(transform.forward, targetDir, step, 0.0F);
				transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(newDir), lerpValue);
			}

			foreach (WheelCollider wheel in wheels) {
				wheel.motorTorque = 0;
			}
		}

		foreach (WheelCollider wheel in wheels) {
			// a simple car where front wheels steer while rear ones drive
			if (wheel.transform.localPosition.z > 0)
				wheel.steerAngle = currentAngle;

			//				if (wheel.transform.localPosition.z < 0)
			wheel.motorTorque = currentTorque;

			// update visual wheels if any
			if (wheelShape) {
				Quaternion q;
				Vector3 p;
				wheel.GetWorldPose (out p, out q);

				// assume that the only child of the wheelcollider is the wheel shape
				Transform shapeTransform = wheel.transform.GetChild (0);
				shapeTransform.position = p;
				shapeTransform.rotation = q;
			}

		}

		UpdatePlayerPositionServerRPC(transform.position.x, transform.position.y, transform.position.z);
		UpdatePlayerRotationServerRPC(transform.rotation);
	}

	void CheckCurrentGear()
	{
		//Brake
		if(currentGear == GearBox.BRAKE)
		{
			foreach (WheelCollider wheel in wheels)
			{
				if(wheel.brakeTorque != maxBrakeTorque)
				{
					wheel.brakeTorque = maxBrakeTorque;
				}
			}

			isBraking = true;
		}
		else
		{
			if(isBraking)
			{
				foreach (WheelCollider wheel in wheels)
				{
					if(wheel.brakeTorque != 0)
					{
						wheel.brakeTorque = 0;
					}
				}

				isBraking = false;
			}
		}
	}

	void ShiftUp()
	{
		if(gearShiftIndex < gearShiftVelocity.Length -1)
		{
			Debug.Log("Shift up");
			gearShiftIndex ++;
			carAudioScript.ShiftGearsUP();
		}
	}

	void ShiftDown()
	{
		if(gearShiftIndex > 0)
		{
			Debug.Log("Shift down");
			gearShiftIndex --;
			carAudioScript.ShiftGearsDown();
		}
	}

	[ServerRpc]
	void UpdatePlayerPositionServerRPC(float xPos, float yPos, float zPos)
	{
		Position.Value = new Vector3(xPos, yPos, zPos);
	}

	[ServerRpc]
	void UpdatePlayerRotationServerRPC(Quaternion rotation)
	{
		Rotation.Value = rotation;
	}

}
