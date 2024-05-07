using UnityEngine;

public class GravityBox : GravitySource
{
	[SerializeField]
	float gravity = -9.81f;

	[SerializeField, Tooltip("Way to define the box's shape, this acts as a radius but for all three dimensions separately.")]
	Vector3 boundaryDistance = Vector3.one;

	[SerializeField, Min(0f), Tooltip("Represents distance inward relative to the boundary inside the cube at which gravity is full strength.")]
	float innerDistance = 0f;
	[SerializeField, Min(0f), Tooltip("Represents distance inward relative to the boundary inside the cube at which gravity falls off.")]
	float innerFalloffDistance = 0f;

	[SerializeField, Min(0f), Tooltip("Represents distance outward relative to the boundary outside the cube at which gravity is full strength.")]
	float outerDistance = 0f;
	[SerializeField, Min(0f), Tooltip("Represents distance outward relative to the boundary outside the cube at which gravity falls off.")]
	float outerFalloffDistance = 0f;

	// The rate at which we move from the inner radius to the inner falloff radius
	float innerFalloffFactor;

	float outerFalloffFactor;
	private void Awake()
	{
		OnValidate();
	}

	private void OnValidate()
	{
		// Minimum boundary is zero
		boundaryDistance = Vector3.Max(boundaryDistance, Vector3.zero);
		// Max for both inner distances is uqual ot the smallest boundary distance
		float maxInner = Mathf.Min(
			Mathf.Min(boundaryDistance.x, boundaryDistance.y), boundaryDistance.z
			);
		// Inner distance is either the inner distance provided, or the max inner distance calculated
		innerDistance = Mathf.Min(innerDistance, maxInner);
		// innerFalloffDistance is either the innerDistance, or the max between the innerfalloffdistance and maxInner.
		innerFalloffDistance = Mathf.Max(
			Mathf.Min(innerFalloffDistance, maxInner), innerDistance);
		outerFalloffDistance = Mathf.Max(outerFalloffDistance, outerDistance);

		innerFalloffFactor = 1f / (innerFalloffDistance - innerDistance);
		outerFalloffFactor = 1f / (outerFalloffDistance - outerDistance);
	}

	/**
	 * coordinate - relevant position coordinate relative to the box's center
	 * distance - distance to the nearest face along the relevant axis.
	 * 
	 * returns the gravity component along the same axis
	 */
	float GetGravityComponent (float coordinate, float distance)
	{
		// In the null-gravity zone
		if (distance > innerFalloffDistance)
		{
			return 0f;
		}
		float g = gravity;
		// If we need to reduce gravity (approaching null-zone)
        if (distance > innerDistance)
        {
			// Reduce gravity
			g *= 1f - (distance - innerDistance) * innerFalloffFactor;
        }
		// have to flip gravity if the coordinate is less than zero,
		// since that means we're on the opposite side of the center.
        return coordinate > 0f ? -g : g;
	}

	public override Vector3 GetGravity(Vector3 position)
	{
		// Make the incoming position relative to the box's position
		position = transform.InverseTransformDirection(position - transform.position);
		// Start with a zero vector
		Vector3 vector = Vector3.zero;

		// Check if the position lies beyond the right face.
		// If so, set the vector's X component to the X boundary minus
		// the X position. Adjusts the vector to point straight to the face instead
		// of the center.
		int outside = 0;
		if (position.x > boundaryDistance.x)
		{
			vector.x = boundaryDistance.x - position.x;
			outside = 1;
		}
		// Beyond the left face
		else if (position.x < -boundaryDistance.x)
		{
			vector.x = -boundaryDistance.x - position.x;
			outside = 1;
		}

		if (position.y > boundaryDistance.y)
		{
			vector.y = boundaryDistance.y - position.y;
			outside += 1;
		}
		if (position.y < -boundaryDistance.y)
		{
			vector.y = -boundaryDistance.y - position.y;
			outside += 1;
		}

		if (position.z > boundaryDistance.z)
		{
			vector.z = boundaryDistance.z - position.z;
			outside += 1;
		}
		if (position.z < -boundaryDistance.z)
		{
			vector.z = -boundaryDistance.z - position.z;
			outside += 1;
		}

		// We are outside the box.
		if (outside > 0)
		{
			// If we are only outside a single face, we can take the abs sum of the 
			// vector components instead of calculating the length of an arbitrary vector.
			float distance = outside == 1 ?
				Mathf.Abs(vector.x + vector.y + vector.z) : vector.magnitude;
			if (distance > outerFalloffDistance)
			{
				return Vector3.zero;
			}
			float g = gravity / distance;
			if (distance > outerDistance)
			{
				g *= 1f - (distance - outerDistance) * outerFalloffFactor;
			}
			return transform.TransformDirection(g * vector);
		}

		// Absolute distances from the center.
		Vector3 distances;
		distances.x = boundaryDistance.x - Mathf.Abs(position.x);
		distances.y = boundaryDistance.y - Mathf.Abs(position.y);
		distances.z = boundaryDistance.z - Mathf.Abs(position.z);

		// Find smallest distance
		if (distances.x < distances.y) // X value is smaller than y
		{
			if (distances.x < distances.z) // X value is smaller than z
			{
				// assign the vector's x value the gravity
				vector.x = GetGravityComponent(position.x, distances.x);
			}
			else // Z value is smaller than Y and X
			{
				// assign the vector's z value the gravity
				vector.z = GetGravityComponent(position.z, distances.z);
			}
		}
		else if (distances.y < distances.z) // Y is smaller than Z and X
		{
			// Assign the vector's Y value the gravity.
			vector.y = GetGravityComponent(position.y, distances.y);
		}
		else // Last to be checked is Z, if we get this far, Z is smallest.
		{
			// Assign the vector's Z value the gravity.
			vector.z = GetGravityComponent(position.z, distances.z);
		}

		// To support cubes with arbitrary rotation, need to rotate the relative
		// position to align with the cube.
		return transform.TransformDirection(vector);
	}

