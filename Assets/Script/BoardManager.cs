using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
	[Header("Board Config")]
	[SerializeField] float spawnCube = 6;
	[SerializeField] float boardCube = 7;
	[SerializeField] float strikerRadius = 0.37f;
	[SerializeField] float puckRadius = 0.26f;
	[SerializeField] float potRadius = 0.37f;
	[SerializeField] float strikeThreshold = 0.5f;
	[SerializeField] float speedAdder = 1;
	[SerializeField] Vector2 speedMinMax = Vector2.one + Vector2.up * 49;

	[Header("Reference")]
	[SerializeField] Rigidbody2D striker;
	[SerializeField] Rigidbody2D[] pucks;
	[SerializeField] Transform[] pots;

	[Header("Realtime")]
	[SerializeField] Transform tPuck;
	[SerializeField] Transform tPot;
	[SerializeField] Vector2 strikeDir = Vector2.up;
	[SerializeField] float strikeSpeed = 10;
	private void Calc_Strike(Vector2 pot, Vector2 puck, out Vector2 dirVect, out float dist, out float sDot)
	{
		var tDir = (pot - puck);
		dist = tDir.magnitude;
		tDir.Normalize();
		var tPos = (puck - tDir * (puckRadius + strikerRadius));
		dirVect = (tPos - (Vector2)striker.transform.position);
		dist += dirVect.magnitude;
		sDot = Vector2.Dot(tDir, dirVect.normalized);
	}
	private void Strike()
	{
		striker.velocity = strikeDir * strikeSpeed;
	}

	private void Reset_Tokens()
	{
		var pos = Vector3.zero;
		for (int t = 0; t < pucks.Length; t++)
		{
			pos.x = Random.Range(-spawnCube, spawnCube);
			pos.y = Random.Range(-spawnCube, spawnCube);
			pucks[t].velocity = Vector2.zero;
			pucks[t].transform.position = pos;
		}
	}

#if UNITY_EDITOR
	[Header("Editor")]
	[SerializeField] bool placeTokens = false;
	[SerializeField] bool calcDir = false;
	[SerializeField] bool strike = false;
	[Space]
	[SerializeField] float sSpeed;
	[SerializeField] Vector2 sVect;
	[SerializeField] float sAttackDot;
	[SerializeField] bool sValid = false;
	private void OnDrawGizmos()
	{
		if (placeTokens)
		{
			placeTokens = false;
			Reset_Tokens();
		}
		if (calcDir)
		{
			if (tPot && tPuck)
			{
				Calc_Strike(tPot.position, tPuck.position, out sVect, out sSpeed, out sAttackDot);
				Gizmos.DrawWireSphere(striker.position + sVect, strikerRadius);
				Gizmos.DrawRay(striker.position + sVect, tPot.position - tPuck.position);
				Gizmos.DrawRay(striker.position, sVect);
				strikeDir = sVect.normalized;
				sSpeed /= sAttackDot;
				strikeSpeed = Mathf.Clamp(sSpeed + speedAdder, speedMinMax.x, speedMinMax.y);
				sValid = (sSpeed < speedMinMax.y && sAttackDot > strikeThreshold);
			}
		}
		if (strike)
		{
			strike = false;
			Strike();
		}
	}
#endif
}
