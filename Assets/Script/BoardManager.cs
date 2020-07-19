using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
	[System.Serializable]
	public struct PlayLine
	{
		public string name;
		public Vector2 pos;
		public Vector2 dir;
		public float len;
	}
	[SerializeField] PlayLine[] playLines;

	[Header("Board Config")]
	[SerializeField] float spawnCube = 6;
	[SerializeField] float boardCube = 7;
	[Space]
	[SerializeField] float playSideLen = 5;
	[SerializeField] int playSideChecks = 5;
	[SerializeField] float reflectSideLen = 6;
	[SerializeField] int reflectSideChecks = 9;
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
		public Vector2 rVec;
		public float speed;
	}
	[SerializeField] List<StrikeInfo> strikeInfo;
	public float? GetRayToLineSegmentIntersection(Vector3 rayOrg, Vector3 rayDir, Vector3 point1, Vector3 point2)
	{
		var v1 = rayOrg - point1;
		var v2 = point2 - point1;
		var v3 = new Vector3(-rayDir.y, rayDir.x, 0);

		var dot = Vector2.Dot(v2, v3);
		if (Mathf.Abs(dot) < 0.000001f)
			return null;

		var rM = Vector3.Cross(v2, v1) / dot;
		var lM = Vector2.Dot(v1, v3) / dot;

		if (rM.z >= 0.0f && (lM >= 0.0f && lM <= 1.0f))
			return rM.z;

		return null;
	}
	private List<StrikeInfo> Calc_DirectStrike(PlayLine pLine, int lSplit, int potID)
	{
		var hitInfo = new List<StrikeInfo>();
		Vector2 pot, puck;
		for (int p = 0; p < pucks.Length; p++)
		{
			if (pucks[p].gameObject.activeSelf)
			{
				puck = pucks[p].position;
				pot = pots[potID].position;

				// OBSTACLE Check : If Path is Clear from This PUCK to Target POT
				var puck2Pot = Physics2D.CircleCastAll(puck, puckRadius, (pot - puck).normalized, boardCube * 2, 1 << 9);
				if (puck2Pot.Length > 1)
					continue;

				//Some Platform Indipendent Vector Math, to get 
				var hitDir = (pot - puck);
				var dist = hitDir.magnitude;
				hitDir.Normalize();
				var hitPos = (puck - hitDir * (puckRadius + strikerRadius));

				var info = new StrikeInfo(); // We will store the best Hit Vector for this PUCK-POT combo.
				for (int i = -1; i <= lSplit; i++) // to ccheck more than one possiblity to take the shot
				{
					Vector2 sPos;
					if (i < 0) // For Best possible shot
					{
						var rM = GetRayToLineSegmentIntersection(hitPos, -hitDir, pLine.pos, pLine.pos + pLine.dir * pLine.len);
						if (rM != null && rM.HasValue)
							sPos = hitPos - hitDir * rM.Value;
						else
							continue;
					}
					else //when Best line of sight isn't possible, we will try for other positions
					{
						sPos = pLine.pos + pLine.dir * pLine.len * (i / (float)lSplit);
					}

					// OBSTACLE Check : If Position is Clear for Striker Placement (in case a puck is blocking)
					if (Physics2D.OverlapCircle(sPos, strikerRadius, 1 << 9) != null)
						continue;

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
							info.potID = potID;

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
		return hitInfo;
	}
	private List<StrikeInfo> Calc_ReflectedStrike(PlayLine rLine, int lSplit, int potID, PlayLine pLine)
	{
		var hitInfo = new List<StrikeInfo>();
		Vector2 pot, puck;
		for (int p = 0; p < pucks.Length; p++)
		{
			if (pucks[p].gameObject.activeSelf)
			{
				puck = pucks[p].position;
				pot = pots[potID].position;

				// OBSTACLE Check : If Path is Clear from This PUCK to Target POT
				var puck2Pot = Physics2D.CircleCastAll(puck, puckRadius, (pot - puck).normalized, boardCube * 2, 1 << 9);
				if (puck2Pot.Length > 1)
					continue;

				//Some Platform Indipendent Vector Math, to get 
				var hitDir = (pot - puck);
				var dist = hitDir.magnitude;
				hitDir.Normalize();
				var hitPos = (puck - hitDir * (puckRadius + strikerRadius));

				var info = new StrikeInfo(); // We will store the best Hit Vector for this PUCK-POT combo.
				for (int i = -1; i <= lSplit; i++) // to ccheck more than one possiblity to take the shot
				{
					Vector2 sPos;
					if (i < 0) // For Best possible shot
					{
						var rM = GetRayToLineSegmentIntersection(hitPos, -hitDir, rLine.pos, rLine.pos + rLine.dir * rLine.len);
						if (rM != null && rM.HasValue)
							sPos = hitPos - hitDir * rM.Value;
						else
							continue;
					}
					else //when Best line of sight isn't possible, we will try for other positions
					{
						sPos = rLine.pos + rLine.dir * rLine.len * (i / (float)lSplit);
					}

					// OBSTACLE Check : If Position is Clear for Striker Placement (in case a puck is blocking)
					if (Physics2D.OverlapCircle(sPos, strikerRadius, 1 << 9) != null)
						continue;

					var hitVect = (hitPos - sPos);
					var hitDot = Vector2.Dot(hitDir, hitVect.normalized);
					var tSpeed = dist + hitVect.magnitude; //Linear Drag is 1, So X speed will cover X distance.
					var sDir = hitVect.normalized;
					tSpeed = (tSpeed / hitDot) + speedAdder;
					tSpeed = Mathf.Clamp(tSpeed, speedMinMax.x, speedMinMax.y);
					if (tSpeed < speedMinMax.y && hitDot > strikeThreshold && hitDot > info.hDot)
					{
						//OBSTACLE Check :  If Path is Clear from This Position to This PUCK
						var Reflect2Puck = Physics2D.CircleCast(sPos, strikerRadius, sDir, boardCube * 2, 1 << 9);
						if (Reflect2Puck.rigidbody == pucks[p])
						{
							// Let's Check for Reflection THEN
							var reflect = Vector2.Reflect(-sDir, Vector2.Perpendicular(rLine.dir));
							var rM = GetRayToLineSegmentIntersection(sPos, reflect, pLine.pos, pLine.pos + pLine.dir * pLine.len);
							if (rM != null && rM.HasValue)
							{
								if (tSpeed < speedMinMax.y && hitDot > strikeThreshold)
								{
									info.pos = sPos + reflect * rM.Value;
									info.dir = -reflect;
									//OBSTACLE Check :  If Path is Clear from Striker to Refletion Point
									if (!Physics2D.CircleCast(info.pos, strikerRadius, info.dir, rM.Value, 1 << 9))
									{
										info.puckID = p;
										info.potID = potID;
										info.tar = sPos;
										info.rVec = hitVect;
										info.hDot = hitDot;
										info.speed = tSpeed + rM.Value;
									}
								}
							}
						}
					}
				}
				if (info.speed != 0)
					hitInfo.Add(info);
			}
		}
		return hitInfo;
	}

	private void Calc_BestShot()
	{
		if (strikeInfo == null)
			strikeInfo = new List<StrikeInfo>();
		strikeInfo.Clear();

		var d1 = Calc_DirectStrike(playLines[0], playSideChecks, 0);
		strikeInfo.AddRange(d1);

		var d2 = Calc_DirectStrike(playLines[0], playSideChecks, 1);
		strikeInfo.AddRange(d2);

		var b1 = Calc_ReflectedStrike(playLines[5], reflectSideChecks, 3, playLines[0]);
		strikeInfo.AddRange(b1);

		var b2 = Calc_ReflectedStrike(playLines[5], reflectSideChecks, 2, playLines[0]);
		strikeInfo.AddRange(b2);

		var s1 = Calc_ReflectedStrike(playLines[4], reflectSideChecks, 1, playLines[0]);
		strikeInfo.AddRange(s1);

		var s2 = Calc_ReflectedStrike(playLines[6], reflectSideChecks, 0, playLines[0]);
		strikeInfo.AddRange(s2);

		if (strikeInfo.Count > 0)
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
			striker.velocity = Vector2.zero;
		}
	}
	#endregion

	#region GamePlayFunc
	private void Take_Shot()
	{
		striker.gameObject.SetActive(true);
		striker.velocity = strikeDir * strikeSpeed;
	}
	private void Take_Shot(int id)
	{
		if (id >= 0 && id < strikeInfo.Count)
		{
			strikeDir = strikeInfo[id].dir;
			strikeSpeed = strikeInfo[id].speed;
			striker.transform.position = strikeInfo[id].pos;
		}
		Take_Shot();
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
	[SerializeField] int shotID = -1;
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
				Calc_BestShot();
			}
		}
		if (strike)
		{
			strike = false;
			Take_Shot(shotID);
		}

		Gizmos.color = Color.blue;
		if (playLines != null)
			for (int p = 0; p < 4; p++)
				Gizmos.DrawLine(playLines[p].pos, playLines[p].pos + playLines[p].dir * playSideLen);
		if (playLines != null)
			for (int p = 4; p < 8; p++)
				Gizmos.DrawLine(playLines[p].pos, playLines[p].pos + playLines[p].dir * reflectSideLen);
		if (strikeInfo != null && strikeInfo.Count > 0)
		{
			for (int s = 0; s < strikeInfo.Count; s++)
			{
				if (strikeInfo[s].rVec != Vector2.zero)
				{
					Gizmos.color = Color.yellow;
					Gizmos.DrawLine(strikeInfo[s].tar, strikeInfo[s].tar + strikeInfo[s].rVec);
					Gizmos.DrawWireSphere(strikeInfo[s].tar + strikeInfo[s].rVec, strikerRadius);
				}
				else
				{
					Gizmos.color = Color.green;
				}
				Gizmos.DrawLine(strikeInfo[s].pos, strikeInfo[s].tar);
				Gizmos.DrawWireSphere(strikeInfo[s].tar, strikerRadius);
				Gizmos.DrawLine(pucks[strikeInfo[s].puckID].position, pots[strikeInfo[s].potID].position);
			}
		}
	}
#endif
}
