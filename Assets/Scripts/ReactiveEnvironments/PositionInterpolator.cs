using UnityEngine;

public class PositionInterpolator : MonoBehaviour
{
    [SerializeField, Tooltip("The rigidbody to be moved by this component.")]
    Rigidbody body = default;

    [SerializeField, Tooltip("A transform to move the rigidbody relative to.")]
    Transform relativeTo = default;

    [SerializeField, Tooltip("Starting location of interpolation.")]
    Vector3 from = default;

    [SerializeField, Tooltip("Ending location of interpolation.")]
    Vector3 to = default;

    // Called to a move a rigidbody from one place to another using
    // t as a timestep.
    public void Interpolate (float t)
    {
        Vector3 p;
        if (relativeTo)
        {
            p = Vector3.LerpUnclamped(
                relativeTo.TransformPoint(from), relativeTo.TransformPoint(to), t
                );
        }
        else
        {
            p = Vector3.LerpUnclamped(from, to, t);

		}
        body.MovePosition(p);
    }

	private void OnDrawGizmos()
	{
		Gizmos.matrix = relativeTo.localToWorldMatrix;
		Gizmos.color = Color.red;
        Gizmos.DrawWireCube(from, Vector3.one);
		Gizmos.color = Color.green;
		Gizmos.DrawWireCube(to, Vector3.one);
	}
}
