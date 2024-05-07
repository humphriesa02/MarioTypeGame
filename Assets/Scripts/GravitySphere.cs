using UnityEngine;

public class GravitySphere : GravitySource
{
	[SerializeField]
	float gravity = 9.81f;

	[SerializeField, Min(0f), Tooltip("Represents the distance up to which gravity is at full strength.")]
	float outerRadius = 10f;
	[SerializeField, Min(0f), Tooltip("Represents the distance up to which gravity begins to fall off.")]
	float outerFalloffRadius = 15f;

	[SerializeField, Min(0f), Tooltip("Represents the distance up to which gravity is at full internal to the sphere")]
	float innerRadius = 5f;
	[SerializeField, Min(0f), Tooltip("Represents the distance up to which gravity begins to fall off internal to the sphere")]
	float innerFalloffRadius = 1f;

	// The rate at which we move from the outer radius to the outer falloff radius
	float outerFalloffFactor;
	// The rate at which we move from the inner radius to the inner falloff radius
	float innerFalloffFactor;

	private void Awake()
	{
		OnValidate();
	}

	private void OnValidate()
	{
		innerFalloffRadius = Mathf.Max(innerFalloffRadius, 0f);
		innerRadius = Mathf.Max(innerRadius, innerFalloffRadius);
		outerRadius = Mathf.Max(outerRadius, innerRadius);
		// Enforce that the outerFalloutRadius is equal to or greater than outer radius
		outerFalloffRadius = Mathf.Max(outerFalloffRadius, outerRadius);

		innerFalloffFactor = 1f / (innerRadius - innerFalloffRadius);
		outerFalloffFactor = 1f / (outerFalloffRadius - outerRadius);
	}

	// Works by finding the vector pointing from the position to the sphere's center.
	// The distance is that vector's magnitude. If it's beyond the outer falloff radius,
	// then there's no gracity. Otherwise it's the normalized vector scaled by the
	// configured gravity. Note that again we use a positive gravity to represent standard
	// pulling, while negative gravity would push objects away.
	public override Vector3 GetGravity(Vector3 position)
	{
		Vector3 vector = transform.position - position;
		float distance = vector.magnitude;
		// If the player is outside the falloff radius, don't affect them with gravity
		if (distance > outerFalloffRadius || distance < innerFalloffRadius)
		{
			return Vector3.zero;
		}
		// Else calculate the gravity based on distance
		float g = gravity / distance;
		if (distance > outerRadius)
		{
			// If they're falling off of gravity, calculate it here
			g *= 1f - (distance - outerRadius) * outerFalloffFactor;
		}
		else if (distance < innerRadius)
		{
			g *= 1f - (innerRadius - distance) * innerFalloffFactor;
		}
		return g * vector;
	}

	private void OnDrawGizmos()
	{
		Vector3 p = transform.position;
		if (innerFalloffRadius > 0f && innerFalloffRadius < innerRadius)
		{
			Gizmos.color = Color.cyan;
			Gizmos.DrawWireSphere(p, innerFalloffRadius);
		}
		Gizmos.color = Color.yellow;
		if (innerRadius > 0f && innerRadius < outerRadius)
		{
			Gizmos.DrawWireSphere(p, innerRadius);
		}
		Gizmos.DrawWireSphere(p, outerRadius);
		if (outerFalloffRadius > outerRadius)
		{
			Gizmos.color = Color.cyan;
			Gizmos.DrawWireSphere (p, outerFalloffRadius);
		}
	}
}
