using UnityEngine;

/**
 * Made by Jasper Flick A.K.A CatLikeCoding
 * https://catlikecoding.com/unity/tutorials/movement/
 * 
 * Adapted by Alex Humphries
 * 
 * 3D Character Controller
 */
public class MovingSphere : MonoBehaviour
{
	[SerializeField, Range(0f, 100f), Tooltip("How fast the sphere is. Used to calculate velocity.")]
	float maxSpeed = 10f;

	[SerializeField, Range(0f, 100f), Tooltip("How fast the sphere is while climbing. Used to calculate velocity.")]
	float maxClimbSpeed = 2f;

	[SerializeField, Range(0f, 100f), Tooltip("How fast the sphere is while swimming. Used to calculate velocity.")]
	float maxSwimSpeed = 5f;

	[SerializeField, Range(0f, 100f), Tooltip("Controls how quickly we reach max speed.")]
	float maxAcceleration = 10f;

	[SerializeField, Range(0f, 100f), Tooltip("Controls how much movement sphere has in air.")]
	float maxAirAcceleration = 1f;

	[SerializeField, Range(0f, 100f), Tooltip("Controls how quickly we reach top speed while climbing.")]
	float maxClimbAcceleration = 20f;

	[SerializeField, Range(0f, 100f), Tooltip("Controls how quickly we reach top speed while swimming.")]
	float maxSwimAcceleration = 5f;

	[SerializeField, Range(0f, 10f), Tooltip("Controls how high the spehere jumps.")]
	float jumpHeight = 2f;

	[SerializeField, Range(0, 5), Tooltip("Controls how many jumps the sphere has after an initial jump.")]
	int maxAirJumps = 2;

	[SerializeField, Range(0f, 90f),
		Tooltip("Controls the angle in which the sphere is considered 'on the ground'." +
		" Any slope above this angle will not be considered ground.")]
	float maxGroundAngle = 25f;

	[SerializeField, Range(0f, 90f), Tooltip("Controls the angle in which the sphere is considered 'on stairs'." +
		" Any slope above this angle will not be considered on stairs. Allows for a secondary angle for steeper stair slopes")]
	float maxStairsAngle = 50f;

	[SerializeField, Range(90, 180), Tooltip("Controls the angle in which the sphere can climb a given surface.")]
	float maxClimbAngle = 140f;

	[SerializeField, Range(0f, 100f), Tooltip("The speed the sphere needs to move at to miss snapping to the ground.")]
	float maxSnapSpeed = 100f;

	[SerializeField, Min(0f), Tooltip("Distance to shoot the ray to check for snapping to ground.")]
	float probeDistance = 1f;

	[SerializeField, Tooltip("Offset from the center of the sphere, dedicating a point to measure range.")]
	float submergenceOffset = 0.5f;

	[SerializeField, Min(0.1f), Tooltip("Range from offset to end of sphere.")]
	float submergenceRange = 1f;

	[SerializeField, Min(0f), Tooltip("How much the sphere floats. A value of 0 sinks like a rock, a value of 1 is in equilibrium," +
		" anything greater than 1 will float to the surface. A buoyancy of 2 would mean it rises as fast as it would normally fall.")]
	float buoyancy = 1f;

	[SerializeField, Range(0f, 10f), Tooltip("Drag placed on the sphere while in water.")]
	float waterDrag = 1f;

	[SerializeField, Range(0.01f, 1f), Tooltip("Can only swim if we're deep enough into water, but we don't need to be fully submered" +
		"Defines the minimum submergence required for swimming. Must be greater than 0.")]
	float swimThreshhold = 0.5f;

	[SerializeField, Tooltip("Include any ground objects - things the sphere can snap to. Used in raycast.")]
	LayerMask probeMask = -1;

	[SerializeField, Tooltip("Used to determine if we're on stairs or not, so we can use the stair angle. Assign to stairs layer.")]
	LayerMask stairsMask = -1;

	[SerializeField, Tooltip("Used to determine if we're on a climbable surface or not, so we can climb it. Assign to climbable layer.")]
	LayerMask climbMask = -1;

	[SerializeField, Tooltip("Used to determine if we're in swimmable water, so we can swim in it it. Assign to water layer.")]
	LayerMask waterMask = 0;

