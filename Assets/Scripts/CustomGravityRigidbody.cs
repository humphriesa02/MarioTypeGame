using UnityEngine;

[RequireComponent (typeof(Rigidbody))]
public class CustomGravityRigidbody : MonoBehaviour
{
	[SerializeField, Tooltip("Whether a body is allowed to float so it can go to sleep.")]
	bool floatToSleep = false;

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

		body.AddForce(CustomGravity.GetGravity(body.position), ForceMode.Acceleration);
	}
}
