using UnityEngine;

[RequireComponent (typeof(Rigidbody))]
public class CustomGravityRigidbody : MonoBehaviour
{
	[SerializeField, Tooltip("Whether a body is allowed to float so it can go to sleep.")]
	bool floatToSleep = false;

	[Header("Water Interaction")]
	[SerializeField, Tooltip("Offset from the center of the object, dedicating a point to measure range.")]
	float submergenceOffset = 0.5f;

	[SerializeField, Min(0.1f), Tooltip("Range from offset to end of object.")]
	float submergenceRange = 1f;

	[SerializeField, Min(0f), Tooltip("How much the obhect floats. A value of 0 sinks like a rock, a value of 1 is in equilibrium," +
		" anything greater than 1 will float to the surface. A buoyancy of 2 would mean it rises as fast as it would normally fall.")]
	float buoyancy = 1f;

	[SerializeField, Tooltip("Objects often float with their lightest side up. We simulate this by adding a offset vector to apply buoyancy to.")]
	Vector3 buoyancyOffset = Vector3.zero;

	[SerializeField, Range(0f, 10f), Tooltip("Drag placed on the object while in water.")]
	float waterDrag = 1f;

	[SerializeField, Tooltip("Used to determine if object is in water. Assign to water layer.")]
	LayerMask waterMask = 0;

	float submergence;

	Vector3 gravity;

	Rigidbody body;

	// We assume the body is floating but still might fall
	float floatDelay;

	private void Awake()
	{
		body = GetComponent<Rigidbody>();
		body.useGravity = false;
	}

	private void FixedUpdate()
	{
		if (floatToSleep)
		{
			// Don't add force if the body is not moving

			if (body.IsSleeping())
			{
				return;
			}

			if (body.velocity.sqrMagnitude < 0.0001f)
			{
				floatDelay += Time.deltaTime;
				if (floatDelay >= 1f)
					return;
			}
			else
			{
				floatDelay = 0f;
			}
		}

		gravity = CustomGravity.GetGravity(body.position);
		if (submergence > 0f)
		{
			float drag = Mathf.Max(0f, 1f - waterDrag * submergence * Time.deltaTime);
			body.velocity *= drag;
			body.angularVelocity *= drag;
			body.AddForceAtPosition(
				gravity * -(buoyancy * submergence),
				transform.TransformPoint(buoyancyOffset),
				ForceMode.Acceleration
				);
			submergence = 0f;
		}

		body.AddForce(gravity, ForceMode.Acceleration);
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
		if (
			!body.IsSleeping() &&
			(waterMask & (1 << other.gameObject.layer)) != 0)
		{
			EvaluateSubmergence(other);
		}
	}

	void EvaluateSubmergence(Collider collider)
	{
		Vector3 upAxis = -gravity.normalized;
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
	}
}