	[SerializeField, Tooltip("Custom space to define player input")]
	Transform playerInputSpace = default;

	[SerializeField, Tooltip("The 'normal' material of the sphere, changes depending on the state of the sphere. Will be changed to anims.")]
	Material normalMaterial = default;

	[SerializeField, Tooltip("The climbing material of the sphere, changes when the sphere is climbing. Will be changed to anims.")]
	Material climbingMaterial = default;

	[SerializeField, Tooltip("The swimming material of the sphere, changes when the sphere is swimming. Will be changed to anims.")]
	Material swimmingMaterial = default;


	// TODO replace with animations
	MeshRenderer meshRenderer;

	// Part of collision with ground / stairs / climbing detection
	float minGroundDotProduct, minStairsDotProduct, minClimbDotProduct;

	// Velocity and what we want velocity to be
	Vector3 velocity, connectionVelocity;

	Vector3 playerInput;

	// Are we trying to jump?
	bool desiredJump;

	// Are we trying to climb?
	bool desiresClimbing;

	// Direction away from collision. I.e. contact normal ~ opposite direction of
	// when colliding with ground. Steep is opposite when colliding with slopes,
	// climb is when colliding with walls
	Vector3 contactNormal, steepNormal, climbNormal, lastClimbNormal;

	// Number of collision contacts for ground, slopes and walls
	int groundContactCount, steepContactCount, climbContactCount;

	// If we have collision contacts on the ground OnGround is true.
	bool OnGround => groundContactCount > 0;

	// If we have collision contacts on the walls OnSteep is true.
	bool OnSteep => steepContactCount > 0;

	// If we have collision contacts on the walls Climbing is true.
	bool Climbing => climbContactCount > 0 && stepsSinceLastJump > 2;

	// Whether we are in water or not
	bool InWater => submergence > 0f;

	// Whether we are swimming (submergence is greater than the threshold needed for swimming)
	bool Swimming => submergence >= swimThreshhold;

	// How much the sphere is in the water, where 0 is not in the water at all,
	// and 1 is fully submerged.
	float submergence;

	// Tracks how many jumps we've used.
	int jumpPhase;

	// Counts how many 'steps' we've taken since we've been on the ground
	// How many frames
	int stepsSinceLastGrounded = 0;
	// Counts how many 'steps' we've taken since we've last jumped
	// How many frames
	int stepsSinceLastJump = 0;

	Rigidbody body, connectedBody, previousConnectedBody;

	// overriding Unity's built in axises for custom gravity
	Vector3 upAxis, rightAxis, forwardAxis;

	// World position of the body that we are connected to
	Vector3 connectionWorldPosition, connectionLocalPosition;
	
	private void OnValidate()
	{
		// Calculate the min dot products for ground and stairs on first awake
		minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
		minStairsDotProduct = Mathf.Cos(maxStairsAngle * Mathf.Deg2Rad);
		minClimbDotProduct = Mathf.Cos(maxClimbAngle * Mathf.Deg2Rad);
	}

	private void Awake()
	{
		body = GetComponent<Rigidbody>();
		body.useGravity = false;
		meshRenderer = GetComponent<MeshRenderer>();
		OnValidate();
	}

	private void Update()
	{
		// Input gathering
		playerInput.x = Input.GetAxis("Horizontal");
		playerInput.y = Input.GetAxis("Vertical");
		playerInput.z = Swimming ? Input.GetAxis("UpDown") : 0f;
		// Clamp input so diagonals aren't faster
		playerInput = Vector3.ClampMagnitude(playerInput, 1f);

		/*
		 * If a player input space exists then we project its right and forward vectors
		 * on the gravity plane to find the gravity-aligned X and Z axes.
		 * Otherwise we project the world axes. The desired velocity is now
		 * defined relative to these axes, so the input vector need not be converted to a different space.
		 */
		if (playerInputSpace)
		{
			rightAxis = ProjectDirectionOnPlane(playerInputSpace.right, upAxis);
			forwardAxis = ProjectDirectionOnPlane(playerInputSpace.forward, upAxis);
		}
		else
		{
			rightAxis = ProjectDirectionOnPlane(Vector3.right, upAxis);
			forwardAxis = ProjectDirectionOnPlane(Vector3.forward, upAxis);
		}

		if (Swimming)
		{
			desiresClimbing = false;
		}
		else
		{
			// Polling for jump input
			desiredJump |= Input.GetButtonDown("Jump");
			desiresClimbing = Input.GetButton("Climb");
		}
		
		meshRenderer.material = Climbing ? climbingMaterial :
			Swimming ? swimmingMaterial : normalMaterial;
	}

