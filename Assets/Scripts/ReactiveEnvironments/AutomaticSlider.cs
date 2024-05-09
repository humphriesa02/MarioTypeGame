using UnityEngine;
using UnityEngine.Events;

public class AutomaticSlider : MonoBehaviour
{
    [SerializeField, Min(0.01f), Tooltip("Duration of the slider")]
    float duration = 1f;

	[SerializeField, Tooltip("Allows a platform to move back and forth automatically.")]
	public bool autoReverse = false;

	public bool AutoReverse
	{
		get => autoReverse; set => autoReverse = value;
	}

	[SerializeField, Tooltip("Smooths the interpolation for reversing.")]
	bool smoothstep = false;

	// IMPORTANT, since unity cannot serialize a generic event type (float),
	// meaning it wouldn't show up in inspector, we create our own serializeable
	// event type, which extends the float unity event type.
	[System.Serializable]
	public class OnValueChangedEvent : UnityEvent<float> { }

	// Uses the serialized onvaluechanged event class
	[SerializeField, Tooltip("Used to pass along the current value to the slider.")]
	OnValueChangedEvent onValueChanged = default;

    float value;

	public bool Reversed { get; set; }

	// Smoothstep function
	float SmoothedValue => 3f * value * value - 2f * value * value * value;

	void FixedUpdate()
	{
		float delta = Time.deltaTime / duration;
		if (Reversed)
		{
			value -= delta;
			if (value <= 0f)
			{
				if (AutoReverse)
				{
					value = Mathf.Min(1f, -value);
					Reversed = false;
				}
				else
				{
					value = 0f;
					enabled = false;
				}
			}
		}
		else
		{
			value += delta;
			if (value >= 1f)
			{
				if (AutoReverse)
				{
					value = Mathf.Max(0f, 2f - value);
					Reversed = true;
				}
				else
				{
					value = 1f;
					enabled = false;
				}
			}
		}
		onValueChanged.Invoke(smoothstep ? SmoothedValue : value);
	}
}
