using UnityEngine;

public class AccelerationZone : MonoBehaviour
{
	[SerializeField, Min(0f), Tooltip("Acceleration rate to get the body to the desired speed. Make this 0 for instantaneous speed boost.")]
	float acceleration = 10f;

	[SerializeField, Min(0f), Tooltip("Speed to accelerate the object to.")]
	float speed = 10f;

	void OnTriggerEnter(Collider other)
	{
		Rigidbody body = other.attachedRigidbody;
		if (body)
		{
			Accelerate(body);
		}
	}

	void OnTriggerStay(Collider other)
	{
		Rigidbody body = other.attachedRigidbody;
        if (body)
        {
			Accelerate(body);
        }
    }

	void Accelerate(Rigidbody body)
	{
		Vector3 velocity = transform.InverseTransformDirection(body.velocity);
		if (velocity.y >= speed)
		{
			return;
		}
		if (acceleration > 0f)
		{
			// Accelerate the body towards intended speed
			velocity.y = Mathf.MoveTowards(
				velocity.y, speed, acceleration * Time.deltaTime
			);
		}
		else
		{
			velocity.y = speed;
		}
		body.velocity = transform.TransformDirection(velocity);

		if (body.TryGetComponent(out MovingSphere sphere))
		{
			sphere.PreventSnapToGround();
		}
	}
}