	private void FixedUpdate()
	{
		Vector3 gravity = CustomGravity.GetGravity(body.position, out upAxis);
		UpdateState();

		if (InWater)
		{
			velocity *= 1f - waterDrag * submergence *  Time.deltaTime;
		}
		AdjustVelocity();

		if (desiredJump)
		{
			desiredJump = false;
			Jump(gravity);
		}

		if (Climbing)
		{
			// Stick to wall
			velocity -= contactNormal * (maxClimbAcceleration * 0.9f * Time.deltaTime);
		}
		else if (InWater)
		{
			// Float in the water
			velocity += 
				gravity * ((1f - buoyancy * submergence) * Time.deltaTime); 
		}
		else if (OnGround && velocity.sqrMagnitude < 0.01f) // Standing still on a slope
		{
			// Stick to the slope
			velocity += contactNormal *
				(Vector3.Dot(gravity, contactNormal) * Time.deltaTime);
		}
		else if (desiresClimbing && OnGround)
		{
			// Slow us when we want to climb but aren't climbing yet
			velocity += 
				(gravity - contactNormal * (maxClimbAcceleration * 0.9f)) * Time.deltaTime;
		}
		else
		{
			velocity += gravity * Time.deltaTime;
		}
		
		// Apply calculated velocity. This is how movement is applied
		body.velocity = velocity;

		ClearState();
	}

	/**
	 * Calculates velocity and modifies it here
	 * Since velocity is a member variable, it is not returned here
	 * but instead just modified.
	 */
	void AdjustVelocity()
	{
		float acceleration, speed;
		// determining the projected X and Z axes by projecting
		// the custom right and forward vectors on the contact normal.
		// Vectors are normalized to get proper directions
		Vector3 xAxis, zAxis;
		if (Climbing)
		{
			acceleration = maxClimbAcceleration;
			speed = maxClimbSpeed;
			xAxis = Vector3.Cross(contactNormal, upAxis);
			zAxis = upAxis;
		}
		else if (InWater)
		{
			// The deeper we are in the water, the more we should depend on the swim accel. and speed
			// We interpolate between regular and swim values based on swim factor,
			// which is submergence divided by swim threshhold constrained to a max of 1.
			float swimFactor = Mathf.Min(1f, submergence / swimThreshhold);
			acceleration = Mathf.LerpUnclamped(
				OnGround ? maxAcceleration : maxAirAcceleration,
				maxSwimAcceleration, swimFactor
				);
			speed = Mathf.LerpUnclamped(maxSpeed, maxSwimSpeed, swimFactor);
			xAxis = rightAxis;
			zAxis = forwardAxis;
		}
		else
		{
			acceleration = OnGround ? maxAcceleration : maxAirAcceleration;
			speed = OnGround && desiresClimbing ? maxClimbSpeed : maxSpeed;
			xAxis = rightAxis;
			zAxis = forwardAxis;
		}
		xAxis = ProjectDirectionOnPlane(xAxis, contactNormal);
		zAxis = ProjectDirectionOnPlane(zAxis, contactNormal);

		// Allows for velocity relative to a moving platform
		Vector3 relativeVelocity = velocity - connectionVelocity;
		// project the current velocity on both vectors to get the relative X and Z speeds.
		float currentX = Vector3.Dot(relativeVelocity, xAxis);
		float currentZ = Vector3.Dot(relativeVelocity, zAxis);

		// Apply delta time to acceleration for speed change
		float maxSpeedChange = acceleration * Time.deltaTime;

		// Move from the current x and z speeds to the desired speeds using speed change
		float newX = Mathf.MoveTowards(currentX, playerInput.x * speed, maxSpeedChange);
		float newZ = Mathf.MoveTowards(currentZ, playerInput.y * speed, maxSpeedChange);

		// Fancy velocity calculation
		velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);

