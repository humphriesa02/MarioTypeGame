using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class DetectionZone : MonoBehaviour
{
	[SerializeField, Tooltip("Used for events that are called as an object enters and leaves the detection zone.")]
	UnityEvent onFirstEnter = default, onLastExit = default;

	List<Collider> colliders = new List<Collider>();

	void Awake()
	{
		// Only enable this detection zone if something is in it.
		enabled = false;	
	}

	void FixedUpdate()
	{
		// Loop through colliders, checking if they get deleted from the scene,
		// or deactivated. If so, remove from list of colliders.
		for (int i = 0; i < colliders.Count; i++)
		{
			Collider collider = colliders[i];
			if (!collider || !collider.gameObject.activeInHierarchy)
			{
				colliders.RemoveAt(i--);
				if (colliders.Count == 0)
				{
					onLastExit.Invoke();
					enabled = false;
				}
			}
		}
	}

	void OnTriggerEnter(Collider other)
	{
		if (colliders.Count == 0)
		{
			onFirstEnter.Invoke();
			enabled = true;
		}
		colliders.Add(other);
	}

	void OnTriggerExit(Collider other)
	{
		if (colliders.Remove(other) && colliders.Count == 0)
		{
			onLastExit.Invoke();
			enabled = false;
		}
	}

	// If the detection zone is disabled or deleted,
	// clear the colliders and invoke the exit function.
	void OnDisable()
	{
#if UNITY_EDITOR
		if (enabled && gameObject.activeInHierarchy)
		{
			return;
		}
#endif
		if (colliders.Count > 0)
		{
			colliders.Clear();
			onLastExit.Invoke();
		}
	}
}
