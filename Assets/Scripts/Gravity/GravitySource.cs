using UnityEngine;

/**
 * Base class for gravity sources.
 * Contains the virtual "GetGravity" function,
 * which gets gravity relative to the source.
 */
public class GravitySource : MonoBehaviour
{
	// On a base level, the get gravity takes the position
	// of the player, and returns some sort of gravity relative to it.
    public virtual Vector3 GetGravity (Vector3 position)
    {
        return Physics.gravity;
    }

	// Currently, when a gravity source is enabled, it gets
	// added to the CustomGravity manager's sources list.
	private void OnEnable()
	{
		CustomGravity.Register(this);	
	}
	// Currently, when a gravity source is disabled, it gets
	// removed from the CustomGravity manager's sources list.
	private void OnDisable()
	{
		CustomGravity.Unregister(this);
	}
}
