using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
	[Header("Board Config")]
	[SerializeField] float spawnBox;
	[SerializeField] float strikerRadius = 0.37f;
	[SerializeField] float tokenRadius = 0.26f;
	[SerializeField] float potRadius = 0.37f;

	[Header("Reference")]
	[SerializeField] Rigidbody2D striker;
	[SerializeField] Rigidbody2D[] tokens;
	[SerializeField] Transform[] pots;

	[Header("Realtime")]
	[SerializeField] Rigidbody2D target;
	private void Strike()
	{
		
	}
	private void Reset_Tokens()
	{
		var pos = Vector3.zero;
		for (int t = 0; t < tokens.Length; t++)
		{
			pos.x = Random.Range(-spawnBox, spawnBox);
			pos.y = Random.Range(-spawnBox, spawnBox);

			tokens[t].MovePosition(pos);
			tokens[t].transform.position = pos;
		}
	}

#if UNITY_EDITOR
	[Header("Editor")]
	[SerializeField] bool placeTokens = false;
	[SerializeField] bool strike = false;
	private void OnDrawGizmosSelected()
	{
		if (placeTokens)
		{
			placeTokens = false;
			Reset_Tokens();
		}
		if (strike)
		{
			strike = false;
			Strike();
		}
	}
#endif
}