		if (Swimming)
		{
			float currentY = Vector3.Dot(relativeVelocity, upAxis);
			float newY = Mathf.MoveTowards(
				currentY, playerInput.z * speed, maxSpeedChange
				);
			velocity += upAxis * (newY - currentY);
		}
	}

	/**
	 * Used for correctly projecting direction on a plane
	 * Returns a vector correlating to the axis
	 * achieved by determining how much slope we have.
	 * Modified to work with custom gravity
	 * and image describing here: https://catlikecoding.com/unity/tutorials/movement/physics/slopes/projecting-vector.png
	 */

	Vector3 ProjectDirectionOnPlane (Vector3 direction, Vector3 normal)
	{
		return (direction - normal * Vector3.Dot(direction, normal)).normalized;
	}

	// Clears collision data
	void ClearState()
	{
		groundContactCount = steepContactCount = climbContactCount =  0;
		contactNormal = steepNormal = climbNormal = Vector3.zero;
		connectionVelocity = Vector3.zero;
		previousConnectedBody = connectedBody;
		connectedBody = null;
		submergence = 0f;
	}

	void UpdateState()
	{
		stepsSinceLastGrounded += 1;
		stepsSinceLastJump += 1;
		velocity = body.velocity;
		// This order of checks matters. Climbing overrides swimming, which overrides the ground check, etc.
		if (CheckClimbing() || CheckSwimming() || OnGround || SnapToGround() || CheckSteepContacts())
		{
			stepsSinceLastGrounded = 0;
			if (stepsSinceLastJump > 1) // Avoid false landing
			{
				jumpPhase = 0;
			}
			if (groundContactCount > 1)
			{
				contactNormal.Normalize();
			}
		}
		else
		{
			contactNormal = upAxis;
		}

		if (connectedBody)
		{
			if (connectedBody.isKinematic || connectedBody.mass >= body.mass)
			{
				UpdateConnectionState();
			}
		}
	}

	void UpdateConnectionState()
	{
		if (connectedBody == previousConnectedBody)
		{
			Vector3 connectionMovement =
			 connectedBody.transform.TransformPoint(connectionLocalPosition) - 
			 connectionWorldPosition;
			connectionVelocity = connectionMovement / Time.deltaTime;
		}
		connectionWorldPosition = body.position;
		connectionLocalPosition = connectedBody.transform.InverseTransformPoint(connectionWorldPosition);
	}

	void Jump(Vector3 gravity)
	{
		Vector3 jumpDirection;
		if (OnGround)
		{
			jumpDirection = contactNormal;
		}
		else if (OnSteep)
		{
			jumpDirection = steepNormal;
			jumpPhase = 0;
		}
		else if (maxAirJumps > 0 && jumpPhase <= maxAirJumps)
		{
			if (jumpPhase == 0)
			{
				jumpPhase = 1;
			}
			jumpDirection = contactNormal;
		}
		else
		{
			return;
		}

		stepsSinceLastJump = 0;
		jumpPhase += 1;
		float jumpSpeed = Mathf.Sqrt(2f * gravity.magnitude * jumpHeight);
		// Make it harder to jump while in water
		if (InWater)
		{
			jumpSpeed *= Mathf.Max(0f, 1f - submergence / swimThreshhold);
		}

		jumpDirection = (jumpDirection + upAxis).normalized; // Upwards jump bias, for wall jumps
		float alignedSpeed = Vector3.Dot(velocity, jumpDirection);
		if (alignedSpeed > 0f)
		{
			jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
		}
		velocity += jumpDirection * jumpSpeed;
	}

	void OnCollisionEnter(Collision collision)
	{
		EvaluateCollision(collision);
	}

	void OnCollisionStay(Collision collision)
	{
		EvaluateCollision(collision);
	}

	void OnTriggerEnter(Collider other)
	{
		if ((waterMask & (1 << other.gameObject.layer)) != 0)
		{
			EvaluateSubmergence(other);
		}
	}

	void OnTriggerStay(Collider other)
	{
		if ((waterMask & (1 << other.gameObject.layer)) != 0)
		{
			EvaluateSubmergence(other);
		}
	}

	void EvaluateSubmergence(Collider collider)
	{
		// Shoot a raycast from the top of the sphere downwards,
		// ending at the submergence offset.
		if (Physics.Raycast(
			body.position + upAxis * submergenceOffset,
			-upAxis, out RaycastHit hit, submergenceRange + 1f,
			waterMask, QueryTriggerInteraction.Collide
			))
		{
			submergence = 1f - hit.distance / submergenceRange;
		}
		else
		{
			submergence = 1f;
		}

		if (Swimming)
		{
			connectedBody = collider.attachedRigidbody;
		}
	}

	void EvaluateCollision(Collision collision)
	{
		if (Swimming)
		{
			return;
		}
		int layer = collision.gameObject.layer;
		float minDot = GetMinDot(layer);
		for (int i = 0; i < collision.contactCount; i++)
		{
			Vector3 normal = collision.GetContact(i).normal;
			float upDot = Vector3.Dot(upAxis, normal);
			// We're on ground
			if (upDot >= minDot)
			{
				groundContactCount += 1;
				contactNormal += normal;
				connectedBody = collision.rigidbody;
			}
			else // Not on ground
			{
				// On a slope
				if (upDot > -0.01f)
				{
					steepContactCount += 1;
					steepNormal += normal;
					if (groundContactCount == 0)
					{
						connectedBody = collision.rigidbody;
					}
				}
				// Touching a wall
				if (
					desiresClimbing && upDot >= minClimbDotProduct && 
					(climbMask & (1 << layer)) != 0)
				{
					climbContactCount += 1;
					climbNormal += normal;
					lastClimbNormal = normal;
					connectedBody = collision.rigidbody;
				}
			}
		}
	}

	bool SnapToGround()
	{
		// Determine when NOT to snap to ground

		// If not immediately after leaving the ground
		if (stepsSinceLastGrounded > 1 || stepsSinceLastJump <= 2)
		{
			return false;
		}
		float speed = velocity.magnitude;
		if (speed > maxSnapSpeed)
		{
			return false;
		}
		// If there is nothing below the sphere
		if (!Physics.Raycast(body.position, -upAxis, out RaycastHit hit, probeDistance, probeMask, QueryTriggerInteraction.Ignore))
		{
			return false;
		}

		// Determine if what we hit is ground
		float upDot = Vector3.Dot(upAxis, hit.normal);
		if (upDot < GetMinDot(hit.collider.gameObject.layer))
		{
			return false;
		}

		// snap to ground
		groundContactCount = 1;
		contactNormal = hit.normal;
		float dot = Vector3.Dot(velocity, hit.normal);
		if (dot > 0f)
		{
			velocity = (velocity - hit.normal * dot).normalized * speed;
		}
		connectedBody = hit.rigidbody;
		return true;
	}

	float GetMinDot (int layer)
	{
		return (stairsMask & (1 << layer)) == 0 ? minGroundDotProduct : minStairsDotProduct;
	}

	bool CheckSteepContacts()
	{
		if (steepContactCount > 1)
		{
			steepNormal.Normalize();
			float upDot = Vector3.Dot(upAxis, steepNormal);
			if (upDot >= minGroundDotProduct)
			{
				groundContactCount = 1;
				contactNormal = steepNormal;
				return true;
			}
		}
		return false;
	}

	// returns whether we're climbing,
	// also makes ground contact count and normal equal
	// to climbing equivalents
	bool CheckClimbing()
	{
		if (Climbing)
		{
			// Crevasse fix
			if (climbContactCount > 1)
			{
				climbNormal.Normalize();
				float upDot = Vector3.Dot(upAxis, climbNormal);
				if (upDot >= minGroundDotProduct)
				{
					climbNormal = lastClimbNormal;
				}
			}
			groundContactCount = 1;
			contactNormal = climbNormal;
			return true;
		}
		return false;
	}

	// Returns whether we're swimming, and if so sets the ground
	// contact to zero, makes the contact normal equal to the 
	// up axis.
	bool CheckSwimming()
	{
		if (Swimming)
		{
			groundContactCount = 0;
			contactNormal = upAxis;
			return true;
		}
		return false;
	}

	// Allows an outside object to propel the sphere into the air
	// by disabling it's snapping
	public void PreventSnapToGround()
	{
		stepsSinceLastJump = -1;
	}
}
