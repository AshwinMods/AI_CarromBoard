using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
	[System.Serializable]
	public struct Player
	{
		public string name;
		public Vector2 pos;
		public Vector2 dir;
	}
	[SerializeField] Player[] players;

	[Header("Board Config")]
	[SerializeField] float spawnCube = 6;
	[SerializeField] float boardCube = 7;
	[Space]
	[SerializeField] float playSideLen = 5;
	[SerializeField] int playSideChecks = 5;
	[Space]
	[SerializeField] float strikerRadius = 0.37f;
	[SerializeField] float puckRadius = 0.26f;
	[SerializeField] float potRadius = 0.37f;
	[Space]
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

	#region AI_Calculations
	// I prefer Struct but using Class here for accessibilty. (yeah, ask me :P)
	[System.Serializable]
	public class StrikeInfo
	{
		public int potID;
		public int puckID;
		public float hDot;
		public Vector2 pos;
		public Vector2 dir;
		public Vector2 tar;
		public float speed;
	}
	[SerializeField] List<StrikeInfo> strikeInfo;

	private void Calc_HitPos(Vector2 pot, Vector2 puck, out Vector2 hitPos, out Vector2 hitDir, out float tDist)
	{
		hitDir = (pot - puck);
		tDist = hitDir.magnitude;
		hitDir.Normalize();
		hitPos = (puck - hitDir * (puckRadius + strikerRadius));
	}

	private void Calc_DirectStrike(int plrID, out List<StrikeInfo> hitInfo)
	{
		hitInfo = new List<StrikeInfo>();
		Vector2 pot, puck;
		for (int p = 0; p < pucks.Length; p++)
		{
			if (pucks[p].gameObject.activeSelf)
			{
				puck = pucks[p].position;
				for (int t = 0; t < 2; t++)
				{
					pot = pots[(plrID + t) % 4].position;

					// OBSTACLE Check : If Path is Clear from This PUCK to Target POT
					var puck2Pot = Physics2D.CircleCastAll(puck, puckRadius, (pot - puck).normalized, boardCube * 2, 1 << 9);
					if (puck2Pot.Length > 1)
						continue;

					var info = new StrikeInfo(); // We will store the best Hit Vector for this PUCK-POT combo.
					for (int i = 0; i <= playSideChecks; i++) //A smooth maneuver could have done a better job, but let's just take Samples for now.
					{
						// OBSTACLE Check : If Position is Clear for Striker Placement (in case a puck is blocking)
						var sPos = players[plrID].pos + players[plrID].dir * playSideLen * (i / (float)playSideChecks);
						if (Physics2D.OverlapCircle(sPos, strikerRadius, 1 << 9) != null)
							continue;

						//Some Platform Indipendent Vector Math, to get 
						var hitDir = (pot - puck);
						var dist = hitDir.magnitude;
						hitDir.Normalize();
						var hitPos = (puck - hitDir * (puckRadius + strikerRadius));
						var hitVect = (hitPos - sPos);
						var hitDot = Vector2.Dot(hitDir, hitVect.normalized);
						var tSpeed = dist + hitVect.magnitude; //Linear Drag is 1, So X speed will cover X distance.
						var sDir = hitVect.normalized;
						tSpeed = (tSpeed / hitDot) + speedAdder;
						tSpeed = Mathf.Clamp(tSpeed, speedMinMax.x, speedMinMax.y);
						if (tSpeed < speedMinMax.y && hitDot > strikeThreshold && hitDot > info.hDot)
						{
							//OBSTACLE Check :  If Path is Clear from This Position to This PUCK
							var striker2Puck = Physics2D.CircleCast(sPos, strikerRadius, sDir, boardCube * 2, 1 << 9);
							if (striker2Puck.rigidbody == pucks[p])
							{
								info.puckID = p;
								info.potID = (plrID + t) % 4;

								info.pos = sPos;
								info.dir = sDir;
								info.tar = hitPos;
								info.hDot = hitDot;
								info.speed = tSpeed;
							}
						}
					}
					if (info.speed != 0)
						hitInfo.Add(info);
				}
			}
		}
	}
	#endregion

	#region GamePlayFunc
	private void AI_Strike()
	{
		if (strikeInfo != null && strikeInfo.Count > 0)
		{
			int bestStrike = 0;
			float compBase = 0;
			for (int s = 0; s < strikeInfo.Count; s++)
			{
				if (compBase < strikeInfo[s].hDot)
				{
					compBase = strikeInfo[s].hDot;
					bestStrike = s;
				}
			}
			strikeDir = strikeInfo[bestStrike].dir;
			strikeSpeed = strikeInfo[bestStrike].speed;
			striker.transform.position = strikeInfo[bestStrike].pos;
		}
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
			pucks[t].gameObject.SetActive(true);
		}
	}
	#endregion

#if UNITY_EDITOR
	[Header("Editor")]
	[SerializeField] bool placeTokens = false;
	[SerializeField] bool calcDir = false;
	[SerializeField] bool strike = false;
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
				calcDir = false;
				Calc_DirectStrike(0, out strikeInfo);
			}
		}
		if (strike)
		{
			strike = false;
			AI_Strike();
		}

		if (players != null)
		{
			for (int p = 0; p < players.Length; p++)
			{
				Gizmos.DrawLine(players[p].pos, players[p].pos + players[p].dir * playSideLen);
			}
		}
		if (strikeInfo != null && strikeInfo.Count > 0)
		{
			for (int s = 0; s < strikeInfo.Count; s++)
			{
				Gizmos.DrawLine(strikeInfo[s].pos, strikeInfo[s].tar);
				Gizmos.DrawWireSphere(strikeInfo[s].tar, strikerRadius);
				Gizmos.DrawLine(pucks[strikeInfo[s].puckID].position, pots[strikeInfo[s].potID].position);
			}
		}
	}
#endif
}