	void DrawGizmosRect (Vector3 a, Vector3 b, Vector3 c, Vector3 d)
	{
		Gizmos.DrawLine(a, b);
		Gizmos.DrawLine(b, c);
		Gizmos.DrawLine(c, d);
		Gizmos.DrawLine(d, a);
	} 

	// Draw an outer cube, given a distance. 
	void DrawGizmosOuterCube (float distance)
	{
		Vector3 a, b, c, d;
		a.y = b.y = boundaryDistance.y;
		d.y = c.y = -boundaryDistance.y;
		b.z = c.z = boundaryDistance.z;
		d.z = a.z = -boundaryDistance.z;
		a.x = b.x = c.x = d.x = boundaryDistance.x + distance;
		DrawGizmosRect(a, b, c, d);
		a.x = b.x = c.x = d.x = -a.x;
		DrawGizmosRect(a, b, c, d);

		a.x = d.x = boundaryDistance.x;
		b.x = c.x = -boundaryDistance.x;
		a.z = b.z = boundaryDistance.z;
		c.z = d.z = -boundaryDistance.z;
		a.y = b.y = c.y = d.y = boundaryDistance.y + distance;
		DrawGizmosRect(a, b, c, d);
		a.y = b.y = c.y = d.y = -a.y;
		DrawGizmosRect(a, b, c, d);

		a.x = d.x = boundaryDistance.x;
		b.x = c.x = -boundaryDistance.x;
		a.y = b.y = boundaryDistance.y;
		c.y = d.y = -boundaryDistance.y;
		a.z = b.z = c.z = d.z = boundaryDistance.z + distance;
		DrawGizmosRect(a, b, c, d);
		a.z = b.z = c.z = d.z = -a.z;
		DrawGizmosRect(a, b, c, d);

		distance *= 0.5773502692f;
		Vector3 size = boundaryDistance;
		size.x = 2f * (size.x + distance);
		size.y = 2f * (size.y + distance);
		size.z = 2f * (size.z + distance);
		Gizmos.DrawWireCube(Vector3.zero, size);
	}

	private void OnDrawGizmos()
	{
		Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
		Vector3 size;
		if (innerFalloffDistance > innerDistance)
		{
			Gizmos.color = Color.cyan;
			size.x = 2f * (boundaryDistance.x - innerFalloffDistance);
			size.y = 2f * (boundaryDistance.y - innerFalloffDistance);
			size.z = 2f * (boundaryDistance.z - innerFalloffDistance);
			Gizmos.DrawWireCube(Vector3.zero, size);
		}
		if (innerDistance > 0f)
		{
			Gizmos.color = Color.yellow;
			size.x = 2f * (boundaryDistance.x - innerDistance);
			size.y = 2f * (boundaryDistance.y - innerDistance);
			size.z = 2f * (boundaryDistance.z - innerDistance);
			Gizmos.DrawWireCube(Vector3.zero, size);
		}

		Gizmos.color = Color.red;
		Gizmos.DrawWireCube(Vector3.zero, 2f * boundaryDistance);

		if (outerDistance > 0f)
		{
			Gizmos.color = Color.yellow;
			DrawGizmosOuterCube(outerDistance);
		}
		if (outerFalloffDistance > outerDistance)
		{
			Gizmos.color = Color.cyan;
			DrawGizmosOuterCube(outerFalloffDistance);
		}
	}
}
