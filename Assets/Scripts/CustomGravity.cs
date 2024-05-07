using UnityEngine;
using System.Collections.Generic;

/**
 * Gravity managing system that keeps track of what gravity
 * is currently affecting the player / world at a given time.
 */
public static class CustomGravity
{
    // A list of gravity sources acting upon the player at a given time.
    static List<GravitySource> sources = new List<GravitySource>();

    // Adds up all the gravity values from the different gravity
    // sources and returns it
    public static Vector3 GetGravity (Vector3 position)
    {
        Vector3 g = Vector3.zero;
        for (int i = 0; i < sources.Count; i++)
        {
            g += sources[i].GetGravity(position);
        }
        return g;
    }

	// Adds up all the gravity values from the different gravity
	// sources based on the given position, and returns it.
    // Also modifies the incoming upAxis based on the given gravity.
	public static Vector3 GetGravity (Vector3 position, out Vector3 upAxis)
    {
		Vector3 g = Vector3.zero;
		for (int i = 0; i < sources.Count; i++)
		{
			g += sources[i].GetGravity(position);
		}
        upAxis = -g.normalized;
        return g;
    }

    // Gets the up axis based on the given position and
    // the gravity sources in the sources list.
    public static Vector3 GetUpAxis (Vector3 position)
    {
		Vector3 g = Vector3.zero;
		for (int i = 0; i < sources.Count; i++)
		{
			g += sources[i].GetGravity(position);
		}
        return -g.normalized;
	}

    public static void Register (GravitySource source)
    {
        Debug.Assert(
            !sources.Contains(source),
            "Duplicate registration of gravity source!", source
        );
        sources.Add(source);
    }

    public static void Unregister (GravitySource source)
    {
		Debug.Assert(
			sources.Contains(source),
			"Unregistration of unknown gravity source!", source
		);
		sources.Remove(source);
    }
}
