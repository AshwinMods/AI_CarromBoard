using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pot : MonoBehaviour
{
	[SerializeField] float catchSpeedThreshold = 5;
	float thresholdSQ = 0;
	private void OnEnable()
	{
		thresholdSQ = Mathf.Pow(catchSpeedThreshold, 2);
	}
	private void OnTriggerStay2D(Collider2D collision)
	{
		if (collision.attachedRigidbody != null)
		{
			var rb = collision.attachedRigidbody;
			if (rb.velocity.sqrMagnitude < thresholdSQ)
			{
				rb.velocity = Vector2.zero;
				rb.MovePosition(transform.position);
				rb.gameObject.SetActive(false);
			}
		}
	}
}